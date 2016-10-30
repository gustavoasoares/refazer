namespace Refazer.WebAPI.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class add_time : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Fixes", "FixTime", c => c.DateTime(nullable: false));
            AddColumn("dbo.Transformations", "TransformationTime", c => c.DateTime(nullable: false));
            AddColumn("dbo.Sessions", "SessionTime", c => c.DateTime(nullable: false));
            AddColumn("dbo.Submissions", "IsFixed", c => c.String());
            AddColumn("dbo.Submissions", "SubmissionTime", c => c.DateTime(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Submissions", "SubmissionTime");
            DropColumn("dbo.Submissions", "IsFixed");
            DropColumn("dbo.Sessions", "SessionTime");
            DropColumn("dbo.Transformations", "TransformationTime");
            DropColumn("dbo.Fixes", "FixTime");
        }
    }
}
