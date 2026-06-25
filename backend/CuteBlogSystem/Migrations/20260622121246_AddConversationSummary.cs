using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CuteBlogSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddConversationSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConversationSummary",
                table: "AgentConversationMemories",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LastSummarizedMessageId",
                table: "AgentConversationMemories",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SummaryLastUpdate",
                table: "AgentConversationMemories",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConversationSummary",
                table: "AgentConversationMemories");

            migrationBuilder.DropColumn(
                name: "LastSummarizedMessageId",
                table: "AgentConversationMemories");

            migrationBuilder.DropColumn(
                name: "SummaryLastUpdate",
                table: "AgentConversationMemories");
        }
    }
}
