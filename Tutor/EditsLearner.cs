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

        public bool Run(PythonNode code, bool exact)
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

            var checkTemplateWaker = new CheckTemplateWalker(_template, exact);
            root.InnerNode.Walk(checkTemplateWaker);
            MatchResult = checkTemplateWaker.MatchResult;
            if (exact)
                return checkTemplateWaker.HasMatch && MatchResult.Equals(code.InnerNode);
            return checkTemplateWaker.HasMatch;
        }

        public bool ExactMatch(PythonNode ast)
        {
            return Run(ast, true);
        }

        public bool HasMatch(PythonNode ast)
        {
            return Run(ast, false);
        }





        class CheckTemplateWalker : PythonWalker
        {
            private readonly PythonNode _template;
            private bool _exact;

            public Node MatchResult { get; private set; }

            public bool HasMatch { get; private set; }

            public CheckTemplateWalker(PythonNode template, bool exact)
            {
                HasMatch = false;
                _template = template;
                _exact = exact;
            }

            public override bool Walk(SuiteStatement node)
            {
                return CheckTemplate(node);
            }

            public override bool Walk(IfStatement node)
            {
                return CheckTemplate(node);
            }

            public override bool Walk(ExpressionStatement node)
            {
                return CheckTemplate(node);
            }

            public override bool Walk(FunctionDefinition node)
            {
                return CheckTemplate(node);
            }

            public override bool Walk(AugmentedAssignStatement node)
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

            public override bool Walk(NameExpression node)
            {
                return CheckTemplate(node);
            }

            public override bool Walk(Parameter node)
            {
                return CheckTemplate(node);
            }

            public override bool Walk(ParenthesisExpression node)
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
                    return false;
                }
                return !_exact;
            }
        }
    }
}
