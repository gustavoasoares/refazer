using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython.Compiler.Ast;

namespace Tutor.ast
{
    class AssignmentStatementNode : InternalNode
    {
        public AssignmentStatementNode(Node innerNode, bool isAbstract) : base(innerNode, isAbstract)
        {
        }

        public AssignmentStatementNode(Node innerNode, bool isAbstract, int editId) : base(innerNode, isAbstract, editId)
        {
        }

        protected override Tuple<bool, Node> CompareChildren(Node node, Node binding)
        {
            var convertedNode = node as AssignmentStatement;
            if (convertedNode == null) return Tuple.Create<bool, Node>(false, null);

            //if the number of expressions in the left side are different from 
            //the number of children - 1 (the last one is for the right side),
            //the tree is different 
            if (convertedNode.Left.Count != Children.Count - 1)
                return Tuple.Create<bool, Node>(false, null);

            for (var i = 0; i < Children.Count - 1; i++)
            {
                var result = Children[i].Match(convertedNode.Left[i]);
                if (!result.Item1)
                    return Tuple.Create<bool, Node>(false, null);
                binding = AddBindingNode(binding, result.Item2);
            }
            var resultRight = Children.Last().Match(convertedNode.Right);
            if (resultRight.Item1)
            {
                binding = AddBindingNode(binding, resultRight.Item2);
                return Tuple.Create(true, binding);
            }
            return Tuple.Create<bool, Node>(false, null);
        }

        protected override bool IsEqualToInnerNode2(Node node)
        {
            var comparedNode = node as AssignmentStatement;
            if (comparedNode == null) return false;
            return true;
        }
    }
}
