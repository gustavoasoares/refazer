using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython.Compiler.Ast;

namespace Tutor.ast
{
    internal class FunctionDefinitionNode : InternalNode
    {
        public FunctionDefinitionNode(Node innerNode) : base(innerNode)
        {
            InsertStrategy = new InsertFixedList();
        }

        protected override bool IsEqualToInnerNode(Node node)
        {
            var comparedNode = node as FunctionDefinition;
            if (comparedNode == null) return false;
            var inner = (FunctionDefinition)InnerNode;
            if (inner.Name == null && comparedNode.Name == null)
                return true;
            if (inner.Name == null && comparedNode.Name != null)
                return false;
            if (inner.Name != null && comparedNode.Name == null)
                return false;
            if (!comparedNode.Name.Equals(inner.Name))
                return false;
            if (inner.IsGenerator != comparedNode.IsGenerator)
                return false;
            return inner.IsLambda == comparedNode.IsLambda;
        }

        public override PythonNode Clone()
        {
            var pythonNode = new FunctionDefinitionNode(InnerNode);
            pythonNode.Children = Children;
            pythonNode.Id = Id;
            if (Value != null) pythonNode.Value = Value;
            return pythonNode;
        }
    }
}
