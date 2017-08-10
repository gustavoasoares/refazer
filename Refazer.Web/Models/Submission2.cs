using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Refazer.WebAPI.Models
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
    }
}