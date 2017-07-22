namespace Refazer.Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class assignmentkey : DbMigration
    {
        public override void Up()
        {
            DropPrimaryKey("dbo.Assignments");
            AlterColumn("dbo.Assignments", "EndPoint", c => c.String(nullable: false, maxLength: 128));
            AddPrimaryKey("dbo.Assignments", "EndPoint");
        }
        
        public override void Down()
        {
            DropPrimaryKey("dbo.Assignments");
            AlterColumn("dbo.Assignments", "EndPoint", c => c.String(nullable: false));
            AddPrimaryKey("dbo.Assignments", "Id");
        }
    }
}
