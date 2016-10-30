namespace Refazer.WebAPI.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class updateisfixed : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Submissions", "IsFixed", c => c.Boolean(nullable: true));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Submissions", "IsFixed", c => c.String());
        }
    }
}
