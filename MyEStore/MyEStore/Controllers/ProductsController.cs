using Microsoft.AspNetCore.Mvc;
using MyEStore.Entities;
using System.Linq;
using MyEStore.Helpers;
using MyEStore.Models;
using Microsoft.EntityFrameworkCore;

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

            ViewData["ImageUrl"] = "~/Hinh/HangHoa/" + hangHoa.Hinh;

            return View(hangHoa);
        }

        public IActionResult Index(int? cateid, int page = 1, int pageSize = 10)
        {
            // Fetch new products (created within the last 7 days)
            var newProducts = _ctx.HangHoas
                .Where(hh => hh.NgayTao >= DateTime.Now.AddDays(-7))
                .Select(hh => new HangHoaVM
                {
                    MaHh = hh.MaHh,
                    TenHh = hh.TenHh,
                    TenAlias = hh.TenAlias,
                    DonGia = hh.DonGia ?? 0,
                    Hinh = hh.Hinh,
                    GiamGia = hh.GiamGia
                })
                .Take(8) // Limit to 8 new products
                .ToList();

            // Fetch sale products (all products with discount)
            var saleProducts = _ctx.HangHoas
                .Where(hh => hh.GiamGia > 0)
                .Select(hh => new HangHoaVM
                {
                    MaHh = hh.MaHh,
                    TenHh = hh.TenHh,
                    TenAlias = hh.TenAlias,
                    DonGia = hh.DonGia ?? 0,
                    Hinh = hh.Hinh,
                    GiamGia = hh.GiamGia
                })
                .Take(8) // Limit to 8 sale products
                .ToList();

            // Fetch all products with pagination
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
                    GiamGia = hh.GiamGia
                }).ToList();

            // Fetch active sliders
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

        public IActionResult Search(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return RedirectToAction("Index");
            }

            var results = _ctx.HangHoas
                .Where(hh => hh.TenHh.Contains(query) || hh.TenAlias.Contains(query))
                .Select(hh => new HangHoaVM
                {
                    MaHh = hh.MaHh,
                    TenHh = hh.TenHh,
                    TenAlias = hh.TenAlias,
                    DonGia = hh.DonGia ?? 0,
                    Hinh = hh.Hinh,
                    GiamGia = hh.GiamGia
                }).ToList();

            ViewData["Title"] = $"Kết quả tìm kiếm cho '{query}'";
            return View("Index", results);
        }
    }
}