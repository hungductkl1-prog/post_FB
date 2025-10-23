using System.Data;
using Sunny.Subdy.Data.Models;

namespace Sunny.Subdy.Data.Context
{
    public class ScriptActionContext
    {
        private readonly AppDbContext _db;
        private const string TableName = nameof(ScriptAction);

        public ScriptActionContext()
        {
            _db = new AppDbContext("LT_Scipt");
            _db.EnsureTable<ScriptAction>();
        }

        private ScriptAction MapScriptAction(IDataReader reader)
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

            try
            {
                var scriptIdStr = reader["ScriptId"]?.ToString();
                scriptId = Guid.TryParse(scriptIdStr, out var parsedScriptId) ? parsedScriptId : Guid.Empty;
            }
            catch
            {
                scriptId = Guid.Empty;
            }

            return new ScriptAction
            {
                Id = id,
                Name = reader["Name"]?.ToString() ?? "",
                Type = reader["Type"]?.ToString() ?? "",
                Json = reader["Json"]?.ToString() ?? "",
                ByOrder = reader["ByOrder"] != DBNull.Value ? Convert.ToInt32(reader["ByOrder"]) : 0,
                ScriptId = scriptId
            };
        }

        public List<ScriptAction> GetAll()
        {
            string query = $"SELECT * FROM {TableName}";
            return _db.GetAllEntities(query, MapScriptAction);
        }

        public ScriptAction? GetById(Guid id)
        {
            string query = $"SELECT * FROM {TableName} WHERE Id = @id";
            var parameters = new Dictionary<string, object> { { "@id", id.ToString() } };
            return _db.GetAllEntities(query, MapScriptAction, parameters).FirstOrDefault();
        }

        public List<ScriptAction> GetByScriptId(Guid scriptId)
        {
            string query = $"SELECT * FROM {TableName} WHERE ScriptId = @scriptId";
            var parameters = new Dictionary<string, object> { { "@scriptId", scriptId.ToString() } };
            return _db.GetAllEntities(query, MapScriptAction, parameters);
        }

        public bool Add(ScriptAction action) => _db.InsertEntity(action);

        public bool AddRange(List<ScriptAction> actions) => _db.InsertEntities(actions);

        public bool Update(ScriptAction action) => _db.UpdateEntity(action);

        public bool Update(List<ScriptAction> actions) => _db.UpdateEntities(actions);

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

        public List<ScriptAction> GetByIdsInOrder(List<Guid> orderedIds)
        {
            if (orderedIds.Count == 0) return new List<ScriptAction>();

            var placeholders = orderedIds.Select((id, index) => $"@id{index}").ToList();
            var parameters = orderedIds
                .Select((id, index) => new KeyValuePair<string, object>($"@id{index}", id.ToString()))
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            string query = $"SELECT * FROM {nameof(ScriptAction)} WHERE Id IN ({string.Join(",", placeholders)})";

            var result = _db.GetAllEntities(query, MapScriptAction, parameters);

            var resultDict = result.ToDictionary(x => x.Id, x => x);
            return orderedIds.Where(id => resultDict.ContainsKey(id)).Select(id => resultDict[id]).ToList();
        }
        public int GetCountName(string baseName)
        {
            var parameters = new Dictionary<string, object> { { "@name", baseName + "%" } };

            string query = $"SELECT COUNT(*) FROM {TableName} WHERE Name LIKE @name";

            object result = _db.ExecuteScalar(query, parameters);

            return result != null ? Convert.ToInt32(result) : 0;
        }
    }
}
