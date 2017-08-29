using System.Data.Entity;

namespace Refazer.Web.Models
{
    public class RefazerDbContext : DbContext
    {
        public DbSet<Session> Sessions { set; get; }

        public DbSet<Submission> Submissions { get; set; }

        public DbSet<Fix> Fixes { set; get; }

        public DbSet<Transformation> Transformations { set; get; }

        public DbSet<Assignment> Assignments { set; get; }

        public DbSet<Example> Examples { set; get; }

        public DbSet<Cluster> Clusters { set; get; }
    }
}