using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.Extraction.Text.Semantics;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Rules;
using Microsoft.ProgramSynthesis.Specifications;
using Microsoft.ProgramSynthesis.Utils;

namespace Tutor.Transformation
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static class WitnessFunctions
    {
        [WitnessFunction("Apply", 1)]
        public static SubsequenceSpec WitnessPatch(GrammarRule rule, int parameter,
                                                 ExampleSpec spec)
        {
            var examples = new Dictionary<State, IEnumerable<object>>();
            foreach (State input in spec.ProvidedInputs)
            {
                var before = (PythonNode)input[rule.Body[0]];
                var after = (PythonNode)spec.Examples[input];

                var zss = new PythonZss(before, after);
                var editDistance = zss.Compute();
                var newList = new List<Edit>();
                foreach (var edit in editDistance.Item2)
                {
                    if (before.Contains(edit.Target))
                        newList.Add(edit);

                }
                examples[input] = newList;
            }
            return new SubsequenceSpec(examples);
        }

        [WitnessFunction("EditMap", 1)]
        public static SubsequenceSpec WitnessSelectedNodes(GrammarRule rule, int parameter,
                                                 SubsequenceSpec spec)
        {
            var examples = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var edits = spec.Examples[input] as IEnumerable<Edit>;
                examples[input] = edits.Select(e => e.Target);
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

        [WitnessFunction("Update", 2)]
        public static DisjunctiveExamplesSpec WitnessN2(GrammarRule rule, int parameter, ExampleSpec spec)
        {
            var contextExamples = new Dictionary<State, object>();
            foreach (State input in spec.ProvidedInputs)
            {
                var operation = spec.Examples[input] as Update;
                contextExamples[input] = operation.NewNode;
            }
            return new ExampleSpec(contextExamples);
        }

        [WitnessFunction("Insert", 0)]
        public static ExampleSpec WitnessInsertR(GrammarRule rule, int parameter, ExampleSpec spec)
        {
            var contextExamples = new Dictionary<State, object>();
            foreach (State input in spec.ProvidedInputs)
            {
                var operation = spec.Examples[input] as Insert;
                if (operation == null)
                {
                    var operationList = (IEnumerable<Edit>)spec.Examples[input];
                    if (operationList.Count() > 1)
                        return null;
                    operation = operationList.First() as Insert;
                    if (operation == null)
                        return null;
                }
                contextExamples[input] = operation.Target;
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
                {
                    var operationList = (IEnumerable<Edit>)spec.Examples[input];
                    if (operationList.Count() > 1)
                        return null;
                    operation = operationList.First() as Insert;
                    if (operation == null)
                        return null;
                }
                contextExamples[input] = operation.NewNode.Parent.Children.IndexOf(operation.NewNode);
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

                var t1 = node.Parent.GetCopy();
                t1.EditId = 1;
                templateTrees.Add(t1);
                var t2 = node.Parent.GetAbstractCopy();
                t2.EditId = 1;
                templateTrees.Add(t2);
                var t3 = node.GetCopy();
                t3.EditId = 1; 
                templateTrees.Add(t3);
                var t4 = node.GetAbstractCopy();
                t4.EditId = 1;
                templateTrees.Add(t4);
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
