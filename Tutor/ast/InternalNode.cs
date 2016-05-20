using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython.Compiler.Ast;

namespace Tutor.ast
{
    public abstract class InternalNode :  PythonNode
    {
        public InternalNode(Node innerNode, bool isAbstract) : base(innerNode, isAbstract)
        {
        }

        public InternalNode(Node innerNode, bool isAbstract, int editId) : base(innerNode, isAbstract, editId)
        {
        }

        public override Tuple<bool, PythonNode> Match(PythonNode node)
        {
            PythonNode matchResult = null;
            if (!MatchInternalNode(node.InnerNode)) return Tuple.Create<bool, PythonNode>(false, null);

            if (EditId != 0)
            {
                matchResult = node;
            }
            return CompareChildren(node, matchResult);
        }

        protected Tuple<bool, PythonNode> CompareChildren(PythonNode node, PythonNode binding)
        {
            if (Children.Count != node.Children.Count)
                return Tuple.Create<bool, PythonNode>(false, null);

            for (var i = 0; i < Children.Count; i++)
            {
                var childResult = Children[i].Match(node.Children[i]);
                if (!childResult.Item1)
                    return Tuple.Create<bool, PythonNode>(false, null);
                binding = AddBindingNode(binding, childResult.Item2);
            }
            return Tuple.Create(true, binding);
        }
    }
}
