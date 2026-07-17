using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WolvesvilleManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPollWindows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PollCloseCronExpression",
                table: "ClanRegistrations");

            migrationBuilder.DropColumn(
                name: "PollCloseTimeZoneId",
                table: "ClanRegistrations");

            migrationBuilder.AddColumn<string>(
                name: "PollWindowsTimeZoneId",
                table: "ClanRegistrations",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PollLastClaimedWindowEndUtc",
                table: "ClanRegistrations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "QuestPollVotes",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.CreateTable(
                name: "PollWindows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClanRegistrationId = table.Column<int>(type: "integer", nullable: false),
                    StartDayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    EndDayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "interval", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PollWindows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PollWindows_ClanRegistrations_ClanRegistrationId",
                        column: x => x.ClanRegistrationId,
                        principalTable: "ClanRegistrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PollWindows_ClanRegistrationId",
                table: "PollWindows",
                column: "ClanRegistrationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PollWindows");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "QuestPollVotes");

            migrationBuilder.DropColumn(
                name: "PollLastClaimedWindowEndUtc",
                table: "ClanRegistrations");

            migrationBuilder.DropColumn(
                name: "PollWindowsTimeZoneId",
                table: "ClanRegistrations");

            migrationBuilder.AddColumn<string>(
                name: "PollCloseCronExpression",
                table: "ClanRegistrations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PollCloseTimeZoneId",
                table: "ClanRegistrations",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }
    }
}
