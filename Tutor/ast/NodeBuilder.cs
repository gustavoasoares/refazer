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
                case "Parameter":
                    return new ConstantExpression(info.NodeValue);
                default:
                    throw new NotImplementedException(info.NodeType);
            }
        }

        internal static Node Create(NodeInfo info, List<PythonNode> Children)
        {
            var rewriter = new Rewriter(new List<Edit>());
            switch (info.NodeType)
            {
                case "CallExpression":
                    var target = (Expression)Children[0].InnerNode;
                    var args = new List<Arg>();
                    if (Children.Count > 1)
                    {
                        for (var i = 1; i < Children.Count; i++)
                        {
                            args.Add(rewriter.VisitArg((Arg)Children[i].InnerNode));
                        }
                    }
                    return new CallExpression(rewriter.VisitExpression(target), args.ToArray());
                case "Arg":
                    var expression = (Expression)Children[0].InnerNode;
                    return (info.NodeValue == null) ? new Arg(rewriter.VisitExpression(expression)) :
                        new Arg(info.NodeType, expression);
                case "BinaryExpression":
                    var left = (Expression)Children[0].InnerNode;
                    var right = (Expression)Children[1].InnerNode;
                    PythonOperator op = info.NodeValue;
                    return new BinaryExpression(op, rewriter.VisitExpression(left), rewriter.VisitExpression(right));
                case "AugmentedAssignStatement":
                    var left1 = (Expression)Children[0].InnerNode;
                    var right1 = (Expression)Children[1].InnerNode;
                    PythonOperator op1 = info.NodeValue;
                    return new AugmentedAssignStatement(op1, rewriter.VisitExpression(left1), rewriter.VisitExpression(right1));
                case "SuiteStatement":
                    var statements = Children.Select(e => rewriter.VisitStatement((Statement)e.InnerNode));
                    return new SuiteStatement(statements.ToArray());
                case "WhileStatement":
                    var test = (Expression) Children[0].InnerNode;
                    var body = (Statement) Children[1].InnerNode;
                    Statement else_ = (Children.Count == 3) ? rewriter.VisitStatement((Statement)Children[2].InnerNode) : null; 
                    return new WhileStatement(rewriter.VisitExpression(test), rewriter.VisitStatement(body), else_);
                case "ReturnStatement":
                    return new ReturnStatement(rewriter.VisitExpression((Expression)Children[0].InnerNode));
                case "Parameter":
                    var parameter = new Parameter(info.NodeValue);
                    if (Children.Any()) parameter.DefaultValue = rewriter.VisitExpression((Expression)Children[0].InnerNode);
                    return parameter;
                case "ExpressionStatement":
                    return new ReturnStatement(rewriter.VisitExpression((Expression)Children[0].InnerNode));
                case "ParenthesisExpression":
                    return new ParenthesisExpression(rewriter.VisitExpression((Expression)Children[0].InnerNode));  
                case "IfStatement":
                    if (Children.Last().InnerNode is IfStatementTest)
                    {
                        return new IfStatement(Children.Select(e => (IfStatementTest) e.InnerNode).ToArray(), null);
                    }
                    var tests = Children.GetRange(0, Children.Count - 1).Select(e => (IfStatementTest) e.InnerNode);
                    var elseStmt = Children.Last().InnerNode;
                    return new IfStatement(tests.ToArray(), rewriter.VisitStatement((Statement)elseStmt));
                case "IfStatementTest":
                    return new IfStatementTest(rewriter.VisitExpression((Expression)Children[0].InnerNode), rewriter.VisitStatement((Statement)Children[1].InnerNode));
                case "AssignmentStatement":
                    IEnumerable<Expression> leftAssign = Children.GetRange(0, Children.Count - 1).Select(e => rewriter.VisitExpression((Expression)e.InnerNode));
                    return new AssignmentStatement(leftAssign.ToArray(), rewriter.VisitExpression((Expression)Children.Last().InnerNode));
                case "TupleExpression":
                    IEnumerable<Expression> expressions = Children.Select(e => rewriter.VisitExpression((Expression)e.InnerNode));
                    return new TupleExpression(info.NodeValue, expressions.ToArray());
                default:
                    throw new NotImplementedException(info.NodeType);
            }
        }
    }
}
