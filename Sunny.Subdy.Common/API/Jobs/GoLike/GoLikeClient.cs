using System.Text.Json;
using System.Text.Json.Nodes;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Logs;
using System.Net;

namespace Sunny.Subdy.Common.API.Jobs.GoLike
{
    public class GoLikeClient
    {
        public static string UrlGetJob = "https://gateway.golike.net/api/advertising/publishers/_private/get-jobs?fb_id=";
        public static string UrlReportJob = "https://gateway.golike.net/api/advertising/publishers/_private/complete-jobs";
        public static string UrlJobTypes = "https://gateway.golike.net/api/advertising/publishers/_private/get-config";

        public string GetCoin(string token)
        {
            var report = GetCoinReport(token);
            return report.CurrentCoin >= 0 ? report.CurrentCoin.ToString() : "";
        }

        /// <summary>
        /// Gọi /api/statistics/report và trả về số dư hiện tại + tổng pending_coin trên mọi nền tảng.
        /// Trả CurrentCoin = -1 nếu gọi thất bại (caller phân biệt với 0 thực).
        /// </summary>
        public (long CurrentCoin, long PendingCoin) GetCoinReport(string token)
        {
            string apiUrl = "https://gateway.golike.net/api/statistics/report";
            var headers = new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {token}",
                ["ref"] = "LamToolAutoPhone",
                ["Cookie"] = "tool=dtasoftware;",
                ["dta"] = ""
            };
            string json = HttpRequestHelper.GET(apiUrl, headers: headers);
            if (string.IsNullOrWhiteSpace(json))
                return (-1, 0);

            try
            {
                var responseObj = JsonNode.Parse(json)!.AsObject();
                bool isSuccess = responseObj["success"]?.GetValue<bool>() == true;
                if (!isSuccess) return (-1, 0);

                long currentCoin = responseObj["current_coin"]?.GetValue<long>() ?? 0;

                long pending = 0;
                foreach (var kv in responseObj)
                {
                    if (kv.Value is JsonObject child && child["pending_coin"] != null)
                    {
                        pending += child["pending_coin"]!.GetValue<long>();
                    }
                }
                return (currentCoin, pending);
            }
            catch (Exception)
            {
                return (-1, 0);
            }
        }

        public async Task<JsonNode?> GetFacebookJob(string uid, string token, string job_type = "", string fb_name = "")
        {
            // Khớp subdy-phone-farm-tools: gắn fb_name + type vào query khi có
            string url = $"{UrlGetJob}{uid}";
            if (!string.IsNullOrEmpty(fb_name))
                url += $"&fb_name={Uri.EscapeDataString(fb_name)}";
            if (!string.IsNullOrEmpty(job_type))
                url += $"&type={Uri.EscapeDataString(job_type)}";

            var headers = new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {token}",
                ["ref"] = "LamToolAutoPhone",
                ["Cookie"] = "tool=dtasoftware;",
                ["dta"] = ""
            };

            string json = HttpRequestHelper.GET(url, headers: headers);
            if (string.IsNullOrWhiteSpace(json))
                throw new Exception("Không lấy được kết quả từ server.");

            try
            {
                var responseObj = JsonNode.Parse(json)!.AsObject();

                bool isSuccess = Convert.ToBoolean(responseObj["success"]?.ToString());
                if (isSuccess)
                {
                    var jobs = responseObj["data"]?.AsArray();
                    if (jobs != null && jobs.Any())
                        return responseObj;

                    var message = responseObj["message"]?.ToString();
                    throw new Exception(!string.IsNullOrEmpty(message) ? message : "Không có job nào được trả về.");
                }

                throw new Exception(responseObj["message"]?.ToString() ?? "Phản hồi từ server không thành công:\n" + json);
            }
            catch (JsonException ex)
            {
                throw new Exception("Lỗi phân tích JSON:\n" + ex.Message + "\nRaw:\n" + json);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public List<string> GetJobTypes(string token)
        {
            string url = UrlJobTypes;

            var headers = new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {token}",
                ["ref"] = "LamToolAutoPhone",
                ["Cookie"] = "tool=dtasoftware;",
                ["dta"] = ""
            };

            string json = HttpRequestHelper.GET(url, headers: headers);
            if (string.IsNullOrWhiteSpace(json))
                throw new Exception("Không lấy được kết quả từ server.");

            try
            {
                var responseObj = JsonNode.Parse(json)!.AsObject();
                bool isSuccess = responseObj["success"] != null && Convert.ToBoolean(responseObj["success"]?.ToString());

                if (!isSuccess)
                    throw new Exception(responseObj["message"]?.ToString() ?? "Phản hồi không thành công:\n" + json);


                var packageArray = responseObj["data"]?["facebook"]?["package_name"]?.AsArray();

                if (packageArray == null)
                {
                    throw new Exception("Không tìm thấy loại job:\n" + json);
                }
                List<string> lines = new List<string>();
                foreach (var item in packageArray)
                {
                    var fixCoin = item["fix_coin_job"]?.ToString() ?? "0";
                    var name = item["package_name"]?.ToString() ?? "";
                    lines.Add($"(+{fixCoin}) {name}");
                }
                return lines;
            }
            catch (JsonException ex)
            {
                throw new Exception("Lỗi phân tích JSON:\n" + ex.Message + "\nRaw:\n" + json);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<JsonNode?> ReportFacebookJob(string uid, string fullname, string token, JobModel job)
        {
            var headers = new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {token}",
                ["ref"] = "LamToolAutoPhone",
                ["Cookie"] = "tool=dtasoftware;",
                ["dta"] = ""
            };

            // Khớp subdy-phone-farm-tools: success/post_private là bool, không phải string PascalCase
            var body = new JsonObject
            {
                ["job_id"] = job.JobId,
                ["uid"] = uid,
                ["success"] = job.Success,
                ["fb_name"] = fullname ?? "",
                ["id_text"] = "",
                ["post_private"] = job.IsView,
                ["note"] = job.Link ?? ""
            };

            if (job.Type == JobTypes.Comment && !string.IsNullOrEmpty(job.CommentId))
                body["comment_id"] = job.CommentId;

            string jsonBody = body.ToJsonString();
            string json = HttpRequestHelper.POST_JSON(UrlReportJob, headers: headers, jsonBody: jsonBody);

            if (string.IsNullOrWhiteSpace(json))
                throw new Exception("Không lấy được kết quả từ server.");

            try
            {
                var responseObj = JsonNode.Parse(json)!.AsObject();
                bool isSuccess = Convert.ToBoolean(responseObj["success"].ToString());

                if (isSuccess)
                    return responseObj;

                throw new Exception(responseObj["message"]?.ToString() ?? "Phản hồi không thành công:\n" + json);
            }
            catch (JsonException ex)
            {
                throw new Exception("Lỗi phân tích JSON:\n" + ex.Message + "\nRaw:\n" + json);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<Dictionary<string, string>> GetInstagramAccount(string token)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, "https://gateway.golike.net/api/instagram-account");
                request.Headers.Add("authorization", $"Bearer {token}");
                request.Headers.Add("t", "VFZSak1VNUVUVEpPZW1kNFRsRTlQUT09");
                request.Headers.Add("accept", "application/json, text/plain, */*");
                request.Headers.Add("accept-language", "en-US,en;q=0.9");
                request.Headers.Add("origin", "https://app.golike.net");
                request.Headers.Add("sec-ch-ua", "\"Not)A;Brand\";v=\"8\", \"Chromium\";v=\"138\", \"Microsoft Edge\";v=\"138\"");
                request.Headers.Add("sec-ch-ua-mobile", "?0");
                request.Headers.Add("sec-ch-ua-platform", "\"Windows\"");
                request.Headers.Add("sec-fetch-dest", "empty");
                request.Headers.Add("sec-fetch-mode", "cors");
                request.Headers.Add("sec-fetch-site", "same-site");
                request.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36 Edg/138.0.0.0");
                var response = await client.SendAsync(request);
                string json = await response.Content.ReadAsStringAsync();
                var jObject = JsonNode.Parse(json)!.AsObject();
                var data = jObject["data"];
                foreach (var item in data.AsArray())
                {
                    string username = item["instagram_username"]?.ToString();
                    string id = item["id"]?.ToString();
                    if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(id))
                    {
                        result[username] = id;
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                result["error"] = ex.Message;
            }
            return result;
        }
        public async Task<Dictionary<string, string>> VerifyAccountInstagram(string token, string username)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, "https://gateway.golike.net/api/instagram-account/verify-account");
                request.Headers.Add("authorization", $"Bearer {token}");
                request.Headers.Add("t", "VFZSak1VMTZZek5OVkVFd1RuYzlQUT09");
                var content = new StringContent("{\"object_id\":\"" + username + "\"}", null, "application/json");
                request.Content = content;
                var response = await client.SendAsync(request);
                string json = await response.Content.ReadAsStringAsync();
                if (response == null || string.IsNullOrEmpty(json))
                {
                    result["error"] = "Server không phản hồi...";
                    return result;
                }
                if (response != null && response.StatusCode != HttpStatusCode.OK)
                {
                    result["error"] = JsonNode.Parse(json!)!.AsObject()["message"].ToString();
                    return result;
                }
                result["success"] = JsonNode.Parse(json!)!.AsObject()["message"].ToString();
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                result["error"] = ex.Message;
            }
            return result;
        }
        public async Task<Dictionary<string, string>> GetAccount(string token)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, "https://gateway.golike.net/api/users/me");
                request.Headers.Add("authorization", $"Bearer {token}");
                request.Headers.Add("t", "VFZSak1VMTZZek5OVkVFd1RuYzlQUT09");
                var response = await client.SendAsync(request);
                string json = await response.Content.ReadAsStringAsync();

                if (response == null || string.IsNullOrEmpty(json))
                {
                    result["error"] = "Server không phản hồi...";
                    return result;
                }
                if (response != null && response.StatusCode != HttpStatusCode.OK)
                {
                    result["error"] = JsonNode.Parse(json!)!.AsObject()["message"].ToString();
                    return result;
                }
                var data = JsonNode.Parse(json!)!.AsObject();
                result["coin"] = data["data"]["coin"].ToString();
                result["code"] = data["data"]["instagram_verify_code"].ToString();
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                result["error"] = ex.Message;
            }
            return result;
        }

        public async Task<List<JobModel>> GetInstagramJob(string idAccount, string token)
        {
            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://gateway.golike.net/api/advertising/publishers/instagram/jobs?instagram_account_id={idAccount}&data=null");
                request.Headers.Add("authorization", $"Bearer {token}");
                request.Headers.Add("t", "VFZSak1VMTZZek5OYW1NMFQwRTlQUT09");
                var response = await client.SendAsync(request);
                string json = await response.Content.ReadAsStringAsync();
                if (response == null || string.IsNullOrEmpty(json))
                {
                    throw new Exception("Server không phản hồi...");
                }
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception(JsonNode.Parse(json!)!.AsObject()["message"].ToString());
                }
                var jQN = JsonNode.Parse(json!)!.AsObject();
                var dataToken = jQN["data"];
                if (dataToken == null || dataToken is not JsonArray)
                {
                    throw new Exception("Dữ liệu job không hợp lệ.");
                }

                var jJobs = (JsonArray)dataToken;
                var jobs = jJobs
                    .OfType<JsonObject>()
                    .Select(j => new JobModel(j, "https://app.golike.net/"))
                    .ToList();
                return jobs;
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                throw ex;
            }
        }
        public async Task<Dictionary<string, string>> SkipInstagramJob(string idJob, string idAccount, string token)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, "https://gateway.golike.net/api/report/send");
                request.Headers.Add("authorization", $"Bearer {token}");
                request.Headers.Add("t", "VFZSak1VMTZZek5OVkVFd1RuYzlQUT09");
                var content = new StringContent("{\"description\":\"Tôi không muốn làm Job này\",\"users_advertising_id\":" + idJob + ",\"type\":\"ads\",\"provider\":\"instagram\",\"fb_id\":" + idAccount + ",\"error_type\":0}", null, "application/json");
                request.Content = content;
                var response = await client.SendAsync(request);
                string json = await response.Content.ReadAsStringAsync();
                if (response == null || string.IsNullOrEmpty(json))
                {
                    result["error"] = "Server không phản hồi...";
                    return result;
                }
                if (response != null && response.StatusCode != HttpStatusCode.OK)
                {
                    result["error"] = JsonNode.Parse(json!)!.AsObject()["message"].ToString();
                    return result;
                }
                result["success"] = "Bỏ qua job thành công.";
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                result["error"] = ex.Message;
            }
            return result;
        }
        public async Task<Dictionary<string, string>> ReportInstagramJob(string idJob, string idAccount, string token)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, "https://gateway.golike.net/api/advertising/publishers/instagram/complete-jobs");
                request.Headers.Add("authorization", $"Bearer {token}");
                request.Headers.Add("t", "VFZSak1VMTZZek5OVkVFd1RuYzlQUT09");
                var content = new StringContent("{\"instagram_users_advertising_id\":" + idJob + ",\"instagram_account_id\":" + idAccount + ",\"async\":true,\"data\":null}", null, "application /json");
                request.Content = content;
                var response = await client.SendAsync(request);
                string json = await response.Content.ReadAsStringAsync();
                if (response == null || string.IsNullOrEmpty(json))
                {
                    result["error"] = "Server không phản hồi...";
                    return result;
                }
                if (response != null && response.StatusCode != HttpStatusCode.OK)
                {
                    result["error"] = JsonNode.Parse(json!)!.AsObject()["message"].ToString();
                    return result;
                }
                result["success"] = JsonNode.Parse(json!)!.AsObject()["message"].ToString();
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                result["error"] = ex.Message;
            }
            return result;
        }

        // ===================== Threads (QN) =====================
        // Mirror của AutoQNThreads/QNClient.java: list account / get job / complete / skip.


        public async Task<List<JobModel>> GetThreadsJob(string username, string token)
        {
            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get,
                    $"https://gateway.golike.net/api/advertising/publishers/threads/_private/get-jobs?threads_username={username}");
                request.Headers.Add("authorization", $"Bearer {token}");
                request.Headers.Add("accept", "application/json");
                request.Headers.Add("g-version", "g-client");
                var response = await client.SendAsync(request);
                string json = await response.Content.ReadAsStringAsync();
                if (response == null || string.IsNullOrEmpty(json))
                {
                    throw new Exception("Server không phản hồi...");
                }
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception(JsonNode.Parse(json!)!.AsObject()["message"]?.ToString() ?? "Unknown error");
                }

                var jQN = JsonNode.Parse(json!)!.AsObject();
                var dataToken = jQN["data"];
                if (dataToken == null)
                {
                    throw new Exception(jQN["message"]?.ToString() ?? "Dữ liệu job không hợp lệ.");
                }

                // Endpoint /threads/_private/get-jobs trả về schema phẳng:
                //   { job_id, fix_coin, link, type }  — khác /instagram/jobs (id/object_id/fix_coin_job/...).
                // → map thủ công thay vì dùng JobModel(json, "https://app.golike.net/") (chỉ hiểu schema cũ).
                var jobs = new List<JobModel>();
                static JobModel MapThreadsJob(JsonObject j)
                {
                    return new JobModel
                    {
                        JobId = j["job_id"]?.ToString() ?? "",
                        Type = j["type"]?.ToString() ?? "",
                        Link = j["link"]?.ToString() ?? "",
                        ObjectId = j["link"]?.ToString() ?? "",
                        Coin = ((double?)j["fix_coin"]) ?? 0,
                    };
                }
                if (dataToken is JsonArray arr)
                {
                    foreach (var j in arr.OfType<JsonObject>())
                        jobs.Add(MapThreadsJob(j));
                }
                else if (dataToken is JsonObject obj)
                {
                    jobs.Add(MapThreadsJob(obj));
                }
                return jobs;
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                throw ex;
            }
        }

        public async Task<Dictionary<string, string>> ReportThreadsJob(string idJob, string username, string token, bool success)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post,
                    "https://gateway.golike.net/api/advertising/publishers/threads/_private/complete-jobs");
                request.Headers.Add("authorization", $"Bearer {token}");
                request.Headers.Add("accept", "application/json");
                request.Headers.Add("g-version", "g-client");
                var payload = new JsonObject
                {
                    ["job_id"] = long.TryParse(idJob, out var jid) ? JsonValue.Create(jid) : JsonValue.Create(idJob),
                    ["threads_username"] = username,
                    ["success"] = success,
                };
                var body = payload.ToJsonString();
                request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
                var response = await client.SendAsync(request);
                string json = await response.Content.ReadAsStringAsync();
                if (response == null || string.IsNullOrEmpty(json))
                {
                    result["error"] = "Server không phản hồi...";
                    return result;
                }
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    result["error"] = JsonNode.Parse(json!)!.AsObject()["message"]?.ToString() ?? "Unknown error";
                    return result;
                }
                result["success"] = JsonNode.Parse(json!)!.AsObject()["message"]?.ToString() ?? "OK";
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                result["error"] = ex.Message;
            }
            return result;
        }

    }

}
