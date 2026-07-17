using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WolvesvilleManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClanWelcomeMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "WelcomeMessageEnabled",
                table: "ClanRegistrations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "WelcomeMessageTemplate",
                table: "ClanRegistrations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastWelcomedJoinAtUtc",
                table: "ClanRegistrations",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WelcomeMessageEnabled",
                table: "ClanRegistrations");

            migrationBuilder.DropColumn(
                name: "WelcomeMessageTemplate",
                table: "ClanRegistrations");

            migrationBuilder.DropColumn(
                name: "LastWelcomedJoinAtUtc",
                table: "ClanRegistrations");
        }
    }
}
