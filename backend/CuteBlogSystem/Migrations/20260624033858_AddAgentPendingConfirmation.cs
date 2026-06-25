using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CuteBlogSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentPendingConfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentPendingConfirmations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConfirmationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SessionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PlanJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentPendingConfirmations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentPendingConfirmations_ConfirmationId",
                table: "AgentPendingConfirmations",
                column: "ConfirmationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentPendingConfirmations_SessionId_Status",
                table: "AgentPendingConfirmations",
                columns: new[] { "SessionId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentPendingConfirmations");
        }
    }
}
