using AutoMapper;
using Du_An_Ecomerce.Data;
using Du_An_Ecomerce.Helpers;
using Du_An_Ecomerce.ViewModel;
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
    }
}