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

        public static void AssertCorrectTransformation(IEnumerable<Tuple<string, string>> examples)
        {
            var refazer = new Refazer4Python();
            var transformation = refazer.LearnTransformations(examples.ToList()).First();
            foreach (var mistake in examples)
            {
                var output = refazer.Apply(transformation, mistake.Item1);

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
    }
}