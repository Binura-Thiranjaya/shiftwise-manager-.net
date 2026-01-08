using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TandTFuel.Api.Migrations
{
    /// <inheritdoc />
    public partial class updateEmployee1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "EmployeeShifts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedBy",
                table: "EmployeeShifts",
                type: "uniqueidentifier",
                nullable: true);
        }
    }
}
