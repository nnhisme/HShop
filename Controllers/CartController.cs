using Du_An_Ecomerce.Data;
using Du_An_Ecomerce.Helpers;
using Du_An_Ecomerce.Services;
using Du_An_Ecomerce.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Du_An_Ecomerce.Controllers
{
    public class CartController : Controller
    {
        private readonly ILogger<CartController> _logger;
        private readonly Hshop2023Context _dbContext;
        public List<CartItemViewModel> CartItems
        {
            get
            {
                var cartItems = HttpContext.Session.GetObjectFromJson<List<CartItemViewModel>>(MyConstaint.CartSession);
                if (cartItems == null)
                {
                    cartItems = new List<CartItemViewModel>();
                    HttpContext.Session.SetObjectAsJson(MyConstaint.CartSession, cartItems);
                }
                return cartItems;
            }
            set
            {
                HttpContext.Session.SetObjectAsJson(MyConstaint.CartSession, value);
            }
        }

        private readonly PaypalClient _paypalClient;
        private readonly IVnPayService _vnPayService;

        public CartController(ILogger<CartController> logger, Hshop2023Context dbContext, PaypalClient paypalClient, IVnPayService vnPayService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _paypalClient = paypalClient;
            _vnPayService = vnPayService;
        }

        public IActionResult Index()
        {
            return View(CartItems);
        }

        public IActionResult AddToCart(int id, int quantity = 1)
        {
            var product = _dbContext.HangHoas.FirstOrDefault(p => p.MaHh == id);
            if (product != null)
            {
                var existingCartItem = CartItems.FirstOrDefault(c => c.MaSanPham == id);
                if (existingCartItem != null)
                {
                    existingCartItem.SoLuong += quantity;
                }
                else
                {
                    CartItems.Add(new CartItemViewModel
                    {
                        MaSanPham = product.MaHh,
                        TenSanPham = product.TenHh,
                        GiaBan = product.DonGia ?? 0,
                        HinhAnh = product.Hinh ?? "",
                        SoLuong = quantity
                    });
                }
            }
            else
            {
                return Redirect("/404");
            }

            HttpContext.Session.SetObjectAsJson(MyConstaint.CartSession, CartItems);
            return View("Index", CartItems);
        }

        public IActionResult RemoveCart(int id)
        {
            var cartItem = CartItems.FirstOrDefault(c => c.MaSanPham == id);
            if (cartItem != null)
            {
                CartItems.Remove(cartItem);
                HttpContext.Session.SetObjectAsJson(MyConstaint.CartSession, CartItems);
            }
            return RedirectToAction("Index");
        }

        [Authorize]
        public async Task<IActionResult> Checkout()
        {
            var cartItems = CartItems;
            if (cartItems.Count == 0)
            {
                return RedirectToAction("Index");
            }

            ViewBag.PaypalClientId = _paypalClient.ClientId;

            return View(CartItems);
        }

        [Authorize]
        [HttpPost]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Checkout(
    CheckoutViewModel model,
    string payment = PaymentType.CODPayment)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var cartItems = CartItems;

            if (cartItems.Count == 0)
            {
                return RedirectToAction("Index");
            }

            var customerId = HttpContext.User.Claims
                .SingleOrDefault(c => c.Type == "UserId");

            if (customerId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                if (model.GiongKhachHang)
                {
                    var customer = await _dbContext.KhachHangs.FirstOrDefaultAsync(c => c.MaKh == customerId.Value);

                    if (customer == null)
                    {
                        ModelState.AddModelError("", "Customer not found.");
                        return View(model);
                    }

                    model.HoTen = customer.HoTen;
                    model.DiaChi = customer.DiaChi;
                    model.DienThoai = customer.DienThoai;
                }

                decimal totalAmount = (decimal)cartItems.Sum(item =>
                    (item.GiaBan ?? 0) * item.SoLuong);

                bool isVnPay = payment == PaymentType.VnPayPayment;

                var order = new HoaDon
                {
                    MaKh = customerId.Value,
                    NgayDat = DateTime.Now,
                    HoTen = model.HoTen,
                    DiaChi = model.DiaChi,
                    DienThoai = model.DienThoai,
                    GhiChu = model.GhiChu ?? "",
                    CachThanhToan = isVnPay ? "VNPay" : "COD",
                    CachVanChuyen = "Standard",
                    PhiVanChuyen = 0,
                    MaTrangThai = 1
                };

                // Lưu đơn hàng và chi tiết trong cùng transaction
                await using (var transaction =
                    await _dbContext.Database.BeginTransactionAsync())
                {
                    _dbContext.HoaDons.Add(order);
                    await _dbContext.SaveChangesAsync();

                    var orderDetails = cartItems.Select(item => new ChiTietHd
                    {
                        MaHd = order.MaHd,
                        MaHh = item.MaSanPham,
                        SoLuong = item.SoLuong,
                        DonGia = item.GiaBan ?? 0
                    }).ToList();

                    _dbContext.ChiTietHds.AddRange(orderDetails);
                    await _dbContext.SaveChangesAsync();

                    await transaction.CommitAsync();
                }

                // VNPay: Chuyển sang cổng thanh toán
                if (isVnPay)
                {
                    var vnPayRequest = new VnPaymentRequestModel
                    {
                        OrderId = order.MaHd.ToString(),
                        Amount = totalAmount,
                        OrderDescription = $"Payment for order {order.MaHd}"
                    };

                    var paymentUrl = _vnPayService.CreatePaymentUrl(vnPayRequest);

                    return Redirect(paymentUrl);
                }

                // COD: Hoàn tất đặt hàng
                CartItems.Clear();

                HttpContext.Session.SetObjectAsJson(
                    MyConstaint.CartSession, CartItems);

                return RedirectToAction("Success");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during checkout process.");

                ModelState.AddModelError(
                    string.Empty,
                    "An error occurred while processing your order. Please try again.");

                return View(model);
            }
        }

        public IActionResult Success()
        {
            return View();
        }

        public async Task<IActionResult> GetDefaultCustomerInfo()
        {
            var customerId = HttpContext.User.Claims.SingleOrDefault(c => c.Type == "UserId");
            if (customerId == null)
            {
                return Json(new { success = false, message = "User not logged in." });
            }

            var customer = await _dbContext.KhachHangs.FindAsync(customerId.Value);
            if (customer == null)
            {
                return Json(new { success = false, message = "Customer not found." });
            }

            var customerInfo = new
            {
                HoTen = customer.HoTen,
                DiaChi = customer.DiaChi,
                DienThoai = customer.DienThoai
            };

            return Json(new { success = true, data = customerInfo });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreatePaypalOrder([FromBody] PaypalPaymentRequest request)
        {
            try
            {
                var cartItems = CartItems;
                if (cartItems.Count == 0)
                {
                    return BadRequest(new { success = false, message = "Cart is empty." });
                }

                if (string.IsNullOrEmpty(request.Customer?.HoTen))
                {
                    return BadRequest(new { success = false, message = "Customer information is required." });
                }
                if (string.IsNullOrEmpty(request.Customer?.DiaChi))
                {
                    return BadRequest(new { success = false, message = "Customer address is required." });
                }
                if (string.IsNullOrEmpty(request.Customer?.DienThoai))
                {
                    return BadRequest(new { success = false, message = "Customer phone number is required." });
                }

                // Calculate total amount
                decimal totalAmount = (decimal)cartItems.Sum(item => (item.GiaBan ?? 0) * item.SoLuong);
                string currency = "USD"; // You can change this to your desired currency

                // Create PayPal order
                var paypalOrderResponse = await _paypalClient.CreateOrder(totalAmount.ToString("F2"), currency, Guid.NewGuid().ToString());

                if (paypalOrderResponse == null || string.IsNullOrEmpty(paypalOrderResponse.id))
                {
                    return StatusCode(500, new { success = false, message = "Failed to create PayPal order." });
                }

                return Ok(new { success = true, order = paypalOrderResponse });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PayPal order.");
                return StatusCode(500, new { success = false, message = "An error occurred while creating the PayPal order. Please try again." });
            }
        }

        // ProcessPaypalPayment
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ProcessPaypalPayment([FromBody] PaypalPaymentRequest request)
        {
            _dbContext.Database.BeginTransaction();
            try
            {
                var cartItems = CartItems;
                if (cartItems.Count == 0)
                {
                    return BadRequest(new { success = false, message = "Cart is empty." });
                }

                // Create the order in the database
                var customerId = HttpContext.User.Claims.SingleOrDefault(c => c.Type == "UserId");
                if (customerId == null)
                {
                    return Unauthorized(new { success = false, message = "User not logged in." });
                }

                var order = new HoaDon
                {
                    MaKh = customerId.Value,
                    NgayDat = DateTime.Now,
                    HoTen = request.Customer.HoTen,
                    DiaChi = request.Customer.DiaChi,
                    DienThoai = request.Customer.DienThoai,
                    GhiChu = request.GhiChu ?? "",
                    CachThanhToan = "PayPal",
                    CachVanChuyen = "Standard",
                    PhiVanChuyen = 0,
                    MaTrangThai = 1
                };

                _dbContext.HoaDons.Add(order);
                await _dbContext.SaveChangesAsync();

                var orderDetails = cartItems.Select(item => new ChiTietHd
                {
                    MaHd = order.MaHd,
                    MaHh = item.MaSanPham,
                    SoLuong = item.SoLuong,
                    DonGia = item.GiaBan ?? 0
                }).ToList();

                _dbContext.ChiTietHds.AddRange(orderDetails);
                await _dbContext.SaveChangesAsync();

                _dbContext.Database.CommitTransaction();

                // Clear the cart after successful checkout
                CartItems.Clear();
                HttpContext.Session.SetObjectAsJson(MyConstaint.CartSession, CartItems);

                return Ok(new { success = true, message = "Order placed successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during PayPal payment process.");
                _dbContext.Database.RollbackTransaction();
                return StatusCode(500, new { success = false, message = "An error occurred while processing your order. Please try again." });
            }
        }

        [Authorize]
        [Authorize]
        public async Task<IActionResult> PaymentReturn()
        {
            try
            {
                // Xác thực dữ liệu VNPay trả về
                var vnPayResponse =
                    _vnPayService.ProcessPaymentResponse(Request.Query);

                if (!vnPayResponse.Success)
                {
                    ViewBag.ErrorMessage =
                        $"Payment failed. Response code: {vnPayResponse.VnPayResponseCode}";

                    return View("Error");
                }

                // Tìm đơn hàng theo mã giao dịch
                var order = await _dbContext.HoaDons
                    .FirstOrDefaultAsync(o =>
                        o.MaHd.ToString() == vnPayResponse.OrderId);

                if (order == null)
                {
                    ViewBag.ErrorMessage = "Order not found.";
                    return View("Error");
                }

                // Kiểm tra phương thức thanh toán
                if (order.CachThanhToan != "VNPay")
                {
                    ViewBag.ErrorMessage = "Invalid payment method.";
                    return View("Error");
                }

                // Cập nhật trạng thái đơn hàng
                if (order.MaTrangThai == 1)
                {
                    order.MaTrangThai = 2;

                    await _dbContext.SaveChangesAsync();
                }

                // Xóa giỏ hàng sau khi thanh toán thành công
                CartItems.Clear();

                HttpContext.Session.SetObjectAsJson(
                    MyConstaint.CartSession, CartItems);

                return RedirectToAction("Success");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing VNPay response.");

                ViewBag.ErrorMessage = "An error occurred while processing payment.";

                return View("Error");
            }
        }
    }
}