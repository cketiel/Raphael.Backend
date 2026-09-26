using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Raphael.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddDriverCallRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DriverCallRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DriverId = table.Column<int>(type: "int", nullable: false),
                    DriverName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    VehicleRouteId = table.Column<int>(type: "int", nullable: true),
                    RouteName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ProviderId = table.Column<int>(type: "int", nullable: true),
                    OperatingDate = table.Column<DateTime>(type: "date", nullable: false),
                    ScheduleId = table.Column<int>(type: "int", nullable: true),
                    RequestLatitude = table.Column<double>(type: "float", nullable: true),
                    RequestLongitude = table.Column<double>(type: "float", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QueuedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastDriverSignalAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReminderCount = table.Column<int>(type: "int", nullable: false),
                    LastReminderAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CallAttempts = table.Column<int>(type: "int", nullable: false),
                    LastAttemptAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DriverAvailableAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClaimedByUserId = table.Column<int>(type: "int", nullable: true),
                    ClaimedByName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ClaimedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedByUserId = table.Column<int>(type: "int", nullable: true),
                    ResolvedByName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ClosedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReasonCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ResolutionNote = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverCallRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DriverCallRequestEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CallRequestId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    AtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ByUserId = table.Column<int>(type: "int", nullable: true),
                    ByName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Detail = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverCallRequestEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverCallRequestEvents_DriverCallRequests_CallRequestId",
                        column: x => x.CallRequestId,
                        principalTable: "DriverCallRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DriverCallRequestEvents_Request_At",
                table: "DriverCallRequestEvents",
                columns: new[] { "CallRequestId", "AtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DriverCallRequests_Day_Driver",
                table: "DriverCallRequests",
                columns: new[] { "OperatingDate", "DriverId" });

            migrationBuilder.CreateIndex(
                name: "IX_DriverCallRequests_Driver_Open",
                table: "DriverCallRequests",
                column: "DriverId",
                unique: true,
                filter: "[Status] IN (0, 1)");

            migrationBuilder.CreateIndex(
                name: "IX_DriverCallRequests_Status_Queue",
                table: "DriverCallRequests",
                columns: new[] { "Status", "QueuedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DriverCallRequestEvents");

            migrationBuilder.DropTable(
                name: "DriverCallRequests");
        }
    }
}
