using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Refazer.Web.Models
{
    /// <summary>
    /// This class represents a student submission 
    /// </summary>
    public class Submission
    {
        //id of the submission
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { set; get; }
        //Id of the question for the submission

        [Required]
        public int QuestionId { set; get; }
        //Id of the grading session of the submission, which should be generated 
        //by refazer

        public int SubmissionId { set; get; }

        [Required]
        public int SessionId { set; get; }

        [Required]
        //student's code for the submission
        public string Code { set; get; }

        public bool IsFixed { set; get; }

    }
}