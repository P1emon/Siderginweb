using MyEStore.Entities;

namespace MyEStore.Models
{
    public class ThongBaonewModel
    {
        public List<ThongBao> ThongBaos { get; set; }
        public int MaSlider { get; set; }
        public string TieuDe { get; set; }
        public string MoTa { get; set; }
        public string HinhAnh { get; set; }
        public string LinkQuangCao { get; set; }
        public DateTime NgayTao { get; set; }
        public DateTime NgayBatDau { get; set; }
        public DateTime NgayKetThuc { get; set; }
        public bool DaXem { get; set; }
        public string MaKH { get; set; }
    }
}
