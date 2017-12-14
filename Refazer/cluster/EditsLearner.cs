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

        public PythonNode MatchResult { get; private set; }

        public Match(PythonNode template)
        {
            _template = template;
        }


        class CheckTemplateWalker : IVisitor
        {
            private readonly PythonNode _template;
            private bool _exact;

            public PythonNode MatchResult { get; private set; }

            public bool HasMatch { get; private set; }

            public CheckTemplateWalker(PythonNode template, bool exact)
            {
                HasMatch = false;
                _template = template;
                _exact = exact;
            }

            public bool Visit(PythonNode pythonNode)
            {
                return CheckTemplate(pythonNode);
            }

            private bool CheckTemplate(PythonNode node)
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
