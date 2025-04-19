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
            // Lấy sản phẩm từ slug, bao gồm thông tin chi tiết và danh mục
            var hangHoa = _ctx.HangHoas
                .Include(h => h.HangHoaChiTiet) // Sửa từ HangHoaChiTiets thành HangHoaChiTiet
                .Include(h => h.MaLoaiNavigation)
                .SingleOrDefault(p => p.TenAlias == productSlug);

            if (hangHoa == null)
            {
                return NotFound();
            }

            // Kiểm tra slug của danh mục
            var expectedCategorySlug = SlugHelper.GenerateSlug(hangHoa.MaLoaiNavigation.TenLoaiAlias);

            // Nếu categorySlug không khớp, chuyển hướng đến URL chính xác
            if (categorySlug != expectedCategorySlug)
            {
                return RedirectToActionPermanent("Detail", new { categorySlug = expectedCategorySlug, productSlug });
            }

            // Truyền slug hình ảnh
            ViewData["ImageUrl"] = "~/Hinh/HangHoa/" + hangHoa.Hinh;

            return View(hangHoa);
        }

        // Các action khác (Index, Search) giữ nguyên
        public IActionResult Index(int? cateid, int page = 1, int pageSize = 10)
        {
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
            var result = data
                .OrderBy(hh => hh.MaHh)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(hh => new HangHoaVM
                {
                    MaHh = hh.MaHh,
                    TenHh = hh.TenHh,
                    TenAlias = hh.TenAlias,
                    DonGia = hh.DonGia ?? 0,
                    Hinh = hh.Hinh
                }).ToList();

            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewData["CateId"] = cateid;

            return View(result);
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
                    Hinh = hh.Hinh
                }).ToList();

            ViewData["Title"] = $"Kết quả tìm kiếm cho '{query}'";
            return View("Index", results);
        }
    }
}