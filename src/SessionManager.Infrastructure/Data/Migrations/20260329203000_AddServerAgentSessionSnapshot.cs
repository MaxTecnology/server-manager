using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SessionManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddServerAgentSessionSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AgentSessionSnapshotOutput",
                table: "Servers",
                type: "text",
                maxLength: 20000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AgentSessionSnapshotUtc",
                table: "Servers",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AgentSessionSnapshotOutput",
                table: "Servers");

            migrationBuilder.DropColumn(
                name: "AgentSessionSnapshotUtc",
                table: "Servers");
        }
    }
}
