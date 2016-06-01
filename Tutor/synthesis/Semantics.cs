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

        public static IEnumerable<PythonNode> Apply(PythonNode ast, Patch patch)
        {
            var inputs = new List<PythonNode>() {ast};
            var asts = patch.Run(ast);
            return asts;
        }

        public static Patch Patch(IEnumerable<Edit> editSet)
        {
            var patch = new Patch(editSet.ToList());
            return patch;
        }

        public static Patch ConcatPatch(IEnumerable<Edit> editSet, Patch patch)
        {
            patch.EditSets.Insert(0,editSet.ToList());
            return patch;
        }

        public static Edit Delete(PythonNode parent, PythonNode deleted)
        {
            var delete = new Delete(deleted, parent);

            return delete;
        }

        public static Edit Update(PythonNode before, PythonNode after)
        {
            var update = new Update(after, before);            
            return update;
        }

        public static Edit Insert(PythonNode parent, PythonNode newNode, int index)
        {
            var insert = new Insert(newNode, parent, index);
            return insert;
        }

        public static Edit Move(PythonNode parent, PythonNode movedNode, int index)
        {
            var move = new Move(movedNode, parent, index);
            return move;
        }

        public static PythonNode LeafConstNode(NodeInfo info)
        {
            var wrapped = NodeWrapper.Wrap(NodeBuilder.Create(info));
            wrapped.Reference = false;
            return wrapped;
        }

        public static PythonNode ConstNode(NodeInfo info, IEnumerable<PythonNode> children)
        {
            var wrapped = NodeWrapper.Wrap(NodeBuilder.Create(info, children.ToList()));
            wrapped.Reference = false;
            return wrapped;
        }

        public static PythonNode ReferenceNode(PythonNode ast, PythonNode template)
        {
            var match = new Match(template);
            if (match.HasMatch(ast))
            {
                return match.MatchResult;
            }
            return null;
        }

        public static IEnumerable<PythonNode> SingleChild(PythonNode node)
        {
            return (node != null) ? new List<PythonNode>() {node} : null;
        }

        public static IEnumerable<PythonNode> Children(PythonNode node, IEnumerable<PythonNode> children)
        {
            if (node == null || children == null)
                return null;

            var result = new List<PythonNode> {node};
            result.AddRange(children);
            return result;
        }

        public static bool Match(PythonNode ast, PythonNode context)
        {
            var match = new Match(context);
            return match.ExactMatch(ast);

        }

        public static IEnumerable<PythonNode> InOrderSort(PythonNode ast)
        {
            var visitor = new PythonNodeVisitor();
            ast.Walk(visitor);
            return visitor.SortedNodes;
        }
    }

    public class PythonNodeVisitor : IVisitor
    {
        public List<PythonNode> SortedNodes = new List<PythonNode>();
         
        public bool Visit(PythonNode pythonNode)
        {
            SortedNodes.Add(pythonNode);
            return true;
        }
    }
}
