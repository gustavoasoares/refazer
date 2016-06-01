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
            InsertStrategy = new InsertFixedList();
        }

        public AssignmentStatementNode(Node innerNode, bool isAbstract, int editId) : base(innerNode, isAbstract, editId)
        {
            InsertStrategy = new InsertFixedList();
        }

        protected override bool IsEqualToInnerNode(Node node)
        {
            var comparedNode = node as AssignmentStatement;
            if (comparedNode == null) return false;
            return true;
        }

        public override PythonNode Clone()
        {
            var pythonNode = new AssignmentStatementNode(InnerNode, IsAbstract, EditId);
            pythonNode.Children = Children;
            if (Value != null) pythonNode.Value = Value;
            pythonNode.Id = Id;
            return pythonNode;
        }
    }
}
