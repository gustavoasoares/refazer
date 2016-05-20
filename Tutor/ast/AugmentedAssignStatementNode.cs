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
        public AugmentedAssignStatementNode(Node innerNode, bool isAbstract) : base(innerNode, isAbstract)
        {
        }

        public AugmentedAssignStatementNode(Node innerNode, bool isAbstract, int editId) : base(innerNode, isAbstract, editId)
        {
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
            var pythonNode = new AugmentedAssignStatementNode(InnerNode, IsAbstract, EditId);
            pythonNode.Children = Children;
            pythonNode.Id = Id;
            if (Value != null) pythonNode.Value = Value;
            return pythonNode;
        }
    }
}
