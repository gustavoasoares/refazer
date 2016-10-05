using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Refazer.WebAPI.Models
{
    /// <summary>
    /// This class represents an input json for the post request to the controller
    /// api/Refazer/ApplyFixFromExample
    /// </summary>
    public class ApplyFixFromExampleInput
    {
        //code before the trasformation
        public string Before { set; get; }

        //code after the transformation
        public string After { set; get; }

        //id of the current experiment session in the user study
        public int ExperimentId { set; get; }

        //id of the question for the submissions
        public int QuestionId { set; get; }
    }
}