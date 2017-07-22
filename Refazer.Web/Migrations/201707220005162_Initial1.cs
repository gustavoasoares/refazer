namespace Refazer.Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Initial1 : DbMigration
    {
        public override void Up()
        {
            DropPrimaryKey("dbo.Assignments");
            AlterColumn("dbo.Assignments", "EndPoint", c => c.String(nullable: false));
            AddPrimaryKey("dbo.Assignments", "Id");
        }
        
        public override void Down()
        {
            DropPrimaryKey("dbo.Assignments");
            AlterColumn("dbo.Assignments", "EndPoint", c => c.String(nullable: false, maxLength: 128));
            AddPrimaryKey("dbo.Assignments", "EndPoint");
        }
    }
}
