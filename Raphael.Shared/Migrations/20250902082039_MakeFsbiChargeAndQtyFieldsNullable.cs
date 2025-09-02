using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Raphael.Shared.Migrations
{
    /// <inheritdoc />
    public partial class MakeFsbiChargeAndQtyFieldsNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "MinCharge",
                table: "FundingSourceBillingItems",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "MaxCharge",
                table: "FundingSourceBillingItems",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<int>(
                name: "LessOrEqualMaxQty",
                table: "FundingSourceBillingItems",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "GreaterThanMinQty",
                table: "FundingSourceBillingItems",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "FreeQty",
                table: "FundingSourceBillingItems",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "UnitId1",
                table: "BillingItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingItems_UnitId1",
                table: "BillingItems",
                column: "UnitId1");

            migrationBuilder.AddForeignKey(
                name: "FK_BillingItems_Units_UnitId1",
                table: "BillingItems",
                column: "UnitId1",
                principalTable: "Units",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BillingItems_Units_UnitId1",
                table: "BillingItems");

            migrationBuilder.DropIndex(
                name: "IX_BillingItems_UnitId1",
                table: "BillingItems");

            migrationBuilder.DropColumn(
                name: "UnitId1",
                table: "BillingItems");

            migrationBuilder.AlterColumn<decimal>(
                name: "MinCharge",
                table: "FundingSourceBillingItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "MaxCharge",
                table: "FundingSourceBillingItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "LessOrEqualMaxQty",
                table: "FundingSourceBillingItems",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "GreaterThanMinQty",
                table: "FundingSourceBillingItems",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "FreeQty",
                table: "FundingSourceBillingItems",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
