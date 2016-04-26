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
            var inputs = new List<PythonAst>() {(PythonAst) ast.InnerNode};
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

        public static Edit Delete(PythonNode parent, Node deleted)
        {
            var wrappedAfter = NodeWrapper.Wrap(deleted);
            var delete = new Delete(wrappedAfter, parent);

            return delete;
        }

        public static Edit Update(PythonNode before, Node after)
        {
            var wrappedAfter = new PythonNode(after, false);    
            var update = new Update(wrappedAfter, before);            
            return update;
        }

        public static Edit Insert(PythonNode parent, Node newNode, int index)
        {
            var wrappedNewNode = new PythonNode(newNode, false);

            var insert = new Insert(wrappedNewNode, parent, index);
            return insert;
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
