using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tutor.synthesis
{
    public class Variable : TreeTemplate
    {
        public string Type { get; set; }

        public Variable(string type) : base(type)
        {
            Type = type;
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
