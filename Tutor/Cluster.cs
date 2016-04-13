﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tutor
{
    public class Cluster
    {
        public String TestCase { set; get; }
        public List<Mistake> Mistakes { set; get; } 
    }

    public class Mistake
    {
        public String diff { set; get; }
        public string before { set; get; }
        public string after { set; get; }

        public List<String> failed { set; get; }
        
    }

    
}
