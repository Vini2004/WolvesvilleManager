using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WolvesvilleManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWelcomeCheckDiagnostics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastWelcomeCheckAtUtc",
                table: "ClanRegistrations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastWelcomeCheckResult",
                table: "ClanRegistrations",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastWelcomeCheckAtUtc",
                table: "ClanRegistrations");

            migrationBuilder.DropColumn(
                name: "LastWelcomeCheckResult",
                table: "ClanRegistrations");
        }
    }
}
