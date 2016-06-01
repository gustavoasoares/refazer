using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython.Compiler.Ast;

namespace Tutor.ast
{
    class ListComprehensionNode : InternalNode
    {
        public ListComprehensionNode(Node innerNode, bool isAbstract) : base(innerNode, isAbstract)
        {
            InsertStrategy = new InsertFixedList();
        }

        public ListComprehensionNode(Node innerNode, bool isAbstract, int editId) : base(innerNode, isAbstract, editId)
        {
            InsertStrategy = new InsertFixedList();
        }

        protected override bool IsEqualToInnerNode(Node node)
        {
            var comparedNode = node as ListComprehension;
            if (comparedNode == null) return false;
            var inner = (ListComprehension)InnerNode;
            if (inner.Item.Equals(comparedNode.Item))
                return false;
            if (inner.Iterators.Count != comparedNode.Iterators.Count)
                return false;
            for (var i = 0; i < inner.Iterators.Count; i++)
            {
                if (inner.Iterators[i] != comparedNode.Iterators[i])
                    return false;
            }
            return true;
        }


        public override PythonNode Clone()
        {
            var pythonNode = new ListComprehensionNode(InnerNode, IsAbstract, EditId);
            pythonNode.Children = Children;
            pythonNode.Id = Id;
            if (Value != null) pythonNode.Value = Value;
            return pythonNode;
        }
    }
}
