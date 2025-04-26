using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyEStore.Entities;

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

        public async Task<IActionResult> Index()
        {
            var myeStoreContext = _context.HangHoas.Include(h => h.MaLoaiNavigation).Include(h => h.MaNccNavigation);
            return View(await myeStoreContext.ToListAsync());
        }

        // GET: Admin/Product/Create
        public IActionResult Create()
        {
            ViewBag.MaLoai = new SelectList(_context.Loais, "MaLoai", "TenLoai");
            ViewBag.MaNcc = new SelectList(_context.NhaCungCaps, "MaNcc", "TenNcc");
            return View();
        }

        // POST: Admin/Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TenHh,TenAlias,MaLoai,MoTaDonVi,DonGia,Hinh,NgaySx,GiamGia,SoLanXem,MoTa,MaNcc")] HangHoa hangHoa)
        {
            if (ModelState.IsValid)
            {
                _context.Add(hangHoa);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.MaLoai = new SelectList(_context.Loais, "MaLoai", "TenLoai", hangHoa.MaLoai);
            ViewBag.MaNcc = new SelectList(_context.NhaCungCaps, "MaNcc", "TenNcc", hangHoa.MaNcc);
            return View(hangHoa);
        }

        // GET: Admin/Product/CreateDetail
        public IActionResult CreateDetail()
        {
            ViewBag.MaHh = new SelectList(_context.HangHoas, "MaHh", "TenHh");
            return View();
        }

        // POST: Admin/Product/CreateDetail
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDetail([Bind("MaHh,CongDung,DoiTuongSuDung,QuyCachDongGoi,ThanhPhan,CongNgheDacBiet,LoiIchNoiBat,LuuY")] HangHoaChiTiet hangHoaChiTiet)
        {
            if (ModelState.IsValid)
            {
                _context.Add(hangHoaChiTiet);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index)); // Hoặc một action khác phù hợp
            }
            ViewBag.MaHh = new SelectList(_context.HangHoas, "MaHh", "TenHh", hangHoaChiTiet.MaHh);
            return View(hangHoaChiTiet);
        }
    }
}