using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython.Compiler.Ast;

namespace Tutor.ast
{
    class ExpressionStatementNode : InternalNode
    {
        public string Documentation { set; get; }

        public ExpressionStatementNode(Node innerNode) : base(innerNode)
        {
            InsertStrategy = new InsertFixedList();
        }

        protected override bool IsEqualToInnerNode(Node node)
        {
            var comparedNode = node as ExpressionStatement;
            if (comparedNode == null) return false;
            return true;
        }

        public override PythonNode Clone()
        {
            var pythonNode = new ExpressionStatementNode(InnerNode);
            pythonNode.Children = Children;
            pythonNode.Id = Id;
            pythonNode.Documentation = Documentation;
            if (Value != null) pythonNode.Value = Value;
            return pythonNode;
        }
    }
}
