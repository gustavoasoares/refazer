using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Refazer.WebAPI.Models
{
    public class Assignment
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Key]
        public String EndPoint { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        public string TestCases { get; set; }

        public List<string> getTestCasesAsList()
        {
            return TestCases.Split(';').ToList();
        }
    }
}