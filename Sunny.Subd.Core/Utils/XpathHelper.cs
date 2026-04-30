namespace Sunny.Subd.Core.Utils
{
    public static class XpathHelper
    {
        // Lấy đoạn text dễ đọc bên trong XPath, ví dụ:
        //   //*[contains(@text, "We limit how often you can post")]  =>  We limit how often you can post
        //   //*[@text="Add a mobile number to your account"]         =>  Add a mobile number to your account
        // Nếu không tách được, trả về chính xpath gốc để vẫn còn thông tin debug.
        public static string ExtractReadable(string xpath)
        {
            if (string.IsNullOrWhiteSpace(xpath)) return string.Empty;
            int first = xpath.IndexOf('"');
            if (first < 0)
            {
                first = xpath.IndexOf('\'');
                if (first < 0) return xpath;
                int secondSingle = xpath.IndexOf('\'', first + 1);
                if (secondSingle <= first) return xpath;
                return xpath.Substring(first + 1, secondSingle - first - 1);
            }
            int second = xpath.IndexOf('"', first + 1);
            if (second <= first) return xpath;
            return xpath.Substring(first + 1, second - first - 1);
        }
    }
}
