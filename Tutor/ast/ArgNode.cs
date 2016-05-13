using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython.Compiler.Ast;

namespace Tutor.ast
{
    class ArgNode : InternalNode
    {
        public ArgNode(Node innerNode, bool isAbstract) : base(innerNode, isAbstract)
        {
        }

        public ArgNode(Node innerNode, bool isAbstract, int editId) : base(innerNode, isAbstract, editId)
        {
        }

        protected override Tuple<bool, Node> CompareChildren(Node node, Node binding)
        {
            var convertedNode = (Arg)node;
            if (convertedNode == null) return Tuple.Create<bool, Node>(false, null);

            if (Children.Count != 1)
                return Tuple.Create<bool, Node>(false, null);

            var result = Children[0].Match(convertedNode.Expression);
            if (!result.Item1)
                return Tuple.Create<bool, Node>(false, null);
            binding = AddBindingNode(binding, result.Item2);
            return Tuple.Create<bool, Node>(true, binding);
        }

        protected override bool IsEqualToInnerNode2(Node node)
        {
            var comparedNode = node as Arg;
            if (comparedNode == null) return false;
            var inner = (Arg)InnerNode;

            if (inner.Name == null && comparedNode.Name == null)
                return true;
            if (inner.Name == null && comparedNode.Name != null)
                return false;
            if (inner.Name != null && comparedNode.Name == null)
                return false;
            return comparedNode.Name.Equals(inner.Name);
        }
    }
}
