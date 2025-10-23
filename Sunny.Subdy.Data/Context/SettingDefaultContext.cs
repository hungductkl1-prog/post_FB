using Sunny.Subdy.Data.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunny.Subdy.Data.Context
{
    public class SettingDefaultContext
    {
        private readonly AppDbContext _db;
        private const string TableName = nameof(SettingDefault);

        public SettingDefaultContext()
        {
            _db = new AppDbContext("LT_SettingDefault");
            _db.EnsureTable<SettingDefault>();
        }

        private SettingDefault MapScriptAction(IDataReader reader)
        {
            Guid id, scriptId;

            try
            {
                var idStr = reader["Id"]?.ToString();
                id = Guid.TryParse(idStr, out var parsedId) ? parsedId : Guid.Empty;
            }
            catch
            {
                id = Guid.Empty;
            }

          

            return new SettingDefault
            {
                Id = id,
                Name = reader["Name"]?.ToString() ?? "",
                Platform = reader["Platform"]?.ToString() ?? "",
                Json = reader["Json"]?.ToString() ?? "",
            };
        }

        public List<SettingDefault> GetAll()
        {
            string query = $"SELECT * FROM {TableName}";
            return _db.GetAllEntities(query, MapScriptAction);
        }
        public List<SettingDefault> GetByPlatform(string platform)
        {
            string query = $"SELECT * FROM {TableName} WHERE Platform = @platform";
            var parameters = new Dictionary<string, object> { { "@platform", platform.ToString() } };
            return _db.GetAllEntities(query, MapScriptAction, parameters);
        }

        public SettingDefault? GetById(Guid id)
        {
            string query = $"SELECT * FROM {TableName} WHERE Id = @id";
            var parameters = new Dictionary<string, object> { { "@id", id.ToString() } };
            return _db.GetAllEntities(query, MapScriptAction, parameters).FirstOrDefault();
        }

        public List<SettingDefault> GetByScriptId(Guid scriptId)
        {
            string query = $"SELECT * FROM {TableName} WHERE ScriptId = @scriptId";
            var parameters = new Dictionary<string, object> { { "@scriptId", scriptId.ToString() } };
            return _db.GetAllEntities(query, MapScriptAction, parameters);
        }

        public bool Add(SettingDefault action) => _db.InsertEntity(action);

        public bool AddRange(List<SettingDefault> actions) => _db.InsertEntities(actions);

        public bool Update(SettingDefault action) => _db.UpdateEntity(action);

        public bool Update(List<SettingDefault> actions) => _db.UpdateEntities(actions);

        public bool DeleteById(Guid id)
        {
            string query = $"DELETE FROM {TableName} WHERE Id = @id";
            var parameters = new Dictionary<string, object> { { "@id", id.ToString() } };
            return _db.ExecuteNonQuery(query, parameters);
        }

        public bool DeleteByScriptId(Guid scriptId)
        {
            string query = $"DELETE FROM {TableName} WHERE ScriptId = @scriptId";
            var parameters = new Dictionary<string, object> { { "@scriptId", scriptId.ToString() } };
            return _db.ExecuteNonQuery(query, parameters);
        }

        public List<SettingDefault> GetByIdsInOrder(List<Guid> orderedIds)
        {
            if (orderedIds.Count == 0) return new List<SettingDefault>();

            var placeholders = orderedIds.Select((id, index) => $"@id{index}").ToList();
            var parameters = orderedIds
                .Select((id, index) => new KeyValuePair<string, object>($"@id{index}", id.ToString()))
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            string query = $"SELECT * FROM {nameof(ScriptAction)} WHERE Id IN ({string.Join(",", placeholders)})";

            var result = _db.GetAllEntities(query, MapScriptAction, parameters);

            var resultDict = result.ToDictionary(x => x.Id, x => x);
            return orderedIds.Where(id => resultDict.ContainsKey(id)).Select(id => resultDict[id]).ToList();
        }
    }
   
}
