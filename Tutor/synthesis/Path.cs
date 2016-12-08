using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tutor.synthesis
{
    public class Path
    {
        protected bool Equals(Path other)
        {
            return _k == other._k && _index == other._index && Equals(Token, other.Token);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Path)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = _k;
                hashCode = (hashCode * 397) ^ _index;
                hashCode = (hashCode * 397) ^ (Token != null ? Token.GetHashCode() : 0);
                return hashCode;
            }
        }

        private readonly int _k;
        private int _index;
        public TreeTemplate Token { get; set; }

        public Path(TreeTemplate token, int k)
        {
            _k = k;
            Token = token;
        }

        public Path()
        {
        }

        public virtual bool Match(PythonNode parent, PythonNode node)
        {
            _index = 0;
            foreach (var child in parent.Children)
            {
                if (RecursivelyMatch(child, node))
                {
                    return true;
                }
            }
            return false;
        }

        private bool RecursivelyMatch(PythonNode current, PythonNode node)
        {
            if (Token.Match(current))
            {
                _index++;
                if (_index == _k)
                {
                    return current.Equals(node);
                }
            }
            foreach (var child in current.Children)
            {
                if (RecursivelyMatch(child, node))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
