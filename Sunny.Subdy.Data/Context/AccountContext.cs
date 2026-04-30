using Microsoft.Data.Sqlite;
using Sunny.Subdy.Data.Models;
using System;

namespace Sunny.Subdy.Data.Context
{
    public class AccountContext
    {
        private readonly AppDbContext _db;
        private readonly JobHistoryContext _jobHistoryContext;
        private const string TableName = nameof(Account);

        public AccountContext()
        {
            _db = new AppDbContext("LT_Account");
            _db.EnsureTable<Account>();
            _jobHistoryContext = new JobHistoryContext();
        }

        private static bool HasColumn(SqliteDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private Account MapToAccount(SqliteDataReader reader)
        {
            Guid.TryParse(reader["Id"]?.ToString(), out var id);

            var account = new Account
            {
                Checked = HasColumn(reader, "Checked") && reader["Checked"] != DBNull.Value && Convert.ToBoolean(reader["Checked"]),
                Running = false,
                ColorType = 0,
                Uid_Email = "",
                Id = id,
                Platformt = reader["Platformt"]?.ToString() ?? string.Empty,
                Uid = reader["Uid"]?.ToString() ?? string.Empty,
                Password = reader["Password"]?.ToString() ?? string.Empty,
                TowFA = reader["TowFA"]?.ToString() ?? string.Empty,
                Cookie = reader["Cookie"]?.ToString() ?? string.Empty,
                Token = reader["Token"]?.ToString() ?? string.Empty,
                Proxy = reader["Proxy"]?.ToString() ?? string.Empty,
                Email = reader["Email"]?.ToString() ?? string.Empty,
                Phone = reader["Phone"]?.ToString() ?? string.Empty,
                UserAgent = reader["UserAgent"]?.ToString() ?? string.Empty,
                TokenJob = reader["TokenJob"]?.ToString() ?? string.Empty,
                FullName = reader["FullName"]?.ToString() ?? string.Empty,
                State = reader["State"]?.ToString() ?? string.Empty,
                Status = reader["Status"]?.ToString() ?? string.Empty,
                Result = reader["Result"]?.ToString() ?? string.Empty,
                Serial = reader["Serial"]?.ToString() ?? string.Empty,
                IP = reader["IP"]?.ToString() ?? string.Empty,
                UserName = reader["UserName"]?.ToString() ?? string.Empty,
                NameFolder = reader["NameFolder"]?.ToString() ?? string.Empty,
                Gender = reader["Gender"]?.ToString() ?? string.Empty,
                Friends = reader["Friends"]?.ToString() ?? string.Empty,
                Groups = reader["Groups"]?.ToString() ?? string.Empty,
                Follow = reader["Follow"]?.ToString() ?? string.Empty,
                Birthday = reader["Birthday"]?.ToString() ?? string.Empty,
                PagePro5 = reader["PagePro5"]?.ToString() ?? string.Empty,
                DateCreate = reader["DateCreate"]?.ToString() ?? string.Empty,
                Avatar = reader["Avatar"]?.ToString() ?? string.Empty,
                Note = reader["Note"]?.ToString() ?? string.Empty,
                DeviceInfo = reader["DeviceInfo"]?.ToString() ?? string.Empty,
                EmailAddress = reader["EmailAddress"]?.ToString() ?? string.Empty,
                PassMail = reader["PassMail"]?.ToString() ?? string.Empty,
                MailClientId = reader["MailClientId"]?.ToString() ?? string.Empty,
                MailRefreshToken = reader["MailRefreshToken"]?.ToString() ?? string.Empty,
                PassPrivateEmailAddress = reader["PassPrivateEmailAddress"]?.ToString() ?? string.Empty,
                RecentInteraction = reader["RecentInteraction"]?.ToString() ?? string.Empty,
                IsView = reader["IsView"] != DBNull.Value && Convert.ToBoolean(reader["IsView"]),
                JobTotal = reader["JobTotal"] != DBNull.Value ? Convert.ToInt32(reader["JobTotal"]) : 0,
                NameScript = HasColumn(reader, "NameScript") ? reader["NameScript"]?.ToString() ?? string.Empty : string.Empty,
            };

          //  account.JobHistory = _jobHistoryContext.GetByUid(account.Uid, DateTime.Now.ToString("dd/MM/yyyy")) ?? new JobHistory();

            return account;
        }

        public List<Account> GetAll(string query, Dictionary<string, object>? parameters = null)
            => _db.GetAllEntities(query, MapToAccount, parameters);

        public List<Account> GetAll(List<string> nameFolders, string platform,bool? isView = true)
        {
            var parameters = new Dictionary<string, object>();
            var whereClauses = new List<string>();

            if (isView.HasValue)
            {
                whereClauses.Add("IsView = @isView");
                parameters["@isView"] = isView.Value ? 1 : 0;
            }

            if (nameFolders?.Any() == true)
            {
                var paramNames = nameFolders.Select((f, i) =>
                {
                    string key = $"@folder{i}";
                    parameters[key] = f;
                    return key;
                }).ToList();

                whereClauses.Add($"NameFolder IN ({string.Join(", ", paramNames)})");
            }
            whereClauses.Add("Platformt = @platformt");
            parameters["@platformt"] = platform;

            string query = $"SELECT * FROM {TableName}" +
                           (whereClauses.Any() ? $" WHERE {string.Join(" AND ", whereClauses)}" : "");

            return GetAll(query, parameters);
        }
        public bool UpdateIsViewFalse(List<Guid> ids)
        {
            if (ids == null || ids.Count == 0) return false;

            var parameters = new Dictionary<string, object>();
            var paramNames = ids.Select((id, i) =>
            {
                string key = $"@id{i}";
                parameters[key] = id.ToString();
                return key;
            }).ToList();

            string query = $"UPDATE {TableName} SET IsView = 0 WHERE Id IN ({string.Join(", ", paramNames)})";
            return _db.ExecuteNonQuery(query, parameters);
        }
        public bool UpdateIsViewTrue(List<Guid> ids)
        {
            if (ids == null || ids.Count == 0) return false;

            var parameters = new Dictionary<string, object>();
            var paramNames = ids.Select((id, i) =>
            {
                string key = $"@id{i}";
                parameters[key] = id.ToString();
                return key;
            }).ToList();

            string query = $"UPDATE {TableName} SET IsView = 1 WHERE Id IN ({string.Join(", ", paramNames)})";
            return _db.ExecuteNonQuery(query, parameters);
        }
        public Account? Get(Guid id)
        {
            const string query = $"SELECT * FROM {TableName} WHERE Id = @id";
            var parameters = new Dictionary<string, object> { ["@id"] = id.ToString() };
            return GetAll(query, parameters).FirstOrDefault();
        }

        public Account? GetByUid(string uid)
        {
            const string query = $"SELECT * FROM {TableName} WHERE Uid = @uid";
            var parameters = new Dictionary<string, object> { ["@uid"] = uid };
            return GetAll(query, parameters).FirstOrDefault();
        }
        public List<Account> GetByIds(List<Guid> ids)
        {
            if (ids == null || ids.Count == 0)
                return new List<Account>();
            var parameters = new Dictionary<string, object>();
            var paramNames = new List<string>();

            for (int i = 0; i < ids.Count; i++)
            {
                string paramName = $"@id{i}";
                paramNames.Add(paramName);
                parameters[paramName] = ids[i];
            }

            string inClause = string.Join(", ", paramNames);
            string query = $"SELECT * FROM {TableName} WHERE Id IN ({inClause})";

            return GetAll(query, parameters);
        }
        public int CountByFolder(string folderName)
        {
            const string query = $"SELECT COUNT(*) FROM {TableName} WHERE NameFolder = @folder";
            var parameters = new Dictionary<string, object> { ["@folder"] = folderName };
            var result = _db.ExecuteScalar(query, parameters);
            return int.TryParse(result?.ToString(), out var count) ? count : 0;
        }

        public bool Add(Account account) => _db.InsertEntity(account);
        public bool AddRange(List<Account> accounts) => _db.InsertEntities(accounts);

        public bool Update(Account account) => _db.UpdateEntity(account);
        public bool Update(List<Account> accounts) => _db.UpdateEntities(accounts);

        public bool UpdateAccountsFolderName(string oldFolder, string newFolder)
        {
            const string query = $"UPDATE {TableName} SET NameFolder = @newFolder WHERE NameFolder = @oldFolder";
            var parameters = new Dictionary<string, object>
            {
                ["@newFolder"] = newFolder,
                ["@oldFolder"] = oldFolder
            };
            return _db.ExecuteNonQuery(query, parameters);
        }

        public bool DeleteById(Guid id)
        {
            const string query = $"DELETE FROM {TableName} WHERE Id = @id";
            var parameters = new Dictionary<string, object> { ["@id"] = id.ToString() };
            return _db.ExecuteNonQuery(query, parameters);
        }

        public bool DeleteByIds(List<Guid> ids)
        {
            if (ids == null || ids.Count == 0) return false;

            var parameters = new Dictionary<string, object>();
            var paramNames = ids.Select((id, i) =>
            {
                string key = $"@id{i}";
                parameters[key] = id.ToString();
                return key;
            }).ToList();

            string query = $"DELETE FROM {TableName} WHERE Id IN ({string.Join(", ", paramNames)})";
            return _db.ExecuteNonQuery(query, parameters);
        }
    }

}
