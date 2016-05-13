using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython.Compiler.Ast;

namespace Tutor.ast
{
    class WhileStatementNode : InternalNode
    {
        public WhileStatementNode(Node innerNode, bool isAbstract) : base(innerNode, isAbstract)
        {
        }

        public WhileStatementNode(Node innerNode, bool isAbstract, int editId) : base(innerNode, isAbstract, editId)
        {
        }

        protected override Tuple<bool, Node> CompareChildren(Node node, Node binding)
        {
            var convertedNode = (WhileStatement)node;
            if (convertedNode == null) return Tuple.Create<bool, Node>(false, null);

            var totalChildren = (convertedNode.ElseStatement == null) ? 2 : 3;
            if (totalChildren != Children.Count)
                return Tuple.Create<bool, Node>(false, null);
            var result = Children[0].Match(convertedNode.Test);
            if (!result.Item1)
                return Tuple.Create<bool, Node>(false, null);
            binding = AddBindingNode(binding, result.Item2);

            result = Children[1].Match(convertedNode.Body);
            if (!result.Item1)
                return Tuple.Create<bool, Node>(false, null);
            binding = AddBindingNode(binding, result.Item2);

            if (convertedNode.ElseStatement != null)
            {
                result = Children[2].Match(convertedNode.ElseStatement);
                if (!result.Item1)
                    return Tuple.Create<bool, Node>(false, null);
                binding = AddBindingNode(binding, result.Item2);
            }
            return Tuple.Create(true, binding);
        }

        protected override bool IsEqualToInnerNode2(Node node)
        {
            var comparedNode = node as WhileStatement;
            if (comparedNode == null) return false;
            return true;
        }
    }
}
