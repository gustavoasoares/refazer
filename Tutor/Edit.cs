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
        public PythonNode NewNode { get; }
        public PythonNode Target { get; }

        public Node Context { get; protected set; }

        public Edit()
        {
            
        }

        public Edit(PythonNode newNode, PythonNode target)
        {
            NewNode = newNode;
            Target = target;
        }

        public abstract Expression Run(Node ast);

        public bool CanApply(Node node)
        {
            return node.Equals(Target.InnerNode);
        }

        public abstract Node Apply(Node node);
    }


    public class Insert : Edit
    {
        private Dictionary<int, Node> context;
        public int Index { get; }

        public InsertNodeSynthesizer NodeSynthesizer { private set; get; }

        public Insert(PythonNode newNode, PythonNode target) : base(newNode, target)
        {
        }

        public Insert(InsertNodeSynthesizer generateBinary, Dictionary<int, Node> context)
        {
            NodeSynthesizer = generateBinary;
            this.context = context;
        }

        public Insert(PythonNode newNode, PythonNode target, int index) : this(newNode, target)
        {
            Index = index;
        }

        public override Expression Run(Node ast)
        {
            var rewriter = new InsertRewriter(this);
            return rewriter.Rewrite(ast);
        }

        public override Node Apply(Node node)
        {
            return NodeSynthesizer.GetNode();
        }
    }

    public class Delete : Edit
    {
        public Delete(PythonNode newNode, PythonNode target) : base(newNode, target)
        {
        }

        public override Expression Run(Node ast)
        {
            throw new NotImplementedException();
        }

        public override Node Apply(Node node)
        {
            throw new NotImplementedException();
        }
    }

    public class Update : Edit
    {
        public Update(PythonNode newNode, PythonNode target) : base(newNode, target)
        {
        }

        public override Expression Run(Node ast)
        {
            var rewriter = new Rewriter(this);
            return rewriter.Rewrite(ast);
        }

        public override Node Apply(Node node)
        {
            return NewNode.InnerNode;
        }

        public override string ToString()
        {
            var value = "?";
            var inner = NewNode.InnerNode as IronPython.Compiler.Ast.ConstantExpression;
            if (inner != null)
                value = inner.Value.ToString();

            return "Rewrite: " + Target.InnerNode.NodeName + " to: " + NewNode.InnerNode.NodeName + "-" + value;
        }
    }
}
