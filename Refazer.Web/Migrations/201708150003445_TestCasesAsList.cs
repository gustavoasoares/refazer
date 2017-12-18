namespace Refazer.Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class TestCasesAsList : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Assignments", "TestCases", c => c.String());
            DropColumn("dbo.Assignments", "TestCase");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Assignments", "TestCase", c => c.String());
            DropColumn("dbo.Assignments", "TestCases");
        }
    }
}
