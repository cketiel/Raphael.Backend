using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Raphael.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddFundingSourceToIntegrator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FundingSourceId",
                table: "Integrators",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Integrators_FundingSourceId",
                table: "Integrators",
                column: "FundingSourceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Integrators_FundingSources_FundingSourceId",
                table: "Integrators",
                column: "FundingSourceId",
                principalTable: "FundingSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Integrators_FundingSources_FundingSourceId",
                table: "Integrators");

            migrationBuilder.DropIndex(
                name: "IX_Integrators_FundingSourceId",
                table: "Integrators");

            migrationBuilder.DropColumn(
                name: "FundingSourceId",
                table: "Integrators");
        }
    }
}
