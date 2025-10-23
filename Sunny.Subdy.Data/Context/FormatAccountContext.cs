using System.Data;
using Sunny.Subdy.Data.Models;

namespace Sunny.Subdy.Data.Context
{
    public class FormatAccountContext
    {
        private readonly AppDbContext _db;
        private const string TableName = nameof(FormatAccount);

        public FormatAccountContext()
        {
            _db = new AppDbContext("LT_Account");
            _db.EnsureTable<FormatAccount>();
        }

        private FormatAccount Map(IDataReader reader)
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

            return new FormatAccount
            {
                Id = id,
                Name = reader["Name"]?.ToString() ?? "",
                Fields = reader["Fields"]?.ToString() ?? ""
            };
        }

        public List<FormatAccount> GetAll()
        {
            string query = $"SELECT * FROM {TableName}";
            return _db.GetAllEntities(query, Map);
        }

        public FormatAccount? GetById(Guid id)
        {
            string query = $"SELECT * FROM {TableName} WHERE Id = @id";
            var parameters = new Dictionary<string, object> { { "@id", id.ToString() } };
            return _db.GetAllEntities(query, Map, parameters).FirstOrDefault();
        }

        public FormatAccount? GetByName(string name)
        {
            string query = $"SELECT * FROM {TableName} WHERE Name = @name";
            var parameters = new Dictionary<string, object> { { "@name", name } };
            return _db.GetAllEntities(query, Map, parameters).FirstOrDefault();
        }

        public bool Add(FormatAccount formatAccount) => _db.InsertEntity(formatAccount);

        public bool AddRange(List<FormatAccount> formats) => _db.InsertEntities(formats);

        public bool Update(FormatAccount formatAccount) => _db.UpdateEntity(formatAccount);

        public bool Update(List<FormatAccount> formats) => _db.UpdateEntities(formats);

        public bool DeleteById(Guid id)
        {
            string query = $"DELETE FROM {TableName} WHERE Id = @id";
            var parameters = new Dictionary<string, object> { { "@id", id.ToString() } };
            return _db.ExecuteNonQuery(query, parameters);
        }
    }
}
