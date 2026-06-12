using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLNS.FullNet.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeTimekeepingStatusNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Đổi cột Status từ NOT NULL (defaultValue "") thành nullable
            // để tương thích với dữ liệu cũ có thể có NULL
            // Fix: Status column was never added, so AddColumn instead of AlterColumn
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Timekeepings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
