using Microsoft.Data.Sqlite;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Logs;
using Sunny.Subdy.Data.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Sunny.Subdy.Data.Context
{
    public class JobHistoryContext
    {
        private readonly AppDbContext _db;
        private const string TableName = nameof(JobHistory);

        public JobHistoryContext()
        {
            _db = new AppDbContext("LT_JobHistory");
            _db.EnsureTable<JobHistory>();
            _db.ExecuteNonQuery($"CREATE INDEX IF NOT EXISTS idx_uid_date ON {TableName} (Uid, DateTime)");
        }

        public bool Add(JobHistory job)
        {
            job.DateTime = DateTime.Now.ToString("dd/MM/yyyy");
            return _db.InsertEntity(job);
        }

        public bool Update(JobHistory job)
        {
            return _db.UpdateEntity(job);
        }

        public bool Delete(string uid, string date)
        {
            string query = $"DELETE FROM {TableName} WHERE Uid = @uid AND DateTime = @date";
            var parameters = new Dictionary<string, object>
            {
                ["@uid"] = uid,
                ["@date"] = date
            };
            return _db.ExecuteNonQuery(query, parameters);
        }

        public List<JobHistory>? GetByUidAndDate(string uid, string date)
        {
            string query = $"SELECT * FROM {TableName} WHERE Uid = @uid AND DateTime = @date LIMIT 1";
            var parameters = new Dictionary<string, object>
            {
                ["@uid"] = uid,
                ["@date"] = date
            };
            return _db.GetAllEntities(query, MapToJob, parameters);
        }

        public JobHistory? GetByUidDateService(string uid, string date, string service)
        {
            string query = $@"
            SELECT * FROM {TableName}
            WHERE Uid = @uid AND DateTime = @date AND Service = @service
            LIMIT 1";
            var parameters = new Dictionary<string, object>
            {
                ["@uid"] = uid,
                ["@date"] = date,
                ["@service"] = service
            };
            return _db.GetAllEntities(query, MapToJob, parameters).FirstOrDefault();
        }

        public int CountByUidAndStatus(string uid, string status)
        {
            string query = $@"SELECT COUNT(*) FROM {TableName} WHERE Uid = @uid AND Status = @status";
            var parameters = new Dictionary<string, object>
            {
                ["@uid"] = uid,
                ["@status"] = status
            };

            object? result = _db.ExecuteScalar(query, parameters);
            return result != null && int.TryParse(result.ToString(), out int count) ? count : 0;
        }
        public int CountByUidAndStatusToday(string uid, string status)
        {
            string date = DateTime.Now.ToString("dd/MM/yyyy");
            string query = $@"SELECT COUNT(*) FROM {TableName} WHERE Uid = @uid AND Status = @status AND DateTime = @date LIMIT 1";
            var parameters = new Dictionary<string, object>
            {
                ["@uid"] = uid,
                ["@status"] = status,
                ["@date"] = date
            };

            object? result = _db.ExecuteScalar(query, parameters);
            return result != null && int.TryParse(result.ToString(), out int count) ? count : 0;
        }
        private JobHistory MapToJob(SqliteDataReader reader)
        {
            return new JobHistory
            {
                Id = Guid.TryParse(reader["Id"]?.ToString(), out var id) ? id : Guid.NewGuid(),
                Uid = reader["Uid"]?.ToString(),
                Platform = reader["Platform"]?.ToString(),
                Method = reader["Method"]?.ToString(),
                Description = reader["Description"]?.ToString(),
                Status = reader["Status"]?.ToString(),
                IdJob = reader["IdJob"]?.ToString(),
                IdObject = reader["IdObject"]?.ToString(),
                Coin = reader["Coin"]?.ToString(),
                Service = reader["Service"]?.ToString(),
                DateTime = reader["DateTime"]?.ToString()
            };
        }
        public Dictionary<string, int> GetHistorySummaryToDayByUid(string uid, string platform)
        {
            var parameters = new Dictionary<string, object>();
            var whereClauses = new List<string>();

            whereClauses.Add($"{nameof(JobHistory.Platform)} = @platformt");
            parameters["@platformt"] = platform;
            whereClauses.Add($"{nameof(JobHistory.DateTime)} = @date");
            parameters["@date"] = DateTime.Now.ToString("dd/MM/yyyy");
            //whereClauses.Add($"{nameof(JobHistory.Service)} = @server");
            //parameters["@server"] = service;
            whereClauses.Add($"{nameof(JobHistory.Uid)} = @uid");
            parameters["@uid"] = uid;
            string query = $"SELECT * FROM {nameof(JobHistory)}" +
                           (whereClauses.Any() ? $" WHERE {string.Join(" AND ", whereClauses)}" : "");
            var rows = GetAll(query, parameters);
            var result = new Dictionary<string, int>();
            foreach (var row in rows)
            {
                string type = "_skip"; if (row.Status == "Success") { type = ""; }
                string jobType = row.Method.ToString() + type;
                if (!result.ContainsKey(jobType))
                {
                    result[jobType] = 0;
                }
                result[jobType]++;
            }
            return result;
        }
        public Dictionary<string, int> GetJobTotals(string platformt, string server, string date)
        {
            var parameters = new Dictionary<string, object>();
            var whereClauses = new List<string>();

            whereClauses.Add($"{nameof(JobHistory.Platform)} = @platformt");
            parameters["@platformt"] = platformt;
            whereClauses.Add($"{nameof(JobHistory.DateTime)} = @date");
            parameters["@date"] = date;
            whereClauses.Add($"{nameof(JobHistory.Service)} = @server");
            parameters["@server"] = server;
            string query = $"SELECT * FROM {nameof(JobHistory)}" +
                           (whereClauses.Any() ? $" WHERE {string.Join(" AND ", whereClauses)}" : "");
            var rows = GetAll(query, parameters);
            var result = new Dictionary<string, int>();
            foreach (var row in rows)
            {
                string type = "_skip"; if (row.Status == "Success") { type = ""; }
                string jobType = row.Method.ToString() + type;
                if (!result.ContainsKey(jobType))
                {
                    result[jobType] = 0;
                }
                result[jobType]++;
            }
            return result;
        }
        public Dictionary<string, string> GetHistorySummaryToDay(string platform)
        {
            var result = new Dictionary<string, string>
            {
                ["Job Total"] = "0/0|0/0",
            };

            string dateStr = DateTime.Now.ToString("dd/MM/yyyy");

            string query = $@"
SELECT * FROM {TableName}
WHERE Platform = @platform AND DateTime = @date";

            var parameters = new Dictionary<string, object>
            {
                ["@platform"] = platform,
                ["@date"] = dateStr
            };

            var histories = _db.GetAllEntities(query, MapToJob, parameters);

            int successCount = 0, failCount = 0;
            int successCoin = 0, failCoin = 0;
            var methodCount_Success = new Dictionary<string, int>();
            var methodXu_Success = new Dictionary<string, int>();
            var methodCount_Fail = new Dictionary<string, int>();
            var methodXu_Fail = new Dictionary<string, int>();

            foreach (var history in histories)
            {
                bool isSuccess = history.Status == "Success";
                int coin = int.TryParse(history.Coin, out var c) ? c : 0;

                var method = history.Method?.Trim();
                if (string.IsNullOrEmpty(method)) continue;

                if (isSuccess)
                {
                    successCount++;
                    successCoin += coin;

                    methodCount_Success.TryAdd(method, 0);
                    methodXu_Success.TryAdd(method, 0);

                    methodCount_Success[method]++;
                    methodXu_Success[method] += coin;
                }
                else
                {
                    failCount++;
                    failCoin += coin;

                    methodCount_Fail.TryAdd(method, 0);
                    methodXu_Fail.TryAdd(method, 0);

                    methodCount_Fail[method]++;
                    methodXu_Fail[method] += coin;
                }
            }

            result["Job Total"] = $"{successCount.ToMoneyString()}/{failCount.ToMoneyString()}|{successCoin.ToMoneyString()}/{failCoin.ToMoneyString()}";

            foreach (var method in methodCount_Success)
            {
                try
                {
                    var key = method.Key;

                    var countSuccess = method.Value;
                    var xuSuccess = methodXu_Success.TryGetValue(key, out var sXu) ? sXu : 0;
                    var countFail = methodCount_Fail.TryGetValue(key, out var fCount) ? fCount : 0;
                    var xuFail = methodXu_Fail.TryGetValue(key, out var fXu) ? fXu : 0;

                    result[key] = $"{countSuccess.ToMoneyString()}/{countFail.ToMoneyString()}|{xuSuccess.ToMoneyString()}/{xuFail.ToMoneyString()}";
                }
                catch (Exception e)
                {

                }


            }

            return result;
        }
        public int GetCountSuccessByUid(string uid)
        {
            string status = "Success";
            string query = $@"
SELECT * FROM {TableName}
WHERE Uid = @uid AND Status = @status";

            var parameters = new Dictionary<string, object>
            {
                ["@uid"] = uid,
                ["@status"] = status,
            };
            int count = 0;
            try
            {
                count = _db.GetAllEntities(query, MapToJob, parameters).Count;
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
            }

            return count;
        }
        public List<JobHistory> GetAll(string query, Dictionary<string, object>? parameters = null)
    => _db.GetAllEntities(query, MapToJob, parameters);

        /// <summary>
        /// Bulk query: lấy số job success/fail hôm nay cho nhiều uid cùng lúc.
        /// Trả về Dictionary[uid] = (success, fail, coin)
        /// </summary>
        public Dictionary<string, (int success, int fail, double coin)> GetTodayCountsByUids(IEnumerable<string> uids, string platform)
        {
            var result = new Dictionary<string, (int success, int fail, double coin)>(StringComparer.OrdinalIgnoreCase);
            var uidList = uids.Where(u => !string.IsNullOrEmpty(u)).ToList();
            if (uidList.Count == 0) return result;

            string dateStr = System.DateTime.Now.ToString("dd/MM/yyyy");
            var parameters = new Dictionary<string, object>
            {
                ["@platform"] = platform,
                ["@date"] = dateStr
            };
            for (int i = 0; i < uidList.Count; i++)
                parameters[$"@uid{i}"] = uidList[i];

            string inClause = string.Join(",", Enumerable.Range(0, uidList.Count).Select(i => $"@uid{i}"));
            string query = $@"SELECT Uid, Status, Coin FROM {TableName}
WHERE Platform = @platform AND DateTime = @date AND Uid IN ({inClause})";

            var rows = _db.GetAllEntities(query, r => (
                uid: r["Uid"]?.ToString() ?? "",
                status: r["Status"]?.ToString() ?? "",
                coin: r["Coin"]?.ToString() ?? ""
            ), parameters);

            foreach (var (uid, status, coin) in rows)
            {
                if (!result.ContainsKey(uid)) result[uid] = (0, 0, 0);
                var cur = result[uid];
                double.TryParse(coin, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double coinVal);
                if (status == "Success")
                    result[uid] = (cur.success + 1, cur.fail, cur.coin + coinVal);
                else
                    result[uid] = (cur.success, cur.fail + 1, cur.coin);
            }
            return result;
        }
        public (int distinctUidCount, int totalCount) GetUidStatsByPlatform(string platform)
        {
            var parameters = new Dictionary<string, object>
            {
                ["@platform"] = platform
            };

            string queryDistinct = $@"SELECT COUNT(DISTINCT Uid) FROM {TableName} WHERE Platform = @platform";
            string queryTotal = $@"SELECT COUNT(*) FROM {TableName} WHERE Platform = @platform";

            int distinctCount = 0, totalCount = 0;

            try
            {
                object? distinctResult = _db.ExecuteScalar(queryDistinct, parameters);
                if (distinctResult != null && int.TryParse(distinctResult.ToString(), out int d))
                    distinctCount = d;

                object? totalResult = _db.ExecuteScalar(queryTotal, parameters);
                if (totalResult != null && int.TryParse(totalResult.ToString(), out int t))
                    totalCount = t;
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
            }

            return (distinctCount, totalCount);
        }
        public bool ClearAll()
        {
            try
            {
                string query = $"DELETE FROM {TableName}";
                _db.ExecuteNonQuery(query);
                return true;
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                return false;
            }
        }

        /// <summary>
        /// Lấy danh sách UID không trùng trong khoảng ngày cho một platform.
        /// fromDate / toDate định dạng "yyyy-MM-dd".
        /// </summary>
        public List<string> GetDistinctUidsByDateRange(string platform, string fromDate, string toDate)
        {
            var result = new List<string>();
            try
            {
                string query = $@"
SELECT DISTINCT {nameof(JobHistory.Uid)} FROM {TableName}
WHERE {nameof(JobHistory.Platform)} = @platform
  AND {nameof(JobHistory.Uid)} IS NOT NULL
  AND {nameof(JobHistory.Uid)} != ''
  AND date(substr({nameof(JobHistory.DateTime)}, 7, 4) || '-' || substr({nameof(JobHistory.DateTime)}, 4, 2) || '-' || substr({nameof(JobHistory.DateTime)}, 1, 2))
      BETWEEN @fromDate AND @toDate";

                var parameters = new Dictionary<string, object>
                {
                    ["@platform"] = platform,
                    ["@fromDate"] = fromDate,
                    ["@toDate"] = toDate
                };

                var rows = _db.GetAllEntities(query, r => r["Uid"]?.ToString() ?? "", parameters);
                result.AddRange(rows.Where(u => !string.IsNullOrEmpty(u)));
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
            }
            return result;
        }
    }
}
