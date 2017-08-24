namespace Refazer.Web.Models
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

        public int SubmissionId { set; get; }

        //id of the current experiment session in the user study
        public int SessionId { set; get; }

        //id of the question for the submissions
        public int QuestionId { set; get; }

        public string Ranking { set; get; }

        public int SynthesizedTransformations { set; get; }
    }
}