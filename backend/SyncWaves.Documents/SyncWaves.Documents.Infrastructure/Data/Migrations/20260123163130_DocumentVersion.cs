using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SyncWaves.Documents.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class DocumentVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Version",
                schema: "documents",
                table: "Documents",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                schema: "documents",
                table: "Documents");
        }
    }
}
