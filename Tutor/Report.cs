using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Tutor
{
    class Report
    {
        /// <summary>
        /// Constants for writing log files
        /// </summary>
        private const string LogFolder = "C:/Users/Gustavo/Box Sync/pesquisa/tutor/hw02-sp16/";
        public const string CsvSeparator = ", ";
        private const string Table1Path = LogFolder + "table1.csv";
        private const string Table2Path = LogFolder + "table2.csv";
        private const string Table3Path = LogFolder +  "table3.csv";
        private const string LogEditClusters = LogFolder + "logEditClusters.txt";
        private const string Path = LogFolder + "editClusters/";
        private const string LogFile =
            LogFolder + "editClusters.json";
        private const string LogTable =
           LogFolder + "editClusterTable.csv";

        private const string ImportFolder = "C:/Users/Gustavo/Box Sync/pesquisa/tutor/hw02-sp16/";

        internal TestBasedCluster TestBasedCluster = new TestBasedCluster();
        private readonly EditClusters _editClusters = new EditClusters();

        /// <summary>
        /// Generate reports of student submissions with respect to clusters based on test cases and edit distance
        /// </summary>
        public void GenerateReport()
        {
            var questionLogs = CreateQuestionList();
            TestBasedCluster.GenerateCluster(questionLogs);

            GenerateQuestionClusterTable();
           // LogTestBasedClustersPerQuestion();
           // GenerateTable3();
           // LogEditsClusterInfo();

        }

        private static Tuple<TestBasedCluster.Question, string>[] CreateQuestionList()
        {
            //todo: create a class Question to represent quetions
            var product = new Tuple<TestBasedCluster.Question, string>(TestBasedCluster.Question.Product,
                ImportFolder + "mistake_pairs_product_complete.json");
            var countChange = new Tuple<TestBasedCluster.Question, string>(TestBasedCluster.Question.CountChange,
                ImportFolder + "mistake_pairs_count_change_complete.json");
            var accumulate = new Tuple<TestBasedCluster.Question, string>(TestBasedCluster.Question.Accumulate,
                ImportFolder + "mistake_pairs_accumulate_complete.json");
            var summationUsingAccumulate =
                new Tuple<TestBasedCluster.Question, string>(TestBasedCluster.Question.SummationUsingAccumulate,
                    ImportFolder + "mistake_pairs_summation_using_accumulate_complete.json");

            var filteredAccumulate = new Tuple<TestBasedCluster.Question, string>(TestBasedCluster.Question.FilteredAccumulate,
                ImportFolder + "mistake_pairs_filtered_accumulate_complete.json");

            var repeted = new Tuple<TestBasedCluster.Question, string>(TestBasedCluster.Question.Repeated,
                ImportFolder + "mistake_pairs_repeated_complete.json");

            var questionLogs = new[]
            {
                product, accumulate, summationUsingAccumulate, filteredAccumulate,
                repeted, countChange
            };
            return questionLogs;
        }

        /// <summary>
        /// Generates a table with the following columns:
        /// Question: name of the question
        /// Mistake: Number of incorrect submissions
        /// Cluster: number of TestBased clusters for each question
        /// Max_cluster the size of the biggest test based cluster
        /// </summary>
        public void GenerateQuestionClusterTable()
        {
            var csvFile = new StringBuilder();
            csvFile.Append("Question, Mistake, Cluster, Max_Cluster");
            foreach (var cluster in TestBasedCluster.Clusters)
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

        /// <summary>
        /// Genererates a json file and a table wit hthe following colums:
        /// Question: name of the question
        /// Size: size of the cluster
        /// The json file contains the clusters
        /// </summary>
        public void LogTestBasedClustersPerQuestion()
        {
            var csvFile = new StringBuilder();

            csvFile.Append("Question, Size");
            foreach (var cluster in TestBasedCluster.Clusters)
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

                var seriazedCluster = JsonConvert.SerializeObject(items, Formatting.Indented);
                File.WriteAllText(LogFolder + cluster.Key.ToString() + ".json", seriazedCluster);
            }
            File.WriteAllText(Table2Path, csvFile.ToString());
        }

        public void GenerateTable3()
        {
            var csvFile = new StringBuilder();
            var logEditDistance = new StringBuilder();

            csvFile.Append("Question, TestBasedClusterSize, EditDistanceCluster, MaxEditCluster");
            foreach (var cluster in TestBasedCluster.Clusters)
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
                         _editClusters.ClassifyMistakesByEditDistance(keyValuePair.Value, TestBasedCluster.Question.CountChange);
                    logEditDistance.Append("Edit distance clusters total: " + editDistanceClusters.Count +
                        "====================================");
                    logEditDistance.Append(Environment.NewLine);

                    var editItems = from pair in editDistanceClusters
                                    orderby pair.Value.Count descending
                                    select pair;

                    var indexCluster = 1;
                    foreach (var editDistanceCluster in editItems)
                    {
                        _editClusters.Add(editDistanceCluster.Key, cluster.Key, keyValuePair.Key,
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
        }

        public void LogEditsClusterInfo()
        {
            var items = from item in _editClusters.Clusters
                        orderby item.Size descending
                        select item;

            var table = new StringBuilder();
            table.Append("Operation, Size, MultipleQuestions, MultipleTestCases");
            var log = new StringBuilder();
            var counter = 1;
            foreach (var cluster in items)
            {
                table.Append(Environment.NewLine);
                table.Append(cluster.Edits.Count);
                table.Append(CsvSeparator);
                table.Append(cluster.Size);
                table.Append(CsvSeparator);
                table.Append(cluster.Questions.Count > 1);
                table.Append(CsvSeparator);
                table.Append(cluster.TestCases.Count > 1);
                log.Append(Environment.NewLine);
                log.Append(counter++ + "===================================");
                log.Append(Environment.NewLine);
                log.Append("SIZE: " + cluster.Size);
                log.Append(Environment.NewLine);
                log.Append("QUESTIONS: " + cluster.Questions.Count);
                log.Append(Environment.NewLine);
                log.Append("TEST_CASES: " + cluster.TestCases.Count);
                log.Append(Environment.NewLine);
                log.Append("EDITS:");
                foreach (var edit in cluster.Edits)
                {
                    log.Append(Environment.NewLine);
                    log.Append(edit);
                }

                log.Append(Environment.NewLine);
                log.Append("\tMISTAKES:");
                log.Append(Environment.NewLine);
                log.Append("\tBEFORE:");
                log.Append(Environment.NewLine);
                log.Append(cluster.Mistakes.FirstOrDefault().before);
                log.Append(Environment.NewLine);
                log.Append("\tAFTER:");
                log.Append(Environment.NewLine);
                log.Append(cluster.Mistakes.FirstOrDefault().after);

                var mistakes = new StringBuilder();
                mistakes.Append("\tMISTAKES:");
                foreach (var mistake in cluster.Mistakes)
                {
                    mistakes.Append(Environment.NewLine);
                    mistakes.Append("\tBEFORE:");
                    mistakes.Append(Environment.NewLine);
                    mistakes.Append(mistake.before);
                    mistakes.Append(Environment.NewLine);
                    mistakes.Append("\tAFTER:");
                    mistakes.Append(Environment.NewLine);
                    mistakes.Append(mistake.after);
                }
                var fileName = Path + "editCluster" + (counter - 1) + ".txt";
                File.WriteAllText(fileName, mistakes.ToString());
            }
            File.WriteAllText(LogFile, log.ToString());
            File.WriteAllText(LogTable, table.ToString());
        }
    }
}
