using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython.Compiler.Ast;
using IronPython.Modules;
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
        public List<IEnumerable<ProgramNode>> ProsePrograms { get; }

        private Result<Grammar> _grammar = DSLCompiler.LoadGrammarFromFile(@"C:\Users\Gustavo\git\Tutor\Tutor\Transformation.grammar");

        public SubmissionFixer()
        {
            ProsePrograms = new List<IEnumerable<ProgramNode>>();
        }
        public bool Fix(string program, string programAfter, Dictionary<string, int> tests)
        {
            PythonAst ast = null;
            try
            {
                ast = ASTHelper.ParseContent(program);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                return false;
            }
            var input = State.Create(_grammar.Value.InputSymbol, NodeWrapper.Wrap(ast));

            var unparser = new Unparser();

            foreach (var proseProgram in ProsePrograms)
            {
                if (TryFix(tests, proseProgram.First(), input, unparser)) return true;
                //if (TryFix(tests, proseProgram.ToList()[1], input, unparser)) return true;
                //if (TryFix(tests, proseProgram.ToList()[2], input, unparser)) return true;
            }

            //learn a new program
            var astAfter = NodeWrapper.Wrap(ASTHelper.ParseContent(programAfter));
            var examples = new Dictionary<State, object> { { input, astAfter } };
            var spec = new ExampleSpec(examples);
            var prose = new SynthesisEngine(_grammar.Value);
            var learned = prose.LearnGrammar(spec);
            ProsePrograms.Add(learned.RealizedPrograms);
            if (TryFix(tests, learned.RealizedPrograms.First(), input, unparser)) return true;
            return false;
        }

        private static bool TryFix(Dictionary<string, int> tests, ProgramNode current, State input, Unparser unparser)
        {
            var output = current.Invoke(input);
            if (output != null)
            {
                var programSet = output as IEnumerable<PythonAst>;

                foreach (var changedProgram in programSet)
                {
                    var newCode = unparser.Unparse(changedProgram);

                    Console.Out.WriteLine(changedProgram);
                    Console.Out.WriteLine("===================");
                    Console.Out.WriteLine("Fixed:");
                    Console.Out.WriteLine(newCode);

                    var isFixed = true;
                    foreach (var test in tests)
                    {
                        var script = newCode + Environment.NewLine + test.Key;
                        try
                        {
                            var result = ASTHelper.Run(script);
                            if (result != test.Value)
                                isFixed = false;
                        }
                        catch (Exception)
                        {
                            isFixed = false;
                        }
                    }
                    if (isFixed) return true;
                }
            }
            return false;
        }
    }
}
