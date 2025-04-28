using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyEStore.Entities;
using System.Linq;
using System.Threading.Tasks;

namespace MyEStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController : Controller
    {
        private readonly MyeStoreContext _context;

        public ProductController(MyeStoreContext context)
        {
            _context = context;
        }

        // GET: Admin/Product
        public async Task<IActionResult> Index()
        {
            var myeStoreContext = _context.HangHoas
                .Include(h => h.MaLoaiNavigation)
                .Include(h => h.MaNccNavigation);
            return View(await myeStoreContext.ToListAsync());
        }

        // GET: Admin/Product/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hangHoa = await _context.HangHoas
                .Include(h => h.MaLoaiNavigation)
                .Include(h => h.MaNccNavigation)
                .FirstOrDefaultAsync(m => m.MaHh == id);

            if (hangHoa == null)
            {
                return NotFound();
            }

            return View(hangHoa);
        }

        // GET: Admin/Product/Create
        public IActionResult Create()
        {
            ViewData["MaLoai"] = new SelectList(_context.Loais, "MaLoai", "TenLoai");
            ViewData["MaNcc"] = new SelectList(_context.NhaCungCaps, "MaNcc", "TenCongTy");
            return View();
        }

        // POST: Admin/Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaHh,TenHh,TenAlias,MaLoai,MoTaDonVi,DonGia,Hinh,NgaySx,GiamGia,SoLanXem,MoTa,MaNcc,NgayTao,SoLuong")] HangHoa hangHoa)
        {
            if (ModelState.IsValid)
            {
                _context.Add(hangHoa);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaLoai"] = new SelectList(_context.Loais, "MaLoai", "TenLoai", hangHoa.MaLoai);
            ViewData["MaNcc"] = new SelectList(_context.NhaCungCaps, "MaNcc", "TenCongTy", hangHoa.MaNcc);
            return View(hangHoa);
        }

        // GET: Admin/Product/Details
        public async Task<IActionResult> DetailsHangHoaChiTiet(int? maHh)
        {
            if (maHh == null)
            {
                return NotFound();
            }

            var hangHoaChiTiet = await _context.HangHoaChiTiets
                .Include(h => h.HangHoa)
                .FirstOrDefaultAsync(m => m.MaHh == maHh);

            if (hangHoaChiTiet == null)
            {
                return NotFound();
            }

            return View(hangHoaChiTiet);
        }
        // GET: Admin/Product/CreateDetails
        public async Task<IActionResult> CreateDetails(int? maHh)
        {
            if (maHh == null)
            {
                ViewBag.HangHoaList = new SelectList(_context.HangHoas, "MaHh", "TenHh");
                return View(new HangHoaChiTiet());
            }

            var hangHoa = await _context.HangHoas.FirstOrDefaultAsync(h => h.MaHh == maHh);
            if (hangHoa == null)
            {
                return NotFound();
            }

            var hangHoaChiTiet = await _context.HangHoaChiTiets.FirstOrDefaultAsync(h => h.MaHh == maHh)
                ?? new HangHoaChiTiet { MaHh = maHh.Value };
            ViewBag.TenHh = hangHoa.TenHh;
            ViewBag.MaHh = hangHoa.MaHh;
            ViewBag.HangHoaList = new SelectList(_context.HangHoas, "MaHh", "TenHh", maHh);
            return View(hangHoaChiTiet);
        }

        // POST: Admin/Product/CreateDetails
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDetails([Bind("MaChiTiet,MaHh,CongDung,DoiTuongSuDung,QuyCachDongGoi,ThanhPhan,CongNgheDacBiet,LoiIchNoiBat,LuuY")] HangHoaChiTiet hangHoaChiTiet)
        {
            // Remove validation errors for HangHoa navigation property
            ModelState.Remove("HangHoa");

            // Validate MaHh
            if (hangHoaChiTiet.MaHh <= 0)
            {
                ModelState.AddModelError("MaHh", "Vui lòng chọn một sản phẩm hợp lệ.");
            }
            else
            {
                var hangHoaExists = await _context.HangHoas.AnyAsync(h => h.MaHh == hangHoaChiTiet.MaHh);
                if (!hangHoaExists)
                {
                    ModelState.AddModelError("MaHh", "Sản phẩm không tồn tại.");
                }
            }

            if (ModelState.IsValid)
            {
                var existingChiTiet = await _context.HangHoaChiTiets.FirstOrDefaultAsync(h => h.MaHh == hangHoaChiTiet.MaHh);
                if (existingChiTiet != null)
                {
                    // Update existing record
                    existingChiTiet.CongDung = hangHoaChiTiet.CongDung;
                    existingChiTiet.DoiTuongSuDung = hangHoaChiTiet.DoiTuongSuDung;
                    existingChiTiet.QuyCachDongGoi = hangHoaChiTiet.QuyCachDongGoi;
                    existingChiTiet.ThanhPhan = hangHoaChiTiet.ThanhPhan;
                    existingChiTiet.CongNgheDacBiet = hangHoaChiTiet.CongNgheDacBiet;
                    existingChiTiet.LoiIchNoiBat = hangHoaChiTiet.LoiIchNoiBat;
                    existingChiTiet.LuuY = hangHoaChiTiet.LuuY;
                    _context.Update(existingChiTiet);
                }
                else
                {
                    // Create new record
                    _context.Add(hangHoaChiTiet);
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = existingChiTiet != null ? "Cập nhật chi tiết sản phẩm thành công!" : "Tạo chi tiết sản phẩm thành công!";
                return RedirectToAction(nameof(Index));
            }

            var hangHoa = await _context.HangHoas.FirstOrDefaultAsync(h => h.MaHh == hangHoaChiTiet.MaHh);
            ViewBag.TenHh = hangHoa?.TenHh ?? "Không tìm thấy sản phẩm";
            ViewBag.HangHoaList = new SelectList(_context.HangHoas, "MaHh", "TenHh", hangHoaChiTiet.MaHh);
            return View(hangHoaChiTiet);
        }

        private bool HangHoaExists(int id)
        {
            return _context.HangHoas.Any(e => e.MaHh == id);
        }
    }
}