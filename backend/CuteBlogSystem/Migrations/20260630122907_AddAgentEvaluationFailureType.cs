using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CuteBlogSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentEvaluationFailureType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailureType",
                table: "AgentEvaluationResults",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_AgentEvaluationResults_FailureType",
                table: "AgentEvaluationResults",
                column: "FailureType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AgentEvaluationResults_FailureType",
                table: "AgentEvaluationResults");

            migrationBuilder.DropColumn(
                name: "FailureType",
                table: "AgentEvaluationResults");
        }
    }
}
