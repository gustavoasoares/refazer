namespace Refazer.Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Fixes",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        FixedCode = c.String(),
                        SubmissionId = c.Int(nullable: false),
                        SessionId = c.Int(nullable: false),
                        QuestionId = c.Int(nullable: false),
                        Time = c.DateTime(nullable: false),
                        Transformation_ID = c.Int(),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Transformations", t => t.Transformation_ID)
                .Index(t => t.Transformation_ID);
            
            CreateTable(
                "dbo.Transformations",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Program = c.String(nullable: false),
                        Examples = c.String(nullable: false),
                        Time = c.DateTime(nullable: false),
                        Rank = c.Int(nullable: false),
                        RankType = c.Int(nullable: false),
                        SessionId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.ID);
            
            CreateTable(
                "dbo.Sessions",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Time = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.ID);
            
            CreateTable(
                "dbo.Submissions",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        QuestionId = c.Int(nullable: false),
                        SubmissionId = c.Int(nullable: false),
                        SessionId = c.Int(nullable: false),
                        Code = c.String(nullable: false),
                        IsFixed = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.ID);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Fixes", "Transformation_ID", "dbo.Transformations");
            DropIndex("dbo.Fixes", new[] { "Transformation_ID" });
            DropTable("dbo.Submissions");
            DropTable("dbo.Sessions");
            DropTable("dbo.Transformations");
            DropTable("dbo.Fixes");
        }
    }
}
