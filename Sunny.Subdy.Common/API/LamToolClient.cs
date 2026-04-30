using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Sunny.Subdy.Common.API.Model;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Logs;

namespace Sunny.Subdy.Common.API
{
    public class LamToolClient
    {
        private const string Token = "sk_db7f51b03102275fcf5669ab70e2875dd624d686e3b96575d42058f550cd04b9";
        public static User Authentication(string username, string password)
        {
            try
            {
                string url = "https://lamtool.net/api/auth/login";
                var header = new Dictionary<string, string>
                {
                    ["cookie"] = "ref=lamtool",
                };
                var body = new Dictionary<string, string>
                {
                    ["username"] = username,
                    ["password"] = password
                };
                string resurl = HttpRequestHelper.POST(url, headers: header, body: body);
                if (string.IsNullOrEmpty(resurl))
                {
                    throw new Exception("Đã xảy ra lỗi server. Vui lòng thử lại hoặc liên hệ admin.");
                }
                var jObject = JsonNode.Parse(resurl)!.AsObject();
                if (!Convert.ToBoolean(jObject["success"]?.ToString())) throw new Exception(jObject["error"].ToString());
                User user = new User();
                user.UserName = username;
                user.Password = password;
                user.Id = jObject["data"]["user"]["_id"].ToString();
                user.Token = jObject["data"]["token"].ToString();
                user.Balance = double.Parse(jObject["data"]["user"]["balance"].ToString());
                user.Email = jObject["data"]["user"]["email"].ToString();
                user.Role = jObject["data"]["user"]["role"].ToString();
                return user;

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }
        public static string AddBalance(string username, double amount, string description)
        {
            try
            {
                string url = "https://lamtool.net/api/balance/add";
                var header = new Dictionary<string, string>
                {
                    ["X-API-Key"] = Token,
                    ["Content-Type"] = "application/json"
                };
                var body = new Dictionary<string, string>
                {
                    ["username"] = username,
                    ["amount"] = amount.ToString(),
                    ["description"] = description,
                };
                string resurl = HttpRequestHelper.POST(url, body: body);
                if (string.IsNullOrEmpty(resurl))
                {
                    throw new Exception("Đã xảy ra lỗi server. Vui lòng thử lại hoặc liên hệ admin.");
                }
                var jObject = JsonNode.Parse(resurl)!.AsObject();
                if (!Convert.ToBoolean(jObject["success"]?.ToString())) return jObject["error"].ToString();

                return jObject["message"].ToString();

            }
            catch (Exception ex)
            {
                return ex.Message;
            }

        }
        public static string EscapeJsonString(string s)
        {
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }
        public static (string message, string coin) SubtractBalance(string username, double amount, string description)
        {
            try
            {
                string url = "https://lamtool.net/api/balance/subtract";
                var header = new Dictionary<string, string>
                {
                    ["X-API-Key"] = Token,
                    ["Content-Type"] = "application/json",
                    ["Cookie"] = "ref=lamtool"
                };

                string json = $"{{\"username\":\"{EscapeJsonString(username)}\",\"amount\":{amount},\"description\":\"{EscapeJsonString(description)}\"}}";

                string resurl = HttpRequestHelper.POST_JSON(url, headers: header, jsonBody: json);
                if (string.IsNullOrEmpty(resurl))
                {
                    throw new Exception("Đã xảy ra lỗi server. Vui lòng thử lại hoặc liên hệ admin.");
                }

                var jObject = JsonNode.Parse(resurl)!.AsObject();
                if (!resurl.Contains("success")) return ("error: " + jObject["error"]?.ToString(), "0");
                if (!Convert.ToBoolean(jObject["success"]?.ToString())) return ("error: " + jObject["error"]?.ToString(), "0");

                return (jObject["message"].ToString(), jObject["balance"]["current"].ToString());
            }
            catch (Exception ex)
            {
                return ("error: " + ex.Message, "0");
            }
        }
        public static (bool success, string error, JsonArray licenses) GetLicenses(string bearerToken)
        {
            try
            {
                string url = "https://auto.golike.net/api/tool/licenses";
                var headers = new Dictionary<string, string>
                {
                    ["accept"] = "*/*",
                    ["Authorization"] = $"Bearer {bearerToken}",
                };
                string resurl = HttpRequestHelper.GET(url, headers: headers);
                if (string.IsNullOrEmpty(resurl))
                {
                    throw new Exception("Đã xảy ra lỗi server. Vui lòng thử lại hoặc liên hệ admin.");
                }

                var node = JsonNode.Parse(resurl);
                var arr = node?.AsArray();
                if (arr == null)
                {
                    var errObj = node?.AsObject();
                    string err = errObj?["error"]?.ToString() ?? errObj?["message"]?.ToString() ?? "Phản hồi không hợp lệ.";
                    return (false, err, null);
                }
                return (true, string.Empty, arr);
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                return (false, ex.Message, null);
            }
        }

        public static (bool valid, string error, string plan, string licenseId, string expiresAt, string toolSlug) VerifyLicense(
            string bearerToken,
            string toolSlug,
            string machineId,
            string machineLabel,
            string platform = "WINDOWS",
            string toolUserAgent = "")
        {
            try
            {
                string url = "https://auto.golike.net/api/tool/verify";
                var headers = new Dictionary<string, string>
                {
                    ["accept"] = "application/json",
                    ["Authorization"] = $"Bearer {bearerToken}",
                    ["Content-Type"] = "application/json",
                };

                string json = $"{{\"toolSlug\":\"{EscapeJsonString(toolSlug)}\"," +
                              $"\"machineId\":\"{EscapeJsonString(machineId)}\"," +
                              $"\"machineLabel\":\"{EscapeJsonString(machineLabel)}\"," +
                              $"\"platform\":\"{EscapeJsonString(platform)}\"," +
                              $"\"toolUserAgent\":\"{EscapeJsonString(toolUserAgent)}\"}}";

                string resurl = HttpRequestHelper.POST_JSON(url, headers: headers, jsonBody: json);
                if (string.IsNullOrEmpty(resurl))
                {
                    throw new Exception("Đã xảy ra lỗi server. Vui lòng thử lại hoặc liên hệ admin.");
                }

                var jObject = JsonNode.Parse(resurl)!.AsObject();
                bool valid = Convert.ToBoolean(jObject["valid"]?.ToString() ?? "false");
                if (!valid)
                {
                    string err = jObject["error"]?.ToString() ?? jObject["message"]?.ToString() ?? "License không hợp lệ.";
                    return (false, err, string.Empty, string.Empty, string.Empty, string.Empty);
                }

                return (
                    true,
                    string.Empty,
                    jObject["plan"]?.ToString() ?? string.Empty,
                    jObject["licenseId"]?.ToString() ?? string.Empty,
                    jObject["expiresAt"]?.ToString() ?? string.Empty,
                    jObject["toolSlug"]?.ToString() ?? string.Empty);
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                return (false, ex.Message, string.Empty, string.Empty, string.Empty, string.Empty);
            }
        }

        public static (bool success, string newVersion, string urlUpdate) GetApiResponseAsync(string key, string nameApp, string version)
        {
            try
            {
                string url = "https://auto.golike.net/api/public/tools/auto-phone-farm/latest?platform=WINDOWS";
                string json = HttpRequestHelper.GET(url);

                var obj = JsonNode.Parse(json)!.AsObject();

                string newVersion = obj["version"]?.ToString() ?? "";
                string updateUrl = obj["updateFile"]?["url"]?.ToString() ?? "";
                bool success = !string.IsNullOrEmpty(newVersion) && !string.IsNullOrEmpty(updateUrl);

                return (success, newVersion, updateUrl);
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                return (false, string.Empty, string.Empty);
            }
        }
        // Format: yy.MM.dd.build (vd 26.04.28.1) — so sánh trái→phải: year, month, day, build.
        public static bool IsNewerVersion(string oldVersion, string newVersion)
        {
            if (string.IsNullOrWhiteSpace(oldVersion) || string.IsNullOrWhiteSpace(newVersion))
                return false;

            string[] oldParts = oldVersion.Trim().Split('.');
            string[] newParts = newVersion.Trim().Split('.');
            int len = Math.Max(oldParts.Length, newParts.Length);

            for (int i = 0; i < len; i++)
            {
                int oldPart = i < oldParts.Length && int.TryParse(oldParts[i], out var op) ? op : 0;
                int newPart = i < newParts.Length && int.TryParse(newParts[i], out var np) ? np : 0;

                if (newPart > oldPart) return true;
                if (newPart < oldPart) return false;
            }

            return false;
        }

        public static string BuildUpsertAccountsQuery(List<AccountsVip> accounts)
        {
            if (accounts == null || accounts.Count == 0)
                return string.Empty;

            var values = string.Join(",\n", accounts.Select(a =>
                $"('{SubdyHelper.EscapeString(a.Uid)}', '{SubdyHelper.EscapeString(a.State)}', '{SubdyHelper.EscapeString(a.DeviceId)}', '{a.DateAt.ToString("yyyy-MM-dd HH:mm:ss")}')"));

            string sql = $@"
INSERT INTO accounts (uid, state, deviceid, dateAt)
VALUES {values}
ON DUPLICATE KEY UPDATE
    state = VALUES(state),
    deviceid = VALUES(deviceid),
    dateAt = VALUES(dateAt);";

            return sql;
        }
        public static string BuildFindJobsByUidsQuery(List<string> uids)
        {
            if (uids == null || uids.Count == 0)
                return string.Empty;

            var inClause = string.Join(",", uids.Select(uid => $"'{uid}'"));

            return $@"
SELECT id, uid, object_id, link, uid_profile, type, created_at
FROM jobs
WHERE uid IN ({inClause});";
        }

        public static async Task<List<JobModel>> GetTaskByUids(List<string> uids)
        {
            var jobs = new List<JobModel>();
            try
            {

                if (uids == null || uids.Count == 0)
                    return jobs;
                string sqlQuery = BuildFindJobsByUidsQuery(uids);
                if (string.IsNullOrEmpty(sqlQuery))
                    return jobs;
                string url = "https://vipfb.lamtool.net/api/db/query";

                var body = new Dictionary<string, string>
                {
                    ["sql"] = sqlQuery,
                    ["db_query_admin_password"] = "lam@300925",
                };
                string bodyJson = System.Text.Json.JsonSerializer.Serialize(body);
                string json = HttpRequestHelper.POST_JSON(url, jsonBody: bodyJson);
                if (string.IsNullOrEmpty(json))
                {
                    throw new Exception("Đã xảy ra lỗi server. Vui lòng thử lại hoặc liên hệ admin.");
                }
                var responseObj = JsonNode.Parse(json)!.AsObject();
                bool isSuccess = Convert.ToBoolean(responseObj["success"].ToString());
                if (!isSuccess)
                {
                    throw new Exception(responseObj["error"].ToString());
                }
                var dataArray = responseObj["result"]?.AsArray();
                if (dataArray != null)
                {
                    foreach (var item in dataArray)
                    {
                        var job = new JobModel
                        {
                            JobId = item["id"]?.ToString(),
                            FromId = item["uid"]?.ToString(),
                            ObjectId = item["object_id"]?.ToString(),
                            Link = item["link"]?.ToString(),
                            Type = item["type"]?.ToString()
                        };
                        jobs.Add(job);
                    }
                }
                return jobs;
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
              //  return ex.Message;
            }
            return jobs;
        }

        public static async Task<string> UpsertAccountsVipAsync(List<AccountsVip> accounts)
        {
            try
            {
                if (accounts == null || accounts.Count == 0)
                    return "Danh sách tài khoản trống.";
                string sqlQuery = BuildUpsertAccountsQuery(accounts);
                if (string.IsNullOrEmpty(sqlQuery))
                    return "Không thể xây dựng truy vấn SQL.";
                string url = "https://vipfb.lamtool.net/api/db/query";

                var body = new Dictionary<string, string>
                {
                    ["sql"] = sqlQuery,
                    ["db_query_admin_password"] = "lam@300925",
                };
                string bodyJson = System.Text.Json.JsonSerializer.Serialize(body);
                string resurl = HttpRequestHelper.POST_JSON(url, jsonBody: bodyJson);
                if (string.IsNullOrEmpty(resurl))
                {
                    throw new Exception("Đã xảy ra lỗi server. Vui lòng thử lại hoặc liên hệ admin.");
                }
                var jObject = JsonNode.Parse(resurl)!.AsObject();
                if (!Convert.ToBoolean(jObject["success"]?.ToString())) return jObject["error"].ToString();
                return jObject["success"].ToString();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public static async Task<string> DeleteTaskById(string id)
        {
            try
            {

                string url = "https://vipfb.lamtool.net/api/db/query";

                var body = new Dictionary<string, string>
                {
                    ["sql"] = $"DELETE FROM jobs WHERE id = {id}",
                    ["db_query_admin_password"] = "lam@300925",
                };
                string bodyJson = System.Text.Json.JsonSerializer.Serialize(body);
                string resurl = HttpRequestHelper.POST_JSON(url, jsonBody: bodyJson);
                if (string.IsNullOrEmpty(resurl))
                {
                    throw new Exception("Đã xảy ra lỗi server. Vui lòng thử lại hoặc liên hệ admin.");
                }
                var jObject = JsonNode.Parse(resurl)!.AsObject();
                if (!Convert.ToBoolean(jObject["success"]?.ToString())) return jObject["error"].ToString();
                return jObject["success"].ToString();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        public static List<JobModel> JobVip { get; set; } = new List<JobModel>();
    }
    public class AccountsVip
    {
        [JsonPropertyName("uid")]
        public string Uid { get; set; }
        [JsonPropertyName("state")]
        public string State { get; set; }
        [JsonPropertyName("deviceid")]
        public string DeviceId { get; set; }
        [JsonPropertyName("dateAt")]
        public DateTime DateAt { get; set; }
    }
}
