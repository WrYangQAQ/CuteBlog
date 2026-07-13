using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CuteBlogSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentTestCasesIsDeleted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ExpectSuccess",
                table: "AgentTestCases",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "ExpectRequiresConfirmation",
                table: "AgentTestCases",
                newName: "ExpectedSuccess");

            migrationBuilder.AddColumn<bool>(
                name: "ExpectedRequiresConfirmation",
                table: "AgentTestCases",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_AgentTestCases_IsDeleted",
                table: "AgentTestCases",
                column: "IsDeleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AgentTestCases_IsDeleted",
                table: "AgentTestCases");

            migrationBuilder.DropColumn(
                name: "ExpectedRequiresConfirmation",
                table: "AgentTestCases");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "AgentTestCases",
                newName: "ExpectSuccess");

            migrationBuilder.RenameColumn(
                name: "ExpectedSuccess",
                table: "AgentTestCases",
                newName: "ExpectRequiresConfirmation");
        }
    }
}
