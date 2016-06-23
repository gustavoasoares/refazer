using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsQuery.ExtensionMethods.Internal;
using IronPython.Compiler.Ast;
using IronPython.Modules;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Compiler;
using Microsoft.ProgramSynthesis.Diagnostics;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Specifications;

namespace Tutor
{
    public class SubmissionFixer
    {
        private readonly List<Tuple<List<Mistake>, ProgramNode>> _classification;
        public List<IEnumerable<ProgramNode>> ProsePrograms { get; }

        public Dictionary<string, int> UsedPrograms { get; }

        private Result<Grammar> _grammar = DSLCompiler.LoadGrammarFromFile(@"C:\Users\Gustavo\git\Tutor\Tutor\synthesis\Transformation.grammar");

        public SubmissionFixer(List<Tuple<List<Mistake>, ProgramNode>> classification)
        {
            _classification = classification;
            ProsePrograms = new List<IEnumerable<ProgramNode>>();
            UsedPrograms = new Dictionary<string, int>();
        }

        public SubmissionFixer()
        {
            ProsePrograms = new List<IEnumerable<ProgramNode>>();
            UsedPrograms = new Dictionary<string, int>();
        }

        public bool Fix(Mistake mistake, Dictionary<string, long> tests)
        {
            PythonAst ast = null;
            try
            {
                ast = ASTHelper.ParseContent(mistake.before);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                return false;
            }
            var input = State.Create(grammar.Value.InputSymbol, NodeWrapper.Wrap(ast));

            var unparser = new Unparser();

            long totalTime = 0;
            foreach (var tuple in _classification)
            {
                mistake.Time = totalTime;
                var belongs = false;
                foreach (var mistake1 in tuple.Item1)
                {
                    if (mistake.Equals(mistake1))
                        belongs = true;
                }
                if (belongs)
                {
                    var listWithoutCurrentMistake = tuple.Item1.Where(e => !e.Equals(mistake));
                    if (!listWithoutCurrentMistake.Any()) return false;
                    var program = LearnProgram(listWithoutCurrentMistake.ToList());
                    if (program == null) return false;

                    var watch = new Stopwatch();
                    watch.Start();
                    var fixedCode = TryFix(tests, program, input, unparser);
                    watch.Stop();
                    totalTime += watch.ElapsedMilliseconds;
                    if (fixedCode != null)
                    {
                        mistake.Time = totalTime;
                        mistake.UsedFix = program.ToString();
                        mistake.SynthesizedAfter = fixedCode;
                        return true;
                    }
                }
                else
                {
                    var watch = new Stopwatch();
                    watch.Start();
                    var fixedCode = TryFix(tests, tuple.Item2, input, unparser);
                    watch.Stop();
                    totalTime += watch.ElapsedMilliseconds;
                    if (fixedCode != null)
                    {
                        mistake.Time = totalTime;
                        mistake.UsedFix = tuple.Item2.ToString();
                        mistake.SynthesizedAfter = fixedCode;
                        return true;
                    }
                }
            }
            mistake.Time = totalTime;
            return false;
        }

        public bool ParallelFix(Mistake mistake, Dictionary<string, long> tests)
        {
            PythonAst ast = null;
            try
            {
                ast = ASTHelper.ParseContent(mistake.before);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                return false;
            }
            var input = State.Create(grammar.Value.InputSymbol, NodeWrapper.Wrap(ast));
            var unparser = new Unparser();
            var isFixed = false;
            foreach (var tuple in _classification)
            {
                if (isFixed)
                    break;
                isFixed = TryInParallel(mistake, tests, tuple.Item2, input);
            }
            return isFixed;
        }

        private bool TryInParallel(Mistake mistake, Dictionary<string, long> tests, ProgramNode program, State input)
        {
            var  unparser = new Unparser();
            bool isFixed = false;
            var fixedCode = TryFix(tests, program, input, unparser);
            if (fixedCode != null)
            {
                mistake.UsedFix = program.ToString();
                mistake.SynthesizedAfter = fixedCode;
                isFixed = true;
            }
            return isFixed;
        }

        private static Result<Grammar> grammar =
            DSLCompiler.LoadGrammarFromFile(@"C:\Users\Gustavo\git\Tutor\Tutor\synthesis\Transformation.grammar");

        public static ProgramNode LearnProgram(List<Mistake> list, Mistake next)
        {
            var mistakes =  (list.Any()) ?  new List<Mistake>() { list.First(),  next } :
                new List<Mistake>() { next };
            if (LearnProgram(mistakes) != null)
            {
                mistakes  = (list.Count > 30) ? new List<Mistake>(list.GetRange(0,30)) { next }:
                    new List<Mistake>(list) { next };
                return LearnProgram(mistakes);
            }
            return null;
        }

        public static ProgramNode LearnProgram(List<Mistake> mistakes)
        {
            var examples = new Dictionary<State, object>();
            var unparser = new Unparser();
            foreach (var mistake in mistakes)
            {
                var astBefore = NodeWrapper.Wrap(ASTHelper.ParseContent(mistake.before));
                var input = State.Create(grammar.Value.InputSymbol, astBefore);
                var astAfter = NodeWrapper.Wrap(ASTHelper.ParseContent(mistake.after));
                examples.Add(input, astAfter);
            }
            var spec = new ExampleSpec(examples);
            var prose = new SynthesisEngine(grammar.Value);
            var learned = prose.LearnGrammarTopK(spec, "Score", k: 1);
            return learned.Any() ? learned.First() : null;
        }

        public static ProgramNode LearnProgram(Mistake mistake, State input)
        {
            var examples = new Dictionary<State, object>();

            var astAfter = NodeWrapper.Wrap(ASTHelper.ParseContent(mistake.after));
            examples.Add(input, astAfter);

            var spec = new ExampleSpec(examples);
            var prose = new SynthesisEngine(grammar.Value);
            var learned = prose.LearnGrammarTopK(spec, "Score", k: 1);
            return learned.Any() ? learned.First() : null;
        }


        private string TryFix(Dictionary<string, long> tests, ProgramNode current, State input, Unparser unparser)
        {
            //Console.Out.WriteLine("===================");
            //Console.Out.WriteLine("TRY:");
            //Console.Out.WriteLine(current);
            //Console.Out.WriteLine("===================");

            object output = null;
            try
            {
                output = current.Invoke(input);
            }
            catch (Exception)
            {
                return null;
            }
            if (output != null)
            {
                var programSet = output as IEnumerable<PythonNode>;
                var range = programSet.Count() < 10 ? programSet.ToList() : programSet.ToList().GetRange(0, 10); 
                foreach (var changedProgram in range)
                {
                    var newCode = unparser.Unparse(changedProgram);

                    //Console.Out.WriteLine(changedProgram);
                    //Console.Out.WriteLine("===================");
                    //Console.Out.WriteLine("Fixed:");
                    //Console.Out.WriteLine(newCode);

                    var isFixed = IsFixed(tests, newCode);
                    if (isFixed)
                    {
                        if (UsedPrograms.ContainsKey(current.ToString()))
                        {
                            var count = UsedPrograms[current.ToString()];
                            UsedPrograms.Remove(current.ToString());
                            UsedPrograms.Add(current.ToString(), count + 1);
                        }
                        else
                        {
                            UsedPrograms.Add(current.ToString(), 1);
                        }
                        return newCode;
                    }
                }
            }
            return null;
        }

        private static Object thisLock = new Object();

        public static bool IsFixed(Dictionary<string, long> tests, string newCode)
        {
            var isFixed = true;
            var script = newCode;
            foreach (var test in tests)
            {
               script +=  Environment.NewLine + test.Key;
            }
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("python.exe", "-c \"" + script + "\"")
                {
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                var p = Process.Start(psi);
                if (p == null)
                    isFixed = false;
                else
                {
                    if (!p.HasExited)
                    {
                        p.WaitForExit(600);
                    }
                    if (!p.HasExited || p.ExitCode != 0)
                        isFixed = false;
                    lock (thisLock)
                    {
                        if (!p.HasExited)
                        {
                            try
                            {
                                p.Kill();
                            }
                            catch (AggregateException)
                            {
                             //do nothing   
                            }
                        }
                        p.Close();
                    }
                    //var result = ASTHelper.Run(script);
                    //if (result != test.Value)
                    //    isFixed = false;
                }
            }
            catch (TestCaseException)
            {
                isFixed = false;
            }
            catch (RuntimeBinderException)
            {
                isFixed = false;
            }
            return isFixed;
        }

        public bool FixItSelf(Mistake mistake, Dictionary<string, long> tests)
        {
            var unparser = new Unparser();

            PythonAst ast = null;
            try
            {
                ast = ASTHelper.ParseContent(mistake.before);
                var cleanCode = unparser.Unparse(NodeWrapper.Wrap(ast));
                mistake.before = cleanCode;
                ast = ASTHelper.ParseContent(cleanCode);

                var astAfter = NodeWrapper.Wrap(ASTHelper.ParseContent(mistake.after));
                var cleanedAfter = unparser.Unparse(astAfter);
                mistake.after = cleanedAfter;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                return false;
            }
            var input = State.Create(grammar.Value.InputSymbol, NodeWrapper.Wrap(ast));
            var program = LearnProgram(mistake, input);
            if (program == null) return false;

            var fixedCode = TryFix(tests, program, input, unparser);
            if (fixedCode != null)
            {
                return true;
            }
            return false;
        }
    }
}
