using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Refazer.Web.Models
{
    public class TransformationSet
    {
        public Cluster Cluster { get; set; }

        public List<Core.Transformation> TransformationList { get; set; }

    }
}