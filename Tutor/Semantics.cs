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

        public static IEnumerable<PythonAst> Apply(PythonNode ast, IEnumerable<PythonAst> patch)
        {
            return patch;
        }

        public static IEnumerable<Edit> Single(Edit edit)
        {
            var result = new List<Edit>();
            if (edit != null) result.Add(edit);
            return result;
        }

        public static IEnumerable<Edit> Edits(Edit edit, IEnumerable<Edit> edits)
        {
            var result = new List<Edit>();
            if (edit != null && edits != null)
            {
                result.Add(edit);
                result.AddRange(edits);
            }
            return result;
        }

        public static PythonAst Update(PythonNode ast, PythonNode before, Node after)
        {
            var wrappedAfter = new PythonNode(after, false);
            var update = new Update(wrappedAfter, before);
            var newAst = update.Run(ast.InnerNode);
            return (PythonAst) newAst;
        }

        public static Edit Insert(Node parent, Node newNode, int index)
        {
            var wrappedParent = new PythonNode(parent, false);
            var wrappedNewNode = new PythonNode(newNode, false);

            var update = new Insert(wrappedNewNode, wrappedParent, index);
            return update;
        }

        public static Node LeafConstNode(NodeInfo info)
        {
            return NodeBuilder.Create(info);
        }

        public static Node ConstNode(NodeInfo info, IEnumerable<Node> children)
        {
            return NodeBuilder.Create(info, children.ToList());
        }

        public static Node ReferenceNode(PythonNode ast, PythonNode template)
        {
            var match = new Match(template);
            if (match.HasMatch(ast))
            {
                return match.MatchResult;
            }
            return null;
        }

        public static IEnumerable<Node> SingleChild(Node node)
        {
            return (node != null) ? new List<Node>() {node} : null;
        }

        public static IEnumerable<Node> Children(Node node, IEnumerable<Node> children)
        {
            if (node == null || children == null)
                return null;

            var result = new List<Node> {node};
            result.AddRange(children);
            return result;
        }

        public static bool Match(PythonNode ast, PythonNode context)
        {
            var match = new Match(context);
            return match.HasMatch(ast);

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
