using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Raphael.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddPassengerSignatureToSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "PassengerSignature",
                table: "Schedules",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GPSData_IdVehicleRoute",
                table: "GPSData",
                column: "IdVehicleRoute");

            migrationBuilder.AddForeignKey(
                name: "FK_GPSData_VehicleRoutes_IdVehicleRoute",
                table: "GPSData",
                column: "IdVehicleRoute",
                principalTable: "VehicleRoutes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GPSData_VehicleRoutes_IdVehicleRoute",
                table: "GPSData");

            migrationBuilder.DropIndex(
                name: "IX_GPSData_IdVehicleRoute",
                table: "GPSData");

            migrationBuilder.DropColumn(
                name: "PassengerSignature",
                table: "Schedules");
        }
    }
}
