using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython.Compiler.Ast;

namespace Tutor.ast
{
    class MemberExpressionNode : InternalNode
    {
        public MemberExpressionNode(Node innerNode) : base(innerNode)
        {
            InsertStrategy = new InsertFixedList();
        }

        protected override bool IsEqualToInnerNode(Node node)
        {
            var comparedNode = node as MemberExpression;
            if (comparedNode == null) return false;
            var inner = (MemberExpression)InnerNode;
            return inner.Name.Equals(comparedNode.Name);
        }

        public override PythonNode Clone()
        {
            var pythonNode = new MemberExpressionNode(InnerNode);
            pythonNode.Children = Children;
            pythonNode.Id = Id;
            if (Value != null) pythonNode.Value = Value;
            return pythonNode;
        }
    }
}
