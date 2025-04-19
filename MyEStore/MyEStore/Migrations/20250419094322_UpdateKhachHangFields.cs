using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyEStore.Migrations
{
    /// <inheritdoc />
    public partial class UpdateKhachHangFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HangHoaChiTiets_MaHh",
                table: "HangHoaChiTiets");

            migrationBuilder.AlterColumn<string>(
                name: "MatKhau",
                table: "KhachHang",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DiaChi",
                table: "KhachHang",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(60)",
                oldMaxLength: 60,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HangHoaChiTiets_MaHh",
                table: "HangHoaChiTiets",
                column: "MaHh",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HangHoaChiTiets_MaHh",
                table: "HangHoaChiTiets");

            migrationBuilder.AlterColumn<string>(
                name: "MatKhau",
                table: "KhachHang",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DiaChi",
                table: "KhachHang",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HangHoaChiTiets_MaHh",
                table: "HangHoaChiTiets",
                column: "MaHh");
        }
    }
}
