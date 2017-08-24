using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;

namespace Refazer.Web.Models
{
    /// <summary>
    /// This class represents an experiement session in a user study for evaluation
    /// our grading ui
    /// </summary>
    public class Session
    {
        [Key,DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { set; get; }

        //TODO: add more information about the experiment? 
        [Required, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime Time { set; get; }
    }
}