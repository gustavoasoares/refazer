using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Refazer.Web.Models
{
    public class Cluster
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public String KeyPoint { get; set; }

        [Required]
        public String ExamplesReferenceStr { get; set; }

        public List<int> ExamplesReferenceList;

        public void AddExampleReference(int exampleId)
        {
            if (ExamplesReferenceList == null)
            {
                ExamplesReferenceList = new List<int>();
            }

            ExamplesReferenceList.Add(exampleId);
            ExamplesReferenceStr = string.Join(",", ExamplesReferenceList);
        }
    }
}