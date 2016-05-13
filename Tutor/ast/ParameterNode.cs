using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython.Compiler.Ast;

namespace Tutor.ast
{
    class ParameterNode : InternalNode
    {
        public ParameterNode(Node innerNode, bool isAbstract) : base(innerNode, isAbstract)
        {
        }

        public ParameterNode(Node innerNode, bool isAbstract, int editId) : base(innerNode, isAbstract, editId)
        {
        }

        protected override Tuple<bool, Node> CompareChildren(Node node, Node binding)
        {
            var convertedNode = (Parameter)node;
            if (convertedNode == null) return Tuple.Create<bool, Node>(false, null);

            if (Children.Count != 1 && convertedNode.DefaultValue != null)
                return Tuple.Create<bool, Node>(false, null);
            if (Children.Count != 0 && convertedNode.DefaultValue == null)
                return Tuple.Create<bool, Node>(false, null);

            if (Children.Any())
            {
                var result = Children[0].Match(convertedNode.DefaultValue);
                if (!result.Item1)
                    return Tuple.Create<bool, Node>(false, null);
                binding = AddBindingNode(binding, result.Item2);
            }
            return Tuple.Create(true, binding);
        }

        protected override bool IsEqualToInnerNode(Node node)
        {
            var comparedNode = node as Parameter;
            if (comparedNode == null) return false;
            var inner = (Parameter)InnerNode;

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
