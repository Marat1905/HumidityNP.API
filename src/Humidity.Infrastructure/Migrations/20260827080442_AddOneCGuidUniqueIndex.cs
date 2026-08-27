using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Humidity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOneCGuidUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OneCGuid",
                table: "Vehicles",
                type: "character varying(50)",
                unicode: false,
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_OneCGuid",
                table: "Vehicles",
                column: "OneCGuid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vehicles_OneCGuid",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "OneCGuid",
                table: "Vehicles");
        }
    }
}
