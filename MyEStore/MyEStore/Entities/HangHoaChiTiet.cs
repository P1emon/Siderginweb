using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyEStore.Entities;

public class HangHoaChiTiet
{
    [Key]
    public int MaChiTiet { get; set; }

    [Required]
    public int MaHh { get; set; }

    [Required]
    [StringLength(500)]
    public string CongDung { get; set; } = null!;

    [Required]
    [StringLength(500)]
    public string DoiTuongSuDung { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string QuyCachDongGoi { get; set; } = null!;

    [Required]
    [StringLength(1000)]
    public string ThanhPhan { get; set; } = null!;

    [Required]
    [StringLength(1000)]
    public string CongNgheDacBiet { get; set; } = null!;

    [Required]
    [StringLength(1000)]
    public string LoiIchNoiBat { get; set; } = null!;

    [Required]
    [StringLength(500)]
    public string LuuY { get; set; } = null!;

    [ForeignKey("MaHh")]
    public virtual HangHoa HangHoa { get; set; } = null!;
}