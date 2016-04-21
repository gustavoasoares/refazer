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
                default:
                    throw new NotImplementedException(info.NodeType);
            }
        }
    }
}
