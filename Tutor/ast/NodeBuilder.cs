using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython.Compiler;
using IronPython.Compiler.Ast;
using Tutor.ast;

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

        internal static PythonNode Create(NodeInfo info, List<PythonNode> Children)
        {
            if (Children == null) Children = new List<PythonNode>();
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
                    var inner = new CallExpression(rewriter.VisitExpression(target), args.ToArray());
                    return new CallExpressionNode(inner, false) {Children = Children};
                case "Arg":
                    var expression = (Expression)Children[0].InnerNode;
                    var innerArg = (info.NodeValue == null) ? new Arg(rewriter.VisitExpression(expression)) :
                        new Arg(info.NodeType, expression);
                    return new ArgNode(innerArg, false) {Children = Children, Value = innerArg.Name};
                case "BinaryExpression":
                    var left = (Expression)Children[0].InnerNode;
                    var right = (Expression)Children[1].InnerNode;
                    PythonOperator op = info.NodeValue;
                    var binaryExpression = new BinaryExpression(op, rewriter.VisitExpression(left), rewriter.VisitExpression(right));
                    return new BinaryExpressionNode(binaryExpression, false) {Children = Children, Value = binaryExpression.Operator };
                case "AugmentedAssignStatement":
                    var left1 = (Expression)Children[0].InnerNode;
                    var right1 = (Expression)Children[1].InnerNode;
                    PythonOperator op1 = info.NodeValue;
                    var augmentedAssignStatement = new AugmentedAssignStatement(op1, rewriter.VisitExpression(left1), rewriter.VisitExpression(right1));
                    return new AugmentedAssignStatementNode(augmentedAssignStatement, false) {Children = Children, Value = augmentedAssignStatement.Operator };
                case "SuiteStatement":
                    var statements = Children.Select(e => rewriter.VisitStatement((Statement)e.InnerNode));
                    var suiteStatement = new SuiteStatement(statements.ToArray());
                    return new SuiteStatementNode(suiteStatement, false) {Children = Children};
                case "WhileStatement":
                    var test = (Expression) Children[0].InnerNode;
                    var body = (Statement) Children[1].InnerNode;
                    Statement else_ = (Children.Count == 3) ? rewriter.VisitStatement((Statement)Children[2].InnerNode) : null;
                    var whileStatement = new WhileStatement(rewriter.VisitExpression(test), rewriter.VisitStatement(body), else_);
                    return new WhileStatementNode(whileStatement, false) {Children = Children};
                case "ReturnStatement":
                    var returnStatement = new ReturnStatement(rewriter.VisitExpression((Expression)Children[0].InnerNode));
                    return new ReturnStatementNode(returnStatement, false) {Children = Children};
                case "Parameter":
                    var parameter = new Parameter(info.NodeValue);
                    if (Children.Any()) parameter.DefaultValue = rewriter.VisitExpression((Expression)Children[0].InnerNode);
                    return new ParameterNode(parameter, false) {Children = Children, Value = parameter.Name};
                case "ExpressionStatement":
                    var expressionStatement = new ExpressionStatement(rewriter.VisitExpression((Expression)Children[0].InnerNode));
                    return new ExpressionStatementNode(expressionStatement, false) {Children = Children};
                case "ParenthesisExpression":
                    var parenthesisExpression = new ParenthesisExpression(rewriter.VisitExpression((Expression)Children[0].InnerNode));
                    return new ParenthesisExpressionNode(parenthesisExpression, false) {Children = Children};  
                case "IfStatement":
                    if (Children.Last().InnerNode is IfStatementTest)
                    {
                        var ifStatement = new IfStatement(Children.Select(e => (IfStatementTest) e.InnerNode).ToArray(), null);
                        return new IfStatementNode(ifStatement, false) {Children = Children};
                    }
                    var tests = Children.GetRange(0, Children.Count - 1).Select(e => (IfStatementTest) e.InnerNode);
                    var elseStmt = Children.Last().InnerNode;
                    var statement = new IfStatement(tests.ToArray(), rewriter.VisitStatement((Statement)elseStmt));
                    return new IfStatementNode(statement, false) {Children = Children};
                case "IfStatementTest":
                    var ifStatementTest = new IfStatementTest(rewriter.VisitExpression((Expression)Children[0].InnerNode), rewriter.VisitStatement((Statement)Children[1].InnerNode));
                    return new IfStatementTestNode(ifStatementTest, false) { Children = Children};
                case "AssignmentStatement":
                    IEnumerable<Expression> leftAssign = Children.GetRange(0, Children.Count - 1).Select(e => rewriter.VisitExpression((Expression)e.InnerNode));
                    var assignmentStatement = new AssignmentStatement(leftAssign.ToArray(), rewriter.VisitExpression((Expression)Children.Last().InnerNode));
                    return new AssignmentStatementNode(assignmentStatement, false) {Children = Children};
                case "TupleExpression":
                    IEnumerable<Expression> expressions = Children.Select(e => rewriter.VisitExpression((Expression)e.InnerNode));
                    var tupleExpression = new TupleExpression(info.NodeValue, expressions.ToArray());
                    return new TupleExpressionNode(tupleExpression, false) {Children = Children};
                default:
                    throw new NotImplementedException(info.NodeType);
            }
        }
    }
}
