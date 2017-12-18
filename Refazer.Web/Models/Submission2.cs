using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Refazer.Web.Models
{
    public class Submission2
    {
        [Required]
        public String Question { get; set; }

        [Required]
        public String Code { get; set; }

        [Required]
        public String EndPoint { get; set; }

        [ForeignKey("EndPoint")]
        public Assignment Assignment { get; set; }

        public String KeyPoint()
        {
            return EndPoint + "/" + Question + "/";
        }
    }
}