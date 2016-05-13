using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsQuery.ExtensionMethods;
using IronPython.Compiler.Ast;

namespace Tutor
{
    public abstract class PythonNode
    {
        public int EditId { get; set; }

        public string Value { get; set; }

        public bool IsTemplate { get; set; }

        public Node InnerNode { get;}
        public bool IsAbstract { get; }
        public List<PythonNode> Children { get; }
        public PythonNode Parent { get; set; }
        public bool Reference { get; set; }

        public PythonNode(Node innerNode, bool isAbstract)
        {
            IsTemplate = false;
            InnerNode = innerNode;
            IsAbstract = isAbstract;
            Children = new List<PythonNode>();
            Value = "";
            Reference = false;
        }

        public PythonNode(Node innerNode, bool isAbstract, int editId) : this(innerNode, isAbstract)
        {
            EditId = editId;
        }

        public void AddChild(PythonNode node)
        {
            Children.Add(node);
        }


        public abstract Tuple<bool, Node> Match2(Node node);
        

        protected bool MatchInternalNode(Node node)
        {
            if (IsAbstract)
            {
                if (!InnerNode.NodeName.Equals(node.NodeName))
                    return false;
            }
            else
            {
                if (!IsEqualToInnerNode2(node))
                    return false;
            }
            return true;
        }

        protected abstract bool IsEqualToInnerNode2(Node node);


        public Tuple<bool, Node> Match(Node node)
        {
            return Match2(node);
        }

        protected Node AddBindingNode(Node current, Node matchresult)
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
            var isEqual = IsTemplate ? Match(other.InnerNode).Item1 : Equals(InnerNode, other.InnerNode);
            return isEqual && this.IsAbstract == other.IsAbstract && 
                EditId == other.EditId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((PythonNode)obj);
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

            if (IsAbstract)
            {
                str.Append("Abstract"); 
            }
            else
            {
                str.Append(Value);
            }
            if (EditId != 0)
            {
                str.Append(", *"); 
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
                return ((Value != null ? Value.GetHashCode() : 0) * 397) ^ (InnerNode!= null ? InnerNode.GetHashCode() : 0);
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
            var result = (PythonNode) Activator.CreateInstance(type, InnerNode, false, EditId);
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
            var result = (PythonNode)Activator.CreateInstance(type, InnerNode, false, EditId);
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

        public bool Contains(PythonNode node)
        {
            if (Match(node.InnerNode).Item1)
                return true;

            foreach (var child in Children)
            {
                var contains = child.Contains(node);
                if (contains)
                    return true;
            }
            return false;
        }

        public Tuple<bool, int> FindHeightTarget(int height)
        {
            if (EditId == 1)
                return Tuple.Create(true, height);
            foreach (var child in Children)
            {
                var result = child.FindHeightTarget(height + 1);
                if (result.Item1)
                    return result;
            }
            return Tuple.Create(false, height);
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

        public PythonNode GetCorrespondingNode(PythonNode node)
        {
            if (Match(node.InnerNode).Item1)
                return this;

            foreach (var child in Children)
            {
                var childResult = child.GetCorrespondingNode(node);
                if (childResult != null)
                    return childResult;
            }
            return null;
        }

        public int CountAbstract()
        {
            var value = IsAbstract ? 1 : 0;
            var numberOfAbsChildren = Children.Sum(e => e.CountAbstract());
            return value + numberOfAbsChildren; 
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
