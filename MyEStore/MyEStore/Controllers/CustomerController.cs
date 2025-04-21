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
using System.Security.Cryptography;
using System.Net.Http;
using System.Text.Json;

namespace MyEStore.Controllers
{
    public class CustomerController : Controller
    {
        private readonly MyeStoreContext _ctx;
        private readonly HttpClient _httpClient = new HttpClient();
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
            if (kh == null)
            {
                ViewBag.ThongBao = "Tên tài khoản không tồn tại.";
                return View();
            }

            if (kh.IsLocked && kh.LockoutEnd > DateTime.Now)
            {
                ViewBag.ThongBao = $"Tài khoản của bạn đã bị tạm khóa đến {kh.LockoutEnd?.ToString("HH:mm dd/MM/yyyy")}. Vui lòng kiểm tra email để mở khóa tài khoản.";
                return View();
            }

            if (!kh.HieuLuc)
            {
                ViewBag.ThongBao = "Tài khoản của bạn chưa được kích hoạt. Vui lòng kiểm tra email để kích hoạt hoặc yêu cầu gửi lại email kích hoạt.";
                return View();
            }

            if (!BCrypt.Net.BCrypt.Verify(model.Password + kh.RandomKey, kh.MatKhau))
            {
                kh.FailedLoginAttempts++;
                if (kh.FailedLoginAttempts >= 5)
                {
                    kh.IsLocked = true;
                    kh.LockoutEnd = DateTime.Now.AddMinutes(30); // Khóa 30 phút
                    var unlockCode = Guid.NewGuid().ToString();
                    kh.ActivationCode = unlockCode; // Tái sử dụng ActivationCode cho mã mở khóa
                    kh.ActivationCodeExpiry = DateTime.Now.AddMinutes(5); // Mã mở khóa hết hạn sau 5 phút

                    var unlockLink = Url.Action("UnlockAccount", "Customer", new { code = unlockCode }, Request.Scheme);
                    var message = $@"
                    <div style='font-family: Arial, sans-serif; padding: 25px; background-color: #f5f7fa; color: #333;'>
                        <div style='max-width: 600px; margin: auto; background-color: #ffffff; padding: 30px; border-radius: 10px; box-shadow: 0 3px 15px rgba(0, 0, 0, 0.1);'>
                            <div style='text-align: center; margin-bottom: 25px; border-bottom: 2px solid #f0f0f0; padding-bottom: 20px;'>
                                <h1 style='color: #0066cc; font-size: 24px; margin: 0;'>SiderGin Support</h1>
                                <p style='color: #666; margin: 5px 0 0;'>Yêu cầu mở khóa tài khoản</p>
                            </div>
                            <h2 style='color: #0066cc; margin-top: 0;'>Xin chào {kh.HoTen},</h2>
                            <p style='line-height: 1.6; margin-bottom: 20px;'>Tài khoản của bạn tại <strong>SiderGin</strong> đã bị tạm khóa do nhập sai mật khẩu quá nhiều lần. Vui lòng nhấp vào liên kết bên dưới trong vòng <strong>5 phút</strong> để mở khóa tài khoản:</p>
                            <div style='text-align: center; margin: 25px 0;'>
                                <a href='{unlockLink}' style='display: inline-block; padding: 12px 24px; background-color: #0066cc; color: #ffffff; text-decoration: none; border-radius: 6px; font-weight: bold;'>Mở khóa tài khoản</a>
                            </div>
                            <p style='line-height: 1.6; margin-bottom: 20px;'>Liên kết này sẽ hết hạn sau 5 phút. Nếu liên kết hết hạn, bạn có thể yêu cầu mở khóa lại tại trang đăng nhập.</p>
                            <p style='line-height: 1.6;'>Nếu bạn không thực hiện yêu cầu này, vui lòng liên hệ với chúng tôi qua:</p>
                            <div style='display: flex; margin: 15px 0 25px;'>
                                <div style='margin-right: 20px;'>
                                    <p style='margin: 0; color: #666;'>
                                        <span style='font-size: 16px;'>📞</span> Hotline
                                    </p>
                                    <p style='margin: 5px 0 0; font-weight: bold;'>0123 456 789</p>
                                </div>
                                <div>
                                    <p style='margin: 0; color: #666;'>
                                        <span style='font-size: 16px;'>✉️</span> Email hỗ trợ
                                    </p>
                                    <p style='margin: 5px 0 0; font-weight: bold;'>support@sidergin.com</p>
                                </div>
                            </div>
                            <div style='margin-top: 30px; padding-top: 20px; border-top: 1px solid #eaeaea;'>
                                <p style='margin: 0;'>Trân trọng,<br><strong>Đội ngũ hỗ trợ SiderGin</strong></p>
                            </div>
                        </div>
                    </div>";

                    await SendEmail(kh.Email, "Mở khóa tài khoản SiderGin", message);
                    _ctx.SaveChanges();

                    ViewBag.ThongBao = "Tài khoản của bạn đã bị tạm khóa do nhập sai mật khẩu quá nhiều lần. Vui lòng kiểm tra email để mở khóa tài khoản.";
                    return View();
                }
                _ctx.SaveChanges();
                ViewBag.ThongBao = $"Mật khẩu không đúng. Bạn còn {5 - kh.FailedLoginAttempts} lần thử.";
                return View();
            }

            // Đăng nhập thành công, reset số lần thử
            kh.FailedLoginAttempts = 0;
            kh.DangNhapLanCuoi = DateTime.Now;
            _ctx.SaveChanges();

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

        [HttpGet]
        public IActionResult UnlockAccount(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                ViewBag.ErrorMessage = "Mã mở khóa không hợp lệ.";
                return View("UnlockAccount");
            }

            var customer = _ctx.KhachHangs.SingleOrDefault(kh => kh.ActivationCode == code && kh.IsLocked);
            if (customer == null)
            {
                ViewBag.ErrorMessage = "Mã mở khóa không tồn tại hoặc đã được sử dụng.";
                return View("UnlockAccount");
            }

            if (customer.ActivationCodeExpiry < DateTime.Now)
            {
                ViewBag.ErrorMessage = "Liên kết mở khóa đã hết hạn. Vui lòng yêu cầu mở khóa lại tại trang đăng nhập.";
                return View("UnlockAccount");
            }

            customer.IsLocked = false;
            customer.LockoutEnd = null;
            customer.FailedLoginAttempts = 0;
            customer.ActivationCode = null;
            customer.ActivationCodeExpiry = null;
            _ctx.SaveChanges();

            ViewBag.SuccessMessage = "Tài khoản của bạn đã được mở khóa thành công! Vui lòng đăng nhập lại.";
            return View("UnlockAccount");
        }

        [HttpGet]
        public IActionResult ResendActivationEmail()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendActivationEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.ErrorMessage = "Vui lòng nhập email.";
                return View();
            }

            var customer = await _ctx.KhachHangs.SingleOrDefaultAsync(kh => kh.Email == email && !kh.HieuLuc);
            if (customer == null)
            {
                ViewBag.ErrorMessage = "Email không tồn tại hoặc tài khoản đã được kích hoạt.";
                return View();
            }

            var newActivationCode = Guid.NewGuid().ToString();
            customer.ActivationCode = newActivationCode;
            customer.ActivationCodeExpiry = DateTime.Now.AddMinutes(5);
            await _ctx.SaveChangesAsync();

            var activationLink = Url.Action("ActivateAccount", "Customer", new { code = newActivationCode }, Request.Scheme);
            var message = $@"
            <div style='font-family: Arial, sans-serif; padding: 25px; background-color: #f5f7fa; color: #333;'>
                <div style='max-width: 600px; margin: auto; background-color: #ffffff; padding: 30px; border-radius: 10px; box-shadow: 0 3px 15px rgba(0, 0, 0, 0.1);'>
                    <div style='text-align: center; margin-bottom: 25px; border-bottom: 2px solid #f0f0f0; padding-bottom: 20px;'>
                        <h1 style='color: #0066cc; font-size: 24px; margin: 0;'>SiderGin Support</h1>
                        <p style='color: #666; margin: 5px 0 0;'>Yêu cầu gửi lại email kích hoạt</p>
                    </div>
                    <h2 style='color: #0066cc; margin-top: 0;'>Xin chào {customer.HoTen},</h2>
                    <p style='line-height: 1.6; margin-bottom: 20px;'>Bạn đã yêu cầu gửi lại email kích hoạt cho tài khoản tại <strong>SiderGin</strong>. Vui lòng kích hoạt tài khoản của bạn trong vòng <strong>5 phút</strong> bằng cách nhấp vào liên kết bên dưới:</p>
                    <div style='text-align: center; margin: 25px 0;'>
                        <a href='{activationLink}' style='display: inline-block; padding: 12px 24px; background-color: #0066cc; color: #ffffff; text-decoration: none; border-radius: 6px; font-weight: bold;'>Kích hoạt tài khoản</a>
                    </div>
                    <p style='line-height: 1.6; margin-bottom: 20px;'>Liên kết này sẽ hết hạn sau 5 phút. Nếu liên kết hết hạn, bạn có thể yêu cầu gửi lại email kích hoạt tại trang đăng nhập.</p>
                    <p style='line-height: 1.6;'>Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email này hoặc liên hệ với chúng tôi qua:</p>
                    <div style='display: flex; margin: 15px 0 25px;'>
                        <div style='margin-right: 20px;'>
                            <p style='margin: 0; color: #666;'>
                                <span style='font-size: 16px;'>📞</span> Hotline
                            </p>
                            <p style='margin: 5px 0 0; font-weight: bold;'>0123 456 789</p>
                        </div>
                        <div>
                            <p style='margin: 0; color: #666;'>
                                <span style='font-size: 16px;'>✉️</span> Email hỗ trợ
                            </p>
                            <p style='margin: 5px 0 0; font-weight: bold;'>support@sidergin.com</p>
                        </div>
                    </div>
                    <div style='margin-top: 30px; padding-top: 20px; border-top: 1px solid #eaeaea;'>
                        <p style='margin: 0;'>Trân trọng,<br><strong>Đội ngũ hỗ trợ SiderGin</strong></p>
                    </div>
                </div>
            </div>";

            await SendEmail(customer.Email, "Gửi lại email kích hoạt SiderGin", message);

            ViewBag.SuccessMessage = "Email kích hoạt đã được gửi lại. Vui lòng kiểm tra hộp thư của bạn.";
            return View();
        }

        [HttpGet]
        public IActionResult ActivateAccount(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                ViewBag.ErrorMessage = "Mã kích hoạt không hợp lệ.";
                return View("ActivationResult");
            }

            var customer = _ctx.KhachHangs.SingleOrDefault(kh => kh.ActivationCode == code);
            if (customer == null)
            {
                ViewBag.ErrorMessage = "Mã kích hoạt không tồn tại hoặc đã được sử dụng.";
                return View("ActivationResult");
            }

            if (customer.HieuLuc)
            {
                ViewBag.SuccessMessage = "Tài khoản của bạn đã được kích hoạt trước đó.";
                return View("ActivationResult");
            }

            if (customer.ActivationCodeExpiry < DateTime.Now)
            {
                ViewBag.ErrorMessage = "Liên kết kích hoạt đã hết hạn. Vui lòng yêu cầu gửi lại email kích hoạt.";
                return View("ActivationResult");
            }

            customer.HieuLuc = true;
            customer.ActivationCode = null;
            customer.ActivationCodeExpiry = null;
            _ctx.SaveChanges();

            ViewBag.SuccessMessage = "Tài khoản của bạn đã được kích hoạt thành công! Vui lòng đăng nhập.";
            return View("ActivationResult");
        }


        private async Task<bool> IsRealEmail(string email)
        {
            string apiKey = "03424848e67c47c19fe0a512b4b8d768"; // Thay bằng key bạn nhận được
            string requestUrl = $"https://emailvalidation.abstractapi.com/v1/?api_key={apiKey}&email={email}";

            try
            {
                var response = await _httpClient.GetAsync(requestUrl);
                if (!response.IsSuccessStatusCode)
                    return false;

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<EmailVerificationResult>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // Bạn có thể kiểm tra nhiều thuộc tính hơn nếu muốn
                return result.Deliverability == "DELIVERABLE";
            }
            catch
            {
                return false;
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM model)
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
            // Kiểm tra email có thực sự tồn tại không
            if (!await IsRealEmail(model.Email))
            {
                ViewBag.ThongBao = "Email không hợp lệ hoặc không tồn tại. Vui lòng sử dụng địa chỉ email thực.";
                return View(model);
            }


            var randomKey = GenerateRandomKey();
            var activationCode = Guid.NewGuid().ToString();
            var activationExpiry = DateTime.Now.AddMinutes(5);
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password + randomKey);

            var newCustomer = new KhachHang
            {
                MaKh = model.UserName,
                MatKhau = hashedPassword,
                HoTen = model.FullName,
                Email = model.Email,
                DienThoai = model.PhoneNumber,
                DiaChi = model.Address,
                RandomKey = randomKey,
                NgayTaoTaiKhoan = DateTime.Now,
                HieuLuc = false,
                ActivationCode = activationCode,
                ActivationCodeExpiry = activationExpiry
            };

            _ctx.KhachHangs.Add(newCustomer);
            _ctx.SaveChanges();

            var activationLink = Url.Action("ActivateAccount", "Customer", new { code = activationCode }, Request.Scheme);
            var message = $@"
              <html>
              <head>
                <meta http-equiv='Content-Type' content='text/html; charset=UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Kích hoạt tài khoản SiderGin</title>
              </head>
              <body style='font-family: Arial, sans-serif; padding: 25px; background-color: #f5f7fa; color: #333;'>
                  <div style='max-width: 600px; margin: auto; background-color: #ffffff; padding: 30px; border-radius: 10px; box-shadow: 0 3px 15px rgba(0, 0, 0, 0.1);'>
                      <div style='text-align: center; margin-bottom: 25px; border-bottom: 2px solid #f0f0f0; padding-bottom: 20px;'>
                          <h1 style='color: #0066cc; font-size: 24px; margin: 0;'>SiderGin Support</h1>
                          <p style='color: #666; margin: 5px 0 0;'>Chào mừng bạn đến với SiderGin!</p>
                      </div>
                      <h2 style='color: #0066cc; margin-top: 0;'>Xin chào {newCustomer.HoTen},</h2>
                      <p style='line-height: 1.6; margin-bottom: 20px;'>Cảm ơn bạn đã đăng ký tài khoản tại <strong>SiderGin</strong>. Vui lòng kích hoạt tài khoản của bạn trong vòng <strong>5 phút</strong> bằng cách nhấp vào liên kết bên dưới:</p>
                      <div style='text-align: center; margin: 25px 0;'>
                          <a href='{activationLink}' style='display: inline-block; padding: 12px 24px; background-color: #0066cc; color: #ffffff; text-decoration: none; border-radius: 6px; font-weight: bold;'>Kích hoạt tài khoản</a>
                      </div>
                      <p style='line-height: 1.6; margin-bottom: 20px;'>Liên kết này sẽ hết hạn sau 5 phút. Nếu liên kết hết hạn, bạn có thể yêu cầu gửi lại email kích hoạt tại trang đăng nhập.</p>
                      <p style='line-height: 1.6;'>Nếu bạn không thực hiện đăng ký này, vui lòng bỏ qua email này hoặc liên hệ với chúng tôi qua:</p>
                      <div style='display: flex; margin: 15px 0 25px;'>
                          <div style='margin-right: 20px;'>
                              <p style='margin: 0; color: #666;'>
                                  <span style='font-size: 16px;'>📞</span> Hotline
                              </p>
                              <p style='margin: 5px 0 0; font-weight: bold;'>0123 456 789</p>
                          </div>
                          <div>
                              <p style='margin: 0; color: #666;'>
                                  <span style='font-size: 16px;'>✉️</span> Email hỗ trợ
                              </p>
                              <p style='margin: 5px 0 0; font-weight: bold;'>support@sidergin.com</p>
                          </div>
                      </div>
                      <div style='margin-top: 30px; padding-top: 20px; border-top: 1px solid #eaeaea;'>
                          <p style='margin: 0;'>Trân trọng,<br><strong>Đội ngũ hỗ trợ SiderGin</strong></p>
                      </div>
                  </div>
              </body>
              </html>";


            await SendEmail(newCustomer.Email, "Kích hoạt tài khoản SiderGin", message);

            TempData["ThongBao"] = "Đăng ký thành công! Vui lòng kiểm tra email để kích hoạt tài khoản trong vòng 5 phút.";
            return RedirectToAction("Login");
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
                DiaChi = customer.DiaChi,
                DiaChiPhu = customer.DiaChiPhu,
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

            if (!BCrypt.Net.BCrypt.Verify(model.Password + customer.RandomKey, customer.MatKhau))
            {
                TempData["Error"] = "Mật khẩu hiện tại không chính xác.";
                return RedirectToAction("Profile");
            }

            if (model.NewPassword != model.ConfirmPassword)
            {
                TempData["Error"] = "Mật khẩu mới và mật khẩu xác nhận không khớp.";
                return RedirectToAction("Profile");
            }

            var newRandomKey = GenerateRandomKey();
            customer.MatKhau = BCrypt.Net.BCrypt.HashPassword(model.NewPassword + newRandomKey);
            customer.RandomKey = newRandomKey;
            _ctx.SaveChanges();

            TempData["Success"] = "Mật khẩu của bạn đã được thay đổi thành công.";
            return RedirectToAction("Profile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateAddress(string province, string district, string ward, string streetAddress, string diaChi,string diaChiPhu)
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

            if (string.IsNullOrWhiteSpace(province) || string.IsNullOrWhiteSpace(district) ||
                string.IsNullOrWhiteSpace(ward) || string.IsNullOrWhiteSpace(streetAddress) ||
                string.IsNullOrWhiteSpace(diaChi))
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ thông tin địa chỉ.";
                return RedirectToAction("Profile");
            }

            try
            {
                customer.DiaChi = diaChi;
                customer.DiaChiPhu =diaChiPhu;
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

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.ErrorMessage = "Vui lòng nhập email.";
                return View();
            }

            var khachHang = await _ctx.KhachHangs.SingleOrDefaultAsync(kh => kh.Email == email);
            if (khachHang == null)
            {
                ViewBag.ErrorMessage = "Email không tồn tại.";
                return View();
            }

            // Tạo và lưu mã OTP
            var otpCode = GenerateOtpCode();
            khachHang.OtpCode = otpCode;
            khachHang.OtpExpiry = DateTime.Now.AddMinutes(5); // OTP hết hạn sau 5 phút
            await _ctx.SaveChangesAsync();

            // Gửi email chứa OTP
            string message = $@"
            <div style='font-family: Arial, sans-serif; padding: 25px; background-color: #f5f7fa; color: #333;'>
                <div style='max-width: 600px; margin: auto; background-color: #ffffff; padding: 30px; border-radius: 10px; box-shadow: 0 3px 15px rgba(0, 0, 0, 0.1);'>
                    <div style='text-align: center; margin-bottom: 25px; border-bottom: 2px solid #f0f0f0; padding-bottom: 20px;'>
                        <h1 style='color: #0066cc; font-size: 24px; margin: 0;'>SiderGin Support</h1>
                        <p style='color: #666; margin: 5px 0 0;'>Xác minh tài khoản</p>
                    </div>
                    <h2 style='color: #0066cc; margin-top: 0;'>Xin chào {khachHang.HoTen},</h2>
                    <p style='line-height: 1.6; margin-bottom: 20px;'>Bạn vừa yêu cầu đặt lại mật khẩu cho tài khoản tại <strong>SiderGin</strong>. Vui lòng sử dụng mã OTP dưới đây để xác minh danh tính của bạn:</p>
                    <div style='background-color: #f8f9fa; border-left: 4px solid #0066cc; padding: 15px 20px; margin: 25px 0; border-radius: 4px;'>
                        <p style='margin: 0 0 5px; font-size: 14px; color: #666;'>Mã OTP của bạn:</p>
                        <p style='font-size: 22px; font-weight: bold; color: #d9534f; margin: 0; letter-spacing: 1px; font-family: Consolas, monospace;'>{otpCode}</p>
                    </div>
                    <p style='line-height: 1.6; margin-bottom: 20px;'>Mã OTP này sẽ hết hạn sau <strong>5 phút</strong>. Vui lòng nhập mã vào trang xác minh để tiếp tục quá trình đặt lại mật khẩu.</p>
                    <p style='line-height: 1.6;'>Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email này hoặc liên hệ với chúng tôi qua:</p>
                    <div style='display: flex; margin: 15px 0 25px;'>
                        <div style='margin-right: 20px;'>
                            <p style='margin: 0; color: #666;'>
                                <span style='font-size: 16px;'>📞</span> Hotline
                            </p>
                            <p style='margin: 5px 0 0; font-weight: bold;'>0123 456 789</p>
                        </div>
                        <div>
                            <p style='margin: 0; color: #666;'>
                                <span style='font-size: 16px;'>✉️</span> Email hỗ trợ
                            </p>
                            <p style='margin: 5px 0 0; font-weight: bold;'>support@sidergin.com</p>
                        </div>
                    </div>
                    <div style='margin-top: 30px; padding-top: 20px; border-top: 1px solid #eaeaea;'>
                        <p style='margin: 0;'>Trân trọng,<br><strong>Đội ngũ hỗ trợ SiderGin</strong></p>
                    </div>
                </div>
            </div>";

            await SendEmail(khachHang.Email, "Mã OTP xác minh tài khoản SiderGin", message);

            ViewBag.SuccessMessage = "Mã OTP đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư (và thư mục spam) để lấy mã.";
            ViewBag.Email = email; // Lưu email để sử dụng trong bước xác minh OTP
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> VerifyOtp(string email, string otpCode)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(otpCode))
            {
                ViewBag.ErrorMessage = "Vui lòng nhập email và mã OTP.";
                ViewBag.Email = email;
                return View("ForgotPassword");
            }

            var khachHang = await _ctx.KhachHangs.SingleOrDefaultAsync(kh => kh.Email == email);
            if (khachHang == null)
            {
                ViewBag.ErrorMessage = "Email không tồn tại.";
                ViewBag.Email = email;
                return View("ForgotPassword");
            }

            if (khachHang.OtpCode != otpCode)
            {
                ViewBag.ErrorMessage = "Mã OTP không chính xác.";
                ViewBag.Email = email;
                return View("ForgotPassword");
            }

            if (khachHang.OtpExpiry < DateTime.Now)
            {
                ViewBag.ErrorMessage = "Mã OTP đã hết hạn. Vui lòng yêu cầu gửi lại mã.";
                ViewBag.Email = email;
                return View("ForgotPassword");
            }

            // OTP hợp lệ, tạo mật khẩu mới
            var newPassword = GenerateRandomPassword();
            var newRandomKey = GenerateRandomKey();
            khachHang.MatKhau = BCrypt.Net.BCrypt.HashPassword(newPassword + newRandomKey);
            khachHang.RandomKey = newRandomKey;
            khachHang.OtpCode = null; // Xóa OTP sau khi sử dụng
            khachHang.OtpExpiry = null; // Xóa thời gian hết hạn
            await _ctx.SaveChangesAsync();

            // Gửi email chứa mật khẩu mới
            string message = $@"
            <div style='font-family: Arial, sans-serif; padding: 25px; background-color: #f5f7fa; color: #333;'>
                <div style='max-width: 600px; margin: auto; background-color: #ffffff; padding: 30px; border-radius: 10px; box-shadow: 0 3px 15px rgba(0, 0, 0, 0.1);'>
                    <div style='text-align: center; margin-bottom: 25px; border-bottom: 2px solid #f0f0f0; padding-bottom: 20px;'>
                        <h1 style='color: #0066cc; font-size: 24px; margin: 0;'>SiderGin Support</h1>
                        <p style='color: #666; margin: 5px 0 0;'>Thông báo đặt lại mật khẩu</p>
                    </div>
                    <h2 style='color: #0066cc; margin-top: 0;'>Xin chào {khachHang.HoTen},</h2>
                    <p style='line-height: 1.6; margin-bottom: 20px;'>Tài khoản của bạn tại <strong>SiderGin</strong> đã được xác minh thành công. Dưới đây là mật khẩu mới của bạn:</p>
                    <div style='background-color: #f8f9fa; border-left: 4px solid #0066cc; padding: 15px 20px; margin: 25px 0; border-radius: 4px;'>
                        <p style='margin: 0 0 5px; font-size: 14px; color: #666;'>Mật khẩu mới của bạn:</p>
                        <p style='font-size: 22px; font-weight: bold; color: #d9534f; margin: 0; letter-spacing: 1px; font-family: Consolas, monospace;'>{newPassword}</p>
                    </div>
                    <div style='background-color: #fff8e1; padding: 15px; border-radius: 6px; margin-bottom: 25px;'>
                        <p style='margin: 0; display: flex; align-items: center;'>
                            <span style='font-size: 20px; margin-right: 10px;'>⚠️</span>
                            <span><strong>Lưu ý:</strong> Vui lòng đăng nhập và thay đổi mật khẩu này ngay lập tức để đảm bảo an toàn cho tài khoản của bạn.</span>
                        </p>
                    </div>
                    <div style='background-color: #f0f7ff; padding: 20px; border-radius: 6px; margin-bottom: 25px;'>
                        <h3 style='color: #0066cc; margin-top: 0; display: flex; align-items: center;'>
                            <span style='margin-right: 10px;'>📌</span>
                            <span>Hướng dẫn đổi mật khẩu:</span>
                        </h3>
                        <ol style='margin: 15px 0 0; padding-left: 25px;'>
                            <li style='margin-bottom: 12px; line-height: 1.5;'>Đăng nhập vào hệ thống với mật khẩu mới được cung cấp ở trên.</li>
                            <li style='margin-bottom: 12px; line-height: 1.5;'>Nhấp vào biểu tượng người dùng ở góc trên bên phải và chọn <strong>Thông tin cá nhân</strong>.</li>
                            <li style='margin-bottom: 12px; line-height: 1.5;'>Chọn tab <strong>Bảo mật</strong> hoặc <strong>Đổi mật khẩu</strong>.</li>
                            <li style='margin-bottom: 12px; line-height: 1.5;'>Nhập mật khẩu hiện tại (mật khẩu mới được cấp) và mật khẩu mới mong muốn.</li>
                            <li style='margin-bottom: 0; line-height: 1.5;'>Nhấn <strong>Lưu thay đổi</strong> để hoàn tất.</li>
                        </ol>
                    </div>
                    <p style='line-height: 1.6;'>Nếu bạn không thực hiện yêu cầu này hoặc cần hỗ trợ thêm, vui lòng liên hệ ngay với chúng tôi qua:</p>
                    <div style='display: flex; margin: 15px 0 25px;'>
                        <div style='margin-right: 20px;'>
                            <p style='margin: 0; color: #666;'>
                                <span style='font-size: 16px;'>📞</span> Hotline
                            </p>
                            <p style='margin: 5px 0 0; font-weight: bold;'>0123 456 789</p>
                        </div>
                        <div>
                            <p style='margin: 0; color: #666;'>
                                <span style='font-size: 16px;'>✉️</span> Email hỗ trợ
                            </p>
                            <p style='margin: 5px 0 0; font-weight: bold;'>support@sidergin.com</p>
                        </div>
                    </div>
                    <div style='margin-top: 30px; padding-top: 20px; border-top: 1px solid #eaeaea;'>
                        <p style='margin: 0;'>Trân trọng,<br><strong>Đội ngũ hỗ trợ SiderGin</strong></p>
                    </div>
                </div>
            </div>";

            await SendEmail(khachHang.Email, "Mật khẩu mới của bạn", message);

            ViewBag.SuccessMessage = "Mật khẩu mới đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư và đăng nhập.";
            return View("ForgotPassword");
        }

        private string GenerateOtpCode()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString(); // Tạo mã OTP 6 chữ số
        }

        private string GenerateRandomPassword(int length = 8)
        {
            const string validChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            return new string(Enumerable.Repeat(validChars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private string GenerateRandomKey(int length = 16)
        {
            var randomBytes = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            return Convert.ToBase64String(randomBytes);
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
                From = new MailAddress("truongminhduc4002@gmail.com", "Sidergin Support"),
                Subject = subject,
                Body = message,
                IsBodyHtml = true,
                Priority = MailPriority.Normal,
                HeadersEncoding = System.Text.Encoding.UTF8,
                SubjectEncoding = System.Text.Encoding.UTF8,
                BodyEncoding = System.Text.Encoding.UTF8
            };

            mailMessage.To.Add(toEmail);
            mailMessage.ReplyToList.Add(new MailAddress("truongminhduc4002@gmail.com", "Sidergin Support"));

            // Header chuẩn hóa
            mailMessage.Headers.Add("X-Priority", "3");
            mailMessage.Headers.Add("X-MSMail-Priority", "Normal");
            mailMessage.Headers.Add("Importance", "Normal");
            mailMessage.Headers.Add("X-Mailer", "Sidergin App");

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