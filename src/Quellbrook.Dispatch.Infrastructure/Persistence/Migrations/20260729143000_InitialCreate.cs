using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quellbrook.Dispatch.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "consignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceLevel = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    PostalCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Zone = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ParcelCount = table.Column<int>(type: "integer", nullable: false),
                    TotalWeightGrams = table.Column<int>(type: "integer", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    RouteId = table.Column<Guid>(type: "uuid", nullable: true),
                    OutForDeliveryAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeliveredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Proof = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consignments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "drivers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Depot = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Licence = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    ShiftStart = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    ShiftEnd = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_drivers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                columns: table => new
                {
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inbox_messages", x => x.MessageId);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DispatchedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Attempts = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "routes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Depot = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Zone = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ServiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Express = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_routes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "vehicles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Registration = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Depot = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Kind = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    CapacityGrams = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "route_stops",
                columns: table => new
                {
                    ConsignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RouteId = table.Column<Guid>(type: "uuid", nullable: false),
                    WeightGrams = table.Column<int>(type: "integer", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_route_stops", x => new { x.RouteId, x.ConsignmentId });
                    table.ForeignKey(
                        name: "FK_route_stops_routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_consignments_OrderId",
                table: "consignments",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_consignments_RouteId",
                table: "consignments",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_inbox_messages_ProcessedAt",
                table: "inbox_messages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_DispatchedAt_OccurredAt",
                table: "outbox_messages",
                columns: new[] { "DispatchedAt", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_routes_ServiceDate_Zone",
                table: "routes",
                columns: new[] { "ServiceDate", "Zone" });

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_Registration",
                table: "vehicles",
                column: "Registration",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "consignments");

            migrationBuilder.DropTable(
                name: "drivers");

            migrationBuilder.DropTable(
                name: "inbox_messages");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "route_stops");

            migrationBuilder.DropTable(
                name: "vehicles");

            migrationBuilder.DropTable(
                name: "routes");
        }
    }
}
