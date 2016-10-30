namespace Refazer.WebAPI.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class sessionIdtoTransformation : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Transformations", "SessionId", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Transformations", "SessionId");
        }
    }
}
