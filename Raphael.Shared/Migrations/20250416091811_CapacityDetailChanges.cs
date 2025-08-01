using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Raphael.Shared.Migrations
{
    /// <inheritdoc />
    public partial class CapacityDetailChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_Capacities_CapacityTypeId",
                table: "Vehicles");

            migrationBuilder.AlterColumn<int>(
                name: "CapacityTypeId",
                table: "Vehicles",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "CapacityDetailTypeId",
                table: "Vehicles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CapacityDetailTypeId",
                table: "CapacityDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CapacityDetailTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CapacityDetailTypes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_CapacityDetailTypeId",
                table: "Vehicles",
                column: "CapacityDetailTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_CapacityDetails_CapacityDetailTypeId",
                table: "CapacityDetails",
                column: "CapacityDetailTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_CapacityDetails_CapacityDetailTypes_CapacityDetailTypeId",
                table: "CapacityDetails",
                column: "CapacityDetailTypeId",
                principalTable: "CapacityDetailTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_Capacities_CapacityTypeId",
                table: "Vehicles",
                column: "CapacityTypeId",
                principalTable: "Capacities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_CapacityDetailTypes_CapacityDetailTypeId",
                table: "Vehicles",
                column: "CapacityDetailTypeId",
                principalTable: "CapacityDetailTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CapacityDetails_CapacityDetailTypes_CapacityDetailTypeId",
                table: "CapacityDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_Capacities_CapacityTypeId",
                table: "Vehicles");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_CapacityDetailTypes_CapacityDetailTypeId",
                table: "Vehicles");

            migrationBuilder.DropTable(
                name: "CapacityDetailTypes");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_CapacityDetailTypeId",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_CapacityDetails_CapacityDetailTypeId",
                table: "CapacityDetails");

            migrationBuilder.DropColumn(
                name: "CapacityDetailTypeId",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "CapacityDetailTypeId",
                table: "CapacityDetails");

            migrationBuilder.AlterColumn<int>(
                name: "CapacityTypeId",
                table: "Vehicles",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_Capacities_CapacityTypeId",
                table: "Vehicles",
                column: "CapacityTypeId",
                principalTable: "Capacities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

