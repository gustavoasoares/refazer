using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython.Compiler;
using IronPython.Compiler.Ast;

namespace Tutor
{
    public class NodeWrapper
    {
        public static PythonNode Wrap(PythonAst ast)
        {
            var result = new PythonNode(ast, false);
            result.AddChild(Wrap(ast.Body, result));
            
            return result;
        }

        private static PythonNode Wrap(Statement stmt, PythonNode parent)
        {
            if (stmt is SuiteStatement) return Wrap(stmt as SuiteStatement, parent);
            if (stmt is FunctionDefinition) return Wrap(stmt as FunctionDefinition, parent);
            if (stmt is ReturnStatement) return Wrap(stmt as ReturnStatement, parent);
            if (stmt is IfStatement) return Wrap(stmt as IfStatement, parent);
            if (stmt is AssignmentStatement) return Wrap(stmt as AssignmentStatement, parent);
            if (stmt is AugmentedAssignStatement) return Wrap(stmt as AugmentedAssignStatement, parent);
            if (stmt is ForStatement) return Wrap(stmt as ForStatement, parent);
            if (stmt is WhileStatement) return Wrap(stmt as WhileStatement, parent);
            if (stmt is ExpressionStatement) return Wrap(stmt as ExpressionStatement, parent);
            if (stmt is ImportStatement) return Wrap(stmt as ImportStatement, parent);
            throw new NotImplementedException(stmt.NodeName);
        }


        private static PythonNode Wrap(ImportStatement stmt, PythonNode parent)
        {
            var result = new PythonNode(stmt, false) { Parent = parent };
            return result;
        }
        private static PythonNode Wrap(ExpressionStatement stmt, PythonNode parent)
        {
            var result = new PythonNode(stmt, false) { Parent = parent };
            result.AddChild(Wrap(stmt.Expression, result));
            return result;
        }

        private static PythonNode Wrap(ForStatement stmt, PythonNode parent)
        {
            var result = new PythonNode(stmt, false) {Parent = parent};
            result.AddChild(Wrap(stmt.Left,result));
            result.AddChild(Wrap(stmt.List, result));
            result.AddChild(Wrap(stmt.Body, result));
            if (stmt.Else != null)
            {
                result.AddChild(Wrap(stmt.Else, result));
            }
            return result;
        }

        private static PythonNode Wrap(AugmentedAssignStatement stmt, PythonNode parent)
        {
            var result = new PythonNode(stmt, false) { Parent = parent, Value = stmt.Operator.ToString()};
            result.AddChild(Wrap(stmt.Left, result));
            result.AddChild(Wrap(stmt.Right, result));
            return result;
        }

        private static PythonNode Wrap(WhileStatement stmt, PythonNode parent)
        {
            var result = new PythonNode(stmt, false) { Parent = parent };
            result.AddChild(Wrap(stmt.Test, result));
            result.AddChild(Wrap(stmt.Body, result));
            if (stmt.ElseStatement != null)
            {
                result.AddChild(Wrap(stmt.ElseStatement, result));
            }
            return result;
        }


        private static PythonNode Wrap(AssignmentStatement stmt, PythonNode parent)
        {
            var result = new PythonNode(stmt, false) { Parent = parent };
            foreach (var expression in stmt.Left)
            {
                result.AddChild(Wrap(expression, result));
            }
            result.AddChild(Wrap(stmt.Right, result));
            return result;
        }

        private static PythonNode Wrap(SuiteStatement stmt, PythonNode parent)
        {
            var result = new PythonNode(stmt, false) { Parent = parent };
            foreach (var statement in stmt.Statements)
            {
                result.AddChild(Wrap(statement, result));
            }
            return result;
        }

        private static PythonNode Wrap(FunctionDefinition stmt, PythonNode parent)
        {
            var result = new PythonNode(stmt, false) { Parent = parent };
            if (stmt.Decorators != null)
            {
                foreach (var exp in stmt.Decorators)
                {
                    result.AddChild(Wrap(exp, result));
                }
            }
            foreach (var param in stmt.Parameters)
            {
                result.AddChild(Wrap(param, result));
            }
            result.AddChild(Wrap(stmt.Body, result));
            return result;
        }


        private static PythonNode Wrap(Parameter parameter, PythonNode parent)
        {
            var result = new PythonNode(parameter, false) { Parent = parent, Value = parameter.Name};
            if (parameter.DefaultValue != null)
            {
                result.AddChild(Wrap(parameter.DefaultValue, result));
            }
            return result;
        }

        private static PythonNode Wrap(Expression exp, PythonNode parent)
        {
            if (exp is NameExpression) return Wrap(exp as NameExpression, parent);
            if (exp is BinaryExpression) return Wrap(exp as BinaryExpression, parent);
            if (exp is ConstantExpression) return Wrap(exp as ConstantExpression, parent);
            if (exp is CallExpression) return Wrap(exp as CallExpression, parent);
            if (exp is TupleExpression) return Wrap(exp as TupleExpression, parent);
            if (exp is ParenthesisExpression) return Wrap((ParenthesisExpression) exp, parent);
            if (exp is MemberExpression) return Wrap((MemberExpression)exp, parent);
            throw  new NotImplementedException();
        }

        private static PythonNode Wrap(MemberExpression exp, PythonNode parent)
        {
            var result = new PythonNode(exp, false) { Parent = parent };
            //todo add target child
            return result;
        }

        private static PythonNode Wrap(ParenthesisExpression exp, PythonNode parent)
        {
            var result = new PythonNode(exp, false) { Parent = parent };
            result.AddChild(Wrap(exp.Expression, result));
            return result;
        }

        private static PythonNode Wrap(TupleExpression exp, PythonNode parent)
        {
            var result = new PythonNode(exp, false) { Parent = parent };
            foreach (var item in exp.Items)
            {
                result.AddChild(Wrap(item, result));
            }
            return result;
        }

        private static PythonNode Wrap(NameExpression exp, PythonNode parent)
        {
            return new PythonNode(exp, false) { Parent = parent, Value = exp.Name};
        }
        private static PythonNode Wrap(BinaryExpression exp, PythonNode parent)
        {
            var result = new PythonNode(exp, false) { Parent = parent, Value = exp.Operator.ToString()};
            result.AddChild(Wrap(exp.Left, result));
            result.AddChild(Wrap(exp.Right, result));
            return result;
        }

        private static PythonNode Wrap(ConstantExpression exp, PythonNode parent)
        {
            return new PythonNode(exp, false) { Parent = parent, Value = exp.Value.ToString()};
        }

        private static PythonNode Wrap(CallExpression exp, PythonNode parent)
        {
            var result = new PythonNode(exp, false) { Parent = parent};
            result.AddChild(Wrap(exp.Target, result));
            for (var i = 0; i < exp.Args.Count; i++)
            {
                var arg = exp.Args[i];
                result.AddChild(Wrap(arg, result));
            }
            return result;
        }

        private static PythonNode Wrap(Arg arg, PythonNode parent)
        {
            var result = new PythonNode(arg, false) {Parent = parent, Value = arg.Name};
            result.AddChild(Wrap(arg.Expression, result));
            return result;
        }

        private static PythonNode Wrap(ReturnStatement stmt, PythonNode parent)
        {
            var result = new PythonNode(stmt, false) { Parent = parent };
            result.AddChild(Wrap(stmt.Expression, result));
            return result;
        }

        private static PythonNode Wrap(IfStatement stmt, PythonNode parent)
        {
            var result = new PythonNode(stmt, false) { Parent = parent };
            for (var i = 0; i < stmt.Tests.Count; i++)
            {
                var test = stmt.Tests[i];
                var child = Wrap(test, result);
                result.AddChild(child);
                child.AddChild(Wrap(test.Body,child));
            }
            if (stmt.ElseStatement != null)
            {
                result.AddChild(Wrap(stmt.ElseStatement,result));
            }
            return result;
        }

        private static PythonNode Wrap(IfStatementTest test, PythonNode parent)
        {
            var result = new PythonNode(test, false) { Parent = parent };
            result.AddChild(Wrap(test.Test, result));
            return result;
           
        }
    }
}
