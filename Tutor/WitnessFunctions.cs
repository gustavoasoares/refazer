using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Rules;
using Microsoft.ProgramSynthesis.Specifications;

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

                //todo remove this limitation
                if (editDistance.Distance >= 20)
                    return null;

                var rootAndNonRootEdits = SplitEditsByRootsAndNonRoots(editDistance);
                var edits = new List<Edit>();
                foreach (var root in rootAndNonRootEdits.Item1)
                {
                    edits.Add(root);
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
                                    //create an insert in the parent node
                                    var insert = new Insert(child, root.TargetNode, 
                                        mappedNodeInTheResultingAst.Parent.Children.IndexOf(mappedNodeInTheResultingAst));
                                    edits.Add(insert);
                                }
                            }
                        }
                    }
                }
                examples[input] = edits;
            }
            return new ExampleSpec(examples);
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
                    if (edit.TargetNode.Equals(edit1.ModifiedNode))
                        isRoot = false;
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
                var edits = spec.Examples[input] as IEnumerable<Edit>;
                if (edits == null || edits.Count() > 1)
                    return null;
                examples[input] = new List<Edit>() {edits.First()};
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
                var edits = spec.Examples[input] as IEnumerable<Edit>;
                if (edits == null || edits.Count() < 2)
                    return null;
                examples[input] = new List<Edit>() { edits.First() };
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
                var edits = spec.Examples[input] as IEnumerable<Edit>;
                if (edits == null || edits.Count() < 2)
                    return null;
                var tail = new List<Edit>(edits);
                tail.RemoveAt(0);
                examples[input] = tail;
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
        public static DisjunctiveExamplesSpec WitnessMatchTemplate(GrammarRule rule, int parameter,
            ExampleSpec spec)
        {
            var examples = new Dictionary<State, IEnumerable<object>>();
            foreach (State input in spec.ProvidedInputs)
            {
                
                var selectedNode = (PythonNode)input[rule.Body[0]];
                selectedNode.EditId = 1;
                var templateTrees = new List<PythonNode>();

                if (selectedNode.Parent != null)
                {
                    var t1 = selectedNode.Parent.GetCopy();
                    templateTrees.Add(t1);
                    var t2 = selectedNode.Parent.GetAbstractCopy();
                    templateTrees.Add(t2);
                }
                var t3 = selectedNode.GetCopy();
                templateTrees.Add(t3);
                var t4 = selectedNode.GetAbstractCopy();
                templateTrees.Add(t4);
                examples[input] = templateTrees;
                selectedNode.EditId = 0;
            }
            return DisjunctiveExamplesSpec.From(examples);
        }

        [WitnessFunction("Single", 0)]
        public static ExampleSpec WitnessSingleChange(GrammarRule rule, int parameter, ExampleSpec spec)
        {
            var editExamples = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var operations = spec.Examples[input] as IEnumerable<Edit>;
                if (operations.Count().Equals(1))
                    editExamples[input] = operations.First();
                else
                    return null;
            }
            return new ExampleSpec(editExamples);
        }

        [WitnessFunction("Changes", 0)]
        public static ExampleSpec WitnessChange(GrammarRule rule, int parameter, ExampleSpec spec)
        {
            var editExamples = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var operations = spec.Examples[input] as IEnumerable<Edit>;


                if (operations.Count() > 1)
                    editExamples[input] = operations.First();
                else
                    return null;
            }
            return new ExampleSpec(editExamples);
        }

        [WitnessFunction("Changes", 1)]
        public static ExampleSpec WitnessChanges(GrammarRule rule, int parameter, ExampleSpec spec)
        {
            var editExamples = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var operations = spec.Examples[input] as IEnumerable<Edit>;

                var newList = operations.ToList();
                if (newList.Count > 1)
                {
                    newList.RemoveAt(0);
                    editExamples[input] = newList;
                }
                else
                {
                    return null;
                }
            }
            return new ExampleSpec(editExamples);
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

        [WitnessFunction("ReferenceNode", 1)]
        public static DisjunctiveExamplesSpec WitnessContext(GrammarRule rule, int parameter, ExampleSpec spec)
        {
            var templateExamples = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var templateTrees = new List<PythonNode>();
                var node = spec.Examples[input] as PythonNode;

                var ast = (PythonNode)input[rule.Body[0]];
                if (!ast.Contains(node))
                    return null;

                var t3 = node.GetCopy();
                t3.EditId = 1; 
                templateTrees.Add(t3);
                var t4 = node.GetAbstractCopy();
                t4.EditId = 1;
                templateTrees.Add(t4);

                var t1 = t3.Parent.GetCopy();
                templateTrees.Add(t1);
                var t2 = t3.Parent.GetAbstractCopy();
                templateTrees.Add(t2);

                templateExamples[input] = templateTrees;
            }
            return DisjunctiveExamplesSpec.From(templateExamples);
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
