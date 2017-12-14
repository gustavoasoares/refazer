using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython.Compiler.Ast;

namespace Tutor.ast
{
    class AugmentedAssignStatementNode : InternalNode
    {
        public AugmentedAssignStatementNode(Node innerNode) : base(innerNode)
        {
            InsertStrategy = new InsertFixedList();
        }

     

        protected override bool IsEqualToInnerNode(Node node)
        {
            var comparedNode = node as AugmentedAssignStatement;
            if (comparedNode == null) return false;
            var inner = (AugmentedAssignStatement)InnerNode;
            return inner.Operator.Equals(comparedNode.Operator);
        }

        public override PythonNode Clone()
        {
            var pythonNode = new AugmentedAssignStatementNode(InnerNode);
            pythonNode.Children = Children;
            pythonNode.Id = Id;
            if (Value != null) pythonNode.Value = Value;
            return pythonNode;
        }
    }
}
