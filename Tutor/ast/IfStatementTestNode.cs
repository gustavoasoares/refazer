using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython.Compiler.Ast;

namespace Tutor.ast
{
    class IfStatementTestNode : InternalNode
    {
        public IfStatementTestNode(Node innerNode, bool isAbstract) : base(innerNode, isAbstract)
        {
            InsertStrategy = new InsertFixedList();
        }

        public IfStatementTestNode(Node innerNode, bool isAbstract, int editId) : base(innerNode, isAbstract, editId)
        {
            InsertStrategy = new InsertFixedList();
        }

        protected override bool IsEqualToInnerNode(Node node)
        {
            var comparedNode = node as IfStatementTest;
            if (comparedNode == null) return false;
            return true;
        }

        public override PythonNode Clone()
        {
            var pythonNode = new IfStatementTestNode(InnerNode, IsAbstract, EditId);
            pythonNode.Children = Children;
            pythonNode.Id = Id;
            if (Value != null) pythonNode.Value = Value;
            return pythonNode;
        }

    }
}
