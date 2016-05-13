using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython.Compiler.Ast;

namespace Tutor.ast
{
    class IfStatementNode : InternalNode
    {
        public IfStatementNode(Node innerNode, bool isAbstract) : base(innerNode, isAbstract)
        {
        }

        public IfStatementNode(Node innerNode, bool isAbstract, int editId) : base(innerNode, isAbstract, editId)
        {
        }

        protected override bool IsEqualToInnerNode(Node node)
        {
            var comparedNode = node as IfStatement;
            if (comparedNode == null) return false;
            return true;
        }

        protected override Tuple<bool, Node> CompareChildren(Node node, Node binding)
        {
            var convertedNode = (IfStatement)node;
            if (convertedNode == null) return Tuple.Create<bool, Node>(false, null);

            if (convertedNode.ElseStatement != null)
            {
                if (Children.Count != convertedNode.Tests.Count + 1)
                    return Tuple.Create<bool, Node>(false, null);

                for (var i = 0; i < Children.Count - 1; i++)
                {
                    var result = Children[i].Match(convertedNode.Tests[i]);
                    if (!result.Item1)
                        return Tuple.Create<bool, Node>(false, null);
                    binding = AddBindingNode(binding, result.Item2);
                }
                var result2 = Children.Last().Match(convertedNode.ElseStatement);
                if (!result2.Item1)
                    return Tuple.Create<bool, Node>(false, null);
                binding = AddBindingNode(binding, result2.Item2);
                return Tuple.Create<bool, Node>(true, binding);

            }
            else
            {
                if (Children.Count != convertedNode.Tests.Count)
                    return Tuple.Create<bool, Node>(false, null);
                for (var i = 1; i < Children.Count; i++)
                {
                    var result = Children[i].Match(convertedNode.Tests[i]);
                    if (!result.Item1)
                        return Tuple.Create<bool, Node>(false, null);
                    binding = AddBindingNode(binding, result.Item2);
                }
                return Tuple.Create<bool, Node>(true, binding);
            }
        }
    }
}
