using System.Text.Json.Nodes;
using Sunny.Subdy.Common.API.Jobs.GoLike;
using Sunny.Subdy.Common.API.Model;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Logs;

namespace Sunny.Subdy.Common.API
{
    public class SubdyClient
    {
        private const string BaseUrl = "https://dev.subdy.net/api";

        private static string EscapeJson(string s)
        {
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }

        /// <summary>
        /// Detect if the response body is an HTML error page (502, 503, Nginx, etc.)
        /// instead of valid JSON from the API.
        /// </summary>
        private static void CheckForServerError(string response)
        {
            if (string.IsNullOrWhiteSpace(response)) return;
            var trimmed = response.TrimStart();
            if (trimmed.StartsWith("<", StringComparison.Ordinal))
            {
                // HTML response — parse out common status hints
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
                // Generic fallback for any other HTML
                throw new Exception("Server đang gặp sự cố, vui lòng thử lại sau.");
            }
        }

        public static User Login(string username, string password)
        {
            // Chuyển sang dùng Golike Private API (gateway.golike.net)
            return GoLikePrivateClient.Login(username, password);
        }
        public static string GetTokenAutoGolike(string username, string password)
        {
            try
            {
                string url = "https://auto.golike.net/api/user/auth/login";
                var headers = new Dictionary<string, string>
                {
                    ["Content-Type"] = "application/json",
                    ["accept"] = "application/json"
                };
                string jsonBody = $"{{\"username\":\"{EscapeJson(username)}\",\"password\":\"{EscapeJson(password)}\",\"source\":\"tool\"}}";
                string response = HttpRequestHelper.POST_JSON(url, headers: headers, jsonBody: jsonBody);

                if (string.IsNullOrEmpty(response))
                {
                    throw new Exception("Không thể kết nối đến server Auto Golike. Vui lòng thử lại.");
                }

                CheckForServerError(response);

                var jObject = JsonNode.Parse(response)!.AsObject();

                string accessToken = jObject["accessToken"]?.ToString();
                if (string.IsNullOrEmpty(accessToken))
                {
                    string message = jObject["message"]?.ToString()
                                     ?? jObject["error"]?.ToString()
                                     ?? "Đăng nhập Auto Golike thất bại";
                    throw new Exception(message);
                }

                return accessToken;
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                throw new Exception(ex.Message);
            }
        }
        public static User Register(string username, string email, string password, string fullName)
        {
            try
            {
                string url = $"{BaseUrl}/auth/register";
                var headers = new Dictionary<string, string>
                {
                    ["Content-Type"] = "application/json",
                    ["accept"] = "*/*"
                };
                string jsonBody = $"{{\"username\":\"{EscapeJson(username)}\",\"email\":\"{EscapeJson(email)}\",\"password\":\"{EscapeJson(password)}\",\"fullName\":\"{EscapeJson(fullName)}\"}}";
                string response = HttpRequestHelper.POST_JSON(url, headers: headers, jsonBody: jsonBody);

                if (string.IsNullOrEmpty(response))
                {
                    throw new Exception("Không thể kết nối đến server. Vui lòng thử lại.");
                }

                CheckForServerError(response);

                var jObject = JsonNode.Parse(response)!.AsObject();
                bool success = jObject["success"]?.GetValue<bool>() ?? false;
                if (!success)
                {
                    throw new Exception(jObject["message"]?.ToString() ?? "Đăng ký thất bại");
                }

                var data = jObject["data"];
                var userData = data["user"];

                var user = new User
                {
                    Id = userData["id"]?.ToString(),
                    UserName = userData["username"]?.ToString(),
                    Email = userData["email"]?.ToString(),
                    FullName = userData["fullName"]?.ToString(),
                    Balance = userData["balance"]?.GetValue<double>() ?? 0,
                    Token = data["access_token"]?.ToString(),
                    ApiKey = data["refresh_token"]?.ToString(),
                    Password = password
                };

                return user;
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                throw new Exception(ex.Message);
            }
        }

        // [DEPRECATED - FarmJob Subdy] Đã thay thế bằng GoLikePrivateClient.GetJobs
        //public static List<JobModel> GetJobs(string apiKey, string uid, int limit = 20)
        //{
        //    try
        //    {
        //        string url = $"{BaseUrl}/jobs/get?api_key={apiKey}&uid={uid}&platform=facebook";
        //        var headers = new Dictionary<string, string>
        //        {
        //            ["Content-Type"] = "application/json",
        //            ["accept"] = "application/json"
        //        };
        //        string response = HttpRequestHelper.GET(url, headers: headers);
        //
        //        if (string.IsNullOrEmpty(response))
        //            throw new Exception("Không thể kết nối đến server.");
        //
        //        CheckForServerError(response);
        //
        //        var jObject = JsonNode.Parse(response)!.AsObject();
        //        bool success = jObject["success"]?.GetValue<bool>() ?? false;
        //        if (!success)
        //            throw new Exception(jObject["message"]?.ToString() ?? "Lấy danh sách job thất bại");
        //
        //        var jobs = new List<JobModel>();
        //        var dataArray = jObject["data"]?.AsObject();
        //        if (dataArray != null)
        //        {
        //            var job = new JobModel
        //            {
        //                JobId = dataArray["job_id"]?.ToString(),
        //                ObjectId = dataArray["target_url"]?.ToString(),
        //                Link = dataArray["target_url"]?.ToString(),
        //                Type = dataArray["job_type"]?.ToString(),
        //                Coin = dataArray["price"]?.GetValue<double>() ?? 0,
        //            };
        //            jobs.Add(job);
        //        }
        //
        //        return jobs;
        //    }
        //    catch (Exception ex)
        //    {
        //        LogManager.Error(ex);
        //        throw new Exception(ex.Message);
        //    }
        //}
        // [DEPRECATED - FarmJob Subdy] Đã thay thế bằng GoLikePrivateClient.CompleteJob
        //public static ClaimResult ClaimJob(string apiKey, int jobId, string uid)
        //{
        //    try
        //    {
        //        string url = $"{BaseUrl}/jobs/complete";
        //        var headers = new Dictionary<string, string>
        //        {
        //            ["Content-Type"] = "application/json",
        //            ["accept"] = "application/json"
        //        };
        //        string jsonBody = $"{{\"api_key\":\"{EscapeJson(apiKey)}\",\"job_id\":{jobId},\"uid\":\"{EscapeJson(uid)}\",\"success\":true,\"description\":\"Đã thực hiện thành công.\"}}";
        //        string response = HttpRequestHelper.POST_JSON(url, headers: headers, jsonBody: jsonBody);
        //
        //        if (string.IsNullOrEmpty(response))
        //            throw new Exception("Không thể kết nối đến server.");
        //
        //        CheckForServerError(response);
        //
        //        var jObject = JsonNode.Parse(response)!.AsObject();
        //        bool success = jObject["success"]?.GetValue<bool>() ?? false;
        //        if (!success)
        //            throw new Exception(jObject["message"]?.ToString() ?? "Claim job thất bại");
        //
        //        var data = jObject["data"];
        //        return new ClaimResult
        //        {
        //            AttemptId = data["attempt_id"]?.GetValue<int>() ?? 0,
        //            JobId = data["job_id"]?.GetValue<int>() ?? 0,
        //            Uid = data["uid"]?.ToString(),
        //            TargetUrl = data["target_url"]?.ToString(),
        //            JobType = data["job_type"]?.ToString(),
        //            RequiresProof = data["requires_proof"]?.GetValue<bool>() ?? false,
        //            PotentialReward = data["potential_reward"]?.GetValue<double>() ?? 0,
        //            Status = data["status"]?.ToString(),
        //            ExpiresAt = data["expires_at"]?.ToString(),
        //            RemainingSlots = data["remaining_slots"]?.GetValue<int>() ?? 0,
        //            TimeoutMinutes = data["timeout_minutes"]?.GetValue<int>() ?? 0,
        //            Message = data["message"]?.ToString()
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        LogManager.Error(ex);
        //        throw new Exception(ex.Message);
        //    }
        //}
        public static double GetProfile(string token)
        {
            try
            {
                string url = $"{BaseUrl}/user/profile";
                var headers = new Dictionary<string, string>
                {
                    ["accept"] = "*/*",
                    ["Authorization"] = $"Bearer {token}"
                };
                string response = HttpRequestHelper.GET(url, headers: headers);

                if (string.IsNullOrEmpty(response))
                    throw new Exception("Không thể kết nối đến server.");

                CheckForServerError(response);

                var jObject = JsonNode.Parse(response)!.AsObject();
                bool success = jObject["success"]?.GetValue<bool>() ?? false;
                if (!success)
                    throw new Exception("Lấy thông tin profile thất bại");

                return jObject["data"]?["balance"]?.GetValue<double>() ?? 0;
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                throw new Exception(ex.Message);
            }
        }

        // [DEPRECATED - FarmJob Subdy] Đã thay thế bằng GoLikePrivateClient.SkipJob
        //public static string SkipJob(string apiKey, int jobId, string uid, string reason)
        //{
        //    try
        //    {
        //        string url = $"{BaseUrl}/jobs/skip";
        //        var headers = new Dictionary<string, string>
        //        {
        //            ["Content-Type"] = "application/json",
        //            ["accept"] = "application/json"
        //        };
        //        string jsonBody = $"{{\"key\":\"{EscapeJson(apiKey)}\",\"job_id\":{jobId},\"uid\":\"{EscapeJson(uid)}\",\"reason\":{reason}}}";
        //        string response = HttpRequestHelper.POST_JSON(url, headers: headers, jsonBody: jsonBody);
        //
        //        if (string.IsNullOrEmpty(response))
        //            throw new Exception("Không thể kết nối đến server.");
        //
        //        CheckForServerError(response);
        //
        //        var jObject = JsonNode.Parse(response)!.AsObject();
        //        bool success = jObject["success"]?.GetValue<bool>() ?? false;
        //        if (!success)
        //            throw new Exception(jObject["message"]?.ToString() ?? "Claim job thất bại");
        //
        //        var data = jObject["data"];
        //        return data["message"].ToString();
        //    }
        //    catch (Exception ex)
        //    {
        //        LogManager.Error(ex);
        //        return ex.Message;
        //    }
        //
        //}
        public static ApiKeyResult GetApiKey(string bearerToken)
        {
            try
            {
                string url = $"{BaseUrl}/user/api-key";
                var headers = new Dictionary<string, string>
                {
                    ["accept"] = "application/json",
                    ["Authorization"] = $"Bearer {bearerToken}"
                };
                string response = HttpRequestHelper.GET(url, headers: headers);

                if (string.IsNullOrEmpty(response))
                    throw new Exception("Không thể kết nối đến server.");

                CheckForServerError(response);

                var jObject = JsonNode.Parse(response)!.AsObject();
                bool success = jObject["success"]?.GetValue<bool>() ?? false;
                if (!success)
                    throw new Exception(jObject["message"]?.ToString() ?? "Lấy API key thất bại");

                var data = jObject["data"];
                return new ApiKeyResult
                {
                    ApiKey = data["apiKey"]?.ToString(),
                    HasApiKey = data["hasApiKey"]?.GetValue<bool>() ?? false,
                    Message = data["message"]?.ToString()
                };
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                throw new Exception(ex.Message);
            }
        }

        public static PlatformAccount? AddPlatformAccount(string bearerToken, int platformId, string uid, string displayName)
        {
            try
            {
                string url = $"{BaseUrl}/platform-accounts";
                var headers = new Dictionary<string, string>
                {
                    ["Content-Type"] = "application/json",
                    ["accept"] = "*/*",
                    ["Authorization"] = $"Bearer {bearerToken}"
                };
                string jsonBody = $"{{\"platformId\":{platformId},\"uid\":\"{EscapeJson(uid)}\",\"displayName\":\"{EscapeJson(displayName)}\"}}";
                string response = HttpRequestHelper.POST_JSON(url, headers: headers, jsonBody: jsonBody);

                if (string.IsNullOrEmpty(response))
                    throw new Exception("Không thể kết nối đến server.");

                CheckForServerError(response);

                var jObject = JsonNode.Parse(response)!.AsObject();
                bool success = jObject["success"]?.GetValue<bool>() ?? false;
                if (!success)
                {
                    string errCode = jObject["error"]?["code"]?.ToString();
                    if (errCode == "RES_3003")
                        return null; // Uid đã tồn tại, bỏ qua
                    throw new Exception(jObject["error"]?["message"]?.ToString() ?? jObject["message"]?.ToString() ?? "Thêm platform account thất bại");
                }

                var data = jObject["data"];
                return new PlatformAccount
                {
                    Id = data["id"]?.GetValue<int>() ?? 0,
                    UserId = data["userId"]?.GetValue<int>() ?? 0,
                    PlatformId = data["platformId"]?.GetValue<int>() ?? 0,
                    Uid = data["uid"]?.ToString(),
                    DisplayName = data["displayName"]?.ToString(),
                    IsActive = data["isActive"]?.GetValue<bool>() ?? true,
                    CreatedAt = data["createdAt"]?.ToString(),
                    UpdatedAt = data["updatedAt"]?.ToString()
                };
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                throw new Exception(ex.Message);
            }
        }

        public static ApiKeyResult CreateApiKey(string bearerToken)
        {
            try
            {
                string url = $"{BaseUrl}/user/api-key";
                var headers = new Dictionary<string, string>
                {
                    ["Content-Type"] = "application/json",
                    ["accept"] = "application/json",
                    ["Authorization"] = $"Bearer {bearerToken}"
                };
                string response = HttpRequestHelper.POST_JSON(url, headers: headers, jsonBody: "");

                if (string.IsNullOrEmpty(response))
                    throw new Exception("Không thể kết nối đến server.");

                CheckForServerError(response);

                var jObject = JsonNode.Parse(response)!.AsObject();
                bool success = jObject["success"]?.GetValue<bool>() ?? false;
                if (!success)
                    throw new Exception(jObject["message"]?.ToString() ?? "Tạo API key thất bại");

                var data = jObject["data"];
                return new ApiKeyResult
                {
                    ApiKey = data["apiKey"]?.ToString(),
                    HasApiKey = true,
                    Message = data["message"]?.ToString()
                };
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                throw new Exception(ex.Message);
            }
        }
    }
}
