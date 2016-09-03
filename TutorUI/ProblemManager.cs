using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Tutor;

namespace TutorUI
{
    enum ProblemNames
    {
        Product = 1, Repeated =2, CountChange = 3, Accumulate = 4, FilteredAccumulate = 5, Summation = 6,
        G = 7, G_iter = 8, Pingpong = 9
    }

    internal class ProblemManager
    {
        internal static ProblemManager Instance { get; } = new ProblemManager();
        internal IList<Problem> Problems { get; } = new List<Problem>();

        internal void CreateProblems()
        {
            var dir = new DirectoryInfo("../../benchmark/");
            foreach (var file in dir.GetFiles())
            {
                var problemName = (ProblemNames) Enum.Parse(typeof (ProblemNames), file.Name.Split('.')[0]);

                var submissions =
                    JsonConvert.DeserializeObject<List<Mistake>>(File.ReadAllText(file.FullName, Encoding.ASCII));
                var mistakes = new List<Mistake>();
                var count = 1;
                foreach (var mistake in submissions)
                {
                    mistake.Id = count;
                    count++;
                    mistake.before = GetQuestion(mistake.before, problemName);
                    mistake.after = GetQuestion(mistake.after, problemName);
                    if (mistake.date != null)
                        mistake.SubmissionTime = DateTime.Parse(mistake.date);
                    //Notice that some submissions have the same before and after
                    //probabily some error during ok python. Let's igore them for now
                    if (mistake.before.Equals(mistake.after))
                        continue;
                    mistakes.Add(mistake);
                }
                var problem = new Problem(problemName.ToString(), mistakes);
                problem.Tests = GetTests(problemName);
                problem.StaticTests = GetStaticTests(problemName);
                Problems.Add(problem);
            }
            AddIncorrectAttempts();
        }

        private Tuple<string, List<string>> GetStaticTests(ProblemNames problem)
        {
            switch (problem)
            {
                case ProblemNames.Summation:
                    return Tuple.Create("summation_using_accumulate", new List<string>() {"recursion", "for", "while"});
                case ProblemNames.G:
                    return Tuple.Create("g", new List<string>() {"for", "while"});
                case ProblemNames.G_iter:
                    return Tuple.Create("g_iter", new List<string>() {"recursion"});
                case ProblemNames.Pingpong:
                    return Tuple.Create("pingpong", new List<string>() { "Assign", "AugAssign" });
                default:
                    return null;
            }
        }

        private void AddIncorrectAttempts()
        {
            var dir = new DirectoryInfo("../../benchmark_incorrect/");
            foreach (var file in dir.GetFiles())
            {
                var problemName = (ProblemNames) Enum.Parse(typeof (ProblemNames), file.Name.Split('.')[0]);

                var submissions =
                    JsonConvert.DeserializeObject<List<Mistake>>(File.ReadAllText(file.FullName, Encoding.ASCII));
                var dic = new Dictionary<int, List<Mistake>>();
                submissions.Reverse();
                foreach (var mistake in submissions)
                {
                    if (mistake.date != null)
                        mistake.SubmissionTime = DateTime.Parse(mistake.date);

                    mistake.before = GetQuestion(mistake.before, problemName);
                    if (dic.ContainsKey(mistake.studentId))
                    {
                        var current = dic[mistake.studentId];
                        //only add the next mistake if the student changed something
                        if (!current.Last().before.Equals(mistake.before))
                            dic[mistake.studentId].Add(mistake);
                    }
                    else
                    {
                        dic.Add(mistake.studentId, new List<Mistake>() {mistake});
                    }
                }
                foreach (var student in dic)
                {
                    student.Value.Sort((a,b) => a.SubmissionTime.CompareTo(b.SubmissionTime));
                }
                var problem = GetProblemByName(problemName);
                problem.AttemptsPerStudent = dic;
            }
        }

        private static string GetTestSetup()
        {
            //File.ReadAllText("../../Resources/construct_check.py");
            var testSetup = @"
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
            return testSetup;
        }

        private static Dictionary<string, long> GetTests(ProblemNames problem)
        {
            var testSetup = GetTestSetup();

            switch (problem)
            {
                case ProblemNames.Product:
                    return new Dictionary<string, long>
                    {
                        {testSetup + "assert(product(3, identity)==6)", 6},
                        {"assert(product(5, identity)==120)", 120},
                        {"assert(product(3, square)==36)", 36},
                        {"assert(product(5, square)==14400)", 14400}
                    };
                case ProblemNames.Repeated:
                    return new Dictionary<string, long>
                    {
                        {testSetup + "assert(repeated(increment, 3)(5)== 8)", 8},
                        {"assert(repeated(triple, 5)(1)==243)", 243},
                        {"assert(repeated(square, 2)(5)==625)", 625},
                        {"assert(repeated(square, 3)(5)==390625)", 390625},
                        {"assert(repeated(square, 0)(5)==5)", 5}
                    };
                case ProblemNames.G:
                    return new Dictionary<string, long>
                    {
                        {testSetup + "assert(g(1)==1)", 15},
                        {"assert(g(2)==2)", 26},
                        {"assert(g(3)==3)", 11},
                        {"assert(g(4)==10)", 25},
                        {"assert(g(5)==22)", 72}
                    };
                case ProblemNames.G_iter:
                    return new Dictionary<string, long>
                    {
                        {testSetup + "assert(g_iter(1)==1)", 15},
                        {"assert(g_iter(2)==2)", 26},
                        {"assert(g_iter(3)==3)", 11},
                        {"assert(g_iter(4)==10)", 25},
                        {"assert(g_iter(5)==22)", 72}
                    };
                case ProblemNames.Accumulate:
                    return new Dictionary<string, long>
                    {
                        {testSetup + "assert(accumulate(add, 0, 5, identity)==15)", 15},
                        {"assert(accumulate(add, 11, 5, identity)==26)", 26},
                        {"assert(accumulate(add, 11, 0, identity)==11)", 11},
                        {"assert(accumulate(add, 11, 3, square)==25)", 25},
                        {"assert(accumulate(mul, 2, 3, square)==72)", 72}
                    };
                case ProblemNames.CountChange:
                    return new Dictionary<string, long>
                    {
                        {testSetup + "assert(count_change(7)==6)", 6},
                        {"assert(count_change(10)==14)", 14},
                        {"assert(count_change(20)==60)", 60},
                        {"assert(count_change(100)==9828)", 9828}
                    };
                case ProblemNames.Summation:
                    return new Dictionary<string, long>
                    {
                        {testSetup + "assert(summation_using_accumulate(5, square)==55)", 6},
                        {"assert(summation_using_accumulate(5, triple)==45)", 14},
                    };
                case ProblemNames.FilteredAccumulate:
                    return new Dictionary<string, long>
                    {
                        {testSetup + "assert(filtered_accumulate(add, 0, true, 5, identity)==15)", 15},
                        {"assert(filtered_accumulate(add, 11, false, 5, identity)==11)", 11},
                        {"assert(filtered_accumulate(add, 0, odd, 5, identity)==9)", 9},
                        {"assert(filtered_accumulate(mul, 1, odd, 5, square)==255)", 255},
                    };
                case ProblemNames.Pingpong:
                    return new Dictionary<string, long>
                    {
                        {testSetup + "assert(pingpong(7)==7)", 7},
                        {"assert(pingpong(8)==6)", 11},
                        {"assert(pingpong(15)==1)", 9},
                        {"assert(pingpong(21)==-1)", 255},
                        {"assert(pingpong(22)==0)", 255},
                        {"assert(pingpong(30)==6)", 255},
                        {"assert(pingpong(68)==2)", 255},
                        {"assert(pingpong(69)==1)", 255},
                        {"assert(pingpong(70)==0)", 255},
                        {"assert(pingpong(71)==1)", 255},
                        {"assert(pingpong(72)==0)", 255},
                        {"assert(pingpong(100)==2)", 255},
                    };
            }
            return null;
        }

        private string GetQuestion(string backup, ProblemNames problemName)
        {
            int start;
            int end;
            switch (problemName)
            {
                case ProblemNames.Product:
                    start = backup.IndexOf("def product(", StringComparison.Ordinal);
                    end = backup.IndexOf("def factorial(", StringComparison.Ordinal);
                    break;
                case ProblemNames.Accumulate:
                    start = backup.IndexOf("def accumulate(", StringComparison.Ordinal);
                    end = backup.IndexOf("def summation_using_accumulate(", StringComparison.Ordinal);
                    break;
                case ProblemNames.Summation:
                    start = backup.IndexOf("def accumulate(", StringComparison.Ordinal);
                    end = backup.IndexOf("def product_using_accumulate(", StringComparison.Ordinal);
                    break;
                case ProblemNames.FilteredAccumulate:
                    start = backup.IndexOf("def filtered_accumulate(", StringComparison.Ordinal);
                    end = backup.IndexOf("def repeated(", StringComparison.Ordinal);
                    break;
                case ProblemNames.Repeated:
                    start = backup.IndexOf("def repeated(", StringComparison.Ordinal);
                    end = backup.IndexOf("def g(", StringComparison.Ordinal);
                    break;
                case ProblemNames.CountChange:
                    start = backup.IndexOf("def count_change(", StringComparison.Ordinal);
                    end = backup.IndexOf("def move_stack(", StringComparison.Ordinal);
                    break;
                case ProblemNames.G:
                    start = backup.IndexOf("def g(", StringComparison.Ordinal);
                    end = backup.IndexOf("def g_iter(", StringComparison.Ordinal);
                    break;
                case ProblemNames.G_iter:
                    start = backup.IndexOf("def g_iter(", StringComparison.Ordinal);
                    end = backup.IndexOf("def pingpong(", StringComparison.Ordinal);
                    break;
                case ProblemNames.Pingpong:
                    start = backup.IndexOf("def pingpong(", StringComparison.Ordinal);
                    end = backup.IndexOf("def count_change(", StringComparison.Ordinal);
                    break;
                default:
                    start = 0;
                    end = backup.Length - 1;
                    break;
            }
            //it may be possible that the student changed the signature of the method, invalidating the pattern
            //in this case, consider the whole file.
            start = start == -1 ? 0 : start;
            var result = (end == -1 || end < start) ? backup.Substring(start) : backup.Substring(start, end - start);
            return result;
        }

        public Problem GetProblemByName(ProblemNames problemName)
        {
            return Problems.First(p => p.Id.Equals(problemName.ToString()));
        }

        public static void Save(Problem problem)
        {
            var dir = new DirectoryInfo("../../benchmark/");
            var submissionsToJson = JsonConvert.SerializeObject(problem.Mistakes);
            File.WriteAllText(dir + problem.Id + ".json", submissionsToJson);
        }

        public void ExtractFunctionAndSaveIncorrectBenchmark(ProblemNames problemName)
        {
            var problem = GetProblemByName(problemName);
            if (problem != null)
            {
                var file = new DirectoryInfo("../../benchmark_incorrect/" + problem.Id + ".json");
                var submissions =
                    JsonConvert.DeserializeObject<List<Mistake>>(File.ReadAllText(file.FullName, Encoding.ASCII));

                foreach (var mistake in submissions)
                {
                    if (mistake.date != null)
                        mistake.SubmissionTime = DateTime.Parse(mistake.date);
                    mistake.before = GetQuestion(mistake.before, problemName);
                }
                var submissionsToJson = JsonConvert.SerializeObject(submissions);
                File.WriteAllText(file.FullName, submissionsToJson);
            }

        }
    }
}