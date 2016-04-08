using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using IronPython.Compiler.Ast;
using IronPython.Runtime;
using BinaryExpression = IronPython.Compiler.Ast.BinaryExpression;
using ConstantExpression = System.Linq.Expressions.ConstantExpression;
using Expression = System.Linq.Expressions.Expression;

namespace Tutor
{
    public abstract class Operation
    {
        public PythonNode NewNode { get; }
        public PythonNode Target { get; }

        public Operation(PythonNode newNode, PythonNode target)
        {
            NewNode = newNode;
            Target = target;
        }

        public abstract Expression Run(PythonAst code, Node node); 
    }


    public class Insert : Operation
    {
        public Insert(PythonNode newNode, PythonNode target) : base(newNode, target)
        {
        }

        public override Expression Run(PythonAst code, Node node)
        {
            throw new NotImplementedException();
        }
    }

    public class Delete : Operation
    {
        public Delete(PythonNode newNode, PythonNode target) : base(newNode, target)
        {
        }

        public override Expression Run(PythonAst code, Node node)
        {
            throw new NotImplementedException();
        }
    }
    public class Update : Operation
    {
        public Update(PythonNode newNode, PythonNode target) : base(newNode, target)
        {
        }

        public override Expression Run(PythonAst code, Node node)
        {
            var rewriter = new UpdateRewriter(node, NewNode.InnerNode);
            return rewriter.Update(code);
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
                        var newAst = new PythonAst(VisitStatement(ast.Body), ast.Module, ModuleOptions.AbsoluteImports, false);
                        return newAst;
                }
                return node;
            }

            private Statement VisitStatement(SuiteStatement node)
            {
                return node.Equals(_oldNode) ? _newNode as Statement :
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
                var left = VisitExpression(node.Left);
                var right = VisitExpression(node.Right);
                return new BinaryExpression(node.Operator, left, right);
            }

            private IronPython.Compiler.Ast.Expression VisitExpression(NameExpression node)
            {
                if (node.Equals(_oldNode))
                {
                    return _newNode as NameExpression;
                }
                else
                {
                    return new NameExpression(node.Name);
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
                if (expression is TupleExpression)
                    return VisitExpression(expression as TupleExpression);
                if (expression is CallExpression)
                    return VisitExpression(expression as CallExpression);
                if (expression is ParenthesisExpression)
                    return VisitExpression(expression as ParenthesisExpression);
                if (expression is IronPython.Compiler.Ast.IndexExpression)
                    return VisitExpression(expression as IronPython.Compiler.Ast.IndexExpression);
                throw new Exception("Transformation not implemented yet: " + expression.NodeName);

            }

            private IronPython.Compiler.Ast.Expression VisitExpression(IronPython.Compiler.Ast.IndexExpression exp)
            {
                if (exp.Equals(_oldNode))
                {
                    return _newNode as IronPython.Compiler.Ast.IndexExpression;
                }
                return new IronPython.Compiler.Ast.IndexExpression(VisitExpression(exp.Target),
                    VisitExpression(exp.Index));
            }

            private IronPython.Compiler.Ast.Expression VisitExpression(ParenthesisExpression exp)
            {
                if (exp.Equals(_oldNode))
                {
                    return _newNode as ParenthesisExpression;
                }
                return new ParenthesisExpression(VisitExpression(exp.Expression));
            }

            private IronPython.Compiler.Ast.Expression VisitExpression(CallExpression exp)
            {
                if (exp.Equals(_oldNode))
                {
                    return _newNode as CallExpression;
                }
                var newArgs = exp.Args.Select(VisitArg);
                return new CallExpression(VisitExpression(exp.Target),newArgs.ToArray());
            }

            private Arg VisitArg(Arg arg)
            {
                if (arg.Equals(_oldNode))
                {
                    return _newNode as Arg;
                }
                return new Arg(arg.Name, VisitExpression(arg.Expression));
            }

            private IronPython.Compiler.Ast.Expression VisitExpression(TupleExpression exp)
            {
                if (exp.Equals(_oldNode))
                {
                    return _newNode as TupleExpression;
                }
                var newExpressions = exp.Items.Select(VisitExpression);
                return new TupleExpression(exp.IsExpandable,newExpressions.ToArray());
            }

            private Statement VisitStatement(Statement stmt)
            {
                if (stmt == null)
                    return stmt;
                if (stmt is ExpressionStatement)
                    return VisitStatement(stmt as ExpressionStatement);
                if (stmt is SuiteStatement)
                    return VisitStatement(stmt as SuiteStatement);
                if (stmt is IfStatement)
                    return VisitStatement(stmt as IfStatement);
                if (stmt is FunctionDefinition)
                    return VisitStatement(stmt as FunctionDefinition);
                if (stmt is ReturnStatement)
                    return VisitStatement(stmt as ReturnStatement);
                if (stmt is AssignmentStatement)
                    return VisitStatement(stmt as AssignmentStatement);
                if (stmt is WhileStatement)
                    return VisitStatement(stmt as WhileStatement);
                if (stmt is AugmentedAssignStatement)
                    return VisitStatement(stmt as AugmentedAssignStatement);
                if (stmt is ForStatement)
                    return VisitStatement(stmt as ForStatement);
                throw new Exception("Not implemented yet!");
            }

            private Statement VisitStatement(ForStatement stmt)
            {
                if (stmt.Equals(_oldNode))
                {
                    return _newNode as ForStatement;
                }
                return new ForStatement(VisitExpression(stmt.Left), VisitExpression(stmt.List), VisitStatement(stmt.Body),
                    VisitStatement(stmt.Else));
            }

            private Statement VisitStatement(AugmentedAssignStatement stmt)
            {
                if (stmt.Equals(_oldNode))
                {
                    return _newNode as AugmentedAssignStatement;
                }
                return new AugmentedAssignStatement(stmt.Operator,VisitExpression(stmt.Left),VisitExpression(stmt.Right));
            }

            private Statement VisitStatement(WhileStatement stmt)
            {
                if (stmt.Equals(_oldNode))
                {
                    return _newNode as WhileStatement;
                }

                return new WhileStatement(VisitExpression(stmt.Test), VisitStatement(stmt.Body),VisitStatement(stmt.ElseStatement));
            }

            private Statement VisitStatement(AssignmentStatement stmt)
            {
                if (stmt.Equals(_oldNode))
                {
                    return _newNode as AssignmentStatement;
                }

                var expressions = stmt.Left.Select(VisitExpression).ToList();
                return new AssignmentStatement(expressions.ToArray(), VisitExpression(stmt.Right));
            }

            private Statement VisitStatement(ReturnStatement stmt)
            {
                if (stmt.Equals(_oldNode))
                {
                    return _newNode as ReturnStatement;
                }
                return new ReturnStatement(VisitExpression(stmt.Expression));
            }

            private Statement VisitStatement(FunctionDefinition stmt)
            {
                if (stmt.Equals(_oldNode))
                {
                    return _newNode as FunctionDefinition;
                }
                var parameters = new List<Parameter>();
                foreach (var parameter in stmt.Parameters)
                {
                    parameters.Add(VisitExpression(parameter));
                }
                var def = new FunctionDefinition(stmt.Name, parameters.ToArray(), VisitStatement(stmt.Body));
                return def;
            }

            private Parameter VisitExpression(Parameter parameter)
            {
                if (parameter.Equals(_oldNode))
                {
                    return _newNode as Parameter;
                }
                //todo: not checking if the expression inside the parameter was changed
                return parameter;
            }

            private Statement VisitStatement(IfStatement stmt)
            {
                if (stmt.Equals(_oldNode))
                {
                    return _newNode as IfStatement;
                }
                else
                {
                    var newTests = new List<IfStatementTest>();
                    foreach (var ifStatementTest in stmt.Tests)
                    {
                        var newTest = new IfStatementTest(VisitExpression(ifStatementTest.Test),
                            VisitStatement(ifStatementTest.Body));
                        newTests.Add(newTest);
                    }
                    return new IfStatement(newTests.ToArray(), VisitStatement(stmt.ElseStatement));
                }
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
