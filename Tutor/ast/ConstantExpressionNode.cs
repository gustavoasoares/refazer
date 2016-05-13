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
        public ConstantExpressionNode(Node innerNode, bool isAbstract) : base(innerNode, isAbstract)
        {
        }

        public ConstantExpressionNode(Node innerNode, bool isAbstract, int editId) : base(innerNode, isAbstract, editId)
        {
        }

        protected override bool IsEqualToInnerNode(Node node)
        {
            var inner = InnerNode as ConstantExpression;
            var comparedNode = node as ConstantExpression;
            if (comparedNode == null) return false;
            return inner.Value.Equals(comparedNode.Value);
        }
    }
}
