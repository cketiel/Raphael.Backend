using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Raphael.Shared.Migrations
{
    /// <inheritdoc />
    public partial class NotesWorkerAndUniquePlaces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // RE-054 starts from zero (decided 2026-10-18): places and notes are created as trips come
            // in. Whatever the first version left in these tables (DEV only; production never ran it
            // with data) goes, so the unique indexes below cannot meet an old duplicate. Trips are not
            // touched: their comments stay as they are and the notes worker composes them again.
            migrationBuilder.Sql("DELETE FROM [PlaceNotes]; DELETE FROM [CustomerNotes]; DELETE FROM [TripStopNotes]; DELETE FROM [PlaceEntrances]; DELETE FROM [Places];");

            migrationBuilder.DropIndex(
                name: "IX_Places_AddressKey",
                table: "Places");

            migrationBuilder.DropIndex(
                name: "IX_Places_CustomerId",
                table: "Places");

            migrationBuilder.AddColumn<int>(
                name: "ComposedHash",
                table: "TripStopNotes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PlaceChosenByUser",
                table: "TripStopNotes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessedAtUtc",
                table: "TripStopNotes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AddressKey",
                table: "Places",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.CreateIndex(
                name: "UX_Places_Facility",
                table: "Places",
                columns: new[] { "OwnerProviderId", "AddressKey" },
                unique: true,
                filter: "[CustomerId] IS NULL AND [IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_Places_Home",
                table: "Places",
                columns: new[] { "CustomerId", "OwnerProviderId", "AddressKey" },
                unique: true,
                filter: "[CustomerId] IS NOT NULL AND [IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Places_Facility",
                table: "Places");

            migrationBuilder.DropIndex(
                name: "UX_Places_Home",
                table: "Places");

            migrationBuilder.DropColumn(
                name: "ComposedHash",
                table: "TripStopNotes");

            migrationBuilder.DropColumn(
                name: "PlaceChosenByUser",
                table: "TripStopNotes");

            migrationBuilder.DropColumn(
                name: "ProcessedAtUtc",
                table: "TripStopNotes");

            migrationBuilder.AlterColumn<string>(
                name: "AddressKey",
                table: "Places",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(400)",
                oldMaxLength: 400);

            migrationBuilder.CreateIndex(
                name: "IX_Places_AddressKey",
                table: "Places",
                column: "AddressKey");

            migrationBuilder.CreateIndex(
                name: "IX_Places_CustomerId",
                table: "Places",
                column: "CustomerId",
                filter: "[CustomerId] IS NOT NULL");
        }
    }
}
