using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using IronPython.Compiler.Ast;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Compiler;
using Microsoft.ProgramSynthesis.Diagnostics;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Specifications;
using Microsoft.Scripting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
            var choice = 2;
            switch (choice)
            {
                case 1:
                    AnalyzeResults();
                    break;
                case 2:
                    RunExperiment();
                    break;
            }
            Console.ReadKey();
        }

        private static void AnalyzeResults()
        {
            var submissions = JsonConvert.DeserializeObject<List<Mistake>>(File.ReadAllText(LogFolder + "submissionsResults-repeated.json"));
            
            var usedPrograms = new HashSet<string>();
            foreach (var submission in submissions)
            {
                if (submission.IsFixed && !usedPrograms.Contains(submission.UsedFix))
                {
                    Console.Out.WriteLine("Used program:");
                    Console.Out.WriteLine(submission.UsedFix);
                    Console.Out.WriteLine("\r\nBefore:");
                    Console.Out.WriteLine(submission.before);
                    Console.Out.WriteLine("\r\nAfter:");
                    Console.Out.WriteLine(submission.SynthesizedAfter);
                    usedPrograms.Add(submission.UsedFix);

                }
            }
        }

        private static void RunExperiment()
        {
            var notImplementedYet = 0;


            var product = new Tuple<TestBasedCluster.Question, string>(TestBasedCluster.Question.Product,
                LogFolder + "mistake_pairs_product_complete.json");
            var repeted = new Tuple<TestBasedCluster.Question, string>(TestBasedCluster.Question.Repeated,
                           LogFolder+ "mistake_pairs_repeated_complete.json");
            var questionLogs = new[] {product, repeted};


            var cluster = new TestBasedCluster();
            cluster.GenerateCluster(questionLogs);
            var clusters = cluster.Clusters[TestBasedCluster.Question.Repeated];

            var tests = GetTests("repeated");


            var values = from pair in clusters
                orderby pair.Value.Count descending
                select pair.Value;

            var submissions = new List<Mistake>();
            submissions.AddRange(values.ElementAt(1));
            submissions.AddRange(values.ElementAt(2));
            submissions.AddRange(values.ElementAt(3));
            submissions.AddRange(values.ElementAt(4));
            submissions.AddRange(values.ElementAt(5));
            submissions.AddRange(values.ElementAt(6));
            submissions.AddRange(values.ElementAt(7));

            List<Tuple<List<Mistake>, ProgramNode>> classification = new List<Tuple<List<Mistake>, ProgramNode>>();
            for (var i = 0; i < submissions.Count; i++)
            {
                var current = submissions[i];
                current.Id = i + 1;
                var hasGroup = false;
                foreach (var group in classification)
                {
                    foreach (var mistake in @group.Item1)
                    {
                        if (current.Equals(mistake))
                            hasGroup = true;
                    }
                }
                if (!hasGroup)
                {
                    var list = new List<Mistake>() {current};
                    Console.Out.WriteLine("New group with mistake: " + i);
                    for (var j = i + 1; j < submissions.Count; j++)
                    {
                        var next = submissions[j];

                        hasGroup = false;
                        foreach (var group in classification)
                        {
                            foreach (var mistake in @group.Item1)
                            {
                                if (next.Equals(mistake))
                                    hasGroup = true;
                            }
                        }
                        if (!hasGroup)
                        {
                            Console.Out.WriteLine("Trying to add mistake " + j);
                            try
                            {
                                var topProgram = SubmissionFixer.LearnProgram(list, next);
                                if (topProgram != null)
                                {
                                    list.Add(next);
                                    next.GeneratedFix = topProgram.ToString();
                                    Console.Out.WriteLine("Added!");
                                }
                            }
                            catch (SyntaxErrorException)
                            {
                                Console.Out.WriteLine("Syntax error on input");
                            }
                            catch (NotImplementedException)
                            {
                                Console.Out.WriteLine("Not implemented yet");
                            }
                        }
                    }
                    try
                    {
                        var learnProgram = SubmissionFixer.LearnProgram(list);
                        if (learnProgram != null)
                        {
                            classification.Add(Tuple.Create(list, learnProgram));
                            current.GeneratedFix = learnProgram.ToString();
                        }
                    }
                    catch (SyntaxErrorException)
                    {
                        Console.Out.WriteLine("Syntax error on input");
                    }
                    catch (NotImplementedException)
                    {
                        notImplementedYet++;
                        Console.Out.WriteLine("Syntax error on input");
                    }
                }
            }


            var count = 0;
            var doesNotCompile = 0;

            var submissionCount = 0;

            var fixer = new SubmissionFixer(classification);
            var notFixed = new List<Mistake>();
            foreach (var mistake in submissions)
            {
                submissionCount += 1;
                Console.Out.WriteLine(
                    "=============================================================================================== " +
                    submissionCount);
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

                Console.Out.WriteLine("Diff =====================");
                Console.Out.WriteLine(mistake.diff);
                Console.Out.WriteLine("Before ===================================");
                Console.Out.WriteLine(mistake.before);

                try
                {
                    var watch = Stopwatch.StartNew();
                    var isFixed = fixer.Fix(mistake, tests);
                    watch.Stop();
                    var timeInMS = watch.ElapsedMilliseconds;
                    timeToFix.Add(timeInMS);

                    mistake.IsFixed = isFixed;
                    if (isFixed)
                    {
                        count++;
                        Console.Out.WriteLine("Fixed!" + count);
                    }
                    else
                    {
                        notFixed.Add(mistake);
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
            Console.Out.WriteLine("Total of groups: " + classification.Count);
            foreach (var tuple in classification)
            {
                Console.Out.WriteLine("Number of mistakes: " + tuple.Item1.Count);
                Console.Out.WriteLine(tuple.Item2);
            }

            Console.Out.WriteLine("=====================");
            foreach (var submission in submissions)
            {
                if (submission.IsFixed)
                {
                    if (submission.GeneratedFix == null ||
                        !submission.GeneratedFix.ToString().Equals(submission.UsedFix.ToString()))
                    {
                        Console.Out.WriteLine("Different Fix ------------------------------------");
                        Console.Out.WriteLine("learned");
                        Console.Out.WriteLine(submission.GeneratedFix);
                        Console.Out.WriteLine("Used");
                        Console.Out.WriteLine(submission.UsedFix);
                        Console.Out.WriteLine("input");
                        Console.Out.WriteLine(submission.before);
                        Console.Out.WriteLine("output");
                        Console.Out.WriteLine(submission.after);
                        Console.Out.WriteLine("Synthesized output");
                        Console.Out.WriteLine(submission.SynthesizedAfter);
                    }
                }
            }

            var submissionsToJson = JsonConvert.SerializeObject(submissions);
            File.WriteAllText(LogFolder + "submissionsResults.json", submissionsToJson);
        }

        private static Dictionary<string, long> GetTests(string question)
        {
            var testSetup = GetTestSetup();

            switch (question)
            {
                case "product":
                    return new Dictionary<string, long>
            {
                {testSetup + "product(3, identity)", 6},
                {testSetup + "product(5, identity)", 120},
                {testSetup + "product(3, square)", 36},
                {testSetup + "product(5, square)", 14400}
            };
                case "repeated":
                    return new Dictionary<string, long>
            {
                {testSetup + "add_three = repeated(increment, 3)\nadd_three(5)", 8},
                {testSetup + "repeated(triple, 5)(1)", 243},
                {testSetup + "repeated(square, 2)(5)", 625},
                {testSetup + "repeated(square, 3)(5)", 390625},
                {testSetup + "repeated(square, 0)(5)", 5}
            };

            }
            return null;
        }

        private static string GetTestSetup()
        {
            return @"
def square(x):
    return x * x

def identity(x):
    return x

def triple(x):
    return 3 * x

def increment(x):
    return x + 1
";
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

        private static int CountEdits(string node)
        {
            var pat = @"Update";
            var pat2 = @"Insert";
            var pat3 = @"Delete";
            var r = new Regex(pat, RegexOptions.IgnoreCase);
            var m = r.Matches(node);
            var updates = m.Count;
            r = new Regex(pat2, RegexOptions.IgnoreCase);
            m = r.Matches(node);
            var inserts = m.Count;
            r = new Regex(pat3,RegexOptions.IgnoreCase);
            m = r.Matches(node);
            var deletes = m.Count;
            return updates + deletes + inserts;
            //var result = (node.Symbol.Name.Equals("edit")) ? 1 : 0;
            //node.Children.ForEach(e => result += CountEdits(e));
            //return result;
        }
    }
}
