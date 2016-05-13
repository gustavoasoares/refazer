using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython.Compiler.Ast;

namespace Tutor.ast
{
    class UnaryExpressionNode : InternalNode
    {
        public UnaryExpressionNode(Node innerNode, bool isAbstract) : base(innerNode, isAbstract)
        {
        }

        public UnaryExpressionNode(Node innerNode, bool isAbstract, int editId) : base(innerNode, isAbstract, editId)
        {
        }

        protected override bool IsEqualToInnerNode2(Node node)
        {
            throw new NotImplementedException();
        }

        protected override Tuple<bool, Node> CompareChildren(Node node, Node binding)
        {
            throw new NotImplementedException();
        }
    }
}
