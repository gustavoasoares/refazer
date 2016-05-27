using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython.Compiler.Ast;

namespace Tutor.ast
{
    class SuiteStatementNode : InternalNode
    {
        public SuiteStatementNode(Node innerNode, bool isAbstract) : base(innerNode, isAbstract)
        {
            InsertStrategy = new InsertNodeInDynamicList();
        }

        public SuiteStatementNode(Node innerNode, bool isAbstract, int editId) : base(innerNode, isAbstract, editId)
        {
            InsertStrategy = new InsertNodeInDynamicList();
        }

        protected override bool IsEqualToInnerNode(Node node)
        {
            var comparedNode = node as SuiteStatement;
            if (comparedNode == null) return false;
            return true;
        }

        public override PythonNode Clone()
        {
            var pythonNode = new SuiteStatementNode(InnerNode, IsAbstract, EditId);
            pythonNode.Children = Children;
            pythonNode.Id = Id;
            if (Value != null) pythonNode.Value = Value;
            return pythonNode;
        }
    }
}
