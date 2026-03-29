using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SessionManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentWindowsMvp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AgentId",
                table: "Servers",
                type: "text",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AgentLastHeartbeatUtc",
                table: "Servers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AgentLastIpAddress",
                table: "Servers",
                type: "text",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AgentVersion",
                table: "Servers",
                type: "text",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AgentCommands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServerId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedBy = table.Column<string>(maxLength: 120, nullable: false),
                    CommandText = table.Column<string>(maxLength: 400, nullable: false),
                    Status = table.Column<string>(maxLength: 20, nullable: false),
                    AssignedAgentId = table.Column<string>(maxLength: 120, nullable: true),
                    PickedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResultOutput = table.Column<string>(maxLength: 4000, nullable: true),
                    ErrorMessage = table.Column<string>(maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentCommands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentCommands_Servers_ServerId",
                        column: x => x.ServerId,
                        principalTable: "Servers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentCommands_ServerId_Status_CreatedAtUtc",
                table: "AgentCommands",
                columns: new[] { "ServerId", "Status", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentCommands");

            migrationBuilder.DropColumn(
                name: "AgentId",
                table: "Servers");

            migrationBuilder.DropColumn(
                name: "AgentLastHeartbeatUtc",
                table: "Servers");

            migrationBuilder.DropColumn(
                name: "AgentLastIpAddress",
                table: "Servers");

            migrationBuilder.DropColumn(
                name: "AgentVersion",
                table: "Servers");
        }
    }
}
