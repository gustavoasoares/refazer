namespace Refazer.WebAPI.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class addingranktype : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Transformations", "RankType", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Transformations", "RankType");
        }
    }
}
