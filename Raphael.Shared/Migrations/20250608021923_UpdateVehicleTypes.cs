using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meditrans.Shared.Migrations
{
    /// <inheritdoc />
    public partial class UpdateVehicleTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_VehicleGroup_GroupId",
                table: "Vehicles");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_VehicleType_VehicleTypeId",
                table: "Vehicles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VehicleType",
                table: "VehicleType");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VehicleGroup",
                table: "VehicleGroup");

            migrationBuilder.RenameTable(
                name: "VehicleType",
                newName: "VehicleTypes");

            migrationBuilder.RenameTable(
                name: "VehicleGroup",
                newName: "VehicleGroups");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VehicleTypes",
                table: "VehicleTypes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VehicleGroups",
                table: "VehicleGroups",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_VehicleGroups_GroupId",
                table: "Vehicles",
                column: "GroupId",
                principalTable: "VehicleGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_VehicleTypes_VehicleTypeId",
                table: "Vehicles",
                column: "VehicleTypeId",
                principalTable: "VehicleTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_VehicleGroups_GroupId",
                table: "Vehicles");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_VehicleTypes_VehicleTypeId",
                table: "Vehicles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VehicleTypes",
                table: "VehicleTypes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VehicleGroups",
                table: "VehicleGroups");

            migrationBuilder.RenameTable(
                name: "VehicleTypes",
                newName: "VehicleType");

            migrationBuilder.RenameTable(
                name: "VehicleGroups",
                newName: "VehicleGroup");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VehicleType",
                table: "VehicleType",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VehicleGroup",
                table: "VehicleGroup",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_VehicleGroup_GroupId",
                table: "Vehicles",
                column: "GroupId",
                principalTable: "VehicleGroup",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_VehicleType_VehicleTypeId",
                table: "Vehicles",
                column: "VehicleTypeId",
                principalTable: "VehicleType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
