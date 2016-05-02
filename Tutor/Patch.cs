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

        public Patch(List<List<Edit>> editSets)
        {
            EditSets = editSets;
        }

        public Patch(List<Edit> editSet)
        {
            EditSets = new List<List<Edit>> {editSet};
        }


        public IEnumerable<PythonAst> Run(PythonNode ast)
        {
            var results = new List<PythonAst>();

            //if it does not find a match to some edit, return null
            //one edit may depends of another one.
            var hasEmptySet = EditSets.Any(e => !(e.Any()));
            if (hasEmptySet)
                return null;

            var combinations = GetAllCombinations();
            foreach (var combination in combinations)
            {
                var rewriter = new Rewriter(combination);
                var newAst = rewriter.Rewrite(ast.InnerNode);
                results.Add((PythonAst)newAst);
            }
            return results;
        }

        private List<List<Edit>> GetAllCombinations()
        {
            var combinations = new List<List<Edit>>();
            GetAllCombinationsUtil(combinations, new List<Edit>(), 0);
            return combinations;
        }

        private void GetAllCombinationsUtil(ICollection<List<Edit>> combinations, List<Edit> edits, int editSetIndex)
        {
            if (edits.Count.Equals(EditSets.Count))
            {
                combinations.Add(edits);
            }
            else
            {
                var editSet = EditSets[editSetIndex];
                foreach (var edit in editSet)
                {
                    var newEdits = new List<Edit>(edits);
                    newEdits.Add(edit);
                    var newIndex = editSetIndex + 1;
                    GetAllCombinationsUtil(combinations, newEdits, newIndex);
                }
            }
        }
    }
}
