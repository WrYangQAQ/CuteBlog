using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CuteBlogSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceIdToAgentEvaluationRun : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SourceId",
                table: "AgentEvaluationRuns",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentEvaluationRuns_SourceId",
                table: "AgentEvaluationRuns",
                column: "SourceId");

            migrationBuilder.AddForeignKey(
                name: "FK_AgentEvaluationRuns_AgentEvaluationRuns_SourceId",
                table: "AgentEvaluationRuns",
                column: "SourceId",
                principalTable: "AgentEvaluationRuns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AgentEvaluationRuns_AgentEvaluationRuns_SourceId",
                table: "AgentEvaluationRuns");

            migrationBuilder.DropIndex(
                name: "IX_AgentEvaluationRuns_SourceId",
                table: "AgentEvaluationRuns");

            migrationBuilder.DropColumn(
                name: "SourceId",
                table: "AgentEvaluationRuns");
        }
    }
}
