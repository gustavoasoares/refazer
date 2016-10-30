namespace Refazer.WebAPI.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class utc_to_pst : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.Fixes", "Time");
            DropColumn("dbo.Transformations", "Time");
            DropColumn("dbo.Sessions", "Time");
            DropColumn("dbo.Submissions", "Time");
            AddColumn("dbo.Fixes", "Time", c => c.DateTime(nullable: false, defaultValueSql: "GETDATE()"));
            AddColumn("dbo.Transformations", "Time", c => c.DateTime(nullable: false, defaultValueSql: "GETDATE()"));
            AddColumn("dbo.Sessions", "Time", c => c.DateTime(nullable: false, defaultValueSql: "GETDATE()"));
            AddColumn("dbo.Submissions", "Time", c => c.DateTime(nullable: false, defaultValueSql: "GETDATE()"));
            
        }
        
        public override void Down()
        {
            DropColumn("dbo.Fixes", "Time");
            DropColumn("dbo.Transformations", "Time");
            DropColumn("dbo.Sessions", "Time");
            DropColumn("dbo.Submissions", "Time");
            AddColumn("dbo.Fixes", "Time", c => c.DateTime(nullable: false, defaultValueSql: "GETUTCDATE()"));
            AddColumn("dbo.Transformations", "Time", c => c.DateTime(nullable: false, defaultValueSql: "GETUTCDATE()"));
            AddColumn("dbo.Sessions", "Time", c => c.DateTime(nullable: false, defaultValueSql: "GETUTCDATE()"));
            AddColumn("dbo.Submissions", "Time", c => c.DateTime(nullable: false, defaultValueSql: "GETUTCDATE()"));
        }
    }
}
