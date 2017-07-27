using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Refazer.Core;
using Tutor;

namespace Refazer.Test
{
    public class TestUtils
    {
        public static void AssertCorrectTransformation(string before, string after)
        {
            AssertCorrectTransformation(new List<Tuple<string, string>>() { Tuple.Create(before, after) });
        }

        public static void AssertCorrectExtraction(string code, int nodeId)
        {
            //AssertCorrectExtraction(new List<Tuple<string, int>>() { Tuple.Create(before, id) });
            var rootNode = NodeWrapper.Wrap(ASTHelper.ParseContent(code));
            var extractedNode = rootNode.Find(nodeId);
            var examples = new List<Tuple<PythonNode, PythonNode>>() { Tuple.Create(rootNode, extractedNode) };

            var farbindn = new Farbindn();
            var learnExtractions = farbindn.LearnExtractions(examples, 1000, "specific");
            var extraction = learnExtractions.First();
            foreach (var extractionExample in examples)
            {
                IEnumerable<PythonNode> output = farbindn.Apply(extraction, extractionExample.Item1);
                Assert.AreEqual(nodeId, output.First().Id);
            }
        }

        public static void AssertCorrectTransformation(IEnumerable<Tuple<string, string>> examples)
        {
            var refazer = new Refazer4Python();
            var learnTransformations = refazer.LearnTransformations(examples.ToList(), numberOfPrograms: 1000);
            var transformation = learnTransformations.First();
            foreach (var mistake in examples)
            {
                var output = refazer.Apply(transformation, mistake.Item1);
                Console.Out.WriteLine("");
                var isFixed = false;
                foreach (var newCode in output)
                {
                    var unparser = new Unparser();
                    isFixed = mistake.Item2.Equals(newCode);
                    if (isFixed)
                        break;
                }
                Assert.IsTrue(isFixed);
            }
        }

        public static void AssertCorrectExtraction(List<Tuple<string, int>> examples_strings_and_ints)
        {
            //AssertCorrectExtraction(new List<Tuple<string, int>>() { Tuple.Create(before, id) });

            Console.Out.WriteLine(examples_strings_and_ints);
            //initialize list
            var examples = new List<Tuple<PythonNode, PythonNode>>();
            // iterate over strings and ints examples, construct new set of examples filled with Python nodes
            foreach (var string_int_tuple in examples_strings_and_ints)
            {
                var rootNode = NodeWrapper.Wrap(ASTHelper.ParseContent(string_int_tuple.Item1));
                var extractedNode = rootNode.Find(string_int_tuple.Item2);
                examples.Add(Tuple.Create(rootNode, extractedNode));
            }
            Console.Out.WriteLine(examples);

            var farbindn = new Farbindn();
            var learnExtractions = farbindn.LearnExtractions(examples, 1000, "specific");
            var extraction = learnExtractions.First();
            foreach (var extractionExample in examples)
            {
                IEnumerable<PythonNode> output = farbindn.Apply(extraction, extractionExample.Item1);
                Assert.AreEqual(extractionExample.Item1, output.First().Id);
            }
        }
    }
}