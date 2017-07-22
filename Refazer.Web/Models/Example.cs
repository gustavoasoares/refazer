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

        public String Question { get; set; }

        public String IncorrectCode { get; set; }

        public String CorrectCode { get; set; }

        public String EndPoint { get; set; }

        [ForeignKey("EndPoint")]
        public Assignment assignment { get; set; }
    }
}