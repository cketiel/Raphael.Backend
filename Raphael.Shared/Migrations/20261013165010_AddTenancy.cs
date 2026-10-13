using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Raphael.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddTenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OwnerProviderId",
                table: "VehicleTypes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerProviderId",
                table: "Vehicles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerProviderId",
                table: "VehicleRoutes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerProviderId",
                table: "VehicleGroups",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerProviderId",
                table: "Units",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerProviderId",
                table: "SpaceTypes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsTenant",
                table: "Providers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OwnerProviderId",
                table: "FundingSources",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerProviderId",
                table: "CapacityDetailTypes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerProviderId",
                table: "Capacities",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerProviderId",
                table: "BillingItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleTypes_OwnerProviderId",
                table: "VehicleTypes",
                column: "OwnerProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_OwnerProviderId",
                table: "Vehicles",
                column: "OwnerProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleRoutes_OwnerProviderId",
                table: "VehicleRoutes",
                column: "OwnerProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleGroups_OwnerProviderId",
                table: "VehicleGroups",
                column: "OwnerProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Units_OwnerProviderId",
                table: "Units",
                column: "OwnerProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_SpaceTypes_OwnerProviderId",
                table: "SpaceTypes",
                column: "OwnerProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_FundingSources_OwnerProviderId",
                table: "FundingSources",
                column: "OwnerProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_CapacityDetailTypes_OwnerProviderId",
                table: "CapacityDetailTypes",
                column: "OwnerProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Capacities_OwnerProviderId",
                table: "Capacities",
                column: "OwnerProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingItems_OwnerProviderId",
                table: "BillingItems",
                column: "OwnerProviderId");

            migrationBuilder.AddForeignKey(
                name: "FK_BillingItems_Providers_OwnerProviderId",
                table: "BillingItems",
                column: "OwnerProviderId",
                principalTable: "Providers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Capacities_Providers_OwnerProviderId",
                table: "Capacities",
                column: "OwnerProviderId",
                principalTable: "Providers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CapacityDetailTypes_Providers_OwnerProviderId",
                table: "CapacityDetailTypes",
                column: "OwnerProviderId",
                principalTable: "Providers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FundingSources_Providers_OwnerProviderId",
                table: "FundingSources",
                column: "OwnerProviderId",
                principalTable: "Providers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SpaceTypes_Providers_OwnerProviderId",
                table: "SpaceTypes",
                column: "OwnerProviderId",
                principalTable: "Providers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Units_Providers_OwnerProviderId",
                table: "Units",
                column: "OwnerProviderId",
                principalTable: "Providers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VehicleGroups_Providers_OwnerProviderId",
                table: "VehicleGroups",
                column: "OwnerProviderId",
                principalTable: "Providers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VehicleRoutes_Providers_OwnerProviderId",
                table: "VehicleRoutes",
                column: "OwnerProviderId",
                principalTable: "Providers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_Providers_OwnerProviderId",
                table: "Vehicles",
                column: "OwnerProviderId",
                principalTable: "Providers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VehicleTypes_Providers_OwnerProviderId",
                table: "VehicleTypes",
                column: "OwnerProviderId",
                principalTable: "Providers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BillingItems_Providers_OwnerProviderId",
                table: "BillingItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Capacities_Providers_OwnerProviderId",
                table: "Capacities");

            migrationBuilder.DropForeignKey(
                name: "FK_CapacityDetailTypes_Providers_OwnerProviderId",
                table: "CapacityDetailTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_FundingSources_Providers_OwnerProviderId",
                table: "FundingSources");

            migrationBuilder.DropForeignKey(
                name: "FK_SpaceTypes_Providers_OwnerProviderId",
                table: "SpaceTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_Units_Providers_OwnerProviderId",
                table: "Units");

            migrationBuilder.DropForeignKey(
                name: "FK_VehicleGroups_Providers_OwnerProviderId",
                table: "VehicleGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_VehicleRoutes_Providers_OwnerProviderId",
                table: "VehicleRoutes");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_Providers_OwnerProviderId",
                table: "Vehicles");

            migrationBuilder.DropForeignKey(
                name: "FK_VehicleTypes_Providers_OwnerProviderId",
                table: "VehicleTypes");

            migrationBuilder.DropIndex(
                name: "IX_VehicleTypes_OwnerProviderId",
                table: "VehicleTypes");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_OwnerProviderId",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_VehicleRoutes_OwnerProviderId",
                table: "VehicleRoutes");

            migrationBuilder.DropIndex(
                name: "IX_VehicleGroups_OwnerProviderId",
                table: "VehicleGroups");

            migrationBuilder.DropIndex(
                name: "IX_Units_OwnerProviderId",
                table: "Units");

            migrationBuilder.DropIndex(
                name: "IX_SpaceTypes_OwnerProviderId",
                table: "SpaceTypes");

            migrationBuilder.DropIndex(
                name: "IX_FundingSources_OwnerProviderId",
                table: "FundingSources");

            migrationBuilder.DropIndex(
                name: "IX_CapacityDetailTypes_OwnerProviderId",
                table: "CapacityDetailTypes");

            migrationBuilder.DropIndex(
                name: "IX_Capacities_OwnerProviderId",
                table: "Capacities");

            migrationBuilder.DropIndex(
                name: "IX_BillingItems_OwnerProviderId",
                table: "BillingItems");

            migrationBuilder.DropColumn(
                name: "OwnerProviderId",
                table: "VehicleTypes");

            migrationBuilder.DropColumn(
                name: "OwnerProviderId",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "OwnerProviderId",
                table: "VehicleRoutes");

            migrationBuilder.DropColumn(
                name: "OwnerProviderId",
                table: "VehicleGroups");

            migrationBuilder.DropColumn(
                name: "OwnerProviderId",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "OwnerProviderId",
                table: "SpaceTypes");

            migrationBuilder.DropColumn(
                name: "IsTenant",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "OwnerProviderId",
                table: "FundingSources");

            migrationBuilder.DropColumn(
                name: "OwnerProviderId",
                table: "CapacityDetailTypes");

            migrationBuilder.DropColumn(
                name: "OwnerProviderId",
                table: "Capacities");

            migrationBuilder.DropColumn(
                name: "OwnerProviderId",
                table: "BillingItems");
        }
    }
}
