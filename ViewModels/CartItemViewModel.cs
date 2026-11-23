namespace Du_An_Ecomerce.ViewModel
{
    public class CartItemViewModel
    {
        public int MaSanPham { get; set; }

        public string TenSanPham { get; set; }

        public double? GiaBan { get; set; }

        public string HinhAnh { get; set; }

        public int SoLuong { get; set; }

        public double ThanhTien => GiaBan.GetValueOrDefault() * SoLuong;
    }
}