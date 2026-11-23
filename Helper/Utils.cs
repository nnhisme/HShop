namespace Du_An_Ecomerce.Helpers
{
    public static class Utils
    {
        public static string GenerateRandomKey(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static string UploadFile(IFormFile file, string folder)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return null; // Không có tệp được tải lên
                }

                // Tạo tên tệp duy nhất để tránh trùng lặp
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;

                // Xác định đường dẫn lưu trữ tệp
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Hinh", folder);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Lưu tệp vào thư mục
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(fileStream);
                }

                return uniqueFileName; // Trả về tên tệp đã lưu
            }
            catch (Exception ex)
            {
                // Xử lý lỗi nếu cần
                return string.Empty; // Hoặc trả về null hoặc thông báo lỗi tùy theo yêu cầu của bạn
            }
        }
    }
}