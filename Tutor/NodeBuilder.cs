using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython.Compiler;
using IronPython.Compiler.Ast;

namespace Tutor
{
    public class NodeBuilder
    {
        public static Node Create(NodeInfo info)
        {
            
            switch (info.NodeType)
            {
                case "NameExpression":
                    return new NameExpression(info.NodeValue);
                case "literal": 
                    return new ConstantExpression(info.NodeValue);
                default:
                    throw new NotImplementedException(info.NodeType);
            }
        }

        internal static Node Create(NodeInfo info, List<Node> Children)
        {
            switch (info.NodeType)
            {
                case "CallExpression":
                    var target = (Expression)Children[0];
                    var args = new List<Arg>();
                    if (Children.Count > 1)
                    {
                        for (var i = 1; i < Children.Count; i++)
                        {
                            args.Add((Arg)Children[i]);
                        }
                    }
                    return new CallExpression(target, args.ToArray());
                case "Arg":
                    var expression = (Expression)Children[0];
                    return (info.NodeValue == null) ? new Arg(expression) :
                        new Arg(info.NodeType, expression);
                case "BinaryExpression":
                    var left = (Expression)Children[0];
                    var right = (Expression)Children[1];
                    PythonOperator op = info.NodeValue;
                    return new BinaryExpression(op, left, right);
                case "SuiteStatement":
                    var statements = Children.Select(e => (Statement) e);
                    return new SuiteStatement(statements.ToArray());
                case "WhileStatement":
                    var test = (Expression) Children[0];
                    var body = (Statement) Children[1];
                    Statement else_ = (Children.Count == 3) ? (Statement) Children[2] : null; 
                    return new WhileStatement(test, body, else_);
                case "ReturnStatement":
                    return new ReturnStatement((Expression) Children[0]);
                case "ExpressionStatement":
                    return new ReturnStatement((Expression)Children[0]);
                case "IfStatement":
                    if (Children.Last() is IfStatementTest)
                    {
                        return new IfStatement(Children.Select(e => (IfStatementTest) e).ToArray(), null);
                    }
                    var tests = Children.GetRange(0, Children.Count - 1).Select(e => (IfStatementTest) e);
                    var elseStmt = Children.Last();
                    return new IfStatement(tests.ToArray(), (Statement) elseStmt);
                case "IfStatementTest":
                    return new IfStatementTest((Expression) Children[0],(Statement) Children[1]);
                case "AssignmentStatement":
                    IEnumerable<Expression> leftAssign = Children.GetRange(0, Children.Count - 1).Select(e => (Expression) e);
                    return new AssignmentStatement(leftAssign.ToArray(), (Expression) Children.Last());
                case "TupleExpression":
                    IEnumerable<Expression> expressions = Children.Select(e => (Expression) e);
                    return new TupleExpression(info.NodeValue, expressions.ToArray());
                default:
                    throw new NotImplementedException(info.NodeType);
            }
        }
    }
}
