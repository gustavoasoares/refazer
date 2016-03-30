using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Instrumentation;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Tutor
{
    internal class EditClusters
    {
        private const string Path = @"C:\Users\Gustavo\Box Sync\pesquisa\tutor\hw02-sp16\editClusters\";

        private const string LogFile = 
            "C:/Users/Gustavo/Box Sync/pesquisa/tutor/hw02-sp16/editClusters.json";
        private const string LogTable =
           "C:/Users/Gustavo/Box Sync/pesquisa/tutor/hw02-sp16/editClusterTable.csv";

        internal List<EditCluster> Clusters { set; get; }

        internal EditClusters ()
        {
            Clusters = new List<EditCluster>();
        }

        internal void Add(HashSet<String> edits, Classifier.Question question, string testcase, 
            int size, List<Mistake> mistakes)
        {
            if (HasCluster(edits))
            {
                var cluster = GetClusterByEdits(edits);
                cluster.Questions.Add(question);
                cluster.TestCases.Add(testcase);
                cluster.Size += size;
                cluster.Mistakes.AddRange(mistakes);
            }
            else
            {
                var cluster = new EditCluster()
                {
                    Edits = edits,
                    Questions = new HashSet<Classifier.Question>() {question},
                    TestCases = new HashSet<string>() {testcase} ,
                    Size = size,
                    Mistakes = new List<Mistake>()
                };
                cluster.Mistakes.AddRange(mistakes);
                Clusters.Add(cluster);
            }
        }

        private EditCluster GetClusterByEdits(HashSet<string> edits)
        {
            return Clusters.First(cluster => cluster.Edits.SetEquals(edits));
        }

        private bool HasCluster(HashSet<string> edits)
        {
            return Clusters.Any(cluster => cluster.Edits.SetEquals(edits));
        }

        public void LogInfo()
        {
            var items = from item in Clusters
                orderby item.Size descending
                select item;

            var table = new StringBuilder();
            table.Append("Edits, Size, MultipleQuestions, MultipleTestCases");
            var log = new StringBuilder();
            var counter = 1;
            foreach (var cluster in items)
            {
                table.Append(Environment.NewLine);
                table.Append(cluster.Edits.Count);
                table.Append(Classifier.CsvSeparator);
                table.Append(cluster.Size);
                table.Append(Classifier.CsvSeparator);
                table.Append(cluster.Questions.Count > 1);
                table.Append(Classifier.CsvSeparator);
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
                mistakes.Append( "\tMISTAKES:");
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
                var fileName = Path + "editCluster" + (counter-1) +".txt";
                File.WriteAllText(fileName, mistakes.ToString());
            }
            File.WriteAllText(LogFile, log.ToString());
            File.WriteAllText(LogTable, table.ToString());
        }
    }

    internal class EditCluster
    {
        internal HashSet<String> Edits { set; get; }
        internal HashSet<Classifier.Question> Questions { set; get; }

        internal HashSet<String> TestCases { set; get; } 

        internal int Size { set; get; }

        internal List<Mistake> Mistakes { set; get; }

    }
}
