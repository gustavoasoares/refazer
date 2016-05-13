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
        public FunctionDefinitionNode(Node innerNode, bool isAbstract) : base(innerNode, isAbstract)
        {
        }

        public FunctionDefinitionNode(Node innerNode, bool isAbstract, int editId) : base(innerNode, isAbstract, editId)
        {
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

        protected override Tuple<bool, Node> CompareChildren(Node node, Node binding)
        {
            var convertedNode = (FunctionDefinition)node;
            if (convertedNode == null) return Tuple.Create<bool, Node>(false, null);

            var currentChild = 0;
            if (convertedNode.Decorators != null && convertedNode.Decorators.Any())
            {
                foreach (var decoretor in convertedNode.Decorators)
                {
                    var result = Children[currentChild].Match(decoretor);
                    if (!result.Item1)
                        return Tuple.Create<bool, Node>(false, null);
                    binding = AddBindingNode(binding, result.Item2);
                    currentChild++;
                }
            }
            if (convertedNode.Parameters != null && convertedNode.Parameters.Any())
            {
                foreach (var parameter in convertedNode.Parameters)
                {
                    var result = Children[currentChild].Match(parameter);
                    if (!result.Item1)
                        return Tuple.Create<bool, Node>(false, null);
                    binding = AddBindingNode(binding, result.Item2);
                    currentChild++;
                }
            }
            var result2 = Children[currentChild].Match(convertedNode.Body);
            if (!result2.Item1)
                return Tuple.Create<bool, Node>(false, null);
            binding = AddBindingNode(binding, result2.Item2);
            return Tuple.Create(true, binding);
        }
    }
}
