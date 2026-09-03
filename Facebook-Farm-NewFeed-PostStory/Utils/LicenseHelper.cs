using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Facebook_Farm_NewFeed_PostStory.Utils
{
    public static class LicenseHelper
    {
        private const string SheetId = "124xigwT5czvI8jUiCtqeD_ffOxANcXHKpBEWzVSmcW0";
        private const string CsvUrl = "https://docs.google.com/spreadsheets/d/" + SheetId + "/export?format=csv";

        /// <summary>Kiểm tra DeviceId có trong Google Sheet và còn hạn.</summary>
        public static (bool valid, string message) CheckDeviceActivation()
        {
            try
            {
                string csv = HttpRequestHelper.GET(CsvUrl);
                if (string.IsNullOrEmpty(csv))
                    return (false, "Không thể kết nối đến server. Vui lòng kiểm tra internet và thử lại.");

                var lines = csv.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length <= 1)
                    return (false, "Danh sách kích hoạt trống. Vui lòng gửi mã thiết bị cho admin.");

                string deviceId = Globals.DeviceId;
                if (string.IsNullOrEmpty(deviceId))
                    return (false, "Không xác định được mã thiết bị.");

                // Dòng 0 là header, bắt đầu từ dòng 1
                for (int i = 1; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line)) continue;

                    var cols = ParseCsvLine(line);
                    if (cols.Count < 3) continue;

                    string key = cols[0].Trim();
                    if (!string.Equals(key, deviceId, StringComparison.OrdinalIgnoreCase))
                        continue;

                    string expiryDateStr = cols[2].Trim();
                    if (DateTime.TryParseExact(expiryDateStr, "dd/MM/yyyy", null,
                        System.Globalization.DateTimeStyles.None, out var expiryDate))
                    {
                        if (DateTime.Now <= expiryDate)
                            return (true, "Thiết bị đã được kích hoạt.");
                        else
                            return (false, $"Thiết bị đã hết hạn từ {expiryDate:dd/MM/yyyy}. Vui lòng liên hệ admin để gia hạn.");
                    }
                    else
                    {
                        return (false, "Ngày hết hạn không hợp lệ. Vui lòng liên hệ admin.");
                    }
                }

                return (false, "Mã thiết bị chưa được kích hoạt. Vui lòng sao chép mã bên dưới và gửi cho admin.");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi kết nối: {ex.Message}");
            }
        }

        /// <summary>Parse 1 dòng CSV hỗ trợ quoted fields (", quotes trong field dùng "").</summary>
        private static List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var current = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    // "" trong quoted field → escaped quote
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            result.Add(current.ToString());
            return result;
        }
    }
}
