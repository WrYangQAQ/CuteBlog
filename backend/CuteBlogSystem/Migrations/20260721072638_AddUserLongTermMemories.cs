using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CuteBlogSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddUserLongTermMemories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserLongTermMemories",
                columns: table => new
                {
                    MemoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    MemoryType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MemoryGroup = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MemoryKey = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourceSessionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SourceMessageId = table.Column<long>(type: "bigint", nullable: true),
                    SourceAction = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Confidence = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false, defaultValue: 0.5000m),
                    Importance = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false, defaultValue: 0.5000m),
                    IsPinned = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AccessCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LastAccessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastDecayAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)1),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ArchivedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevisionNo = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    SupersedesMemoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLongTermMemories", x => x.MemoryId);
                    table.CheckConstraint("CK_UserLongTermMemories_AccessCount", "[AccessCount] >= 0");
                    table.CheckConstraint("CK_UserLongTermMemories_Confidence", "[Confidence] >= 0 AND [Confidence] <= 1");
                    table.CheckConstraint("CK_UserLongTermMemories_Importance", "[Importance] >= 0 AND [Importance] <= 1");
                    table.CheckConstraint("CK_UserLongTermMemories_MetadataJson", "[MetadataJson] IS NULL OR ISJSON([MetadataJson]) = 1");
                    table.CheckConstraint("CK_UserLongTermMemories_RevisionNo", "[RevisionNo] >= 1");
                    table.ForeignKey(
                        name: "FK_UserLongTermMemories_UserLongTermMemories_SupersedesMemoryId",
                        column: x => x.SupersedesMemoryId,
                        principalTable: "UserLongTermMemories",
                        principalColumn: "MemoryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserLongTermMemories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserLongTermMemories_Status_IsPinned_LastDecayAt",
                table: "UserLongTermMemories",
                columns: new[] { "Status", "IsPinned", "LastDecayAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserLongTermMemories_SupersedesMemoryId",
                table: "UserLongTermMemories",
                column: "SupersedesMemoryId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLongTermMemories_UserId_ContentHash",
                table: "UserLongTermMemories",
                columns: new[] { "UserId", "ContentHash" });

            migrationBuilder.CreateIndex(
                name: "IX_UserLongTermMemories_UserId_CreatedAt",
                table: "UserLongTermMemories",
                columns: new[] { "UserId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_UserLongTermMemories_UserId_MemoryKey_MemoryGroup_MemoryType",
                table: "UserLongTermMemories",
                columns: new[] { "UserId", "MemoryKey", "MemoryGroup", "MemoryType" },
                unique: true,
                filter: "[MemoryKey] IS NOT NULL AND [Status] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_UserLongTermMemories_UserId_Status_Importance",
                table: "UserLongTermMemories",
                columns: new[] { "UserId", "Status", "Importance" },
                descending: new[] { false, false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserLongTermMemories");
        }
    }
}
