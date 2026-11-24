namespace Du_An_Ecomerce.ViewModel
{
    public class PaypalPaymentRequest
    {
        public string OrderId { get; set; }
        public decimal Amount { get; set; }
        public CustomerInfo Customer { get; set; }
        public string? GhiChu { get; set; }

    }

    public class CustomerInfo
    {
        public string HoTen { get; set; }
        public string DiaChi { get; set; }
        public string DienThoai { get; set; }
    }
}