using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace Refazer.WebAPI.Models
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





    //public class SessionDbContext : DbContext
    //{
    //    public DbSet<Session> Sessions { set; get; }
    //}

    public class RefazerDbContext : DbContext
    {
        public DbSet<Session> Sessions { set; get; }
        public DbSet<Submission> Submissions { get; set; }
        public DbSet<Fix> Fixes { set; get; }

        public DbSet<Transformation> Transformations { set; get; }
    }

}