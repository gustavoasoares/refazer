using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython.Compiler.Ast;

namespace Tutor.ast
{
    class TupleExpressionNode : InternalNode
    {
        public TupleExpressionNode(Node innerNode) : base(innerNode)
        {
            InsertStrategy = new InsertNodeInDynamicList();
        }

        protected override bool IsEqualToInnerNode(Node node)
        {
            var comparedNode = node as TupleExpression;
            if (comparedNode == null) return false;
            return true;
        }

        public override PythonNode Clone()
        {
            var pythonNode = new TupleExpressionNode(InnerNode);
            pythonNode.Children = Children;
            pythonNode.Id = Id;
            if (Value != null) pythonNode.Value = Value;
            return pythonNode;
        }
    }
}
