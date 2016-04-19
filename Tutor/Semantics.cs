using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using IronPython.Compiler.Ast;

namespace Tutor.Transformation
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static class Semantics
    {
        public static IEnumerable<PythonAst> Patch(PythonNode ast, IEnumerable<Change> changes)
        {
            var result = new List<PythonAst>() {(PythonAst) ast.InnerNode};
            foreach (var change in changes)
            {
                var pythonAsts = new List<PythonAst>();
                foreach (var pythonAst in result)
                {
                    pythonAsts.AddRange(change.Run(pythonAst));
                }
                result = pythonAsts;
            }
            return result;
        }

     
        public static IEnumerable<Change> Single(Change change)
        {
            var result = new List<Change>();
            if (change != null) result.Add(change);
            return result;
        }

        public static IEnumerable<Change> Changes(Change change, IEnumerable<Change> changes)
        {
            var result = new List<Change>();
            if (change != null && changes != null)
            {
                result.Add(change);
                result.AddRange(changes);
            }
            return result;
        }

        public static Change Change(Operation edit, IEnumerable<Dictionary<int, Node>> context)
        {
            return (edit != null && context != null) ? new Change(context,edit) : null;
        } 

        public static IEnumerable<Dictionary<int, Node>> Match(PythonNode ast, PythonNode template)
        {
            var match = new Match(template);
            var hasMatch = match.Run(ast.InnerNode as PythonAst);

            return (hasMatch) ? match.MatchResult : null;
        }
    }
}
