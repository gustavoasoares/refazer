using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IronPython.Modules;

namespace Tutor.synthesis
{
    public class Wildcard : TreeTemplate
    {
        public bool IsWildCard { set; get; }

        public bool IsLeaf { set; get; }

        public Wildcard(string type, bool isLeaf = false) : base(type)
        {
            IsWildCard = true;
            IsLeaf = isLeaf;
        }

        protected bool Equals(Wildcard other)
        {
            return base.Equals(other) && IsWildCard == other.IsWildCard && IsLeaf == other.IsLeaf;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Wildcard) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode*397) ^ IsWildCard.GetHashCode();
                hashCode = (hashCode*397) ^ IsLeaf.GetHashCode();
                return hashCode;
            }
        }

        public Wildcard(string type, IEnumerable<TreeTemplate> children) : base(type)
        {
            Children = children.ToList();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("{$");
            sb.Append(Type);
            if (Target)
            {
                sb.Append(", * ");
            }
            Children.ForEach(c => sb.Append(c.ToString()));
            sb.Append("}");
            return sb.ToString();
        }

        public override bool Match(PythonNode root)
        {
            if (Type.Equals("any"))
                return true;
            if (!root.GetType().Name.Equals(Type))
                return false;
            return true;
        }
    }
}
