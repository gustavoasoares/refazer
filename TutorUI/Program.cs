using System;
using System.Collections.Generic;
using System.Linq;

using Tutor;

namespace TutorUI
{
    class Program
    {
        static void Main(string[] args)
        {
            var product = new Tuple<TestBasedCluster.Question, string>(TestBasedCluster.Question.Product,
              "C:/Users/Gustavo/Box Sync/pesquisa/tutor/hw02-sp16/" + "mistake_pairs_product_complete.json");
            var questionLogs = new[] { product };
            var cluster = new TestBasedCluster();
            cluster.GenerateCluster(questionLogs);
            var clusters = cluster.Clusters[TestBasedCluster.Question.Product];
            var values = from pair in clusters
                         orderby pair.Value.Count descending
                         select pair.Value;
            var biggest = values.First();

            var count = 0;

            var fixer = new SubmissionFixer();

            var testSetup = 
@"def square(x):
    return x * x

def identity(x):
    return x
";
            var tests = new Dictionary<String, int>
                        {
                            {testSetup + "product(3, identity)", 6},
                            {testSetup + "product(5, identity)", 120},
                            {testSetup + "product(3, square)", 36},
                            {testSetup + "product(5, square)", 14400}
                        };

            foreach (var mistake in biggest)
            {
                try
                {
                    var before = ASTHelper.ParseContent(mistake.before);
                    var after = ASTHelper.ParseContent(mistake.after);
                    var diff = new PythonZss(NodeWrapper.Wrap(before), NodeWrapper.Wrap(after));
                    var changes = diff.Compute();
                    if (changes.Item1 > 1)
                        continue;
                    if ((changes.Item2.Any(e => e is Delete)))
                        continue;

                }
                catch (Exception)
                {
                    continue;
                }
                

                Console.Out.WriteLine("Diff =====================");
                Console.Out.WriteLine(mistake.diff);
                Console.Out.WriteLine("Before ===================================");
                Console.Out.WriteLine(mistake.before);
                var isFixed = fixer.Fix(mistake.before, mistake.after, tests);
                if (isFixed)
                {
                    count++;
                    Console.Out.WriteLine("Fixed!" + count);
                }
                else
                {
                    Console.Out.WriteLine("HELPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPP!");
                }
            }
            Console.Out.WriteLine("Total tested: " + biggest.Count);
            Console.Out.WriteLine("Fixed: " + count);
            Console.Out.WriteLine("Not Fixed: " + (biggest.Count - count));
            Console.Out.WriteLine("Program sets: " + (fixer.ProsePrograms.Count));
            fixer.ProsePrograms.ForEach(e => Console.Out.WriteLine(e.First()));
            Console.ReadKey();
        }


    }
}
