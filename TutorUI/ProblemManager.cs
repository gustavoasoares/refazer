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
        Product = 1, Repeated =2, CountChange = 3, Accumulate = 4, FilteredAccumulate = 5, Summation = 6
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
                foreach (var mistake in submissions)
                {
                    mistake.before = GetQuestion(mistake.before, problemName);
                    mistake.after = GetQuestion(mistake.after, problemName);

                    //Notice that some submissions have the same before and after
                    //probabily some error during ok python. Let's igore them for now
                    if (mistake.before.Equals(mistake.after))
                        continue;
                    mistakes.Add(mistake);
                }
                var problem = new Problem(problemName.ToString(), mistakes);
                problem.Tests = GetTests(problemName);
                Problems.Add(problem);
            }
            AddIncorrectAttempts();
        }

        private void AddIncorrectAttempts()
        {
            var dir = new DirectoryInfo("../../benchmark_incorrect/");
            foreach (var file in dir.GetFiles())
            {
                var problemName = (ProblemNames)Enum.Parse(typeof(ProblemNames), file.Name.Split('.')[0]);

                var submissions =
                    JsonConvert.DeserializeObject<List<Mistake>>(File.ReadAllText(file.FullName, Encoding.ASCII));
                var dic = new Dictionary<int, IList<Mistake>>();
                foreach (var mistake in submissions)
                {
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
                        {testSetup + "add_three = repeated(increment, 3)\nadd_three(5)", 8},
                        {testSetup + "repeated(triple, 5)(1)", 243},
                        {testSetup + "repeated(square, 2)(5)", 625},
                        {testSetup + "repeated(square, 3)(5)", 390625},
                        {testSetup + "repeated(square, 0)(5)", 5}
                    };
                case ProblemNames.Accumulate:
                    return new Dictionary<string, long>
                    {
                        {testSetup + "accumulate(add, 0, 5, identity)", 15},
                        {testSetup + "accumulate(add, 11, 5, identity)", 26},
                        {testSetup + "accumulate(add, 11, 0, identity)", 11},
                        {testSetup + "accumulate(add, 11, 3, square)", 25},
                        {testSetup + "accumulate(mul, 2, 3, square)", 72}
                    };
                case ProblemNames.CountChange:
                    return new Dictionary<string, long>
                    {
                        {testSetup + "count_change(7)", 6},
                        {testSetup + "count_change(10)", 14},
                        {testSetup + "count_change(20)", 60},
                        {testSetup + "count_change(100)", 9828}
                    };
                case ProblemNames.FilteredAccumulate:
                    return new Dictionary<string, long>
                    {
                        {testSetup + "filtered_accumulate(add, 0, true, 5, identity)", 15},
                        {testSetup + "filtered_accumulate(add, 11, false, 5, identity)", 11},
                        {testSetup + "filtered_accumulate(add, 0, odd, 5, identity)", 9},
                        {testSetup + "filtered_accumulate(mul, 1, odd, 5, square)", 255},
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
                    start = backup.IndexOf("def summation_using_accumulate(", StringComparison.Ordinal);
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
                default:
                    start = 0;
                    end = backup.Length - 1;
                    break;
            }
            //it may be possible that the student changed the signature of the method, invalidating the pattern
            //in this case, consider the whole file.
            start = start == -1 ? 0 : start;
            end = (end == -1 || end < start) ? backup.Length - 1 : end;

            return backup.Substring(start, end - start);
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
    }
}