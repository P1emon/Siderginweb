using MyEStore.Helpers;
using MyEStore.Models;
using System.Security.Policy;

namespace MyEStore.Servicess
{
    public class VnPayService : IVnpayService 
    {
        private IConfiguration _config;

        public VnPayService(IConfiguration config)
        {
            _config = config;
        }
        public string CreatePaymentUrl(HttpContext context, VnPaymentRequestModel model)
        {
            var vnpay = new VnPayLibrary();

            vnpay.AddRequestData("vnp_Version", _config["VnPay:Version"]);
            vnpay.AddRequestData("vnp_Command", _config["VnPay:Command"]);
            vnpay.AddRequestData("vnp_TmnCode", _config["VnPay:TmnCode"]);
            vnpay.AddRequestData("vnp_Amount", (model.Amount * 100).ToString());

            // ✅ Chỉ để dòng này
            vnpay.AddRequestData("vnp_TxnRef", model.OrderId.ToString());

            // Chuyển UTC sang giờ Việt Nam (SE Asia Standard Time ~ GMT+7)
            TimeZoneInfo vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            DateTime vnTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);

            vnpay.AddRequestData("vnp_CreateDate", vnTime.ToString("yyyyMMddHHmmss"));
            DateTime vnExpire = vnTime.AddMinutes(15);
            vnpay.AddRequestData("vnp_ExpireDate", vnExpire.ToString("yyyyMMddHHmmss"));

            vnpay.AddRequestData("vnp_CurrCode", _config["VnPay:CurrCode"]);
            vnpay.AddRequestData("vnp_IpAddr", Utils.GetIpAddress(context));
            vnpay.AddRequestData("vnp_Locale", _config["VnPay:Locale"]);
            vnpay.AddRequestData("vnp_OrderInfo", "Thanh toán cho đơn hàng:" + model.OrderId);
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", _config["VnPay:PaymentBackReturnUrl"]);

            return vnpay.CreateRequestUrl(_config["VnPay:BaseUrl"], _config["VnPay:HashSecret"]);
        }


        public VnPaymentResponseModel PaymentExcute(IQueryCollection collections)
        {
            var vnpay = new VnPayLibrary();
            foreach (var (key, value) in collections)
            {
                if (!string.IsNullOrEmpty(value) && key.StartsWith("vnp_"))
                {
                    vnpay.AddResponseData(key, value.ToString());
                }
            }

            var vnp_orderId = vnpay.GetResponseData("vnp_TxnRef");
            var vnp_TransactionId = vnpay.GetResponseData("vnp_TransactionNo");
            var vnp_SecureHash = collections.FirstOrDefault(x => x.Key == "vnp_SecureHash").Value;
            var vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
            var vnp_OrderInfo = vnpay.GetResponseData("vnp_OrderInfo");

            // Kiểm tra chữ ký
            bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, _config["VnPay:HashSecret"]);
            if (!checkSignature)
            {
                return new VnPaymentResponseModel
                {
                    Success = false,
                    Message = "Chữ ký bảo mật không hợp lệ."
                };
            }

            if (vnp_ResponseCode == "00")
            {
                return new VnPaymentResponseModel
                {
                    Success = true,
                    PaymentMethod = "VnPay",
                    OrderDescription = vnp_OrderInfo,
                    OrderId = vnp_orderId,
                    TransactionId = vnp_TransactionId,
                    Token = vnp_SecureHash,
                    VnPayResponseCode = vnp_ResponseCode,
                    Message = "Giao dịch thành công."
                };
            }
            else
            {
                return new VnPaymentResponseModel
                {
                    Success = false,
                    PaymentMethod = "VnPay",
                    OrderDescription = vnp_OrderInfo,
                    OrderId = vnp_orderId,
                    TransactionId = vnp_TransactionId,
                    Token = vnp_SecureHash,
                    VnPayResponseCode = vnp_ResponseCode,
                    Message = $"Giao dịch thất bại. Mã lỗi: {vnp_ResponseCode}"
                };
            }
        }

    }
}
