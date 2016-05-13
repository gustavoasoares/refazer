using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython.Compiler.Ast;

namespace Tutor.ast
{
    class LambdaExpressionNode : InternalNode
    {
        public LambdaExpressionNode(Node innerNode, bool isAbstract) : base(innerNode, isAbstract)
        {
        }

        public LambdaExpressionNode(Node innerNode, bool isAbstract, int editId) : base(innerNode, isAbstract, editId)
        {
        }

        protected override bool IsEqualToInnerNode(Node node)
        {
            var comparedNode = node as LambdaExpression;
            if (comparedNode == null) return false;
            return true;
        }

        protected override Tuple<bool, Node> CompareChildren(Node node, Node binding)
        {
            var convertedNode = (LambdaExpression)node;
            if (convertedNode == null) return Tuple.Create<bool, Node>(false, null);

            if (Children.Count != 1)
                return Tuple.Create<bool, Node>(false, null);

            var result = Children[0].Match(convertedNode.Function);
            if (!result.Item1)
                return Tuple.Create<bool, Node>(false, null);
            binding = AddBindingNode(binding, result.Item2);
            return Tuple.Create(true, binding);
        }
    }
}
