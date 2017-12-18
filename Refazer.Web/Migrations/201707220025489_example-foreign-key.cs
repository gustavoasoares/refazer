namespace Refazer.Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class exampleforeignkey : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Examples", "EndPoint", c => c.String(maxLength: 128));
            CreateIndex("dbo.Examples", "EndPoint");
            AddForeignKey("dbo.Examples", "EndPoint", "dbo.Assignments", "EndPoint");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Examples", "EndPoint", "dbo.Assignments");
            DropIndex("dbo.Examples", new[] { "EndPoint" });
            AlterColumn("dbo.Examples", "EndPoint", c => c.String(nullable: false));
        }
    }
}
