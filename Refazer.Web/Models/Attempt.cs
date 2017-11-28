using System;
using System.Collections.Generic;

namespace Refazer.Web.Models
{
    public class Attempt
    {
        public String EndPoint { get; set; }

        public Boolean PassedTests { get; set; }

        public String SubmittedCode { get; set; }

        public List<String> LogsList { get; set; }

        public List<String> FixedCodeList { get; set; }
    }
}