using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.Scripting;
using Newtonsoft.Json;
using Tutor;

namespace TutorUI
{
    class Program
    {
        enum Options
        {
            RunExperiment = 1,
            AnalyzeResults = 2,
            TestExperiment = 3,
            PrintMistakes = 4,
            CleanData = 5
        }

        

        private static readonly List<long> TimeToFix = new List<long>();
        private static readonly TraceSource Source = new TraceSource("experiment");

        private const string LogFolder = "../../benchmark/";
        private const string TimeFile = LogFolder + "time.txt";

        static void Main(string[] args)
        {
            ProblemManager.Instance.CreateProblems();

            PrintMainMenu();
            var choice = int.Parse(Console.ReadLine());
            
            switch (choice)
            {
                case (int)Options.AnalyzeResults:
                    PrintProblemsMenu();
                    choice = int.Parse(Console.ReadLine());
                    var problemName = (ProblemNames)choice;
                    AnalyzeResults();
                    break;
                case (int)Options.RunExperiment:
                    PrintProblemsMenu();
                    choice = int.Parse(Console.ReadLine());
                    problemName = (ProblemNames)choice;
                    var problem = ProblemManager.Instance.GetProblemByName(problemName);
                    if (problem != null)
                    {
                        PrintExperimentOptions();
                        choice = int.Parse(Console.ReadLine());
                        if (choice == 1)
                        {
                            RunExperiment(problem);
                        }
                        else if (choice == 2)
                        {
                            Console.Out.WriteLine("Write the number of submissions to evaluate:");
                            choice = int.Parse(Console.ReadLine());
                            RunExperiment(problem, choice);
                        }
                    }
                       
                    else
                        Console.Out.WriteLine("Problem not found.");
                    break;
                case (int)Options.TestExperiment:
                    PrintProblemsMenu();
                    choice = int.Parse(Console.ReadLine());
                    problemName = (ProblemNames)choice;
                    problem = ProblemManager.Instance.GetProblemByName(problemName);
                    if (problem != null)
                    {
                        PrintExperimentOptions();
                        choice = int.Parse(Console.ReadLine());
                        if (choice == 1)
                        {
                            CheckCanFixItself(problem);
                        }
                        else if (choice == 2)
                        {
                            Console.Out.WriteLine("Write the number of submissions to evaluate:");
                            choice = int.Parse(Console.ReadLine());
                            CheckCanFixItself(problem, choice);
                        }
                    }

                    else
                        Console.Out.WriteLine("Problem not found.");
                    break;
                case (int)Options.PrintMistakes:
                    PrintProblemsMenu();
                    choice = int.Parse(Console.ReadLine());
                    problemName = (ProblemNames) choice;
                    PrintMistake(problemName);
                    break;
                case (int)Options.CleanData:
                    PrintProblemsMenu();
                    choice = int.Parse(Console.ReadLine());
                    problemName = (ProblemNames)choice;
                    CleanProblemSumissions(problemName);
                    break;
                default:
                    Console.Out.WriteLine("Invalid option.");
                    break;
            }
            Console.ReadKey();
        }

        private static void PrintExperimentOptions()
        {
            Console.Out.WriteLine("1. Run all submissions");
            Console.Out.WriteLine("1. Set number of submissions");
        }

        private static void CleanProblemSumissions(ProblemNames problemName)
        {
            var problem = ProblemManager.Instance.GetProblemByName(problemName);
            if (problem != null)
            {
                Source.TraceEvent(TraceEventType.Information, 1, "Problem: " + problem.Id);
                var i = 1;
                var isfixed = 0;
                var notfixed = 0;
                var cleanMistakes = new List<Mistake>();
                foreach (var mistake in problem.Mistakes)
                {
                    Source.TraceEvent(TraceEventType.Start, 1, "Testing Mistake " + i);
                    if (SubmissionFixer.IsFixed(problem.Tests, mistake.after))
                    {
                        Source.TraceEvent(TraceEventType.Information, 1, "Fixed " + ++isfixed);
                        cleanMistakes.Add(mistake);
                    }
                    else
                    {
                        Source.TraceEvent(TraceEventType.Information, 1, "Not Fixed " + ++notfixed);
                    }
                    Source.TraceEvent(TraceEventType.Stop, 1, "Testing Mistake " + i);
                    i++;
                }
                problem.Mistakes = cleanMistakes;
                ProblemManager.Save(problem);
            }
            else
            {
                Console.Out.WriteLine("Invalid problem.");
            }
        }

        private static void PrintMistake(ProblemNames problemName)
        {
            var problem = ProblemManager.Instance.GetProblemByName(problemName);
            if (problem != null)
            {
                Console.Out.WriteLine("Problem: " + problem.Id);
                int i = 1;
                var tests = GetTests("product");
                var isfixed = 0;
                var notfixed = 0;
                foreach (var mistake in problem.Mistakes)
                {
                    if (SubmissionFixer.IsFixed(tests, mistake.after))
                    {
                        Console.Out.WriteLine("Fixed " + ++isfixed);
                    }
                    else
                    {
                        Console.Out.WriteLine("NotFixed " + ++notfixed);
                    }
                    //Console.Out.WriteLine("======================================");
                    //Console.Out.WriteLine("Mistake " + i);
                    //Console.Out.WriteLine("======================================");
                    //Console.Out.WriteLine("Before");
                    //Console.Out.WriteLine("======================================");
                    //Console.Out.WriteLine(mistake.before);
                    //Console.Out.WriteLine("======================================");
                    //Console.Out.WriteLine("After");
                    //Console.Out.WriteLine("======================================");
                    //Console.Out.WriteLine(mistake.after);
                    //Console.Out.WriteLine("======================================");
                    //Console.Out.WriteLine("Diff");
                    //Console.Out.WriteLine("======================================");
                    //Console.Out.WriteLine(mistake.diff);
                    //i++;
                }
            }
            else
            {
                Console.Out.WriteLine("Invalid problem.");   
            }
        }

        private static void PrintProblemsMenu()
        {
            Console.Out.WriteLine("Select the problem: ");
            var names = Enum.GetValues(typeof(ProblemNames)).Cast<ProblemNames>();
            var i = 1;
            foreach (var name in names)
            {
                Console.Out.WriteLine(i + ". " + name);
                i++;
            }
        }

        private static void PrintMainMenu()
        {
            Console.Out.WriteLine("Choose one of the options: ");
            Console.Out.WriteLine("1. Run Experiment");
            Console.Out.WriteLine("2. Analyze Results");
            Console.Out.WriteLine("3. Test experiment");
            Console.Out.WriteLine("4. Print mistakes");
            Console.Out.WriteLine("5. Check and Clean submissions");
        }

        private static void CheckCanFixItself(Problem problem, int numberOfSubmissions = 0)
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
                Source.TraceEvent(TraceEventType.Start, 1, "Submission " + submissionCount);

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
                    Source.TraceEvent(TraceEventType.Information, 0, "Input does not compile");
                    continue;
                }
                catch (NotImplementedException)
                {
                    Source.TraceEvent(TraceEventType.Error, 0, mistake.before);
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
                        Source.TraceEvent(TraceEventType.Information, 4,
                            "Program fixed: " + count);
                    }
                    else
                    {
                        notFixed.Add(mistake);
                        Source.TraceEvent(TraceEventType.Error, 3,
                        "Program not fixed:\r\nbefore\r\n" + mistake.before + " \r\n" +
                        mistake.after);
                    }
                }
                catch (NotImplementedException e)
                {
                    Source.TraceEvent(TraceEventType.Error, 2, 
                        "Transformation not implemented:\r\nbefore\r\n" + mistake.before + " \r\n" +
                        mistake.after + "\r\n" + e.Message);
                    transformationNotImplemented++;
                }
                Source.TraceEvent(TraceEventType.Stop, 1, "Submission " + submissionCount);
            }

            Source.TraceEvent(TraceEventType.Information, 5, "Total submissions: " + submissions.Count);
            Source.TraceEvent(TraceEventType.Information, 5, "input does not compile: " + doesNotCompile);
            Source.TraceEvent(TraceEventType.Information, 5, "Fixed: " + count);
            Source.TraceEvent(TraceEventType.Information, 5, "Not Fixed: " + notFixed.Count);
            Source.TraceEvent(TraceEventType.Information, 5, "parser not implemented: " + notImplementedYet);
            Source.TraceEvent(TraceEventType.Information, 5, "transformation not implemented: " + transformationNotImplemented);
        }

        private static void AnalyzeResults()
        {
            var submissions = JsonConvert.DeserializeObject<List<Mistake>>(File.ReadAllText(LogFolder + "submissionsResults-product.json"));

            var usedPrograms = new HashSet<string>();
            foreach (var submission in submissions)
            {
                if (submission.IsFixed)
                {
                    Console.Out.WriteLine("Diff:");
                    Console.Out.WriteLine(submission.diff);
                    Console.Out.WriteLine("Used program:");
                    Console.Out.WriteLine(submission.UsedFix);
                    Console.Out.WriteLine("\r\nBefore:");
                    Console.Out.WriteLine(submission.before);
                    Console.Out.WriteLine("\r\nFixed After:");
                    Console.Out.WriteLine(submission.SynthesizedAfter);
                    Console.Out.WriteLine("\r\nAfter:");
                    Console.Out.WriteLine(submission.after);
                    usedPrograms.Add(submission.UsedFix);

                }
            }
            Console.Out.WriteLine("Total: " + submissions.Count);
            Console.Out.WriteLine("Fixed: " + submissions.Where(e => e.IsFixed).Count());
        }

        private static void RunExperiment(Problem problem, int numberOfSumissions = 0)
        {
            var submissions = numberOfSumissions == 0 ? problem.Mistakes.ToList()
                : problem.Mistakes.ToList().GetRange(0, numberOfSumissions);

            var notImplementedYet = 0;
            var transformationNotImplemented = 0;

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
                    Source.TraceEvent(TraceEventType.Start, 6, "New group with mistake: " + i);
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
                            Source.TraceEvent(TraceEventType.Information, 6, "Trying to add mistake " + j);
                            try
                            {
                                var topProgram = SubmissionFixer.LearnProgram(list, next);
                                if (topProgram != null)
                                {
                                    list.Add(next);
                                    next.GeneratedFix = topProgram.ToString();
                                    Source.TraceEvent(TraceEventType.Information, 6, "Added");
                                }
                            }
                            catch (SyntaxErrorException)
                            {
                                Source.TraceEvent(TraceEventType.Information, 0, "Input does not compile");
                            }
                            catch (NotImplementedException e)
                            {
                                Source.TraceEvent(TraceEventType.Error, 2, 
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
                        Source.TraceEvent(TraceEventType.Information, 0, "Input does not compile");
                    }
                    catch (NotImplementedException)
                    {
                        transformationNotImplemented++;
                        Source.TraceEvent(TraceEventType.Error, 1, "feature not implemented");
                    }
                    Source.TraceEvent(TraceEventType.Stop, 6, "Ending group: " + i);
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
                Source.TraceEvent(TraceEventType.Start, 1, "Submission " + submissionCount);
                //if (submissionCount < 479)
                //    continue;
                var unparser = new Unparser();
                PythonNode before = null;
                try
                {
                    before = NodeWrapper.Wrap(ASTHelper.ParseContent(mistake.before));
                    before = NodeWrapper.Wrap(ASTHelper.ParseContent(unparser.Unparse(before)));
                }
                catch (SyntaxErrorException)
                {
                    Source.TraceEvent(TraceEventType.Information, 0, "Input does not compile");
                    doesNotCompile++;
                    continue;
                }
                catch (NotImplementedException)
                {
                    Source.TraceEvent(TraceEventType.Error, 0, mistake.before);
                    notImplementedYet++;
                    continue;
                }

                try
                {
                    var watch = Stopwatch.StartNew();
                    var isFixed = fixer.Fix(mistake, problem.Tests);
                    watch.Stop();
                    var timeInMS = watch.ElapsedMilliseconds;
                    TimeToFix.Add(timeInMS);

                    mistake.IsFixed = isFixed;
                    if (isFixed)
                    {
                        count++;
                        Source.TraceEvent(TraceEventType.Information, 4,
                            "Program fixed: " + count);
                    }
                    else
                    {
                        notFixed.Add(mistake);
                        Source.TraceEvent(TraceEventType.Error, 3,
                        "Program not fixed:\r\nbefore\r\n" + mistake.before + " \r\n" +
                        mistake.after);
                    }
                }
                catch (NotImplementedException e)
                {
                    Source.TraceEvent(TraceEventType.Error, 2,
                                    "Transformation not implemented:\r\nbefore\r\n" +  mistake.before + " \r\n" +
                                    mistake.after + "\r\n" + e.Message);
                    transformationNotImplemented++;
                }
                Source.TraceEvent(TraceEventType.Stop, 1, "Submission " + submissionCount);

            }

            Source.TraceEvent(TraceEventType.Information, 5, "Total submissions: " + submissions.Count);
            Source.TraceEvent(TraceEventType.Information, 5, "input does not compile: " + doesNotCompile);
            Source.TraceEvent(TraceEventType.Information, 5, "Fixed: " + count);
            Source.TraceEvent(TraceEventType.Information, 5, "Not Fixed: " + notFixed.Count);
            Source.TraceEvent(TraceEventType.Information, 5, "parser not implemented: " + notImplementedYet);
            Source.TraceEvent(TraceEventType.Information, 5, "transformation not implemented: " + transformationNotImplemented);
            Source.TraceEvent(TraceEventType.Information, 5, "Script sets: " + fixer.ProsePrograms.Count);
            Source.TraceEvent(TraceEventType.Information, 5, "Used Programs: " + (fixer.UsedPrograms.Count));


            var editSetDistribution = fixer.UsedPrograms.Select(e => Tuple.Create(CountEdits(e.Key), e.Value));
            Source.TraceEvent(TraceEventType.Information, 5, "Distribution of fixes");
            Source.TraceEvent(TraceEventType.Information, 5, "Edits, Submissions");
            foreach (var tuple in editSetDistribution)
            {
                Source.TraceEvent(TraceEventType.Information, 5, tuple.Item1 + " , " + tuple.Item2);
            }
            fixer.UsedPrograms.ForEach(e => Source.TraceEvent(TraceEventType.Information, 5, e + "\r\n"));

            //LogPerformance();
            Source.TraceEvent(TraceEventType.Information, 5, "Total of groups: " + classification.Count);
            foreach (var tuple in classification)
            {
                Source.TraceEvent(TraceEventType.Information, 5, "Number of mistakes: " + tuple.Item1.Count);
                Source.TraceEvent(TraceEventType.Information, 5, tuple.Item2.ToString());
            }

            var submissionsToJson = JsonConvert.SerializeObject(submissions);
            File.WriteAllText("submissionsResults.json", submissionsToJson);
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
