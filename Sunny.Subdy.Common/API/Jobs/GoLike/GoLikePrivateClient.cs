using System.Text.Json.Nodes;
using Sunny.Subdy.Common.API.Model;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Logs;

namespace Sunny.Subdy.Common.API.Jobs.GoLike
{
    /// <summary>
    /// Client cho Golike "Facebook Private Job API" (gateway.golike.net).
    /// Thay thế các endpoint Subdy FarmJob: Login / GetJobs / CompleteJob / SkipJob.
    /// </summary>
    public static class GoLikePrivateClient
    {
        private const string BaseUrl = "https://gateway.golike.net";

        private static string EscapeJson(string s)
        {
            if (s == null) return string.Empty;
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }

        private static void CheckForServerError(string response)
        {
            if (string.IsNullOrWhiteSpace(response)) return;
            var trimmed = response.TrimStart();
            if (trimmed.StartsWith("<", StringComparison.Ordinal))
            {
                if (response.Contains("502") || response.Contains("Bad Gateway"))
                    throw new Exception("Server đang bảo trì, vui lòng thử lại sau.");
                if (response.Contains("503") || response.Contains("Service Unavailable"))
                    throw new Exception("Server tạm thời không khả dụng, vui lòng thử lại sau.");
                if (response.Contains("504") || response.Contains("Gateway Timeout"))
                    throw new Exception("Server phản hồi quá chậm (timeout), vui lòng thử lại sau.");
                if (response.Contains("403") || response.Contains("Forbidden"))
                    throw new Exception("Truy cập bị từ chối (403), vui lòng liên hệ hỗ trợ.");
                if (response.Contains("404") || response.Contains("Not Found"))
                    throw new Exception("Endpoint không tồn tại (404), vui lòng liên hệ hỗ trợ.");
                throw new Exception("Server đang gặp sự cố, vui lòng thử lại sau.");
            }
        }

        public static User Login(string username, string password)
        {
            try
            {
                string url = $"{BaseUrl}/api/auto/login";

                // API yêu cầu multipart/form-data (xem curl mẫu). Build body thủ công và
                // dùng HttpRequestHelper.Request để không bị override Content-Type.
                string boundary = "----LamToolBoundary" + DateTime.UtcNow.Ticks.ToString("x");
                string body =
                    $"--{boundary}\r\n" +
                    "Content-Disposition: form-data; name=\"username\"\r\n\r\n" +
                    username + "\r\n" +
                    $"--{boundary}\r\n" +
                    "Content-Disposition: form-data; name=\"password\"\r\n\r\n" +
                    password + "\r\n" +
                    $"--{boundary}--\r\n";

                string headerText =
                    $"Content-Type: multipart/form-data; boundary={boundary}\r\n" +
                    "accept: application/json\r\n" +
                    "x-requested-with: XMLHttpRequest\r\n";

                string response = HttpRequestHelper.Request("POST", url, headerText, body);

                if (string.IsNullOrEmpty(response))
                    throw new Exception("Không thể kết nối đến server Golike. Vui lòng thử lại.");

                CheckForServerError(response);

                var jObject = JsonNode.Parse(response)!.AsObject();
                bool success = jObject["success"]?.GetValue<bool>() ?? false;
                if (!success)
                    throw new Exception(jObject["message"]?.ToString() ?? "Đăng nhập Golike thất bại");

                // Token nằm ở root, không nằm trong data.
                string token = jObject["token"]?.ToString();
                if (string.IsNullOrEmpty(token))
                    throw new Exception("Phản hồi không có token.");

                var data = jObject["data"]?.AsObject();
                return new User
                {
                    Id = data?["id"]?.ToString(),
                    UserName = data?["username"]?.ToString() ?? username,
                    Email = data?["email"]?.ToString(),
                    FullName = data?["name"]?.ToString(),
                    Balance = data?["coin"] != null ? Convert.ToDouble(data["coin"]!.ToString()) : 0,
                    Role = data?["role"]?.ToString(),
                    IsActive = (data?["status"]?.GetValue<int>() ?? 1) == 1,
                    IsBanned = false,
                    Token = token,
                    ApiKey = token,
                    Password = password
                };
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                throw new Exception(ex.Message);
            }
        }

        public static List<JobModel> GetJobs(string token, string fbId, string type = "")
        {
            try
            {
                string url = $"{BaseUrl}/api/advertising/publishers/_private/get-jobs?fb_id={Uri.EscapeDataString(fbId)}";
                if (!string.IsNullOrEmpty(type))
                    url += $"&type={Uri.EscapeDataString(type)}";

                var headers = new Dictionary<string, string>
                {
                    ["accept"] = "application/json",
                    ["Authorization"] = token
                };
                string response = HttpRequestHelper.GET(url, headers: headers);

                if (string.IsNullOrEmpty(response))
                    throw new Exception("Không thể kết nối đến server Golike.");

                CheckForServerError(response);

                var jObject = JsonNode.Parse(response)!.AsObject();
                bool success = jObject["success"]?.GetValue<bool>() ?? false;
                if (!success)
                    throw new Exception(jObject["message"]?.ToString() ?? "Lấy danh sách job Golike thất bại");

                var jobs = new List<JobModel>();
                var dataArray = jObject["data"]?.AsArray();
                if (dataArray != null)
                {
                    foreach (var node in dataArray)
                    {
                        if (node == null) continue;
                        var item = node.AsObject();
                        var job = new JobModel
                        {
                            JobId = item["id"]?.ToString() ?? "",
                            ObjectId = item["object_id"]?.ToString() ?? "",
                            Link = item["link"]?.ToString() ?? "",
                            Type = item["type"]?.ToString() ?? "",
                            Coin = item["fix_coin_job"] != null
                                ? Convert.ToDouble(item["fix_coin_job"]!.ToString())
                                : 0
                        };
                        var commentRun = item["comment_run"]?.AsObject();
                        if (commentRun != null)
                        {
                            string msg = commentRun["message"]?.ToString();
                            if (!string.IsNullOrEmpty(msg))
                                job.Contents = new List<string> { msg };
                            job.CommentId = commentRun["id"]?.ToString() ?? "";
                        }
                        jobs.Add(job);
                    }
                }
                return jobs;
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                throw new Exception(ex.Message);
            }
        }

        public static string CompleteJob(string token, string jobId, string uid, bool success, string commentId = "", string message = "")
        {
            try
            {
                string url = $"{BaseUrl}/api/advertising/publishers/_private/complete-jobs";
                var headers = new Dictionary<string, string>
                {
                    ["Content-Type"] = "application/json",
                    ["accept"] = "application/json",
                    ["Authorization"] = $"Bearer {token}"
                };
                string jsonBody = $"{{\"job_id\":\"{EscapeJson(jobId)}\",\"uid\":\"{EscapeJson(uid)}\",\"success\":{(success ? "true" : "false")},\"comment_id\":\"{EscapeJson(commentId)}\",\"message\":\"{EscapeJson(message)}\"}}";
                string response = HttpRequestHelper.POST_JSON(url, headers: headers, jsonBody: jsonBody);

                if (string.IsNullOrEmpty(response))
                    throw new Exception("Không thể kết nối đến server Golike.");

                CheckForServerError(response);

                var jObject = JsonNode.Parse(response)!.AsObject();
                bool ok = jObject["success"]?.GetValue<bool>() ?? false;
                if (!ok)
                    throw new Exception(jObject["message"]?.ToString() ?? "Complete job Golike thất bại");

                return jObject["message"]?.ToString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                throw new Exception(ex.Message);
            }
        }

        public static string SkipJob(string token, string jobId, string uid, string reason)
        {
            try
            {
                return CompleteJob(token, jobId, uid, success: false, commentId: "", message: reason ?? "");
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                return ex.Message;
            }
        }
    }
}
