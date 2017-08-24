using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IronPython.Compiler.Ast;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tutor;

namespace Refazer.Experiments
{
    class Program
    {
        enum Options
        {
            RunExperiment = 1,
            AnalyzeResults = 2,
            TestExperiment = 3,
            PrintMistakes = 4,
            CleanData = 5, 
            PrintIncorretAttempts = 6,
            PrintNotFixed = 7,
            GetTimeTable =8,
            GetBootstrapTable=9,
            EditDistance=10
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
                    var problem = ProblemManager.Instance.GetProblemByName(problemName);
                    AnalyzeResults(problemName);
                    break;
                case (int)Options.RunExperiment:
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
                            RunExperiment(problem, learn: true);
                        }
                        else if (choice == 2)
                        {
                            RunExperiment(problem);
                        } else if (choice == 3)
                        {
                            Console.Out.WriteLine("Write the number of submissions to evaluate:");
                            choice = int.Parse(Console.ReadLine());
                            RunExperiment(problem, choice);
                        }
                        else if (choice == 4)
                        {
                            RunExperiment(problem, incorrect: true);
                        } else if (choice == 5)
                        {
                            RunBootstrapExperiment(problem); 
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
                    PrintCleaningOptions();
                    choice = int.Parse(Console.ReadLine());
                    if (choice == 1)
                    {
                        CleanProblemSumissions(problemName);
                    }
                    else if (choice == 2)
                    {
                        CleanIncorrectBenchmark(problemName);
                    }
                   
                    break;
                case (int)Options.PrintIncorretAttempts:
                    PrintProblemsMenu();
                    choice = int.Parse(Console.ReadLine());
                    problemName = (ProblemNames)choice;
                    PrintIncorrectAttemptResults(problemName);
                    break;
                case (int)Options.PrintNotFixed:
                    PrintProblemsMenu();
                    choice = int.Parse(Console.ReadLine());
                    problemName = (ProblemNames)choice;
                    PrintNotFixed(problemName);
                    break;
                case (int)Options.GetTimeTable:
                    PrintProblemsMenu();
                    choice = int.Parse(Console.ReadLine());
                    problemName = (ProblemNames)choice;
                    problem = ProblemManager.Instance.GetProblemByName(problemName);
                    ExtractTimeFromIncorrectResults(problem);
                    break;
                case (int)Options.GetBootstrapTable:
                    PrintProblemsMenu();
                    choice = int.Parse(Console.ReadLine());
                    problemName = (ProblemNames)choice;
                    problem = ProblemManager.Instance.GetProblemByName(problemName);
                    ExtractBootsrapTable(problem);
                    break;
                case (int)Options.EditDistance:
                    PrintProblemsMenu();
                    choice = int.Parse(Console.ReadLine());
                    problemName = (ProblemNames)choice;
                    CalculateEditDistance(problemName);
                    break;
                default:
                    Console.Out.WriteLine("Invalid option.");

                    break;
            }
            Console.ReadKey();
        }

        private static void CleanIncorrectBenchmark(ProblemNames problemName)
        {
            ProblemManager.Instance.ExtractFunctionAndSaveIncorrectBenchmark(problemName);
        }

        private static void PrintCleaningOptions()
        {
            Console.Out.WriteLine("1. Remove incorrect input/output examples");
            Console.Out.WriteLine("2. Extract problem functions from incorrect benchmark");
        }

        private static void RunBootstrapExperiment(Problem problem)
        {
            var fixedStudents = new List<int>();
            var classification = new ConcurrentQueue<Tuple<List<Mistake>, ProgramNode>>();
            var fixer = new SubmissionFixer(classification);
            var incorrect = new List<Mistake>();
            foreach (var student in problem.AttemptsPerStudent)
            {
                incorrect.AddRange(student.Value);
            }
            incorrect.Sort((a, b) => a.SubmissionTime.CompareTo(b.SubmissionTime));

            var correctList = problem.Mistakes.ToList();
            correctList.Sort((a, b) => a.SubmissionTime.CompareTo(b.SubmissionTime));
            var correct = new Queue<Mistake>(correctList);

            var tempFixer = new SubmissionFixer();
            var count = 0;
            foreach (var mistake in incorrect)
            {
                count++;
                //if this student has already received a hint, there will be no attempts after that
                //so ignore attempts from student's that already received hints
                if (fixedStudents.Contains(mistake.studentId))
                    continue;

                while (mistake.SubmissionTime.CompareTo(correct.First().SubmissionTime) > 0)
                {
                    var pair = correct.Dequeue();
                    //if this student has already received a hint, there will be no attempts after that
                    //so ignore correct solutions from student's that already received hints
                    if (fixedStudents.Contains(pair.studentId))
                        continue;
                    var hasGroup = false;
                    foreach (var current in classification)
                    {
                        try
                        {
                            var topProgram = tempFixer.LearnProgram(current.Item1, pair);
                            if (topProgram != null)
                            {
                                current.Item1.Add(pair);
                                pair.GeneratedFix = topProgram.ToString();
                                Source.TraceEvent(TraceEventType.Information, 6, "Added");
                                hasGroup = true;
                                break;
                            }
                        }
                        catch (SyntaxErrorException)
                        {
                            Source.TraceEvent(TraceEventType.Information, 0, "Input does not compile");
                        }
                        catch (NotImplementedException e)
                        {
                            Source.TraceEvent(TraceEventType.Error, 2,
                                "Transformation not implemented:\r\nbefore\r\n" + pair.before + " \r\n" +
                                pair.after + "\r\n" + e.Message);
                        }
                    }
                    if (!hasGroup)
                    {
                        var newList = new List<Mistake>() { pair };
                        try
                        {
                            var program = tempFixer.LearnProgram(newList);
                            classification.Enqueue(Tuple.Create(newList, program));
                        }
                        catch (SyntaxErrorException)
                        {
                            Source.TraceEvent(TraceEventType.Information, 0, "Input does not compile");
                        }
                        catch (NotImplementedException e)
                        {
                            Source.TraceEvent(TraceEventType.Error, 2,
                                "Transformation not implemented:\r\nbefore\r\n" + pair.before + " \r\n" +
                                pair.after + "\r\n" + e.Message);
                        }
                    }
                }

                Source.TraceEvent(TraceEventType.Start, 1, "Submission " + count);
                var unparser = new Unparser();
                PythonNode before = null;
                try
                {
                    before = NodeWrapper.Wrap(ASTHelper.ParseContent(mistake.before));
                    before = NodeWrapper.Wrap(ASTHelper.ParseContent(unparser.Unparse(before)));
                    var isFixed = fixer.Fix(mistake, problem.Tests);
                    mistake.IsFixed = isFixed;
                    if (isFixed)
                    {
                        Source.TraceEvent(TraceEventType.Information, 4,
                            "Program fixed: " + mistake.Id);
                        fixedStudents.Add(mistake.studentId);
                    }
                    else
                    {
                        Source.TraceEvent(TraceEventType.Error, 3,
                            "Program not fixed:\r\nbefore\r\n" + mistake.before + " \r\n" +
                            mistake.after);
                    }
                }
                catch (SyntaxErrorException)
                {
                    mistake.ErrorFlag = 1;
                    Source.TraceEvent(TraceEventType.Information, 0, "Input does not compile");
                }
                catch (NotImplementedException)
                {
                    mistake.ErrorFlag = 2;
                    Source.TraceEvent(TraceEventType.Error, 0, mistake.before);
                }
                catch (Exception)
                {
                    Source.TraceEvent(TraceEventType.Error, 0, "Transformation not tested");
                }

            }
            Source.TraceEvent(TraceEventType.Information, 1, "Summary of results");
            Source.TraceEvent(TraceEventType.Information, 1, "Students that received hints");
            foreach (var student in fixedStudents)
            {
                Source.TraceEvent(TraceEventType.Information, 1, student.ToString());
            }
            var submissionsToJson = JsonConvert.SerializeObject(incorrect);
            File.WriteAllText(problem.Id + "_bootstrap.json", submissionsToJson);
        }

        private static void PrintNotFixed(ProblemNames problemName)
        {
            var problem = ProblemManager.Instance.GetProblemByName(problemName);
            
            if (problem != null)
            {
                var classification = new ConcurrentQueue<Tuple<List<Mistake>, ProgramNode>>();
                var backupName = "../../resources/" + problem.Id + "-classification.json";
                var backup = new FileInfo(backupName);
                var backupClass = (backup.Exists) ?
                    JsonConvert.DeserializeObject<List<List<Mistake>>>(
                        File.ReadAllText(backupName)) : null;


                Console.Out.WriteLine("Problem: " + problem.Id);
                int i = 1;
                var tests = GetTests("product");
                var fileName = "../../results/" + problemName.ToString() + "-mistakes.json";
                var submissions = JsonConvert.DeserializeObject<List<Mistake>>(File.ReadAllText(fileName));

                var clustersBiggerThanOne = 0;
                var clustersBiggerThanTwo = 0;
                var countFixed = 0;
                var countNotFixed = 0;
                foreach (var mistake in submissions)
                {
                    if (!mistake.IsFixed && mistake.ErrorFlag == 0)
                    {
                        

                        if (backupClass != null)
                        {
                            foreach (var mistakes in backupClass)
                            {
                                if (mistakes.Contains(mistake))
                                {
                                    if (mistakes.Count > 1)
                                    {
                                        clustersBiggerThanOne++;
                                        if (mistakes.Count > 2)
                                            clustersBiggerThanTwo++;

                                        var fixer = new SubmissionFixer();
                                        //var program = fixer.LearnProgram(mistakes.Where(m => !m.Equals(mistake)).ToList());
                                        //if (program == null) throw new Exception();
                                        PythonAst ast = null;
                                        ast = ASTHelper.ParseContent(mistake.before);
                                        var input = State.Create(fixer.grammar.Value.InputSymbol, NodeWrapper.Wrap(ast));
                                        var unparser = new Unparser();
                                        //var fixedCode = fixer.TryFix(tests, program, input, unparser);
                                        //if (fixedCode == null)
                                        //{
                                        //    Console.Out.WriteLine("Should have fixed");
                                        //    countNotFixed++;
                                        //}
                                        //else
                                        //{
                                        //    Console.Out.WriteLine("PPROGRAM WAS FIXED");
                                        //    countFixed++;
                                        //}

                                        Console.Out.WriteLine("======================================");
                                        Console.Out.WriteLine("Size of the cluster: " + mistakes.Count);
                                        Console.Out.WriteLine("======================================");
                                        Console.Out.WriteLine("Mistake " + i);
                                        Console.Out.WriteLine("======================================");
                                        Console.Out.WriteLine("Before");
                                        Console.Out.WriteLine("======================================");
                                        Console.Out.WriteLine(mistake.before);
                                        Console.Out.WriteLine("======================================");
                                        Console.Out.WriteLine("After");
                                        Console.Out.WriteLine("======================================");
                                        Console.Out.WriteLine(mistake.after);
                                        Console.Out.WriteLine("======================================");
                                        Console.Out.WriteLine("Diff");
                                        Console.Out.WriteLine("======================================");
                                        Console.Out.WriteLine(mistake.diff);
                                    }
                                }
                            }
                        }
                        i++;
                    }
                    //if (i % 50 == 0)
                    //    Console.ReadKey();
                }
                Console.Out.WriteLine("Clusters bigger than one: " + clustersBiggerThanOne);
                Console.Out.WriteLine("Clusters bigger than two: " + clustersBiggerThanTwo);
                Console.Out.WriteLine("Fixed: " + countFixed);
                Console.Out.WriteLine("Not Fixed: " + countNotFixed);
            }
            else
            {
                Console.Out.WriteLine("Invalid problem.");
            }
        }

        private static void PrintIncorrectAttemptResults(ProblemNames problemName)
        {
            var fileName = "../../results/attemptperstudent-" + problemName.ToString() + ".json";
            var students = JsonConvert.DeserializeObject<Dictionary<int,IList<Mistake>>>(File.ReadAllText(fileName));

            var cluster = new Dictionary<string, List<Mistake>>();
            Console.Out.WriteLine("Student, Fixed, Total");
            foreach (var student in students)
            {
                if (student.Value.Count > 40)
                {
                    Console.Out.WriteLine("");
                }
                var id = student.Key;
                var total = student.Value.Count;
                var fixedIndex = 0;
                for (var i = 0; i < student.Value.Count; i++)
                {
                    if (student.Value[i].IsFixed)
                    {
                        fixedIndex = i + 1;
                        break;
                    }
                }
                Console.Out.WriteLine(id + ", " + fixedIndex + ", " + total);
            }
        }

        private static void PrintExperimentOptions()
        {
            Console.Out.WriteLine("1. Learn clusters");
            Console.Out.WriteLine("2. Run all submissions");
            Console.Out.WriteLine("3. Set number of submissions");
            Console.Out.WriteLine("4. Run on incorrect submissions");
            Console.Out.WriteLine("5. Bootstrap");
        }

        private static void CleanProblemSumissions(ProblemNames problemName)
        {
            var unparser = new Unparser();
            var problem = ProblemManager.Instance.GetProblemByName(problemName);
            if (problem != null)
            {
                Source.TraceEvent(TraceEventType.Information, 1, "Problem: " + problem.Id);
                var i = 1;
                var isfixed = 0;
                var notfixed = 0;
                var cleanMistakes = new List<Mistake>();
                Source.TraceEvent(TraceEventType.Information, 1, "Total mistakes: " + problem.Mistakes.Count());
                foreach (var mistake in problem.Mistakes)
                {
                    Source.TraceEvent(TraceEventType.Start, 1, "Testing Mistake " + i);
                    var submissionFixer = new SubmissionFixer();

                    PythonAst ast;
                    try
                    {
                        ast = ASTHelper.ParseContent(mistake.after);
                    }
                    catch (SyntaxErrorException)
                    {
                        Source.TraceEvent(TraceEventType.Information, 1, "Input does not compile");
                        i++;
                        continue;
                    }

                    try
                    {
                        var after = NodeWrapper.Wrap(ast);
                        if (problem.StaticTests != null && !submissionFixer.CheckStaticTests(after, problem.StaticTests))
                        {
                            Source.TraceEvent(TraceEventType.Information, 1, "Not Fixed by static analysis " + ++notfixed);
                            i++;
                            continue;
                        }
                        var code = unparser.Unparse(after);
                        if (submissionFixer.IsFixed(problem.Tests, code))
                        {
                            Source.TraceEvent(TraceEventType.Information, 1, "Fixed " + ++isfixed);
                            cleanMistakes.Add(mistake);
                        }
                        else
                        {
                            Source.TraceEvent(TraceEventType.Information, 1, "Not Fixed by tests" + ++notfixed);
                        }
                        Source.TraceEvent(TraceEventType.Stop, 1, "Testing Mistake " + i);
                        i++;
                    }
                    catch (NotImplementedException)
                    {
                        cleanMistakes.Add(mistake);
                        Source.TraceEvent(TraceEventType.Stop, 1, "Adding not implemented Mistake " + i);
                        i++;
                    }
                }
                problem.Mistakes = cleanMistakes;
                Source.TraceEvent(TraceEventType.Information, 1, "Total mistakes after clean: " + problem.Mistakes.Count());
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
                //int i = 1;
                var tests = GetTests("product");
                //var isfixed = 0;
                //var notfixed = 0;
                var subFixer = new SubmissionFixer();
                foreach (var mistake in problem.Mistakes)
                {

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
            Console.Out.WriteLine("6. Print incorrect attempts results");
            Console.Out.WriteLine("7. Print not fixed submissions");
            Console.Out.WriteLine("8. Extract time table from incorrect results");
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

        private static void AnalyzeResults(ProblemNames problemName)
        {
            var fileName = "../../results/" + problemName.ToString() + "-mistakes.json";
            var submissions = JsonConvert.DeserializeObject<List<Mistake>>(File.ReadAllText(fileName));

            var cluster = new Dictionary<string, List<Mistake>>();
            var unparser = new Unparser();
            foreach (var submission in submissions)
            {
                if (submission.IsFixed)
                {
                    var before = ASTHelper.ParseContent(submission.before);
                    submission.before = unparser.Unparse(NodeWrapper.Wrap(before));
                }

                //if (submission.IsFixed)
                //{
                //    if (cluster.ContainsKey(submission.UsedFix))
                //    {
                //        cluster[submission.UsedFix].Add(submission);
                //    }
                //    else
                //    {
                //        cluster.Add(submission.UsedFix, new List<Mistake>() {submission});
                //    }
                //}               
            }
            var submissionsToJson = JsonConvert.SerializeObject(submissions);
            File.WriteAllText(fileName, submissionsToJson);

            //var ordered = from entry in cluster
            //              orderby entry.Value.Count descending
            //              select entry;

            //var problem = ProblemManager.Instance.GetProblemByName(problemName);
            //var fixer = new SubmissionFixer();
            //var tests = problem.Tests;
            //var mistakeCount = 0;
            //foreach (var mistake in ordered)
            //{
            //    Console.Out.WriteLine("===========================================");
            //    Console.Out.WriteLine("Used program");
            //    Console.Out.WriteLine("===========================================");
            //    Console.Out.WriteLine(mistake.Key);
            //    Console.Out.WriteLine("===========================================");
            //    Console.Out.WriteLine("Total submissions: " + mistake.Value.Count);
            //    Console.Out.WriteLine("===========================================");
            //    Console.Out.WriteLine("Examples");
            //    Console.Out.WriteLine("===========================================");
            //    var numberOfExamples = mistake.Value.Count > 1 ? 2 : 1;
            //    for (int i = 0; i < numberOfExamples; i++)
            //    {
            //        mistakeCount++;
            //        Console.Out.WriteLine("Diff");
            //        Console.Out.WriteLine("===========================================");
            //        var submission = mistake.Value[i];
            //        Console.Out.WriteLine(submission.diff);
            //        Console.Out.WriteLine("===========================================");
            //        Console.Out.WriteLine("\r\nBefore:");
            //        Console.Out.WriteLine(submission.before);
            //        Console.Out.WriteLine("\r\nFixed After:");
            //        Console.Out.WriteLine(submission.SynthesizedAfter);
            //        Console.Out.WriteLine("\r\nAfter:");
            //        Console.Out.WriteLine(submission.after);
            //        Console.Out.WriteLine("===========================================");

            //        //if (fixer.IsFixed(tests, submission.SynthesizedAfter)) {
            //        //    Console.Out.WriteLine("Fixed"); 
            //        //} else
            //        //{
            //        //    Console.Out.WriteLine("Not fixed");
            //        //}

            //        if (mistakeCount % 50 == 0)
            //            Console.ReadKey();
            //    }


            //}
            //Console.Out.WriteLine("Total: " + submissions.Count);
            //Console.Out.WriteLine("Fixed: " + submissions.Where(e => e.IsFixed).Count());
            //Console.Out.WriteLine("Total of scripts: " + cluster.Count);
        }

        private static void RunExperiment(Problem problem, int numberOfSumissions = 0, bool learn = false, bool incorrect = false)
        {
            var submissions = numberOfSumissions == 0 ? problem.Mistakes.ToList()
                : problem.Mistakes.ToList().GetRange(0, numberOfSumissions);

            //var notImplementedYet = 0;
            var transformationNotImplemented = 0;

            var classification = new ConcurrentQueue<Tuple<List<Mistake>, ProgramNode>>();
            var backupName = "../../resources/" + problem.Id + "-classification.json";
            var backup = new FileInfo(backupName);
            var tempFixer = new SubmissionFixer();
            if (backup.Exists && !learn)
            {
                Source.TraceEvent(TraceEventType.Start, 6, "Learning scripts from existing classification");
                var backupClass =
                    JsonConvert.DeserializeObject<List<List<Mistake>>>(
                        File.ReadAllText(backupName));

                Source.TraceEvent(TraceEventType.Information, 6, "Number of clusters: " + backupClass.Count);
                var clusterCount = 1; 
                foreach (var list in backupClass)
                {
                    Source.TraceEvent(TraceEventType.Information, 6, "Learning cluster " + clusterCount + " with mistakes: " + list.Count);
                    var learnProgram = tempFixer.LearnProgram(list);
                    if (learnProgram != null)
                    {
                        classification.Enqueue(Tuple.Create(list, learnProgram));
                    }
                    clusterCount++;
                }
                Source.TraceEvent(TraceEventType.Stop, 6, "Learning scripts from existing classification");
            }
            else
            {
                Source.TraceEvent(TraceEventType.Start, 6, "Learning classification without backup");
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
                                    var topProgram = tempFixer.LearnProgram(list, next);
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
                            var learnProgram = tempFixer.LearnProgram(list);
                            if (learnProgram != null)
                            {
                                classification.Enqueue(Tuple.Create(list, learnProgram));
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
            }

            SaveClassification(classification, problem.Id);
            if (incorrect)
            {
                RunIncorrectAttemptExperiment(problem, classification);
            }
            else if (!learn)
            {
                var watch = new Stopwatch();
                watch.Start();
                Parallel.ForEach(submissions, (mistake) =>
                {
                    Source.TraceEvent(TraceEventType.Start, 1, "Submission " + mistake.Id);
                    var unparser = new Unparser();
                    PythonNode before = null;
                    try
                    {
                        before = NodeWrapper.Wrap(ASTHelper.ParseContent(mistake.before));
                        before = NodeWrapper.Wrap(ASTHelper.ParseContent(unparser.Unparse(before)));
                        var fixer = new SubmissionFixer(classification);
                        var isFixed = fixer.Fix(mistake, problem.Tests);
                        mistake.IsFixed = isFixed;
                        if (isFixed)
                        {
                            Source.TraceEvent(TraceEventType.Information, 4,
                                "Program fixed: " + mistake.Id);
                        }
                        else
                        {
                            Source.TraceEvent(TraceEventType.Error, 3,
                                "Program not fixed:\r\nbefore\r\n" + mistake.before + " \r\n" +
                                mistake.after);
                        }
                    }
                    catch (SyntaxErrorException)
                    {
                        mistake.ErrorFlag = 1;
                        Source.TraceEvent(TraceEventType.Information, 0, "Input does not compile");
                    }
                    catch (NotImplementedException)
                    {
                        mistake.ErrorFlag = 2; 
                        Source.TraceEvent(TraceEventType.Error, 0, mistake.before);
                    }
                    catch (Exception)
                    {
                        Source.TraceEvent(TraceEventType.Error, 0, "Transformation not tested");
                    }
                });
                watch.Stop();
                double total = ((double)watch.ElapsedMilliseconds / 1000) / 60;
                Source.TraceEvent(TraceEventType.Information, 0, "Total time: " + total);
               
                var count = 0;
                var notFixed = 0;
                var compError = 0;
                var notImpYet = 0;
                foreach (var submission in submissions)
                {
                    if (submission.IsFixed) count++;
                    else notFixed++;
                    if (submission.ErrorFlag == 1)
                        compError++;
                    else if (submission.ErrorFlag == 2)
                        notImpYet++;
                }

                Source.TraceEvent(TraceEventType.Information, 5, "Total submissions: " + submissions.Count);
                Source.TraceEvent(TraceEventType.Information, 5, "input does not compile: " + compError);
                Source.TraceEvent(TraceEventType.Information, 5, "Fixed: " + count);
                Source.TraceEvent(TraceEventType.Information, 5, "Not Fixed: " + notFixed);
                Source.TraceEvent(TraceEventType.Information, 5, "parser not implemented: " + notImpYet);
                //Source.TraceEvent(TraceEventType.Information, 5, "transformation not implemented: " + transformationNotImplemented);
                //Source.TraceEvent(TraceEventType.Information, 5, "Script sets: " + fixer.ProsePrograms.Count);
                //Source.TraceEvent(TraceEventType.Information, 5, "Used Programs: " + (fixer.UsedPrograms.Count));


                //var editSetDistribution = fixer.UsedPrograms.Select(e => Tuple.Create(CountEdits(e.Key), e.Value));
                //Source.TraceEvent(TraceEventType.Information, 5, "Distribution of fixes");
                //Source.TraceEvent(TraceEventType.Information, 5, "Edits, Submissions");
                //foreach (var tuple in editSetDistribution)
                //{
                //    Source.TraceEvent(TraceEventType.Information, 5, tuple.Item1 + " , " + tuple.Item2);
                //}
                //fixer.UsedPrograms.ForEach(e => Source.TraceEvent(TraceEventType.Information, 5, e + "\r\n"));

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
        }

        private static void CalculateEditDistance(ProblemNames problem)
        {
            var fileName = "../../results/" + problem + ".json";
            var listOfFixes = (IEnumerable) JsonConvert.DeserializeObject(File.ReadAllText(fileName));
            var distances = new List<int>();
            foreach (var fix in listOfFixes)
            {
                var fixDic = (JObject) fix;
                var before = (string) fixDic["before"];
                var after = (string) fixDic["SynthesizedAfter"];
                if (before != null && after != null)
                {
                    var beforeAst = ASTHelper.ParseContent(before);
                    var afterAst = ASTHelper.ParseContent(after);
                    var zss = new PythonZss(NodeWrapper.Wrap(beforeAst), NodeWrapper.Wrap(afterAst));
                    var editDistance = zss.Compute();
                    distances.Add(editDistance.Distance);
                }
            }
            PrintDistanceMetrics(distances, problem.ToString());
        }

        private static void PrintDistanceMetrics(List<int> distances, string problem)
        {
            
            Console.Out.WriteLine("Summary of distances between before and after solutions");
            Console.Out.WriteLine("Average distance: " + distances.Average());
            distances.Sort();
            StringBuilder sb = new StringBuilder();
            sb.Append(problem);
            sb.Append(Environment.NewLine); 
            foreach (var distance in distances)
            {
                sb.Append(distance);
                sb.Append(Environment.NewLine);
            }
            File.WriteAllText(problem + "_distance.csv", sb.ToString());
        }

        private static void ExtractBootsrapTable(Problem problem)
        {
            var fileName = "../../results/" + problem.Id.ToString() + "_bootstrap.json";
            var submissions = JsonConvert.DeserializeObject<List<Mistake>>(File.ReadAllText(fileName));
            var students = new Dictionary<int, List<Mistake>>();
            foreach (var submission in submissions)
            {
                if (students.ContainsKey(submission.studentId))
                {
                    students[submission.studentId].Add(submission);
                }
                else
                {
                    students.Add(submission.studentId, new List<Mistake>() {submission});
                }
            }
            var csv = new StringBuilder();
            csv.Append("Subject,Feedback,Attempt,Time" + Environment.NewLine);
            foreach (var student in students)
            {
                var attempts = student.Value;
                attempts.Sort((a,b)=> a.SubmissionTime.CompareTo(b.SubmissionTime));
                var attemptIndex = 0;
                var startTime = attempts.First().SubmissionTime;
                if (attempts.Exists(a => a.IsFixed))
                {
                    var index = attempts.FindIndex(a => a.IsFixed);
                    attemptIndex = index + 1; 
                }
                var hintEndTime = attemptIndex == 0
                    ? attempts.Last().SubmissionTime
                    : attempts[attemptIndex - 1].SubmissionTime;
                var totalTime = hintEndTime.Subtract(startTime).TotalMinutes;
                csv.Append(student.Key + ",hint," + attemptIndex + "," + totalTime + Environment.NewLine);
                csv.Append(student.Key + ",no hint," + attempts.Count + "," + attempts.Last().SubmissionTime.Subtract(startTime).TotalMinutes + Environment.NewLine);
            }
            Console.Out.WriteLine(csv.ToString());
        }


        private static void ExtractTimeFromIncorrectResults(Problem problem)
        {
            var results = new Queue<string>(File.ReadAllLines("../../results/" + problem.Id +".csv"));
            //remove header
            results.Dequeue();

            //the list of results / 2 should have the same size of the list of students 
            //because we have two lines per result (no hint and hint) 
            Debug.Assert(results.Count /2 == problem.AttemptsPerStudent.Count);

            var sb = new StringBuilder();
            var count = 1;
            sb.Append("Subject, Feedback, Time" + Environment.NewLine);
            var countIncorrect = 0;
            foreach (var student in problem.AttemptsPerStudent.Reverse())
            {
                if (count == 395)
                    Console.Out.WriteLine("Achei");
                var submissions = student.Value;

                var l1 = submissions.ToList();
                var l2 = submissions.ToList();
                l2.Sort((a, b) => a.SubmissionTime.CompareTo(b.SubmissionTime));

                if (!l1.SequenceEqual(l2))
                {
                    Console.Out.WriteLine("this is wrong");
                    countIncorrect++;
                }

                var startTime = submissions.First().SubmissionTime;

                //the first line is the hint treatment
                var hintResult = results.Dequeue().Split(',');
                var attempt = int.Parse(hintResult[2]);
                var hintEndTime = attempt != 0 ? submissions[attempt - 1].SubmissionTime : submissions.Last().SubmissionTime;
                var noHintEndTime = submissions.Last().SubmissionTime;

                sb.Append(count + ",hint," + hintEndTime.Subtract(startTime).TotalMinutes + Environment.NewLine);
                sb.Append(count + ",no hint," + noHintEndTime.Subtract(startTime).TotalMinutes + Environment.NewLine);

                //the second line is the no hint treatment, just ignore
                var noHintResult = results.Dequeue();
                count++;
            }
            Console.Out.WriteLine(sb.ToString());
        }

        private static
            void RunIncorrectAttemptExperiment(Problem problem, ConcurrentQueue<Tuple<List<Mistake>, ProgramNode>> classification)
        {
            var fixer = new SubmissionFixer(classification);
            var results = new System.Collections.Concurrent.ConcurrentDictionary<int, Tuple<int,int>>();
            
            var watch =  new Stopwatch();
            watch.Start();
            try
            {
                Parallel.ForEach(problem.AttemptsPerStudent, (student) =>
                {
                    Source.TraceEvent(TraceEventType.Start, 1, "Student " + student.Key);
                    var submissions = student.Value;
                    var attemptCount = 0;
                    var fixedAttempt = 0;
                    foreach (var mistake in submissions)
                    {
                        attemptCount++;
                        Source.TraceEvent(TraceEventType.Start, 1,
                            "Student " + student.Key + ", Attempt " + attemptCount);
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
                            //doesNotCompile++;
                            continue;
                        }
                        catch (NotImplementedException)
                        {
                            Source.TraceEvent(TraceEventType.Error, 0, mistake.before);
                            //notImplementedYet++;
                            continue;
                        }

                        try
                        {
                            var isFixed = fixer.Fix(mistake, problem.Tests);
                            mistake.IsFixed = isFixed;
                            if (isFixed)
                            {
                                fixedAttempt = attemptCount;
                                Source.TraceEvent(TraceEventType.Information, 4,
                                    "Student " + student.Key + ", Fixed Attempt " + attemptCount);
                                break;
                            }
                            Source.TraceEvent(TraceEventType.Error, 3,
                                "Program not fixed:\r\nbefore\r\n" + mistake.before + " \r\n" +
                                mistake.after);
                        }
                        catch (NotImplementedException e)
                        {
                            Source.TraceEvent(TraceEventType.Error, 2,
                                "Transformation not implemented:\r\nbefore\r\n" + mistake.before + " \r\n" +
                                mistake.after + "\r\n" + e.Message);
                            //transformationNotImplemented++;
                        }
                        catch (Exception e)
                        {
                            Source.TraceEvent(TraceEventType.Error, 2, e.StackTrace);
                        }
                    }
                    var newItem = Tuple.Create(fixedAttempt, submissions.Count);
                    results.AddOrUpdate(student.Key, newItem, (key, existing) => newItem);
                });
            }
            catch (AggregateException)
            {
                Source.TraceEvent(TraceEventType.Error, 2, "Exception in the outer loop. This aggregate exception should not happen");
            }
            watch.Stop();
            double total = ((double) watch.ElapsedMilliseconds / 1000)/60;
            Source.TraceEvent(TraceEventType.Information, 0, "Total time: " + total);
            Source.TraceEvent(TraceEventType.Information, 5, "Subject,Feedback,Attempt,Time");
            foreach (var current in results)
            {
                var result = current.Value;
                var attempts = problem.AttemptsPerStudent[current.Key];
                var startTime = attempts.First().SubmissionTime;
                var endTime = attempts.Last().SubmissionTime;
                var hintEndTime = result.Item1 != 0 ? attempts[result.Item1 - 1].SubmissionTime : endTime;
                Source.TraceEvent(TraceEventType.Information, 5, current.Key + ",hint," + result.Item1 + "," + hintEndTime.Subtract(startTime).TotalMinutes);
                Source.TraceEvent(TraceEventType.Information, 5, current.Key + ",no hint," + result.Item2 + "," + endTime.Subtract(startTime).TotalMinutes);
            }
            var submissionsToJson = JsonConvert.SerializeObject(problem.AttemptsPerStudent);
            File.WriteAllText("attemptperstudent-" + problem.Id + ".json", submissionsToJson);

            ValidateResults(results, problem.AttemptsPerStudent, problem);
        }

        private static void ValidateResults(ConcurrentDictionary<int, Tuple<int, int>> results, IDictionary<int, List<Mistake>> attemptsPerStudent, Problem p)
        {
            foreach (var student in attemptsPerStudent)
            {
                var result = results[student.Key];
                if (result.Item2 != student.Value.Count)
                    Source.TraceEvent(TraceEventType.Error, 5, "Length of the mistakes is different from the one in the results: " + 
                        result.Item2 + " " + student.Value.Count);
                if (result.Item1 > 0)
                {
                    var index = result.Item1 - 1; 
                    var fixedSubmission = student.Value[index];
                    if (!fixedSubmission.IsFixed)
                        Source.TraceEvent(TraceEventType.Error, 5, "Submission should have been fixed but is marked as notFixed. Student: " +
                                                                   student.Key + " Submission: " + index);
                    else
                    {
                        var fixer = new SubmissionFixer();
                        if (!fixer.IsFixed(p.Tests, fixedSubmission.SynthesizedAfter))
                        {
                            Source.TraceEvent(TraceEventType.Error, 5, "Submission should have been fixed but tests failed. Student: " +
                                                                   student.Key + " Submission: " + index);
                        }
                    }
                }                
            }
        }


        private static void SaveClassification(ConcurrentQueue<Tuple<List<Mistake>, ProgramNode>> classification, string id)
        {
            var  list = classification.Select(tuple => tuple.Item1).ToList();
            var submissionsToJson = JsonConvert.SerializeObject(list);
            File.WriteAllText(id + "-classification.json", submissionsToJson);
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
