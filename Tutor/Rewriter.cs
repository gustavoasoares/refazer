using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using IronPython.Compiler.Ast;
using IronPython.Runtime;
using BinaryExpression = System.Linq.Expressions.BinaryExpression;
using Expression = System.Linq.Expressions.Expression;

namespace Tutor
{
    public class InsertRewriter : ExpressionVisitor
    {
        private readonly Insert _edit;

        public InsertRewriter(Insert edit)
        {
            _edit = edit;
        }

        protected override Expression VisitExtension(Expression exp)
        {
            var node = exp as Node;
            if (node == null)
                return node;
            if (node.Equals(_edit.Context))
            {
                return _edit.NewNode.InnerNode;
            }

            switch (node.NodeName)
            {
                case "PythonAst":
                    var ast = node as PythonAst;
                    var newAst = new PythonAst(VisitStatement(ast.Body) as Statement, ast.Module, ModuleOptions.AbsoluteImports, false);
                    return newAst;
            }
            return node;
        }

        private Node VisitStatement(SuiteStatement node)
        {
            if (_edit.CanApply(node)) return _edit.Apply(node);

            return new SuiteStatement(node.Statements.Select(VisitStatement).ToArray());
        }

        private Node VisitStatement(ExpressionStatement node)
        {
            if (_edit.CanApply(node)) return _edit.Apply(node);

            return new ExpressionStatement(VisitExpression(node.Expression));
        }

        private Node VisitExpression(IronPython.Compiler.Ast.BinaryExpression node)
        {
            if (_edit.CanApply(node)) return _edit.Apply(node);

            var left = VisitExpression(node.Left);
            var right = VisitExpression(node.Right);
            return new IronPython.Compiler.Ast.BinaryExpression(node.Operator, left, right);
        }

        private Node VisitExpression(NameExpression node)
        {
            if (_edit.CanApply(node)) return _edit.Apply(node);

            return new NameExpression(node.Name);
        }

        private Node VisitExpression(IronPython.Compiler.Ast.ConstantExpression node)
        {
            if (_edit.CanApply(node)) return _edit.Apply(node);
            return node;
        }

        private IronPython.Compiler.Ast.Expression VisitExpression(IronPython.Compiler.Ast.Expression expression)
        {
            if (expression is IronPython.Compiler.Ast.BinaryExpression)
                return (IronPython.Compiler.Ast.Expression)VisitExpression(expression as IronPython.Compiler.Ast.BinaryExpression);
            if (expression is NameExpression)
                return (IronPython.Compiler.Ast.Expression)VisitExpression(expression as NameExpression);
            if (expression is IronPython.Compiler.Ast.ConstantExpression)
                return (IronPython.Compiler.Ast.Expression)VisitExpression(expression as IronPython.Compiler.Ast.ConstantExpression);
            if (expression is TupleExpression)
                return (IronPython.Compiler.Ast.Expression)VisitExpression(expression as TupleExpression);
            if (expression is CallExpression)
                return (IronPython.Compiler.Ast.Expression)VisitExpression(expression as CallExpression);
            if (expression is ParenthesisExpression)
                return (IronPython.Compiler.Ast.Expression)VisitExpression(expression as ParenthesisExpression);
            if (expression is IronPython.Compiler.Ast.IndexExpression)
                return (IronPython.Compiler.Ast.Expression)VisitExpression(expression as IronPython.Compiler.Ast.IndexExpression);
            throw new Exception("Transformation not implemented yet: " + expression.NodeName);

        }

        private Node VisitExpression(IronPython.Compiler.Ast.IndexExpression exp)
        {
            if (_edit.CanApply(exp)) return _edit.Apply(exp);

            return new IronPython.Compiler.Ast.IndexExpression(VisitExpression(exp.Target),
                VisitExpression(exp.Index));
        }

        private Node VisitExpression(ParenthesisExpression exp)
        {
            if (_edit.CanApply(exp)) return _edit.Apply(exp);

            return new ParenthesisExpression(VisitExpression(exp.Expression));
        }

        private Node VisitExpression(CallExpression exp)
        {
            if (_edit.CanApply(exp))
            {
                switch (_edit.Index)
                {
                    case 1:
                        var args = new List<Arg>();
                        args.Add((Arg)_edit.NewNode.InnerNode);
                        args.AddRange(exp.Args);
                        return new CallExpression(exp.Target, args.ToArray());
                    default:
                        throw new NotImplementedException();
                }
            }

            var newArgs = exp.Args.Select(VisitArg);
            return new CallExpression(VisitExpression(exp.Target), newArgs.ToArray());
        }

        private Arg VisitArg(Arg arg)
        {
            if (_edit.CanApply(arg))
            {
                return new Arg(arg.Name, (IronPython.Compiler.Ast.Expression) _edit.NewNode.InnerNode);
            }
            return new Arg(arg.Name, VisitExpression(arg.Expression));
        }

        private Node VisitExpression(TupleExpression exp)
        {
            if (_edit.CanApply(exp)) return _edit.Apply(exp);
            var newExpressions = exp.Items.Select(VisitExpression);
            return new TupleExpression(exp.IsExpandable, newExpressions.ToArray());
        }

        private Statement VisitStatement(Statement stmt)
        {
            if (stmt == null)
                return stmt;
            if (stmt is ExpressionStatement)
                return (Statement)VisitStatement(stmt as ExpressionStatement);
            if (stmt is SuiteStatement)
                return (Statement)VisitStatement(stmt as SuiteStatement);
            if (stmt is IfStatement)
                return (Statement)VisitStatement(stmt as IfStatement);
            if (stmt is FunctionDefinition)
                return (Statement)VisitStatement(stmt as FunctionDefinition);
            if (stmt is ReturnStatement)
                return (Statement)VisitStatement(stmt as ReturnStatement);
            if (stmt is AssignmentStatement)
                return (Statement)VisitStatement(stmt as AssignmentStatement);
            if (stmt is WhileStatement)
                return (Statement)VisitStatement(stmt as WhileStatement);
            if (stmt is AugmentedAssignStatement)
                return (Statement)VisitStatement(stmt as AugmentedAssignStatement);
            if (stmt is ForStatement)
                return (Statement)VisitStatement(stmt as ForStatement);
            throw new Exception("Not implemented yet!");
        }

        private Node VisitStatement(ForStatement stmt)
        {
            if (_edit.CanApply(stmt)) return _edit.Apply(stmt);
            return new ForStatement(VisitExpression(stmt.Left), VisitExpression(stmt.List), VisitStatement(stmt.Body),
                VisitStatement(stmt.Else));
        }

        private Node VisitStatement(AugmentedAssignStatement stmt)
        {
            if (_edit.CanApply(stmt)) return _edit.Apply(stmt);
            return new AugmentedAssignStatement(stmt.Operator, VisitExpression(stmt.Left), VisitExpression(stmt.Right));
        }

        private Node VisitStatement(WhileStatement stmt)
        {
            if (_edit.CanApply(stmt))
            {
                switch (_edit.Index)
                {
                    case 1:
                        return new WhileStatement(stmt.Test, (Statement) _edit.NewNode.InnerNode, stmt.ElseStatement);
                    default:
                        throw new NotImplementedException();

                }
            }

            return new WhileStatement(VisitExpression(stmt.Test), VisitStatement(stmt.Body), VisitStatement(stmt.ElseStatement));
        }

        private Node VisitStatement(AssignmentStatement stmt)
        {
            if (_edit.CanApply(stmt)) return _edit.Apply(stmt);

            var expressions = stmt.Left.Select(VisitExpression).ToList();
            return new AssignmentStatement(expressions.ToArray(), VisitExpression(stmt.Right));
        }

        private Node VisitStatement(ReturnStatement stmt)
        {
            if (_edit.CanApply(stmt))
            {
                return new ReturnStatement((IronPython.Compiler.Ast.Expression)_edit.NewNode.InnerNode);
            }
            return new ReturnStatement(VisitExpression(stmt.Expression));
        }

        private Node VisitStatement(FunctionDefinition stmt)
        {
            if (_edit.CanApply(stmt)) return _edit.Apply(stmt);
            var parameters = new List<Parameter>();
            foreach (var parameter in stmt.Parameters)
            {
                parameters.Add((Parameter)VisitExpression(parameter));
            }
            var def = new FunctionDefinition(stmt.Name, parameters.ToArray(), VisitStatement(stmt.Body));
            return def;
        }

        private Node VisitExpression(Parameter parameter)
        {
            if (_edit.CanApply(parameter)) return _edit.Apply(parameter);

            var newParam = new Parameter(parameter.Name);
            if (parameter.DefaultValue != null)
                newParam.DefaultValue = VisitExpression(parameter.DefaultValue);
            return newParam;
        }

        private Node VisitStatement(IfStatement stmt)
        {
            if (_edit.CanApply(stmt)) return _edit.Apply(stmt);

            var newTests = new List<IfStatementTest>();
            foreach (var ifStatementTest in stmt.Tests)
            {
                var newTest = new IfStatementTest(VisitExpression(ifStatementTest.Test),
                    VisitStatement(ifStatementTest.Body));
                newTests.Add(newTest);
            }
            return new IfStatement(newTests.ToArray(), VisitStatement(stmt.ElseStatement));
        }

        public Node Rewrite(Node code)
        {
            var newAst = Visit(code);
            return newAst as Node;
        }
    }
    public class Rewriter : ExpressionVisitor
    {
        private readonly Edit _edit; 

        public Rewriter(Edit edit)
        {
            _edit = edit;
        }

        protected override Expression VisitExtension(Expression exp)
        {
            var node = exp as Node;
            if (node == null)
                return node;
            if (node.Equals(_edit.Context))
            {
                return _edit.NewNode.InnerNode;
            }

            switch (node.NodeName)
            {
                case "PythonAst":
                    var ast = node as PythonAst;
                    var newAst = new PythonAst(VisitStatement(ast.Body) as Statement, ast.Module, ModuleOptions.AbsoluteImports, false);
                    return newAst;
            }
            return node;
        }

        private Node VisitStatement(SuiteStatement node)
        {
            if (_edit.CanApply(node)) return _edit.Apply(node);

            return new SuiteStatement(node.Statements.Select(VisitStatement).ToArray());
        }

        private Node VisitStatement(ExpressionStatement node)
        {
            if (_edit.CanApply(node)) return _edit.Apply(node);

            return new ExpressionStatement(VisitExpression(node.Expression));
        }

        private Node VisitExpression(IronPython.Compiler.Ast.BinaryExpression node)
        {
            if (_edit.CanApply(node)) return _edit.Apply(node);

            var left = VisitExpression(node.Left);
            var right = VisitExpression(node.Right);
            return new IronPython.Compiler.Ast.BinaryExpression(node.Operator, left, right);
        }

        private Node VisitExpression(NameExpression node)
        {
            if (_edit.CanApply(node)) return _edit.Apply(node);

            return new NameExpression(node.Name);
        }

        private Node VisitExpression(IronPython.Compiler.Ast.ConstantExpression node)
        {
            if (_edit.CanApply(node)) return _edit.Apply(node);
            return node;
        }

        private IronPython.Compiler.Ast.Expression VisitExpression(IronPython.Compiler.Ast.Expression expression)
        {
            if (expression is IronPython.Compiler.Ast.BinaryExpression)
                return  (IronPython.Compiler.Ast.Expression) VisitExpression(expression as IronPython.Compiler.Ast.BinaryExpression);
            if (expression is NameExpression)
                return (IronPython.Compiler.Ast.Expression) VisitExpression(expression as NameExpression);
            if (expression is IronPython.Compiler.Ast.ConstantExpression)
                return (IronPython.Compiler.Ast.Expression) VisitExpression(expression as IronPython.Compiler.Ast.ConstantExpression);
            if (expression is TupleExpression)
                return (IronPython.Compiler.Ast.Expression) VisitExpression(expression as TupleExpression);
            if (expression is CallExpression)
                return (IronPython.Compiler.Ast.Expression) VisitExpression(expression as CallExpression);
            if (expression is ParenthesisExpression)
                return (IronPython.Compiler.Ast.Expression) VisitExpression(expression as ParenthesisExpression);
            if (expression is IronPython.Compiler.Ast.IndexExpression)
                return (IronPython.Compiler.Ast.Expression) VisitExpression(expression as IronPython.Compiler.Ast.IndexExpression);
            throw new Exception("Transformation not implemented yet: " + expression.NodeName);

        }

        private Node VisitExpression(IronPython.Compiler.Ast.IndexExpression exp)
        {
            if (_edit.CanApply(exp)) return _edit.Apply(exp);

            return new IronPython.Compiler.Ast.IndexExpression(VisitExpression(exp.Target),
                VisitExpression(exp.Index));
        }

        private Node VisitExpression(ParenthesisExpression exp)
        {
            if (_edit.CanApply(exp)) return _edit.Apply(exp);

            return new ParenthesisExpression(VisitExpression(exp.Expression));
        }

        private Node VisitExpression(CallExpression exp)
        {
            if (_edit.CanApply(exp)) return _edit.Apply(exp);

            var newArgs = exp.Args.Select(VisitArg);
            return new CallExpression(VisitExpression(exp.Target), newArgs.ToArray());
        }

        private Arg VisitArg(Arg arg)
        {
            if (_edit.CanApply(arg)) return _edit.Apply(arg) as Arg;
            return new Arg(arg.Name, VisitExpression(arg.Expression));
        }

        private Node VisitExpression(TupleExpression exp)
        {
            if (_edit.CanApply(exp)) return _edit.Apply(exp);
            var newExpressions = exp.Items.Select(VisitExpression);
            return new TupleExpression(exp.IsExpandable, newExpressions.ToArray());
        }

        private Statement VisitStatement(Statement stmt)
        {
            if (stmt == null)
                return stmt;
            if (stmt is ExpressionStatement)
                return (Statement) VisitStatement(stmt as ExpressionStatement);
            if (stmt is SuiteStatement)
                return (Statement) VisitStatement(stmt as SuiteStatement);
            if (stmt is IfStatement)
                return (Statement)VisitStatement(stmt as IfStatement);
            if (stmt is FunctionDefinition)
                return (Statement) VisitStatement(stmt as FunctionDefinition);
            if (stmt is ReturnStatement)
                return (Statement) VisitStatement(stmt as ReturnStatement);
            if (stmt is AssignmentStatement)
                return (Statement) VisitStatement(stmt as AssignmentStatement);
            if (stmt is WhileStatement)
                return (Statement) VisitStatement(stmt as WhileStatement);
            if (stmt is AugmentedAssignStatement)
                return (Statement) VisitStatement(stmt as AugmentedAssignStatement);
            if (stmt is ForStatement)
                return (Statement) VisitStatement(stmt as ForStatement);
            throw new Exception("Not implemented yet!");
        }

        private Node VisitStatement(ForStatement stmt)
        {
            if (_edit.CanApply(stmt)) return _edit.Apply(stmt);
            return new ForStatement(VisitExpression(stmt.Left), VisitExpression(stmt.List), VisitStatement(stmt.Body),
                VisitStatement(stmt.Else));
        }

        private Node VisitStatement(AugmentedAssignStatement stmt)
        {
            if (_edit.CanApply(stmt)) return _edit.Apply(stmt);
            return new AugmentedAssignStatement(stmt.Operator, VisitExpression(stmt.Left), VisitExpression(stmt.Right));
        }

        private Node VisitStatement(WhileStatement stmt)
        {
            if (_edit.CanApply(stmt)) return _edit.Apply(stmt);

            return new WhileStatement(VisitExpression(stmt.Test), VisitStatement(stmt.Body), VisitStatement(stmt.ElseStatement));
        }

        private Node VisitStatement(AssignmentStatement stmt)
        {
            if (_edit.CanApply(stmt)) return _edit.Apply(stmt);

            var expressions = stmt.Left.Select(VisitExpression).ToList();
            return new AssignmentStatement(expressions.ToArray(), VisitExpression(stmt.Right));
        }

        private Node VisitStatement(ReturnStatement stmt)
        {
            if (_edit.CanApply(stmt)) return _edit.Apply(stmt);
            return new ReturnStatement(VisitExpression(stmt.Expression));
        }

        private Node VisitStatement(FunctionDefinition stmt)
        {
            if (_edit.CanApply(stmt)) return _edit.Apply(stmt);
            var parameters = new List<Parameter>();
            foreach (var parameter in stmt.Parameters)
            {
                parameters.Add((Parameter) VisitExpression(parameter));
            }
            var def = new FunctionDefinition(stmt.Name, parameters.ToArray(), VisitStatement(stmt.Body));
            return def;
        }

        private Node VisitExpression(Parameter parameter)
        {
            if (_edit.CanApply(parameter)) return _edit.Apply(parameter);

            var newParam = new Parameter(parameter.Name);
            if (parameter.DefaultValue != null)
                newParam.DefaultValue = VisitExpression(parameter.DefaultValue);
            return newParam;
        }

        private Node VisitStatement(IfStatement stmt)
        {
            if (_edit.CanApply(stmt)) return _edit.Apply(stmt);

            var newTests = new List<IfStatementTest>();
            foreach (var ifStatementTest in stmt.Tests)
            {
                var newTest = new IfStatementTest(VisitExpression(ifStatementTest.Test),
                    VisitStatement(ifStatementTest.Body));
                newTests.Add(newTest);
            }
            return new IfStatement(newTests.ToArray(), VisitStatement(stmt.ElseStatement));
        }

        public Node Rewrite(Node code)
        {
            var newAst = Visit(code);
            return newAst as Node;
        }
    }
}
