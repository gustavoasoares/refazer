using System.Collections.Generic;

namespace Refazer.Web.Models
{
    public class RefazerInput
    {
        public List<Dictionary<string, object>> submissions { set; get; }

        public IEnumerable<Dictionary<string, string>> Examples { set; get; }
    }
}