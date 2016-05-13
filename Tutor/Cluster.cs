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
