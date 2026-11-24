using System.Security.Claims;
using AutoMapper;
using Du_An_Ecomerce.Data;
using Du_An_Ecomerce.Helpers;
using Du_An_Ecomerce.ViewModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Du_An_Ecomerce.Controllers
{
    public class KhachHangController : Controller
    {
        private readonly ILogger<KhachHangController> _logger;
        private readonly Hshop2023Context _dbContext;
        private readonly IMapper _mapper;

        public KhachHangController(ILogger<KhachHangController> logger, Hshop2023Context dbContext, IMapper mapper)
        {
            _logger = logger;
            _dbContext = dbContext;
            _mapper = mapper;
        }

        [HttpGet]
        public IActionResult DangKy()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> DangKy(RegisterViewModel model, IFormFile? Hinh)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra xem tên đăng nhập đã tồn tại hay chưa
                var existingUser = _dbContext.KhachHangs.FirstOrDefault(u => u.MaKh == model.MaKH);
                if (existingUser != null)
                {
                    ModelState.AddModelError("MaKH", "Tên đăng nhập đã tồn tại");
                    return View(model);
                }

                // Tạo đối tượng KhachHang từ dữ liệu trong model
                var khachHang = _mapper.Map<KhachHang>(model);
                khachHang.RandomKey = Utils.GenerateRandomKey(10); // Sử dụng phương thức GenerateRandomKey từ Utils.cs để tạo random key
                khachHang.MatKhau = model.MatKhau.ToMd5Hash(khachHang.RandomKey); // Mã hóa mật khẩu bằng MD5 với random key
                khachHang.HieuLuc = true; // Mặc định là hiệu lực
                khachHang.VaiTro = 0; // Mặc định là vai trò User

                // Xử lý tải lên hình ảnh nếu có
                if (Hinh != null && Hinh.Length > 0)
                {
                    // Sử dụng phương thức UploadFile từ Utils.cs để tải lên hình ảnh
                    var uploadedFileName = Utils.UploadFile(Hinh, "KhachHang");
                    if (!string.IsNullOrEmpty(uploadedFileName))
                    {
                        khachHang.Hinh = uploadedFileName; // Lưu tên tệp đã tải lên vào thuộc tính Hinh
                    }
                }

                // Lưu thông tin khách hàng vào cơ sở dữ liệu
                _dbContext.KhachHangs.Add(khachHang);
                _dbContext.SaveChanges();

                // Chuyển hướng đến trang đăng nhập hoặc trang thành công
                return RedirectToAction("DangNhap", "KhachHang");
            }

            // Nếu có lỗi, trả về view với thông tin lỗi
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> DangNhap(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl; // Lưu trữ URL trả về nếu có
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> DangNhap(LoginViewModel model, string? returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                // Tìm khách hàng theo tên đăng nhập
                var khachHang = _dbContext.KhachHangs.FirstOrDefault(u => u.MaKh == model.Username);
                if (khachHang != null)
                {
                    // Kiểm tra mật khẩu
                    var hashedPassword = model.Password.ToMd5Hash(khachHang.RandomKey);
                    if (hashedPassword == khachHang.MatKhau)
                    {
                        if (!khachHang.HieuLuc)
                        {
                            ModelState.AddModelError(string.Empty, "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.");
                            return View(model);
                        }

                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Email, khachHang.Email),
                            new Claim(ClaimTypes.NameIdentifier, khachHang.MaKh),
                            new Claim(ClaimTypes.Name, khachHang.HoTen),
                            new Claim(ClaimTypes.Role, khachHang.VaiTro.ToString())
                        };

                        var claimsIdentity = new ClaimsIdentity(claims, "Login");
                        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                        await HttpContext.SignInAsync(claimsPrincipal);

                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        {
                            return Redirect(returnUrl);
                        }
                        else
                        {
                            return RedirectToAction("Index", "Home");
                        }
                    }
                }

                // Nếu tên đăng nhập hoặc mật khẩu không đúng, hiển thị thông báo lỗi
                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng");
            }

            return View(model);
        }

        [Authorize]
        public async Task<IActionResult> DangXuat()
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("DangNhap", "KhachHang");
            }

            var khachHang = _dbContext.KhachHangs.FirstOrDefault(u => u.MaKh == username);
            if (khachHang == null)
            {
                return RedirectToAction("DangNhap", "KhachHang");
            }

            var model = new ProfileViewModel
            {
                MaKH = khachHang.MaKh,
                HoTen = khachHang.HoTen,
                Email = khachHang.Email,
                SDT = khachHang.DienThoai ?? "",
                DiaChi = khachHang.DiaChi ?? "",
                Hinh = khachHang.Hinh ?? null // Không cần truyền hình ảnh ở đây, vì nó sẽ được xử lý riêng
            };

            return View(model);
        }
    }
}