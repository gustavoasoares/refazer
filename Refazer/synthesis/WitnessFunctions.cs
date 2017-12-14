using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using IronPython.Modules;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Rules;
using Microsoft.ProgramSynthesis.Specifications;
using Microsoft.ProgramSynthesis.Utils;
using Tutor.synthesis;

namespace Tutor.Transformation
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class WitnessFunctions : DomainLearningLogic
    {
        public WitnessFunctions(Grammar grammar) : base(grammar) { }

        [WitnessFunction("Apply",1)]
        public ExampleSpec WitnessPatch(GrammarRule rule, ExampleSpec spec)
        {
            var examples = new Dictionary<State, object>();
            foreach (State input in spec.ProvidedInputs)
            {
                var before = (PythonNode)input[rule.Body[0]];
                var after = (PythonNode)spec.Examples[input];

                var zss = new PythonZss(before, after);
                var editDistance = zss.Compute();

                var rootAndNonRootEdits = SplitEditsByRootsAndNonRoots(editDistance);
                var edits = ExtractPrimaryEdits(rootAndNonRootEdits, editDistance);

                //todo fix some big examples that are not working 
                //if (edits.Count > 18)
                //    return null;

                var patch = new Patch();
                edits.ForEach(e => patch.EditSets.Add(new List<Edit>() { e }));
                examples[input] = patch;
            }
            return new ExampleSpec(examples);
        }

        private static List<Edit> ExtractPrimaryEdits(Tuple<List<Edit>, List<Edit>> rootAndNonRootEdits,
            EditDistance editDistance)
        {
            var edits = new List<Edit>();
            foreach (var root in rootAndNonRootEdits.Item1)
            {
                edits.Add(root);
                UpdateIds(root.ModifiedNode.Children, editDistance);

                if (root is Insert)
                {
                    var parent = root.TargetNode;
                    foreach (var child in parent.Children)
                    {
                        PythonNode mappedNodeInTheResultingAst = null;
                        foreach (var keyValuePair in editDistance.Mapping)
                        {
                            if (keyValuePair.Value.Equals(child))
                            {
                                mappedNodeInTheResultingAst = keyValuePair.Key;
                            }
                        }
                        if (mappedNodeInTheResultingAst == null)
                            continue;

                        if (InsertedNodeContainsNode(root.ModifiedNode, mappedNodeInTheResultingAst))
                        {
                            //create a delete operation to represented the moved node
                            var delete = new Delete(child, root.TargetNode);
                            edits.Add(delete);
                        }
                    }
                }
                if (root is Delete)
                {
                    var children = NotDeletedChildren(root.ModifiedNode,
                        rootAndNonRootEdits.Item2.Where(e => e is Delete));
                    if (children.Any())
                    {
                        foreach (var child in children)
                        {
                            var inserts = rootAndNonRootEdits.Item1.Where(
                                e => (e is Insert) && e.TargetNode.Equals(root.TargetNode));

                            PythonNode mappedNodeInTheResultingAst = null;
                            foreach (var keyValuePair in editDistance.Mapping)
                            {
                                if (keyValuePair.Value.Equals(child))
                                {
                                    mappedNodeInTheResultingAst = keyValuePair.Key;
                                }
                            }
                            if (mappedNodeInTheResultingAst == null)
                                throw new Exception("No mapped node found");

                            var isUsed = false;
                            foreach (var insert in inserts)
                            {
                                if (insert.ModifiedNode.Contains(mappedNodeInTheResultingAst))
                                    isUsed = true;
                            }
                            if (!isUsed)
                            {
                                //create a move in the parent node
                                var move = new Move(child, root.TargetNode,
                                    mappedNodeInTheResultingAst.Parent.Children.IndexOf(mappedNodeInTheResultingAst));
                                edits.Add(move);
                            }
                        }
                    }
                }
            }
            return edits;
        }

        private static void UpdateIds(List<PythonNode> children, EditDistance editDistance)
        {
            foreach (var pythonNode in children)
            {
                if (editDistance.Mapping.ContainsKey(pythonNode))
                {
                    var mapped = editDistance.Mapping[pythonNode];
                    pythonNode.Id = mapped.Id;
                }
                UpdateIds(pythonNode.Children, editDistance);
            }
        }

        private static bool InsertedNodeContainsNode(PythonNode modifiedNode, PythonNode child)
        {
            if (child.Equals(modifiedNode))
                return true;

            foreach (var pythonNode in modifiedNode.Children)
            {
                var childResult = InsertedNodeContainsNode(pythonNode, child);
                if (childResult)
                    return true;
            }
            return false;
        }

        public Tuple<List<Edit>, List<Edit>> SplitEditsByRootsAndNonRoots(EditDistance editDistance)
        {

            var roots = new List<Edit>();
            var nonroots = new List<Edit>();

            foreach (var edit in editDistance.Edits)
            {
                var isRoot = true;
                //check if the target node is a resulting node of another edit. If so, 
                //this edit is not root
                foreach (var edit1 in editDistance.Edits)
                {
                    if (edit1.Equals(edit))
                        continue;

                    if (edit.TargetNode.Equals(edit1.ModifiedNode))
                    {
                        isRoot = false;
                        break;
                    }
                    if (edit is Update && edit1 is Insert)
                    {
                        foreach (var child in edit1.ModifiedNode.Children)
                        {
                            if (edit.ModifiedNode.Equals(child))
                            {
                                isRoot = false;
                                break;
                            }
                        }
                    }
                }

                //if the edit is performed on an node in the input tree
                //add it as a root edit. Otherwise, it is a nonroot edit,
                //that is, an edit that belongs to a parent edit.  
                if (isRoot)
                {
                    if (edit is Insert)
                    {
                        var insert = (Insert)edit;
                        insert.TargetNode = editDistance.Mapping[insert.TargetNode];
                        insert.Index = insert.ModifiedNode.Parent.Children.IndexOf(insert.ModifiedNode);
                    }

                    roots.Add(edit);
                }
                else
                {
                    nonroots.Add(edit);
                }
            }
            return Tuple.Create(roots, nonroots);
        }

        private static List<PythonNode> NotDeletedChildren(PythonNode node, IEnumerable<Edit> edits)
        {
            var result = new List<PythonNode>();
            foreach (var child in node.Children)
            {
                var isDeleted = false;
                foreach (var edit in edits)
                {
                    if (edit.ModifiedNode.Equals(child))
                        isDeleted = true;
                }
                if (!isDeleted)
                    result.Add(child);
                else
                {
                    var childResult = NotDeletedChildren(child, edits);
                    result.AddRange(childResult);
                }
            }
            return result;
        }

        [WitnessFunction("Patch", 0)]
        public SubsequenceSpec WitnessSingleEditSet(GrammarRule rule, 
            ExampleSpec spec)
        {
            var examples = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var patch = spec.Examples[input] as Patch;
                if (patch == null || !patch.EditSets.Any() || patch.EditSets.Count() > 1)
                    return null;
                examples[input] = patch.EditSets.First();
            }
            return new SubsequenceSpec(examples);
        }

        [WitnessFunction("CPatch", 0)]
        public SubsequenceSpec WitnessHeadPatch(GrammarRule rule, 
            ExampleSpec spec)
        {
            var examples = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var patch = spec.Examples[input] as Patch;
                if (patch == null || patch.EditSets.Count() < 2)
                    return null;
                examples[input] = patch.EditSets.First();
            }
            return new SubsequenceSpec(examples);
        }

        [WitnessFunction("CPatch", 1)]
        public ExampleSpec WitnessTailPatch(GrammarRule rule, 
            ExampleSpec spec)
        {
            var examples = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var patch = spec.Examples[input] as Patch;
                if (patch == null || patch.EditSets.Count() < 2)
                    return null;
                var tail = new List<List<Edit>>(patch.EditSets);
                tail.RemoveAt(0);
                var newPatch = new Patch(tail);
                examples[input] = newPatch;
            }
            return new ExampleSpec(examples);
        }


        [WitnessFunction("EditMap", 1)]
        public SubsequenceSpec WitnessSelectedNodes(GrammarRule rule, 
                                                 SubsequenceSpec spec)
        {
            var examples = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var edits = spec.PositiveExamples[input] as IEnumerable<Edit>;
                examples[input] = edits.Select(e => e.TargetNode);
            }
            return new SubsequenceSpec(examples);
        }



        [WitnessFunction("Match", 1)]
        public DisjunctiveExamplesSpec WitnessMatchTemplate(GrammarRule rule, 
            ExampleSpec spec)
        {
            var examples = new Dictionary<State, IEnumerable<object>>();
            foreach (State input in spec.ProvidedInputs)
            {
                var contextPairs = new List<Tuple<int, PythonNode>>();
                var selectedNode = (PythonNode)input[rule.Body[0]];
                contextPairs.Add(Tuple.Create(0, selectedNode));
                if (selectedNode.Parent != null)
                {
                    contextPairs.Add(Tuple.Create(1, selectedNode));
                }
                examples[input] = contextPairs;
            }
            return new DisjunctiveExamplesSpec(examples);
        }

        [WitnessFunction("Update", 1)]
        public ExampleSpec WitnessN2(GrammarRule rule,  ExampleSpec spec)
        {
            var contextExamples = new Dictionary<State, object>();
            foreach (State input in spec.ProvidedInputs)
            {
                var operation = spec.Examples[input] as Update;
                if (operation == null)
                    return null;
                contextExamples[input] = operation.ModifiedNode;
            }
            return new ExampleSpec(contextExamples);
        }

        [WitnessFunction("Insert", 1)]
        public ExampleSpec WitnessInsertN(GrammarRule rule,  ExampleSpec spec)
        {
            var contextExamples = new Dictionary<State, object>();
            foreach (State input in spec.ProvidedInputs)
            {
                var operation = spec.Examples[input] as Insert;
                if (operation == null)
                    return null;
                contextExamples[input] = operation.ModifiedNode;
            }
            return new ExampleSpec(contextExamples);
        }

        [WitnessFunction("Insert", 2)]
        public DisjunctiveExamplesSpec WitnessInsertK(GrammarRule rule,  ExampleSpec spec)
        {
            var contextExamples = new Dictionary<State, IEnumerable<object>>();
            foreach (State input in spec.ProvidedInputs)
            {
                var operation = spec.Examples[input] as Insert;
                if (operation == null)
                    return null;
                var positions = new List<int>();
                positions.Add(operation.Index);
                if (operation.Index == operation.TargetNode.Children.Count)
                    positions.Add(-1);
                contextExamples[input] = positions.Cast<object>();
            }
            return DisjunctiveExamplesSpec.From(contextExamples);
        }

        [WitnessFunction("Move", 1)]
        public ExampleSpec WitnessMoveN(GrammarRule rule,  ExampleSpec spec)
        {
            var contextExamples = new Dictionary<State, object>();
            foreach (State input in spec.ProvidedInputs)
            {
                var operation = spec.Examples[input] as Move;
                if (operation == null)
                    return null;
                contextExamples[input] = operation.ModifiedNode;
            }
            return new ExampleSpec(contextExamples);
        }

        [WitnessFunction("Move", 2)]
        public DisjunctiveExamplesSpec WitnessMoveK(GrammarRule rule,  ExampleSpec spec)
        {
            var contextExamples = new Dictionary<State, IEnumerable<object>>();
            foreach (State input in spec.ProvidedInputs)
            {
                var operation = spec.Examples[input] as Move;
                if (operation == null)
                    return null;
                var positions = new List<int>();
                positions.Add(operation.Index);
                if (operation.Index == operation.TargetNode.Children.Count)
                    positions.Add(-1);
                contextExamples[input] = positions.Cast<object>();
            }
            return DisjunctiveExamplesSpec.From(contextExamples);
        }

        [WitnessFunction("Delete", 1)]
        public ExampleSpec WitnessDeleteK(GrammarRule rule,  ExampleSpec spec)
        {
            var contextExamples = new Dictionary<State, object>();
            foreach (State input in spec.ProvidedInputs)
            {
                var operation = spec.Examples[input] as Delete;
                if (operation == null)
                    return null;
                contextExamples[input] = operation.ModifiedNode;
            }
            return new ExampleSpec(contextExamples);
        }


        [WitnessFunction("LeafConstNode", 0)]
        public ExampleSpec WitnessInfo(GrammarRule rule,  ExampleSpec spec)
        {
            var contextExamples = new Dictionary<State, object>();
            foreach (State input in spec.ProvidedInputs)
            {
                var node = spec.Examples[input] as PythonNode;
                if (node == null || node.Children.Any())
                    return null;

                var info = NodeInfo.CreateInfo(node);
                contextExamples[input] = info;

            }
            return new ExampleSpec(contextExamples);
        }

        [WitnessFunction("ConstNode", 0)]
        public ExampleSpec WitnessInfo2(GrammarRule rule,  ExampleSpec spec)
        {
            var contextExamples = new Dictionary<State, object>();
            foreach (State input in spec.ProvidedInputs)
            {
                var node = spec.Examples[input] as PythonNode;
                if (node == null || !node.Children.Any())
                    return null;

                var info = NodeInfo.CreateInfo(node);
                contextExamples[input] = info;

            }
            return new ExampleSpec(contextExamples);
        }

        [WitnessFunction("ConstNode", 1)]
        public ExampleSpec WitnessChildren(GrammarRule rule,  ExampleSpec spec)
        {
            var contextExamples = new Dictionary<State, object>();
            foreach (State input in spec.ProvidedInputs)
            {
                var node = spec.Examples[input] as PythonNode;
                if (node == null || !node.Children.Any())
                    return null;

                contextExamples[input] = node.Children;

            }
            return new ExampleSpec(contextExamples);
        }



        private static PythonNode GetAncestor(PythonNode node, int d)
        {
            if (d < 0)
                throw new ArgumentOutOfRangeException();
            var result = node;
            while (d > 0)
            {
                if (result.Parent == null)
                    throw new Exception("Null parent node");
                result = result.Parent;
                d--;
            }
            return result;
        }

        [WitnessFunction("Context", 0)]
        public DisjunctiveExamplesSpec WitnessContextK(GrammarRule rule, 
            DisjunctiveExamplesSpec spec)
        {
            var result = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var outerSpec = spec.DisjunctiveExamples[input] as IEnumerable<Tuple<int, PythonNode>>;
                var innerSpec = new List<int>();
                if (outerSpec != null)
                {
                    innerSpec = outerSpec.Select(e => e.Item1).ToList();
                    result[input] = innerSpec.Cast<object>();
                }
                else
                {
                    return null;
                }
            }
            return new DisjunctiveExamplesSpec(result);
        }

        [WitnessFunction("Context", 1, DependsOnParameters = new[] { 0 })]
        public ExampleSpec WitnessContextTemplate(GrammarRule rule, 
          DisjunctiveExamplesSpec spec, ExampleSpec dSpec)
        {
            var result = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var outerSpec = spec.DisjunctiveExamples[input] as IEnumerable<Tuple<int, PythonNode>>;
                var d = (int)dSpec.Examples[input];
                var deepSpec = (int)dSpec.Examples[input];
                if (outerSpec != null)
                {
                    var node = outerSpec.Where(e => e.Item1 == d).First().Item2;
                    result[input] = new TreeTemplate(GetAncestor(node, d), node);
                }
                else
                {
                    return null;
                }
            }
            return new ExampleSpec(result);
        }

        [WitnessFunction("Context", 2, DependsOnParameters = new[] { 0 })]
        public ExampleSpec WitnessContextPath(GrammarRule rule, 
            DisjunctiveExamplesSpec spec, ExampleSpec dSpec)
        {
            var result = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var outerSpec = spec.DisjunctiveExamples[input] as IEnumerable<Tuple<int, PythonNode>>;
                var dependentSpec = (int)dSpec.Examples[input];
                if (outerSpec != null)
                {
                    if (dependentSpec == 0)
                        result[input] = 0;
                    else
                    {
                        var node = outerSpec.Where(e => e.Item1 == 0).First().Item2;
                        result[input] = node.Parent.Children.IndexOf(node) + 1;
                    }
                }
                else
                {
                    return null;
                }
            }
            return new ExampleSpec(result);
        }

        [WitnessFunction("Relative", 0)]
        public DisjunctiveExamplesSpec WitnessRelativeToken(GrammarRule rule,  ExampleSpec spec)
        {
            var result = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var example = spec.DisjunctiveExamples[input].First() as Tuple<PythonNode, PythonNode>;
                if (example != null)
                {
                    //if the anchor node is the target node, we don't generate relative paths
                    //just the root path "0"
                    if (AnchorAndTargetAreEqual(example))
                        return null;
                    var node = example.Item1;
                    var treeTemplate = new TreeTemplate(node.GetType().Name);
                    if (node.Value != null) treeTemplate.Value = node.Value;

                    result[input] = new List<TreeTemplate>() { treeTemplate, new Wildcard(node.GetType().Name) };
                }
                else
                {
                    return null;
                }
            }
            return DisjunctiveExamplesSpec.From(result);
        }

        private static bool AnchorAndTargetAreEqual(Tuple<PythonNode, PythonNode> example)
        {
            return example.Item1.Equals(example.Item2);
        }

        [WitnessFunction("Path", 0)]
        public ExampleSpec WitnessPath(GrammarRule rule,  ExampleSpec spec)
        {
            var result = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var outerSpec = (int)spec.Examples[input];
                if (outerSpec != null)
                {
                    //var innerSpec = outerSpec.Select(e => GetAncestor(e.Item2, e.Item1).Equals(e.Item2) ? 0 
                    //: GetAncestor(e.Item2, e.Item1).Children.IndexOf(e.Item2)).ToList();
                    result[input] = outerSpec;
                }
                else
                {
                    return null;
                }
            }
            return new ExampleSpec(result);
        }

        //[WitnessFunction("Relative", 1, DependsOnParameters = new[] { 0 }) ]
        //public ExampleSpec WitnessRelativeK(GrammarRule rule,  ExampleSpec spec, ExampleSpec tokenSpec)
        //{
        //    var result = new Dictionary<State, object>();
        //    foreach (var input in spec.ProvidedInputs)
        //    {
        //        var example = spec.DisjunctiveExamples[input].First() as Tuple<PythonNode, PythonNode>;
        //        var template = (TreeTemplate)tokenSpec.Examples[input];
        //        if (example != null)
        //        {
        //            var parent = example.Item2;
        //            var target = example.Item1;
        //            var k = 0;
        //            var exampleK = 0;
        //            foreach (var child in parent.Children)
        //            {
        //                if (MatchRecursively(child, template, target, ref k))
        //                {
        //                    exampleK = k;
        //                }
        //            }
        //            if (exampleK != 0)
        //                result[input] = exampleK;
        //            else
        //            {
        //                return null;
        //            }
        //        }
        //        else
        //        {
        //            return null;
        //        }
        //    }
        //    return new ExampleSpec(result);
        //}        //[WitnessFunction("Relative", 1, DependsOnParameters = new[] { 0 }) ]
        //public ExampleSpec WitnessRelativeK(GrammarRule rule,  ExampleSpec spec, ExampleSpec tokenSpec)
        //{
        //    var result = new Dictionary<State, object>();
        //    foreach (var input in spec.ProvidedInputs)
        //    {
        //        var example = spec.DisjunctiveExamples[input].First() as Tuple<PythonNode, PythonNode>;
        //        var template = (TreeTemplate)tokenSpec.Examples[input];
        //        if (example != null)
        //        {
        //            var parent = example.Item2;
        //            var target = example.Item1;
        //            var k = 0;
        //            var exampleK = 0;
        //            foreach (var child in parent.Children)
        //            {
        //                if (MatchRecursively(child, template, target, ref k))
        //                {
        //                    exampleK = k;
        //                }
        //            }
        //            if (exampleK != 0)
        //                result[input] = exampleK;
        //            else
        //            {
        //                return null;
        //            }
        //        }
        //        else
        //        {
        //            return null;
        //        }
        //    }
        //    return new ExampleSpec(result);
        //}

        private static bool MatchRecursively(PythonNode current, TreeTemplate pattern, PythonNode target, ref int i)
        {
            if (pattern.Match(current))
            {
                i++;
                if (current.Equals(target))
                    return true;
            }
            foreach (var child in current.Children)
            {
                if (MatchRecursively(child, pattern, target, ref i))
                    return true;
            }
            return false;
        }


        [WitnessFunction("LeafPattern", 0)]
        public ExampleSpec WitnessLeafPatternToken(GrammarRule rule,  ExampleSpec spec)
        {
            var result = new Dictionary<State, object>();
            foreach (State input in spec.ProvidedInputs)
            {
                var outerSpec = spec.Examples[input] as TreeTemplate;
                if (outerSpec == null)
                    return null;
                result[input] = outerSpec.PythonNode;

            }
            return new ExampleSpec(result);
        }

        [WitnessFunction("Pattern", 0)]
        public ExampleSpec WitnessPatternToken(GrammarRule rule,  ExampleSpec spec)
        {
            var result = new Dictionary<State, object>();
            foreach (State input in spec.ProvidedInputs)
            {
                var outerSpec = spec.Examples[input] as TreeTemplate;
                if (outerSpec == null)
                    return null;
                var innerSpec = new List<PythonNode>();
                var pnode = outerSpec.PythonNode;
                result[input] = pnode;

            }
            return new ExampleSpec(result);
        }

        [WitnessFunction("Pattern", 1)]
        public ExampleSpec WitnessPatternChildren(GrammarRule rule,  ExampleSpec spec)
        {
            var result = new Dictionary<State, object>();
            foreach (State input in spec.ProvidedInputs)
            {
                var nodes = spec.Examples[input] as TreeTemplate;
                if (nodes == null)
                    return null;
                var examples = new List<IEnumerable<PythonNode>>();
                if (nodes.PythonNode.Children.Any())
                {
                    examples.Add(nodes.PythonNode.Children);
                }

                result[input] = examples;
            }
            return new ExampleSpec(result);
        }

        //[WitnessFunction("Node", 0)]
        //public ExampleSpec WitnessNodeInfo(GrammarRule rule,  ExampleSpec spec)
        //{
        //    var result = new Dictionary<State, object>();
        //    foreach (State input in spec.ProvidedInputs)
        //    {
        //        var outerSpec = spec.Examples[input] as PythonNode;
        //        if (outerSpec == null)
        //            return null;
        //        var nodeInfo = new NodeInfo(outerSpec.GetType().Name);
        //        if (outerSpec.Value != null) nodeInfo.NodeValue = outerSpec.Value;
        //        result[input] = nodeInfo;

        //    }
        //    return new ExampleSpec(result);
        //}

        [WitnessFunction("Type", 0)]
        public ExampleSpec WitnessType(GrammarRule rule,  ExampleSpec spec)
        {
            var result = new Dictionary<State, object>();
            foreach (State input in spec.ProvidedInputs)
            {
                var outerSpec = spec.Examples[input] as PythonNode;
                if (outerSpec == null)
                    return null;
                result[input] = outerSpec.GetType().Name;
            }
            return new ExampleSpec(result);
        }

        [WitnessFunction("Node", 0)]
        public ExampleSpec WitnessLeafNodeType(GrammarRule rule, ExampleSpec spec)
        {
            var result = new Dictionary<State, object>();
            foreach (State input in spec.ProvidedInputs)
            {
                var outerSpec = spec.Examples[input] as PythonNode;
                if (outerSpec == null)
                    return null;
                var info = new NodeInfo(outerSpec.GetType().Name);
                if (outerSpec.Value != null) info.NodeValue = outerSpec.Value;
                result[input] = info;
            }
            return new ExampleSpec(result);
        }

        [WitnessFunction("TChild", 0)]
        public ExampleSpec WitnessTemplateChild(GrammarRule rule,  ExampleSpec spec)
        {
            var result = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var children = spec.Examples[input] as List<PythonNode>;
                if (children == null || children.Count > 1)
                    return null;
                if (children.Count.Equals(1))
                    result[input] = children.First();
            }
            return new ExampleSpec(result);
        }

        [WitnessFunction("TChildren", 0)]
        public ExampleSpec WitnessTemplateChildrenHead(GrammarRule rule,  ExampleSpec spec)
        {
            var result = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var outerSpec = spec.Examples[input] as List<PythonNode>;
                if (outerSpec == null || outerSpec.Count < 2)
                    return null;
                result[input] = outerSpec.First();
            }
            return new ExampleSpec(result);
        }

        [WitnessFunction("TChildren", 1)]
        public ExampleSpec WitnessTemplateChildrenTail(GrammarRule rule,  ExampleSpec spec)
        {
            var result = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var outerSpec = spec.Examples[input] as List<PythonNode>;
                if (outerSpec == null || outerSpec.Count < 2)
                    return null;
                var newList = new List<PythonNode>();
                newList.AddRange(outerSpec);
                newList.RemoveAt(0);
                result[input] = newList;
            }
            return new ExampleSpec(result);
        }

        [WitnessFunction("ReferenceNode", 1)]
        public DisjunctiveExamplesSpec WitnessContext(GrammarRule rule,  ExampleSpec spec)
        {
            var templateExamples = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var node = spec.Examples[input] as PythonNode;
                var ast = (PythonNode)input[rule.Body[0]];
                if (ast.ContainsByBinding(node))
                {
                    node = ast.GetCorrespondingNodeByBinding(node);
                }
                else
                {
                    return null;
                }
                var innerSpec = new List<Tuple<int, PythonNode>>() { Tuple.Create(0, node) };
                if (node.Parent != null)
                    innerSpec.Add(Tuple.Create(1, node));
                templateExamples[input] = innerSpec;
            }
            return new DisjunctiveExamplesSpec(templateExamples);
        }

        [WitnessFunction("ReferenceNode", 2, DependsOnParameters = new []{1})]
        public ExampleSpec WitnessK(GrammarRule rule,  ExampleSpec spec, ExampleSpec patternSpec)
        {
            var result = new Dictionary<State, object>();

            foreach (var input in spec.ProvidedInputs)
            {
                var inp = (PythonNode)input[rule.Body[0]];
                var pattern = patternSpec.Examples[input] as Pattern;
                var node = spec.Examples[input] as PythonNode;
                if (pattern != null && node != null)
                {
                    var magicK = new MagicK(inp, node);
                    var k = magicK.GetK(pattern);
                    result[input] = k;
                }
                else
                {
                    return null;
                }
            }
            return new ExampleSpec(result);
        }

        [WitnessFunction("SingleChild", 0)]
        public ExampleSpec WitnessSingleChild(GrammarRule rule,  ExampleSpec spec)
        {
            var childrenExamples = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var children = spec.Examples[input] as IEnumerable<PythonNode>;
                if (children.Count().Equals(1))
                    childrenExamples[input] = children.First();
                else
                    return null;
            }
            return new ExampleSpec(childrenExamples);
        }

        [WitnessFunction("Children", 0)]
        public ExampleSpec WitnessHead(GrammarRule rule,  ExampleSpec spec)
        {
            var headExamples = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var children = spec.Examples[input] as IEnumerable<PythonNode>;
                if (children.Count() > 1)
                    headExamples[input] = children.First();
                else
                    return null;
            }
            return new ExampleSpec(headExamples);
        }

        [WitnessFunction("Children", 1)]
        public ExampleSpec WitnessTail(GrammarRule rule,  ExampleSpec spec)
        {
            var headExamples = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var children = spec.Examples[input] as IEnumerable<PythonNode>;
                if (children.Count() > 1)
                {
                    var newList = new List<PythonNode>();
                    newList.AddRange(children);
                    newList.RemoveAt(0);
                    headExamples[input] = newList;
                }
                else
                    return null;
            }
            return new ExampleSpec(headExamples);
        }
    }
}
