using Sunny.Subdy.Common.Logs;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Sunny.Subd.Core.Email
{
    // Port trung thực của gmailus.py: get_email_from_shopmailmmo + get_otp_from_funotp
    // (hàm OTP trong Python tên "funotp" nhưng thực chất gọi API shopmailmmo).
    // Cung cấp hộp thư Gmail + mã OTP từ shopmailmmo.com cho luồng đăng ký Facebook.
    //
    // QUAN TRỌNG: KHÔNG được thêm service này vào RegistrationType.EmailTypes —
    // danh sách đó bị đọc theo CHỈ SỐ (GetIntType("cbb_Email",0)); chèn vào sẽ dịch
    // âm thầm config đã lưu của Facebook. Module Reg Facebook gọi thẳng service này
    // khi người dùng bật shopmailmmo trong form cài đặt riêng.
    //
    // GHI CHÚ khác biệt có chủ đích so với Python: bước "resend" ở lần thử thứ 3 của
    // Python dùng khớp ảnh mẫu (reeee1.png/dffdssfsfsfsddf.png) không có trong repo
    // này nên được bỏ qua — chỉ giữ vòng gọi API /orders/otp (30 lần x 5s).
    // Kết quả của GetEmail — phân biệt "hết mail tạm thời" (đã tự chờ + retry bên trong
    // tới khi có mail hoặc bị hủy) với "lỗi cứng" (sai key/hết số dư — retryable=false).
    // Caller (RegFacebookRegsiner) dựa vào Status để quyết định DỪNG TOOL hay tiếp tục.
    public enum ShopMailMmoEmailStatus
    {
        Success,     // đã lấy được mail (Email có giá trị)
        FatalError   // sai key / hết số dư / service không tồn tại → đổi thiết bị cũng vô ích
    }

    public sealed class ShopMailMmoEmailResult
    {
        public ShopMailMmoEmailStatus Status { get; init; } = ShopMailMmoEmailStatus.Success;
        public string Email { get; init; } = string.Empty;
        // Mô tả lỗi rõ ràng (đã dịch sang tiếng Việt) khi Status = FatalError.
        public string Message { get; init; } = string.Empty;
    }

    public class ShopMailMmoService
    {
        private const string BaseUrl = "https://api.shopmailmmo.com/api/v2/public";
        private const string ServiceName = "otp_gmail_facebook";

        private readonly string _apiKey;

        // order_id lấy được từ GetEmail, dùng lại ở GetCode. Được reset mỗi lần
        // GetEmail chạy nên không rò rỉ giữa các account trong vòng lặp tuần tự.
        private string _orderId = string.Empty;
        public string OrderId => _orderId;

        public ShopMailMmoService(string apiKey)
        {
            _apiKey = (apiKey ?? string.Empty).Trim();
        }

        // Lấy một hộp thư Gmail — CHỜ VÔ HẠN tới khi shopmailmmo có mail (decision của
        // user: shopmailmmo update mail thường xuyên, có lúc hết rồi lại có). Giống
        // gmailus.py gốc vốn retry gần như vô hạn. KHÔNG trả mail rỗng khi hết mail tạm
        // thời, nên caller không bao giờ reg với email rỗng → không còn "change device
        // oan". Chỉ dừng khi: (a) có mail, (b) lỗi cứng retryable=false (sai key/hết số
        // dư), hoặc (c) người dùng bấm Dừng (ct). Lưu order_id nội bộ để GetCode dùng.
        public async Task<ShopMailMmoEmailResult> GetEmailUntilAvailableAsync(
            CancellationToken ct, int delayMs = 1500)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                LogManager.Warning("[ShopMailMmo] Thiếu API key.");
                return new ShopMailMmoEmailResult
                {
                    Status = ShopMailMmoEmailStatus.FatalError,
                    Message = "Thiếu API key shopmailmmo. Vui lòng điền API key trong form Hành động → tab \"Nguồn mail/OTP\"."
                };
            }

            int attempt = 0;
            while (!ct.IsCancellationRequested)
            {
                attempt++;
                string uniqueKey = Guid.NewGuid().ToString();
                string url = $"{BaseUrl}/orders?api_key={Uri.EscapeDataString(_apiKey)}&service={ServiceName}&unique_key={uniqueKey}";
                string json = await GetJsonAsync(url);

                if (!string.IsNullOrEmpty(json))
                {
                    try
                    {
                        var data = JsonNode.Parse(json)!.AsObject();
                        string status = data["status"]?.GetValue<string>();
                        string message = data["message"]?.GetValue<string>();

                        if (status == "success")
                        {
                            var order = data["data"]?.AsObject();
                            string email = order?["email"]?.GetValue<string>();
                            string orderId = ReadNodeAsString(order?["id"]);
                            if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(orderId))
                            {
                                _orderId = orderId;
                                LogManager.Success($"[ShopMailMmo] Lấy được mail: {email} (sau {attempt} lần chờ).");
                                return new ShopMailMmoEmailResult
                                {
                                    Status = ShopMailMmoEmailStatus.Success,
                                    Email = email
                                };
                            }
                            // success nhưng thiếu email/id — coi như tạm thời, chờ tiếp.
                        }
                        else
                        {
                            bool? retryable = ReadNodeAsBool(data["retryable"]);
                            if (retryable == false)
                            {
                                // LỖI CỨNG — sai key / hết số dư / service không tồn tại.
                                string vn = TranslateShopMailMessage(message, json);
                                LogManager.Warning($"[ShopMailMmo] LỖI CỨNG (retryable=false): {vn}");
                                return new ShopMailMmoEmailResult
                                {
                                    Status = ShopMailMmoEmailStatus.FatalError,
                                    Message = vn
                                };
                            }
                            // retryable=true (hoặc null) → hết mail TẠM THỜI, chờ rồi thử lại.
                        }
                    }
                    catch (Exception ex)
                    {
                        LogManager.Error(ex);
                    }
                }

                // Báo trạng thái để user thấy đang chờ (chứ không treo), mỗi 10 lần/lần ghi.
                if (attempt % 10 == 1)
                {
                    LogManager.Info($"[ShopMailMmo] Đang chờ có mail… (lần {attempt}, shopmailmmo tạm hết mail — sẽ tự thử lại).");
                }

                await Task.Delay(delayMs, ct);
            }

            ct.ThrowIfCancellationRequested();
            return new ShopMailMmoEmailResult
            {
                Status = ShopMailMmoEmailStatus.FatalError,
                Message = "Đã dừng khi đang chờ mail shopmailmmo."
            };
        }

        // Dịch mã lỗi thô của shopmailmmo sang thông báo tiếng Việt rõ ràng.
        private static string TranslateShopMailMessage(string message, string rawJson)
        {
            string m = (message ?? string.Empty).Trim();
            string lower = m.ToLowerInvariant();
            string detail = string.IsNullOrEmpty(m) ? rawJson : m;

            if (lower.Contains("key") || lower.Contains("auth") || lower.Contains("token") || lower.Contains("invalid"))
                return $"API key shopmailmmo không hợp lệ hoặc hết hạn. Phản hồi: {detail}. Vui lòng kiểm tra lại key trong tab \"Nguồn mail/OTP\".";
            if (lower.Contains("balance") || lower.Contains("money") || lower.Contains("fund") || lower.Contains("coin") || lower.Contains("số dư"))
                return $"Tài khoản shopmailmmo hết số dư. Phản hồi: {detail}. Vui lòng nạp thêm tiền để tiếp tục.";
            if (lower.Contains("service") || lower.Contains("notfound") || lower.Contains("not_found") || lower.Contains("not found"))
                return $"Service \"{ServiceName}\" không tồn tại trên tài khoản shopmailmmo này. Phản hồi: {detail}.";
            if (lower.Contains("stock") || lower.Contains("sold") || lower.Contains("empty") || lower.Contains("mail"))
                return $"shopmailmmo báo lỗi mail: {detail}";
            return $"shopmailmmo báo lỗi không thử lại được: {detail}. (Sai key / hết số dư / service không hợp lệ — đổi thiết bị cũng không khắc phục được.)";
        }

        // Đợi mã OTP cho order hiện tại (30 lần x 5s, giống Python). Rỗng nếu hết hạn.
        // Nhận CancellationToken tùy chọn để nút "Dừng" ngắt được lúc đang chờ OTP
        // (tránh treo tối đa 150s sau khi user bấm dừng).
        public async Task<string> GetCode(int retries = 30, int delayMs = 5000, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(_orderId)) return string.Empty;

            for (int i = 0; i < retries; i++)
            {
                if (ct.IsCancellationRequested) break;
                string uniqueKey = Guid.NewGuid().ToString();
                string url = $"{BaseUrl}/orders/otp?order_id={Uri.EscapeDataString(_orderId)}&unique_key={uniqueKey}";
                string json = await GetJsonAsync(url);
                if (!string.IsNullOrEmpty(json))
                {
                    try
                    {
                        var data = JsonNode.Parse(json)!.AsObject();
                        if (data["status"]?.GetValue<string>() == "success")
                        {
                            string otp = data["data"]?.AsObject()?["otp"]?.GetValue<string>();
                            if (!string.IsNullOrEmpty(otp)) return otp;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogManager.Error(ex);
                    }
                }
                await Task.Delay(delayMs, ct);
            }

            return string.Empty;
        }

        // id có thể là số hoặc chuỗi trong JSON — đọc an toàn cả hai.
        private static string ReadNodeAsString(JsonNode node)
        {
            if (node == null) return null;
            try { return node.GetValue<string>(); }
            catch { return node.ToString(); }
        }

        private static bool? ReadNodeAsBool(JsonNode node)
        {
            if (node == null) return null;
            try { return node.GetValue<bool>(); }
            catch
            {
                if (bool.TryParse(node.ToString(), out bool b)) return b;
                return null;
            }
        }

        private static async Task<string> GetJsonAsync(string url)
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.TryAddWithoutValidation("Accept", "application/json");
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                return string.Empty;
            }
        }
    }
}
