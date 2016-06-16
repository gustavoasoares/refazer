using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython.Compiler.Ast;

namespace Tutor.ast
{
    class ConstantExpressionNode : LeafNode
    {
        public ConstantExpressionNode(Node innerNode) : base(innerNode)
        {
        }
        protected override bool IsEqualToInnerNode(Node node)
        {
            var inner = InnerNode as ConstantExpression;
            var comparedNode = node as ConstantExpression;
            if (comparedNode == null) return false;
            if (Value == null && comparedNode.Value == null)
                return true;
            if (Value == null && comparedNode.Value != null)
                return false;
            if (Value != null && comparedNode.Value == null)
                return false;
            return inner.Value.Equals(comparedNode.Value);
        }

        public override PythonNode Clone()
        {
            var pythonNode = new ConstantExpressionNode(InnerNode);
            pythonNode.Id = Id;
            if (Value != null) pythonNode.Value = Value;
            return pythonNode;
        }
    }
}
