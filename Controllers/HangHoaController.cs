using Microsoft.AspNetCore.Mvc;

namespace Du_An_Ecomerce.Controllers
{
    public class HangHoaController : Controller
    {
        public IActionResult Index(int? MaLoai)
        {
            return View();
        }
    }
}