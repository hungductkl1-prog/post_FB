using System;
using System.Text.RegularExpressions;

namespace Sunny.Subd.Core.Utils
{
    public static class SafeParseHelper
    {
        public static string SafeSplit(string input, char separator, int index, string fallback = "")
        {
            if (string.IsNullOrEmpty(input)) return fallback;
            var parts = input.Split(separator);
            if (index < 0 || index >= parts.Length) return fallback;
            return parts[index];
        }

        public static string SafeSplit(string input, string[] separators, int index, string fallback = "")
        {
            if (string.IsNullOrEmpty(input) || separators == null) return fallback;
            var parts = input.Split(separators, StringSplitOptions.None);
            if (index < 0 || index >= parts.Length) return fallback;
            return parts[index];
        }

        public static string SafeSubstring(string input, int startIndex)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            if (startIndex < 0) startIndex = 0;
            if (startIndex >= input.Length) return string.Empty;
            return input.Substring(startIndex);
        }

        public static string SafeSubstring(string input, int startIndex, int length)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            if (startIndex < 0) startIndex = 0;
            if (startIndex >= input.Length) return string.Empty;
            if (length < 0) length = 0;
            if (startIndex + length > input.Length) length = input.Length - startIndex;
            return input.Substring(startIndex, length);
        }

        public static bool TryRegexMatch(string input, string pattern, out string groupValue, int groupIndex = 1)
        {
            groupValue = string.Empty;
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(pattern)) return false;
            try
            {
                var m = Regex.Match(input, pattern);
                if (!m.Success) return false;
                if (groupIndex < 0 || groupIndex >= m.Groups.Count) return false;
                groupValue = m.Groups[groupIndex].Value ?? string.Empty;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static int SafeToInt(string input, int fallback = 0)
        {
            if (string.IsNullOrWhiteSpace(input)) return fallback;
            return int.TryParse(input.Trim(), out var v) ? v : fallback;
        }

        public static long SafeToLong(string input, long fallback = 0)
        {
            if (string.IsNullOrWhiteSpace(input)) return fallback;
            return long.TryParse(input.Trim(), out var v) ? v : fallback;
        }

        public static string SanitizeForDisplay(string input, int maxLength = 500)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            var sb = new System.Text.StringBuilder(Math.Min(input.Length, maxLength));
            int limit = Math.Min(input.Length, maxLength);
            for (int i = 0; i < limit; i++)
            {
                char c = input[i];
                if (c == '\r' || c == '\n' || c == '\t') { sb.Append(' '); continue; }
                if (c < 0x20) continue;
                if (c == '{' || c == '}') { sb.Append(' '); continue; }
                sb.Append(c);
            }
            if (input.Length > maxLength) sb.Append("...");
            return sb.ToString();
        }
    }
}
