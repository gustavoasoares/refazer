using System;
using System.Collections.Generic;
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

namespace Tutor
{
    class Program
    {
        static void Main(string[] args)
        {
            var py = Python.CreateEngine();
            var ast1 = ParseFile(@"C:\Users\Gustavo\Box Sync\pesquisa\tutor\hw02-sp16\example_before.py", py);
            var ast2 = ParseFile(@"C:\Users\Gustavo\Box Sync\pesquisa\tutor\hw02-sp16\example_after.py", py);
            var zss = new PythonZss(ast1,ast2);

            var result = zss.Compute();
            Console.Out.WriteLine(result.Item1);
            var edits = result.Item2;
            foreach (var edit in edits)
            {
                Console.Out.WriteLine(edit);
            }
            Console.ReadKey();
        }

        static PythonAst ParseFile(string path, ScriptEngine py)
        {
            var src = HostingHelpers.GetSourceUnit(py.CreateScriptSourceFromFile(path));
            var pylc = HostingHelpers.GetLanguageContext(py);
            var parser = Parser.CreateParser(new CompilerContext(src, pylc.GetCompilerOptions(), ErrorSink.Default),
                (PythonOptions)pylc.Options);
            return parser.ParseFile(true);
        }
    }
}
