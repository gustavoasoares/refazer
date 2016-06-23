using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using IronPython.Compiler;
using IronPython.Compiler.Ast;
using Tutor.ast;

namespace Tutor
{
    public class Unparser
    {
        private const string Indentation = "    ";

        private StringBuilder _code;
        private readonly string _newLine = Environment.NewLine;
        private int _indent = 0;


        public string Unparse(PythonNode ast)
        {
            _indent = 0;
            _code = new StringBuilder();
            try
            {
                ast.Children.ForEach(e => Write(e));
            }
            catch (ArgumentOutOfRangeException)
            {
                Console.Out.WriteLine("Invalid output program");
            }
            catch (AggregateException)
            {
                Console.Out.WriteLine("Invalid output program");
            }
            return _code.ToString();
        }

        private void Write(PythonNode stmt, bool notlambda = true)
        {
            if (stmt is SuiteStatementNode) Write(stmt as SuiteStatementNode);
            else if (stmt is FunctionDefinitionNode) Write(stmt as FunctionDefinitionNode);
            else if (stmt is ReturnStatementNode) Write(stmt as ReturnStatementNode, notlambda);
            else if (stmt is IfStatementNode) Write(stmt as IfStatementNode);
            else if (stmt is AssignmentStatementNode) Write(stmt as AssignmentStatementNode);
            else if (stmt is AugmentedAssignStatementNode) Write(stmt as AugmentedAssignStatementNode);
            else if (stmt is ForStatementNode) Write(stmt as ForStatementNode);
            else if (stmt is WhileStatementNode) Write(stmt as WhileStatementNode);
            else if (stmt is ExpressionStatementNode) Write(stmt as ExpressionStatementNode, notlambda);
            else if (stmt is ImportStatementNode) Write(stmt as ImportStatementNode);
            else if (stmt is NameExpressionNode) Write(stmt as NameExpressionNode);
            else if (stmt is BinaryExpressionNode) Write(stmt as BinaryExpressionNode);
            else if (stmt is ConstantExpressionNode) Write(stmt as ConstantExpressionNode);
            else if (stmt is CallExpressionNode) Write(stmt as CallExpressionNode);
            else if (stmt is ArgNode) Write((ArgNode)stmt);
            else if (stmt is TupleExpressionNode) Write(stmt as TupleExpressionNode);
            else if (stmt is ParenthesisExpressionNode) Write((ParenthesisExpressionNode)stmt);
            else if (stmt is MemberExpressionNode) Write((MemberExpressionNode)stmt);
            else if (stmt is LambdaExpressionNode) Write((LambdaExpressionNode)stmt);
            else if (stmt is IndexExpressionNode) Write((IndexExpressionNode)stmt);
            else if (stmt is ListExpressionNode) Write((ListExpressionNode)stmt);
            else if (stmt is OrExpressionNode) Write((OrExpressionNode)stmt);
            else if (stmt is UnaryExpressionNode) Write((UnaryExpressionNode)stmt);
            else if (stmt is ParameterNode) Write((ParameterNode)stmt);
            else if (stmt is PrintStatementNode) Write((PrintStatementNode)stmt);
            else
                throw new NotImplementedException();
        }

        private void Write(ImportStatementNode stmt)
        {
            Fill();
            _code.Append("import ");
            for (var i = 0; i < stmt.Names.Count; i++)
            {
                var name = stmt.Names[i];
                _code.Append(name);
                if (i < stmt.Names.Count - 1)
                    _code.Append(", ");
            }
        }

        private void Write(ExpressionStatementNode stmt, bool notlambda = true)
        {
            if (string.IsNullOrEmpty(stmt.Documentation))
            {
                if (notlambda) Fill();
                Write(stmt.Children[0]);
            }
        }

        private void Write(PrintStatementNode stmt, bool notlambda = true)
        {
            Fill();
            _code.Append("print");
            //todo print destination
            for (var i = 0; i < stmt.Children.Count; i++)
            {
                Write(stmt.Children[i]);
                if (i < stmt.Children.Count - 1)
                    _code.Append(", ");
            }
        }

        private void Write(ForStatementNode stmt)
        {
            Fill();
            _code.Append("for ");
            Write(stmt.Children[0]);
            _code.Append(" in ");
            Write(stmt.Children[1]);
            Enter();
            Write(stmt.Children[2]);
            Leave();
            if (stmt.Children.Count == 4)
            {
                Fill();
                _code.Append("else");
                Enter();
                Write(stmt.Children[3]);
                Leave();
            }
        }

        private void Write(AugmentedAssignStatementNode stmt)
        {
            Fill();
            Write(stmt.Children[0]);
            _code.Append(" ");
            Write(stmt.Value);
            _code.Append("= ");
            Write(stmt.Children[1]);
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

        private void Write(WhileStatementNode stmt)
        {
            Fill();
            _code.Append("while ");
            Write(stmt.Children[0]);
            Enter();
            Write(stmt.Children[1]);
            Leave();
            if (stmt.Children.Count == 3)
            {
                Fill();
                _code.Append("else");
                Enter();
                Write(stmt.Children[2]);
                Leave();
            }
        }


        private void Write(AssignmentStatementNode stmt)
        {
            Fill();
            foreach (var expression in stmt.Children.GetRange(0,stmt.Children.Count-1))
            {
                Write(expression);
                _code.Append(" = ");
            }
            Write(stmt.Children.Last());
        }

        private void Write(SuiteStatementNode stmt)
        {
            foreach (var statement in stmt.Children)
            {
                Write(statement);
            }
        }

        private void Write(FunctionDefinitionNode stmt)
        {
            //todo add decorator?
            Fill();
            _code.Append("def " + stmt.Value + "(");
            for (var i = 0; i < stmt.Children.Count - 1; i++)
            {
                var parameter = stmt.Children[i];
                Write(parameter);
                if (i < stmt.Children.Count - 2)
                    _code.Append(", ");
            }
            _code.Append(")");
            Enter();
            Write(stmt.Children.Last());
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

        private void Write(ParameterNode parameter)
        {
            _code.Append(parameter.Value);
            if (parameter.Children.Any())
            {
                _code.Append(" = ");
                Write(parameter.Children[0]);
            }
        }



        private void Write(LambdaExpressionNode exp)
        {
            _code.Append("lambda ");
            var function = exp.Children[0];
            var numberOfParameters = function.Children.Count - 1;
            for (var i = 0; i < numberOfParameters; i++)
            {
                var arg = function.Children[i];
                Write(arg);
                if (i < numberOfParameters - 1)
                    _code.Append(", ");
            }
            _code.Append(": ");
            Write(function.Children.Last(), false);
        }

        private void Write(UnaryExpressionNode exp)
        {
            Write(exp.Value);
            _code.Append(" ");
            Write(exp.Children[0]);
        }

        private void Write(OrExpressionNode exp)
        {
            Write(exp.Children[0]);
            _code.Append(" or ");
            Write(exp.Children[1]);
        }

        private void Write(ListExpressionNode exp)
        {
            _code.Append("[");
            for (var i = 0; i < exp.Children.Count; i++)
            {
                Write(exp.Children[i]);
                if (i < exp.Children.Count - 1)
                    _code.Append(", ");
            }
            _code.Append("]");
        }

        private void Write(IndexExpressionNode exp)
        {
            Write(exp.Children[0]);
            _code.Append("[");
            Write(exp.Children[1]);
            _code.Append("]");
        }

        private void Write(MemberExpressionNode exp)
        {
            Write(exp.Children[0]);
            _code.Append(".");
            _code.Append(exp.Value);
        }

        private void Write(ParenthesisExpressionNode exp)
        {
            _code.Append("(");
            Write(exp.Children[0]);
            _code.Append(")");
        }

        private void Write(TupleExpressionNode exp)
        {
            for (var i = 0; i < exp.Children.Count; i++)
            {
                Write(exp.Children[i]);
                if (i < exp.Children.Count - 1)
                    _code.Append(", ");
            }
        }

        private void Write(NameExpressionNode exp)
        {
            _code.Append(exp.Value);
        }
        private void Write(BinaryExpressionNode exp)
        {
            Write(exp.Children[0]);
            Write((PythonOperator) exp.Value);
            Write(exp.Children[1]);
        }

        private void Write(ConstantExpressionNode exp)
        {
            if (exp.Value != null)
                _code.Append(exp.Value.ToString());
        }

        private void Write(CallExpressionNode exp)
        {
            Write(exp.Children[0]);
            _code.Append("(");
            for (var i = 1; i < exp.Children.Count; i++)
            {
                var arg = exp.Children[i];
                Write(arg);
                if (i < exp.Children.Count - 1)
                    _code.Append(", ");
            }
            _code.Append(")");
        }

        private void Write(ArgNode arg)
        {
            Write(arg.Children[0]);
        }

        private void Write(ReturnStatementNode stmt, bool notlambda = true)
        {
            if (notlambda)
            {
                Fill();
                _code.Append("return ");
            }
            if (stmt.Children.Any())
                Write(stmt.Children[0]);
        }

        private void Write(IfStatementNode stmt)
        {
            Fill();
            _code.Append("if ");
            var numberOfIfs = (stmt.Children.Last() is IfStatementTestNode) ? stmt.Children.Count : stmt.Children.Count - 1;
            for (var i = 0; i < numberOfIfs; i++)
            {
                var test = stmt.Children[i];
                Write(test.Children[0]);
                Enter();
                Write(test.Children[1]);
                Leave();
                if (i < numberOfIfs - 1)
                {
                    Fill();
                    _code.Append("elif ");
                }
            }
            if (!(stmt.Children.Last() is IfStatementTestNode))
            {
                Fill();
                _code.Append("else");
                Enter();
                Write(stmt.Children.Last());
                Leave();
            }
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
