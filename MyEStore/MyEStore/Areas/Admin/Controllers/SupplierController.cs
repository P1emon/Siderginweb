using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyEStore.Entities;
using System;
using System.Threading.Tasks;

namespace MyEStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SupplierController : Controller
    {
        private readonly MyeStoreContext _context;

        public SupplierController(MyeStoreContext context)
        {
            _context = context;
        }

        // GET: Supplier
        public async Task<IActionResult> Index()
        {
            var nhaCungCaps = await _context.NhaCungCaps.ToListAsync();
            return View(nhaCungCaps);
        }

        // GET: Supplier/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var nhaCungCap = await _context.NhaCungCaps
                .FirstOrDefaultAsync(m => m.MaNcc == id);
            if (nhaCungCap == null)
            {
                return NotFound();
            }

            return View(nhaCungCap);
        }

        // GET: Supplier/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Supplier/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NhaCungCap model)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(model.MaNcc))
                {
                    ModelState.AddModelError("MaNcc", "Mã nhà cung cấp là bắt buộc.");
                }

                if (string.IsNullOrWhiteSpace(model.TenCongTy))
                {
                    ModelState.AddModelError("TenCongTy", "Tên công ty là bắt buộc.");
                }

                if (string.IsNullOrWhiteSpace(model.Email))
                {
                    ModelState.AddModelError("Email", "Email là bắt buộc.");
                }

                // Validate unique MaNcc
                if (await _context.NhaCungCaps.AnyAsync(n => n.MaNcc == model.MaNcc))
                {
                    ModelState.AddModelError("MaNcc", "Mã nhà cung cấp đã tồn tại.");
                }

                // Validate email format
                if (!IsValidEmail(model.Email))
                {
                    ModelState.AddModelError("Email", "Email không đúng định dạng.");
                }

                // Set Logo to MaNcc
                if (ModelState.IsValid || ModelState.ErrorCount == 0 || (ModelState.ContainsKey("MaNcc") && ModelState["MaNcc"].Errors.Count == 0))
                {
                    model.Logo = model.MaNcc; // Set Logo to MaNcc
                }
                else
                {
                    model.Logo = "0"; // Fallback if MaNcc is invalid
                }

                if (ModelState.IsValid)
                {
                    model.MaNcc = model.MaNcc.Trim().ToUpper();
                    model.TenCongTy = model.TenCongTy.Trim();
                    model.Email = model.Email.Trim().ToLower();

                    _context.NhaCungCaps.Add(model);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Thêm nhà cung cấp thành công!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Đã xảy ra lỗi: {ex.Message}");
            }

            return View(model);
        }

        // GET: Supplier/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var nhaCungCap = await _context.NhaCungCaps.FindAsync(id);
            if (nhaCungCap == null)
            {
                return NotFound();
            }
            return View(nhaCungCap);
        }

        // POST: Supplier/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("MaNcc,TenCongTy,Logo,NguoiLienLac,Email,DienThoai,DiaChi,MoTa")] NhaCungCap nhaCungCap)
        {
            if (id != nhaCungCap.MaNcc)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(nhaCungCap);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NhaCungCapExists(nhaCungCap.MaNcc))
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
            return View(nhaCungCap);
        }

        // GET: Supplier/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var nhaCungCap = await _context.NhaCungCaps
                .FirstOrDefaultAsync(m => m.MaNcc == id);
            if (nhaCungCap == null)
            {
                return NotFound();
            }

            return View(nhaCungCap);
        }

        // POST: Supplier/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var nhaCungCap = await _context.NhaCungCaps.FindAsync(id);
            if (nhaCungCap != null)
            {
                _context.NhaCungCaps.Remove(nhaCungCap);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private bool NhaCungCapExists(string id)
        {
            return _context.NhaCungCaps.Any(e => e.MaNcc == id);
        }
    }
}