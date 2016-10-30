namespace Refazer.WebAPI.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Andrew : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Fixes", "TransformationId", "dbo.Transformations");
            DropIndex("dbo.Fixes", new[] { "TransformationId" });
            RenameColumn(table: "dbo.Fixes", name: "TransformationId", newName: "Transformation_ID");
            AlterColumn("dbo.Fixes", "Transformation_ID", c => c.Int());
            CreateIndex("dbo.Fixes", "Transformation_ID");
            AddForeignKey("dbo.Fixes", "Transformation_ID", "dbo.Transformations", "ID");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Fixes", "Transformation_ID", "dbo.Transformations");
            DropIndex("dbo.Fixes", new[] { "Transformation_ID" });
            AlterColumn("dbo.Fixes", "Transformation_ID", c => c.Int(nullable: false));
            RenameColumn(table: "dbo.Fixes", name: "Transformation_ID", newName: "TransformationId");
            CreateIndex("dbo.Fixes", "TransformationId");
            AddForeignKey("dbo.Fixes", "TransformationId", "dbo.Transformations", "ID", cascadeDelete: true);
        }
    }
}
