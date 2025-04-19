using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyEStore.Migrations
{
    /// <inheritdoc />
    public partial class AddActivationCodeExpiryToKhachHang : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ActivationCodeExpiry",
                table: "KhachHang",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActivationCodeExpiry",
                table: "KhachHang");
        }
    }
}
