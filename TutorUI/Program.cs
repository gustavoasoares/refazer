using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

        private static TraceSource _source = new TraceSource("experiment");

        private const string LogFolder = "C:/Users/Gustavo/Box Sync/pesquisa/tutor/hw02-sp16/";
        private const string TimeFile = LogFolder + "time.txt";


        static void Main(string[] args)
        {
            var choice =2;
            switch (choice)
            {
                case 1:
                    AnalyzeResults();
                    break;
                case 2:
                    RunExperiment();
                    break;
                case 3:
                    CheckCanFixItself();
                    break;
            }
            Console.ReadKey();
        }

        private static void CheckCanFixItself()
        {
            var product = new Tuple<TestBasedCluster.Question, string>(TestBasedCluster.Question.Product,
                LogFolder + "mistake_pairs_product_complete.json");
            var repeted = new Tuple<TestBasedCluster.Question, string>(TestBasedCluster.Question.Repeated,
                           LogFolder + "mistake_pairs_repeated_complete.json");
            var questionLogs = new[] { product, repeted };


            var cluster = new TestBasedCluster();
            cluster.GenerateCluster(questionLogs);
            var clusters = cluster.Clusters[TestBasedCluster.Question.Product];

            var tests = GetTests("product");


            var values = from pair in clusters
                         orderby pair.Value.Count descending
                         select pair.Value;

            var submissions = new List<Mistake>();
            foreach (var mistakes in values)
            {
                submissions.AddRange(mistakes);
            }

            var count = 0;
            var doesNotCompile = 0;

            var submissionCount = 0;

            var fixer = new SubmissionFixer();
            var notFixed = new List<Mistake>();
            int notImplementedYet = 0;
            int transformationNotImplemented = 0;
            foreach (var mistake in submissions)
            {
                submissionCount += 1;
                _source.TraceEvent(TraceEventType.Start, 1, "Submission " + submissionCount);

                var unparser = new Unparser();
                PythonNode before = null;
                try
                {
                    before = NodeWrapper.Wrap(ASTHelper.ParseContent(mistake.before));
                    before = NodeWrapper.Wrap(ASTHelper.ParseContent(unparser.Unparse(before)));
                }
                catch (SyntaxErrorException)
                {
                    doesNotCompile++;
                    _source.TraceEvent(TraceEventType.Information, 0, "Input does not compile");
                    continue;
                }
                catch (NotImplementedException)
                {
                    _source.TraceEvent(TraceEventType.Error, 0, mistake.before);
                    notImplementedYet++;
                    continue;
                }

                try
                {
                    var isFixed = fixer.FixItSelf(mistake, tests);
                    mistake.IsFixed = isFixed;
                    if (isFixed)
                    {
                        count++;
                        _source.TraceEvent(TraceEventType.Information, 4,
                            "Program fixed: " + count);
                    }
                    else
                    {
                        notFixed.Add(mistake);
                        _source.TraceEvent(TraceEventType.Error, 3,
                        "Program not fixed:\r\nbefore\r\n" + mistake.before + " \r\n" +
                        mistake.after);
                    }
                }
                catch (NotImplementedException e)
                {
                    _source.TraceEvent(TraceEventType.Error, 2, 
                        "Transformation not implemented:\r\nbefore\r\n" + mistake.before + " \r\n" +
                        mistake.after + "\r\n" + e.Message);
                    transformationNotImplemented++;
                }
                _source.TraceEvent(TraceEventType.Stop, 1, "Submission " + submissionCount);
            }

            _source.TraceEvent(TraceEventType.Information, 5, "Total submissions: " + submissions.Count);
            _source.TraceEvent(TraceEventType.Information, 5, "input does not compile: " + doesNotCompile);
            _source.TraceEvent(TraceEventType.Information, 5, "Fixed: " + count);
            _source.TraceEvent(TraceEventType.Information, 5, "Not Fixed: " + notFixed.Count);
            _source.TraceEvent(TraceEventType.Information, 5, "parser not implemented: " + notImplementedYet);
            _source.TraceEvent(TraceEventType.Information, 5, "transformation not implemented: " + transformationNotImplemented);
        }

        private static void AnalyzeResults()
        {
            var submissions = JsonConvert.DeserializeObject<List<Mistake>>(File.ReadAllText(LogFolder + "submissionsResults.json"));

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
            Console.Out.WriteLine("Total: " + submissions.Count);
            Console.Out.WriteLine("Fixed: " + submissions.Where(e => e.IsFixed).Count());
        }

        private static void RunExperiment()
        {
            var notImplementedYet = 0;

            var product = new Tuple<TestBasedCluster.Question, string>(TestBasedCluster.Question.Product,
                LogFolder + "mistake_pairs_product_complete.json");
            var repeted = new Tuple<TestBasedCluster.Question, string>(TestBasedCluster.Question.Repeated,
                           LogFolder + "mistake_pairs_repeated_complete.json");
            var questionLogs = new[] { product, repeted };

            var cluster = new TestBasedCluster();
            cluster.GenerateCluster(questionLogs);
            var clusters = cluster.Clusters[TestBasedCluster.Question.Product];

            var tests = GetTests("product");
            var values = from pair in clusters
                         orderby pair.Value.Count descending
                         select pair.Value;

            var submissions = new List<Mistake>();
            //var target = values.ToList()[1];
            //submissions.AddRange(target);
            //submissions.AddRange(new List<Mistake>()
            //{
            //    target[55],
            //    target[88],
            //} );
            values.ForEach(submissions.AddRange);
            int transformationNotImplemented = 0;


            var classification = new List<Tuple<List<Mistake>, ProgramNode>>();

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
                    var list = new List<Mistake>() { current };
                    _source.TraceEvent(TraceEventType.Start, 6, "New group with mistake: " + i);
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
                            _source.TraceEvent(TraceEventType.Information, 6, "Trying to add mistake " + j);
                            try
                            {
                                var topProgram = SubmissionFixer.LearnProgram(list, next);
                                if (topProgram != null)
                                {
                                    list.Add(next);
                                    next.GeneratedFix = topProgram.ToString();
                                    _source.TraceEvent(TraceEventType.Information, 6, "Added");
                                }
                            }
                            catch (SyntaxErrorException)
                            {
                                _source.TraceEvent(TraceEventType.Information, 0, "Input does not compile");
                            }
                            catch (NotImplementedException e)
                            {
                                _source.TraceEvent(TraceEventType.Error, 2, 
                                    "Transformation not implemented:\r\nbefore\r\n" + next.before + " \r\n" +
                                    next.after + "\r\n" + e.Message);
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
                        _source.TraceEvent(TraceEventType.Information, 0, "Input does not compile");
                    }
                    catch (NotImplementedException)
                    {
                        transformationNotImplemented++;
                        _source.TraceEvent(TraceEventType.Error, 1, "feature not implemented");
                    }
                    _source.TraceEvent(TraceEventType.Stop, 6, "Ending group: " + i);
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
                _source.TraceEvent(TraceEventType.Start, 1, "Submission " + submissionCount);
                if (submissionCount == 9)
                    Console.Out.WriteLine("Achei");
                var unparser = new Unparser();
                PythonNode before = null;
                try
                {
                    before = NodeWrapper.Wrap(ASTHelper.ParseContent(mistake.before));
                    before = NodeWrapper.Wrap(ASTHelper.ParseContent(unparser.Unparse(before)));
                }
                catch (SyntaxErrorException)
                {
                    _source.TraceEvent(TraceEventType.Information, 0, "Input does not compile");
                    doesNotCompile++;
                    continue;
                }
                catch (NotImplementedException)
                {
                    _source.TraceEvent(TraceEventType.Error, 0, mistake.before);
                    notImplementedYet++;
                    continue;
                }

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
                        _source.TraceEvent(TraceEventType.Information, 4,
                            "Program fixed: " + count);
                    }
                    else
                    {
                        notFixed.Add(mistake);
                        _source.TraceEvent(TraceEventType.Error, 3,
                        "Program not fixed:\r\nbefore\r\n" + mistake.before + " \r\n" +
                        mistake.after);
                    }
                }
                catch (NotImplementedException e)
                {
                    _source.TraceEvent(TraceEventType.Error, 2,
                                    "Transformation not implemented:\r\nbefore\r\n" +  mistake.before + " \r\n" +
                                    mistake.after + "\r\n" + e.Message);
                    transformationNotImplemented++;
                }
                _source.TraceEvent(TraceEventType.Stop, 1, "Submission " + submissionCount);

            }

            _source.TraceEvent(TraceEventType.Information, 5, "Total submissions: " + submissions.Count);
            _source.TraceEvent(TraceEventType.Information, 5, "input does not compile: " + doesNotCompile);
            _source.TraceEvent(TraceEventType.Information, 5, "Fixed: " + count);
            _source.TraceEvent(TraceEventType.Information, 5, "Not Fixed: " + notFixed.Count);
            _source.TraceEvent(TraceEventType.Information, 5, "parser not implemented: " + notImplementedYet);
            _source.TraceEvent(TraceEventType.Information, 5, "transformation not implemented: " + transformationNotImplemented);
            _source.TraceEvent(TraceEventType.Information, 5, "Script sets: " + fixer.ProsePrograms.Count);
            _source.TraceEvent(TraceEventType.Information, 5, "Used Programs: " + (fixer.UsedPrograms.Count));


            var editSetDistribution = fixer.UsedPrograms.Select(e => Tuple.Create(CountEdits(e.Key), e.Value));
            _source.TraceEvent(TraceEventType.Information, 5, "Distribution of fixes");
            _source.TraceEvent(TraceEventType.Information, 5, "Edits, Submissions");
            foreach (var tuple in editSetDistribution)
            {
                _source.TraceEvent(TraceEventType.Information, 5, tuple.Item1 + " , " + tuple.Item2);
            }
            fixer.UsedPrograms.ForEach(e => _source.TraceEvent(TraceEventType.Information, 5, e + "\r\n"));

            //LogPerformance();
            _source.TraceEvent(TraceEventType.Information, 5, "Total of groups: " + classification.Count);
            foreach (var tuple in classification)
            {
                _source.TraceEvent(TraceEventType.Information, 5, "Number of mistakes: " + tuple.Item1.Count);
                _source.TraceEvent(TraceEventType.Information, 5, tuple.Item2.ToString());
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
from operator import add, mul

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
            r = new Regex(pat3, RegexOptions.IgnoreCase);
            m = r.Matches(node);
            var deletes = m.Count;
            return updates + deletes + inserts;
            //var result = (node.Symbol.Name.Equals("edit")) ? 1 : 0;
            //node.Children.ForEach(e => result += CountEdits(e));
            //return result;
        }
    }
}
