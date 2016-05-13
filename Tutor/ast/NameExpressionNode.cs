using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython.Compiler.Ast;

namespace Tutor.ast
{
    class NameExpressionNode : LeafNode
    {
        public NameExpressionNode(Node innerNode, bool isAbstract) : base(innerNode, isAbstract)
        {
        }

        public NameExpressionNode(Node innerNode, bool isAbstract, int editId) : base(innerNode, isAbstract, editId)
        {
        }

        protected override bool IsEqualToInnerNode2(Node node)
        {
            var inner = InnerNode as NameExpression;
            var comparedNode = node as NameExpression;
            if (comparedNode == null) return false;
            return inner.Name.Equals(comparedNode.Name);
        }
    }
}
