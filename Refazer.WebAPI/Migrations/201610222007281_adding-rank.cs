namespace Refazer.WebAPI.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class addingrank : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Transformations", "Rank", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Transformations", "Rank");
        }
    }
}
