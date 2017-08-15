namespace Refazer.Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class TestCasesRequired : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Assignments", "TestCases", c => c.String(nullable: false));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Assignments", "TestCases", c => c.String());
        }
    }
}
