//using System;
//using System.Collections.Generic;
//using System.Linq;
//using IronPython;
//using IronPython.Compiler;
//using IronPython.Compiler.Ast;
//using IronPython.Hosting;
//using Microsoft.Scripting;
//using Microsoft.Scripting.Hosting;
//using Microsoft.Scripting.Hosting.Providers;
//using Microsoft.Scripting.Runtime;

//namespace Tutor
//{
//    //todo: add documentation
//    internal class EditClusters
//    {
//        private ScriptEngine py = Python.CreateEngine();

//        internal List<EditCluster> Clusters { set; get; }

//        internal EditClusters ()
//        {
//            Clusters = new List<EditCluster>();
//        }

//        internal void Add(HashSet<String> edits, TestBasedCluster.Question question, string testcase, 
//            int size, List<Mistake> mistakes)
//        {
//            if (HasCluster(edits))
//            {
//                var cluster = GetClusterByEdits(edits);
//                cluster.Questions.Add(question);
//                cluster.TestCases.Add(testcase);
//                cluster.Size += size;
//                cluster.Mistakes.AddRange(mistakes);
//            }
//            else
//            {
//                var cluster = new EditCluster()
//                {
//                    Edits = edits,
//                    Questions = new HashSet<TestBasedCluster.Question>() {question},
//                    TestCases = new HashSet<string>() {testcase} ,
//                    Size = size,
//                    Mistakes = new List<Mistake>()
//                };
//                cluster.Mistakes.AddRange(mistakes);
//                Clusters.Add(cluster);
//            }
//        }

//        private EditCluster GetClusterByEdits(HashSet<string> edits)
//        {
//            return Clusters.First(cluster => cluster.Edits.SetEquals(edits));
//        }

//        private bool HasCluster(HashSet<string> edits)
//        {
//            return Clusters.Any(cluster => cluster.Edits.SetEquals(edits));
//        }

//        public Dictionary<HashSet<string>, List<Mistake>> ClassifyMistakesByEditDistance(List<Mistake> mistakes,
//           TestBasedCluster.Question question)
//        {
//            var result = new Dictionary<HashSet<string>, List<Mistake>>(new SetComparer<string>());

//            foreach (var mistake in mistakes)
//            {
//                try
//                {

//                    var ast1 = ParseContent(mistake.before);
//                    var ast2 = ParseContent(mistake.after);
//                    var zss = new PythonZss(ast1, ast2);

//                    var editDistance = zss.Compute();
//                    if (result.ContainsKey(editDistance.Item2))
//                    {
//                        result[editDistance.Item2].Add(mistake);
//                    }
//                    else
//                    {
//                        var newMistakeList = new List<Mistake>() { mistake };
//                        result.Add(editDistance.Item2, newMistakeList);
//                    }
//                }
//                catch (SyntaxErrorException)
//                {
//                    var syntaxError = "syntax error";
//                    var synTaxErrorSet = new HashSet<string>() { syntaxError };
//                    if (result.ContainsKey(synTaxErrorSet))
//                    {
//                        result[synTaxErrorSet].Add(mistake);
//                    }
//                    else
//                    {
//                        result.Add(synTaxErrorSet, new List<Mistake>() { mistake });
//                    }
//                }
//            }
//            return result;
//        }

//        private PythonAst ParseFile(string path, ScriptEngine py)
//        {
//            var src = HostingHelpers.GetSourceUnit(py.CreateScriptSourceFromFile(path));
//            return Parse(src);
//        }

//        private PythonAst ParseContent(string content)
//        {
//            var src = HostingHelpers.GetSourceUnit(py.CreateScriptSourceFromString(content));
//            return Parse(src);
//        }

//        private PythonAst Parse(SourceUnit src)
//        {
//            var pylc = HostingHelpers.GetLanguageContext(py);
//            var parser = Parser.CreateParser(new CompilerContext(src, pylc.GetCompilerOptions(), ErrorSink.Default),
//                (PythonOptions)pylc.Options);
//            return parser.ParseFile(true);
//        }

//    }

//    internal class EditCluster
//    {
//        internal HashSet<String> Edits { set; get; }
//        internal HashSet<TestBasedCluster.Question> Questions { set; get; }

//        internal HashSet<String> TestCases { set; get; } 

//        internal int Size { set; get; }

//        internal List<Mistake> Mistakes { set; get; }

//    }
//}
