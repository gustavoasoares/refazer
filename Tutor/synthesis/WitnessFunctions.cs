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

        [WitnessFunction("ConcatPatch", 0)]
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

        [WitnessFunction("ConcatPatch", 1)]
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
                //var solutions = GetAllTemplates(selectedNode, selectedNode);
                //if (selectedNode.Parent != null)
                //{
                //    solutions.AddRange(GetAllTemplates(selectedNode.Parent, selectedNode));
                //}
                examples[input] = selectedNode;
            }
            return new ExampleSpec(examples);
        }

        private static List<TreeTemplate> GetAllTemplates(PythonNode root, PythonNode selectedNode)
        {
            var visitor = new PythonNodeVisitor();
            root.Walk(visitor);
            var inOrderNodes = visitor.SortedNodes;
            
            var dic = new Dictionary<int, List<TreeTemplate>>();
            for (var i = 0; i < inOrderNodes.Count; i++)
            {
                List<TreeTemplate> templates = new List<TreeTemplate>();
                var node = inOrderNodes[i];
                var selected = node.Equals(selectedNode); 
                templates.Add(new Variable(node.GetType().Name) {Target = selected});
                var template = new TreeTemplate(node.GetType().Name);
                if (node.Value != null) template.Value = node.Value;
                template.Target = selected;
                templates.Add(template);
                dic.Add(i, templates);
            }
            var nodes = Enumerable.Range(0, inOrderNodes.Count).ToArray();
            var solutions = new List<Dictionary<int, TreeTemplate>>();
            var currentSolution = new Dictionary<int,TreeTemplate>();
            GetAllTemplatesAux(currentSolution, solutions, nodes, dic, inOrderNodes);
            var result = new List<TreeTemplate>();
            foreach (var solution in solutions)
            {
                result.Add(CreateTemplate(root, solution, inOrderNodes));
            }
            return result; 
        }

        private static TreeTemplate CreateTemplate(PythonNode root, Dictionary<int, TreeTemplate> solution, List<PythonNode> inOrderNodes)
        {
            var id = inOrderNodes.IndexOf(root);
            var template = solution[id];
            if (template is Variable)
                return template;
            foreach (var child in root.Children)
            {
                template.Children.Add(CreateTemplate(child, solution,inOrderNodes));
            }
            return template;
        }

        private static void GetAllTemplatesAux(Dictionary<int, TreeTemplate> currentSolution, List<Dictionary<int, TreeTemplate>> solutions, int[] nodes, Dictionary<int, List<TreeTemplate>> dic, List<PythonNode> inOrderNodes)
        {
            if (nodes.IsEmpty())
            {
                solutions.Add(currentSolution);
            }
            else
            {
                var nodeId = nodes.First();
                var treeTemplates = dic[nodeId];
                if (IsValid(nodeId, currentSolution, inOrderNodes))
                {
                    foreach (var treeTemplate in treeTemplates)
                    {
                        var newList = new List<int>(nodes);
                        newList.RemoveAt(0);
                        var copy = new Dictionary<int, TreeTemplate>(currentSolution);
                        copy.Add(nodeId, treeTemplate);
                        GetAllTemplatesAux(copy, solutions, newList.ToArray(), dic, inOrderNodes);
                    }
                }
                else
                {
                    var newList = new List<int>(nodes);
                    newList.RemoveAt(0);
                    var copy = new Dictionary<int, TreeTemplate>(currentSolution);
                    copy.Add(nodeId, new InvalidTemplate(""));
                    GetAllTemplatesAux(copy, solutions, newList.ToArray(), dic, inOrderNodes);
                }
            }
        }

        private static bool IsValid(int nodeId, Dictionary<int, TreeTemplate> currentSolution, List<PythonNode> nodes)
        {
            foreach (var pair in currentSolution)
            {
                if (pair.Value is Variable || pair.Value is InvalidTemplate)
                {
                    var node = nodes[pair.Key];
                    var currentNode = nodes[nodeId];
                    if (currentNode.Parent != null && currentNode.Parent.Equals(node))
                    {                        
                        return false; 
                    }
                }
            }
            return true;
        }


        [WitnessFunction("Update", 1)]
        public static DisjunctiveExamplesSpec WitnessN2(GrammarRule rule, int parameter, ExampleSpec spec)
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
        public static DisjunctiveExamplesSpec WitnessInsertN(GrammarRule rule, int parameter, ExampleSpec spec)
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
        public static DisjunctiveExamplesSpec WitnessMoveN(GrammarRule rule, int parameter, ExampleSpec spec)
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

        [WitnessFunction("Variable", 0)]
        public static ExampleSpec WitnessVariable(GrammarRule rule, int parameter, ExampleSpec spec)
        {
            var variableExamples = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var node = spec.Examples[input] as PythonNode;
                if (node == null)
                    return null;
                variableExamples[input] = node.GetType().Name;
            }
            return new ExampleSpec(variableExamples);
        }

        [WitnessFunction("Tree", 0)]
        public static ExampleSpec WitnessTreeInfo(GrammarRule rule, int parameter, ExampleSpec spec)
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

        [WitnessFunction("Node", 0)]
        public static ExampleSpec WitnessNodeInfo(GrammarRule rule, int parameter, ExampleSpec spec)
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

        [WitnessFunction("Tree", 1)]
        public static ExampleSpec WitnessTreeChildren(GrammarRule rule, int parameter, ExampleSpec spec)
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

        [WitnessFunction("TemplateChild", 0)]
        public static ExampleSpec WitnessTemplateChild(GrammarRule rule, int parameter, ExampleSpec spec)
        {
            var childrenExamples = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var children = spec.Examples[input] as IEnumerable<PythonNode>;
                if (children != null && children.Count().Equals(1))
                    childrenExamples[input] = children.First();
                else
                    return null;
            }
            return new ExampleSpec(childrenExamples);
        }

        [WitnessFunction("TemplateChildren", 0)]
        public static ExampleSpec WitnessTemplateChildrenHead(GrammarRule rule, int parameter, ExampleSpec spec)
        {
            var headExamples = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var children = spec.Examples[input] as IEnumerable<PythonNode>;
                if (children != null && children.Count() > 1)
                    headExamples[input] = children.First();
                else
                    return null;
            }
            return new ExampleSpec(headExamples);
        }

        [WitnessFunction("TemplateChildren", 1)]
        public static ExampleSpec WitnessTemplateChildrenTail(GrammarRule rule, int parameter, ExampleSpec spec)
        {
            var headExamples = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var children = spec.Examples[input] as IEnumerable<PythonNode>;
                if (children != null && children.Count() > 1)
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

        [WitnessFunction("ReferenceNode", 1)]
        public static ExampleSpec WitnessContext(GrammarRule rule, int parameter, ExampleSpec spec)
        {
            var templateExamples = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var node = spec.Examples[input] as PythonNode;

                var ast = (PythonNode)input[rule.Body[0]];
                if (ast.ContainsByBinding(node))
                {
                    node = ast.GetCorrespondingNodeByBinding(node);
                }
                //else if (ast.Contains(node))
                //{
                //    node = ast.GetCorrespondingNode(node);
                //}
                else
                {
                    return null;
                }

                //var solutions = GetAllTemplates(node, node);
                //if (node.Parent != null)
                //{
                //    solutions.AddRange(GetAllTemplates(node.Parent, node));
                //}
                templateExamples[input] = node;
            }
            return new ExampleSpec(templateExamples);
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
