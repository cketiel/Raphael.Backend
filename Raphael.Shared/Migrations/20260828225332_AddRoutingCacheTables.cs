using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Raphael.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddRoutingCacheTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GeocodeCache",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NormalizedAddress = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    PlaceId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    FormattedAddress = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    FetchedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeocodeCache", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ObservedLegTimes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OriginLatE4 = table.Column<int>(type: "int", nullable: false),
                    OriginLngE4 = table.Column<int>(type: "int", nullable: false),
                    DestLatE4 = table.Column<int>(type: "int", nullable: false),
                    DestLngE4 = table.Column<int>(type: "int", nullable: false),
                    TimeBucket = table.Column<byte>(type: "tinyint", nullable: false),
                    DayType = table.Column<byte>(type: "tinyint", nullable: false),
                    DurationSeconds = table.Column<int>(type: "int", nullable: false),
                    DistanceMeters = table.Column<int>(type: "int", nullable: true),
                    Source = table.Column<byte>(type: "tinyint", nullable: false),
                    VehicleRouteId = table.Column<int>(type: "int", nullable: true),
                    ScheduleId = table.Column<int>(type: "int", nullable: true),
                    ObservedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObservedLegTimes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RouteLegCache",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OriginLatE4 = table.Column<int>(type: "int", nullable: false),
                    OriginLngE4 = table.Column<int>(type: "int", nullable: false),
                    DestLatE4 = table.Column<int>(type: "int", nullable: false),
                    DestLngE4 = table.Column<int>(type: "int", nullable: false),
                    TimeBucket = table.Column<byte>(type: "tinyint", nullable: false),
                    DayType = table.Column<byte>(type: "tinyint", nullable: false),
                    TrafficMode = table.Column<byte>(type: "tinyint", nullable: false),
                    DurationSeconds = table.Column<int>(type: "int", nullable: false),
                    DurationInTrafficSeconds = table.Column<int>(type: "int", nullable: true),
                    DistanceMeters = table.Column<int>(type: "int", nullable: false),
                    FetchedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteLegCache", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GeocodeCache_Address",
                table: "GeocodeCache",
                column: "NormalizedAddress",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GeocodeCache_FetchedAtUtc",
                table: "GeocodeCache",
                column: "FetchedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ObservedLegTimes_Leg",
                table: "ObservedLegTimes",
                columns: new[] { "OriginLatE4", "OriginLngE4", "DestLatE4", "DestLngE4", "DayType", "TimeBucket" });

            migrationBuilder.CreateIndex(
                name: "IX_ObservedLegTimes_ObservedAtUtc",
                table: "ObservedLegTimes",
                column: "ObservedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_RouteLegCache_FetchedAtUtc",
                table: "RouteLegCache",
                column: "FetchedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_RouteLegCache_Leg",
                table: "RouteLegCache",
                columns: new[] { "OriginLatE4", "OriginLngE4", "DestLatE4", "DestLngE4", "TimeBucket", "DayType", "TrafficMode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_Key",
                table: "SystemSettings",
                column: "Key",
                unique: true);

            // Seeded so the panel has something to show and the routing service has a stated
            // default rather than a hard-coded one. MaxSavings is the deliberate starting point:
            // a traffic-aware request costs twice as much and caches per hour, so the system
            // begins cheap and an administrator opts into precision when the office asks for it.
            migrationBuilder.InsertData(
                table: "SystemSettings",
                columns: new[] { "Key", "Value", "Description", "UpdatedAtUtc", "UpdatedBy" },
                values: new object[,]
                {
                    {
                        "Routing.TrafficMode",
                        "MaxSavings",
                        "How travel times are bought. MaxSavings: free-flow time plus our own buffer, "
                            + "at half the price and cached once per pair of points. Precision: Google's "
                            + "traffic estimate at the scheduled departure hour, twice the price and "
                            + "cached per hour of the day.",
                        new DateTime(2026, 8, 28, 0, 0, 0, DateTimeKind.Utc),
                        null
                    },
                    {
                        "Routing.DefaultBufferPercent",
                        "12",
                        "Whole percent added to a free-flow duration in MaxSavings mode, until there "
                            + "are enough observed times to calibrate it per hour.",
                        new DateTime(2026, 8, 28, 0, 0, 0, DateTimeKind.Utc),
                        null
                    }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GeocodeCache");

            migrationBuilder.DropTable(
                name: "ObservedLegTimes");

            migrationBuilder.DropTable(
                name: "RouteLegCache");

            migrationBuilder.DropTable(
                name: "SystemSettings");
        }
    }
}
