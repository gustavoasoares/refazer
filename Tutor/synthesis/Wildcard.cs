using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tutor.synthesis
{
    public class Wildcard : TreeTemplate
    {

        public Wildcard(string type) : base(type)
        {
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
            if (!root.GetType().Name.Equals(Type))
                return false;
            return true;
        }
    }
}
