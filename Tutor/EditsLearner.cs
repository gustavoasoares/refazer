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
    public class Change
    {
        public IEnumerable<Dictionary<int, Node>> Context { get;  }
        public Operation Operation { get; }

        public Change(IEnumerable<Dictionary<int, Node>> context, Operation operation)
        {
            Context = context;
            Operation = operation;
        }


        public List<PythonAst> Run(PythonAst ast)
        {
            var result = new List<PythonAst>();
            result.AddRange(Context.Select(dict => Operation.Run(ast, dict[1]) as PythonAst));
            return result;
        }
    }

    public class Match
    {
        private PythonNode _template;

        public string NodeType { set; get; }
        public List<string> Children { set; get; }

        public IEnumerable<Dictionary<int, Node>> MatchResult { get; private set; }

        public Match(PythonNode template)
        {
            this._template = template;
        }

        public bool Run(Node code)
        {
            var checkTemplateWaker = new CheckTemplateWalker(_template);
            code.Walk(checkTemplateWaker);
            MatchResult = checkTemplateWaker.MatchResult;
            return checkTemplateWaker.HasMatch;
        }

        public bool HasMatch(PythonAst ast)
        {
            return Run(ast);
        }

        class CheckTemplateWalker : PythonWalker
        {
            private readonly PythonNode _template;

            public List<Dictionary<int, Node>> MatchResult { get; }

            public bool HasMatch { get; private set; }

            public CheckTemplateWalker(PythonNode template)
            {
                HasMatch = false;
                _template = template;
                MatchResult = new List<Dictionary<int, Node>>();
            }

            public override bool Walk(SuiteStatement node)
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
                    MatchResult.Add(result.Item2);
                    return false;
                }
                return true;
            }
        }
    }
}
