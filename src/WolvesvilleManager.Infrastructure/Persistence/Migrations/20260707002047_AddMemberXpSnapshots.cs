using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WolvesvilleManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberXpSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MemberXpSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClanRegistrationId = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Xp = table.Column<long>(type: "bigint", nullable: false),
                    TakenAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberXpSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberXpSnapshots_ClanRegistrations_ClanRegistrationId",
                        column: x => x.ClanRegistrationId,
                        principalTable: "ClanRegistrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MemberXpSnapshots_ClanRegistrationId_TakenAtUtc",
                table: "MemberXpSnapshots",
                columns: new[] { "ClanRegistrationId", "TakenAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MemberXpSnapshots");
        }
    }
}
