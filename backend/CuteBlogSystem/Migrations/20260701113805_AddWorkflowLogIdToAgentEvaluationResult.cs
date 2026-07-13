using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CuteBlogSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowLogIdToAgentEvaluationResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WorkflowLogId",
                table: "AgentEvaluationResults",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentEvaluationResults_WorkflowLogId",
                table: "AgentEvaluationResults",
                column: "WorkflowLogId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AgentEvaluationResults_WorkflowLogId",
                table: "AgentEvaluationResults");

            migrationBuilder.DropColumn(
                name: "WorkflowLogId",
                table: "AgentEvaluationResults");
        }
    }
}
