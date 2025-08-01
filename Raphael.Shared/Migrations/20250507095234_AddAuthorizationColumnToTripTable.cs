using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meditrans.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthorizationColumnToTripTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Authorization",
                table: "Trips",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Authorization",
                table: "Trips");
        }
    }
}
