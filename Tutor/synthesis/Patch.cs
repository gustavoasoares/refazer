using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython.Compiler.Ast;
using Microsoft.Scripting;
using Tutor.ast;

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


        public IEnumerable<PythonNode> Run(PythonNode ast)
        {
            var results = new List<PythonNode>();

            //if it does not find a match to some edit, return null
            //one edit may depends of another one.
            var hasEmptySet = EditSets.Any(e => !(e.Any()));
            if (hasEmptySet)
                return null;

            var combinations = GetAllCombinations();
            var unparser = new Unparser();
            foreach (var combination in combinations)
            {
                combination.ForEach(e => e.Applied = false);
                var rewriter = new Rewriter2(combination);
                try
                {
                    var newAst = ast.Rewrite(rewriter);

                    //hack to check if there is syntax errors: 
                    var code = unparser.Unparse(newAst);
                    ASTHelper.ParseContent(code);
                    results.Add(newAst);
                }
                catch (TransformationNotApplicableExpection e)
                {
                    //does not add program
                }
                catch (SyntaxErrorException)
                {
                    //does not add program
                }
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
