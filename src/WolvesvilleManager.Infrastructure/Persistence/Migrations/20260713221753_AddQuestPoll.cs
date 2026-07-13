using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WolvesvilleManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestPoll : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PollToken",
                table: "ClanRegistrations",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "QuestPollVotes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClanRegistrationId = table.Column<int>(type: "integer", nullable: false),
                    QuestId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    VoterId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestPollVotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestPollVotes_ClanRegistrations_ClanRegistrationId",
                        column: x => x.ClanRegistrationId,
                        principalTable: "ClanRegistrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClanRegistrations_PollToken",
                table: "ClanRegistrations",
                column: "PollToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuestPollVotes_ClanRegistrationId_VoterId",
                table: "QuestPollVotes",
                columns: new[] { "ClanRegistrationId", "VoterId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuestPollVotes");

            migrationBuilder.DropIndex(
                name: "IX_ClanRegistrations_PollToken",
                table: "ClanRegistrations");

            migrationBuilder.DropColumn(
                name: "PollToken",
                table: "ClanRegistrations");
        }
    }
}
