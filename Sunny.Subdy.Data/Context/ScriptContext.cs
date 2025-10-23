using System.Data;
using Sunny.Subdy.Data.Models;

namespace Sunny.Subdy.Data.Context
{
    public class ScriptContext
    {
        private readonly AppDbContext _db;
        private const string TableName = nameof(Script);

        public ScriptContext()
        {
            _db = new AppDbContext("LT_Scipt");
            _db.EnsureTable<Script>();
        }

        private Script MapScript(IDataReader reader)
        {
            Guid id;

            try
            {
                var idStr = reader["Id"]?.ToString();
                id = Guid.TryParse(idStr, out var parsedGuid) ? parsedGuid : Guid.Empty;
            }
            catch
            {
                id = Guid.Empty;
            }

            return new Script
            {
                Id = id,
                Platform = reader["Platform"]?.ToString() ?? "",
                Name = reader["Name"]?.ToString() ?? "",
                DateCreate = reader["DateCreate"]?.ToString() ?? "",
            };
        }

        public List<Script> GetAll()
        {
            string query = $"SELECT * FROM {TableName}";
            return _db.GetAllEntities(query, MapScript);
        }
        public List<Script>? GetByPlatform(string platform)
        {
            string query = $"SELECT * FROM {TableName} WHERE Platform = @platform";
            var parameters = new Dictionary<string, object> { { "@platform", platform } };
            return _db.GetAllEntities(query, MapScript, parameters);
        }
        public Script? GetById(Guid id)
        {
            string query = $"SELECT * FROM {TableName} WHERE Id = @id";
            var parameters = new Dictionary<string, object> { { "@id", id.ToString() } };
            return _db.GetAllEntities(query, MapScript, parameters).FirstOrDefault();
        }

        public Script? GetByName(string name, string platform)
        {
            string query = $"SELECT * FROM {TableName} WHERE Name = @name AND Platform = @platform";
            var parameters = new Dictionary<string, object> { { "@name", name }, { "@platform", platform } };
            return _db.GetAllEntities(query, MapScript, parameters).FirstOrDefault();
        }

        public bool Add(Script script) => _db.InsertEntity(script);

        public bool AddRange(List<Script> scripts) => _db.InsertEntities(scripts);

        public bool Update(Script script) => _db.UpdateEntity(script);

        public bool Update(List<Script> scripts) => _db.UpdateEntities(scripts);

        public bool DeleteById(Guid id)
        {
            string query = $"DELETE FROM {TableName} WHERE Id = @id";
            var parameters = new Dictionary<string, object> { { "@id", id.ToString() } };
            return _db.ExecuteNonQuery(query, parameters);
        }
    }

}
