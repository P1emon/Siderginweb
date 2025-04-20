using MyEStore.Entities;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyEStore.Models
{
    public class Slider
    {
        [Key]
        public int MaSlider { get; set; }

        [Required]
        [StringLength(100)]
        public string TieuDe { get; set; }

        [StringLength(500)]
        public string MoTa { get; set; }

        [Required]
        [StringLength(250)]
        public string HinhAnh { get; set; }

        [Required]
        [StringLength(250)]
        public string LinkQuangCao { get; set; }

        [ForeignKey("HangHoa")]
        public int? MaHH { get; set; }
        public HangHoa HangHoa { get; set; }

        [ForeignKey("NhanVien")]
        public string MaNV { get; set; }
        public NhanVien NhanVien { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.Now;
        public DateTime NgayBatDau { get; set; }
        public DateTime NgayKetThuc { get; set; }

        public bool HieuLuc { get; set; } = true;
    }
}