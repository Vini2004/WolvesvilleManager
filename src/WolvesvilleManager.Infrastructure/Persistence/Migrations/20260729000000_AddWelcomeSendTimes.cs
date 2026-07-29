using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WolvesvilleManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWelcomeSendTimes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "WelcomeSendTime1",
                table: "ClanRegistrations",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "WelcomeSendTime2",
                table: "ClanRegistrations",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WelcomePingJobId",
                table: "ClanRegistrations",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WelcomeSendTime1",
                table: "ClanRegistrations");

            migrationBuilder.DropColumn(
                name: "WelcomeSendTime2",
                table: "ClanRegistrations");

            migrationBuilder.DropColumn(
                name: "WelcomePingJobId",
                table: "ClanRegistrations");
        }
    }
}
