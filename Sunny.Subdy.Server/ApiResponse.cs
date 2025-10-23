using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Sunny.Subdy.Server
{
    /// <summary>
    /// Lớp đại diện cho định dạng phản hồi JSON chuẩn.
    /// </summary>
    /// <typeparam name="T">Kiểu dữ liệu của trường Data.</typeparam>
    public class ApiResponse<T>
    {
        // Thay [JsonProperty("...")] bằng [JsonPropertyName("...")]
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("data")]
        public T Data { get; set; }

        // Constructor cho phản hồi thành công
        public ApiResponse(bool success, string message = "Operation successful.", T data = default(T))
        {
            Success = success;
            Message = message;
            Data = data;
        }

        // Constructor cho phản hồi thất bại hoặc không có dữ liệu cụ thể
        public ApiResponse(bool success, string message)
        {
            Success = success;
            Message = message;
            Data = default(T); // Mặc định là giá trị null hoặc 0
        }

        /// <summary>
        /// Tạo một phản hồi thành công.
        /// </summary>
        public static ApiResponse<T> SuccessResponse(T data = default(T), string message = "Operation successful.")
        {
            return new ApiResponse<T>(true, message, data);
        }

        /// <summary>
        /// Tạo một phản hồi thất bại.
        /// </summary>
        public static ApiResponse<T> ErrorResponse(string message = "Operation failed.", T data = default(T))
        {
            return new ApiResponse<T>(false, message, data);
        }

    }
    public class JsonResponse<T>
    {
        public bool success { get; set; }
        public string message { get; set; }
        public T data { get; set; }
    }
}
