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
    public class Patch
    {
        public IEnumerable<Node> Context { get;  }
        public Operation Operation { get; }

        public Patch(IEnumerable<Node> context, Operation operation)
        {
            Context = context;
            Operation = operation;
        }


        public List<PythonAst> Run(PythonAst ast)
        {
            var result = new List<PythonAst>();
            result.AddRange(Context.Select(node => Operation.Run(ast, node) as PythonAst));
            return result;
        }
    }

    public class Match
    {
        private PythonNode _template;

        public string NodeType { set; get; }
        public List<string> Children { set; get; }

        public Dictionary<int, List<Node>> MatchResult { get; private set; }

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

            public Dictionary<int, List<Node>> MatchResult { get; }

            public bool HasMatch { get; private set; }

            public CheckTemplateWalker(PythonNode template)
            {
                HasMatch = false;
                _template = template;
                MatchResult = new Dictionary<int, List<Node>>();
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

            public override bool Walk(ConstantExpression node)
            {
                return CheckTemplate(node);
            }

            public override bool Walk(TupleExpression node)
            {
                return CheckTemplate(node);
            }

            private bool CheckTemplate(Node node)
            {
                var result = _template.Match(node);
                if (result.Item1)
                {
                    HasMatch = true;
                    foreach (var keyValuePair in result.Item2)
                    {
                        if (MatchResult.ContainsKey(keyValuePair.Key))
                        {
                            MatchResult[keyValuePair.Key].Add(keyValuePair.Value);
                        }
                        else
                        {
                            MatchResult.Add(keyValuePair.Key, new List<Node>() {keyValuePair.Value});
                        }
                    }
                    return false;
                }
                return true;
            }
        }
    }
}
