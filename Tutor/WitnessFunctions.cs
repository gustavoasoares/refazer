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
using static Tutor.Transformation.RegexUtils;

namespace Tutor.Transformation
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static class WitnessFunctions
    {
        [WitnessFunction("Patch", 1)]
        public static ExampleSpec WitnessPatches(GrammarRule rule, int parameter, ExampleSpec spec)
        {
            var patchExamples = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var before = (PythonNode)input[rule.Body[0]];
                var after = (PythonNode)spec.Examples[input];

                var zss = new PythonZss(before, after);
                var editDistance = zss.Compute();
                patchExamples[input] = editDistance.Item2;
            }
            return  new ExampleSpec(patchExamples);
        }

        [WitnessFunction("Single", 0)]
        public static ExampleSpec WitnessSingleChange(GrammarRule rule, int parameter, ExampleSpec spec)
        {
            var editExamples = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var operations = spec.Examples[input] as IEnumerable<Operation>;
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
                var operations = spec.Examples[input] as IEnumerable<Operation>;

                //var roots = new List<List<Operation>>();
                //var nonroots = new HashSet<Operation>();
                //foreach (var operation in operations)
                //{
                //    var parent = operation.Target;
                //    var isRoot = true;
                //    foreach (var ops in operations)
                //    {
                //        if (parent.Equals(ops.NewNode))
                //        {
                //            isRoot = false;
                //        }
                //    }
                //    if (isRoot)
                //    {
                //        roots.Add(new List<Operation>() { operation });
                //    }
                //    else
                //    {
                //        nonroots.Add(operation);
                //    }
                //}
                //foreach (var operationList in roots)
                //{
                //    var head = operationList.First();
                //    var root = head.Target;
                //    var visitor = new SubSequentNodesVisitor(nonroots);
                //    root.Walk(visitor);
                //    operationList.AddRange(visitor.SubOperations);
                //    nonroots.RemoveWhere(op => visitor.SubOperations.Contains(op));
                //}

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
                var operations = spec.Examples[input] as IEnumerable<Operation>;

                //var roots = new List<List<Operation>>();
                //var nonroots = new HashSet<Operation>();
                //foreach (var operation in operations)
                //{
                //    var parent = operation.Target;
                //    var isRoot = true;
                //    foreach (var ops in operations)
                //    {
                //        if (parent.Equals(ops.NewNode))
                //        {
                //            isRoot = false;
                //        }
                //    }
                //    if (isRoot)
                //    {
                //        roots.Add(new List<Operation>() { operation });
                //    }
                //    else
                //    {
                //        nonroots.Add(operation);
                //    }
                //}
                //foreach (var operationList in roots)
                //{
                //    var head = operationList.First();
                //    var root = head.Target;
                //    var visitor = new SubSequentNodesVisitor(nonroots);
                //    root.Walk(visitor);
                //    operationList.AddRange(visitor.SubOperations);
                //    nonroots.RemoveWhere(op => visitor.SubOperations.Contains(op));
                //}

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

        [WitnessFunction("Change", 0)]
        public static ExampleSpec WitnessEdit(GrammarRule rule, int parameter, ExampleSpec spec)
        {
            var editExamples = new Dictionary<State, object>();
            foreach (var input in spec.ProvidedInputs)
            {
                var operation = spec.Examples[input] as Operation;
                if (operation == null)
                {
                    var operationList = (IEnumerable<Operation>)spec.Examples[input];
                    if (operationList.Count() > 1)
                        return null;
                    operation = operationList.First();
                }
                editExamples[input] = operation;
            }
            return new ExampleSpec(editExamples);
        }

        [WitnessFunction("Change", 1)]
        public static DisjunctiveExamplesSpec WitnessContext(GrammarRule rule, int parameter, ExampleSpec spec)
        {
            var contextExamples = new Dictionary<State, IEnumerable<object>>();
            foreach (State input in spec.ProvidedInputs)
            {
                var operation = spec.Examples[input] as Operation;
                if (operation == null)
                {
                    var operationList = (IEnumerable<Operation>)spec.Examples[input];
                    if (operationList.Count() > 1)
                        return null;
                    operation = operationList.First();
                }
                var target = operation.Target;
                var context = new Dictionary<int, PythonNode>();
                context.Add(0, target);
                contextExamples[input] = new List<object>() { context };
            }
            return DisjunctiveExamplesSpec.From(contextExamples);
        }

        [WitnessFunction("Match", 1)]
        public static DisjunctiveExamplesSpec WitnessTemplate(GrammarRule rule, int parameter, ExampleSpec spec)
        {
            var templateExamples = new Dictionary<State, IEnumerable<object>>();
            foreach (var input in spec.ProvidedInputs)
            {
                var templateTrees = new List<object>();
                foreach (Dictionary<int,PythonNode> context in spec.DisjunctiveExamples[input])
                {
                    var node = context[0];
                    node.EditId = 1;
                    templateTrees.Add(node.Parent);
                    templateTrees.Add(node.Parent.GetAbstractCopy());
                    templateTrees.Add(node);
                    templateTrees.Add(node.GetAbstractCopy());
                }
                templateExamples[input] = templateTrees;
            }
            return DisjunctiveExamplesSpec.From(templateExamples);
        }
    }
}
