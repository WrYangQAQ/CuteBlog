using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CuteBlogSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentWorkflowLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentWorkflowLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserMessage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    Recovered = table.Column<bool>(type: "bit", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlanJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExecutionResultJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FailureAnalysis = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecoveryPlanJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecoveryExecutionResultJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentWorkflowLogs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentWorkflowLogs");
        }
    }
}
