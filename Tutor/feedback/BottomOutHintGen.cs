using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ProgramSynthesis.Learning;
using Tutor.Transformation;

namespace Tutor.feedback
{
    public class BottomOutHintGen
    {
        public IEnumerable<string> Generate(string before, string after)
        {
            var result = new List<string>();

            //parse code
            var beforeAst = Parse(before);
            var afterAast = Parse(after);

            //run tree edit distance
            var zss = new PythonZss(beforeAst, afterAast);
            var editDistance = zss.Compute();

            //get primary edits
            var rootAndNonRootEdits = WitnessFunctions.SplitEditsByRootsAndNonRoots(editDistance);
            //replace insert and delete by update
            var unparser = new Unparser();
            foreach (var edit in rootAndNonRootEdits.Item1)
            {
                if (edit is Update)
                {
                    result.Add("Update " + unparser.Unparse(edit.TargetNode)  + " to " + unparser.Unparse(edit.ModifiedNode));
                } else if (edit is Insert)
                {
                    result.Add("Insert " + unparser.Unparse(edit.ModifiedNode));
                }
               
            }
            //for each edit, create a hint
            return result;
        }

        private PythonNode Parse(string code)
        {
            return NodeWrapper.Wrap(ASTHelper.ParseContent(code));
        }

       
    }
}
