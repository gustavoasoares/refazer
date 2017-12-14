using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython.Compiler;
using IronPython.Compiler.Ast;

namespace Tutor
{
    public interface INodeSynthesizer
    {
        Node GetNode();
    }

    public class BindingNodeSynthesizer : INodeSynthesizer
    {
        private readonly int _bindingId;
        public Dictionary<int, Node> Bindings { set; get; }

        public BindingNodeSynthesizer(int bindingId)
        {
            _bindingId = bindingId;
        }

        public Node GetNode()
        {
            var node = Bindings[_bindingId];
            switch (node.NodeName)
            {
                case "NameExpression":
                    return new NameExpression(((NameExpression)node).Name);
            }
            throw new NotImplementedException(node.NodeName);
        }
    }

    public class InsertNodeSynthesizer : INodeSynthesizer
    {
        public string NodeType { private set; get; }

        public dynamic Value { private set; get; }
        public List<INodeSynthesizer> Children { private set; get; }


        public InsertNodeSynthesizer(string nodeType, dynamic value, List<INodeSynthesizer> children)
        {
            NodeType = nodeType;
            Value = value;
            Children = children;
        }

        public InsertNodeSynthesizer(string nodeType, dynamic value)
        {
            NodeType = nodeType;
            Value = value;
            Children = new List<INodeSynthesizer>();
        }

        public Node GetNode()
        {
            switch (NodeType)
            {
                case "NameExpression": 
                    return new NameExpression(Value);
                case "CallExpression":
                    var target = (Expression)Children[0].GetNode();
                    var args = new List<Arg>();
                    if (Children.Count > 1)
                    {
                        for (var i = 1; i < Children.Count; i++)
                        {
                            args.Add((Arg) Children[i].GetNode());
                        }
                    }
                    return new CallExpression(target, args.ToArray());
                case "Arg":
                    var expression = (Expression) Children[0].GetNode();
                    return (Value == null) ? new Arg(expression) :
                        new Arg(Value, expression);
                case "BinaryExpression":

                    var left = (Expression) Children[0].GetNode();
                    var right = (Expression) Children[1].GetNode();
                    PythonOperator op = Value;
                    return new BinaryExpression(op, left, right);
                default:
                    throw new NotImplementedException(NodeType);
            }
        }
    }
}
