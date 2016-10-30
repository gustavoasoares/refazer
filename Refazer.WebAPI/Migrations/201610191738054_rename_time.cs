namespace Refazer.WebAPI.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class rename_time : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Fixes", "Time", c => c.DateTime(nullable: false, defaultValueSql: "GETUTCDATE()"));
            AddColumn("dbo.Transformations", "Time", c => c.DateTime(nullable: false, defaultValueSql: "GETUTCDATE()"));
            AddColumn("dbo.Sessions", "Time", c => c.DateTime(nullable: false, defaultValueSql: "GETUTCDATE()"));
            AddColumn("dbo.Submissions", "Time", c => c.DateTime(nullable: false, defaultValueSql: "GETUTCDATE()"));
            DropColumn("dbo.Fixes", "FixTime");
            DropColumn("dbo.Transformations", "TransformationTime");
            DropColumn("dbo.Sessions", "SessionTime");
            DropColumn("dbo.Submissions", "SubmissionTime");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Submissions", "SubmissionTime", c => c.DateTime(nullable: false));
            AddColumn("dbo.Sessions", "SessionTime", c => c.DateTime(nullable: false));
            AddColumn("dbo.Transformations", "TransformationTime", c => c.DateTime(nullable: false));
            AddColumn("dbo.Fixes", "FixTime", c => c.DateTime(nullable: false));
            DropColumn("dbo.Submissions", "Time");
            DropColumn("dbo.Sessions", "Time");
            DropColumn("dbo.Transformations", "Time");
            DropColumn("dbo.Fixes", "Time");
        }
    }
}
