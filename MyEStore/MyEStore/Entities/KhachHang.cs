using System;
using System.Collections.Generic;

namespace MyEStore.Entities;

public partial class KhachHang
{
    public string MaKh { get; set; } = null!;

    public string? MatKhau { get; set; }

    public string HoTen { get; set; } = null!;

    public bool GioiTinh { get; set; }

    public DateTime NgaySinh { get; set; }

    public string? DiaChi { get; set; }

    public string? DiaChiPhu { get; set; }

    public string? DienThoai { get; set; }

    public string Email { get; set; } = null!;

    public string? Hinh { get; set; }

    public bool HieuLuc { get; set; }

    public int VaiTro { get; set; }

    public string? RandomKey { get; set; }

    public string? ActivationCode { get; set; }

    public DateTime? ActivationCodeExpiry { get; set; }

    public DateTime NgayTaoTaiKhoan { get; set; }

    public DateTime? DangNhapLanCuoi { get; set; }

    public int Diem { get; set; }

    public int FailedLoginAttempts { get; set; } = 0;

    public bool IsLocked { get; set; } = false;

    public DateTime? LockoutEnd { get; set; }
    public DateTime? MuaHangLanCuoi { get; set; }

    public virtual ICollection<BanBe> BanBes { get; set; } = new List<BanBe>();

    public virtual ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();

    public virtual ICollection<YeuThich> YeuThiches { get; set; } = new List<YeuThich>();
}