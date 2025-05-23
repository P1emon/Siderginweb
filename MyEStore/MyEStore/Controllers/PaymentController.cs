﻿using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyEStore.Entities;
using MyEStore.Helpers;
using MyEStore.Models;
using MyEStore.Servicess;
using Newtonsoft.Json;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System;

namespace MyEStore.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly PaypalClient _paypalClient;
        private readonly MyeStoreContext _ctx;
        private readonly IConfiguration _configuration;
        private readonly IVnpayService _vnpayService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(PaypalClient paypalClient, MyeStoreContext ctx, IConfiguration configuration, IVnpayService vnpayService, ILogger<PaymentController> logger)
        {
            _paypalClient = paypalClient;
            _ctx = ctx;
            _configuration = configuration;
            _vnpayService = vnpayService;
            _logger = logger;
        }

        [HttpPost]
        public IActionResult MomoPayment()
        {
            var endpoint = _configuration["MoMo:Endpoint"];
            var partnerCode = _configuration["MoMo:PartnerCode"];
            var accessKey = _configuration["MoMo:AccessKey"];
            var secretKey = _configuration["MoMo:SecretKey"];
            var returnUrl = _configuration["MoMo:ReturnUrl"];
            var notifyUrl = _configuration["MoMo:NotifyUrl"];

            var tongTien = CartItems.Sum(p => p.ThanhTien);

            var orderInfo = "Thanh toán đơn hàng tại MyEStore";
            var requestId = Guid.NewGuid().ToString();
            var orderId = "DH" + DateTime.Now.Ticks.ToString();
            var extraData = "";

            string rawHash = $"accessKey={accessKey}&amount={tongTien}&extraData={extraData}&ipnUrl={notifyUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={returnUrl}&requestId={requestId}&requestType=captureWallet";
            string signature = GenerateSignature(rawHash, secretKey);

            var body = new
            {
                partnerCode = partnerCode,
                accessKey = accessKey,
                requestId = requestId,
                amount = tongTien.ToString(),
                orderId = orderId,
                orderInfo = orderInfo,
                redirectUrl = returnUrl,
                ipnUrl = notifyUrl,
                extraData = extraData,
                requestType = "captureWallet",
                signature = signature
            };

            using (var client = new HttpClient())
            {
                var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
                var response = client.PostAsync(endpoint, content).Result;

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = response.Content.ReadAsStringAsync().Result;
                    dynamic jsonResponse = JsonConvert.DeserializeObject(responseContent);
                    string payUrl = jsonResponse.payUrl;

                    return Redirect(payUrl);
                }
                else
                {
                    return BadRequest("Không thể tạo yêu cầu thanh toán MoMo.");
                }
            }
        }

        public IActionResult MomoReturn()
        {
            var queryString = Request.Query;
            string resultCode = queryString["resultCode"];
            string orderId = queryString["orderId"];
            string message = queryString["message"];
            string transId = queryString["transId"];
            string amount = queryString["amount"];

            if (resultCode == "0")
            {
                try
                {
                    var hoaDon = new HoaDon
                    {
                        MaKh = User.FindFirstValue("UserId"),
                        NgayDat = DateTime.Now,
                        HoTen = User.Identity.Name,
                        DiaChi = "N/A",
                        CachThanhToan = "MoMo",
                        CachVanChuyen = "N/A",
                        MaTrangThai = 0,
                        GhiChu = $"transId={transId}, amount={amount}"
                    };
                    _ctx.Add(hoaDon);
                    _ctx.SaveChanges();

                    foreach (var item in CartItems)
                    {
                        var cthd = new ChiTietHd
                        {
                            MaHd = hoaDon.MaHd,
                            MaHh = item.MaHh,
                            DonGia = item.DonGia,
                            SoLuong = item.SoLuong,
                            GiamGia = 1
                        };
                        _ctx.Add(cthd);
                    }
                    _ctx.SaveChanges();

                    HttpContext.Session.Set(CART_KEY, new List<CartItem>());

                    TempData["TransactionId"] = transId;
                    TempData["OrderId"] = orderId;

                    ViewBag.Message = "Thanh toán thành công!";
                    ViewBag.OrderId = orderId;
                    return View("Success");
                }
                catch (Exception e)
                {
                    ViewBag.Message = "Có lỗi khi lưu thông tin hóa đơn: " + e.Message;
                    return View("MomoFail");
                }
            }
            else
            {
                ViewBag.Message = "Thanh toán thất bại: " + message;
                ViewBag.OrderId = orderId;
                return View("MomoFail");
            }
        }

        private static string GenerateSignature(string data, string secretKey)
        {
            var encoding = Encoding.UTF8;
            using (var hmacsha256 = new HMACSHA256(encoding.GetBytes(secretKey)))
            {
                byte[] hashmessage = hmacsha256.ComputeHash(encoding.GetBytes(data));
                return BitConverter.ToString(hashmessage).Replace("-", "").ToLower();
            }
        }

        [HttpPost]
        public IActionResult MomoNotify()
        {
            using (var reader = new StreamReader(Request.Body))
            {
                var body = reader.ReadToEnd();
                dynamic jsonBody = JsonConvert.DeserializeObject(body);

                if (jsonBody.resultCode == "0")
                {
                    return Ok();
                }
                else
                {
                    return BadRequest();
                }
            }
        }

        public IActionResult Index()
        {
            var maKhachHang = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
            if (maKhachHang != null)
            {
                var khachHang = _ctx.KhachHangs.FirstOrDefault(k => k.MaKh == maKhachHang);
                ViewBag.KhachHangs = khachHang;
            }

            ViewBag.PaypalClientId = _paypalClient.ClientId;
            return View(CartItems);
        }

        [HttpPost]
        public IActionResult UpdateAddress(string newAddress)
        {
            var maKh = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
            if (maKh != null && !string.IsNullOrWhiteSpace(newAddress))
            {
                var kh = _ctx.KhachHangs.FirstOrDefault(k => k.MaKh == maKh);
                if (kh != null)
                {
                    kh.DiaChi = newAddress;
                    _ctx.SaveChanges();
                    return Json(new { success = true, diaChi = kh.DiaChi });
                }
            }

            return Json(new { success = false });
        }

        [HttpPost]
        public IActionResult AddSecondaryAddress(string secondaryAddress)
        {
            var maKh = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
            if (maKh != null && !string.IsNullOrWhiteSpace(secondaryAddress))
            {
                var kh = _ctx.KhachHangs.FirstOrDefault(k => k.MaKh == maKh);
                if (kh != null)
                {
                    kh.DiaChiPhu = secondaryAddress;
                    _ctx.SaveChanges();
                    return Json(new { success = true, diaChiPhu = kh.DiaChiPhu });
                }
            }

            return Json(new { success = false });
        }

        const string CART_KEY = "MY_CART";
        public List<CartItem> CartItems
        {
            get
            {
                var carts = HttpContext.Session.Get<List<CartItem>>(CART_KEY);
                if (carts == null)
                {
                    carts = new List<CartItem>();
                }
                return carts;
            }
        }

        [HttpPost]
        public async Task<IActionResult> PaypalOrder(decimal PhiVanChuyen, CancellationToken cancellationToken)
        {
            var tongTienVND = CartItems.Sum(p => p.ThanhTien);
            const decimal conversionRate = 25400;
            var tongTienUSD = tongTienVND / (double)conversionRate;
            tongTienUSD += (double)(PhiVanChuyen / conversionRate);
            var tongTien = tongTienUSD.ToString("F2");
            var donViTienTe = "USD";
            var orderIdref = "DH" + DateTime.Now.Ticks.ToString();

            try
            {
                var response = await _paypalClient.CreateOrder(tongTien, donViTienTe, orderIdref);
                return Ok(response);
            }
            catch (Exception e)
            {
                var error = new { e.GetBaseException().Message };
                return BadRequest(error);
            }
        }

        [HttpPost]
        public async Task<IActionResult> PaypalCapture(string orderId, string ngayGiao, string selectedAddress, double PhiVanChuyen, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _paypalClient.CaptureOrder(orderId);

                if (response.status == "COMPLETED")
                {
                    var reference = response.purchase_units[0].reference_id;
                    var transactionId = response.purchase_units[0].payments.captures[0].id;

                    var userId = User.FindFirstValue("UserId");
                    var userProfile = _ctx.KhachHangs.FirstOrDefault(u => u.MaKh == userId);

                    DateTime? ngayGiaoDate = null;
                    if (DateTime.TryParse(ngayGiao, out DateTime parsedDate))
                    {
                        ngayGiaoDate = parsedDate;
                    }

                    var hoaDon = new HoaDon
                    {
                        MaKh = userId,
                        NgayDat = DateTime.Now,
                        HoTen = User.Identity.Name,
                        DiaChi = selectedAddress,
                        CachThanhToan = "Paypal",
                        CachVanChuyen = "N/A",
                        MaTrangThai = 1,
                        PhiVanChuyen = 0,
                        NgayGiao = ngayGiaoDate,
                        GhiChu = $"Thanh toán thành công, phí vận chuyển sẽ trả khi nhận hàng, reference_id={reference}, transactionId={transactionId}"
                    };

                    _ctx.Add(hoaDon);
                    await _ctx.SaveChangesAsync();

                    foreach (var item in CartItems)
                    {
                        var cthd = new ChiTietHd
                        {
                            MaHd = hoaDon.MaHd,
                            MaHh = item.MaHh,
                            DonGia = item.DonGia,
                            SoLuong = item.SoLuong,
                            GiamGia = item.GiamGia
                        };
                        _ctx.Add(cthd);

                        // Deduct stock from HangHoa
                        var hangHoa = await _ctx.HangHoas.FirstOrDefaultAsync(hh => hh.MaHh == cthd.MaHh);
                        if (hangHoa != null && hangHoa.SoLuong >= cthd.SoLuong)
                        {
                            hangHoa.SoLuong -= cthd.SoLuong;
                            _ctx.Update(hangHoa);
                        }
                        else
                        {
                            _logger.LogError($"Hàng hóa {cthd.MaHh} không đủ tồn kho hoặc không tồn tại.");
                            throw new Exception($"Hàng hóa {cthd.MaHh} không đủ tồn kho hoặc không tồn tại.");
                        }
                    }
                    await _ctx.SaveChangesAsync();

                    await CongDiemChoKhachHangAsync(hoaDon.MaHd);
                    _logger.LogInformation("Cộng điểm cho khách hàng với MaHd={maHd}", hoaDon.MaHd);

                    try
                    {
                        string customerEmail = userProfile?.Email ?? "Không rõ";
                        string userName = userProfile?.HoTen ?? hoaDon.HoTen;
                        string phone = userProfile?.DienThoai ?? "Không rõ";
                        string adminEmail = _configuration["EmailSettings:AdminEmail"] ?? "phannguyendangkhoa0915@gmail.com";

                        await SendCustomerEmail(customerEmail, hoaDon, phone, userName);
                        await SendAdminEmail(adminEmail, hoaDon, phone, userName, customerEmail);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi khi gửi email Paypal");
                    }

                    HttpContext.Session.Set(CART_KEY, new List<CartItem>());

                    TempData["TransactionId"] = transactionId;
                    TempData["ReferenceId"] = reference;

                    return RedirectToAction("Success");
                }
                else
                {
                    return BadRequest(new { Message = "Có lỗi thanh toán" });
                }
            }
            catch (Exception e)
            {
                var error = new { e.GetBaseException().Message };
                return BadRequest(error);
            }
        }

        [HttpPost]
        public IActionResult VnpayOrder(string ngayGiao, string selectedAddress, double PhiVanChuyen, List<CartItem> CartItems)
        {
            var tongTien = CartItems.Sum(p => p.ThanhTien) ;
            var userId = User.FindFirstValue("UserId");
            var userProfile = _ctx.KhachHangs.FirstOrDefault(u => u.MaKh == userId);
            DateTime? ngayGiaoDate = DateTime.TryParse(ngayGiao, out DateTime parsedDate) ? parsedDate : (DateTime?)null;

            try
            {
                var hoaDon = new HoaDon
                {
                    MaKh = userId ?? string.Empty,
                    NgayDat = DateTime.Now,
                    HoTen = User.Identity?.Name ?? string.Empty,
                    DiaChi = selectedAddress,
                    CachThanhToan = "VNPay",
                    CachVanChuyen = "N/A",
                    MaTrangThai = 0,
                    PhiVanChuyen = 0,
                    NgayGiao = ngayGiaoDate,
                    GhiChu = "Đang chờ thanh toán VNPay"
                };

                _ctx.Add(hoaDon);
                _ctx.SaveChanges();

                foreach (var item in CartItems)
                {
                    var cthd = new ChiTietHd
                    {
                        MaHd = hoaDon.MaHd,
                        MaHh = item.MaHh,
                        DonGia = item.DonGia,
                        SoLuong = item.SoLuong,
                        GiamGia = item.GiamGia
                    };
                    _ctx.Add(cthd);
                }

                _ctx.SaveChanges();

                HttpContext.Session.Set(CART_KEY, new List<CartItem>());

                var paymentRequest = new VnPaymentRequestModel
                {
                    Amount = tongTien,
                    OrderId = hoaDon.MaHd,
                    CreatedDate = DateTime.Now
                };

                var paymentUrl = _vnpayService.CreatePaymentUrl(HttpContext, paymentRequest);
                return Redirect(paymentUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating VNPay payment request.");
                ViewBag.Message = "Lỗi tạo yêu cầu thanh toán: " + ex.Message;
                return View("MomoFail");
            }
        }

        public async Task<IActionResult> Success()
        {
            return View();
        }

        public IActionResult VnpayFail()
        {
            return View();
        }

        public IActionResult VnpayCancel()
        {
            return View();
        }

        public async Task<IActionResult> VnpayCallback()
        {
            try
            {
                var response = _vnpayService.PaymentExcute(Request.Query);

                _logger.LogInformation("VNPay Response: ResponseCode={ResponseCode}, Success={Success}, Message={Message}",
                    response.VnPayResponseCode, response.Success, response.Message);

                if (response.VnPayResponseCode == "00" && response.Success)
                {
                    if (!int.TryParse(response.OrderId, out int maHd))
                    {
                        _logger.LogWarning("OrderId không hợp lệ: {OrderId}", response.OrderId);
                        ViewBag.Message = "Mã đơn hàng không hợp lệ.";
                        return View("VnpayCancel");
                    }

                    var hoaDon = _ctx.HoaDons.FirstOrDefault(h => h.MaHd == maHd);
                    if (hoaDon == null)
                    {
                        _logger.LogWarning("Không tìm thấy hóa đơn có MaHd={maHd}", maHd);
                        ViewBag.Message = "Không tìm thấy hóa đơn.";
                        return View("VnpayCancel");
                    }

                    if (hoaDon.MaTrangThai == 1)
                    {
                        ViewBag.Message = "Hóa đơn đã được xử lý thành công!";
                        return View("Success");
                    }

                    hoaDon.MaTrangThai = 1;
                    hoaDon.CachThanhToan = "VNPay";
                    hoaDon.GhiChu = $"Thanh toán thành công, TransactionId={response.TransactionId}";
                    _ctx.SaveChanges();

                    // Deduct stock from HangHoa
                    var chiTietHds = _ctx.ChiTietHds.Where(ct => ct.MaHd == maHd).ToList();
                    foreach (var cthd in chiTietHds)
                    {
                        var hangHoa = await _ctx.HangHoas.FirstOrDefaultAsync(hh => hh.MaHh == cthd.MaHh);
                        if (hangHoa != null && hangHoa.SoLuong >= cthd.SoLuong)
                        {
                            hangHoa.SoLuong -= cthd.SoLuong;
                            _ctx.Update(hangHoa);
                        }
                        else
                        {
                            _logger.LogError($"Hàng hóa {cthd.MaHh} không đủ tồn kho hoặc không tồn tại.");
                            throw new Exception($"Hàng hóa {cthd.MaHh} không đủ tồn kho hoặc không tồn tại.");
                        }
                    }
                    await _ctx.SaveChangesAsync();

                    await CongDiemChoKhachHangAsync(maHd);
                    _logger.LogInformation("Cộng điểm cho khách hàng với MaHd={maHd}", maHd);

                    try
                    {
                        var userId = User.FindFirstValue("UserId");
                        var userProfile = _ctx.KhachHangs.FirstOrDefault(u => u.MaKh == userId);

                        string customerEmail = userProfile?.Email ?? "Không rõ";
                        string userName = userProfile?.HoTen ?? hoaDon.HoTen;
                        string phone = userProfile?.DienThoai ?? "Không rõ";
                        string adminEmail = _configuration["EmailSettings:AdminEmail"] ?? "phannguyendangkhoa0915@gmail.com";

                        await SendCustomerEmail(customerEmail, hoaDon, phone, userName);
                        await SendAdminEmail(adminEmail, hoaDon, phone, userName, customerEmail);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi khi gửi email xác nhận thanh toán VNPay");
                    }

                    HttpContext.Session.Set(CART_KEY, new List<CartItem>());

                    ViewBag.Message = "Thanh toán thành công!";
                    return View("Success");
                }
                else if (response.VnPayResponseCode == "24")
                {
                    _logger.LogWarning("Giao dịch bị hủy bởi người dùng. ResponseCode={ResponseCode}", response.VnPayResponseCode);
                    ViewBag.Message = "Bạn đã hủy giao dịch. Thanh toán chưa được thực hiện.";
                    return View("VnpayCancel");
                }
                else
                {
                    _logger.LogError("Giao dịch thất bại. ResponseCode={ResponseCode}, Message={Message}",
                        response.VnPayResponseCode, response.Message);
                    ViewBag.Message = $"Giao dịch không thành công: {response.Message}";
                    return View("VnpayCancel");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xử lý callback từ VNPay");
                ViewBag.Message = "Có lỗi xảy ra khi xử lý thanh toán: " + ex.Message;
                return View("MomoFail");
            }
        }

        [HttpPost]
        public IActionResult VnpayOrder2(int MaHd)
        {
            try
            {
                var hoaDon = _ctx.HoaDons.FirstOrDefault(h => h.MaHd == MaHd);

                if (hoaDon == null)
                {
                    _logger.LogWarning($"Không tìm thấy hóa đơn với MaHd: {MaHd}");
                    ViewBag.Message = "Không tìm thấy hóa đơn.";
                    return View("VnpayCancel");
                }

                _ctx.Entry(hoaDon).Collection(h => h.ChiTietHds).Load();

                if (hoaDon.ChiTietHds == null || !hoaDon.ChiTietHds.Any())
                {
                    _logger.LogError($"Không có chi tiết hóa đơn nào cho MaHd: {MaHd}");
                    ViewBag.Message = "Hóa đơn không có chi tiết.";
                    return View("VnpayCancel");
                }

                var tongTien = hoaDon.ChiTietHds.Sum(ct => ct.SoLuong * ct.DonGia * (1 - ct.GiamGia));

                var paymentRequest = new VnPaymentRequestModel
                {
                    Amount = tongTien,
                    OrderId = hoaDon.MaHd,
                    CreatedDate = DateTime.Now
                };

                var paymentUrl = _vnpayService.CreatePaymentUrl(HttpContext, paymentRequest);
                return Redirect(paymentUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating VNPay payment request.");
                ViewBag.Message = "Lỗi tạo yêu cầu thanh toán: " + ex.Message;
                return View("MomoFail");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CodPayment(string selectedAddress, string ngayGiao, double PhiVanChuyen, List<CartItem> CartItems)
        {
            try
            {
                var userId = User.FindFirstValue("UserId");
                var userProfile = _ctx.KhachHangs.FirstOrDefault(u => u.MaKh == userId);

                if (userProfile == null)
                {
                    _logger.LogWarning("Không tìm thấy khách hàng với MaKh: " + userId);
                    return BadRequest("Không tìm thấy thông tin khách hàng.");
                }

                DateTime? giaoDate = null;
                if (DateTime.TryParse(ngayGiao, out DateTime parsed))
                {
                    giaoDate = parsed;
                }

                var hoaDon = new HoaDon
                {
                    MaKh = userId,
                    NgayDat = DateTime.Now,
                    HoTen = User.Identity?.Name ?? userProfile.HoTen,
                    DiaChi = selectedAddress,
                    CachThanhToan = "COD",
                    CachVanChuyen = "N/A",
                    MaTrangThai = 0,
                    PhiVanChuyen = 0,
                    NgayGiao = giaoDate,
                    GhiChu = "Thanh toán khi nhận hàng"
                };
                _ctx.Add(hoaDon);
                _ctx.SaveChanges();

                foreach (var item in CartItems)
                {
                    var cthd = new ChiTietHd
                    {
                        MaHd = hoaDon.MaHd,
                        MaHh = item.MaHh,
                        DonGia = item.DonGia,
                        SoLuong = item.SoLuong,
                        GiamGia = item.GiamGia
                    };
                    _ctx.Add(cthd);

                    // Deduct stock from HangHoa
                    var hangHoa = await _ctx.HangHoas.FirstOrDefaultAsync(hh => hh.MaHh == cthd.MaHh);
                    if (hangHoa != null && hangHoa.SoLuong >= cthd.SoLuong)
                    {
                        hangHoa.SoLuong -= cthd.SoLuong;
                        _ctx.Update(hangHoa);
                    }
                    else
                    {
                        _logger.LogError($"Hàng hóa {cthd.MaHh} không đủ tồn kho hoặc không tồn tại.");
                        throw new Exception($"Hàng hóa {cthd.MaHh} không đủ tồn kho hoặc không tồn tại.");
                    }
                }
                _ctx.SaveChanges();

                await CongDiemChoKhachHangAsync(hoaDon.MaHd);
                _logger.LogInformation("Cộng điểm cho khách hàng với MaHd={maHd}", hoaDon.MaHd);

                try
                {
                    string customerEmail = userProfile.Email ?? "Không rõ";
                    string userName = userProfile.HoTen ?? hoaDon.HoTen;
                    string phone = userProfile.DienThoai ?? "Không rõ";
                    string adminEmail = _configuration["EmailSettings:AdminEmail"] ?? "phannguyendangkhoa0915@gmail.com";

                    await SendCustomerEmail(customerEmail, hoaDon, phone, userName);
                    await SendAdminEmail(adminEmail, hoaDon, phone, userName, customerEmail);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi gửi email COD");
                }

                HttpContext.Session.Set(CART_KEY, new List<CartItem>());
                return RedirectToAction("Success");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý thanh toán COD");
                ViewBag.Message = "Lỗi khi tạo hóa đơn COD: " + ex.GetBaseException().Message;
                return View("MomoFail");
            }
        }

        private async Task CongDiemChoKhachHangAsync(int maHd)
        {
            try
            {
                var chiTietHoaDons = _ctx.ChiTietHds
                    .Where(ct => ct.MaHd == maHd)
                    .ToList();

                double tongTien = chiTietHoaDons.Sum(ct => ct.DonGia * ct.SoLuong);
                int diemCong = (int)(tongTien / 60000);

                var hoaDon = _ctx.HoaDons.FirstOrDefault(h => h.MaHd == maHd);
                if (hoaDon == null)
                {
                    _logger.LogWarning("Không tìm thấy hóa đơn với mã {maHd} để cộng điểm", maHd);
                    return;
                }

                var khachHang = _ctx.KhachHangs.FirstOrDefault(kh => kh.MaKh == hoaDon.MaKh);
                if (khachHang != null)
                {
                    khachHang.Diem += diemCong;
                    khachHang.MuaHangLanCuoi = DateTime.Now;

                    await _ctx.SaveChangesAsync();
                    _logger.LogInformation("Đã cộng {Diem} điểm cho khách hàng {MaKh}", diemCong, khachHang.MaKh);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cộng điểm cho khách hàng với MaHd={maHd}", maHd);
            }
        }

        private async Task SendCustomerEmail(string email, HoaDon order, string phone, string userName)
        {
            if (string.IsNullOrEmpty(email)) return;

            string smtpServer = _configuration["EmailSettings:SmtpServer"];
            int smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);
            string senderEmail = _configuration["EmailSettings:SenderEmail"];
            string senderPassword = _configuration["EmailSettings:SenderPassword"];

            string orderDateFormatted = order.NgayDat.ToString("dd/MM/yyyy HH:mm");
            string formattedAmount = _ctx.ChiTietHds
                .Where(ct => ct.MaHd == order.MaHd)
                .Sum(ct => ct.SoLuong * ct.DonGia * (1 - ct.GiamGia))
                .ToString("N0") + " VNĐ";

            using var smtpClient = new SmtpClient(smtpServer)
            {
                Port = smtpPort,
                Credentials = new NetworkCredential(senderEmail, senderPassword),
                EnableSsl = true,
            };
            string filteredNote = "Không có";
            if (!string.IsNullOrEmpty(order.GhiChu))
            {
                if (order.GhiChu == "Đang chờ thanh toán")
                {
                    filteredNote = order.GhiChu;
                }
                else if (order.GhiChu.StartsWith("Thanh toán thành công"))
                {
                    filteredNote = "Thanh toán thành công";
                }
                else
                {
                    filteredNote = order.GhiChu;
                }
            }

            string subject = $"Xác nhận đơn hàng  #{order.MaHd} - SIDERGIN";
            string body = $@"
            <!DOCTYPE html>
            <html lang='vi'>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Xác nhận đơn hàng</title>
                <style>
                    body {{
                        font-family: 'Segoe UI', Tahoma, Arial, sans-serif;
                        margin: 0;
                        padding: 0;
                        background-color: #f5f5f5;
                        color: #333;
                    }}
                    .container {{
                        max-width: 600px;
                        margin: 30px auto;
                        background-color: #fff;
                        border-radius: 8px;
                        overflow: hidden;
                        box-shadow: 0 4px 10px rgba(0,0,0,0.05);
                    }}
                    .header {{
                        background: linear-gradient(135deg, #6e5ff8 0%, #7d4de3 100%);
                        color: white;
                        padding: 20px;
                        text-align: center;
                    }}
                    .header h1 {{
                        margin: 0;
                        font-size: 26px;
                    }}
                    .content {{
                        padding: 30px 20px;
                    }}
                    .content p {{
                        line-height: 1.6;
                    }}
                    .highlight {{
                        font-weight: bold;
                        color: #6e5ff8;
                    }}
                    .order-info {{
                        background-color: #f9f9f9;
                        padding: 20px;
                        border-radius: 6px;
                        margin-top: 15px;
                    }}
                    .order-info p {{
                        margin: 8px 0;
                    }}
                    .btn {{
                        display: inline-block;
                        padding: 10px 25px;
                        background: linear-gradient(135deg, #6e5ff8 0%, #7d4de3 100%);
                        color: white;
                        text-decoration: none;
                        border-radius: 50px;
                        margin-top: 20px;
                    }}
                    .footer {{
                        text-align: center;
                        padding: 20px;
                        font-size: 13px;
                        color: #777;
                        border-top: 1px solid #eee;
                    }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>ĐƠN HÀNG CỦA BẠN ĐÃ ĐƯỢC XÁC NHẬN</h1>
                    </div>
                    <div class='content'>
                        <p>Xin chào <span class='highlight'>{userName}</span>,</p>
                        <p>Cảm ơn bạn đã đặt hàng tại <strong>SIDERGIN</strong>. Chúng tôi đã nhận được đơn hàng của bạn và đang tiến hành xử lý.</p>

                        <div class='order-info'>
                            <p><strong>🧾 Mã đơn hàng:</strong> #{order.MaHd}</p>
                            <p><strong>📅 Ngày đặt:</strong> {orderDateFormatted}</p>
                            <p><strong>📦 Tổng số lượng sản phẩm:</strong> {_ctx.ChiTietHds.Where(ct => ct.MaHd == order.MaHd).Sum(ct => ct.SoLuong)}</p>
                            <p><strong>💰 Tổng tiền:</strong> {formattedAmount}</p>
                            <p><strong>💳 Thanh toán:</strong> {order.CachThanhToan}</p>
                            <p><strong>🏠 Địa chỉ giao hàng:</strong> {order.DiaChi}</p>
                            
                            <p><strong>📝 Ghi chú:</strong> {filteredNote}</p>
                        </div>

                        <p>Chúng tôi sẽ sớm liên hệ để xác nhận và tiến hành giao hàng.</p>
                        <a href='https://sidergin.com/orders/track/{order.MaHd}' class='btn'>Theo dõi đơn hàng</a>
                    </div>
                    <div class='footer'>
                        © 2025 SIDERGIN - Cảm ơn bạn đã mua sắm cùng chúng tôi!<br/>
                        Mọi thắc mắc vui lòng liên hệ: support@sidergin.com | 📞 0123 456 789
                    </div>
                </div>
            </body>
            </html>";

            var mailMessage = new MailMessage(senderEmail, email, subject, body) { IsBodyHtml = true };

            try
            {
                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation($"Đã gửi email đến {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Gửi mail thất bại tới {email}: {ex.Message}");
            }
        }

        private async Task SendAdminEmail(string email, HoaDon order, string phone, string userName, string customerEmail)
        {
            if (string.IsNullOrEmpty(email)) return;

            string smtpServer = _configuration["EmailSettings:SmtpServer"];
            int smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);
            string senderEmail = _configuration["EmailSettings:SenderEmail"];
            string senderPassword = _configuration["EmailSettings:SenderPassword"];

            string formattedAmount = _ctx.ChiTietHds
                .Where(ct => ct.MaHd == order.MaHd)
                .Sum(ct => ct.SoLuong * ct.DonGia * (1 - ct.GiamGia))
                .ToString("N0") + " VNĐ";

            using var smtpClient = new SmtpClient(smtpServer)
            {
                Port = smtpPort,
                Credentials = new NetworkCredential(senderEmail, senderPassword),
                EnableSsl = true,
            };
            string filteredNote = "Không có";
            if (!string.IsNullOrEmpty(order.GhiChu))
            {
                if (order.GhiChu == "Đang chờ thanh toán")
                {
                    filteredNote = order.GhiChu;
                }
                else if (order.GhiChu.StartsWith("Thanh toán thành công"))
                {
                    filteredNote = "Thanh toán thành công";
                }
                else
                {
                    filteredNote = order.GhiChu;
                }
            }

            string subject = $"📦 [SIDERGIN] Thông báo đơn hàng mới #{order.MaHd}";

            string body = $@"
                <h2>📢 Thông báo đơn hàng mới</h2>
                <p>Xin chào Admin,</p>
                <p>Một đơn hàng mới đã được khách hàng đặt thành công.</p>
                <hr>
                <p><strong>🧾 Mã đơn hàng:</strong> {order.MaHd}</p>
                <p><strong>👤 Họ tên khách hàng:</strong> {userName}</p>
                <p><strong>📧 Email:</strong> {customerEmail ?? "Không có"}</p>
                <p><strong>📞 Số điện thoại:</strong> {phone}</p>
                <p><strong>📦 Tổng số lượng sản phẩm:</strong> {_ctx.ChiTietHds.Where(ct => ct.MaHd == order.MaHd).Sum(ct => ct.SoLuong)}</p>
                <p><strong>💰 Tổng tiền:</strong> {formattedAmount}</p>
                <p><strong>🏠 Địa chỉ giao hàng:</undry> {order.DiaChi}</p>
                
                <p><strong>💳 Phương thức thanh toán:</strong> {order.CachThanhToan}</p>
                <p><strong>📝 Ghi chú:</strong> {filteredNote}</p>
                <hr>
                <p>📞 Vui lòng liên hệ với khách hàng để xác nhận đơn hàng sớm nhất có thể.</p>
                <p>Trân trọng,</p>
                <p><strong>Hệ thống quản lý đơn hàng - SIDERGIN</strong></p>";
            var mailMessage = new MailMessage(senderEmail, email, subject, body) { IsBodyHtml = true };

            try
            {
                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation($"Đã gửi email đến {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Gửi mail thất bại tới {email}: {ex.Message}");
            }
        }
        [HttpGet]
        public IActionResult PrintInvoice(double shippingFee)
        {
            var maKhachHang = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
            KhachHang khachHang = null;
            if (maKhachHang != null)
            {
                khachHang = _ctx.KhachHangs.FirstOrDefault(k => k.MaKh == maKhachHang);
            }

            // Lấy thông tin giỏ hàng từ session
            var cartItems = HttpContext.Session.Get<List<CartItem>>("MY_CART");

            // Tạo một đối tượng HoaDon hoặc ViewModel để chứa dữ liệu cần thiết
            var hoaDon = new HoaDon
            {
                // Các thông tin chung của hóa đơn
                MaKh = maKhachHang,
                NgayDat = DateTime.Now,
                HoTen = User.Identity.Name,
                DiaChi = khachHang?.DiaChi,
                CachThanhToan = "",
                ChiTietHds = cartItems.Select(item => new ChiTietHd
                {
                    MaHh = item.MaHh,
                    DonGia = item.DonGia,
                    SoLuong = item.SoLuong,
                    GiamGia = item.GiamGia,
                    MaHhNavigation = _ctx.HangHoas.FirstOrDefault(hh => hh.MaHh == item.MaHh)
                }).ToList(),
                PhiVanChuyen = 0 // Assign the shipping fee to the HoaDon object
            };

            ViewBag.HoaDon = hoaDon; // Use ViewBag to pass HoaDon to the view
            ViewBag.KhachHang = khachHang;
            return View(hoaDon);
        }
    }
}