using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using IronPython.Modules;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Rules;
using Microsoft.ProgramSynthesis.Specifications;
using Microsoft.ProgramSynthesis.Utils;
using Tutor.synthesis;

namespace Tutor.Transformation
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static class WitnessFunctions
    {
        [WitnessFunction("Apply", 1)]
        public static ExampleSpec WitnessPatch(GrammarRule rule, int parameter,
                                                 ExampleSpec spec)
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
                edits.ForEach(e => patch.EditSets.Add(new List<Edit>() {e}));
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

        private static Tuple<List<Edit>, List<Edit>> SplitEditsByRootsAndNonRoots(EditDistance editDistance)
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
                        var insert = (Insert) edit;
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
            return Tuple.Create(roots,nonroots);
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
        public static SubsequenceSpec WitnessSingleEditSet(GrammarRule rule, int parameter,
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
        public static SubsequenceSpec WitnessHeadPatch(GrammarRule rule, int parameter,
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
        public static ExampleSpec WitnessTailPatch(GrammarRule rule, int parameter,
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
        public static SubsequenceSpec WitnessSelectedNodes(GrammarRule rule, int parameter,
                                                 SubsequenceSpec spec)
        {
            var examples = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var edits = spec.Examples[input] as IEnumerable<Edit>;
                examples[input] = edits.Select(e => e.TargetNode);
            }
            return new SubsequenceSpec(examples);
        }



        [WitnessFunction("Match", 1)]
        public static ExampleSpec WitnessMatchTemplate(GrammarRule rule, int parameter,
            ExampleSpec spec)
        {
            var examples = new Dictionary<State, object>();
            foreach (State input in spec.ProvidedInputs)
            {
                var selectedNode = (PythonNode)input[rule.Body[0]];
                examples[input] = selectedNode.Parent != null ? Tuple.Create(selectedNode.Parent, selectedNode) :
                    Tuple.Create(selectedNode, selectedNode);
            }
            return new ExampleSpec(examples);
        }

        [WitnessFunction("Update", 1)]
        public static ExampleSpec WitnessN2(GrammarRule rule, int parameter, ExampleSpec spec)
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
        public static ExampleSpec WitnessInsertN(GrammarRule rule, int parameter, ExampleSpec spec)
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
        public static ExampleSpec WitnessInsertK(GrammarRule rule, int parameter, ExampleSpec spec)
        {
            var contextExamples = new Dictionary<State, object>();
            foreach (State input in spec.ProvidedInputs)
            {
                var operation = spec.Examples[input] as Insert;
                if (operation == null)
                    return null;
                contextExamples[input] = operation.Index;
            }
            return new ExampleSpec(contextExamples);
        }

        [WitnessFunction("Move", 1)]
        public static ExampleSpec WitnessMoveN(GrammarRule rule, int parameter, ExampleSpec spec)
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
        public static ExampleSpec WitnessMoveK(GrammarRule rule, int parameter, ExampleSpec spec)
        {
            var contextExamples = new Dictionary<State, object>();
            foreach (State input in spec.ProvidedInputs)
            {
                var operation = spec.Examples[input] as Move;
                if (operation == null)
                    return null;
                contextExamples[input] = operation.Index;
            }
            return new ExampleSpec(contextExamples);
        }

        [WitnessFunction("Delete", 1)]
        public static ExampleSpec WitnessDeleteK(GrammarRule rule, int parameter, ExampleSpec spec)
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
        public static ExampleSpec WitnessInfo(GrammarRule rule, int parameter, ExampleSpec spec)
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
        public static ExampleSpec WitnessInfo2(GrammarRule rule, int parameter, ExampleSpec spec)
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
        public static ExampleSpec WitnessChildren(GrammarRule rule, int parameter, ExampleSpec spec)
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

        [WitnessFunction("LeafWildcard", 0)]
        public static ExampleSpec WitnessLeafWildCardType(GrammarRule rule, int parameter, ExampleSpec spec)
        {
            var variableExamples = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var contextTargets = spec.DisjunctiveExamples[input] as IEnumerable<Tuple<PythonNode, PythonNode>>;
                if (contextTargets == null)
                {
                    var list = new List<Tuple<PythonNode, PythonNode>>();
                    foreach (var element in spec.DisjunctiveExamples[input])
                    {
                        var tuple = element as Tuple<PythonNode, PythonNode>;
                        if (tuple == null)
                            return null;
                        list.Add(tuple);
                    }
                    contextTargets = list;
                }
                var types = new List<string>();
                foreach (var contextTarget in contextTargets)
                {
                    var node = contextTarget.Item1;
                    if (contextTarget.Item2 != null && contextTarget.Item2.Equals(contextTarget.Item1))
                        continue;
                    if (node != null && node.Children.Any())
                        continue;
                    types.Add(node.GetType().Name);
                }

                if (!types.Any())
                    return null;
                variableExamples[input] = types;

            }
            return DisjunctiveExamplesSpec.From(variableExamples);
        }

        [WitnessFunction("Wildcard", 0)]
        public static DisjunctiveExamplesSpec WitnessWildCardType(GrammarRule rule, int parameter, DisjunctiveExamplesSpec spec)
        {
            var variableExamples = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var contextTargets = spec.DisjunctiveExamples[input] as IEnumerable<Tuple<PythonNode, PythonNode>>;
                if (contextTargets == null)
                {
                    var list = new List<Tuple<PythonNode, PythonNode>>();
                    foreach (var element in spec.DisjunctiveExamples[input])
                    {
                        var tuple = element as Tuple<PythonNode, PythonNode>;
                        if (tuple == null)
                            return null;
                        list.Add(tuple);
                    }
                    contextTargets = list;
                }
                var types = new List<string>();
                foreach (var contextTarget in contextTargets)
                {
                    var node = contextTarget.Item1;
                    if (contextTarget.Item2 != null && contextTarget.Item2.Equals(contextTarget.Item1))
                        continue;
                    if (node == null || !node.Children.Any())
                        continue;
                    types.Add(node.GetType().Name);
                }

                if (!types.Any())
                    return null;
                variableExamples[input] = types;
            }
            return DisjunctiveExamplesSpec.From(variableExamples);
        }

        [WitnessFunction("Wildcard", 1)]
        public static DisjunctiveExamplesSpec WitnessWildcardValue(GrammarRule rule, int parameter, DisjunctiveExamplesSpec spec)
        {
            var variableExamples = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var contextTargets = spec.DisjunctiveExamples[input] as IEnumerable<Tuple<PythonNode, PythonNode>>;
                if (contextTargets == null)
                {
                    var list = new List<Tuple<PythonNode, PythonNode>>();
                    foreach (var element in spec.DisjunctiveExamples[input])
                    {
                        var tuple = element as Tuple<PythonNode, PythonNode>;
                        if (tuple == null)
                            return null;
                        list.Add(tuple);
                    }
                    contextTargets = list;
                }
                var children = new List<Tuple<List<PythonNode>, PythonNode>>();
                foreach (var contextTarget in contextTargets)
                {
                    var node = contextTarget.Item1;
                    if (contextTarget.Item2 != null && contextTarget.Item2.Equals(contextTarget.Item1))
                        continue;
                    if (node == null || !node.Children.Any())
                        continue;

                    children.Add(Tuple.Create(node.Children, contextTarget.Item2));
                }
                if (!children.Any())
                    return null;
                variableExamples[input] = children;

            }
            return DisjunctiveExamplesSpec.From(variableExamples);
        }

        [WitnessFunction("Target", 0)]
        public static DisjunctiveExamplesSpec WitnessTemplate(GrammarRule rule, int parameter, DisjunctiveExamplesSpec spec)
        {
            var variableExamples = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var contextTargets = spec.DisjunctiveExamples[input] as IEnumerable<Tuple<PythonNode, PythonNode>>;
                if (contextTargets == null)
                {
                    var list = new List<Tuple<PythonNode, PythonNode>>();
                    foreach (var element in spec.DisjunctiveExamples[input])
                    {
                        var tuple = element as Tuple<PythonNode, PythonNode>;
                        if (tuple == null)
                            return null;
                        list.Add(tuple);
                    }
                    contextTargets = list;
                }
                var contexts = new List<Tuple<PythonNode,PythonNode>>();
                foreach (var contextTarget in contextTargets)
                {
                    if (contextTarget.Item2 == null || !contextTarget.Item2.Equals(contextTarget.Item1))
                        continue;

                    contexts.Add(Tuple.Create<PythonNode, PythonNode>(contextTarget.Item1, null));
                }
                if (!contexts.Any())
                    return null;
                variableExamples[input] = contexts;
            }
            return DisjunctiveExamplesSpec.From(variableExamples);
        }

        [WitnessFunction("Node", 0)]
        public static DisjunctiveExamplesSpec WitnessNodeType(GrammarRule rule, int parameter, DisjunctiveExamplesSpec spec)
        {
            var variableExamples = new Dictionary<State, IEnumerable<object>>();
            foreach (State input in spec.ProvidedInputs)
            {
                var contextTargets = spec.DisjunctiveExamples[input] as IEnumerable<Tuple<PythonNode, PythonNode>>;
                if (contextTargets == null)
                {
                    var list = new List<Tuple<PythonNode, PythonNode>>();
                    foreach (var element in spec.DisjunctiveExamples[input])
                    {
                        var tuple = element as Tuple<PythonNode, PythonNode>;
                        if (tuple == null)
                            return null;
                        list.Add(tuple);
                    }
                    contextTargets = list;
                }
                var types = new List<NodeInfo>();
                foreach (var contextTarget in contextTargets)
                {
                    var node = contextTarget.Item1;
                    if (contextTarget.Item2 != null && contextTarget.Item2.Equals(contextTarget.Item1))
                        continue;
                    if (node == null || !node.Children.Any())
                        continue;

                    var nodeInfo = new NodeInfo(node.GetType().Name);
                    if (node.Value != null) nodeInfo.NodeValue = node.Value;
                    types.Add(nodeInfo);
                }
                if (!types.Any())
                    return null;
                variableExamples[input] = types;

            }
            return DisjunctiveExamplesSpec.From(variableExamples);
        }
        [WitnessFunction("Node", 1)]
        public static DisjunctiveExamplesSpec WitnessNodeInfo(GrammarRule rule, int parameter, DisjunctiveExamplesSpec spec)
        {
            var variableExamples = new Dictionary<State, IEnumerable<object>>();
            foreach (State input in spec.ProvidedInputs)
            {
                var contextTargets = spec.DisjunctiveExamples[input] as IEnumerable<Tuple<PythonNode, PythonNode>>;
                if (contextTargets == null)
                {
                    var list = new List<Tuple<PythonNode, PythonNode>>();
                    foreach (var element in spec.DisjunctiveExamples[input])
                    {
                        var tuple = element as Tuple<PythonNode, PythonNode>;
                        if (tuple == null)
                            return null;
                        list.Add(tuple);
                    }
                    contextTargets = list;
                }
                var children = new List<Tuple<List<PythonNode>, PythonNode>>();
                foreach (var contextTarget in contextTargets)
                {
                    var node = contextTarget.Item1;
                    if (contextTarget.Item2 != null && contextTarget.Item2.Equals(contextTarget.Item1))
                        continue;
                    if (node == null || !node.Children.Any())
                        continue;

                    children.Add(Tuple.Create(node.Children, contextTarget.Item2));
                }
                if (!children.Any())
                    return null;
                variableExamples[input] = children;
            }
            return DisjunctiveExamplesSpec.From(variableExamples);
        }

        [WitnessFunction("LeafNode", 0)]
        public static DisjunctiveExamplesSpec WitnessLeafNodeType(GrammarRule rule, int parameter, DisjunctiveExamplesSpec spec)
        {
            var variableExamples = new Dictionary<State, IEnumerable<object>>();
            foreach (State input in spec.ProvidedInputs)
            {
                var contextTargets = spec.DisjunctiveExamples[input] as IEnumerable<Tuple<PythonNode, PythonNode>>;
                if (contextTargets == null)
                {
                    var list = new List<Tuple<PythonNode, PythonNode>>();
                    foreach (var element in spec.DisjunctiveExamples[input])
                    {
                        var tuple = element as Tuple<PythonNode, PythonNode>;
                        if (tuple == null)
                            return null;
                        list.Add(tuple);
                    }
                    contextTargets = list;
                }
                var types = new List<NodeInfo>();
                foreach (var contextTarget in contextTargets)
                {
                    var node = contextTarget.Item1;
                    if (contextTarget.Item2 != null && contextTarget.Item2.Equals(contextTarget.Item1))
                        continue;
                    if (node != null && node.Children.Any())
                        continue;

                    var info = new NodeInfo(node.GetType().Name);
                    if (node.Value != null) info.NodeValue = node.Value;
                    types.Add(info);
                }
                if (!types.Any())
                    return null;
                variableExamples[input] = types;

            }
            return DisjunctiveExamplesSpec.From(variableExamples);
        }

        [WitnessFunction("TChild", 0)]
        public static DisjunctiveExamplesSpec WitnessTemplateChild(GrammarRule rule, int parameter, DisjunctiveExamplesSpec spec)
        {
            var childrenExamples = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var childrenTargets = spec.DisjunctiveExamples[input] as IEnumerable<Tuple<List<PythonNode>, PythonNode>>;
                if (childrenTargets == null)
                {
                    var list = new List<Tuple<List<PythonNode>, PythonNode>>();
                    foreach (var element in spec.DisjunctiveExamples[input])
                    {
                        var tuple = element as Tuple<List<PythonNode>, PythonNode>;
                        if (tuple == null)
                            return null;
                        list.Add(tuple);
                    }
                    childrenTargets = list;
                }
                var contexts = new List<Tuple<PythonNode, PythonNode>>();
                foreach (var childrenTarget in childrenTargets)
                {
                    var children = childrenTarget.Item1;
                    if (children != null && children.Count().Equals(1))
                        contexts.Add(Tuple.Create(children.First(), childrenTarget.Item2));
                }
                if (!contexts.Any())
                    return null;
                childrenExamples[input] = contexts;
            }
            return DisjunctiveExamplesSpec.From(childrenExamples);
        }

        [WitnessFunction("TChildren", 0)]
        public static DisjunctiveExamplesSpec WitnessTemplateChildrenHead(GrammarRule rule, int parameter, DisjunctiveExamplesSpec spec)
        {
            var headExamples = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var childrenTargets = spec.DisjunctiveExamples[input] as IEnumerable<Tuple<List<PythonNode>, PythonNode>>;
                if (childrenTargets == null)
                {
                    var list = new List<Tuple<List<PythonNode>, PythonNode>>();
                    foreach (var element in spec.DisjunctiveExamples[input])
                    {
                        var tuple = element as Tuple<List<PythonNode>, PythonNode>;
                        if (tuple == null)
                            return null;
                        list.Add(tuple);
                    }
                    childrenTargets = list;
                }
                if (childrenTargets == null)
                    return null;
                var contexts = new List<Tuple<PythonNode, PythonNode>>();
                foreach (var childrenTarget in childrenTargets)
                {
                    var children = childrenTarget.Item1;
                    if (children != null && children.Count() > 1)
                        contexts.Add(Tuple.Create(children.First(), childrenTarget.Item2));
                }
                if (!contexts.Any())
                    return null;
                headExamples[input] = contexts;
            }
            return DisjunctiveExamplesSpec.From(headExamples);
        }

        [WitnessFunction("TChildren", 1)]
        public static DisjunctiveExamplesSpec WitnessTemplateChildrenTail(GrammarRule rule, int parameter, DisjunctiveExamplesSpec spec)
        {
            var headExamples = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var childrenTargets = spec.DisjunctiveExamples[input] as IEnumerable<Tuple<List<PythonNode>, PythonNode>>;
                if (childrenTargets == null)
                {
                    var list = new List<Tuple<List<PythonNode>, PythonNode>>();
                    foreach (var element in spec.DisjunctiveExamples[input])
                    {
                        var tuple = element as Tuple<List<PythonNode>, PythonNode>;
                        if (tuple == null)
                            return null;
                        list.Add(tuple);
                    }
                    childrenTargets = list;
                }
                var contexts = new List<Tuple<List<PythonNode>, PythonNode>>();
                foreach (var childrenTarget in childrenTargets)
                {
                    var children = childrenTarget.Item1;
                    if (children != null && children.Count() > 1)
                    {
                        var newList = new List<PythonNode>();
                        newList.AddRange(children);
                        newList.RemoveAt(0);
                        contexts.Add(Tuple.Create(newList, childrenTarget.Item2));
                    }
                    else
                        return null;
                }
                if (contexts.Count == 0)
                    return null;
                headExamples[input] = contexts;

            }
            return DisjunctiveExamplesSpec.From(headExamples);
        }

        [WitnessFunction("ReferenceNode", 1)]
        public static DisjunctiveExamplesSpec WitnessContext(GrammarRule rule, int parameter, ExampleSpec spec)
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
                var contextTargetTuples = new List<Tuple<PythonNode, PythonNode>>();
                contextTargetTuples.Add(Tuple.Create(node, node));
                if (node.Parent != null && ast.Contains(node.Parent))
                    contextTargetTuples.Add(Tuple.Create(node.Parent, node));

                templateExamples[input] = contextTargetTuples;
            }
            return DisjunctiveExamplesSpec.From(templateExamples);
        }

        [WitnessFunction("ReferenceNode", 2, DependsOnParameters = new [] {1})]
        public static DisjunctiveExamplesSpec WitnessK(GrammarRule rule, int parameter, ExampleSpec spec, 
            ExampleSpec templateSpec)
        {
            var result = new Dictionary<State, IEnumerable<object>>();

            foreach (var input in spec.ProvidedInputs)
            {
                var inp = (PythonNode)input[rule.Body[0]];
                var node = spec.Examples[input] as PythonNode;
                var template = (TreeTemplate) templateSpec.Examples[input];
                var matches = template.Matches(inp);
                var witness = -1;
                for (var i = 0; i < matches.Count; i++)
                {
                    if (matches[i].Id == node.Id)
                    {
                        witness = i;
                        break;
                    }
                }
                if (witness < 0)
                    continue;
                var positions = new List<int>();
                positions.Add(witness);
                result[input] = positions.Cast<object>();
            }
            if (!result.Any())
                return null;
            return DisjunctiveExamplesSpec.From(result);
        }

        [WitnessFunction("SingleChild", 0)]
        public static ExampleSpec WitnessSingleChild(GrammarRule rule, int parameter, ExampleSpec spec)
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
        public static ExampleSpec WitnessHead(GrammarRule rule, int parameter, ExampleSpec spec)
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
        public static ExampleSpec WitnessTail(GrammarRule rule, int parameter, ExampleSpec spec)
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
