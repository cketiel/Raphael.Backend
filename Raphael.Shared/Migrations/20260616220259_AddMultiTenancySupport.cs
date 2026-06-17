using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Raphael.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenancySupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IntegratorId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProviderId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProviderId",
                table: "Trips",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_IntegratorId",
                table: "Users",
                column: "IntegratorId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ProviderId",
                table: "Users",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_ProviderId",
                table: "Trips",
                column: "ProviderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Trips_Providers_ProviderId",
                table: "Trips",
                column: "ProviderId",
                principalTable: "Providers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Integrators_IntegratorId",
                table: "Users",
                column: "IntegratorId",
                principalTable: "Integrators",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Providers_ProviderId",
                table: "Users",
                column: "ProviderId",
                principalTable: "Providers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trips_Providers_ProviderId",
                table: "Trips");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Integrators_IntegratorId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Providers_ProviderId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_IntegratorId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_ProviderId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Trips_ProviderId",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "IntegratorId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ProviderId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ProviderId",
                table: "Trips");
        }
    }
}
