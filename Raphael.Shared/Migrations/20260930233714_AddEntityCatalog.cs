using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Raphael.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddEntityCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CatalogProviderId",
                table: "Providers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Comments",
                table: "Providers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactName",
                table: "Providers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerProviderId",
                table: "Providers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "Providers",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Integrators",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CatalogIntegratorId",
                table: "Integrators",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Comments",
                table: "Integrators",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactName",
                table: "Integrators",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Integrators",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Integrators",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Integrators",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerProviderId",
                table: "Integrators",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Integrators",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "Integrators",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CatalogIntegratorCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameEs = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogIntegratorCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CatalogProviderCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameEs = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogProviderCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Counties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    State = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Counties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CatalogIntegrators",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CountyId = table.Column<int>(type: "int", nullable: true),
                    CountyRaw = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    State = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    Zip = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    PhoneDigits = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ContactName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FacilityType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Beds = table.Column<int>(type: "int", nullable: true),
                    ChainName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    GeocodeStatus = table.Column<int>(type: "int", nullable: false),
                    GeocodedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MatchKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SourceFile = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    SourceRow = table.Column<int>(type: "int", nullable: true),
                    ImportBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedByProviderId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByProviderId = table.Column<int>(type: "int", nullable: true),
                    SearchText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogIntegrators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatalogIntegrators_CatalogIntegratorCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "CatalogIntegratorCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CatalogIntegrators_Counties_CountyId",
                        column: x => x.CountyId,
                        principalTable: "Counties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CatalogProviders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CountyId = table.Column<int>(type: "int", nullable: true),
                    CountyRaw = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    State = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    Zip = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    PhoneDigits = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ContactName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Npi = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsPrimaryNemt = table.Column<bool>(type: "bit", nullable: true),
                    SourceUpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EmsLicense = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    ServiceLevel = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    LicenseExpiresOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PlanSegment = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CoverageArea = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ProviderContact = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    EvidenceNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    GeocodeStatus = table.Column<int>(type: "int", nullable: false),
                    GeocodedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MatchKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SourceFile = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    SourceRow = table.Column<int>(type: "int", nullable: true),
                    ImportBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedByProviderId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByProviderId = table.Column<int>(type: "int", nullable: true),
                    SearchText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogProviders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatalogProviders_CatalogProviderCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "CatalogProviderCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CatalogProviders_Counties_CountyId",
                        column: x => x.CountyId,
                        principalTable: "Counties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "CatalogIntegratorCategories",
                columns: new[] { "Id", "Code", "DisplayOrder", "IsActive", "NameEn", "NameEs" },
                values: new object[,]
                {
                    { 1, "NursingHome", 1, true, "Nursing homes", "Nursing homes" },
                    { 2, "AssistedLiving", 2, true, "Assisted living / residential care", "Assisted living / residencias de cuidado" },
                    { 3, "Hospital", 3, true, "Hospitals", "Hospitales" }
                });

            migrationBuilder.InsertData(
                table: "CatalogProviderCategories",
                columns: new[] { "Id", "Code", "DisplayOrder", "IsActive", "NameEn", "NameEs" },
                values: new object[,]
                {
                    { 1, "NemtCompany", 1, true, "NEMT companies", "Empresas NEMT" },
                    { 2, "NemtBroker", 2, true, "NEMT brokers", "Brokers NEMT" },
                    { 3, "PrivateAmbulance", 3, true, "Private ambulance companies", "Empresas de ambulancias privadas" }
                });

            migrationBuilder.InsertData(
                table: "Counties",
                columns: new[] { "Id", "Name", "State" },
                values: new object[,]
                {
                    { 1, "Alachua", "FL" },
                    { 2, "Baker", "FL" },
                    { 3, "Bay", "FL" },
                    { 4, "Bradford", "FL" },
                    { 5, "Brevard", "FL" },
                    { 6, "Broward", "FL" },
                    { 7, "Calhoun", "FL" },
                    { 8, "Charlotte", "FL" },
                    { 9, "Citrus", "FL" },
                    { 10, "Clay", "FL" },
                    { 11, "Collier", "FL" },
                    { 12, "Columbia", "FL" },
                    { 13, "DeSoto", "FL" },
                    { 14, "Dixie", "FL" },
                    { 15, "Duval", "FL" },
                    { 16, "Escambia", "FL" },
                    { 17, "Flagler", "FL" },
                    { 18, "Franklin", "FL" },
                    { 19, "Gadsden", "FL" },
                    { 20, "Gilchrist", "FL" },
                    { 21, "Glades", "FL" },
                    { 22, "Gulf", "FL" },
                    { 23, "Hamilton", "FL" },
                    { 24, "Hardee", "FL" },
                    { 25, "Hendry", "FL" },
                    { 26, "Hernando", "FL" },
                    { 27, "Highlands", "FL" },
                    { 28, "Hillsborough", "FL" },
                    { 29, "Holmes", "FL" },
                    { 30, "Indian River", "FL" },
                    { 31, "Jackson", "FL" },
                    { 32, "Jefferson", "FL" },
                    { 33, "Lafayette", "FL" },
                    { 34, "Lake", "FL" },
                    { 35, "Lee", "FL" },
                    { 36, "Leon", "FL" },
                    { 37, "Levy", "FL" },
                    { 38, "Liberty", "FL" },
                    { 39, "Madison", "FL" },
                    { 40, "Manatee", "FL" },
                    { 41, "Marion", "FL" },
                    { 42, "Martin", "FL" },
                    { 43, "Miami-Dade", "FL" },
                    { 44, "Monroe", "FL" },
                    { 45, "Nassau", "FL" },
                    { 46, "Okaloosa", "FL" },
                    { 47, "Okeechobee", "FL" },
                    { 48, "Orange", "FL" },
                    { 49, "Osceola", "FL" },
                    { 50, "Palm Beach", "FL" },
                    { 51, "Pasco", "FL" },
                    { 52, "Pinellas", "FL" },
                    { 53, "Polk", "FL" },
                    { 54, "Putnam", "FL" },
                    { 55, "Santa Rosa", "FL" },
                    { 56, "Sarasota", "FL" },
                    { 57, "Seminole", "FL" },
                    { 58, "St. Johns", "FL" },
                    { 59, "St. Lucie", "FL" },
                    { 60, "Sumter", "FL" },
                    { 61, "Suwannee", "FL" },
                    { 62, "Taylor", "FL" },
                    { 63, "Union", "FL" },
                    { 64, "Volusia", "FL" },
                    { 65, "Wakulla", "FL" },
                    { 66, "Walton", "FL" },
                    { 67, "Washington", "FL" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Providers_Catalog_Owner",
                table: "Providers",
                columns: new[] { "CatalogProviderId", "OwnerProviderId" });

            migrationBuilder.CreateIndex(
                name: "IX_Integrators_Catalog_Owner",
                table: "Integrators",
                columns: new[] { "CatalogIntegratorId", "OwnerProviderId" });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogIntegratorCategories_Code",
                table: "CatalogIntegratorCategories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatalogIntegrators_Category_Name",
                table: "CatalogIntegrators",
                columns: new[] { "CategoryId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogIntegrators_City",
                table: "CatalogIntegrators",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogIntegrators_County",
                table: "CatalogIntegrators",
                column: "CountyId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogIntegrators_MatchKey",
                table: "CatalogIntegrators",
                column: "MatchKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatalogIntegrators_PhoneDigits",
                table: "CatalogIntegrators",
                column: "PhoneDigits");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogProviderCategories_Code",
                table: "CatalogProviderCategories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatalogProviders_Category_Name",
                table: "CatalogProviders",
                columns: new[] { "CategoryId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogProviders_City",
                table: "CatalogProviders",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogProviders_County",
                table: "CatalogProviders",
                column: "CountyId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogProviders_MatchKey",
                table: "CatalogProviders",
                column: "MatchKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatalogProviders_Npi",
                table: "CatalogProviders",
                column: "Npi",
                filter: "[Npi] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogProviders_PhoneDigits",
                table: "CatalogProviders",
                column: "PhoneDigits");

            migrationBuilder.CreateIndex(
                name: "IX_Counties_State_Name",
                table: "Counties",
                columns: new[] { "State", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Integrators_CatalogIntegrators_CatalogIntegratorId",
                table: "Integrators",
                column: "CatalogIntegratorId",
                principalTable: "CatalogIntegrators",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Providers_CatalogProviders_CatalogProviderId",
                table: "Providers",
                column: "CatalogProviderId",
                principalTable: "CatalogProviders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Integrators_CatalogIntegrators_CatalogIntegratorId",
                table: "Integrators");

            migrationBuilder.DropForeignKey(
                name: "FK_Providers_CatalogProviders_CatalogProviderId",
                table: "Providers");

            migrationBuilder.DropTable(
                name: "CatalogIntegrators");

            migrationBuilder.DropTable(
                name: "CatalogProviders");

            migrationBuilder.DropTable(
                name: "CatalogIntegratorCategories");

            migrationBuilder.DropTable(
                name: "CatalogProviderCategories");

            migrationBuilder.DropTable(
                name: "Counties");

            migrationBuilder.DropIndex(
                name: "IX_Providers_Catalog_Owner",
                table: "Providers");

            migrationBuilder.DropIndex(
                name: "IX_Integrators_Catalog_Owner",
                table: "Integrators");

            migrationBuilder.DropColumn(
                name: "CatalogProviderId",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "Comments",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "ContactName",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "OwnerProviderId",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Integrators");

            migrationBuilder.DropColumn(
                name: "CatalogIntegratorId",
                table: "Integrators");

            migrationBuilder.DropColumn(
                name: "Comments",
                table: "Integrators");

            migrationBuilder.DropColumn(
                name: "ContactName",
                table: "Integrators");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Integrators");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Integrators");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Integrators");

            migrationBuilder.DropColumn(
                name: "OwnerProviderId",
                table: "Integrators");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Integrators");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "Integrators");
        }
    }
}
