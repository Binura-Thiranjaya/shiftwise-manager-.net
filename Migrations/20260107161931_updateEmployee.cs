using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TandTFuel.Api.Migrations
{
    /// <inheritdoc />
    public partial class updateEmployee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "EmployeeShifts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "EmployeeShifts",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
