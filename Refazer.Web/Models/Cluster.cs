using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

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

        private List<int> ExamplesReferenceList;

        public void AddExampleReference(int exampleId)
        {
            if (ExamplesReferenceList == null)
            {
                ExamplesReferenceList = new List<int>();
            }

            ExamplesReferenceList.Add(exampleId);
            ExamplesReferenceStr = string.Join(",", ExamplesReferenceList);
        }

        public List<int> GetExamplesReferenceList()
        {
            return ExamplesReferenceStr.Split(',').Select(Int32.Parse).ToList();
        }
    }
}