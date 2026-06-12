using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Raphael.Shared.Migrations
{
    /// <inheritdoc />
    public partial class GeneralizeIntegrationAndMultiTenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IntegratorId",
                table: "Trips",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Integrators",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApiKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Integrators", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Trips_IntegratorId",
                table: "Trips",
                column: "IntegratorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Trips_Integrators_IntegratorId",
                table: "Trips",
                column: "IntegratorId",
                principalTable: "Integrators",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trips_Integrators_IntegratorId",
                table: "Trips");

            migrationBuilder.DropTable(
                name: "Integrators");

            migrationBuilder.DropIndex(
                name: "IX_Trips_IntegratorId",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "IntegratorId",
                table: "Trips");
        }
    }
}
