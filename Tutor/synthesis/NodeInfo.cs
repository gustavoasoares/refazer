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

        protected bool Equals(NodeInfo other)
        {
            return string.Equals(NodeType, other.NodeType) && Equals(NodeValue, other.NodeValue);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((NodeInfo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((NodeType != null ? NodeType.GetHashCode() : 0)*397) ^ (NodeValue != null ? NodeValue.GetHashCode() : 0);
            }
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
                case "FunctionDefinition":
                    nodeValue = ((FunctionDefinition)node.InnerNode).Name;
                    break;
                case "literal":
                    nodeValue = ((ConstantExpression) node.InnerNode).Value;
                    break;
                case "AugmentedAssignStatement":
                    nodeValue = ((AugmentedAssignStatement)node.InnerNode).Operator;
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
                case "Parameter":
                    nodeValue = ((Parameter)node.InnerNode).Name;
                    break;
                case "Arg":
                case "CallExpression":
                case "LambdaExpression":
                case "SuiteStatement":
                case "IfStatement":
                case "IfStatementTest":
                case "AssignmentStatement":
                case "ReturnStatement":
                case "ExpressionStatement":
                case "WhileStatement":
                case "ParenthesisExpression":
                case "ForStatement":
                    nodeValue = null;
                    break;
                default: 
                    throw new NotImplementedException();
            }
            return nodeValue == null ? new NodeInfo(type) : new NodeInfo(type, nodeValue);
        }
    }
}
