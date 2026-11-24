using Du_An_Ecomerce.Helpers;
using Du_An_Ecomerce.ViewModel;

namespace Du_An_Ecomerce.Services
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(VnPaymentRequestModel requestModel);

        VnPaymentResponseModel ProcessPaymentResponse(IQueryCollection collection);
    }

    public class VnPayService : IVnPayService
    {
        private readonly VnPayLibrary _vnPayLibrary;
        private readonly IConfiguration _configuration;
        private readonly HttpContext _httpContext;

        public VnPayService(VnPayLibrary vnPayLibrary, IConfiguration configuration, HttpContext httpContext)
        {
            _vnPayLibrary = vnPayLibrary;
            _configuration = configuration;
            _httpContext = httpContext;
        }

        public string CreatePaymentUrl(VnPaymentRequestModel requestModel)
        {
            var vnp_TmnCode = _configuration["VnPayOptions:vnp_TmnCode"];
            var vnp_HashSecret = _configuration["VnPayOptions:vnp_HashSecret"];
            var vnp_Url = _configuration["VnPayOptions:vnp_Url"];
            var vnp_Version = _configuration["VnPayOptions:vnp_Version"];
            var vnp_Command = _configuration["VnPayOptions:vnp_Command"];
            var vnp_CurrCode = _configuration["VnPayOptions:vnp_CurrCode"];
            var vnp_Locale = _configuration["VnPayOptions:vnp_Locale"];
            var vnp_ReturnUrl = _configuration["VnPayOptions:vnp_ReturnUrl"];

            // Add request data
            _vnPayLibrary.AddRequestData("vnp_Version", vnp_Version);
            _vnPayLibrary.AddRequestData("vnp_Command", vnp_Command);
            _vnPayLibrary.AddRequestData("vnp_TmnCode", vnp_TmnCode);
            _vnPayLibrary.AddRequestData("vnp_Amount", ((int)(requestModel.Amount * 100)).ToString());
            _vnPayLibrary.AddRequestData("vnp_CurrCode", vnp_CurrCode);
            _vnPayLibrary.AddRequestData("vnp_TxnRef", requestModel.OrderId);
            _vnPayLibrary.AddRequestData("vnp_OrderInfo", requestModel.OrderDescription);
            _vnPayLibrary.AddRequestData("vnp_ReturnUrl", vnp_ReturnUrl);
            _vnPayLibrary.AddRequestData("vnp_Locale", vnp_Locale);
            _vnPayLibrary.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            _vnPayLibrary.AddRequestData("vnp_IpAddr", VNPayUtils.GetIpAddress(_httpContext));

            // Create payment URL
            return _vnPayLibrary.CreateRequestUrl(vnp_Url, vnp_HashSecret);
        }

        public VnPaymentResponseModel ProcessPaymentResponse(IQueryCollection collection)
        {
            var vnp_HashSecret = _configuration["VnPayOptions:vnp_HashSecret"];

            // Add response data
            foreach (var key in collection.Keys)
            {
                if (key.StartsWith("vnp_"))
                {
                    _vnPayLibrary.AddResponseData(key, collection[key]);
                }
            }

            // Validate signature
            var vnp_SecureHash = _vnPayLibrary.GetResponseData("vnp_SecureHash");
            var isValidSignature = _vnPayLibrary.ValidateSignature(vnp_SecureHash, vnp_HashSecret);

            return new VnPaymentResponseModel
            {
                Success = isValidSignature,
                PaymentMethod = "VnPay",
                OrderDescription = _vnPayLibrary.GetResponseData("vnp_OrderInfo"),
                OrderId = _vnPayLibrary.GetResponseData("vnp_TxnRef"),
                PaymentId = _vnPayLibrary.GetResponseData("vnp_PayDate"),
                TransactionId = _vnPayLibrary.GetResponseData("vnp_TransactionNo"),
                Token = _vnPayLibrary.GetResponseData("vnp_BankTranNo"),
                VnPayResponseCode = _vnPayLibrary.GetResponseData("vnp_ResponseCode")
            };
        }
    }
}