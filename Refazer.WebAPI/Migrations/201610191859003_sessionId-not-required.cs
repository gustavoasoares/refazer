namespace Refazer.WebAPI.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class sessionIdnotrequired : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Transformations", "SessionId", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Transformations", "SessionId", c => c.Int(nullable: false));
        }
    }
}
