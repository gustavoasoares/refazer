using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython.Compiler;
using IronPython.Compiler.Ast;

namespace Tutor
{
    public class Unparser
    {
        private const string Indentation = "    ";

        private StringBuilder _code;
        private readonly string _newLine = Environment.NewLine;
        private int _indent = 0;


        public string Unparse(PythonAst ast)
        {
            _code = new StringBuilder();
            Write(ast.Body);
            return _code.ToString();
        }

        private void Write(Statement stmt)
        {
            if (stmt is SuiteStatement) Write(stmt as SuiteStatement);
            if (stmt is FunctionDefinition) Write(stmt as FunctionDefinition);
            if (stmt is ReturnStatement) Write(stmt as ReturnStatement);
            if (stmt is IfStatement) Write(stmt as IfStatement);
            if (stmt is AssignmentStatement) Write(stmt as AssignmentStatement);
            if (stmt is AugmentedAssignStatement) Write(stmt as AugmentedAssignStatement);
            if (stmt is ForStatement) Write(stmt as ForStatement);
            if (stmt is WhileStatement) Write(stmt as WhileStatement);
            if (stmt is ExpressionStatement) Write(stmt as ExpressionStatement);
        }

        private void Write(ExpressionStatement stmt)
        {
            if (string.IsNullOrEmpty(stmt.Documentation))
            {
                Write(stmt.Expression);
            }
        }

        private void Write(ForStatement stmt)
        {
            Fill();
            _code.Append("for ");
            Write(stmt.Left);
            _code.Append(" in ");
            Write(stmt.List);
            Enter();
            Write(stmt.Body);
            Leave();
            if (stmt.Else != null)
            {
                Fill();
                _code.Append("else");
                Enter();
                Write(stmt.Else);
                Leave();
            }
        }

        private void Write(AugmentedAssignStatement stmt)
        {
            Fill();
            Write(stmt.Left);
            _code.Append(" ");
            Write(stmt.Operator);
            _code.Append("= ");
            Write(stmt.Right);
        }

        private void Write(PythonOperator op)
        {
            switch (op)
            {
                case PythonOperator.Equals:
                    _code.Append("==");
                    break;
                case PythonOperator.Add:
                    _code.Append("+");
                    break;
                case PythonOperator.Subtract:
                    _code.Append("-");
                    break;
                case PythonOperator.LessThan:
                    _code.Append("<");
                    break;
                case PythonOperator.LessThanOrEqual:
                    _code.Append("<=");
                    break;
                case PythonOperator.GreaterThan:
                    _code.Append(">");
                    break;
                case PythonOperator.GreaterThanOrEqual:
                    _code.Append(">=");
                    break;
                case PythonOperator.Multiply:
                    _code.Append("*");
                    break;
                case PythonOperator.Divide:
                    _code.Append("/");
                    break;
                default:
                    throw new Exception("Operator string not defined: " + op);
            }
        }

        private void Write(WhileStatement stmt)
        {
            Fill();
            _code.Append("while ");
            Write(stmt.Test);
            Enter();
            Write(stmt.Body);
            Leave();
            if (stmt.ElseStatement != null)
            {
                Fill();
                _code.Append("else");
                Enter();
                Write(stmt.ElseStatement);
                Leave();
            }
        }

        
        private void Write(AssignmentStatement stmt)
        {
            Fill();
            foreach (var expression in stmt.Left)
            {
                Write(expression);
                _code.Append(" = ");
            }
            Write(stmt.Right);
        }

        private void Write(SuiteStatement stmt)
        {
            foreach (var statement in stmt.Statements)
            {
                Write(statement);
            }
        }

        private void Write(FunctionDefinition stmt)
        {
            //todo add decorator?
            Fill();
            _code.Append("def " + stmt.Name + "(");
            for (var i = 0; i < stmt.Parameters.Count; i++)
            {
                var parameter = stmt.Parameters[i];
                Write(parameter);
                if (i < stmt.Parameters.Count - 1)
                    _code.Append(", ");
            }
            _code.Append(")");
            Enter();
            Write(stmt.Body);
            Leave();
        }

        private void Leave()
        {
            _indent -= 1;
        }

        private void Enter()
        {
            _code.Append(":");
            _indent += 1;
        }

        private void Write(Parameter parameter)
        {
            _code.Append(parameter.Name);
            if (parameter.DefaultValue != null)
            {
                _code.Append(" = ");
                Write(parameter.DefaultValue);
            }
        }

        private void Write(Expression exp)
        {
            if (exp is NameExpression) Write(exp as NameExpression);
            if (exp is BinaryExpression) Write(exp as BinaryExpression);
            if (exp is ConstantExpression) Write(exp as ConstantExpression);
            if (exp is CallExpression) Write(exp as CallExpression);
            if (exp is TupleExpression) Write(exp as TupleExpression);
            if (exp is ParenthesisExpression) Write((ParenthesisExpression) exp);
        }

        private void Write(ParenthesisExpression exp)
        {
            _code.Append("(");
            Write(exp.Expression);
            _code.Append(")");
        }

        private void Write(TupleExpression exp)
        {
            for (var i = 0; i < exp.Items.Count; i++)
            {
                Write(exp.Items[i]);
                if (i < exp.Items.Count - 1)
                    _code.Append(", ");
            }
        }

        private void Write(NameExpression exp)
        {
            _code.Append(exp.Name);
        }
        private void Write(BinaryExpression exp)
        {
            Write(exp.Left);
            Write(exp.Operator);
            Write(exp.Right);
        }

        private void Write(ConstantExpression exp)
        {
            _code.Append(exp.Value);
        }

        private void Write(CallExpression exp)
        {
            Write(exp.Target);
            _code.Append("(");
            for (var i = 0; i < exp.Args.Count; i++)
            {
                var arg = exp.Args[i];
                Write(arg);
                if (i < exp.Args.Count - 1)
                    _code.Append(", ");
            }
            _code.Append(")");
        }

        private void Write(Arg arg)
        {
            Write(arg.Expression);
        }

        private void Write(ReturnStatement stmt)
        {
            Fill();
            _code.Append("return ");
            Write(stmt.Expression);
        }

        private void Write(IfStatement stmt)
        {
            Fill();
            _code.Append("if ");
            for (var i = 0; i < stmt.Tests.Count; i++)
            {
                var test = stmt.Tests[i];
                Write(test);
                Enter();
                Write(test.Body);
                Leave();
                if (i < stmt.Tests.Count - 1)
                {
                    Fill();
                    _code.Append("elif");
                    Enter();
                }
            }
            if (stmt.ElseStatement != null)
            {
                Fill();
                _code.Append("else");
                Enter();
                Write(stmt.ElseStatement);
                Leave();
            }
        }

        private void Write(IfStatementTest test)
        {
            Write(test.Test);
        }

        private void Fill()
        {
            _code.Append(_newLine);
            for (var i = 0; i < _indent; i++)
            {
                _code.Append(Indentation);
            }
        }
    }
}
