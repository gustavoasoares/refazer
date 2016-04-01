using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using IronPython.Compiler.Ast;
using BinaryExpression = IronPython.Compiler.Ast.BinaryExpression;
using ConstantExpression = System.Linq.Expressions.ConstantExpression;
using Expression = System.Linq.Expressions.Expression;

namespace Tutor
{
    public class Edits
    {

        public static Expression Update(PythonAst code, MatchResult context, Node newNode)
        {
            var rewriter = new UpdateRewriter(context.Bindings.First(), newNode);
            return rewriter.Update(code); ;
        }

        class UpdateRewriter : ExpressionVisitor
        {
            private readonly Node _oldNode;
            private readonly Node _newNode;

            public UpdateRewriter(Node oldNode, Node newNode)
            {
                this._oldNode = oldNode;
                this._newNode = newNode;
            }

            protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
            {
                return base.VisitMemberAssignment(node);
            }

            protected override Expression VisitExtension(Expression exp)
            {
                var node = exp as Node;
                if (node == null)
                    return node;
                if (node.Equals(_oldNode))
                {
                    return _newNode;
                }

                switch (node.NodeName)
                {
                    case "PythonAst":
                        var ast = node as PythonAst;
                        return VisitStatement(ast.Body);
                }
                return node;
            }

            private Statement VisitStatement(SuiteStatement node)
            {
                return node.Equals(_oldNode) ? _newNode as Statement: 
                    new SuiteStatement(node.Statements.Select(VisitStatement).ToArray());
            }

            private Statement VisitStatement(ExpressionStatement node)
            {
                if (node.Equals(_oldNode))
                {
                    return _newNode as ExpressionStatement;
                }
                return new ExpressionStatement(VisitExpression(node.Expression));
            }

            private IronPython.Compiler.Ast.Expression VisitExpression(BinaryExpression node)
            {
                if (node.Equals(_oldNode))
                {
                    return _newNode as BinaryExpression;
                }
                return new BinaryExpression(node.Operator, VisitExpression(node.Left), VisitExpression(node.Right));
            }

            private IronPython.Compiler.Ast.Expression VisitExpression(NameExpression node)
            {
                if (node.Equals(_oldNode))
                {
                    return _newNode as NameExpression;
                }
                else
                {
                    return node;
                }
            }

            private IronPython.Compiler.Ast.Expression VisitExpression(IronPython.Compiler.Ast.ConstantExpression node)
            {
                if (node.Equals(_oldNode))
                {
                    return _newNode as IronPython.Compiler.Ast.ConstantExpression;
                }
                else
                {
                    return node;
                }
            }

            private IronPython.Compiler.Ast.Expression VisitExpression(IronPython.Compiler.Ast.Expression expression)
            {
                if (expression is BinaryExpression)
                    return VisitExpression(expression as BinaryExpression);
                if (expression is NameExpression)
                    return VisitExpression(expression as NameExpression);
                if (expression is IronPython.Compiler.Ast.ConstantExpression)
                    return VisitExpression(expression as IronPython.Compiler.Ast.ConstantExpression);
                ; return expression;
            }

            private Statement VisitStatement(Statement stmt)
            {
                if (stmt is ExpressionStatement)
                    return VisitStatement(stmt as ExpressionStatement);
                if (stmt is SuiteStatement)
                    return VisitStatement(stmt as SuiteStatement);
                ; return stmt;
            }

            private Expression UpdateNodeIfMatch(Expression node)
            {
                var exp = (node.Equals(_oldNode)) ? _newNode : node;
                return exp;
            }

            public Expression Update(PythonAst code)
            {
                var newAst = Visit(code);
                return newAst;
            }
        }
    }

}
