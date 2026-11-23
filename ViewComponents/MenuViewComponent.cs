using Du_An_Ecomerce.Data;
using Du_An_Ecomerce.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Du_An_Ecomerce.ViewComponents
{
    public class MenuViewComponent : ViewComponent
    {
        private readonly ILogger<MenuViewComponent> _logger;
        private readonly Hshop2023Context _dbContext;
        public MenuViewComponent(ILogger<MenuViewComponent> logger, Hshop2023Context context)
        {
            _logger = logger;
            _dbContext = context;
        }
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var data = await _dbContext.Loais.Select(l => new MenuViewModel
            {
                MaLoai = l.MaLoai,
                TenLoai = l.TenLoai,
                SoLuong = _dbContext.HangHoas.Count(h => h.MaLoai == l.MaLoai)
            }).OrderBy(l => l.TenLoai).ToListAsync();

            return View(data);
        }
    }
}