using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Logs;
using System.Net;
using System.Security.Policy;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Sunny.Subdy.Common.API.Jobs.GoLike
{
    public class GoLikeClient
    {
        public static string UrlGetJob = "https://gateway.golike.net/api/advertising/publishers/_private/get-jobs?fb_id=";
        public static string UrlReportJob = "https://gateway.golike.net/api/advertising/publishers/_private/complete-jobs";
        public static string UrlJobTypes = "https://gateway.golike.net/api/advertising/publishers/_private/get-config";

        public string GetCoin(string token)
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
                throw new Exception("Không lấy được kết quả từ server.");

            try
            {
                var responseObj = JObject.Parse(json);

                bool isSuccess = responseObj["success"]?.ToObject<bool>() == true;
                if (isSuccess)
                {
                    var currentCoin = responseObj["current_coin"]?.ToObject<int>() ?? 0;
                    return currentCoin.ToString();
                }

                return "";
            }
            catch (JsonReaderException ex)
            {
                return "";
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        public async Task<JToken> GetFacebookJob(string uid, string token, string job_type = "")
        {
            string url = $"{UrlGetJob}{uid}";

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
                var responseObj = JObject.Parse(json);

                bool isSuccess = Convert.ToBoolean(responseObj["success"]);
                if (isSuccess)
                {
                    var jobs = responseObj["data"] as JArray;
                    if (jobs != null && jobs.Any())
                        return responseObj;

                    var message = responseObj["message"]?.ToString();
                    throw new Exception(!string.IsNullOrEmpty(message) ? message : "Không có job nào được trả về.");
                }

                throw new Exception(responseObj["message"]?.ToString() ?? "Phản hồi từ server không thành công:\n" + json);
            }
            catch (JsonReaderException ex)
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
                var responseObj = JObject.Parse(json);
                bool isSuccess = responseObj["success"] != null && Convert.ToBoolean(responseObj["success"]);

                if (!isSuccess)
                    throw new Exception(responseObj["message"]?.ToString() ?? "Phản hồi không thành công:\n" + json);


                var packageArray = responseObj["data"]?["facebook"]?["package_name"] as JArray;

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
            catch (JsonReaderException ex)
            {
                throw new Exception("Lỗi phân tích JSON:\n" + ex.Message + "\nRaw:\n" + json);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<JToken> ReportFacebookJob(string uid, string fullname, string token, JobModel job)
        {
            var headers = new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {token}",
                ["ref"] = "LamToolAutoPhone",
                ["Cookie"] = "tool=dtasoftware;",
                ["dta"] = ""
            };

            var body = new Dictionary<string, string>
            {
                ["job_id"] = job.JobId,
                ["uid"] = uid,
                ["success"] = job.Success.ToString(),
                ["fb_name"] = fullname,
                ["id_text"] = "",
                ["post_private"] = job.IsView.ToString(),
                ["note"] = job.Link
            };

            if (job.Type == JobTypes.Comment && !string.IsNullOrEmpty(job.CommentId))
                body["comment_id"] = job.CommentId;

            string jsonBody = JsonConvert.SerializeObject(body);
            string json = HttpRequestHelper.POST_JSON(UrlReportJob, headers: headers, jsonBody: jsonBody);

            if (string.IsNullOrWhiteSpace(json))
                throw new Exception("Không lấy được kết quả từ server.");

            try
            {
                var responseObj = JObject.Parse(json);
                bool isSuccess = Convert.ToBoolean(responseObj["success"].ToString());

                if (isSuccess)
                    return responseObj;

                throw new Exception(responseObj["message"]?.ToString() ?? "Phản hồi không thành công:\n" + json);
            }
            catch (JsonReaderException ex)
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
                var options = new RestClientOptions("https://gateway.golike.net")
                {
                    
                };
                var client = new RestClient(options);
                var request = new RestRequest("/api/instagram-account", Method.Get);
                request.AddHeader("authorization", $"Bearer {token}");
                request.AddHeader("t", "VFZSak1VNUVUVEpPZW1kNFRsRTlQUT09");
                request.AddHeader("accept", "application/json, text/plain, */*");
                request.AddHeader("accept-language", "en-US,en;q=0.9");
               // request.AddHeader("content-type", "application/json;charset=utf-8");
                request.AddHeader("origin", "https://app.golike.net");
                request.AddHeader("sec-ch-ua", "\"Not)A;Brand\";v=\"8\", \"Chromium\";v=\"138\", \"Microsoft Edge\";v=\"138\"");
                request.AddHeader("sec-ch-ua-mobile", "?0");
                request.AddHeader("sec-ch-ua-platform", "\"Windows\"");
                request.AddHeader("sec-fetch-dest", "empty");
                request.AddHeader("sec-fetch-mode", "cors");
                request.AddHeader("sec-fetch-site", "same-site");
                request.AddHeader("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36 Edg/138.0.0.0");
                RestResponse response = await client.ExecuteAsync(request);
                string json = response.Content;
                var jObject = JObject.Parse(json);
                var data = jObject["data"];
                foreach (var item in data)
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
                    result["error"] = JObject.Parse(json!)["message"].ToString();
                    return result;
                }
                result["success"] = JObject.Parse(json!)["message"].ToString();
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
                    result["error"] = JObject.Parse(json!)["message"].ToString();
                    return result;
                }
                var data = JObject.Parse(json!);
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
                    throw new Exception(JObject.Parse(json!)["message"].ToString());
                }
                var jGolike = JObject.Parse(json!);
                var dataToken = jGolike["data"];
                if (dataToken == null || dataToken.Type != JTokenType.Array)
                {
                    throw new Exception("Dữ liệu job không hợp lệ.");
                }

                var jJobs = (JArray)dataToken;
                var jobs = jJobs
                    .OfType<JObject>()
                    .Select(j => new JobModel(j, JobServices.GoLike))
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
                    result["error"] = JObject.Parse(json!)["message"].ToString();
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
                    result["error"] = JObject.Parse(json!)["message"].ToString();
                    return result;
                }
                result["success"] = JObject.Parse(json!)["message"].ToString();
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
