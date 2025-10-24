using Sunny.Subdy.Data.Models;
using System.Data;

namespace Sunny.Subdy.Data.Context
{
    public class DeviceModelContext
    {
        private readonly AppDbContext _db;
        private const string TableName = nameof(DeviceModel);

        public DeviceModelContext()
        {
            _db = new AppDbContext("LT_Device");
            _db.EnsureTable<DeviceModel>();
        }

        private DeviceModel MapDeviceModel(IDataReader reader)
        {
            // Đọc an toàn, tránh lỗi khi dữ liệu bị null hoặc kiểu khác
            int id = reader["Id"] != DBNull.Value ? Convert.ToInt32(reader["Id"]) : 0;
            string serial = reader["Serial"]?.ToString() ?? "";
            string nameDevice = reader["NameDevice"]?.ToString() ?? "";
            string os = reader["OS"]?.ToString() ?? "";
            string status = reader["Status"]?.ToString() ?? "";
            string state = reader["State"]?.ToString() ?? "";
            string model = reader["Model"]?.ToString() ?? "";
            string nameFolder = reader["NameFolder"]?.ToString() ?? "";

            int port = reader["Port"] != DBNull.Value ? Convert.ToInt32(reader["Port"]) : 0;
            bool isScrcpy = reader["IsScrcpy"] != DBNull.Value && Convert.ToBoolean(reader["IsScrcpy"]);
            bool isChecked = reader["Checked"] != DBNull.Value && Convert.ToBoolean(reader["Checked"]);

            // NotMapped fields (vẫn có thể gán mặc định)
            bool isControl = false;
            int color = 0;

            return new DeviceModel
            {
                Id = id,
                Serial = serial,
                NameDevice = nameDevice,
                OS = os,
                Status = status,
                State = state,
                Model = model,
                NameFolder = nameFolder,
                Port = port,
                IsScrcpy = isScrcpy,
                Checked = isChecked,
                IsControl = isControl,
                TypeColor = color
            };
        }

        // ===== CRUD =====

        public List<DeviceModel> GetAll()
        {
            string query = $"SELECT * FROM {TableName}";
            return _db.GetAllEntities(query, MapDeviceModel);
        }

        public DeviceModel? GetById(int id)
        {
            string query = $"SELECT * FROM {TableName} WHERE Id = @id";
            var parameters = new Dictionary<string, object> { { "@id", id } };
            return _db.GetAllEntities(query, MapDeviceModel, parameters).FirstOrDefault();
        }

        public DeviceModel? GetBySerial(string serial)
        {
            string query = $"SELECT * FROM {TableName} WHERE Serial = @serial";
            var parameters = new Dictionary<string, object> { { "@serial", serial } };
            return _db.GetAllEntities(query, MapDeviceModel, parameters).FirstOrDefault();
        }

        public List<DeviceModel>? GetByFolder(string folderName)
        {
            string query = $"SELECT * FROM {TableName} WHERE NameFolder = @folder";
            var parameters = new Dictionary<string, object> { { "@folder", folderName } };
            return _db.GetAllEntities(query, MapDeviceModel, parameters);
        }

        public List<DeviceModel>? GetByState(string state)
        {
            string query = $"SELECT * FROM {TableName} WHERE State = @state";
            var parameters = new Dictionary<string, object> { { "@state", state } };
            return _db.GetAllEntities(query, MapDeviceModel, parameters);
        }

        public List<DeviceModel>? GetByOS(string os)
        {
            string query = $"SELECT * FROM {TableName} WHERE OS = @os";
            var parameters = new Dictionary<string, object> { { "@os", os } };
            return _db.GetAllEntities(query, MapDeviceModel, parameters);
        }

        public bool Add(DeviceModel device) => _db.InsertEntity(device);

        public bool AddRange(List<DeviceModel> devices) => _db.InsertEntities(devices);

        public bool Update(DeviceModel device) => _db.UpdateEntity(device);

        public bool UpdateRange(List<DeviceModel> devices) => _db.UpdateEntities(devices);

        public bool DeleteById(int id)
        {
            string query = $"DELETE FROM {TableName} WHERE Id = @id";
            var parameters = new Dictionary<string, object> { { "@id", id } };
            return _db.ExecuteNonQuery(query, parameters);
        }

        public bool DeleteBySerial(string serial)
        {
            string query = $"DELETE FROM {TableName} WHERE Serial = @serial";
            var parameters = new Dictionary<string, object> { { "@serial", serial } };
            return _db.ExecuteNonQuery(query, parameters);
        }

        public bool DeleteByFolder(string folderName)
        {
            string query = $"DELETE FROM {TableName} WHERE NameFolder = @folder";
            var parameters = new Dictionary<string, object> { { "@folder", folderName } };
            return _db.ExecuteNonQuery(query, parameters);
        }
    }
}
