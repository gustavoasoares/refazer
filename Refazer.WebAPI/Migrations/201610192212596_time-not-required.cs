namespace Refazer.WebAPI.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class timenotrequired : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Submissions", "Time", c => c.DateTime(nullable: false));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Submissions", "Time", c => c.DateTime(nullable: false));
        }
    }
}
