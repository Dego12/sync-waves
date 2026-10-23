using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SyncWaves.RTC.Infrastructure.Migrations.Data
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "rtc");

            migrationBuilder.CreateTable(
                name: "DocumentStates",
                schema: "rtc",
                columns: table => new
                {
                    DocumentId = table.Column<Guid>(type: "uuid", maxLength: 64, nullable: false),
                    CurrentVersion = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentStates", x => x.DocumentId);
                });

            migrationBuilder.CreateTable(
                name: "DocumentVersions",
                schema: "rtc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", maxLength: 64, nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentVersions_DocumentStates_DocumentId",
                        column: x => x.DocumentId,
                        principalSchema: "rtc",
                        principalTable: "DocumentStates",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVersions_DocumentId_VersionNumber",
                schema: "rtc",
                table: "DocumentVersions",
                columns: new[] { "DocumentId", "VersionNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentVersions",
                schema: "rtc");

            migrationBuilder.DropTable(
                name: "DocumentStates",
                schema: "rtc");
        }
    }
}
