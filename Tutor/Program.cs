using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython;
using IronPython.Compiler;
using IronPython.Compiler.Ast;
using IronPython.Hosting;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting.Hosting.Providers;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tutor
{
    class Program
    {
      

        static void Main(string[] args)
        {
            var classifier = new Classifier();
            classifier.GenerateQuestionClusterTable();
            classifier.GenerateTable2();
            classifier.GenerateTable3();

            Console.ReadKey();
        }

      
    }

    class Classifier
    {
        public const string CsvSeparator = ", ";
        private const string Table1Path = @"C:\Users\Gustavo\Box Sync\pesquisa\tutor\hw02-sp16\table1.csv";
        private const string Table2Path = @"C:\Users\Gustavo\Box Sync\pesquisa\tutor\hw02-sp16\table2.csv";
        private const string Table3Path = @"C:\Users\Gustavo\Box Sync\pesquisa\tutor\hw02-sp16\table3.csv";
        private const string LogEditClusters = @"C:\Users\Gustavo\Box Sync\pesquisa\tutor\hw02-sp16\logEditClusters.txt";

        public Dictionary<Question, Dictionary<string, List<Mistake>>> Clusters = 
            new Dictionary<Question, Dictionary<string, List<Mistake>>>();

        public enum Question
        {
            Product, Accumulate, SummationUsingAccumulate ,  FilteredAccumulate, Repeated, CountChange
        }

        private ScriptEngine py = Python.CreateEngine();
        

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
                    end = backup.Length-1;
                    break;
            }
            //it may be possible that the student changed the signature of the method, invalidating the pattern
            //in this case, consider the whole file.
            start = start == -1 ? 0 : start;
            end = (end == -1 || end < start) ? backup.Length - 1 : end;
            
            return backup.Substring(start, end - start);
        }

        public Dictionary<HashSet<string>, List<Mistake>> ClassifyMistakesByEditDistance(List<Mistake> mistakes, 
            Question question)
        {
            var result = new Dictionary<HashSet<string>, List<Mistake>>(new SetComparer<string>());
            
            foreach (var mistake in mistakes)
            {
                try
                {
                   
                    var ast1 = ParseContent(mistake.before, py);
                    var ast2 = ParseContent(mistake.after, py);
                    var zss = new PythonZss(ast1, ast2);

                    var editDistance = zss.Compute();
                    if (result.ContainsKey(editDistance.Item2))
                    {
                        result[editDistance.Item2].Add(mistake);
                    }
                    else
                    {
                        var newMistakeList = new List<Mistake>() { mistake };
                        result.Add(editDistance.Item2, newMistakeList);
                    }
                }
                catch (SyntaxErrorException)
                {
                    var syntaxError = "syntax error";
                    var synTaxErrorSet = new HashSet<string>() {syntaxError};
                    if (result.ContainsKey(synTaxErrorSet))
                    {
                        result[synTaxErrorSet].Add(mistake);
                    }
                    else
                    {
                        result.Add(synTaxErrorSet, new List<Mistake>() {mistake});
                    }
                }
            }
            return result;
        }

        private PythonAst ParseFile(string path, ScriptEngine py)
        {
            var src = HostingHelpers.GetSourceUnit(py.CreateScriptSourceFromFile(path));
            return Parse(py, src);
        }

        private PythonAst ParseContent(string content, ScriptEngine py)
        {
            var src = HostingHelpers.GetSourceUnit(py.CreateScriptSourceFromString(content));
            return Parse(py, src);
        }

        private PythonAst Parse(ScriptEngine py, SourceUnit src)
        {
            var pylc = HostingHelpers.GetLanguageContext(py);
            var parser = Parser.CreateParser(new CompilerContext(src, pylc.GetCompilerOptions(), ErrorSink.Default),
                (PythonOptions)pylc.Options);
            return parser.ParseFile(true);
        }

        public void GenerateQuestionClusterTable()
        {
            var product = new Tuple<Question,string>(Question.Product, 
                "C:/Users/Gustavo/Box Sync/pesquisa/tutor/hw02-sp16/mistake_pairs_product_complete.json");
            var countChange = new Tuple<Question, string>(Question.CountChange, 
                "C:/Users/Gustavo/Box Sync/pesquisa/tutor/hw02-sp16/mistake_pairs_count_change_complete.json");
            var accumulate = new Tuple<Question, string>(Question.Accumulate,
                "C:/Users/Gustavo/Box Sync/pesquisa/tutor/hw02-sp16/mistake_pairs_accumulate_complete.json");
            var summationUsingAccumulate = new Tuple<Question, string>(Question.SummationUsingAccumulate,
                "C:/Users/Gustavo/Box Sync/pesquisa/tutor/hw02-sp16/mistake_pairs_summation_using_accumulate_complete.json");

            var filteredAccumulate = new Tuple<Question, string>(Question.FilteredAccumulate,
                "C:/Users/Gustavo/Box Sync/pesquisa/tutor/hw02-sp16/mistake_pairs_filtered_accumulate_complete.json");

            var repeted = new Tuple<Question, string>(Question.Repeated,
                "C:/Users/Gustavo/Box Sync/pesquisa/tutor/hw02-sp16/mistake_pairs_repeated_complete.json");

            var questionLogs = new Tuple<Question, string>[] {product, accumulate, summationUsingAccumulate, filteredAccumulate,
                repeted, countChange };
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
                Clusters.Add(questionLog.Item1,cluster);
            }
            var csvFile = new StringBuilder();
            csvFile.Append("Question, Mistake, Cluster, Max_Cluster");
            foreach (var cluster in Clusters)
            {
                csvFile.Append(Environment.NewLine);
                csvFile.Append(cluster.Key);
                csvFile.Append(CsvSeparator);
                csvFile.Append(cluster.Value.Values.Sum(e => e.Count));
                csvFile.Append(CsvSeparator);
                csvFile.Append(cluster.Value.Keys.Count);
                csvFile.Append(CsvSeparator);
                csvFile.Append(cluster.Value.Values.Max(e => e.Count));
            }
            File.WriteAllText(Table1Path, csvFile.ToString());
        }

        public void GenerateTable2()
        {
            var csvFile = new StringBuilder();

            csvFile.Append("Question, Size");
            foreach (var cluster in Clusters)
            {
                
                var items = from pair in cluster.Value
                    orderby pair.Value.Count descending
                    select pair;

                foreach (var keyValuePair in items)
                {
                    csvFile.Append(Environment.NewLine);
                    csvFile.Append(cluster.Key);
                    csvFile.Append(CsvSeparator);
                    csvFile.Append(keyValuePair.Value.Count);
                }

                var seriazedCluster = JsonConvert.SerializeObject(items,Formatting.Indented);
                File.WriteAllText("C:/Users/Gustavo/Box Sync/pesquisa/tutor/hw02-sp16/" + cluster.Key.ToString() + ".json", seriazedCluster);
            }
            File.WriteAllText(Table2Path, csvFile.ToString());
        }

        public void GenerateTable3()
        {
            var csvFile = new StringBuilder();
            var logEditDistance = new StringBuilder();
            var editClusters = new EditClusters();

            csvFile.Append("Question, ClusterSize, SimilarEditDistance, MaxEditCluster");
            foreach (var cluster in Clusters)
            {

                var items = from pair in cluster.Value
                            orderby pair.Value.Count descending
                            select pair;

                logEditDistance.Append("Question: " + cluster.Key);


                foreach (var keyValuePair in items)
                {
                    logEditDistance.Append("============= Test Case =================");
                    logEditDistance.Append(Environment.NewLine);
                    logEditDistance.Append(keyValuePair.Key);
                    logEditDistance.Append(Environment.NewLine);
                    logEditDistance.Append("Total: " + keyValuePair.Value.Count + "=========================");
                    logEditDistance.Append(Environment.NewLine);

                    csvFile.Append(Environment.NewLine);
                    csvFile.Append(cluster.Key);
                    csvFile.Append(CsvSeparator);
                    csvFile.Append(keyValuePair.Value.Count);

                    var editDistanceClusters = 
                        ClassifyMistakesByEditDistance(keyValuePair.Value, Classifier.Question.CountChange);
                    logEditDistance.Append("Edit distance clusters total: " + editDistanceClusters.Count+ 
                        "====================================");
                    logEditDistance.Append(Environment.NewLine);

                    var editItems = from pair in editDistanceClusters
                        orderby pair.Value.Count descending
                        select pair;

                    var indexCluster = 1;
                    foreach (var editDistanceCluster in editItems)
                    {
                        editClusters.Add(editDistanceCluster.Key,cluster.Key,keyValuePair.Key,
                            editDistanceCluster.Value.Count, editDistanceCluster.Value);
                        logEditDistance.Append("====== Edit distance Cluster " + indexCluster++ + "========= Solutions: " 
                            + editDistanceCluster.Value.Count);
                        logEditDistance.Append(Environment.NewLine);
                        foreach (var editDistance in editDistanceCluster.Key)
                        {
                            logEditDistance.Append(editDistance);
                            logEditDistance.Append(Environment.NewLine);
                        }
                    }

                    csvFile.Append(CsvSeparator);
                    csvFile.Append(editDistanceClusters.Count);
                    csvFile.Append(CsvSeparator);
                    csvFile.Append(editDistanceClusters.Values.Max(e => e.Count));
                }
            }
            File.WriteAllText(LogEditClusters, logEditDistance.ToString());
            File.WriteAllText(Table3Path, csvFile.ToString());
            editClusters.LogInfo();
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
