using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SessionManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260329212000_IncreaseAgentCommandLength")]
    public partial class IncreaseAgentCommandLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CommandText",
                table: "AgentCommands",
                type: "text",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldMaxLength: 400);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CommandText",
                table: "AgentCommands",
                type: "text",
                maxLength: 400,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldMaxLength: 4000);
        }
    }
}
