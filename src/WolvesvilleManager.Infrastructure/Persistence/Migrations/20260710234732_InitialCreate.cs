using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClanId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ClanName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ClanTag = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    ProtectedApiKey = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClanRegistrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MemberXpSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClanRegistrationId = table.Column<int>(type: "integer", nullable: false),
                    PlayerId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Xp = table.Column<long>(type: "bigint", nullable: false),
                    TakenAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "ScheduledTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClanRegistrationId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CronExpression = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TimeZoneId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    MinVotes = table.Column<int>(type: "integer", nullable: false),
                    NextRunAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastRunAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExternalRunJobId = table.Column<int>(type: "integer", nullable: true),
                    ExternalWarmupJobId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ScheduledTaskId = table.Column<int>(type: "integer", nullable: false),
                    RanAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Outcome = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
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
                name: "IX_MemberXpSnapshots_ClanRegistrationId_TakenAtUtc",
                table: "MemberXpSnapshots",
                columns: new[] { "ClanRegistrationId", "TakenAtUtc" });

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
                name: "MemberXpSnapshots");

            migrationBuilder.DropTable(
                name: "TaskExecutionLogs");

            migrationBuilder.DropTable(
                name: "ScheduledTasks");

            migrationBuilder.DropTable(
                name: "ClanRegistrations");
        }
    }
}
