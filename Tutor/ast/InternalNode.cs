using System;
using System.Collections.Generic;
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

        public override Tuple<bool, Node> Match2(Node node)
        {
            Node matchResult = null;
            if (!MatchInternalNode(node)) return Tuple.Create<bool, Node>(false, null);

            if (EditId != 0)
            {
                matchResult = node;
            }
            return CompareChildren(node, matchResult);
        }
        protected abstract Tuple<bool, Node> CompareChildren(Node node, Node binding);

    }
}
