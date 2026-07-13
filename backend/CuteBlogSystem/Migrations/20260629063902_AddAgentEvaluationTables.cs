using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CuteBlogSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentEvaluationTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentEvaluationRuns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TotalCount = table.Column<int>(type: "int", nullable: false),
                    PassedCount = table.Column<int>(type: "int", nullable: false),
                    FailedCount = table.Column<int>(type: "int", nullable: false),
                    ModelUsed = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentEvaluationRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgentTestCases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UserMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SessionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ExpectedActionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpectSuccess = table.Column<bool>(type: "bit", nullable: false),
                    ExpectRequiresConfirmation = table.Column<bool>(type: "bit", nullable: false),
                    ExpectedAnswerSummary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    EnableSemanticJudge = table.Column<bool>(type: "bit", nullable: false),
                    SemanticJudgeThreshold = table.Column<double>(type: "float", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentTestCases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgentEvaluationResults",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RunId = table.Column<long>(type: "bigint", nullable: false),
                    TestCaseId = table.Column<int>(type: "int", nullable: false),
                    CaseName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Passed = table.Column<bool>(type: "bit", nullable: false),
                    ErrorsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActualActionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActualSuccess = table.Column<bool>(type: "bit", nullable: false),
                    ActualRequiresConfirmation = table.Column<bool>(type: "bit", nullable: false),
                    SemanticScore = table.Column<double>(type: "float", nullable: true),
                    SemanticJudgeReason = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    SemanticJudgePassed = table.Column<bool>(type: "bit", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentEvaluationResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentEvaluationResults_AgentEvaluationRuns_RunId",
                        column: x => x.RunId,
                        principalTable: "AgentEvaluationRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgentEvaluationResults_AgentTestCases_TestCaseId",
                        column: x => x.TestCaseId,
                        principalTable: "AgentTestCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentEvaluationResults_CreatedAt",
                table: "AgentEvaluationResults",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AgentEvaluationResults_Passed",
                table: "AgentEvaluationResults",
                column: "Passed");

            migrationBuilder.CreateIndex(
                name: "IX_AgentEvaluationResults_RunId",
                table: "AgentEvaluationResults",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentEvaluationResults_TestCaseId",
                table: "AgentEvaluationResults",
                column: "TestCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentEvaluationRuns_StartedAt",
                table: "AgentEvaluationRuns",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AgentTestCases_Category",
                table: "AgentTestCases",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_AgentTestCases_IsEnabled",
                table: "AgentTestCases",
                column: "IsEnabled");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentEvaluationResults");

            migrationBuilder.DropTable(
                name: "AgentEvaluationRuns");

            migrationBuilder.DropTable(
                name: "AgentTestCases");
        }
    }
}
