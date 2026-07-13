using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CuteBlogSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddEvaluationSnapshotTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TestCaseSnapshotJson",
                table: "AgentEvaluationResults",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.CreateTable(
                name: "AgentEvaluationReportSnapshots",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RunId = table.Column<long>(type: "bigint", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MarkdownContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlannerPromptVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ActionRegistryVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EvaluationVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FinalAnswerPromptVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentEvaluationReportSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentEvaluationReportSnapshots_AgentEvaluationRuns_RunId",
                        column: x => x.RunId,
                        principalTable: "AgentEvaluationRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentEvaluationReportSnapshots_CreatedAt",
                table: "AgentEvaluationReportSnapshots",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AgentEvaluationReportSnapshots_IsDeleted",
                table: "AgentEvaluationReportSnapshots",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_AgentEvaluationReportSnapshots_RunId",
                table: "AgentEvaluationReportSnapshots",
                column: "RunId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentEvaluationReportSnapshots");

            migrationBuilder.DropColumn(
                name: "TestCaseSnapshotJson",
                table: "AgentEvaluationResults");
        }
    }
}
