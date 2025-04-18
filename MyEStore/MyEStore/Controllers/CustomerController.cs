using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyEStore.Entities;
using MyEStore.Models;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Net;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;

namespace MyEStore.Controllers
{
    public class CustomerController : Controller
    {
        private readonly MyeStoreContext _ctx;
        public CustomerController(MyeStoreContext ctx)
        {
            _ctx = ctx;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginVM model, string? ReturnUrl)
        {
            ViewBag.ReturnUrl = ReturnUrl;

            var kh = _ctx.KhachHangs.SingleOrDefault(p => p.MaKh == model.UserName);
            if (kh == null || !BCrypt.Net.BCrypt.Verify(model.Password, kh.MatKhau))
            {
                ViewBag.ThongBao = "Sai thông tin đăng nhập";
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, kh.HoTen),
                new Claim(ClaimTypes.Email, kh.Email),
                new Claim("UserId", kh.MaKh),
                new Claim(ClaimTypes.Role, "Administrator")
            };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var claimPrincipal = new ClaimsPrincipal(claimsIdentity);

            await HttpContext.SignInAsync(claimPrincipal);

            if (!string.IsNullOrEmpty(ReturnUrl))
            {
                return Redirect(ReturnUrl);
            }

            return RedirectToAction("Index", "Cart");
        }

        [Authorize]
        public IActionResult PurchaseOrder()
        {
            return View();
        }

        [Authorize(Roles = "Accountant")]
        public IActionResult Statistics()
        {
            return View();
        }

        [HttpGet("/Forbidden")]
        public IActionResult Forbidden()
        {
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return Redirect("/");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (_ctx.KhachHangs.Any(kh => kh.MaKh == model.UserName || kh.Email == model.Email))
            {
                ViewBag.ThongBao = "Username or Email already exists.";
                return View(model);
            }

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);

            var newCustomer = new KhachHang
            {
                MaKh = model.UserName,
                MatKhau = hashedPassword,
                HoTen = model.FullName,
                Email = model.Email,
                DienThoai = model.PhoneNumber,
                DiaChi = model.Address,
            };

            _ctx.KhachHangs.Add(newCustomer);
            _ctx.SaveChanges();

            TempData["ThongBao"] = "Account successfully created! Please log in.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Profile()
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login");
            }

            var customer = _ctx.KhachHangs.SingleOrDefault(kh => kh.MaKh == userId);

            if (customer == null)
            {
                return NotFound("Không tìm thấy thông tin người dùng.");
            }

            var model = new ProfileVM
            {
                FullName = customer.HoTen,
                Email = customer.Email,
                PhoneNumber = customer.DienThoai,
                DiaChi = customer.DiaChi // Thêm DiaChi vào ProfileVM
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Profile(ProfileUpdateVM model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
                return View(model);
            }

            var userId = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login");
            }

            var customer = _ctx.KhachHangs.SingleOrDefault(kh => kh.MaKh == userId);

            if (customer == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin người dùng.";
                return View(model);
            }

            if (_ctx.KhachHangs.Any(kh => kh.Email == model.Email && kh.MaKh != userId))
            {
                TempData["Error"] = "Email đã được sử dụng bởi người dùng khác.";
                return View(model);
            }

            customer.HoTen = model.FullName;
            customer.Email = model.Email;
            customer.DienThoai = model.PhoneNumber;

            _ctx.SaveChanges();

            TempData["Success"] = "Thông tin của bạn đã được cập nhật thành công.";
            return RedirectToAction("Profile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(ChangePasswordVM model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
                return RedirectToAction("Profile");
            }

            var userId = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;

            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            var customer = _ctx.KhachHangs.SingleOrDefault(kh => kh.MaKh == userId);

            if (customer == null)
            {
                return NotFound("Customer not found.");
            }

            if (!BCrypt.Net.BCrypt.Verify(model.Password, customer.MatKhau))
            {
                TempData["Error"] = "Mật khẩu hiện tại không chính xác.";
                return RedirectToAction("Profile");
            }

            if (model.NewPassword != model.ConfirmPassword)
            {
                TempData["Error"] = "Mật khẩu mới và mật khẩu xác nhận không khớp.";
                return RedirectToAction("Profile");
            }

            customer.MatKhau = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            _ctx.SaveChanges();

            TempData["Success"] = "Mật khẩu của bạn đã được thay đổi thành công.";
            return RedirectToAction("Profile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateAddress(string province, string district, string ward, string streetAddress, string diaChi)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login");
            }

            var customer = _ctx.KhachHangs.SingleOrDefault(kh => kh.MaKh == userId);

            if (customer == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin người dùng.";
                return RedirectToAction("Profile");
            }

            // Validate input
            if (string.IsNullOrWhiteSpace(province) || string.IsNullOrWhiteSpace(district) ||
                string.IsNullOrWhiteSpace(ward) || string.IsNullOrWhiteSpace(streetAddress) ||
                string.IsNullOrWhiteSpace(diaChi))
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ thông tin địa chỉ.";
                return RedirectToAction("Profile");
            }

            try
            {
                customer.DiaChi = diaChi; // Lưu địa chỉ đầy đủ
                _ctx.SaveChanges();
                TempData["Success"] = "Cập nhật địa chỉ nhận hàng thành công.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Đã xảy ra lỗi khi cập nhật địa chỉ: {ex.Message}";
            }

            return RedirectToAction("Profile");
        }

        public IActionResult TransactionHistory()
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;

            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            var orders = _ctx.HoaDons
                .Where(hd => hd.MaKh == userId)
                .Include(hd => hd.ChiTietHds)
                .OrderByDescending(hd => hd.NgayDat)
                .ToList();

            return View(orders);
        }

        [Authorize]
        public IActionResult OrderDetails(int id)
        {
            var order = _ctx.HoaDons
                .Where(hd => hd.MaHd == id)
                .Select(hd => new
                {
                    hd.MaHd,
                    hd.NgayDat,
                    hd.HoTen,
                    hd.DiaChi,
                    hd.CachThanhToan,
                    hd.PhiVanChuyen,
                    Products = hd.ChiTietHds.Select(ct => new
                    {
                        ct.MaHh,
                        ct.SoLuong,
                        ct.DonGia,
                        ProductName = ct.MaHhNavigation.TenHh,
                        Hinh = ct.MaHhNavigation.Hinh
                    }).ToList()
                }).FirstOrDefault();

            if (order == null)
            {
                return NotFound("Order not found.");
            }

            return View(order);
        }

        #region ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var khachHang = await _ctx.KhachHangs.SingleOrDefaultAsync(kh => kh.Email == email);
            if (khachHang == null)
            {
                ViewBag.ErrorMessage = "Email không tồn tại.";
                return View();
            }

            var newPassword = GenerateRandomPassword();
            khachHang.MatKhau = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _ctx.SaveChangesAsync();

            string message = $@"
        <p>Xin chào: {khachHang.HoTen},</p>
        <p>Mật khẩu mới của bạn là: <strong>{newPassword}</strong></p>
        <p>Vui lòng đăng nhập lại và đổi mật khẩu mới để đảm bảo an toàn.</p>";

            await SendEmail(khachHang.Email, "Mật khẩu mới của bạn", message);

            ViewBag.SuccessMessage = "Mật khẩu mới đã được gửi đến email của bạn.";
            return View();
        }

        private string GenerateRandomPassword(int length = 8)
        {
            const string validChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            return new string(Enumerable.Repeat(validChars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private async Task SendEmail(string toEmail, string subject, string message)
        {
            var client = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("truongminhduc4002@gmail.com", "hocekpuhklqvkniu"),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress("truongminhduc4002@gmail.com"),
                Subject = subject,
                Body = message,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            try
            {
                await client.SendMailAsync(mailMessage);
            }
            catch (SmtpException ex)
            {
                Console.WriteLine($"SMTP Exception: {ex.Message}");
                throw;
            }
        }
        #endregion

        public IActionResult Thongbao()
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;

            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            var today = DateTime.Today;

            var orders = _ctx.HoaDons
                .Where(hd => hd.MaKh == userId && hd.NgayDat.Date >= today)
                .Include(hd => hd.ChiTietHds)
                .OrderByDescending(hd => hd.NgayDat)
                .Select(hd => new
                {
                    hd.MaHd,
                    hd.NgayDat,
                    hd.NgayGiao,
                    DaysToDelivery = hd.NgayGiao.HasValue ? (hd.NgayGiao.Value - DateTime.Now).Days : (int?)null
                })
                .ToList();

            var model = orders.Select(o => new Thongbao
            {
                MaHd = o.MaHd,
                NgayDat = o.NgayDat,
                NgayGiao = o.NgayGiao,
                DaysToDelivery = o.DaysToDelivery
            }).ToList();

            var upcomingDeliveriesCount = orders.Count(o => o.DaysToDelivery.HasValue && o.DaysToDelivery.Value <= 99);
            ViewData["UpcomingDeliveriesCount"] = upcomingDeliveriesCount;

            return View(model);
        }
    }
}