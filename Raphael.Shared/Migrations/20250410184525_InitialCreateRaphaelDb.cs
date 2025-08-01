using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Raphael.Shared.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateRaphaelDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Capacities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Capacities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FundingSources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FAX = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactFirst = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactLast = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SignaturePickup = table.Column<bool>(type: "bit", nullable: false),
                    SignatureDropoff = table.Column<bool>(type: "bit", nullable: false),
                    DriverSignaturePickup = table.Column<bool>(type: "bit", nullable: false),
                    DriverSignatureDropoff = table.Column<bool>(type: "bit", nullable: false),
                    RequireOdometer = table.Column<bool>(type: "bit", nullable: false),
                    BarcodeScanRequired = table.Column<bool>(type: "bit", nullable: false),
                    VectorcareFacilityId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundingSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Providers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Logo = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Providers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Units",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Abbreviation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Units", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VehicleGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Color = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SpaceTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LoadTime = table.Column<int>(type: "int", nullable: false),
                    UnloadTime = table.Column<int>(type: "int", nullable: false),
                    CapacityTypeId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpaceTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpaceTypes_Capacities_CapacityTypeId",
                        column: x => x.CapacityTypeId,
                        principalTable: "Capacities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DriverLicense = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BillingItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UnitId = table.Column<int>(type: "int", nullable: false),
                    IsCopay = table.Column<bool>(type: "bit", nullable: false),
                    ARAccount = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ARSubAccount = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ARCompany = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    APAccount = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    APSubAccount = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    APCompany = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillingItems_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VIN = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Brand = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Model = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Color = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Plate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsInactive = table.Column<bool>(type: "bit", nullable: false),
                    GroupId = table.Column<int>(type: "int", nullable: false),
                    CapacityTypeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vehicles_Capacities_CapacityTypeId",
                        column: x => x.CapacityTypeId,
                        principalTable: "Capacities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Vehicles_VehicleGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "VehicleGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CapacityDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SpaceTypeId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CapacityDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CapacityDetails_SpaceTypes_SpaceTypeId",
                        column: x => x.SpaceTypeId,
                        principalTable: "SpaceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Zip = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MobilePhone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClientCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PolicyNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FundingSourceId = table.Column<int>(type: "int", nullable: false),
                    SpaceTypeId = table.Column<int>(type: "int", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DOB = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Customers_FundingSources_FundingSourceId",
                        column: x => x.FundingSourceId,
                        principalTable: "FundingSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Customers_SpaceTypes_SpaceTypeId",
                        column: x => x.SpaceTypeId,
                        principalTable: "SpaceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FundingSourceBillingItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FundingSourceId = table.Column<int>(type: "int", nullable: false),
                    BillingItemId = table.Column<int>(type: "int", nullable: false),
                    SpaceTypeId = table.Column<int>(type: "int", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Per = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    ProcedureCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MinCharge = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxCharge = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GreaterThanMinQty = table.Column<int>(type: "int", nullable: false),
                    LessOrEqualMaxQty = table.Column<int>(type: "int", nullable: false),
                    FreeQty = table.Column<int>(type: "int", nullable: false),
                    FromDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ToDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundingSourceBillingItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FundingSourceBillingItems_BillingItems_BillingItemId",
                        column: x => x.BillingItemId,
                        principalTable: "BillingItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FundingSourceBillingItems_FundingSources_FundingSourceId",
                        column: x => x.FundingSourceId,
                        principalTable: "FundingSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FundingSourceBillingItems_SpaceTypes_SpaceTypeId",
                        column: x => x.SpaceTypeId,
                        principalTable: "SpaceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VehicleRoutes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DriverId = table.Column<int>(type: "int", nullable: false),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    Garage = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleRoutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleRoutes_Users_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VehicleRoutes_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Trips",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Day = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FromTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    ToTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    PickupAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PickupLatitude = table.Column<double>(type: "float", nullable: false),
                    PickupLongitude = table.Column<double>(type: "float", nullable: false),
                    DropoffAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DropoffLatitude = table.Column<double>(type: "float", nullable: false),
                    DropoffLongitude = table.Column<double>(type: "float", nullable: false),
                    SpaceTypeId = table.Column<int>(type: "int", nullable: false),
                    PickupNote = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsCancelled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Trips_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Trips_SpaceTypes_SpaceTypeId",
                        column: x => x.SpaceTypeId,
                        principalTable: "SpaceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "Schedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TripId = table.Column<int>(type: "int", nullable: false),
                    VehicleRouteId = table.Column<int>(type: "int", nullable: false),
                    ETA = table.Column<TimeSpan>(type: "time", nullable: false),
                    Distance = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Schedules_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Schedules_VehicleRoutes_VehicleRouteId",
                        column: x => x.VehicleRouteId,
                        principalTable: "VehicleRoutes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BillingItems_UnitId",
                table: "BillingItems",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_CapacityDetails_SpaceTypeId",
                table: "CapacityDetails",
                column: "SpaceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_FundingSourceId",
                table: "Customers",
                column: "FundingSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_SpaceTypeId",
                table: "Customers",
                column: "SpaceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_FundingSourceBillingItems_BillingItemId",
                table: "FundingSourceBillingItems",
                column: "BillingItemId");

            migrationBuilder.CreateIndex(
                name: "IX_FundingSourceBillingItems_FundingSourceId",
                table: "FundingSourceBillingItems",
                column: "FundingSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_FundingSourceBillingItems_SpaceTypeId",
                table: "FundingSourceBillingItems",
                column: "SpaceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_TripId",
                table: "Schedules",
                column: "TripId");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_VehicleRouteId",
                table: "Schedules",
                column: "VehicleRouteId");

            migrationBuilder.CreateIndex(
                name: "IX_SpaceTypes_CapacityTypeId",
                table: "SpaceTypes",
                column: "CapacityTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_CustomerId",
                table: "Trips",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_SpaceTypeId",
                table: "Trips",
                column: "SpaceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleRoutes_DriverId",
                table: "VehicleRoutes",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleRoutes_VehicleId",
                table: "VehicleRoutes",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_CapacityTypeId",
                table: "Vehicles",
                column: "CapacityTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_GroupId",
                table: "Vehicles",
                column: "GroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CapacityDetails");

            migrationBuilder.DropTable(
                name: "FundingSourceBillingItems");

            migrationBuilder.DropTable(
                name: "Providers");

            migrationBuilder.DropTable(
                name: "Schedules");

            migrationBuilder.DropTable(
                name: "BillingItems");

            migrationBuilder.DropTable(
                name: "Trips");

            migrationBuilder.DropTable(
                name: "VehicleRoutes");

            migrationBuilder.DropTable(
                name: "Units");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Vehicles");

            migrationBuilder.DropTable(
                name: "FundingSources");

            migrationBuilder.DropTable(
                name: "SpaceTypes");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "VehicleGroups");

            migrationBuilder.DropTable(
                name: "Capacities");
        }
    }
}

