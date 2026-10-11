using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlemanApi.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "car_types",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "STANDARD")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<double>(
                name: "SecurityDeposit",
                table: "car_types",
                type: "double",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "car_types");

            migrationBuilder.DropColumn(
                name: "SecurityDeposit",
                table: "car_types");
        }
    }
}
