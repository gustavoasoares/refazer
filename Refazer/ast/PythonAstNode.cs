using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython.Compiler.Ast;

namespace Tutor.ast
{
    class PythonAstNode : InternalNode
    {
        public PythonAstNode(Node innerNode) : base(innerNode)
        {
            InsertStrategy = new InsertFixedList();
        }

        protected override bool IsEqualToInnerNode(Node node)
        {
            var comparedNode = node as PythonAst;
            if (comparedNode == null) return false;
            return true;
        }

        public override PythonNode Clone()
        {
            var pythonNode = new PythonAstNode(InnerNode);
            pythonNode.Children = Children;
            pythonNode.Id = Id;
            if (Value != null) pythonNode.Value = Value;
            return pythonNode;
        }
    }
}
