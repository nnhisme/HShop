using Du_An_Ecomerce.Data;
using Du_An_Ecomerce.Helpers;
using Du_An_Ecomerce.ViewModel;
using Microsoft.AspNetCore.Mvc;

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

        public CartController(ILogger<CartController> logger, Hshop2023Context dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
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
    }
}