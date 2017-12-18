namespace Refazer.Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddingCluster : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Clusters",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        KeyPoint = c.String(nullable: false),
                        ExamplesReferenceStr = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Clusters");
        }
    }
}
