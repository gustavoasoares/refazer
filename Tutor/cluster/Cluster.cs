using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ProgramSynthesis.AST;

namespace Tutor
{
    public class Cluster
    {
        public String TestCase { set; get; }
        public List<Mistake> Mistakes { set; get; } 
    }

    public class Mistake
    {
        protected bool Equals(Mistake other)
        {
            return string.Equals(diff, other.diff) && string.Equals(before, other.before) && string.Equals(after, other.after) && Equals(failed, other.failed);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Mistake) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (diff != null ? diff.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (before != null ? before.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (after != null ? after.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (failed != null ? failed.GetHashCode() : 0);
                return hashCode;
            }
        }

        public int Id { set; get; }
        public String diff { set; get; }
        public string before { set; get; }
        public string after { set; get; }

        public long Time { get; set; }
        public string SynthesizedAfter { set; get; }

        public bool IsFixed { set; get; } = false;

        public string GeneratedFix { set; get; }

        public string UsedFix { set; get;  }
        public List<String> failed { set; get; }
        
    }

    
}
