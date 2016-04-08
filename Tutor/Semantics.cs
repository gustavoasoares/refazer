using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using IronPython.Compiler.Ast;
using Microsoft.ProgramSynthesis.Extraction.Text.Semantics;

namespace Tutor.Transformation
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static class Semantics
    {
        public static IEnumerable<PythonAst> Apply(PythonNode ast, Operation edit, IEnumerable<Node> context)
        {
            var result = new List<PythonAst>();
            result.AddRange(context.Select(node => edit.Run(ast.InnerNode as PythonAst, node) as PythonAst));
            return result;
        }

        public static IEnumerable<Node> Match(PythonNode ast, PythonNode template)
        {
            var match = new Match(template);
            var hasMatch = match.Run(ast.InnerNode as PythonAst);
            return (hasMatch) ? match.MatchResult[1] : new List<Node>();
        }

    public static StringRegion SubStr(StringRegion v, Tuple<uint?, uint?> posPair)
        {
            uint? start = posPair.Item1, end = posPair.Item2;
            if (start == null || end == null || start < v.Start || start > v.End || end < v.Start || end > v.End)
                return null;
            return v.Slice((uint) start, (uint) end);
        }

        public static uint? AbsPos(StringRegion v, int k)
        {
            if (Math.Abs(k) > v.Length + 1) return null;
            return (uint) (k > 0 ? (v.Start + k - 1) : (v.End + k + 1));
        }

        public static uint? RegPos(StringRegion v, Tuple<RegularExpression, RegularExpression> rr, int k)
        {
            List<PositionMatch> ms = rr.Item1.Run(v).Where(m => rr.Item2.MatchesAt(v, m.Right)).ToList();
            int index = k > 0 ? (k - 1) : (ms.Count + k);
            return index < 0 || index >= ms.Count ? null : (uint?) ms[index].Right;
        }
    }
}
