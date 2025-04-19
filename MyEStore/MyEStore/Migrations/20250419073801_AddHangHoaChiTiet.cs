using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyEStore.Migrations
{
    /// <inheritdoc />
    public partial class AddHangHoaChiTiet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HangHoaChiTiets",
                columns: table => new
                {
                    MaChiTiet = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaHh = table.Column<int>(type: "int", nullable: false),
                    CongDung = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DoiTuongSuDung = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    QuyCachDongGoi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ThanhPhan = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CongNgheDacBiet = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    LoiIchNoiBat = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    LuuY = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HangHoaChiTiets", x => x.MaChiTiet);
                    table.ForeignKey(
                        name: "FK_HangHoaChiTiets_HangHoa_MaHh",
                        column: x => x.MaHh,
                        principalTable: "HangHoa",
                        principalColumn: "MaHH",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HangHoaChiTiets_MaHh",
                table: "HangHoaChiTiets",
                column: "MaHh");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HangHoaChiTiets");
        }
    }
}
