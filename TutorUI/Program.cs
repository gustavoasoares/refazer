using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using IronPython.Compiler.Ast;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.Scripting;
using Newtonsoft.Json;
using Tutor;

namespace TutorUI
{
    class Program
    {
        private static List<long> timeToFix = new List<long>();

        private const string LogFolder = "C:/Users/Gustavo/Box Sync/pesquisa/tutor/hw02-sp16/";
        private const string TimeFile = LogFolder + "time.txt";


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

            var submissions = new List<Mistake>();
            submissions.AddRange(values.ElementAt(0));
            submissions.AddRange(values.ElementAt(1));
            submissions.AddRange(values.ElementAt(2));
            submissions.AddRange(values.ElementAt(3));

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

            var doesNotCompile = 0;
            var notImplementedYet = 0;

            var submissionCount = 0; 
            foreach (var mistake in submissions)
            {
                submissionCount += 1;
                Console.Out.WriteLine("=============================================================================================== " + submissionCount);
                var unparser = new Unparser();
                PythonAst before = null;
                try
                {
                    before = ASTHelper.ParseContent(mistake.before);
                    before = ASTHelper.ParseContent(unparser.Unparse(before));

                }
                catch (SyntaxErrorException)
                {
                    Console.Out.WriteLine("ERROR: INPUT DOES NOT COMPILE");
                    doesNotCompile++;
                    continue;
                }
                catch (NotImplementedException)
                {
                    Console.Out.WriteLine("Not Implemented yet");
                    notImplementedYet++;
                    continue;
                }

                var after = ASTHelper.ParseContent(mistake.after);
                after = ASTHelper.ParseContent(unparser.Unparse(after));
                var diff = new PythonZss(NodeWrapper.Wrap(before), NodeWrapper.Wrap(after));
                Console.Out.WriteLine("Diff =====================");

                Console.Out.WriteLine(mistake.diff);
                Console.Out.WriteLine("Before ===================================");
                Console.Out.WriteLine(mistake.before);

                try
                {
                    var watch = Stopwatch.StartNew();
                    var isFixed = fixer.Fix(mistake.before, mistake.after, tests);
                    watch.Stop();
                    var timeInMS = watch.ElapsedMilliseconds;
                    timeToFix.Add(timeInMS);

                    if (isFixed)
                    {
                        count++;
                        Console.Out.WriteLine("Fixed!" + count);
                    }
                    else
                    {
                        Console.Out.WriteLine("ERROR: PROGRAM NOT FIXED");
                    }
                }
                catch (NotImplementedException)
                {
                    notImplementedYet++;
                }
                
            }

            Console.Out.WriteLine("Total tested: " + submissions.Count);
            Console.Out.WriteLine("Does not compile: " + doesNotCompile);
            Console.Out.WriteLine("Not implemented yet: " + notImplementedYet);
            Console.Out.WriteLine("Fixed: " + count);
            Console.Out.WriteLine("Not Fixed: " + (submissionCount - count));
            Console.Out.WriteLine("Program sets: " + (fixer.ProsePrograms.Count));
            Console.Out.WriteLine("Used Programs: " + (fixer.UsedPrograms.Count));
            var editSetDistribution = fixer.UsedPrograms.Select(e => Tuple.Create(CountEdits(e.Key), e.Value));
            Console.Out.WriteLine("Distribution of fixes: ");
            Console.Out.WriteLine("Edits, Submissions");
            foreach (var tuple in editSetDistribution)
            {
                Console.Out.WriteLine(tuple.Item1 + " , " + tuple.Item2);
            }
            fixer.UsedPrograms.ForEach(e => Console.Out.WriteLine(e + "\r\n"));

            LogPerformance();
            Console.ReadKey();
        }

        private static void LogPerformance()
        {
            var sb = new StringBuilder();
            sb.Append("Time");
            sb.Append(Environment.NewLine);
            timeToFix.ForEach(e => sb.Append(e + Environment.NewLine));
            sb.Append(Environment.NewLine);
            sb.Append("Average time: ");
            sb.Append(timeToFix.Average());
            sb.Append(Environment.NewLine);
            sb.Append("Max time: ");
            sb.Append(timeToFix.Max());
            sb.Append(Environment.NewLine);
            sb.Append("Min time: ");
            sb.Append(timeToFix.Min());
            Console.Out.WriteLine(sb.ToString());
            File.WriteAllText(TimeFile, sb.ToString());
        }

        private static int CountEdits(ProgramNode node)
        {
            var result = (node.Symbol.Name.Equals("edit")) ? 1 : 0;
            node.Children.ForEach(e => result += CountEdits(e));
            return result;
        }
    }
}
