using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython.Compiler.Ast;

namespace Tutor
{
    public class NodeInfo
    {
        public string NodeType { get; }
        public dynamic NodeValue { get; }

        public NodeInfo(string nodeType, dynamic nodeValue)
        {
            NodeType = nodeType;
            NodeValue = nodeValue;
        }

        public NodeInfo(string type)
        {
            NodeType = type;
        }

        public override string ToString()
        {
            return NodeValue == null ? NodeType : NodeType + "-" + NodeValue;
        }

        public static NodeInfo CreateInfo(PythonNode node)
        {
            var type = node.InnerNode.NodeName;
            dynamic nodeValue; 
            switch (type)
            {
                case "literal":
                    nodeValue = ((ConstantExpression) node.InnerNode).Value;
                    break;
                case "BinaryExpression":
                    nodeValue = ((BinaryExpression) node.InnerNode).Operator;
                    break;
                case "NameExpression":
                    nodeValue = ((NameExpression) node.InnerNode).Name;
                    break;
                case "TupleExpression":
                    nodeValue = ((TupleExpression) node.InnerNode).IsExpandable;
                    break;
                case "Arg":
                case "CallExpression":
                case "SuiteStatement":
                case "IfStatement":
                case "IfStatementTest":
                case "AssignmentStatement":
                
                    nodeValue = null;
                    break;
                default: 
                    throw new NotImplementedException();
            }
            return nodeValue == null ? new NodeInfo(type) : new NodeInfo(type, nodeValue);
        }
    }
}
