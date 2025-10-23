using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Sunny.Subdy.Common.Logs;

namespace Sunny.Subdy.Server
{
    // Delegate cho các hàm xử lý yêu cầu
    public delegate Task RequestHandler(HttpListenerContext context, Dictionary<string, string> routeParams);

    public class ApiRouter
    {
        // Cấu trúc dữ liệu nội bộ để lưu trữ các route với mẫu regex
        private readonly List<(string httpMethod, Regex regex, string[] paramNames, RequestHandler handler)> _routes;

        public ApiRouter()
        {
            _routes = new List<(string, Regex, string[], RequestHandler)>();
        }

        private void AddRoute(string method, string pathPattern, RequestHandler handler)
        {
            // Chuyển đổi pathPattern thành regex và xác định tên tham số
            // Ví dụ: "/{id}/Change" -> "^/(?<id>[^/]+)/Change$"
            var regexPattern = Regex.Replace(pathPattern, @"\{(\w+)\}", "(?<" + "$1" + ">[^/]+)");
            regexPattern = "^" + regexPattern + "$"; // Đảm bảo khớp toàn bộ chuỗi
            var regex = new Regex(regexPattern, RegexOptions.IgnoreCase); // <-- ĐÂY LÀ ĐIỂM QUAN TRỌNG CHO VIỆC KHÔNG PHÂN BIỆT CHỮ HOA/THƯỜNG

            // Trích xuất tên tham số từ pattern (ví dụ: "id")
            var paramNames = Regex.Matches(pathPattern, @"\{(\w+)\}")
                                    .Cast<Match>()
                                    .Select(m => m.Groups[1].Value)
                                    .ToArray();

            _routes.Add((method, regex, paramNames, handler));
            Debug.WriteLine($"[ApiRouter] Registered: Method={method}, Pattern='{pathPattern}', Generated Regex='{regex.ToString()}'");
            LogManager.Info($"[ApiRouter] Registered: Method={method}, Pattern='{pathPattern}', Generated Regex='{regex.ToString()}'"); // THÊM LOG
        }


        public void Get(string pathPattern, RequestHandler handler) => AddRoute("GET", pathPattern, handler);
        public void Post(string pathPattern, RequestHandler handler) => AddRoute("POST", pathPattern, handler);

        public async Task RouteRequest(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            string method = request.HttpMethod.ToUpper();
            string rawUrlPath = request.Url.AbsolutePath;

            LogManager.Info($"[ApiRouter] Incoming Request: Method={method}, Path='{rawUrlPath}'");

            try
            {
                foreach (var route in _routes)
                {
                    if (route.httpMethod.Equals(method, StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.WriteLine($"[ApiRouter] Trying to match '{rawUrlPath}' against regex '{route.regex.ToString()}'");
                        LogManager.Info($"[ApiRouter] Trying to match '{rawUrlPath}' against regex '{route.regex.ToString()}'");
                        var match = route.regex.Match(rawUrlPath);
                        if (match.Success)
                        {
                            var routeParams = new Dictionary<string, string>();
                            foreach (var paramName in route.paramNames)
                            {
                                routeParams[paramName] = match.Groups[paramName].Value;
                            }
                            Debug.WriteLine($"[ApiRouter] MATCHED! Regex '{route.regex.ToString()}' matched path '{rawUrlPath}'. Executing handler.");
                            LogManager.Info($"[ApiRouter] MATCHED! Regex '{route.regex.ToString()}' matched path '{rawUrlPath}'. Executing handler."); // THÊM LOG
                            await route.handler(context, routeParams);
                            return;
                        }
                        else
                        {
                            Debug.WriteLine($"[ApiRouter] NO MATCH for '{rawUrlPath}' against regex '{route.regex.ToString()}'");
                            LogManager.Info($"[ApiRouter] NO MATCH for '{rawUrlPath}' against regex '{route.regex.ToString()}'"); // THÊM LOG
                        }
                    }
                }

                await SendJsonResponse(response, ApiResponse<object>.ErrorResponse("Endpoint not found."), HttpStatusCode.NotFound);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ApiRouter] NO MATCH for '{rawUrlPath}' against regex '{ex.ToString()}'");
                LogManager.Error(ex);
                await SendJsonResponse(response, ApiResponse<object>.ErrorResponse($"Internal server error: {ex.Message}"), HttpStatusCode.InternalServerError);
            }
            finally
            {
                // Đảm bảo response được đóng nếu handler chưa đóng
                try { response.Close(); } catch { }
            }
        }

        // Các phương thức hỗ trợ khác
        public static async Task<string> ReadRequestBody(HttpListenerRequest request)
        {
            if (!request.HasEntityBody)
                return string.Empty;

            using (StreamReader reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                return await reader.ReadToEndAsync();
            }
        }

        public static async Task SendJsonResponse<T>(HttpListenerResponse response, ApiResponse<T> apiResponse, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            response.ContentType = "application/json";
            response.StatusCode = (int)statusCode;

            // Sử dụng JsonSerializer.Serialize thay vì JsonConvert.SerializeObject
            // Bạn có thể thêm JsonSerializerOptions nếu muốn cấu hình (ví dụ: CamelCase)
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // Tự động chuyển PascalCase sang camelCase
                WriteIndented = true // Để JSON dễ đọc hơn (tùy chọn)
            };

            string jsonResponse = System.Text.Json.JsonSerializer.Serialize(apiResponse, options);
            byte[] buffer = Encoding.UTF8.GetBytes(jsonResponse);
            response.ContentLength64 = buffer.Length;

            using (Stream output = response.OutputStream)
            {
                await output.WriteAsync(buffer, 0, buffer.Length);
            }
        }
    }
}
