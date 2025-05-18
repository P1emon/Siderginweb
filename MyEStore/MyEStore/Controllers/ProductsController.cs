using Microsoft.AspNetCore.Mvc;
using MyEStore.Entities;
using System.Linq;
using MyEStore.Helpers;
using MyEStore.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace MyEStore.Controllers
{
    public class ProductsController : Controller
    {
        private readonly MyeStoreContext _ctx;

        public ProductsController(MyeStoreContext ctx)
        {
            _ctx = ctx;
        }

        [HttpGet("san-pham/{categorySlug}/{productSlug}")]
        public IActionResult Detail(string categorySlug, string productSlug)
        {
            var hangHoa = _ctx.HangHoas
                .Include(h => h.HangHoaChiTiet)
                .Include(h => h.MaLoaiNavigation)
                .SingleOrDefault(p => p.TenAlias == productSlug);

            if (hangHoa == null)
            {
                return NotFound();
            }

            var expectedCategorySlug = SlugHelper.GenerateSlug(hangHoa.MaLoaiNavigation.TenLoaiAlias);

            if (categorySlug != expectedCategorySlug)
            {
                return RedirectToActionPermanent("Detail", new { categorySlug = expectedCategorySlug, productSlug });
            }

            var recommendedProducts = _ctx.HangHoas
                .Where(h => h.MaLoai == hangHoa.MaLoai && h.MaHh != hangHoa.MaHh)
                .Select(h => new HangHoaVM
                {
                    MaHh = h.MaHh,
                    TenHh = h.TenHh,
                    TenAlias = h.TenAlias,
                    DonGia = h.DonGia ?? 0,
                    Hinh = h.Hinh,
                    GiamGia = h.GiamGia,
                    IsInStock = h.SoLuong > 0
                })
                .Take(4)
                .ToList();

            ViewData["ImageUrl"] = "~/Hinh/HangHoa/" + hangHoa.Hinh;
            ViewData["RecommendedProducts"] = recommendedProducts;

            return View(hangHoa);
        }

        public IActionResult Index(int? cateid, int page = 1, int pageSize = 10)
        {
            var newProducts = _ctx.HangHoas
                .Where(hh => hh.NgayTao >= DateTime.Now.AddDays(-7))
                .Select(hh => new HangHoaVM
                {
                    MaHh = hh.MaHh,
                    TenHh = hh.TenHh,
                    TenAlias = hh.TenAlias,
                    DonGia = hh.DonGia ?? 0,
                    Hinh = hh.Hinh,
                    GiamGia = hh.GiamGia,
                    IsInStock = hh.SoLuong > 0
                })
                .Take(8)
                .ToList();

            var saleProducts = _ctx.HangHoas
                .Where(hh => hh.GiamGia > 0)
                .Select(hh => new HangHoaVM
                {
                    MaHh = hh.MaHh,
                    TenHh = hh.TenHh,
                    TenAlias = hh.TenAlias,
                    DonGia = hh.DonGia ?? 0,
                    Hinh = hh.Hinh,
                    GiamGia = hh.GiamGia,
                    IsInStock = hh.SoLuong > 0
                })
                .Take(8)
                .ToList();

            var data = _ctx.HangHoas.AsQueryable();

            if (cateid.HasValue)
            {
                data = data.Where(hh => hh.MaLoai == cateid.Value);
                var tenLoai = _ctx.Loais
                    .Where(l => l.MaLoai == cateid.Value)
                    .Select(l => l.TenLoai)
                    .FirstOrDefault();
                ViewData["Title"] = tenLoai ?? "Danh sách hàng hóa";
            }
            else
            {
                ViewData["Title"] = "Danh sách hàng hóa";
            }

            int totalItems = data.Count();
            var allProducts = data
                .OrderBy(hh => hh.MaHh)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(hh => new HangHoaVM
                {
                    MaHh = hh.MaHh,
                    TenHh = hh.TenHh,
                    TenAlias = hh.TenAlias,
                    DonGia = hh.DonGia ?? 0,
                    Hinh = hh.Hinh,
                    GiamGia = hh.GiamGia,
                    IsInStock = hh.SoLuong > 0
                }).ToList();

            var sliders = _ctx.Sliders
                .Where(s => s.HieuLuc && s.NgayBatDau <= DateTime.Now && s.NgayKetThuc >= DateTime.Now)
                .OrderBy(s => s.MaSlider)
                .ToList();

            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewData["CateId"] = cateid;
            ViewData["Sliders"] = sliders;
            ViewData["NewProducts"] = newProducts;
            ViewData["SaleProducts"] = saleProducts;

            return View(allProducts);
        }

        public IActionResult Search(string query, int page = 1, int pageSize = 10)
        {
            if (string.IsNullOrEmpty(query))
            {
                return RedirectToAction("Index");
            }

            // Chuẩn hóa chuỗi tìm kiếm
            query = query.Trim().ToLower();
            var keywords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Tìm kiếm trong TenHh, TenAlias và MoTa
            var rawResults = _ctx.HangHoas
                .AsQueryable()
                .Where(hh => keywords.Any(k =>
                    (hh.TenHh != null && hh.TenHh.ToLower().Contains(k)) ||
                    (hh.TenAlias != null && hh.TenAlias.ToLower().Contains(k)) ||
                    (hh.MoTa != null && hh.MoTa.ToLower().Contains(k))))
                .Select(hh => new
                {
                    HangHoa = hh,
                    TenHh = hh.TenHh,
                    TenAlias = hh.TenAlias,
                    MoTa = hh.MoTa
                })
                .AsEnumerable() // Chuyển sang xử lý trong bộ nhớ
                .Select(x => new
                {
                    x.HangHoa,
                    Relevance = keywords.Sum(k =>
                        (x.TenHh != null && x.TenHh.ToLower().Contains(k) ? 3 : 0) +
                        (x.TenAlias != null && x.TenAlias.ToLower().Contains(k) ? 2 : 0) +
                        (x.MoTa != null && x.MoTa.ToLower().Contains(k) ? 1 : 0))
                })
                .OrderByDescending(x => x.Relevance)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new HangHoaVM
                {
                    MaHh = x.HangHoa.MaHh,
                    TenHh = x.HangHoa.TenHh,
                    TenAlias = x.HangHoa.TenAlias,
                    DonGia = x.HangHoa.DonGia ?? 0,
                    Hinh = x.HangHoa.Hinh,
                    GiamGia = x.HangHoa.GiamGia,
                    IsInStock = x.HangHoa.SoLuong > 0
                })
                .ToList();

            // Tính tổng số kết quả để phân trang
            int totalItems = _ctx.HangHoas
                .Count(hh => keywords.Any(k =>
                    (hh.TenHh != null && hh.TenHh.ToLower().Contains(k)) ||
                    (hh.TenAlias != null && hh.TenAlias.ToLower().Contains(k)) ||
                    (hh.MoTa != null && hh.MoTa.ToLower().Contains(k))));

            // Cập nhật ViewData
            ViewData["Title"] = $"Kết quả tìm kiếm cho '{query}'";
            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewData["Query"] = query;

            return View("Index", rawResults);
        }
    }
}