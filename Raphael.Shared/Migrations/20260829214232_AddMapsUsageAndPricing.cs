using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Raphael.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddMapsUsageAndPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MapsPricingTiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Sku = table.Column<byte>(type: "tinyint", nullable: false),
                    FreeCapPerMonth = table.Column<int>(type: "int", nullable: false),
                    FromRequest = table.Column<int>(type: "int", nullable: false),
                    ToRequest = table.Column<int>(type: "int", nullable: true),
                    PricePerThousand = table.Column<decimal>(type: "decimal(10,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapsPricingTiers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MapsUsageDaily",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Day = table.Column<DateTime>(type: "date", nullable: false),
                    Sku = table.Column<byte>(type: "tinyint", nullable: false),
                    Billed = table.Column<bool>(type: "bit", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapsUsageDaily", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "MapsPricingTiers",
                columns: new[] { "Id", "FreeCapPerMonth", "FromRequest", "PricePerThousand", "Sku", "ToRequest" },
                values: new object[,]
                {
                    { 1, 10000, 10001, 5.00m, (byte)0, 100000 },
                    { 2, 10000, 100001, 4.00m, (byte)0, 500000 },
                    { 3, 10000, 500001, 3.00m, (byte)0, 1000000 },
                    { 4, 10000, 1000001, 1.50m, (byte)0, 5000000 },
                    { 5, 10000, 5000001, 0.38m, (byte)0, null },
                    { 6, 5000, 5001, 10.00m, (byte)1, 100000 },
                    { 7, 5000, 100001, 8.00m, (byte)1, 500000 },
                    { 8, 5000, 500001, 6.00m, (byte)1, 1000000 },
                    { 9, 5000, 1000001, 3.00m, (byte)1, 5000000 },
                    { 10, 5000, 5000001, 0.75m, (byte)1, null },
                    { 11, 10000, 10001, 5.00m, (byte)2, 100000 },
                    { 12, 10000, 100001, 4.00m, (byte)2, 500000 },
                    { 13, 10000, 500001, 3.00m, (byte)2, 1000000 },
                    { 14, 10000, 1000001, 1.50m, (byte)2, 5000000 },
                    { 15, 10000, 5000001, 0.38m, (byte)2, null },
                    { 16, 10000, 10001, 7.00m, (byte)3, 100000 },
                    { 17, 10000, 100001, 5.60m, (byte)3, 500000 },
                    { 18, 10000, 500001, 4.20m, (byte)3, 1000000 },
                    { 19, 10000, 1000001, 2.10m, (byte)3, 5000000 },
                    { 20, 10000, 5000001, 0.53m, (byte)3, null },
                    { 21, 10000, 10001, 2.83m, (byte)4, 100000 },
                    { 22, 10000, 100001, 2.27m, (byte)4, 500000 },
                    { 23, 10000, 500001, 1.70m, (byte)4, 1000000 },
                    { 24, 10000, 1000001, 0.85m, (byte)4, 5000000 },
                    { 25, 10000, 5000001, 0.21m, (byte)4, null },
                    { 26, 10000, 10001, 5.00m, (byte)5, 100000 },
                    { 27, 10000, 100001, 4.00m, (byte)5, 500000 },
                    { 28, 10000, 500001, 3.00m, (byte)5, 1000000 },
                    { 29, 10000, 1000001, 1.50m, (byte)5, 5000000 },
                    { 30, 10000, 5000001, 0.38m, (byte)5, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_MapsPricingTiers_Sku_From",
                table: "MapsPricingTiers",
                columns: new[] { "Sku", "FromRequest" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MapsUsageDaily_Day_Sku_Billed",
                table: "MapsUsageDaily",
                columns: new[] { "Day", "Sku", "Billed" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MapsPricingTiers");

            migrationBuilder.DropTable(
                name: "MapsUsageDaily");
        }
    }
}
