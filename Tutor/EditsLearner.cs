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


        public List<PythonAst> Run(PythonAst ast)
        {
            var result = new List<PythonAst>();
            if (match.HasMatch(ast))
            {
                result.AddRange(match.MatchResult[1].Select(node => update.Run(ast, node) as PythonAst));
            }
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

            public override bool Walk(BinaryExpression node)
            {
                return CheckTemplate(node);
            }
            public override bool Walk(AssignmentStatement node)
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
                return false;
            }
        }
    }
}
