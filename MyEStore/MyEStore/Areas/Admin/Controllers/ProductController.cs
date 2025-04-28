using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyEStore.Entities;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaHh,TenHh,TenAlias,MaLoai,MoTaDonVi,DonGia,Hinh,NgaySx,GiamGia,SoLanXem,MoTa,MaNcc,NgayTao,SoLuong")] HangHoa hangHoa, IFormFile hinhFile)
        {
            // Xóa validation cho một số trường nếu cần
            ModelState.Remove("TenAlias"); // Nếu TenAlias có thể tự động tạo
            ModelState.Remove("NgayTao"); // Nếu NgayTao có thể tự động tạo
            ModelState.Remove("SoLanXem"); // Nếu SoLanXem mặc định là 0

            if (ModelState.IsValid)
            {
                // Xử lý các trường thiếu
                if (string.IsNullOrEmpty(hangHoa.TenAlias))
                {
                    hangHoa.TenAlias = hangHoa.TenHh.ToLower().Replace(" ", "-");
                }

                hangHoa.NgayTao = DateTime.Now;
                hangHoa.SoLanXem = 0;

                // Xử lý upload hình ảnh nếu có
                if (hinhFile != null && hinhFile.Length > 0)
                {
                    // Định nghĩa đường dẫn lưu file
                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + hinhFile.FileName;
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");

                    // Tạo thư mục nếu không tồn tại
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    // Lưu file
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await hinhFile.CopyToAsync(stream);
                    }

                    // Lưu tên file vào cơ sở dữ liệu
                    hangHoa.Hinh = uniqueFileName;
                }

                _context.Add(hangHoa);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Tạo sản phẩm thành công!";
                return RedirectToAction(nameof(Index));
            }

            // Nếu ModelState không hợp lệ, hiển thị lỗi
            foreach (var key in ModelState.Keys)
            {
                if (ModelState[key].Errors.Count > 0)
                {
                    Console.WriteLine($"Lỗi ở {key}: {string.Join(", ", ModelState[key].Errors.Select(e => e.ErrorMessage))}");
                }
            }

            ViewData["MaLoai"] = new SelectList(_context.Loais, "MaLoai", "TenLoai", hangHoa.MaLoai);
            ViewData["MaNcc"] = new SelectList(_context.NhaCungCaps, "MaNcc", "TenCongTy", hangHoa.MaNcc);
            return View(hangHoa);
        }

        // GET: Admin/Product/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hangHoa = await _context.HangHoas.FindAsync(id);
            if (hangHoa == null)
            {
                return NotFound();
            }
            ViewData["MaLoai"] = new SelectList(_context.Loais, "MaLoai", "TenLoai", hangHoa.MaLoai);
            ViewData["MaNcc"] = new SelectList(_context.NhaCungCaps, "MaNcc", "TenCongTy", hangHoa.MaNcc);
            return View(hangHoa);
        }

        // POST: Admin/Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaHh,TenHh,TenAlias,MaLoai,MoTaDonVi,DonGia,Hinh,NgaySx,GiamGia,SoLanXem,MoTa,MaNcc,NgayTao,SoLuong")] HangHoa hangHoa, IFormFile hinhFile)
        {
            if (id != hangHoa.MaHh)
            {
                return NotFound();
            }

            ModelState.Remove("TenAlias");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingProduct = await _context.HangHoas.AsNoTracking().FirstOrDefaultAsync(h => h.MaHh == id);

                    // Cập nhật TenAlias nếu cần
                    if (string.IsNullOrEmpty(hangHoa.TenAlias))
                    {
                        hangHoa.TenAlias = hangHoa.TenHh.ToLower().Replace(" ", "-");
                    }

                    // Giữ ngày tạo ban đầu
                    hangHoa.NgayTao = existingProduct.NgayTao;

                    // Giữ số lần xem ban đầu
                    hangHoa.SoLanXem = existingProduct.SoLanXem;

                    // Xử lý upload hình ảnh mới nếu có
                    if (hinhFile != null && hinhFile.Length > 0)
                    {
                        // Xóa hình cũ nếu có
                        if (!string.IsNullOrEmpty(existingProduct.Hinh))
                        {
                            var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products", existingProduct.Hinh);
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        // Lưu hình mới
                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + hinhFile.FileName;
                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");

                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await hinhFile.CopyToAsync(stream);
                        }

                        hangHoa.Hinh = uniqueFileName;
                    }
                    else
                    {
                        // Giữ hình cũ nếu không upload hình mới
                        hangHoa.Hinh = existingProduct.Hinh;
                    }

                    _context.Update(hangHoa);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật sản phẩm thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!HangHoaExists(hangHoa.MaHh))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["MaLoai"] = new SelectList(_context.Loais, "MaLoai", "TenLoai", hangHoa.MaLoai);
            ViewData["MaNcc"] = new SelectList(_context.NhaCungCaps, "MaNcc", "TenCongTy", hangHoa.MaNcc);
            return View(hangHoa);
        }

        // GET: Admin/Product/Delete/5
        public async Task<IActionResult> Delete(int? id)
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

        // POST: Admin/Product/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var hangHoa = await _context.HangHoas.FindAsync(id);
            _context.HangHoas.Remove(hangHoa);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Xóa sản phẩm thành công!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Product/Details/5
        public async Task<IActionResult> DetailsHangHoaChiTiet(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hangHoaChiTiet = await _context.HangHoaChiTiets
                .Include(h => h.HangHoa)
                .FirstOrDefaultAsync(m => m.MaHh == id);

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
            // Đảm bảo rằng HangHoa navigation property không gây lỗi
            ModelState.Remove("HangHoa");

            // Kiểm tra MaHh
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
                try
                {
                    var existingChiTiet = await _context.HangHoaChiTiets.FirstOrDefaultAsync(h => h.MaHh == hangHoaChiTiet.MaHh);
                    if (existingChiTiet != null)
                    {
                        // Cập nhật chi tiết đã tồn tại
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
                        // Tạo chi tiết mới
                        _context.Add(hangHoaChiTiet);
                    }

                    await _context.SaveChangesAsync();
                    TempData["Success"] = existingChiTiet != null ? "Cập nhật chi tiết sản phẩm thành công!" : "Tạo chi tiết sản phẩm thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    // Log lỗi
                    ModelState.AddModelError("", "Không thể lưu thay đổi. Vui lòng thử lại, và nếu vấn đề vẫn tiếp diễn, hãy liên hệ với quản trị viên.");
                }
            }

            var hangHoa = await _context.HangHoas.FirstOrDefaultAsync(h => h.MaHh == hangHoaChiTiet.MaHh);
            ViewBag.TenHh = hangHoa?.TenHh ?? "Không tìm thấy sản phẩm";
            ViewBag.MaHh = hangHoaChiTiet.MaHh;
            ViewBag.HangHoaList = new SelectList(_context.HangHoas, "MaHh", "TenHh", hangHoaChiTiet.MaHh);
            return View(hangHoaChiTiet);
        }

        // GET: Admin/Product/EditDetails/5
        public async Task<IActionResult> EditDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hangHoaChiTiet = await _context.HangHoaChiTiets
                .Include(h => h.HangHoa)
                .FirstOrDefaultAsync(m => m.MaHh == id);

            if (hangHoaChiTiet == null)
            {
                // Nếu không tìm thấy chi tiết, chuyển hướng đến tạo mới
                return RedirectToAction(nameof(CreateDetails), new { maHh = id });
            }

            ViewBag.TenHh = hangHoaChiTiet.HangHoa?.TenHh;
            ViewBag.MaHh = hangHoaChiTiet.MaHh;
            return View(hangHoaChiTiet);
        }

        // POST: Admin/Product/EditDetails/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDetails([Bind("MaChiTiet,MaHh,CongDung,DoiTuongSuDung,QuyCachDongGoi,ThanhPhan,CongNgheDacBiet,LoiIchNoiBat,LuuY")] HangHoaChiTiet hangHoaChiTiet)
        {
            // Đảm bảo rằng HangHoa navigation property không gây lỗi
            ModelState.Remove("HangHoa");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(hangHoaChiTiet);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật chi tiết sản phẩm thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!HangHoaChiTietExists(hangHoaChiTiet.MaChiTiet))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            var hangHoa = await _context.HangHoas.FirstOrDefaultAsync(h => h.MaHh == hangHoaChiTiet.MaHh);
            ViewBag.TenHh = hangHoa?.TenHh;
            ViewBag.MaHh = hangHoaChiTiet.MaHh;
            return View(hangHoaChiTiet);
        }

       

       
        private bool HangHoaExists(int id)
        {
            return _context.HangHoas.Any(e => e.MaHh == id);
        }

        private bool HangHoaChiTietExists(int id)
        {
            return _context.HangHoaChiTiets.Any(e => e.MaChiTiet == id);
        }
    }
}