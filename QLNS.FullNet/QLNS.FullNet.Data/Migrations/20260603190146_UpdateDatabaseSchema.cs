using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLNS.FullNet.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDatabaseSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "CheckInTime",
                table: "Timekeepings",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "CheckOutTime",
                table: "Timekeepings",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Date",
                table: "Timekeepings",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "EmployeeId",
                table: "Timekeepings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "Timekeepings",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Timekeepings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Timekeepings_EmployeeId",
                table: "Timekeepings",
                column: "EmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Timekeepings_Employees_EmployeeId",
                table: "Timekeepings",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Timekeepings_Employees_EmployeeId",
                table: "Timekeepings");

            migrationBuilder.DropIndex(
                name: "IX_Timekeepings_EmployeeId",
                table: "Timekeepings");

            migrationBuilder.DropColumn(
                name: "CheckInTime",
                table: "Timekeepings");

            migrationBuilder.DropColumn(
                name: "CheckOutTime",
                table: "Timekeepings");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "Timekeepings");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "Timekeepings");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "Timekeepings");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Timekeepings");
        }
    }
}
