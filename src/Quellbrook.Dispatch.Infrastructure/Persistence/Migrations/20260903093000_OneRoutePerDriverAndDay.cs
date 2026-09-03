using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quellbrook.Dispatch.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OneRoutePerDriverAndDay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_routes_DriverId_ServiceDate",
                table: "routes",
                columns: new[] { "DriverId", "ServiceDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_routes_DriverId_ServiceDate",
                table: "routes");
        }
    }
}
