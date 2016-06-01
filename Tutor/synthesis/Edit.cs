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

        public Boolean Applied { get; set; }

        public Node Context { get; protected set; }

        public Edit()
        {
            Applied = false;
        }

        public Edit(PythonNode modifiedNode, PythonNode targetNode)
        {
            ModifiedNode = modifiedNode;
            TargetNode = targetNode;
        }


        public bool CanApply2(PythonNode node)
        {
            if (Applied)
                return false;

            if (node.Id == TargetNode.Id)
            {
                Applied = true;
                return true;
            }
            return false;
        }

        public bool CanApply(Node node)
        {
            return false;
        }

        public abstract PythonNode Apply(PythonNode node);
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
        

        public override PythonNode Apply(PythonNode node)
        {
            node.Insert(ModifiedNode.CloneTree(), Index);            
            return node;
        }
    }

    public class Move : Edit
    {
        private Dictionary<int, Node> context;
        public int Index { get; set; }

        public InsertNodeSynthesizer NodeSynthesizer { private set; get; }

        public Move(PythonNode modifiedNode, PythonNode targetNode) : base(modifiedNode, targetNode)
        {
        }

        public Move(InsertNodeSynthesizer generateBinary, Dictionary<int, Node> context)
        {
            NodeSynthesizer = generateBinary;
            this.context = context;
        }

        public Move(PythonNode modifiedNode, PythonNode targetNode, int index) : this(modifiedNode, targetNode)
        {
            Index = index;
        }


        public override PythonNode Apply(PythonNode node)
        {
            node.Insert(ModifiedNode.CloneTree(), Index);
            return node;
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

        public override PythonNode Apply(PythonNode node)
        {
            var newList = new List<PythonNode>();
            newList.AddRange(node.Children);
            node.Children = newList.Where(e => e.Id != ModifiedNode.Id).ToList();
            return node;
        }
    }

    public class Update : Edit
    {
        public Update(PythonNode modifiedNode, PythonNode targetNode) : base(modifiedNode, targetNode)
        {
        }

        public override PythonNode Apply(PythonNode node)
        {
            return ModifiedNode;
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
