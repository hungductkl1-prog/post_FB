using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace Sunny.Subdy.Common.API.Mail
{
    public class DongVanFbCodeResult
    {
        public bool Status { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }

    public static class DongVanFbClient
    {
        public const string Url = "https://tools.dongvanfb.net/api/graph_code";
        private static readonly HttpClient _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        public static async Task<DongVanFbCodeResult> GetFacebookCodeAsync(
            string email, string refreshToken, string clientId, CancellationToken ct = default)
        {
            var result = new DongVanFbCodeResult();
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(refreshToken) || string.IsNullOrWhiteSpace(clientId))
            {
                result.Error = "Thiếu email/refresh_token/client_id";
                return result;
            }
            try
            {
                var body = new
                {
                    email,
                    refresh_token = refreshToken,
                    client_id = clientId,
                    type = "facebook"
                };
                using var resp = await _http.PostAsJsonAsync(Url, body, ct);
                var raw = await resp.Content.ReadAsStringAsync(ct);
                if (!resp.IsSuccessStatusCode)
                {
                    result.Error = $"HTTP {(int)resp.StatusCode}: {raw}";
                    return result;
                }
                var data = JsonNode.Parse(raw)?.AsObject();
                if (data == null)
                {
                    result.Error = $"Phản hồi không hợp lệ: {raw}";
                    return result;
                }
                result.Status = data["status"]?.GetValue<bool>() ?? false;
                result.Code = data["code"]?.ToString() ?? string.Empty;
                result.Content = data["content"]?.ToString() ?? string.Empty;
                result.Date = data["date"]?.ToString() ?? string.Empty;
                if (!result.Status && string.IsNullOrEmpty(result.Error))
                {
                    result.Error = $"API trả về status=false: {raw}";
                }
                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
                return result;
            }
        }
    }
}
