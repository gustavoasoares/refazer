using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Refazer.WebAPI.Models
{
    public class RefazerInput
    {
        public List<Dictionary<string, object>> submissions { set; get; }

        public IEnumerable<Dictionary<string, string>> Examples { set; get; }
    }

    public class Submission
    {
        
    }
}