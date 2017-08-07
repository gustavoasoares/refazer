namespace Refazer.Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class deletetransformation2 : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Transformation2", "EndPoint", "dbo.Assignments");
            DropIndex("dbo.Transformation2", new[] { "EndPoint" });
            DropTable("dbo.Transformation2");
        }
        
        public override void Down()
        {
            CreateTable(
                "dbo.Transformation2",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        EndPoint = c.String(nullable: false, maxLength: 128),
                        IncorrectCode = c.String(nullable: false),
                        CorrectCode = c.String(nullable: false),
                        Program = c.String(nullable: false),
                        Rank = c.Int(nullable: false),
                        RankType = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.ID);
            
            CreateIndex("dbo.Transformation2", "EndPoint");
            AddForeignKey("dbo.Transformation2", "EndPoint", "dbo.Assignments", "EndPoint", cascadeDelete: true);
        }
    }
}
