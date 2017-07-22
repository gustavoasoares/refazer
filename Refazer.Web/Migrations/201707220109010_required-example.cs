namespace Refazer.Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class requiredexample : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Examples", "Question", c => c.String(nullable: false));
            AlterColumn("dbo.Examples", "IncorrectCode", c => c.String(nullable: false));
            AlterColumn("dbo.Examples", "CorrectCode", c => c.String(nullable: false));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Examples", "CorrectCode", c => c.String());
            AlterColumn("dbo.Examples", "IncorrectCode", c => c.String());
            AlterColumn("dbo.Examples", "Question", c => c.String());
        }
    }
}
