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

        public Dictionary<ProgramNode, int> UsedPrograms { get;  } 

        private Result<Grammar> _grammar = DSLCompiler.LoadGrammarFromFile(@"C:\Users\Gustavo\git\Tutor\Tutor\Transformation.grammar");

        public SubmissionFixer()
        {
            ProsePrograms = new List<IEnumerable<ProgramNode>>();
            UsedPrograms = new Dictionary<ProgramNode, int>();
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
                var size = proseProgram.Count();
                if (TryFix(tests, proseProgram.First(), input, unparser)) return true;
                if (size > 1) 
                    if (TryFix(tests, proseProgram.ElementAt(1), input, unparser)) return true;
                if (size > 2)
                    if (TryFix(tests, proseProgram.ElementAt(2), input, unparser)) return true;
            }

            //learn a new program
            var astAfter = NodeWrapper.Wrap(ASTHelper.ParseContent(programAfter));
            var examples = new Dictionary<State, object> { { input, astAfter } };
            var spec = new ExampleSpec(examples);
            var prose = new SynthesisEngine(_grammar.Value);
            var learned = prose.LearnGrammarTopK(spec, "Score");
            if (learned.Any())
            {
                ProsePrograms.Add(learned);
                if (TryFix(tests, learned.First(), input, unparser)) return true;
            }
            return false;
        }

        private bool TryFix(Dictionary<string, int> tests, ProgramNode current, State input, Unparser unparser)
        {
            Console.Out.WriteLine("===================");
            Console.Out.WriteLine("TRY:");
            Console.Out.WriteLine(current);
            Console.Out.WriteLine("===================");

            object output = null;
            try
            {
                output = current.Invoke(input);
            }
            catch (Exception)
            {
                return false; 
            }
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
                        catch (TestCaseException)
                        {
                            isFixed = false;
                        }
                        if (!isFixed)
                            break;
                    }
                    if (isFixed)
                    {
                        if (UsedPrograms.ContainsKey(current))
                        {
                            var count = UsedPrograms[current];
                            UsedPrograms.Remove(current);
                            UsedPrograms.Add(current, count + 1);
                        }
                        else
                        {
                            UsedPrograms.Add(current, 1);
                        }
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
