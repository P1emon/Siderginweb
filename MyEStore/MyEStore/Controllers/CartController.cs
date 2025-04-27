using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyEStore.Entities;
using MyEStore.Models;
using System.Collections.Generic;
using System.Linq;

namespace MyEStore.Controllers
{
    public class CartController : Controller
    {
        const string CART_KEY = "MY_CART";
        private readonly MyeStoreContext _ctx;

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

        public CartController(MyeStoreContext ctx)
        {
            _ctx = ctx;
        }

        public IActionResult Index()
        {
            return View(CartItems);
        }

        [HttpGet]
        public IActionResult GetCartCount()
        {
            var cartCount = CartItems.Sum(item => item.SoLuong);
            return Json(cartCount);
        }

        public IActionResult AddToCart(int id, int qty = 1)
        {
            var hangHoa = _ctx.HangHoas.SingleOrDefault(h => h.MaHh == id);
            if (hangHoa == null)
            {
                TempData["ThongBao"] = $"Không tìm thấy hàng hóa có mã {id}";
                return RedirectToAction("Index", "Products");
            }

            // Check available stock
            if (hangHoa.SoLuong < qty)
            {
                TempData["ThongBao"] = $"Sản phẩm {hangHoa.TenHh} chỉ còn {hangHoa.SoLuong} đơn vị trong kho.";
                return RedirectToAction("Index", "Products");
            }

            var cart = CartItems;
            var cartItem = cart.SingleOrDefault(p => p.MaHh == id);
            if (cartItem != null)
            {
                // Check if increasing quantity exceeds stock
                if (hangHoa.SoLuong < cartItem.SoLuong + qty)
                {
                    TempData["ThongBao"] = $"Sản phẩm {hangHoa.TenHh} chỉ còn {hangHoa.SoLuong} đơn vị trong kho.";
                    return RedirectToAction("Index");
                }
                cartItem.SoLuong += qty;
            }
            else
            {
                cartItem = new CartItem
                {
                    MaHh = id,
                    SoLuong = qty,
                    TenHh = hangHoa.TenHh,
                    Hinh = hangHoa.Hinh,
                    DonGia = hangHoa.DonGia ?? 0,
                    GiamGia = hangHoa.GiamGia
                };
                cart.Add(cartItem);
            }

            HttpContext.Session.Set(CART_KEY, cart);
            return RedirectToAction("Index");
        }

        public IActionResult RemoveCartItem(int id)
        {
            var cart = CartItems;
            var cartItem = cart.SingleOrDefault(p => p.MaHh == id);
            if (cartItem != null)
            {
                cart.Remove(cartItem);
                HttpContext.Session.Set(CART_KEY, cart);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult UpdateQuantity([FromBody] UpdateQuantityRequest request)
        {
            var cart = CartItems;
            var cartItem = cart.SingleOrDefault(p => p.MaHh == request.Id);
            if (cartItem == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại trong giỏ hàng." });
            }

            var hangHoa = _ctx.HangHoas.SingleOrDefault(h => h.MaHh == request.Id);
            if (hangHoa == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại." });
            }

            // Check if requested quantity exceeds stock
            if (request.Qty > hangHoa.SoLuong)
            {
                return Json(new
                {
                    success = false,
                    message = $"Sản phẩm {hangHoa.TenHh} chỉ còn {hangHoa.SoLuong} đơn vị trong kho."
                });
            }

            // Update quantity
            cartItem.SoLuong = request.Qty;
            HttpContext.Session.Set(CART_KEY, cart);

            var itemTotalPrice = cartItem.ThanhTien;
            var cartTotal = cart.Sum(item => item.ThanhTien);

            return Json(new
            {
                success = true,
                newItemTotalPrice = itemTotalPrice,
                cartTotal = cartTotal
            });
        }

        [HttpPost]
        public IActionResult CheckStockBeforeCheckout()
        {
            var cart = CartItems;
            if (!cart.Any())
            {
                return Json(new
                {
                    
                    success = false,
                    message = "Giỏ hàng của bạn đang trống."
                });
            }

            var outOfStockItems = new List<string>();
            foreach (var cartItem in cart)
            {
                var hangHoa = _ctx.HangHoas.SingleOrDefault(h => h.MaHh == cartItem.MaHh);
                if (hangHoa == null)
                {
                    outOfStockItems.Add($"Sản phẩm {cartItem.TenHh} không còn tồn tại.");
                }
                else if (hangHoa.SoLuong < cartItem.SoLuong)
                {
                    outOfStockItems.Add($"Sản phẩm {cartItem.TenHh} chỉ còn {hangHoa.SoLuong} đơn vị trong kho, nhưng bạn đã chọn {cartItem.SoLuong}.");
                }
            }

            if (outOfStockItems.Any())
            {
                // Join messages with <br> for HTML display in SweetAlert
                var message = string.Join("<br>", outOfStockItems);
                return Json(new { success = false, message });
            }

            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult ReorderItems(int orderId)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
            if (userId == null)
            {
                return RedirectToAction("Login", "Customer");
            }

            var orderDetails = _ctx.ChiTietHds
                .Where(ct => ct.MaHd == orderId)
                .Include(ct => ct.MaHhNavigation)
                .ToList();

            if (orderDetails == null || !orderDetails.Any())
            {
                TempData["ThongBao"] = "Không tìm thấy thông tin đơn hàng để mua lại.";
                return RedirectToAction("TransactionHistory", "Customer");
            }

            var cart = CartItems;
            foreach (var item in orderDetails)
            {
                var product = item.MaHhNavigation;
                if (product == null)
                {
                    TempData["ThongBao"] = $"Sản phẩm mã {item.MaHh} không còn tồn tại.";
                    continue;
                }

                // Check stock availability
                if (item.SoLuong > product.SoLuong)
                {
                    TempData["ThongBao"] = $"Sản phẩm {product.TenHh} chỉ còn {product.SoLuong} đơn vị trong kho.";
                    continue;
                }

                var existingItem = cart.SingleOrDefault(c => c.MaHh == item.MaHh);
                if (existingItem != null)
                {
                    if (existingItem.SoLuong + item.SoLuong > product.SoLuong)
                    {
                        TempData["ThongBao"] = $"Sản phẩm {product.TenHh} chỉ còn {product.SoLuong} đơn vị trong kho.";
                        continue;
                    }
                    existingItem.SoLuong += item.SoLuong;
                }
                else
                {
                    var cartItem = new CartItem
                    {
                        MaHh = item.MaHh,
                        TenHh = product.TenHh,
                        DonGia = product.DonGia ?? 0,
                        GiamGia = product.GiamGia,
                        SoLuong = item.SoLuong,
                        Hinh = product.Hinh
                    };
                    cart.Add(cartItem);
                }
            }

            HttpContext.Session.Set(CART_KEY, cart);
            TempData["ThongBao"] = "Đã thêm sản phẩm vào giỏ hàng thành công!";
            return RedirectToAction("Index", "Cart");
        }

        public class UpdateQuantityRequest
        {
            public int Id { get; set; }
            public int Qty { get; set; }
        }

        [Authorize]
        public IActionResult PaymentCallBack()
        {
            return View();
        }
    }
}