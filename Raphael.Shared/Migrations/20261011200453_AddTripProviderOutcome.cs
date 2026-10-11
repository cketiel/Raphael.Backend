using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Raphael.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddTripProviderOutcome : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "ProviderDropoffArrivedAt",
                table: "Trips",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "ProviderDroppedOffAt",
                table: "Trips",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProviderOutcome",
                table: "Trips",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProviderOutcomeAtUtc",
                table: "Trips",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProviderOutcomeByUserId",
                table: "Trips",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderOutcomeReason",
                table: "Trips",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "ProviderPickedUpAt",
                table: "Trips",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "ProviderPickupArrivedAt",
                table: "Trips",
                type: "time",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProviderDropoffArrivedAt",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "ProviderDroppedOffAt",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "ProviderOutcome",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "ProviderOutcomeAtUtc",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "ProviderOutcomeByUserId",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "ProviderOutcomeReason",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "ProviderPickedUpAt",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "ProviderPickupArrivedAt",
                table: "Trips");
        }
    }
}
