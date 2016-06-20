using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython.Compiler.Ast;

namespace Tutor.ast
{
    class IfStatementNode : InternalNode
    {
        public bool HasElse { set; get; } = false;
        public IfStatementNode(Node innerNode) : base(innerNode)
        {
            InsertStrategy = new InsertNodeInDynamicList();
        }

        protected override bool IsEqualToInnerNode(Node node)
        {
            var comparedNode = node as IfStatement;
            if (comparedNode == null) return false;
            return true;
        }

        public override PythonNode Clone()
        {
            var pythonNode = new IfStatementNode(InnerNode);
            pythonNode.Children = Children;
            pythonNode.Id = Id;
            pythonNode.HasElse = HasElse;
            if (Value != null) pythonNode.Value = Value;
            return pythonNode;
        }
    }
}
