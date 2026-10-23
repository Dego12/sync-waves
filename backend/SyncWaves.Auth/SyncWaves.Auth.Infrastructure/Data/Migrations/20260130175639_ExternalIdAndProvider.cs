using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SyncWaves.Auth.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExternalIdAndProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                schema: "users",
                table: "Users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalProvider",
                schema: "users",
                table: "Users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_ExternalProvider_ExternalId",
                schema: "users",
                table: "Users",
                columns: new[] { "ExternalProvider", "ExternalId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_ExternalProvider_ExternalId",
                schema: "users",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                schema: "users",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ExternalProvider",
                schema: "users",
                table: "Users");
        }
    }
}
