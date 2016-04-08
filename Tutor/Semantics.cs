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
        public static IEnumerable<PythonAst> Apply(PythonNode ast, Patch patch)
        {            
            return (patch != null) ? patch.Run(ast.InnerNode as PythonAst) : new List<PythonAst>();
        }

        public static Patch Patch(Operation edit, IEnumerable<Node> context)
        {
            return (edit != null && context != null) ? new Patch(context,edit) : null;
        } 

        public static IEnumerable<Node> Match(PythonNode ast, PythonNode template)
        {
            var match = new Match(template);
            var hasMatch = match.Run(ast.InnerNode as PythonAst);
            return (hasMatch) ? match.MatchResult[1] : new List<Node>();
        }
    }
}
