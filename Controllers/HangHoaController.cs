using Du_An_Ecomerce.Data;
using Du_An_Ecomerce.ViewComponents;
using Du_An_Ecomerce.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Du_An_Ecomerce.Controllers
{
    public class HangHoaController : Controller
    {
        private readonly ILogger<HangHoaController> _logger;
        private readonly Hshop2023Context _dbContext;
        public HangHoaController(ILogger<HangHoaController> logger, Hshop2023Context dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public IActionResult Index(int? MaLoai)
        {
            if (MaLoai.HasValue)
            {
                var products = _dbContext.HangHoas
                    .Where(p => p.MaLoai == MaLoai.Value)
                    .Select(p => new ProductViewModel
                    {
                        MaSanPham = p.MaHh,
                        TenLoai = p.MaLoaiNavigation.TenLoai,
                        TenSanPham = p.TenHh,
                        GiaBan = p.DonGia ?? 0,
                        HinhAnh = p.Hinh ?? "",
                        MoTaNgan = p.MoTaDonVi ?? ""
                    })
                    .ToList();

                return View(products);
            }
            else
            {
                var products = _dbContext.HangHoas
                    .Select(p => new ProductViewModel
                    {
                        MaSanPham = p.MaHh,
                        TenLoai = p.MaLoaiNavigation.TenLoai,
                        TenSanPham = p.TenHh,
                        GiaBan = p.DonGia,
                        HinhAnh = p.Hinh ?? "",
                        MoTaNgan = p.MoTaDonVi ?? ""
                    })
                    .ToList();

                return View(products);
            }
        }

        public async Task<IActionResult> Search(string query)
        {
            var products = _dbContext.HangHoas
                .Where(p => p.TenHh.Contains(query))
                .Select(p => new ProductViewModel
                {
                    MaSanPham = p.MaHh,
                    TenLoai = p.MaLoaiNavigation.TenLoai,
                    TenSanPham = p.TenHh,
                    GiaBan = p.DonGia,
                    HinhAnh = p.Hinh ?? "",
                    MoTaNgan = p.MoTaDonVi ?? ""
                })
                .ToList();

            return View("Index", products);
        }

        public async Task<IActionResult> Detail(int id)
        {
            var product = await _dbContext.HangHoas
                            .Include(p => p.MaLoaiNavigation)
                            .FirstOrDefaultAsync(p => p.MaHh == id);

            if (product == null)
            {
                TempData["ErrorMessage"] = "Sản phẩm không tồn tại.";
                return RedirectToAction("PageNotFound", "Home");
            }

            var detailProductViewModel = new DetailProductViewProduct
            {
                MaSanPham = product.MaHh,
                TenLoai = product.MaLoaiNavigation.TenLoai,
                TenSanPham = product.TenHh,
                GiaBan = product.DonGia,
                HinhAnh = product.Hinh ?? "",
                MoTaNgan = product.MoTaDonVi ?? "",
                MoTaChiTiet = product.MoTa ?? "",
                SoLuongTon = 10,
                DiemDanhGia = 5
            };

            return View(detailProductViewModel);
        }
    }
}