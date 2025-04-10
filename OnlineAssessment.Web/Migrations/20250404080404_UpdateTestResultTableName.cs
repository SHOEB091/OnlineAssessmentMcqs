using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineAssessment.Web.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTestResultTableName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TestResults_Tests_TestId",
                table: "TestResults");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TestResults",
                table: "TestResults");

            migrationBuilder.RenameTable(
                name: "TestResults",
                newName: "testresult");

            migrationBuilder.RenameIndex(
                name: "IX_TestResults_TestId",
                table: "testresult",
                newName: "IX_testresult_TestId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_testresult",
                table: "testresult",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_testresult_Tests_TestId",
                table: "testresult",
                column: "TestId",
                principalTable: "Tests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_testresult_Tests_TestId",
                table: "testresult");

            migrationBuilder.DropPrimaryKey(
                name: "PK_testresult",
                table: "testresult");

            migrationBuilder.RenameTable(
                name: "testresult",
                newName: "TestResults");

            migrationBuilder.RenameIndex(
                name: "IX_testresult_TestId",
                table: "TestResults",
                newName: "IX_TestResults_TestId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TestResults",
                table: "TestResults",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TestResults_Tests_TestId",
                table: "TestResults",
                column: "TestId",
                principalTable: "Tests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
