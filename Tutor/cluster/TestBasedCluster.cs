using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Tutor
{
    public class TestBasedCluster
    {
        public Dictionary<Question, Dictionary<string, List<Mistake>>> Clusters =
            new Dictionary<Question, Dictionary<string, List<Mistake>>>();

        public enum Question
        {
            Product, Accumulate, SummationUsingAccumulate, FilteredAccumulate, Repeated, CountChange
        }

        public string GetQuestion(string backup, Question question)
        {
            int start;
            int end;
            switch (question)
            {
                case Question.Product:
                    start = backup.IndexOf("def product(", StringComparison.Ordinal);
                    end = backup.IndexOf("def factorial(", StringComparison.Ordinal);
                    break;
                case Question.Accumulate:
                    start = backup.IndexOf("def accumulate(", StringComparison.Ordinal);
                    end = backup.IndexOf("def summation_using_accumulate(", StringComparison.Ordinal);
                    break;
                case Question.SummationUsingAccumulate:
                    start = backup.IndexOf("def summation_using_accumulate(", StringComparison.Ordinal);
                    end = backup.IndexOf("def product_using_accumulate(", StringComparison.Ordinal);
                    break;
                case Question.FilteredAccumulate:
                    start = backup.IndexOf("def filtered_accumulate(", StringComparison.Ordinal);
                    end = backup.IndexOf("def repeated(", StringComparison.Ordinal);
                    break;
                case Question.Repeated:
                    start = backup.IndexOf("def repeated(", StringComparison.Ordinal);
                    end = backup.IndexOf("def g(", StringComparison.Ordinal);
                    break;
                case Question.CountChange:
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
        
        public void GenerateCluster(Tuple<Question, string>[] questionLogs)
        {
            foreach (var questionLog in questionLogs)
            {
                var cluster = new Dictionary<String, List<Mistake>>();
                var submissions = JsonConvert.DeserializeObject<List<Mistake>>(File.ReadAllText(questionLog.Item2, Encoding.ASCII));
                foreach (var submission in submissions)
                {
                    submission.before = GetQuestion(submission.before, questionLog.Item1);
                    submission.after = GetQuestion(submission.after, questionLog.Item1);

                    //Notice that some submissions have the same before and after
                    //probabily some error during ok python. Let's igore them for now
                    if (submission.before.Equals(submission.after))
                        continue;

                    if (submission.failed != null)
                    {
                        var testCase = string.Join("\n", submission.failed);
                        if (cluster.Keys.Contains(testCase))
                        {
                            cluster[testCase].Add(submission);
                        }
                        else
                        {
                            var mistakes = new List<Mistake> { submission };
                            cluster.Add(testCase, mistakes);
                        }
                    }
                }
                Clusters.Add(questionLog.Item1, cluster);
            }
        }
    }

    class SetComparer<T> : IEqualityComparer<HashSet<T>>
    {
        public bool Equals(HashSet<T> x, HashSet<T> y)
        {
            return x.SetEquals(y);
        }

        public int GetHashCode(HashSet<T> obj)
        {
            int hashcode = 0;
            foreach (T t in obj)
            {
                hashcode ^= t.GetHashCode();
            }
            return hashcode;
        }
    }
}
