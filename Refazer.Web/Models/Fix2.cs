using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Refazer.Web.Models
{
    public class Fix2
    {
        public Cluster Cluster { get; set; }

        public List<String> FixedCodeList { get; set; }

        public Fix2() { }

        public Fix2(Cluster Cluster, List<String> FixedCodeList)
        {
            this.Cluster = Cluster;
            this.FixedCodeList = FixedCodeList;
        }
    }
}