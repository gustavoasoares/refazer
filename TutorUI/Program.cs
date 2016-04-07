using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython.Compiler.Ast;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Compiler;
using Microsoft.ProgramSynthesis.Extraction.Text.Semantics;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Learning.Logging;
using Microsoft.ProgramSynthesis.Specifications;
using Microsoft.ProgramSynthesis.VersionSpace;
using Tutor;

namespace TutorUI
{
    class Program
    {
        static void Main(string[] args)
        {
            var grammar = DSLCompiler.LoadGrammarFromFile(@"C:\Users\Gustavo\git\Tutor\Tutor\Transformation.grammar");

            var astBefore = ASTHelper.ParseContent("x = 0");
            var input = State.Create(grammar.Value.InputSymbol, astBefore);
            var astAfter = ASTHelper.ParseContent("x = 1");

            var examples = new Dictionary<State, object> { { input, astAfter } };
            var spec = new ExampleSpec(examples);

            var prose = new SynthesisEngine(grammar.Value);
            var learned = prose.LearnGrammar(spec);
            var first = learned.RealizedPrograms.First();
            var output = first.Invoke(input) as IEnumerable<PythonAst> ;
            var fixedProgram = output.First();
            var unparser = new Unparser();
            var newCode = unparser.Unparse(fixedProgram);
            Debug.Assert("x=1" == newCode);
        }

     
    }
}
