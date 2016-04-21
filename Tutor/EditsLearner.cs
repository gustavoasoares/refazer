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
    public class Match
    {
        private PythonNode _template;

        public string NodeType { set; get; }
        public List<string> Children { set; get; }

        public Node MatchResult { get; private set; }

        public Match(PythonNode template)
        {
            this._template = template;
        }

        public bool Run(PythonNode code)
        {
            var targetInfo = _template.FindHeightTarget(0).Item2;
            var root = code; 
            while (targetInfo > 0)
            {
                if (code.Parent == null)
                    return false;
                root = code.Parent;
                targetInfo --;
            }

            var checkTemplateWaker = new CheckTemplateWalker(_template);
            root.InnerNode.Walk(checkTemplateWaker);
            MatchResult = checkTemplateWaker.MatchResult;
            return checkTemplateWaker.HasMatch && MatchResult.Equals(code.InnerNode);
        }

        public bool HasMatch(PythonNode ast)
        {
            return Run(ast);
        }

        

        class CheckTemplateWalker : PythonWalker
        {
            private readonly PythonNode _template;

            public Node MatchResult { get; private set; }

            public bool HasMatch { get; private set; }

            public CheckTemplateWalker(PythonNode template)
            {
                HasMatch = false;
                _template = template;
            }

            public override bool Walk(SuiteStatement node)
            {
                return CheckTemplate(node);
            }

            public override bool Walk(IfStatement node)
            {
                return CheckTemplate(node);
            }

            public override bool Walk(IfStatementTest node)
            {
                return CheckTemplate(node);
            }

            public override bool Walk(BinaryExpression node)
            {
                return CheckTemplate(node);
            }
            public override bool Walk(AssignmentStatement node)
            {
                return CheckTemplate(node);
            }

            public override bool Walk(ForStatement node)
            {
                return CheckTemplate(node);
            }

            public override bool Walk(WhileStatement node)
            {
                return CheckTemplate(node);
            }
            public override bool Walk(ReturnStatement node)
            {
                return CheckTemplate(node);
            }

            public override bool Walk(ConstantExpression node)
            {
                return CheckTemplate(node);
            }

            public override bool Walk(Parameter node)
            {
                return CheckTemplate(node);
            }

            public override bool Walk(TupleExpression node)
            {
                return CheckTemplate(node);
            }

            public override bool Walk(CallExpression node)
            {
                return CheckTemplate(node);
            }

            public override bool Walk(Arg node)
            {
                return CheckTemplate(node);
            }

            private bool CheckTemplate(Node node)
            {
                var result = _template.Match(node);
                if (result.Item1)
                {
                    HasMatch = true;
                    MatchResult = result.Item2;
                }
                return false;
            }
        }
    }
}
