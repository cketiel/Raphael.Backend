using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meditrans.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueConstraintToCustomerRiderId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "RiderId",
                table: "Customers",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_RiderId",
                table: "Customers",
                column: "RiderId",
                unique: true,
                filter: "[RiderId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Customers_RiderId",
                table: "Customers");

            migrationBuilder.AlterColumn<string>(
                name: "RiderId",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }
    }
}
