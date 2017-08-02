namespace Refazer.Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class transformation2 : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Transformation2",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Time = c.DateTime(nullable: false),
                        EndPoint = c.String(nullable: false, maxLength: 128),
                        IncorrectCode = c.String(nullable: false),
                        CorrectCode = c.String(nullable: false),
                        Program = c.String(nullable: false),
                        Rank = c.Int(nullable: false),
                        RankType = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Assignments", t => t.EndPoint, cascadeDelete: true)
                .Index(t => t.EndPoint);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Transformation2", "EndPoint", "dbo.Assignments");
            DropIndex("dbo.Transformation2", new[] { "EndPoint" });
            DropTable("dbo.Transformation2");
        }
    }
}
