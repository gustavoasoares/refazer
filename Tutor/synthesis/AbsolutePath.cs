using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tutor.synthesis
{
    class AbsolutePath : Path
    {
        private readonly int _k;

        protected bool Equals(AbsolutePath other)
        {
            return base.Equals(other) && _k == other._k;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AbsolutePath)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ _k;
            }
        }

        public AbsolutePath(int k) : base()
        {
            _k = k;
        }

        public override bool Match(PythonNode parent, PythonNode node)
        {
            return parent.Children.IndexOf(node) == _k - 1;
        }
    }
}
