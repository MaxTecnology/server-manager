using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SessionManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260330003000_AddServerAdOuSnapshot")]
    public partial class AddServerAdOuSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AgentAdOuSnapshotOutput",
                table: "Servers",
                type: "text",
                maxLength: 500000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AgentAdOuSnapshotUtc",
                table: "Servers",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AgentAdOuSnapshotOutput",
                table: "Servers");

            migrationBuilder.DropColumn(
                name: "AgentAdOuSnapshotUtc",
                table: "Servers");
        }
    }
}
