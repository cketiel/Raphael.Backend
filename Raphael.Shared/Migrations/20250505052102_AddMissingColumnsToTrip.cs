using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Raphael.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingColumnsToTrip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PickupNote",
                table: "Trips",
                newName: "Type");

            migrationBuilder.AddColumn<double>(
                name: "Charge",
                table: "Trips",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Created",
                table: "Trips",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<double>(
                name: "Distance",
                table: "Trips",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverNoShowReason",
                table: "Trips",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Dropoof",
                table: "Trips",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DropoofComment",
                table: "Trips",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DropoofPhone",
                table: "Trips",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ETA",
                table: "Trips",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Paid",
                table: "Trips",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Pickup",
                table: "Trips",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PickupComment",
                table: "Trips",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PickupPhone",
                table: "Trips",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TripId",
                table: "Trips",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VehicleRouteId",
                table: "Trips",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "WillCall",
                table: "Trips",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Trips_VehicleRouteId",
                table: "Trips",
                column: "VehicleRouteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Trips_VehicleRoutes_VehicleRouteId",
                table: "Trips",
                column: "VehicleRouteId",
                principalTable: "VehicleRoutes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trips_VehicleRoutes_VehicleRouteId",
                table: "Trips");

            migrationBuilder.DropIndex(
                name: "IX_Trips_VehicleRouteId",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "Charge",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "Created",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "Distance",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "DriverNoShowReason",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "Dropoof",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "DropoofComment",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "DropoofPhone",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "ETA",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "Paid",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "Pickup",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "PickupComment",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "PickupPhone",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "TripId",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "VehicleRouteId",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "WillCall",
                table: "Trips");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "Trips",
                newName: "PickupNote");
        }
    }
}

