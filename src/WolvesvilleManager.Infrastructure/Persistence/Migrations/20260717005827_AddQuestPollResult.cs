using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WolvesvilleManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestPollResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuestPollResults",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClanRegistrationId = table.Column<int>(type: "integer", nullable: false),
                    QuestName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Votes = table.Column<int>(type: "integer", nullable: false),
                    WasShuffle = table.Column<bool>(type: "boolean", nullable: false),
                    DecidedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestPollResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestPollResults_ClanRegistrations_ClanRegistrationId",
                        column: x => x.ClanRegistrationId,
                        principalTable: "ClanRegistrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuestPollResults_ClanRegistrationId_DecidedAtUtc",
                table: "QuestPollResults",
                columns: new[] { "ClanRegistrationId", "DecidedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuestPollResults");
        }
    }
}
