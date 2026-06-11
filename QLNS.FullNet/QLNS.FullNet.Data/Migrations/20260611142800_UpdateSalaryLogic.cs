using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLNS.FullNet.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSalaryLogic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ShiftId",
                table: "Timekeepings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WageMultiplier",
                table: "Shifts",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DailyWage",
                table: "Positions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_Timekeepings_ShiftId",
                table: "Timekeepings",
                column: "ShiftId");

            migrationBuilder.AddForeignKey(
                name: "FK_Timekeepings_Shifts_ShiftId",
                table: "Timekeepings",
                column: "ShiftId",
                principalTable: "Shifts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Timekeepings_Shifts_ShiftId",
                table: "Timekeepings");

            migrationBuilder.DropIndex(
                name: "IX_Timekeepings_ShiftId",
                table: "Timekeepings");

            migrationBuilder.DropColumn(
                name: "ShiftId",
                table: "Timekeepings");

            migrationBuilder.DropColumn(
                name: "WageMultiplier",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "DailyWage",
                table: "Positions");
        }
    }
}
