using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Raphael.Shared.Migrations
{
    /// <inheritdoc />
    public partial class BillingItemOwnerIntegrator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OwnerIntegratorId",
                table: "BillingItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingItems_OwnerIntegratorId",
                table: "BillingItems",
                column: "OwnerIntegratorId");

            migrationBuilder.AddForeignKey(
                name: "FK_BillingItems_Integrators_OwnerIntegratorId",
                table: "BillingItems",
                column: "OwnerIntegratorId",
                principalTable: "Integrators",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BillingItems_Integrators_OwnerIntegratorId",
                table: "BillingItems");

            migrationBuilder.DropIndex(
                name: "IX_BillingItems_OwnerIntegratorId",
                table: "BillingItems");

            migrationBuilder.DropColumn(
                name: "OwnerIntegratorId",
                table: "BillingItems");
        }
    }
}
