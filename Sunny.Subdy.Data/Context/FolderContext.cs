using System.Data;
using Sunny.Subdy.Data.Models;

namespace Sunny.Subdy.Data.Context
{
    public class FolderContext
    {
        private readonly AppDbContext _db;
        private const string TableName = nameof(Folder);

        public FolderContext()
        {
            _db = new AppDbContext("LT_Account");
            _db.EnsureTable<Folder>();
           
        }

        private Folder MapFolder(IDataReader reader)
        {
            Guid id;

            try
            {
                // Luôn ép về string, tránh lỗi nếu data là byte[] do lưu sai kiểu
                var idStr = reader["Id"]?.ToString();

                id = Guid.TryParse(idStr, out var parsedGuid)
                    ? parsedGuid
                    : Guid.Empty;
            }
            catch
            {
                // Nếu vẫn lỗi (do BLOB hay dữ liệu hỏng), trả về Guid.Empty để không crash
                id = Guid.Empty;
            }

            return new Folder
            {
                Id = id,
                Name = reader["Name"]?.ToString() ?? "",
                DateCreate = reader["DateCreate"]?.ToString() ?? "",
                Count = reader["Count"]?.ToString() ?? "0",
                Type = reader["Type"]?.ToString() ?? "",
                IsView = reader["IsView"] != DBNull.Value && Convert.ToBoolean(reader["IsView"])
            };
        }

        public List<Folder> GetAll()
        {
            string query = $"SELECT * FROM {TableName}";
            return _db.GetAllEntities(query, MapFolder);
        }
        public List<Folder>? GetByType(string type)
        {
            string query = $"SELECT * FROM {TableName} WHERE Type = @type";
            var parameters = new Dictionary<string, object> { { "@type", type.ToString() } };
            return _db.GetAllEntities(query, MapFolder, parameters);
        }
        public Folder? GetById(Guid id)
        {
            string query = $"SELECT * FROM {TableName} WHERE Id = @id";
            var parameters = new Dictionary<string, object> { { "@id", id.ToString() } };
            return _db.GetAllEntities(query, MapFolder, parameters).FirstOrDefault();
        }
        public Folder? GetByName(string name, string platform)
        {
            string query = $"SELECT * FROM {TableName} WHERE Name = @name AND Type = @type";
            var parameters = new Dictionary<string, object> { { "@name", name }, { "@type", platform } };
            return _db.GetAllEntities(query, MapFolder, parameters).FirstOrDefault();
        }
        public bool Add(Folder folder) => _db.InsertEntity(folder);

        public bool AddRange(List<Folder> folders) => _db.InsertEntities(folders);

        public bool Update(Folder folder) => _db.UpdateEntity(folder);

        public bool Update(List<Folder> folders) => _db.UpdateEntities(folders);

        public bool DeleteById(Guid id)
        {
            string query = $"DELETE FROM {TableName} WHERE Id = @id";
            var parameters = new Dictionary<string, object>
    {
        { "@id", id.ToString() } // ép về string
    };
            return _db.ExecuteNonQuery(query, parameters);
        }
    }
}
