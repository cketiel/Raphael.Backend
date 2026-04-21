using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Raphael.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexToTrips : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PickupAddress",
                table: "Trips",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "DropoffAddress",
                table: "Trips",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Trip_Unique_Active_Trip",
                table: "Trips",
                columns: new[] { "Date", "CustomerId", "PickupAddress", "DropoffAddress", "FromTime", "ToTime" },
                unique: true,
                filter: "[IsCancelled] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Trip_Unique_Active_Trip",
                table: "Trips");

            migrationBuilder.AlterColumn<string>(
                name: "PickupAddress",
                table: "Trips",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "DropoffAddress",
                table: "Trips",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);
        }
    }
}
