using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyEStore.Migrations
{
    /// <inheritdoc />
    public partial class AddDiaChiPhuToKhachHang : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DiaChiPhu",
                table: "KhachHang",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiaChiPhu",
                table: "KhachHang");
        }
    }
}
