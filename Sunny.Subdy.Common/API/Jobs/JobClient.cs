using System.Text.Json.Nodes;
using Sunny.Subdy.Common.API.Jobs.GoLike;
using Sunny.Subdy.Common.API.Jobs.TuongTacCheo;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sunny.Subdy.Common.API.Jobs
{

    public class JobClient
    {
        public static async Task<List<JobModel>> GetFacebookJob(string jobService, string uid, string token, string job_type = "", string prefix = "")
        {
            switch (jobService)
            {
                case "https://app.golike.net/":
                    {
                        var golikeClient = new GoLikeClient();
                        JsonNode? jGolike = await golikeClient.GetFacebookJob(uid, token, job_type);

                        if (jGolike == null)
                        {
                            throw new Exception("Không lấy được kết quả từ server.");
                        }

                        var dataToken = jGolike["data"];
                        if (dataToken is not JsonArray)
                        {
                            throw new Exception("Dữ liệu job không hợp lệ.");
                        }

                        var jJobs = (JsonArray)dataToken;
                        var jobs = jJobs
                            .OfType<JsonObject>()
                            .Select(j => new JobModel(j, jobService, job_type))
                            .ToList();

                        if (jobs.Count > 0)
                        {
                            foreach (var job in jobs)
                            {
                                if (job.Type.Contains(JobTypes.LikeComment))
                                {
                                    job.Type = JobTypes.LikeComment;
                                }
                                else if (job.Type.Contains(JobTypes.LikePage))
                                {
                                    job.Type = JobTypes.LikePage;
                                }
                                else if (job.Type.Contains(JobTypes.Like))
                                {
                                    job.Type = JobTypes.Like;
                                }
                            }

                            return jobs;
                        }

                        throw new Exception("Không có job nào.");
                    }
                case "https://tuongtaccheo.com/":
                    {
                        var client = new TuongTacCheoClient();
                        JsonNode? jResult = await client.GetFacebookJob(token, job_type, prefix);

                        if (jResult is not JsonArray)
                            throw new Exception("Dữ liệu trả về không hợp lệ hoặc không phải là mảng.");

                        var jJobs = (JsonArray)jResult;

                        var filteredJobs = jJobs
    .OfType<JsonObject>()
    .Where(j =>
    {
        // Nếu JSON không có "loaicx" thì coi như hợp lệ (sẽ gán sau)
        var loaicx = j["loaicx"]?.ToString();
        return string.IsNullOrEmpty(job_type) ||
               string.Equals(loaicx, job_type, StringComparison.OrdinalIgnoreCase) ||
               string.IsNullOrEmpty(loaicx);
    })
    .Select(j =>
    {
        // Nếu thiếu "loaicx" thì gán luôn job_type
        if (j["loaicx"] == null || string.IsNullOrEmpty(j["loaicx"]!.ToString()))
        {
            j["loaicx"] = job_type;
        }
        return new JobModel(j, jobService, job_type);
    })
    .ToList();

                        if (filteredJobs.Count > 0)
                            return filteredJobs;

                        throw new Exception("Không có job nào.");
                    }
                default:
                    throw new Exception("JobService không hợp lệ.");
            }
        }

        public static async Task<string> ReportFacebookJob(string jobService, string uid, string fullname, string token, JobModel job, string prefix)
        {
            switch (jobService)
            {
                case "https://app.golike.net/":
                    {
                        var golikeClient = new GoLikeClient();
                        JsonNode? jGolike = await golikeClient.ReportFacebookJob(uid, token, token, job);

                        if (jGolike == null)
                        {
                            throw new Exception("Không lấy được kết quả từ server.");
                        }
                        if (!string.IsNullOrEmpty(jGolike["message"]?.ToString()))
                        {
                            return jGolike["message"]?.ToString();
                        }
                        throw new Exception(jGolike.ToJsonString());
                    }
                case "https://tuongtaccheo.com/":
                    {
                        var golikeClient = new TuongTacCheoClient();
                        JsonNode? jGolike = await golikeClient.ReportFacebookJob(token, job, prefix);

                        if (jGolike == null)
                        {
                            throw new Exception("Không lấy được kết quả từ server.");
                        }
                        if (!string.IsNullOrEmpty(jGolike["mess"]?.ToString()))
                        {
                            return jGolike["mess"]?.ToString();
                        }
                        throw new Exception(jGolike.ToJsonString());
                    }
                default:
                    throw new Exception("JobService không hợp lệ.");
            }

        }

        public static List<string> GetJobTypes(string jobService, string token)
        {
            switch (jobService)
            {
                case "https://app.golike.net/":
                    {
                        var golikeClient = new GoLikeClient();
                        List<string> lines = golikeClient.GetJobTypes(token);

                        if (lines == null || !lines.Any())
                        {
                            throw new Exception("Không lấy được kết quả từ server.");
                        }
                        return lines;
                    }

                default:
                    throw new Exception("JobService không hợp lệ.");
            }
        }

        public static (double current_coin, double pending_coin) GetCoin(string jobService, string token)
        {
            switch (jobService)
            {
                case "https://app.golike.net/":
                    {
                        var golikeClient = new GoLikeClient();
                        string json = golikeClient.GetCoin(token);

                        if (string.IsNullOrWhiteSpace(json))
                            throw new Exception("Không lấy được kết quả từ server.");

                        using JsonDocument doc = JsonDocument.Parse(json);
                        JsonElement root = doc.RootElement;

                        double currentCoin = root.TryGetProperty("current_coin", out JsonElement coinEl) && coinEl.TryGetDouble(out double val)
                            ? val
                            : 0;

                        string[] platforms = new[]
                        {
                    "facebook", "instagram", "tiktok", "youtube", "twitter",
                    "shopee", "lazada", "review", "traffic", "threads",
                    "linkedin", "snapchat", "pinterest"
                };

                        double pendingTotal = 0;

                        foreach (string platform in platforms)
                        {
                            if (root.TryGetProperty(platform, out JsonElement platformEl) &&
                                platformEl.TryGetProperty("pending_coin", out JsonElement pendingEl) &&
                                pendingEl.TryGetDouble(out double pendingVal))
                            {
                                pendingTotal += pendingVal;
                            }
                        }

                        return (currentCoin, pendingTotal);
                    }

                default:
                    throw new Exception("JobService không hợp lệ.");
            }
        }
    }
}
