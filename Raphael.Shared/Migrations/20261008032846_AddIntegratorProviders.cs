using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Raphael.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddIntegratorProviders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IntegratorProviders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IntegratorId = table.Column<int>(type: "int", nullable: false),
                    CatalogProviderId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedByProviderId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegratorProviders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegratorProviders_CatalogProviders_CatalogProviderId",
                        column: x => x.CatalogProviderId,
                        principalTable: "CatalogProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IntegratorProviders_Integrators_IntegratorId",
                        column: x => x.IntegratorId,
                        principalTable: "Integrators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IntegratorProviders_CatalogProvider",
                table: "IntegratorProviders",
                column: "CatalogProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegratorProviders_Integrator_Provider",
                table: "IntegratorProviders",
                columns: new[] { "IntegratorId", "CatalogProviderId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IntegratorProviders");
        }
    }
}
