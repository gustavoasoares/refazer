using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using IronPython.Compiler.Ast;
using IronPython.Runtime;
using ConstantExpression = IronPython.Compiler.Ast.ConstantExpression;
using Expression = System.Linq.Expressions.Expression;

namespace Tutor
{
    
    public class Rewriter : ExpressionVisitor
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

        protected override Expression VisitExtension(Expression exp)
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

        private Node VisitStatement(SuiteStatement node)
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
                        var insert = (Insert) edit;
                        var suite = newCode as SuiteStatement;
                        var newList = new List<Statement>(suite.Statements);
                        newList.Insert(insert.Index, (Statement) insert.ModifiedNode.InnerNode);
                        newCode = new SuiteStatement(newList.ToArray());
                    }
                    else
                    {
                        var suite = newCode as SuiteStatement;
                        var newList = new List<Statement>(suite.Statements);
                        newList = newList.Where(e => !edit.ModifiedNode.Match(e).Item1).ToList();
                        newCode = new SuiteStatement(newList.ToArray());
                    }
                }
            }
            if (changed)
            {
                var suite = newCode as SuiteStatement;
                var newList = new List<Statement>(suite.Statements);
                newList = newList.Select(VisitStatement).ToList();
                return new SuiteStatement(newList.ToArray());
            }

            return new SuiteStatement(node.Statements.Select(VisitStatement).ToArray());
        }

        private Node VisitStatement(ExpressionStatement node)
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

        private Node VisitExpression(IronPython.Compiler.Ast.BinaryExpression node)
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

        private Node VisitExpression(NameExpression node)
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

        private Node VisitExpression(IronPython.Compiler.Ast.ConstantExpression node)
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

        private Node VisitExpression(ParenthesisExpression exp)
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

        private Node VisitExpression(CallExpression exp)
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

        private Arg VisitArg(Arg arg)
        {
            var newCode = arg;
            foreach (var edit in _edits)
            {
                if (edit.CanApply(arg))
                {
                    if (edit is Update)
                    {
                        newCode = (Arg)edit.ModifiedNode.InnerNode;
                    }
                    else if (edit is Insert)
                    {
                        newCode = new Arg(arg.Name, (IronPython.Compiler.Ast.Expression) edit.ModifiedNode.InnerNode);
                    }
                    else
                    {
                        if (edit.ModifiedNode.Match(newCode.Expression).Item1)
                        {
                            var deleted = new ConstantExpression("tutor:deleted");
                            newCode = new Arg(arg.Name, deleted);
                        }
                        else
                        {
                            throw new Exception("Deleted expression not found");
                        }
                    }
                }
            }
            return new Arg(newCode.Name, VisitExpression(newCode.Expression));
        }

        private Node VisitExpression(TupleExpression exp)
        {
            var newCode = exp;
            foreach (var edit in _edits)
            {
                if (edit.CanApply(exp))
                {
                    if (edit is Update)
                    {
                        newCode = (TupleExpression) edit.ModifiedNode.InnerNode;
                    }
                    else if (edit is Insert)
                    {
                        var insert = (Insert) edit;
                        var items = new List<IronPython.Compiler.Ast.Expression>(newCode.Items);
                        if (insert.Index > items.Count)
                            throw new Exception("Not possible to insert expresstion at this possition");
                        items.Insert(insert.Index,(IronPython.Compiler.Ast.Expression) insert.ModifiedNode.InnerNode);
                        newCode = new TupleExpression(newCode.IsExpandable, items.ToArray());
                    }
                    else
                    {
                        if (!(newCode.Items.Any(e => !(edit.ModifiedNode.Match(e).Item1))))
                            throw new Exception("deleted method not found");
                        var items=  newCode.Items.Where(e => !(edit.ModifiedNode.Match(e).Item1));
                        newCode = new TupleExpression(newCode.IsExpandable, items.ToArray());
                    }
                }
            }
            var newExpressions = newCode.Items.Select(VisitExpression);
            return new TupleExpression(newCode.IsExpandable, newExpressions.ToArray());
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

        private Node VisitStatement(AugmentedAssignStatement stmt)
        {
            AugmentedAssignStatement newCode = stmt;
            foreach (var edit in _edits)
            {
                if (edit.CanApply(stmt))
                {
                    if (edit is Update)
                    {
                        newCode = (AugmentedAssignStatement) edit.ModifiedNode.InnerNode;
                    }
                    else if (edit is Insert)
                    {
                        var insert = (Insert) edit;
                        if (insert.Index == 0)
                        {
                            var deleted = newCode.Left as IronPython.Compiler.Ast.ConstantExpression;
                            if (deleted != null && deleted.Value.Equals("Tutor:deletedNode"))
                            {
                                newCode = new AugmentedAssignStatement(newCode.Operator,
                                    (IronPython.Compiler.Ast.Expression) insert.ModifiedNode.InnerNode,newCode.Right);
                            }
                            else
                            {
                                throw new Exception("Not possible to insert a node. There is already a node at this position");
                            }
                        }
                        else if (insert.Index == 1)
                        {
                            var deleted = newCode.Right as IronPython.Compiler.Ast.ConstantExpression;
                            if (deleted != null && deleted.Value.Equals("Tutor:deletedNode"))
                            {
                                newCode = new AugmentedAssignStatement(newCode.Operator,
                                    newCode.Left, (IronPython.Compiler.Ast.Expression)insert.ModifiedNode.InnerNode);
                            }
                            else
                            {
                                throw new Exception("Not possible to insert a node. There is already a node at this position");
                            }
                        } else
                        {
                            throw new Exception("Index out of bound");
                        }
                    }
                    else
                    {
                        var deleted = new IronPython.Compiler.Ast.ConstantExpression("Tutor:deletedNode");
                        if ((edit.ModifiedNode.Match(newCode.Left).Item1))
                        {
                            newCode = new AugmentedAssignStatement(newCode.Operator, deleted, newCode.Right);
                        }
                        else
                        {
                            if ((!edit.ModifiedNode.Match(newCode.Right).Item1))
                                throw new Exception("node not found to perform the delete");
                            newCode = new AugmentedAssignStatement(newCode.Operator, newCode.Left, deleted);
                        }
                    }
                }
            }
            return new AugmentedAssignStatement(newCode.Operator, VisitExpression(newCode.Left), VisitExpression(newCode.Right));
        }

        private Node VisitStatement(WhileStatement stmt)
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

        private Node VisitStatement(AssignmentStatement stmt)
        {
            var newCode = stmt;
            foreach (var edit in _edits)
            {
                if (edit.CanApply(stmt))
                {
                    if (edit is Update)
                    {
                        newCode = (AssignmentStatement) edit.ModifiedNode.InnerNode;
                    }
                    else if (edit is Insert)
                    {
                        var insert = (Insert) edit;
                        if (insert.Index == 1)
                        {
                            if (newCode.Left.Count == 1)
                            {
                                var deleted = newCode.Right as IronPython.Compiler.Ast.ConstantExpression;
                                if (deleted != null && deleted.Value.Equals("Tutor:deletedNode"))
                                {
                                    newCode = new AssignmentStatement(newCode.Left.ToArray(), (IronPython.Compiler.Ast.Expression) edit.ModifiedNode.InnerNode);
                                }
                                else
                                {
                                    throw new Exception("Not possible to insert a node. There is already a node at this position");
                                }
                            }
                            else
                            {
                                throw new NotImplementedException();
                            }
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                    }
                    else
                    {
                        var node = (AssignmentStatement) stmt;
                        var leftDelete = node.Left.Any(e => edit.ModifiedNode.Match(e).Item1);
                        if (leftDelete)
                        {
                            var newLeft = node.Left.Where(e => !(edit.ModifiedNode.Match(e).Item1));
                            newCode = new AssignmentStatement(newLeft.ToArray(),node.Right);
                        }
                        else
                        {
                            if ((!edit.ModifiedNode.Match(node.Right).Item1))
                                throw new Exception("node not found to perform the delete");
                            var deleted = new IronPython.Compiler.Ast.ConstantExpression("Tutor:deletedNode");
                            newCode = new AssignmentStatement(node.Left.ToArray(), deleted);

                        }
                    }
                }
            }
            var expressions = newCode.Left.Select(VisitExpression).ToList();
            return new AssignmentStatement(expressions.ToArray(), VisitExpression(newCode.Right));
        }

        private Node VisitStatement(ReturnStatement stmt)
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

        private Node VisitStatement(FunctionDefinition stmt)
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

        private Node VisitExpression(Parameter parameter)
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

        private Node VisitStatement(IfStatement stmt)
        {
            var newCode = stmt;
            foreach (var edit in _edits)
            {
                if (edit.CanApply(stmt))
                {
                    if (edit is Update)
                    {
                        newCode = (IfStatement) edit.ModifiedNode.InnerNode;
                    }
                    else if (edit is Insert)
                    {
                        var insert = (Insert)edit;
                        if (insert.ModifiedNode.InnerNode is IfStatementTest)
                        {
                            var newIfs = new List<IfStatementTest>(newCode.Tests);
                            if (insert.Index <= newIfs.Count)
                            {
                                newIfs.Insert(insert.Index,(IfStatementTest) insert.ModifiedNode.InnerNode);
                            }
                            else
                            {
                                throw new Exception("Cannot insert test in this position");
                            }
                            newCode = new IfStatement(newIfs.ToArray(), newCode.ElseStatement);
                        }
                        else
                        {
                            if (insert.Index != newCode.Tests.Count || newCode.ElseStatement != null)
                            {
                                throw new Exception("Cannot add else statement");
                            }
                            newCode = new IfStatement(newCode.Tests.ToArray(), (Statement) insert.ModifiedNode.InnerNode);
                        }
                    }
                    else
                    {
                        if (edit.ModifiedNode.InnerNode is IfStatementTest)
                        {
                            var newList = new List<IfStatementTest>(newCode.Tests);
                            newList = newList.Where(e => !edit.ModifiedNode.Match(e).Item1).ToList();
                            var else_ = (newCode.ElseStatement != null &&
                                         edit.ModifiedNode.Match(newCode.ElseStatement).Item1)
                                ? (Statement) edit.ModifiedNode.InnerNode
                                : newCode.ElseStatement;
                            newCode = new IfStatement(newList.ToArray(), else_);
                        }
                        else
                        {
                            if (newCode.ElseStatement != null && edit.ModifiedNode.Match(newCode.ElseStatement).Item1)
                            {
                                newCode = new IfStatement(newCode.Tests.ToArray(), null);
                            }
                            else
                            {
                                throw new Exception("Not possible to delete statement");
                            }
                        }
                    }
                }
            }

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
            var newAst = Visit(code);
            return newAst as Node;
        }
    }
}
