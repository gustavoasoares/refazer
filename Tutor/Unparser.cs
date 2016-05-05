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

        private void Write(Statement stmt, bool notlambda = true)
        {
            if (stmt is SuiteStatement) Write(stmt as SuiteStatement);
            else if (stmt is FunctionDefinition) Write(stmt as FunctionDefinition);
            else if (stmt is ReturnStatement) Write(stmt as ReturnStatement, notlambda);
            else if (stmt is IfStatement) Write(stmt as IfStatement);
            else if (stmt is AssignmentStatement) Write(stmt as AssignmentStatement);
            else if (stmt is AugmentedAssignStatement) Write(stmt as AugmentedAssignStatement);
            else if (stmt is ForStatement) Write(stmt as ForStatement);
            else if (stmt is WhileStatement) Write(stmt as WhileStatement);
            else if (stmt is ExpressionStatement) Write(stmt as ExpressionStatement, notlambda);
            else if (stmt is ImportStatement) Write(stmt as ImportStatement);
            else 
                throw new NotImplementedException();
        }

        private void Write(ImportStatement stmt)
        {
            Fill();
            _code.Append("import ");
            for (var i = 0; i < stmt.AsNames.Count; i++)
            {
                var name = stmt.AsNames[i];
                _code.Append(name);
                if (i < stmt.AsNames.Count - 1)
                    _code.Append(", ");
            }
        }

        private void Write(ExpressionStatement stmt, bool notlambda = true)
        {
            if (string.IsNullOrEmpty(stmt.Documentation))
            {
                if (notlambda) Fill();
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
                case PythonOperator.Power:
                    _code.Append("**");
                    break;
                case PythonOperator.NotEqual:
                    _code.Append("!=");
                    break;
                case PythonOperator.Not:
                    _code.Append("not");
                    break;
                default:
                    throw new NotImplementedException("Operator string not defined: " + op);
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
            else if (exp is BinaryExpression) Write(exp as BinaryExpression);
            else if (exp is ConstantExpression) Write(exp as ConstantExpression);
            else if (exp is CallExpression) Write(exp as CallExpression);
            else if (exp is TupleExpression) Write(exp as TupleExpression);
            else if (exp is ParenthesisExpression) Write((ParenthesisExpression) exp);
            else if (exp is MemberExpression) Write((MemberExpression)exp);
            else if (exp is LambdaExpression) Write((LambdaExpression)exp);
            else if (exp is IndexExpression) Write((IndexExpression)exp);
            else if (exp is OrExpression) Write((OrExpression)exp);
            else if (exp is UnaryExpression) Write((UnaryExpression)exp);
            else throw new NotImplementedException();
        }

        private void Write(LambdaExpression exp)
        {
            _code.Append("lambda ");
            for (var i = 0; i < exp.Function.Parameters.Count; i++)
            {
                var arg = exp.Function.Parameters[i];
                Write(arg);
                if (i < exp.Function.Parameters.Count - 1)
                    _code.Append(", ");
            }
            _code.Append(": ");
            Write(exp.Function.Body, false);
        }

        private void Write(UnaryExpression exp)
        {
            Write(exp.Op);
            _code.Append(" ");
            Write(exp.Expression);
        }

        private void Write(OrExpression exp)
        {
            Write(exp.Left);
            _code.Append(" or ");
            Write(exp.Right);
        }

        private void Write(IndexExpression exp)
        {
            Write(exp.Target);
            _code.Append("[");
            Write(exp.Index);
            _code.Append("]");
        }

        private void Write(MemberExpression exp)
        {
            Write(exp.Target);
            _code.Append(".");
            _code.Append(exp.Name);
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

        private void Write(ReturnStatement stmt, bool notlambda = true)
        {
            if (notlambda)
            {
                Fill();
                _code.Append("return ");
            }
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
                    _code.Append("elif ");
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
