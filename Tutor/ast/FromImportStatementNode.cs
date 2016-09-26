using System.Collections.Generic;
using System.Linq;
using CsQuery.ExtensionMethods.Internal;
using IronPython.Compiler.Ast;

namespace Tutor.ast
{
    public class FromImportStatementNode : InternalNode
    {
        public FromImportStatementNode(Node innerNode) : base(innerNode)
        {
            InsertStrategy = new InsertFixedList();
        }

        protected override bool IsEqualToInnerNode(Node node)
        {
            var comparedNode = node as FromImportStatement;
            if (comparedNode == null) return false;
            var inner = (FromImportStatement)InnerNode;
            if (!Equals(comparedNode.Root, inner.Root))
                return false;
            if (inner.Names.IsNullOrEmpty() && comparedNode.Names.IsNullOrEmpty())
                return true;
            if (!inner.Names.IsNullOrEmpty() && !inner.Names.SequenceEqual(comparedNode.Names))
                return false;
            if (!comparedNode.Names.IsNullOrEmpty() && !comparedNode.Names.SequenceEqual(inner.Names))
                return false;
            return true;
        }

        public override PythonNode Clone()
        {
            var pythonNode = new FromImportStatementNode(InnerNode);
            pythonNode.Id = Id;
            if (Value != null) pythonNode.Value = Value;
            return pythonNode;
        }
    }
}