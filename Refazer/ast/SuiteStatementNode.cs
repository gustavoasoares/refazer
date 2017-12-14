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
        public SuiteStatementNode(Node innerNode) : base(innerNode)
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
            var pythonNode = new SuiteStatementNode(InnerNode);
            pythonNode.Children = Children;
            pythonNode.Id = Id;
            if (Value != null) pythonNode.Value = Value;
            return pythonNode;
        }
    }
}
