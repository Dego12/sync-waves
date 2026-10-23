using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SyncWaves.Auth.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProfilePictureUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfilePictureUrl",
                schema: "users",
                table: "Users",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfilePictureUrl",
                schema: "users",
                table: "Users");
        }
    }
}
