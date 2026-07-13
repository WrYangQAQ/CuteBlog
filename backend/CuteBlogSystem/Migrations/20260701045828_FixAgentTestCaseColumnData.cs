using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CuteBlogSystem.Migrations
{
    /// <inheritdoc />
    public partial class FixAgentTestCaseColumnData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                 UPDATE AgentTestCases
                 SET
                     ExpectedRequiresConfirmation = ExpectedSuccess,
                     ExpectedSuccess = IsDeleted,
                     IsDeleted = 0
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE AgentTestCases
                SET
                    IsDeleted = ExpectedSuccess,
                    ExpectedSuccess = ExpectedRequiresConfirmation,
                    ExpectedRequiresConfirmation = 0
            """);
        }
    }
}
