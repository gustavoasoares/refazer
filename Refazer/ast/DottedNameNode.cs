using System.Linq;
using CsQuery.ExtensionMethods.Internal;
using IronPython.Compiler.Ast;

namespace Tutor.ast
{
    public class DottedNameNode : LeafNode
    {
        public DottedNameNode(Node innerNode) : base(innerNode)
        {
            InsertStrategy = new InsertFixedList();
        }

        protected override bool IsEqualToInnerNode(Node node)
        {
            var comparedNode = node as DottedName;
            if (comparedNode == null) return false;
            var inner = (DottedName)InnerNode;
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
            var pythonNode = new DottedNameNode(InnerNode);
            pythonNode.Id = Id;
            if (Value != null) pythonNode.Value = Value;
            return pythonNode;
        }
    }
}