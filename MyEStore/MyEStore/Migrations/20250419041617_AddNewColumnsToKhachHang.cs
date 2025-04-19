using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyEStore.Migrations
{
    /// <inheritdoc />
    public partial class AddNewColumnsToKhachHang : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DangNhapLanCuoi",
                table: "KhachHang",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Diem",
                table: "KhachHang",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayTaoTaiKhoan",
                table: "KhachHang",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DangNhapLanCuoi",
                table: "KhachHang");

            migrationBuilder.DropColumn(
                name: "Diem",
                table: "KhachHang");

            migrationBuilder.DropColumn(
                name: "NgayTaoTaiKhoan",
                table: "KhachHang");
        }
    }
}
