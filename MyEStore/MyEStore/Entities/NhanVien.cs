﻿using MyEStore.Models;
using System;
using System.Collections.Generic;

namespace MyEStore.Entities;

public partial class NhanVien
{
    public string MaNv { get; set; } = null!;

    public string HoTen { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? MatKhau { get; set; }
    public DateTime NgayTaoTaiKhoan { get; set; }
    public string? RandomKey { get; set; }
    public bool GioiTinh { get; set; }
    public string? DiaChi { get; set; }
    public string? Hinh { get; set; }
    public DateTime? DangNhapLanCuoi { get; set; }
    public string? DienThoai { get; set; }

    public virtual ICollection<ChuDe> ChuDes { get; set; } = new List<ChuDe>();

    public virtual ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();

    public virtual ICollection<HoiDap> HoiDaps { get; set; } = new List<HoiDap>();

    public virtual ICollection<PhanCong> PhanCongs { get; set; } = new List<PhanCong>();
    public virtual ICollection<Slider> Sliders { get; set; } = new List<Slider>();
}
