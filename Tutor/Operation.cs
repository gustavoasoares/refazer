using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using CsQuery;
using IronPython.Compiler.Ast;
using IronPython.Runtime;
using BinaryExpression = IronPython.Compiler.Ast.BinaryExpression;
using Expression = System.Linq.Expressions.Expression;

namespace Tutor
{
    public abstract class Operation
    {
        public PythonNode NewNode { get; }
        public PythonNode Target { get; }

        public Node Context { get; protected set; }

        public Operation()
        {
            
        }
        public Operation(PythonNode newNode, PythonNode target)
        {
            NewNode = newNode;
            Target = target;
        }

        public abstract Expression Run(Node code, Node context);

        public bool CanApply(Node node)
        {
            return node.Equals(Context);
        }

        public abstract Node Apply(Node node);
    }


    public class Insert : Operation
    {
        private Dictionary<int, Node> context;

        public InsertNodeSynthesizer NodeSynthesizer { private set; get; }
                 
        public Insert(PythonNode newNode, PythonNode target) : base(newNode, target)
        {
        }

        public Insert(InsertNodeSynthesizer generateBinary, Dictionary<int, Node> context)
        {
            NodeSynthesizer = generateBinary;
            this.context = context;
        }

        public override Expression Run(Node code, Node context)
        {
            Context = context;
            var rewriter = new Rewriter(this);
            return rewriter.Rewrite(code);
        }

        public override Node Apply(Node node)
        {
            return NodeSynthesizer.GetNode();
        }
    }

    public class Delete : Operation
    {
        public Delete(PythonNode newNode, PythonNode target) : base(newNode, target)
        {
        }

        public override Expression Run(Node code, Node context)
        {
            throw new NotImplementedException();
        }

        public override Node Apply(Node node)
        {
            throw new NotImplementedException();
        }
    }
    public class Update : Operation
    {
        public Update(PythonNode newNode, PythonNode target) : base(newNode, target)
        {
        }

        public override Expression Run(Node code, Node context)
        {
            Context = context;
            var rewriter = new Rewriter(this);
            return rewriter.Rewrite(code);
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
