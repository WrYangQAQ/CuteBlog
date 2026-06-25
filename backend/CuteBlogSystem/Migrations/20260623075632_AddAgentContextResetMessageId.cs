using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CuteBlogSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentContextResetMessageId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ContextResetMessageId",
                table: "AgentConversationMemories",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContextResetMessageId",
                table: "AgentConversationMemories");
        }
    }
}
