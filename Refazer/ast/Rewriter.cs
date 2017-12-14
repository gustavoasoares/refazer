using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using IronPython.Compiler.Ast;
using IronPython.Runtime;
using ConstantExpression = IronPython.Compiler.Ast.ConstantExpression;
using Expression = System.Linq.Expressions.Expression;
using MemberExpression = IronPython.Compiler.Ast.MemberExpression;

namespace Tutor
{
    
    public class Rewriter
    {
        private List<Edit> _edits;

        public Rewriter(Edit edit)
        {
            _edits = new List<Edit>() {edit};
        }

        public Rewriter(List<Edit> edits)
        {
            _edits = edits;
        }

        public Node VisitStatement(PythonAst exp)
        {
            var node = exp as Node;
            if (node == null)
                return node;
            if (node.Equals(_edits.First().Context))
            {
                return _edits.First().ModifiedNode.InnerNode;
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

        public Node VisitStatement(SuiteStatement node)
        {
            return new SuiteStatement(node.Statements.Select(VisitStatement).ToArray());
        }

        public Node VisitStatement(ExpressionStatement node)
        {
            var changed = false;
            Node newCode = node;
            foreach (var edit in _edits)
            {
                if (edit.CanApply(node))
                {
                    changed = true;
                    if (edit is Update)
                    {
                        newCode = edit.ModifiedNode.InnerNode;
                    }
                    else if (edit is Insert)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }
            if (changed)
                return newCode;

            return new ExpressionStatement(VisitExpression(node.Expression));
        }

        public Node VisitExpression(IronPython.Compiler.Ast.BinaryExpression node)
        {
            var changed = false;
            Node newCode = node;

            foreach (var edit in _edits)
            {
                if (edit.CanApply(node))
                {
                    changed = true;
                    if (edit is Update)
                    {
                        newCode = edit.ModifiedNode.InnerNode;
                    } else if (edit is Delete)
                    {
                        var deleted = new IronPython.Compiler.Ast.ConstantExpression("Tutor:deletedNode");
                        if (edit.ModifiedNode.InnerNode.Equals(node.Left))
                        {
                            var binary = newCode as IronPython.Compiler.Ast.BinaryExpression;
                            newCode = new IronPython.Compiler.Ast.BinaryExpression(binary.Operator, VisitExpression(binary.Right), deleted);
                        }
                        else
                        {
                            var binary = newCode as IronPython.Compiler.Ast.BinaryExpression;
                            newCode = new IronPython.Compiler.Ast.BinaryExpression(binary.Operator, VisitExpression(binary.Left), deleted);
                        }
                    }
                    else
                    {
                        switch (((Insert) edit).Index)
                        {
                            case 0:
                                var binary = newCode as IronPython.Compiler.Ast.BinaryExpression;
                                newCode = new IronPython.Compiler.Ast.BinaryExpression(binary.Operator, (IronPython.Compiler.Ast.Expression) edit.ModifiedNode.InnerNode, VisitExpression(binary.Right));
                                break;
                            case 1:
                                binary = newCode as IronPython.Compiler.Ast.BinaryExpression;
                                newCode = new IronPython.Compiler.Ast.BinaryExpression(binary.Operator, VisitExpression(binary.Left), (IronPython.Compiler.Ast.Expression)edit.ModifiedNode.InnerNode);
                                break;
                        }
                    }
                }
            }
            if (changed)
                return newCode;

            var left = VisitExpression(node.Left);
            var right = VisitExpression(node.Right);
            return new IronPython.Compiler.Ast.BinaryExpression(node.Operator, left, right);
        }

        public Node VisitExpression(NameExpression node)
        {
            var changed = false;
            Node newCode = node;
            foreach (var edit in _edits)
            {
                if (edit.CanApply(node))
                {
                    changed = true;
                    if (edit is Update)
                    {
                        newCode = edit.ModifiedNode.InnerNode;
                    }
                    else if (edit is Insert)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }
            if (changed)
                return newCode;

            return new NameExpression(node.Name);
        }

        public Node VisitExpression(IronPython.Compiler.Ast.ConstantExpression node)
        {
            var changed = false;
            Node newCode = node;
            foreach (var edit in _edits)
            {
                if (edit.CanApply(node))
                {
                    changed = true;
                    if (edit is Update)
                    {
                        newCode = edit.ModifiedNode.InnerNode;
                    }
                    else if (edit is Insert)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }
            if (changed)
                return newCode;
            return node;
        }

        public IronPython.Compiler.Ast.Expression VisitExpression(IronPython.Compiler.Ast.Expression expression)
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
            if (expression is OrExpression)
                return (IronPython.Compiler.Ast.Expression)VisitExpression(expression as OrExpression);
            if (expression is MemberExpression)
                return (IronPython.Compiler.Ast.Expression)VisitExpression(expression as MemberExpression);
            if (expression is IronPython.Compiler.Ast.LambdaExpression)
                return (IronPython.Compiler.Ast.Expression)VisitExpression(expression as IronPython.Compiler.Ast.LambdaExpression);
            throw new Exception("Transformation not implemented yet: " + expression.NodeName);

        }

        public Node VisitExpression(IronPython.Compiler.Ast.LambdaExpression exp)
        {
            var changed = false;
            var newCode = exp;
            foreach (var edit in _edits)
            {
                if (edit.CanApply(exp))
                {
                    changed = true;
                    if (edit is Update)
                    {
                        newCode = (IronPython.Compiler.Ast.LambdaExpression)edit.ModifiedNode.InnerNode;
                    }
                    else if (edit is Insert)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }
            if (changed)
                return newCode;

            return new IronPython.Compiler.Ast.LambdaExpression(newCode.Function);
        }

        public Node VisitExpression(MemberExpression exp)
        {
            var changed = false;
            var newCode = exp;
            foreach (var edit in _edits)
            {
                if (edit.CanApply(exp))
                {
                    changed = true;
                    if (edit is Update)
                    {
                        newCode = (MemberExpression) edit.ModifiedNode.InnerNode;
                    }
                    else if (edit is Insert)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }
            if (changed)
                return newCode;

            return new MemberExpression(VisitExpression(exp.Target), newCode.Name);
        }

        public Node VisitExpression(OrExpression exp)
        {
            var changed = false;
            Node newCode = exp;
            foreach (var edit in _edits)
            {
                if (edit.CanApply(exp))
                {
                    changed = true;
                    if (edit is Update)
                    {
                        newCode = edit.ModifiedNode.InnerNode;
                    }
                    else if (edit is Insert)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }
            if (changed)
                return newCode;

            return new OrExpression(VisitExpression(exp.Left),
                VisitExpression(exp.Right));
        }

        public Node VisitExpression(IronPython.Compiler.Ast.IndexExpression exp)
        {
            var changed = false;
            Node newCode = exp;
            foreach (var edit in _edits)
            {
                if (edit.CanApply(exp))
                {
                    changed = true;
                    if (edit is Update)
                    {
                        newCode = edit.ModifiedNode.InnerNode;
                    }
                    else if (edit is Insert)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }
            if (changed)
                return newCode;

            return new IronPython.Compiler.Ast.IndexExpression(VisitExpression(exp.Target),
                VisitExpression(exp.Index));
        }

        public Node VisitExpression(ParenthesisExpression exp)
        {
            var changed = false;
            Node newCode = exp;
            foreach (var edit in _edits)
            {
                if (edit.CanApply(exp))
                {
                    changed = true;
                    if (edit is Update)
                    {
                        newCode = edit.ModifiedNode.InnerNode;
                    }
                    else if (edit is Insert)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }
            if (changed)
                return newCode;

            return new ParenthesisExpression(VisitExpression(exp.Expression));
        }

        public Node VisitExpression(CallExpression exp)
        {
            var newCode = exp;
            foreach (var edit in _edits)
            {
                if (edit.CanApply(exp))
                {
                    if (edit is Update)
                    {
                        newCode = (CallExpression) edit.ModifiedNode.InnerNode;
                    }
                    else if (edit is Insert)
                    {
                        switch (((Insert)edit).Index)
                        {
                            case 1:
                                var args = new List<Arg>();
                                args.Add((Arg)edit.ModifiedNode.InnerNode);
                                args.AddRange(exp.Args);
                                newCode = new CallExpression(exp.Target, args.ToArray());
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }

            var newArgs = newCode.Args.Select(VisitArg);
            return new CallExpression(VisitExpression(exp.Target), newArgs.ToArray());
        }

        public Arg VisitArg(Arg arg)
        {
            var newCode = arg;
            return new Arg(newCode.Name, VisitExpression(newCode.Expression));
        }

        public Node VisitExpression(TupleExpression exp)
        {
            var newCode = exp;
            var newExpressions = newCode.Items.Select(VisitExpression);
            return new TupleExpression(newCode.IsExpandable, newExpressions.ToArray());
        }

        public Statement VisitStatement(Statement stmt)
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

        public Node VisitStatement(ForStatement stmt)
        {
            var changed = false;
            Node newCode = stmt;
            foreach (var edit in _edits)
            {
                if (edit.CanApply(stmt))
                {
                    changed = true;
                    if (edit is Update)
                    {
                        newCode = edit.ModifiedNode.InnerNode;
                    }
                    else if (edit is Insert)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }
            if (changed)
                return newCode;
            return new ForStatement(VisitExpression(stmt.Left), VisitExpression(stmt.List), VisitStatement(stmt.Body),
                VisitStatement(stmt.Else));
        }

        public Node VisitStatement(AugmentedAssignStatement stmt)
        {
            AugmentedAssignStatement newCode = stmt;
            return new AugmentedAssignStatement(newCode.Operator, VisitExpression(newCode.Left), VisitExpression(newCode.Right));
        }

        public Node VisitStatement(WhileStatement stmt)
        {
            var changed = false;
            Node newCode = stmt;
            foreach (var edit in _edits)
            {
                if (edit.CanApply(stmt))
                {
                    changed = true;
                    if (edit is Update)
                    {
                        newCode = edit.ModifiedNode.InnerNode;
                    }
                    else if (edit is Insert)
                    {
                        switch (((Insert)edit).Index)
                        {
                            case 1:
                                newCode = new WhileStatement(stmt.Test, (Statement)edit.ModifiedNode.InnerNode, stmt.ElseStatement);
                                break;
                            default:
                                throw new NotImplementedException();

                        }
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }
            if (changed)
                return newCode;

            return new WhileStatement(VisitExpression(stmt.Test), VisitStatement(stmt.Body), VisitStatement(stmt.ElseStatement));
        }

        public Node VisitStatement(AssignmentStatement stmt)
        {
            var newCode = stmt;
            var expressions = newCode.Left.Select(VisitExpression).ToList();
            return new AssignmentStatement(expressions.ToArray(), VisitExpression(newCode.Right));
        }

        public Node VisitStatement(ReturnStatement stmt)
        {
            var changed = false;
            var newCode = stmt;
            foreach (var edit in _edits)
            {
                if (edit.CanApply(stmt))
                {
                    changed = true;
                    if (edit is Update)
                    {
                        newCode = (ReturnStatement) edit.ModifiedNode.InnerNode;
                    } else if (edit is Insert)
                    {
                        newCode = new ReturnStatement((IronPython.Compiler.Ast.Expression) edit.ModifiedNode.InnerNode);
                    }
                    else
                    {
                        throw  new NotImplementedException();
                    }
                }
            }
            if (changed)
                return newCode;
            return new ReturnStatement(VisitExpression(stmt.Expression));
        }

        public Node VisitStatement(FunctionDefinition stmt)
        {
            var changed = false;
            Node newCode = stmt;
            foreach (var edit in _edits)
            {
                if (edit.CanApply(stmt))
                {
                    changed = true;
                    if (edit is Update)
                    {
                        newCode = edit.ModifiedNode.InnerNode;
                    }
                    else if (edit is Insert)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }
            if (changed)
                return newCode;
            var parameters = new List<Parameter>();
            foreach (var parameter in stmt.Parameters)
            {
                parameters.Add((Parameter) VisitExpression(parameter));
            }
            var def = new FunctionDefinition(stmt.Name, parameters.ToArray(), VisitStatement(stmt.Body));
            return def;
        }

        public Node VisitExpression(Parameter parameter)
        {
            var changed = false;
            Node newCode = parameter;
            foreach (var edit in _edits)
            {
                if (edit.CanApply(parameter))
                {
                    changed = true;
                    if (edit is Update)
                    {
                        newCode = edit.ModifiedNode.InnerNode;
                    }
                    else if (edit is Insert)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }
            if (changed)
                return newCode;

            var newParam = new Parameter(parameter.Name);
            if (parameter.DefaultValue != null)
                newParam.DefaultValue = VisitExpression(parameter.DefaultValue);
            return newParam;
        }

        public Node VisitStatement(IfStatement stmt)
        {
            var newCode = stmt;
            var newTests = new List<IfStatementTest>();
            foreach (var ifStatementTest in newCode.Tests)
            {
                var newTest = new IfStatementTest(VisitExpression(ifStatementTest.Test),
                    VisitStatement(ifStatementTest.Body));
                newTests.Add(newTest);
            }
            return new IfStatement(newTests.ToArray(), VisitStatement(newCode.ElseStatement));
        }

        public Node Rewrite(Node code)
        {
            var newAst = VisitStatement((PythonAst) code);
            return newAst;
        }
    }
}
