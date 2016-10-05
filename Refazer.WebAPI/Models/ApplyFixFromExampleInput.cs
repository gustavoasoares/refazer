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
        public string CodeBefore { set; get; }

        //code after the transformation
        public string CodeAfter { set; get; }

        public string SubmissionId { set; get; }

        //id of the current experiment session in the user study
        public int SessionId { set; get; }

        //id of the question for the submissions
        public int QuestionId { set; get; }
    }
}