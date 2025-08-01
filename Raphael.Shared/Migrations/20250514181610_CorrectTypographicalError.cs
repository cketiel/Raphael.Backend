using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Raphael.Shared.Migrations
{
    /// <inheritdoc />
    public partial class CorrectTypographicalError : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DropoofPhone",
                table: "Trips",
                newName: "DropoffPhone");

            migrationBuilder.RenameColumn(
                name: "DropoofComment",
                table: "Trips",
                newName: "DropoffComment");

            migrationBuilder.RenameColumn(
                name: "Dropoof",
                table: "Trips",
                newName: "Dropoff");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DropoffPhone",
                table: "Trips",
                newName: "DropoofPhone");

            migrationBuilder.RenameColumn(
                name: "DropoffComment",
                table: "Trips",
                newName: "DropoofComment");

            migrationBuilder.RenameColumn(
                name: "Dropoff",
                table: "Trips",
                newName: "Dropoof");
        }
    }
}

