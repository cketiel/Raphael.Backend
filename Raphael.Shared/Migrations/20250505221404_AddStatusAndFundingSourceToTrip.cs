using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meditrans.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusAndFundingSourceToTrip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trips_VehicleRoutes_VehicleRouteId",
                table: "Trips");

            migrationBuilder.AlterColumn<int>(
                name: "VehicleRouteId",
                table: "Trips",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Trips",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FundingSourceId",
                table: "Trips",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Trips",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_FundingSourceId",
                table: "Trips",
                column: "FundingSourceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Trips_FundingSources_FundingSourceId",
                table: "Trips",
                column: "FundingSourceId",
                principalTable: "FundingSources",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Trips_VehicleRoutes_VehicleRouteId",
                table: "Trips",
                column: "VehicleRouteId",
                principalTable: "VehicleRoutes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trips_FundingSources_FundingSourceId",
                table: "Trips");

            migrationBuilder.DropForeignKey(
                name: "FK_Trips_VehicleRoutes_VehicleRouteId",
                table: "Trips");

            migrationBuilder.DropIndex(
                name: "IX_Trips_FundingSourceId",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "FundingSourceId",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Trips");

            migrationBuilder.AlterColumn<int>(
                name: "VehicleRouteId",
                table: "Trips",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Trips",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddForeignKey(
                name: "FK_Trips_VehicleRoutes_VehicleRouteId",
                table: "Trips",
                column: "VehicleRouteId",
                principalTable: "VehicleRoutes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
