using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsQuery.ExtensionMethods;
using IronPython.Compiler.Ast;
using Tutor.ast;

namespace Tutor
{
    public abstract class PythonNode
    {
        public static int IdCount = 0;

        protected InsertStrategy InsertStrategy { set; get; }

        public int Id { get; set; }

        public dynamic Value { get; set; }

        public Node InnerNode { get;}
        public List<PythonNode> Children { get; set; }
        public PythonNode Parent { get; set; }
        public bool Reference { get; set; }

        public PythonNode(Node innerNode)
        {
            Id = IdCount++;
            InnerNode = innerNode;
            Children = new List<PythonNode>();
            Reference = false;
        }

        public PythonNode Find(int nodeId)
        {
            foreach (var child in Children)
            {
                if (child.Id == nodeId)
                {
                    return child;
                } else
                {
                    child.Find(nodeId);
                }
            }
            return null;
        }

        public void AddChild(PythonNode node)
        {
            Children.Add(node);
        }

        public bool MatchTemplate(PythonNode node)
        {
            if (node.GetType() != GetType())
            {
                return false;
            }
            if (Children.Count != node.Children.Count)
                return false;
            for (var i = 0; i < Children.Count; i++)
            {
                if (!Children[i].MatchTemplate(node.Children[i]))
                    return false;
            }
            return true;
        }


        public abstract Tuple<bool, PythonNode> Match(PythonNode node);


        protected bool MatchInternalNode(Node node)
        {
            if (!IsEqualToInnerNode(node))
                return false;
            return true;
        }

        protected abstract bool IsEqualToInnerNode(Node node);

        protected PythonNode AddBindingNode(PythonNode current, PythonNode matchresult)
        {
            return (matchresult != null && current == null) ? matchresult : current;
        }

        public void PostWalk(SortedTreeVisitor visitor)
        {
            foreach (var child in Children)
            {
                child.PostWalk(visitor);
            }
            visitor.Nodes.Add(this);
        }

        protected bool Equals(PythonNode other)
        {
            return Id == other.Id && Equals(Value, other.Value) && Equals(InnerNode, other.InnerNode) && Equals(Children, other.Children) && Equals(Parent, other.Parent);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PythonNode) obj);
        }


        public PythonNode GetLeftMostDescendant()
        {
            var walker = new SortedTreeVisitor();
            PostWalk(walker);
            if (walker.Nodes.Count == 0)
                throw new Exception("list should not be empty");
            return walker.Nodes.First();
        }

        public string AbstractType()
        {
            return InnerNode.NodeName;
        }

        public override string ToString()
        {
            var str = new StringBuilder();
            str.Append(InnerNode.NodeName);
            str.Append("(");

             if (Value != null)
            {
                str.Append(Value);
            }
            str.Append(")");

            if (Children.Count > 0)
            {
                str.Append("{ ");
                foreach (var child in Children)
                {
                    str.Append(child);
                    str.Append(" ");
                }
                str.Append("}");
            }
            return str.ToString();
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id;
                hashCode = (hashCode*397) ^ (Value != null ? Value.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (InnerNode != null ? InnerNode.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Children != null ? Children.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Parent != null ? Parent.GetHashCode() : 0);
                return hashCode;
            }
        }

        public bool Similar(PythonNode node1)
        {
            var node =  InnerNode.NodeName + ": " + Value;
            var compared = node1.InnerNode.NodeName + ": " + node1.Value;
            return node.Equals(compared);
        }


        public PythonNode GetCopy()
        {
            var type = GetType(); 
            var result = (PythonNode) Activator.CreateInstance(type, InnerNode, false);
            if (Parent != null) result.Parent = Parent;
            result.Value = Value;
            foreach (var child in Children)
            {
                result.AddChild(child.GetCopy());
            }
            return result;
        }

        public PythonNode GetAbstractCopy()
        {
            var type = GetType();
            var result = (PythonNode)Activator.CreateInstance(type, InnerNode, true);
            if (Parent != null) result.Parent = Parent;
            result.Value = Value;
            foreach (var child in Children)
            {
                result.AddChild(child.GetAbstractCopy());
            }
            return result;
        }

        public void Walk(IVisitor visitor)
        {
            if (visitor.Visit(this))
                Children.ForEach(child => child.Walk(visitor));
        }

        public PythonNode Rewrite(IRewriter visitor)
        {
            var result = visitor.Rewrite(this);

            result.Children = result.Children.Select(child => child.Rewrite(visitor)).ToList();
            return result;
        }

        public bool Contains(PythonNode node)
        {
            if (Match(node).Item1)
                return true;

            foreach (var child in Children)
            {
                var contains = child.Contains(node);
                if (contains)
                    return true;
            }
            return false;
        }


        public bool ContainsByBinding(PythonNode node)
        {
            if (Id == node.Id && Equals(Value,node.Value) && GetType() == node.GetType())
                return true;

            foreach (var child in Children)
            {
                var contains = child.ContainsByBinding(node);
                if (contains)
                    return true;
            }
            return false;
        }

      

        public int GetHeight()
        {
            var maxChildHeight = 0; 
            foreach (var child in Children)
            {
                var childHeight = child.GetHeight();
                maxChildHeight = childHeight > maxChildHeight ? childHeight : maxChildHeight;
            }
            return 1 + maxChildHeight;
        }

        public PythonNode GetCorrespondingNodeByBinding(PythonNode node)
        {
            if (Id == node.Id && Equals(Value, node.Value) && GetType() == node.GetType())
                return this;

            foreach (var child in Children)
            {
                var childResult = child.GetCorrespondingNodeByBinding(node);
                if (childResult != null)
                    return childResult;
            }
            return null;
        }

        public PythonNode GetCorrespondingNode(PythonNode node)
        {
            if (Match(node).Item1)
                return this;

            foreach (var child in Children)
            {
                var childResult = child.GetCorrespondingNode(node);
                if (childResult != null)
                    return childResult;
            }
            return null;
        }

        public int CountNodes()
        {
            var value = 1;
            var numberOfNodes = Children.Sum(e => e.CountNodes());
            return value + numberOfNodes;
        }

        public abstract PythonNode Clone();

        public PythonNode CloneTree()
        {
            var result = Clone();
            result.Children = Children.Select(e => e.CloneTree()).ToList();
            return result;
        }

        public virtual void Insert(PythonNode inserted, int index)
        {
            if (InsertStrategy == null)
                throw new TransformationNotApplicableExpection();
            Children = InsertStrategy.Insert(this, inserted, index);
        }
    }

    public interface IVisitor
    {
        bool Visit(PythonNode pythonNode);
    }

    public class SubSequentNodesVisitor : IVisitor
    {
        private readonly IEnumerable<Edit> _operations;
        public List<Edit> SubOperations {get; }

        public SubSequentNodesVisitor(IEnumerable<Edit> operations)
        {
            SubOperations = new List<Edit>();
            _operations = operations;
        }

        public bool Visit(PythonNode pythonNode)
        {
            foreach (var operation in _operations)
            {
                if (pythonNode.Equals(operation.ModifiedNode))
                {
                    SubOperations.Add(operation);
                    return true;
                }
            }
            return false; 
        }
    }


}
