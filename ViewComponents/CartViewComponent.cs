using Du_An_Ecomerce.Data;
using Du_An_Ecomerce.Helpers;
using Du_An_Ecomerce.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Du_An_Ecomerce.ViewComponents
{
    public class CartViewComponent : ViewComponent
    {
        private readonly ILogger<CartViewComponent> _logger;
        private readonly Hshop2023Context _dbContext;
        public CartViewComponent(ILogger<CartViewComponent> logger, Hshop2023Context context)
        {
            _logger = logger;
            _dbContext = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var CartItems = HttpContext.Session.GetObjectFromJson<List<CartItemViewModel>>(MyConstaint.CartSession) ?? new List<CartItemViewModel>();
            return View(CartItems);
        }
    }
}
