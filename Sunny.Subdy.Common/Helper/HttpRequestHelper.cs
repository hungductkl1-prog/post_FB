using Sunny.Subdy.Common.Logs;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Sunny.Subdy.Common.Helper
{
    public static class HttpRequestHelper
    {
        [DllImport("HttpRequestLib.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr HttpRequestW(
            string method, string url, string headers,
            string body, string proxy, string proxyUser, string proxyPass);

        [DllImport("HttpRequestLib.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern void FreeResponse(IntPtr ptr);

        public static string Request(
            string method, string url,
            string headers = "", string body = "",
            string proxy = "", string user = "", string pass = "")
        {
            IntPtr ptr = HttpRequestW(method, url, headers, body, proxy, user, pass);

            if (ptr == IntPtr.Zero)
            {
                LogManager.Info($"Request failed: {method} {url} (null response)");
                return null;
            }

            try
            {
                string response = Marshal.PtrToStringUni(ptr);
                LogManager.Info($"Response from {url}:\n{response}");
                return response;
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                return null;
            }
            finally
            {
                FreeResponse(ptr); // Luôn giải phóng bộ nhớ cấp phát từ DLL
            }
        }

        private static string BuildHeader(Dictionary<string, string> headerDict)
        {
            if (headerDict == null || headerDict.Count == 0) return string.Empty;

            var sb = new StringBuilder();
            foreach (var kv in headerDict)
            {
                sb.Append($"{kv.Key}: {kv.Value}\r\n");
            }
            return sb.ToString();
        }

        private static string BuildFormData(Dictionary<string, string> bodyDict)
        {
            if (bodyDict == null || bodyDict.Count == 0) return string.Empty;

            var list = new List<string>();
            foreach (var kv in bodyDict)
            {
                list.Add($"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}");
            }
            return string.Join("&", list);
        }

        public static string GET(string url, string proxy = "", Dictionary<string, string> headers = null)
        {
            string headerText = BuildHeader(headers);
            return Request("GET", url, headerText, "", proxy, "", "");
        }

        public static string POST(string url, string proxy = "", Dictionary<string, string> headers = null, Dictionary<string, string> body = null)
        {
            string headerText = BuildHeader(headers);
            string bodyText = BuildFormData(body);

            if (headers == null || !headers.ContainsKey("Content-Type"))
            {
                headerText += "Content-Type: application/x-www-form-urlencoded\r\n";
            }

            return Request("POST", url, headerText, bodyText, proxy, "", "");
        }

        public static string POST_JSON(string url, Dictionary<string, string> headers = null, string jsonBody = "")
        {
            if (headers == null)
                headers = new Dictionary<string, string>();

            if (!headers.ContainsKey("Content-Type"))
                headers["Content-Type"] = "application/json";

            string headerText = BuildHeader(headers);
            return Request("POST", url, headerText, jsonBody);
        }

        public static string GET(string url, string proxy, string user, string pass, Dictionary<string, string> headers = null)
        {
            string headerText = BuildHeader(headers);
            return Request("GET", url, headerText, "", proxy, user, pass);
        }

        public static string POST(string url, string proxy, string user, string pass, Dictionary<string, string> headers = null, Dictionary<string, string> body = null)
        {
            string headerText = BuildHeader(headers);
            string bodyText = BuildFormData(body);

            if (headers == null || !headers.ContainsKey("Content-Type"))
            {
                headerText += "Content-Type: application/x-www-form-urlencoded\r\n";
            }

            return Request("POST", url, headerText, bodyText, proxy, user, pass);
        }
    }
}
