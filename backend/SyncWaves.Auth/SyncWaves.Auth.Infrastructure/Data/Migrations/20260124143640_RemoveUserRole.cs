using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SyncWaves.Auth.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUserRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                schema: "users",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Role",
                schema: "users",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
