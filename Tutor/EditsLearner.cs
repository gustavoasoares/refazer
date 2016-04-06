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
        private Match match;
        private Operation update;

        public EditsProgram(Match match, Operation update)
        {
            this.match = match;
            this.update = update;
        }


        public PythonAst Run(PythonAst ast)
        {
            if (match.HasMatch(ast))
            {
                return update.Run(ast, match.MatchResult) as PythonAst;
            }
            return null;
        }
    }

    public class Match
    {

        public string NodeType { set; get; }
        public List<string> Children { set; get; }

        public MatchResult MatchResult { get; set; }
        public BindingInfo BindingInfo { get; set; }

        public Match(string nodeType, List<string> children, BindingInfo bindingInfo)
        {
            BindingInfo = bindingInfo;
            NodeType = nodeType;
            this.Children = children;
        }

        public void Run(Node code)
        {
            MatchResult = new MatchResult();
            var walker = new Walker { Match = this };
            code.Walk(walker);
            MatchResult.Anchor = walker.MatchedNode;
            if (walker.Target != null)
                MatchResult.Bindings = new List<Node>() {walker.Target};
        }

        public bool HasMatch(PythonAst ast)
        {
            Run(ast);
            return MatchResult != null && MatchResult.Anchor != null;
        }

        //todo add the other overriden methods
        //todo, this cannot be a walker, it must be a visit expression becasue the walker does not stop when
        //finds the pattern
        class Walker : PythonWalker
        {
            private bool _continue = true; 
            public Match Match { set; get; }

            public Node MatchedNode { set; get; }

            public Node Target { get; set; }

            public override bool Walk(AssignmentStatement node)
            {
                if (!_continue) return true;

                if (Match.NodeType != node.NodeName)
                    return true;

                var nodes = new List<string>() { Match.NodeType };
                nodes.AddRange(Match.Children);
                var subWalker = new SubTreeWalker(nodes, Match.BindingInfo);
                node.Walk(subWalker);
                if (subWalker.Target == null) return true;
                Target = subWalker.Target;
                MatchedNode = node;
                _continue = false;

                return true;
            }

            public override bool Walk(BinaryExpression node)
            {
                if (!_continue) return true;

                if (Match.NodeType != node.NodeName)
                    return true;

                var nodes = new List<string>() { Match.NodeType };
                nodes.AddRange(Match.Children);
                var subWalker = new SubTreeWalker(nodes, Match.BindingInfo);
                node.Walk(subWalker);
                if (subWalker.Target == null) return true;
                Target = subWalker.Target;
                MatchedNode = node;
                _continue = false;
                return false;
            }

        }

        //todo add the other overriden methods
        class SubTreeWalker : PythonWalker
        {
            private bool _continue = true;

            private int _index;

            private int _value;
            public List<string> Children { get; }

            public Node Target { set; get; } 

            public SubTreeWalker(List<string> children, BindingInfo bindingInfo)
            {
                _index = bindingInfo.BindingIndex;
                _value = bindingInfo.BindingValue;
                this.Children = children;
            }

            public override bool Walk(AssignmentStatement node)
            {
                if (!_continue) return false;
                return CheckSimilarity(node);
            }

            public override bool Walk(TupleExpression node)
            {
                if (!_continue) return false;
                return CheckSimilarity(node);
            }

            public override bool Walk(NameExpression node)
            {
                if (!_continue) return false;

                return CheckSimilarity(node);
            }

            public override bool Walk(ConstantExpression node)
            {
                if (!_continue) return false;

                return CheckSimilarity(node);
            }

            public override bool Walk(BinaryExpression node)
            {
                return CheckSimilarity(node);
            }

            private bool CheckSimilarity(Node node)
            {
                if (node.NodeName.Equals(Children.First()))
                {
                    if (_index == 0 && node is ConstantExpression)
                    {
                        var literal = node as ConstantExpression;
                        if ((int)literal.Value == _value)
                            Target = node;
                    }
                    _index--;
                    Children.RemoveAt(0);
                    return true;
                }
                _continue = false;
                return false;
            }
        }
    }


    public class BindingInfo
    {
        public int BindingIndex { set; get; }

        public int BindingValue { set; get; }
    }
    public class MatchResult
    {
        public Node Anchor { set; get; }
        public List<Node> Bindings { set; get; }
    }
}
