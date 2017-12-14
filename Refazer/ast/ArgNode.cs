using IronPython.Compiler.Ast;

namespace Tutor.ast
{
    class ArgNode : InternalNode
    {
        public ArgNode(Node innerNode) : base(innerNode)
        {
            InsertStrategy = new InsertFixedList();
        }

        
        protected override bool IsEqualToInnerNode(Node node)
        {
            var comparedNode = node as Arg;
            if (comparedNode == null) return false;
            var inner = (Arg)InnerNode;

            if (inner.Name == null && comparedNode.Name == null)
                return true;
            if (inner.Name == null && comparedNode.Name != null)
                return false;
            if (inner.Name != null && comparedNode.Name == null)
                return false;
            return comparedNode.Name.Equals(inner.Name);
        }

        public override PythonNode Clone()
        {
            var pythonNode = new ArgNode(InnerNode);
            pythonNode.Children = Children;
            pythonNode.Id = Id;
            if (Value != null) pythonNode.Value = Value;
            return pythonNode;
        }
    }
}
