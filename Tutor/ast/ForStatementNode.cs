using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython.Compiler.Ast;

namespace Tutor.ast
{
    class ForStatementNode : InternalNode
    {
        public ForStatementNode(Node innerNode, bool isAbstract) : base(innerNode, isAbstract)
        {
        }

        public ForStatementNode(Node innerNode, bool isAbstract, int editId) : base(innerNode, isAbstract, editId)
        {
        }

        protected override bool IsEqualToInnerNode(Node node)
        {
            var comparedNode = node as ForStatement;
            if (comparedNode == null) return false;
            return true;
        }

        protected override Tuple<bool, Node> CompareChildren(Node node, Node binding)
        {
            var convertedNode = (ForStatement)node;
            if (convertedNode == null) return Tuple.Create<bool, Node>(false, null);

            var totalChildren = (convertedNode.Else == null) ? 3 : 4;
            if (totalChildren != Children.Count)
                return Tuple.Create<bool, Node>(false, null);
            var result = Children[0].Match(convertedNode.Left);
            if (!result.Item1)
                return Tuple.Create<bool, Node>(false, null);
            binding = AddBindingNode(binding, result.Item2);

            result = Children[1].Match(convertedNode.List);
            if (!result.Item1)
                return Tuple.Create<bool, Node>(false, null);
            binding = AddBindingNode(binding, result.Item2);

            result = Children[2].Match(convertedNode.Body);
            if (!result.Item1)
                return Tuple.Create<bool, Node>(false, null);
            binding = AddBindingNode(binding, result.Item2);

            if (convertedNode.Else != null)
            {
                result = Children[3].Match(convertedNode.Else);
                if (!result.Item1)
                    return Tuple.Create<bool, Node>(false, null);
                binding = AddBindingNode(binding, result.Item2);
            }
            return Tuple.Create<bool, Node>(true, binding);
        }
    }
}
