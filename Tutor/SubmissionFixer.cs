using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython.Compiler.Ast;

namespace Tutor
{
    public class SubmissionFixer
    {
        public bool Fix(string program, List<EditsProgram> fixes, Dictionary<string, int> tests)
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

            foreach (var editsProgram in fixes)
            {
                var newAst = editsProgram.Run(ast);
                if (newAst == null)
                    continue;
                var unparser = new Unparser();
                var newCode = unparser.Unparse(newAst);

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
            return false;
        }
    }
}
