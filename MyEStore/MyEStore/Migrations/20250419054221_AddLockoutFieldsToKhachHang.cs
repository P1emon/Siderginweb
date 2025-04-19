using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyEStore.Migrations
{
    /// <inheritdoc />
    public partial class AddLockoutFieldsToKhachHang : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailedLoginAttempts",
                table: "KhachHang",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "KhachHang",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockoutEnd",
                table: "KhachHang",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailedLoginAttempts",
                table: "KhachHang");

            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "KhachHang");

            migrationBuilder.DropColumn(
                name: "LockoutEnd",
                table: "KhachHang");
        }
    }
}
