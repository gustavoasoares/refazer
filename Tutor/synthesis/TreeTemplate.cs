using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tutor.synthesis
{
    public class TreeTemplate
    {
        public bool Target { get; set; }

        public string Type { get; set; }


        public IList<TreeTemplate> Children { get; set;  } = new List<TreeTemplate>();
        public dynamic Value { get; set; }

        public TreeTemplate(string type)
        {
            Type = type;
        }

        protected bool Equals(TreeTemplate other)
        {
            return Target == other.Target && string.Equals(Type, other.Type) && Equals(Children, other.Children) && Equals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TreeTemplate) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Target.GetHashCode();
                hashCode = (hashCode*397) ^ (Type != null ? Type.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Children != null ? Children.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Value != null ? Value.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append(Type);
            if (Value != null)
            {
                sb.Append(", ");
                sb.Append(Value.ToString());
            }
            if (Target)
            {
                sb.Append(", * ");
            }
            Children.ForEach(c => sb.Append(c.ToString()));
            sb.Append("}");
            return sb.ToString();
        }

        public Tuple<bool, int> FindHeightTarget(int height)
        {
            if (Target)
                return Tuple.Create(true, height);
            foreach (var child in Children)
            {
                var result = child.FindHeightTarget(height + 1);
                if (result.Item1)
                    return result;
            }
            return Tuple.Create(false, height);
        }

        public virtual bool Match(PythonNode root)
        {
            if (!root.GetType().Name.Equals(Type))
                return false;
            if (Value == null && root.Value == null)
                return true;
            if (Value == null && root.Value != null)
                return false;
            if (Value != null && root.Value == null)
                return false;
            return Equals(Value, root.Value);
        }

        public IList<PythonNode> Matches(PythonNode inp)
        {
            var visitor = new MatchVisitor(this);
            inp.Walk(visitor);
            return visitor.Matches;
        }

    }

    public class MatchVisitor : IVisitor
    {
        private readonly TreeTemplate _template;

        public List<PythonNode> Matches { set; get; }

        public MatchVisitor(TreeTemplate template)
        {
            _template = template;
            Matches = new List<PythonNode>();
        }

        public bool Visit(PythonNode pythonNode)
        {
            PythonNode target = null;
            if (TryMatch(pythonNode, _template, ref target) && target != null)
            {
                Matches.Add(target);
            }
            return true;
        }

        private bool TryMatch(PythonNode node, TreeTemplate template, ref PythonNode target)
        {
            if (template.Match(node))
            {
                if (template.Target)
                    target = node;

                for (var i = 0; i < template.Children.Count; i++)
                {
                    var child = template.Children[i];
                    if (i < node.Children.Count)
                    {
                        var compared = node.Children[i];
                        var result = TryMatch(compared, child, ref target);
                        if (!result)
                            return false;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            else
            {
                return false;
            }
            return true; 
        }
    }
}
