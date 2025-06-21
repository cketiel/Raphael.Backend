using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meditrans.Shared.Migrations
{
    /// <inheritdoc />
    public partial class ImprovedScheduleStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Distance",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "ETA",
                table: "Schedules");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "ActualArriveTime",
                table: "Schedules",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "ActualPerformTime",
                table: "Schedules",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Schedules",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "ArriveDistance",
                table: "Schedules",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuthNo",
                table: "Schedules",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "Schedules",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DistanceToPoint",
                table: "Schedules",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "ETATime",
                table: "Schedules",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EventType",
                table: "Schedules",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FundingSourceName",
                table: "Schedules",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GpsArrive",
                table: "Schedules",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Schedules",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "Odometer",
                table: "Schedules",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PerformDistance",
                table: "Schedules",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Schedules",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "ScheduledApptTime",
                table: "Schedules",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "ScheduledPickupTime",
                table: "Schedules",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Sequence",
                table: "Schedules",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SpaceTypeName",
                table: "Schedules",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "TravelTime",
                table: "Schedules",
                type: "time",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualArriveTime",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "ActualPerformTime",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "ArriveDistance",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "AuthNo",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "Comment",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "DistanceToPoint",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "ETATime",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "EventType",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "FundingSourceName",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "GpsArrive",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "Odometer",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "PerformDistance",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "ScheduledApptTime",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "ScheduledPickupTime",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "Sequence",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "SpaceTypeName",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "TravelTime",
                table: "Schedules");

            migrationBuilder.AddColumn<decimal>(
                name: "Distance",
                table: "Schedules",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "ETA",
                table: "Schedules",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));
        }
    }
}
