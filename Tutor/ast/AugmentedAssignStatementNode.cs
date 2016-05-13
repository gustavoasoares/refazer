using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython.Compiler.Ast;

namespace Tutor.ast
{
    class AugmentedAssignStatementNode : InternalNode
    {
        public AugmentedAssignStatementNode(Node innerNode, bool isAbstract) : base(innerNode, isAbstract)
        {
        }

        public AugmentedAssignStatementNode(Node innerNode, bool isAbstract, int editId) : base(innerNode, isAbstract, editId)
        {
        }

        protected override Tuple<bool, Node> CompareChildren(Node node, Node binding)
        {
            var convertedNode = (AugmentedAssignStatement)node;
            if (convertedNode == null) return Tuple.Create<bool, Node>(false, null);

            var resultLeft = Children[0].Match(convertedNode.Left);
            var resultRight = Children[1].Match(convertedNode.Right);
            if (resultRight.Item1 && resultLeft.Item1)
            {
                binding = AddBindingNode(binding, resultLeft.Item2);
                binding = AddBindingNode(binding, resultRight.Item2);
                return Tuple.Create(true, binding);
            }
            return Tuple.Create<bool, Node>(false, null);
        }

        protected override bool IsEqualToInnerNode2(Node node)
        {
            var comparedNode = node as AugmentedAssignStatement;
            if (comparedNode == null) return false;
            var inner = (AugmentedAssignStatement)InnerNode;
            return inner.Operator.Equals(comparedNode.Operator);
        }
    }
}
