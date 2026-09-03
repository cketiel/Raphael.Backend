using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Raphael.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleScreenIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Schedules_VehicleRouteId",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_GPSData_IdVehicleRoute",
                table: "GPSData");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleRoutes_SmartphoneLogin",
                table: "VehicleRoutes",
                column: "SmartphoneLogin");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_Date_Route_Active",
                table: "Trips",
                columns: new[] { "Date", "VehicleRouteId" },
                filter: "[IsCancelled] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_Route_Date",
                table: "Schedules",
                columns: new[] { "VehicleRouteId", "Date" })
                .Annotation("SqlServer:Include", new[] { "Sequence" });

            migrationBuilder.CreateIndex(
                name: "IX_GPSData_Route_DateTime",
                table: "GPSData",
                columns: new[] { "IdVehicleRoute", "DateTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VehicleRoutes_SmartphoneLogin",
                table: "VehicleRoutes");

            migrationBuilder.DropIndex(
                name: "IX_Trips_Date_Route_Active",
                table: "Trips");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_Route_Date",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_GPSData_Route_DateTime",
                table: "GPSData");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_VehicleRouteId",
                table: "Schedules",
                column: "VehicleRouteId");

            migrationBuilder.CreateIndex(
                name: "IX_GPSData_IdVehicleRoute",
                table: "GPSData",
                column: "IdVehicleRoute");
        }
    }
}
