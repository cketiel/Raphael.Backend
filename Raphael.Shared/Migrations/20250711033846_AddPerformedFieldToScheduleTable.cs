using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Raphael.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformedFieldToScheduleTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Performed",
                table: "Schedules",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Performed",
                table: "Schedules");
        }
    }
}

