using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WolvesvilleManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClanRegistrations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClanId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ClanName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClanTag = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    ProtectedApiKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClanRegistrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScheduledTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClanRegistrationId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CronExpression = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TimeZoneId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    MinVotes = table.Column<int>(type: "int", nullable: false),
                    NextRunAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastRunAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduledTasks_ClanRegistrations_ClanRegistrationId",
                        column: x => x.ClanRegistrationId,
                        principalTable: "ClanRegistrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskExecutionLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScheduledTaskId = table.Column<int>(type: "int", nullable: false),
                    RanAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskExecutionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskExecutionLogs_ScheduledTasks_ScheduledTaskId",
                        column: x => x.ScheduledTaskId,
                        principalTable: "ScheduledTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClanRegistrations_ClanId",
                table: "ClanRegistrations",
                column: "ClanId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledTasks_ClanRegistrationId",
                table: "ScheduledTasks",
                column: "ClanRegistrationId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledTasks_Enabled_NextRunAtUtc",
                table: "ScheduledTasks",
                columns: new[] { "Enabled", "NextRunAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskExecutionLogs_ScheduledTaskId_RanAtUtc",
                table: "TaskExecutionLogs",
                columns: new[] { "ScheduledTaskId", "RanAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskExecutionLogs");

            migrationBuilder.DropTable(
                name: "ScheduledTasks");

            migrationBuilder.DropTable(
                name: "ClanRegistrations");
        }
    }
}
