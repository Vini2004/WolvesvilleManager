using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WolvesvilleManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalCronJobIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExternalRunJobId",
                table: "ScheduledTasks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExternalWarmupJobId",
                table: "ScheduledTasks",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalRunJobId",
                table: "ScheduledTasks");

            migrationBuilder.DropColumn(
                name: "ExternalWarmupJobId",
                table: "ScheduledTasks");
        }
    }
}
