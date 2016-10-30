namespace Refazer.WebAPI.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class init : DbMigration
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
                        TransformationId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Transformations", t => t.TransformationId, cascadeDelete: true)
                .Index(t => t.TransformationId);
            
            CreateTable(
                "dbo.Transformations",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Program = c.String(nullable: false),
                        Examples = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.ID);
            
            CreateTable(
                "dbo.Sessions",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
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
                    })
                .PrimaryKey(t => t.ID);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Fixes", "TransformationId", "dbo.Transformations");
            DropIndex("dbo.Fixes", new[] { "TransformationId" });
            DropTable("dbo.Submissions");
            DropTable("dbo.Sessions");
            DropTable("dbo.Transformations");
            DropTable("dbo.Fixes");
        }
    }
}
