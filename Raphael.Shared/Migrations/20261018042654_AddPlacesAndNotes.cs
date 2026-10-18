using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Raphael.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddPlacesAndNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    OwnerProviderId = table.Column<int>(type: "int", nullable: true),
                    Text = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedByName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerNotes_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerNotes_Providers_OwnerProviderId",
                        column: x => x.OwnerProviderId,
                        principalTable: "Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Places",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AddressKey = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    CatalogIntegratorId = table.Column<int>(type: "int", nullable: true),
                    Source = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    OwnerProviderId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Places", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Places_CatalogIntegrators_CatalogIntegratorId",
                        column: x => x.CatalogIntegratorId,
                        principalTable: "CatalogIntegrators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Places_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Places_Providers_OwnerProviderId",
                        column: x => x.OwnerProviderId,
                        principalTable: "Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlaceEntrances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlaceId = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaceEntrances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlaceEntrances_Places_PlaceId",
                        column: x => x.PlaceId,
                        principalTable: "Places",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlaceNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlaceId = table.Column<int>(type: "int", nullable: false),
                    PlaceEntranceId = table.Column<int>(type: "int", nullable: true),
                    Text = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedByName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaceNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlaceNotes_PlaceEntrances_PlaceEntranceId",
                        column: x => x.PlaceEntranceId,
                        principalTable: "PlaceEntrances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlaceNotes_Places_PlaceId",
                        column: x => x.PlaceId,
                        principalTable: "Places",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TripStopNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TripId = table.Column<int>(type: "int", nullable: false),
                    Side = table.Column<int>(type: "int", nullable: false),
                    PlaceId = table.Column<int>(type: "int", nullable: true),
                    PlaceEntranceId = table.Column<int>(type: "int", nullable: true),
                    FileComment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TripNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripStopNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripStopNotes_PlaceEntrances_PlaceEntranceId",
                        column: x => x.PlaceEntranceId,
                        principalTable: "PlaceEntrances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TripStopNotes_Places_PlaceId",
                        column: x => x.PlaceId,
                        principalTable: "Places",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TripStopNotes_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerNotes_Customer_Status",
                table: "CustomerNotes",
                columns: new[] { "CustomerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerNotes_OwnerProviderId",
                table: "CustomerNotes",
                column: "OwnerProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_PlaceEntrances_LatLng",
                table: "PlaceEntrances",
                columns: new[] { "Latitude", "Longitude" });

            migrationBuilder.CreateIndex(
                name: "IX_PlaceEntrances_PlaceId",
                table: "PlaceEntrances",
                column: "PlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_PlaceNotes_Place_Status",
                table: "PlaceNotes",
                columns: new[] { "PlaceId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PlaceNotes_PlaceEntranceId",
                table: "PlaceNotes",
                column: "PlaceEntranceId");

            migrationBuilder.CreateIndex(
                name: "IX_Places_AddressKey",
                table: "Places",
                column: "AddressKey");

            migrationBuilder.CreateIndex(
                name: "IX_Places_CatalogIntegratorId",
                table: "Places",
                column: "CatalogIntegratorId");

            migrationBuilder.CreateIndex(
                name: "IX_Places_CustomerId",
                table: "Places",
                column: "CustomerId",
                filter: "[CustomerId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Places_OwnerProviderId",
                table: "Places",
                column: "OwnerProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_TripStopNotes_PlaceEntranceId",
                table: "TripStopNotes",
                column: "PlaceEntranceId");

            migrationBuilder.CreateIndex(
                name: "IX_TripStopNotes_PlaceId",
                table: "TripStopNotes",
                column: "PlaceId",
                filter: "[PlaceId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_TripStopNotes_Trip_Side",
                table: "TripStopNotes",
                columns: new[] { "TripId", "Side" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerNotes");

            migrationBuilder.DropTable(
                name: "PlaceNotes");

            migrationBuilder.DropTable(
                name: "TripStopNotes");

            migrationBuilder.DropTable(
                name: "PlaceEntrances");

            migrationBuilder.DropTable(
                name: "Places");
        }
    }
}
