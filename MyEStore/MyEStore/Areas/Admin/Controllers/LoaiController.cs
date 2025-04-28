using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyEStore.Entities;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace MyEStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class LoaiController : Controller
    {
        private readonly MyeStoreContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public LoaiController(MyeStoreContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // GET: Admin/Loai
        public async Task<IActionResult> Index()
        {
            var loaiList = await _context.Loais.ToListAsync();
            return View(loaiList);
        }

        // GET: Admin/Loai/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loai = await _context.Loais
                .FirstOrDefaultAsync(m => m.MaLoai == id);
            if (loai == null)
            {
                return NotFound();
            }

            return View(loai);
        }

        // GET: Admin/Loai/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Loai/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TenLoai,TenLoaiAlias,MoTa")] Loai loai, IFormFile fileUpload)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Xử lý tải lên hình ảnh
                    if (fileUpload != null && fileUpload.Length > 0)
                    {
                        // Tạo tên file duy nhất để tránh trùng lặp
                        string fileName = Path.GetFileNameWithoutExtension(fileUpload.FileName);
                        string extension = Path.GetExtension(fileUpload.FileName);
                        fileName = fileName + DateTime.Now.ToString("yymmssfff") + extension;

                        // Lưu đường dẫn hình ảnh
                        loai.Hinh = fileName;

                        // Lưu file vào thư mục
                        string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "categories");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        string filePath = Path.Combine(uploadsFolder, fileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await fileUpload.CopyToAsync(fileStream);
                        }
                    }

                    _context.Add(loai);
                    await _context.SaveChangesAsync();
                    TempData["success"] = "Thêm loại hàng thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["error"] = "Lỗi: " + ex.Message;
                }
            }

            return View(loai);
        }

        // GET: Admin/Loai/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loai = await _context.Loais.FindAsync(id);
            if (loai == null)
            {
                return NotFound();
            }

            ViewBag.CurrentImage = loai.Hinh;
            return View(loai);
        }

        // POST: Admin/Loai/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaLoai,TenLoai,TenLoaiAlias,MoTa,Hinh")] Loai loai, IFormFile fileUpload, string currentImage)
        {
            if (id != loai.MaLoai)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Xử lý upload hình ảnh mới
                    if (fileUpload != null && fileUpload.Length > 0)
                    {
                        // Xóa hình cũ nếu có
                        if (!string.IsNullOrEmpty(currentImage))
                        {
                            string oldImagePath = Path.Combine(_hostEnvironment.WebRootPath, "images", "categories", currentImage);
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        // Tạo tên file duy nhất
                        string fileName = Path.GetFileNameWithoutExtension(fileUpload.FileName);
                        string extension = Path.GetExtension(fileUpload.FileName);
                        fileName = fileName + DateTime.Now.ToString("yymmssfff") + extension;

                        // Lưu đường dẫn hình ảnh mới
                        loai.Hinh = fileName;

                        // Lưu file vào thư mục
                        string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "categories");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        string filePath = Path.Combine(uploadsFolder, fileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await fileUpload.CopyToAsync(fileStream);
                        }
                    }
                    else
                    {
                        // Nếu không upload hình mới, giữ lại hình cũ
                        loai.Hinh = currentImage;
                    }

                    _context.Update(loai);
                    await _context.SaveChangesAsync();
                    TempData["success"] = "Cập nhật loại hàng thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LoaiExists(loai.MaLoai))
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

            ViewBag.CurrentImage = currentImage;
            return View(loai);
        }

        // GET: Admin/Loai/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loai = await _context.Loais
                .FirstOrDefaultAsync(m => m.MaLoai == id);
            if (loai == null)
            {
                return NotFound();
            }

            return View(loai);
        }

        // POST: Admin/Loai/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var loai = await _context.Loais.FindAsync(id);
            if (loai != null)
            {
                // Kiểm tra xem loại hàng có sản phẩm không
                var hasProducts = await _context.HangHoas.AnyAsync(h => h.MaLoai == id);
                if (hasProducts)
                {
                    TempData["error"] = "Không thể xóa loại hàng này vì đã có sản phẩm thuộc loại này!";
                    return RedirectToAction(nameof(Index));
                }

                // Xóa hình ảnh nếu có
                if (!string.IsNullOrEmpty(loai.Hinh))
                {
                    string imagePath = Path.Combine(_hostEnvironment.WebRootPath, "images", "categories", loai.Hinh);
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                _context.Loais.Remove(loai);
                await _context.SaveChangesAsync();
                TempData["success"] = "Xóa loại hàng thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool LoaiExists(int id)
        {
            return _context.Loais.Any(e => e.MaLoai == id);
        }
    }
}