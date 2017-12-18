namespace Refazer.Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class transformation2droptime : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.Transformation2", "Time");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Transformation2", "Time", c => c.DateTime(nullable: false));
        }
    }
}
