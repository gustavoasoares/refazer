using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using IronPython.Compiler.Ast;
using IronPython.Modules;
using Microsoft.Scripting.Utils;
using BinaryExpression = IronPython.Compiler.Ast.BinaryExpression;
using ConstantExpression = IronPython.Compiler.Ast.ConstantExpression;

namespace Tutor
{
    public class EditsLearner
    {
        public EditsProgram Learn(PythonAst before, PythonAst after)
        {
            throw new NotImplementedException();
        }
    }

    public class EditsProgram
    {
    }

    public class Match
    {
        private readonly Node _ast;

        public string NodeType { set; get; }
        public List<string> Children { set; get; }

        public MatchResult MatchResult { get; set; }
        public int BindingIndex { get; set; }

        public Match(Node code, string nodeType, List<string> children, int bindingIndex)
        {
            BindingIndex = bindingIndex;
            _ast = code;
            NodeType = nodeType;
            this.Children = children;
            Run();
        }

        private void Run()
        {
            MatchResult = new MatchResult();
            var walker = new Walker { Match = this };
            _ast.Walk(walker);
            MatchResult.Anchor = walker.MatchedNode;
            if (walker.Target != null)
                MatchResult.Bindings = new List<Node>() {walker.Target};
        }

        public bool HasMatch(PythonAst ast)
        {
            return MatchResult != null && MatchResult.Anchor != null;
        }

        //todo add the other overriden methods
        class Walker : PythonWalker
        {
            public Match Match { set; get; }

            public Node MatchedNode { set; get; }

            public Node Target { get; set; }

            public override bool Walk(BinaryExpression node)
            {
                if (Match.NodeType != node.NodeName)
                    return true;

                var nodes = new List<string>() {Match.NodeType};
                nodes.AddRange(Match.Children);
                var subWalker = new SubTreeWalker(nodes, Match.BindingIndex);
                node.Walk(subWalker);
                if (subWalker.Children.Count == 0)
                {
                    Target = subWalker.Target;
                    MatchedNode = node;
                    return false;
                }
                return true;
            }

        }

        //todo add the other overriden methods
        class SubTreeWalker : PythonWalker
        {
            private int _bindingIndex;
            public List<string> Children { get; }

            public Node Target { set; get; } 

            public SubTreeWalker(List<string> children, int bindingIndex)
            {
                _bindingIndex = bindingIndex;
                this.Children = children;
            }

            public override bool Walk(NameExpression node)
            {
                if (CheckSimilarity(node)) return true;
                return false;
            }

            public override bool Walk(ConstantExpression node)
            {
                if (CheckSimilarity(node)) return true;
                return false;
            }

            public override bool Walk(BinaryExpression node)
            {
                if (CheckSimilarity(node)) return true;
                return true;
            }

            private bool CheckSimilarity(Node node)
            {
                if (node.NodeName.Equals(Children.First()))
                {
                    if (_bindingIndex == 0)
                    {
                        Target = node;
                    }
                    _bindingIndex--;
                    Children.RemoveAt(0);
                    return true;
                }
                return false;
            }
        }
    }

    public class MatchResult
    {
        public Node Anchor { set; get; }
        public List<Node> Bindings { set; get; }
    }
}
