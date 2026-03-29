using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SessionManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddServerCapabilities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SupportsAd",
                table: "Servers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SupportsRds",
                table: "Servers",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SupportsAd",
                table: "Servers");

            migrationBuilder.DropColumn(
                name: "SupportsRds",
                table: "Servers");
        }
    }
}
