using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tutor.ast
{
    internal class Rewriter2 : IRewriter
    {
        private readonly IEnumerable<Edit> _edits;

        internal Rewriter2(IEnumerable<Edit> edits)
        {
            _edits = edits;
        }

        public PythonNode Rewrite(PythonNode pythonNode)
        {
            var result = pythonNode.Clone();
            foreach (var edit in _edits)
            {
                if (edit.CanApply2(pythonNode))
                {
                    result = edit.Apply(result);
                }
            }
            return result;
        }
    }
}
