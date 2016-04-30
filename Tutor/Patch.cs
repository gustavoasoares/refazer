using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython.Compiler.Ast;

namespace Tutor
{
    public class Patch
    {
        public List<List<Edit>> EditSets { get; }

        public Patch()
        {
            EditSets = new List<List<Edit>>();
        }

        public Patch(List<Edit> editSet)
        {
            EditSets = new List<List<Edit>> {editSet};
        }

        public IEnumerable<PythonAst> Run(PythonNode ast)
        {
            var results = new List<PythonAst>();

            if (EditSets.Count == 1)
            {
                var edits = EditSets.First();
                foreach (var edit in edits)
                {
                    var rewriter = new Rewriter(new List<Edit>() { edit });
                    var newAst = rewriter.Rewrite(ast.InnerNode);
                    results.Add((PythonAst)newAst);
                }
                return results;
            }
            else
            {
                var hasEmptySet = EditSets.Any(e => !(e.Any()));
                if (hasEmptySet)
                    return null;
                List<Edit> firstEdits = EditSets.Select(e => e.First()).ToList();
                if (firstEdits.Any())
                {
                    var rewriter = new Rewriter(firstEdits);
                    var newAst = rewriter.Rewrite(ast.InnerNode);
                    results.Add((PythonAst)newAst);
                    return results;
                }
            }
            return null;
        }
    }
}
