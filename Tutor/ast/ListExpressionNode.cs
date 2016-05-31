using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython.Compiler.Ast;

namespace Tutor.ast
{
    class ListExpressionNode : InternalNode
    {
        public ListExpressionNode(Node innerNode, bool isAbstract) : base(innerNode, isAbstract)
        {
            InsertStrategy = new InsertFixedList();
        }

        public ListExpressionNode(Node innerNode, bool isAbstract, int editId) : base(innerNode, isAbstract, editId)
        {
            InsertStrategy = new InsertFixedList();
        }

        protected override bool IsEqualToInnerNode(Node node)
        {
            var comparedNode = node as ListExpression;
            if (comparedNode == null) return false;
            var inner = (ListExpression)InnerNode;
            if (inner.Items.Count != comparedNode.Items.Count)
                return false;
            for (var i = 0; i < inner.Items.Count; i++)
            {
                if (inner.Items[i] != comparedNode.Items[i])
                    return false;
            }
            return true;
        }

        public override PythonNode Clone()
        {
            var pythonNode = new ListExpressionNode(InnerNode, IsAbstract, EditId);
            pythonNode.Children = Children;
            pythonNode.Id = Id;
            if (Value != null) pythonNode.Value = Value;
            return pythonNode;
        }
    }
}
