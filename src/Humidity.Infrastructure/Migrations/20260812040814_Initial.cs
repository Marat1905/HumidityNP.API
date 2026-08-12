using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Humidity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EntryDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExitDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Counterparty = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Inn = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    VehicleBrand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    VehiclePlate = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Trailer = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Driver = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BaleCount = table.Column<int>(type: "integer", nullable: true),
                    DamagedBaleCount = table.Column<int>(type: "integer", nullable: true),
                    WeightKg = table.Column<double>(type: "double precision", nullable: true),
                    StackNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Measurements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    HumidityValue = table.Column<double>(type: "double precision", nullable: false),
                    TemperatureC = table.Column<double>(type: "double precision", nullable: false),
                    MeasurementType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Material = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Sign = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Measurements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Measurements_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Measurements_Timestamp",
                table: "Measurements",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Measurements_VehicleId",
                table: "Measurements",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_Number",
                table: "Vehicles",
                column: "Number");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_StackNumber",
                table: "Vehicles",
                column: "StackNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_VehiclePlate",
                table: "Vehicles",
                column: "VehiclePlate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Measurements");

            migrationBuilder.DropTable(
                name: "Vehicles");
        }
    }
}
