using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meditrans.Shared.Migrations
{
    /// <inheritdoc />
    public partial class UpdateVehicleRouteEnhancementsAndRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "VehicleRoutes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Garage",
                table: "VehicleRoutes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "VehicleRoutes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FromDate",
                table: "VehicleRoutes",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "FromTime",
                table: "VehicleRoutes",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<double>(
                name: "GarageLatitude",
                table: "VehicleRoutes",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "GarageLongitude",
                table: "VehicleRoutes",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "SmartphoneLogin",
                table: "VehicleRoutes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ToDate",
                table: "VehicleRoutes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "ToTime",
                table: "VehicleRoutes",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.CreateTable(
                name: "RouteAvailabilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleRouteId = table.Column<int>(type: "int", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteAvailabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteAvailabilities_VehicleRoutes_VehicleRouteId",
                        column: x => x.VehicleRouteId,
                        principalTable: "VehicleRoutes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RouteFundingSources",
                columns: table => new
                {
                    VehicleRouteId = table.Column<int>(type: "int", nullable: false),
                    FundingSourceId = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteFundingSources", x => new { x.VehicleRouteId, x.FundingSourceId });
                    table.ForeignKey(
                        name: "FK_RouteFundingSources_FundingSources_FundingSourceId",
                        column: x => x.FundingSourceId,
                        principalTable: "FundingSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RouteFundingSources_VehicleRoutes_VehicleRouteId",
                        column: x => x.VehicleRouteId,
                        principalTable: "VehicleRoutes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RouteSuspensions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleRouteId = table.Column<int>(type: "int", nullable: false),
                    SuspensionStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SuspensionEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteSuspensions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteSuspensions_VehicleRoutes_VehicleRouteId",
                        column: x => x.VehicleRouteId,
                        principalTable: "VehicleRoutes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RouteAvailabilities_VehicleRouteId",
                table: "RouteAvailabilities",
                column: "VehicleRouteId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteFundingSources_FundingSourceId",
                table: "RouteFundingSources",
                column: "FundingSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteSuspensions_VehicleRouteId",
                table: "RouteSuspensions",
                column: "VehicleRouteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RouteAvailabilities");

            migrationBuilder.DropTable(
                name: "RouteFundingSources");

            migrationBuilder.DropTable(
                name: "RouteSuspensions");

            migrationBuilder.DropColumn(
                name: "FromDate",
                table: "VehicleRoutes");

            migrationBuilder.DropColumn(
                name: "FromTime",
                table: "VehicleRoutes");

            migrationBuilder.DropColumn(
                name: "GarageLatitude",
                table: "VehicleRoutes");

            migrationBuilder.DropColumn(
                name: "GarageLongitude",
                table: "VehicleRoutes");

            migrationBuilder.DropColumn(
                name: "SmartphoneLogin",
                table: "VehicleRoutes");

            migrationBuilder.DropColumn(
                name: "ToDate",
                table: "VehicleRoutes");

            migrationBuilder.DropColumn(
                name: "ToTime",
                table: "VehicleRoutes");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "VehicleRoutes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Garage",
                table: "VehicleRoutes",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "VehicleRoutes",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);
        }
    }
}
