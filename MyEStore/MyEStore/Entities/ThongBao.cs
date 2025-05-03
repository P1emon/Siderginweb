using System.ComponentModel.DataAnnotations;

namespace MyEStore.Entities
{
    public class ThongBao
    {
        [Key]
        public int MaTb { get; set; }
        public string? TieuDe { get; set; }
        public string? NoiDung { get; set; }
        public DateTime NgayTao { get; set; }
        public string MaKh { get; set; }
        public string MaMv { get; set; }
        public string MaSlider { get; set; }
        public bool DaXem { get; set; }
    }
}
