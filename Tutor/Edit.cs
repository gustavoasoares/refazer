using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using IronPython.Compiler.Ast;
using Expression = System.Linq.Expressions.Expression;

namespace Tutor
{
    public abstract class Edit
    {
        public PythonNode ModifiedNode { get; }
        public PythonNode TargetNode { get; set; }

        public Node Context { get; protected set; }

        public Edit()
        {
            
        }

        public Edit(PythonNode modifiedNode, PythonNode targetNode)
        {
            ModifiedNode = modifiedNode;
            TargetNode = targetNode;
        }

        public bool CanApply(Node node)
        {
            var result = TargetNode.Match(node);
            return result.Item1;
        }

        public abstract Node Apply(Node node);
    }


    public class Insert : Edit
    {
        private Dictionary<int, Node> context;
        public int Index { get; set; }

        public InsertNodeSynthesizer NodeSynthesizer { private set; get; }

        public Insert(PythonNode modifiedNode, PythonNode targetNode) : base(modifiedNode, targetNode)
        {
        }

        public Insert(InsertNodeSynthesizer generateBinary, Dictionary<int, Node> context)
        {
            NodeSynthesizer = generateBinary;
            this.context = context;
        }

        public Insert(PythonNode modifiedNode, PythonNode targetNode, int index) : this(modifiedNode, targetNode)
        {
            Index = index;
        }
        

        public override Node Apply(Node node)
        {
            return NodeSynthesizer.GetNode();
        }
    }

    public class Delete : Edit
    {
        public int Pos { get; }

        public Delete(PythonNode modifiedNode, PythonNode targetNode) : base(modifiedNode, targetNode)
        {
        }

        public Delete(PythonNode parent, int k)
        {
            TargetNode = parent;
            Pos = k;
        }

        public override Node Apply(Node node)
        {
            throw new NotImplementedException();
        }
    }

    public class Update : Edit
    {
        public Update(PythonNode modifiedNode, PythonNode targetNode) : base(modifiedNode, targetNode)
        {
        }

        public override Node Apply(Node node)
        {
            return ModifiedNode.InnerNode;
        }

        public override string ToString()
        {
            var value = "?";
            var inner = ModifiedNode.InnerNode as IronPython.Compiler.Ast.ConstantExpression;
            if (inner != null)
                value = inner.Value.ToString();

            return "Rewrite: " + TargetNode.InnerNode.NodeName + " to: " + ModifiedNode.InnerNode.NodeName + "-" + value;
        }
    }
}
