using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineAssessment.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddTitleToQuestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Questions",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            // Update existing records to set Title based on Text
            migrationBuilder.Sql(@"
                UPDATE Questions 
                SET Title = CASE 
                    WHEN LENGTH(Text) > 100 THEN LEFT(Text, 100)
                    ELSE Text
                END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Title",
                table: "Questions");
        }
    }
} 