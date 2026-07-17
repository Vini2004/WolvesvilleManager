using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WolvesvilleManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameVoterIdToNickname : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_QuestPollVotes_ClanRegistrationId_VoterId",
                table: "QuestPollVotes");

            migrationBuilder.DropColumn(
                name: "VoterId",
                table: "QuestPollVotes");

            migrationBuilder.AddColumn<string>(
                name: "Nickname",
                table: "QuestPollVotes",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_QuestPollVotes_ClanRegistrationId_Nickname",
                table: "QuestPollVotes",
                columns: new[] { "ClanRegistrationId", "Nickname" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_QuestPollVotes_ClanRegistrationId_Nickname",
                table: "QuestPollVotes");

            migrationBuilder.DropColumn(
                name: "Nickname",
                table: "QuestPollVotes");

            migrationBuilder.AddColumn<string>(
                name: "VoterId",
                table: "QuestPollVotes",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_QuestPollVotes_ClanRegistrationId_VoterId",
                table: "QuestPollVotes",
                columns: new[] { "ClanRegistrationId", "VoterId" },
                unique: true);
        }
    }
}
