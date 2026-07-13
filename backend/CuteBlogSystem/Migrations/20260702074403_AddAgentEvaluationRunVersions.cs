using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CuteBlogSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentEvaluationRunVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActionRegistryVersion",
                table: "AgentEvaluationRuns",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EvaluationVersion",
                table: "AgentEvaluationRuns",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinalAnswerPromptVersion",
                table: "AgentEvaluationRuns",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlannerPromptVersion",
                table: "AgentEvaluationRuns",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActionRegistryVersion",
                table: "AgentEvaluationRuns");

            migrationBuilder.DropColumn(
                name: "EvaluationVersion",
                table: "AgentEvaluationRuns");

            migrationBuilder.DropColumn(
                name: "FinalAnswerPromptVersion",
                table: "AgentEvaluationRuns");

            migrationBuilder.DropColumn(
                name: "PlannerPromptVersion",
                table: "AgentEvaluationRuns");
        }
    }
}
