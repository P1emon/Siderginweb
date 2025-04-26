using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyEStore.Entities;
using MyEStore.Models;

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
            var cart = CartItems;
            var cartItem = cart.SingleOrDefault(p => p.MaHh == id);
            if (cartItem != null)
            {
                cartItem.SoLuong += qty;
            }
            else
            {
                var hangHoa = _ctx.HangHoas.SingleOrDefault(h => h.MaHh == id);
                if (hangHoa == null)
                {
                    TempData["ThongBao"] = $"Không tìm thấy hàng hóa có mã {id}";
                    return RedirectToAction("Index", "Products");
                }
                cartItem = new CartItem
                {
                    MaHh = id,
                    SoLuong = qty,
                    TenHh = hangHoa.TenHh,
                    Hinh = hangHoa.Hinh,
                    DonGia = hangHoa.DonGia ?? 0,
                    GiamGia = hangHoa.GiamGia // Store the discount
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

            if (cartItem != null)
            {
                cartItem.SoLuong = request.Qty; // Update the quantity
                HttpContext.Session.Set(CART_KEY, cart);
            }

            var itemTotalPrice = cartItem?.ThanhTien ?? 0; // Uses discounted price
            var cartTotal = cart.Sum(item => item.ThanhTien); // Uses discounted price

            return Json(new
            {
                newItemTotalPrice = itemTotalPrice,
                cartTotal = cartTotal
            });
        }


        [HttpPost]
        public IActionResult ReorderItems(int orderId)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
            if (userId == null)
            {
                return RedirectToAction("Login", "Customer");
            }

            // Lấy chi tiết đơn hàng cần mua lại
            var orderDetails = _ctx.ChiTietHds
                .Where(ct => ct.MaHd == orderId)
                .Include(ct => ct.MaHhNavigation)
                .ToList();

            if (orderDetails == null || !orderDetails.Any())
            {
                TempData["ThongBao"] = "Không tìm thấy thông tin đơn hàng để mua lại.";
                return RedirectToAction("TransactionHistory", "Customer");
            }

            // Lấy giỏ hàng hiện tại
            var cart = CartItems;

            // Thêm từng sản phẩm vào giỏ hàng
            foreach (var item in orderDetails)
            {
                // Kiểm tra nếu sản phẩm tồn tại trong giỏ hàng
                var existingItem = cart.SingleOrDefault(c => c.MaHh == item.MaHh);

                if (existingItem != null)
                {
                    // Nếu sản phẩm đã có trong giỏ hàng, tăng số lượng
                    existingItem.SoLuong += item.SoLuong;
                }
                else
                {
                    // Nếu sản phẩm chưa có trong giỏ hàng, thêm mới
                    var product = item.MaHhNavigation;
                    if (product != null)
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
            }

            // Lưu giỏ hàng vào session
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