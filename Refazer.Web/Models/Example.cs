using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Refazer.WebAPI.Models
{
    public class Example
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public String Question { get; set; }

        [Required]
        public String IncorrectCode { get; set; }

        [Required]
        public String CorrectCode { get; set; }

        [Required]
        public String EndPoint { get; set; }

        [ForeignKey("EndPoint")]
        public Assignment assignment { get; set; }
    }
}