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
            root.Walk(checkTemplateWaker);
            MatchResult = checkTemplateWaker.MatchResult;
            if (exact)
                return checkTemplateWaker.HasMatch && code.MatchTemplate(MatchResult);
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
