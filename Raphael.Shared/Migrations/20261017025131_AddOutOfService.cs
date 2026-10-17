using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Raphael.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddOutOfService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OutOfServiceRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    ReasonCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ReasonNote = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    OriginVehicleRouteId = table.Column<int>(type: "int", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartedByUserId = table.Column<int>(type: "int", nullable: true),
                    StartedByName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    EndedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndedByUserId = table.Column<int>(type: "int", nullable: true),
                    EndedByName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    OwnerProviderId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutOfServiceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutOfServiceRecords_Providers_OwnerProviderId",
                        column: x => x.OwnerProviderId,
                        principalTable: "Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OutOfServiceTripImpacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OutOfServiceRecordId = table.Column<int>(type: "int", nullable: false),
                    TripId = table.Column<int>(type: "int", nullable: false),
                    VehicleRouteId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    OldVehicleId = table.Column<int>(type: "int", nullable: true),
                    NewVehicleId = table.Column<int>(type: "int", nullable: true),
                    OldDriverId = table.Column<int>(type: "int", nullable: true),
                    NewDriverId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutOfServiceTripImpacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutOfServiceTripImpacts_OutOfServiceRecords_OutOfServiceRecordId",
                        column: x => x.OutOfServiceRecordId,
                        principalTable: "OutOfServiceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutOfServiceRecords_Operation",
                table: "OutOfServiceRecords",
                column: "OperationId");

            migrationBuilder.CreateIndex(
                name: "IX_OutOfServiceRecords_OwnerProviderId",
                table: "OutOfServiceRecords",
                column: "OwnerProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_OutOfServiceRecords_Subject_Open",
                table: "OutOfServiceRecords",
                columns: new[] { "Subject", "SubjectId" },
                unique: true,
                filter: "[EndedAtUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OutOfServiceTripImpacts_OutOfServiceRecordId",
                table: "OutOfServiceTripImpacts",
                column: "OutOfServiceRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_OutOfServiceTripImpacts_Trip",
                table: "OutOfServiceTripImpacts",
                column: "TripId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutOfServiceTripImpacts");

            migrationBuilder.DropTable(
                name: "OutOfServiceRecords");
        }
    }
}
