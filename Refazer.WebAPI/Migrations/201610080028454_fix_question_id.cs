namespace Refazer.WebAPI.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class fix_question_id : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Fixes", "QuestionId", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Fixes", "QuestionId");
        }
    }
}
