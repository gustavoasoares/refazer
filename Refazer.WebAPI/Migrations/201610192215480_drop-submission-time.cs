namespace Refazer.WebAPI.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class dropsubmissiontime : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.Submissions", "Time");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Submissions", "Time", c => c.DateTime(nullable: false));
        }
    }
}
