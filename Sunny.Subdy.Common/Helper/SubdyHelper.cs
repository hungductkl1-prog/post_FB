using Sunny.Subdy.Common.API;
using Sunny.Subdy.Common.Models;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Sunny.Subdy.Common.Helper
{
    public class SubdyHelper
    {
        private static string icon1 = "\ud83d\ude42|\ud83d\ude00|\ud83d\ude04|\ud83d\ude06|\ud83d\ude05|\ud83d\ude02|\ud83e\udd23|\ud83d\ude0a|\ud83d\ude0c|\ud83d\ude09|\ud83d\ude0f|\ud83d\ude0d|\ud83d\ude18|\ud83d\ude17|\ud83d\ude19|\ud83d\ude1a|\ud83e\udd17|\ud83d\ude33|\ud83d\ude43|\ud83d\ude07|\ud83d\ude08|\ud83d\ude1b|\ud83d\ude1d|\ud83d\ude1c|\ud83d\ude0b|\ud83e\udd24|\ud83e\udd13|\ud83d\ude0e|\ud83e\udd11|\ud83d\ude12|\ud83d\ude41|☹\ufe0f|\ud83d\ude1e|\ud83d\ude14|\ud83d\ude16|\ud83d\ude13|\ud83d\ude22|\ud83d\ude22|\ud83d\ude2d|\ud83d\ude1f|\ud83d\ude23|\ud83d\ude29|\ud83d\ude2b|\ud83d\ude15|\ud83e\udd14|\ud83d\ude44|\ud83d\ude24|\ud83d\ude20|\ud83d\ude21|\ud83d\ude36|\ud83e\udd10|\ud83d\ude10|\ud83d\ude11|\ud83d\ude2f|\ud83d\ude32|\ud83d\ude27|\ud83d\ude28|\ud83d\ude30|\ud83d\ude31|\ud83d\ude2a|\ud83d\ude34|\ud83d\ude2c|\ud83e\udd25|\ud83e\udd27|\ud83e\udd12|\ud83d\ude37|\ud83e\udd15|\ud83d\ude35|\ud83e\udd22|\ud83e\udd20|\ud83e\udd21|\ud83d\udc7f|\ud83d\udc79|\ud83d\udc7a|\ud83d\udc7b|\ud83d\udc80|\ud83d\udc7d|\ud83d\udc7e|\ud83e\udd16|\ud83d\udca9|\ud83c\udf83";

        private static string icon2 = "♥\ufe0f|❤\ufe0f|\ud83d\udc9b|\ud83d\udc9a|\ud83d\udc99|\ud83d\udc9c|\ud83d\udda4|\ud83d\udc96|\ud83d\udc9d|\ud83d\udc94|❣\ufe0f|\ud83d\udc95|\ud83d\udc9e|\ud83d\udc93|\ud83d\udc97|\ud83d\udc98|\ud83d\udc9f|\ud83d\udc8c|\ud83d\udc8b|\ud83d\udc44|\ud83d\udc84|\ud83d\udc8d|\ud83d\udcff|\ud83c\udf81|\ud83d\udc59|\ud83d\udc57|\ud83d\udc5a|\ud83d\udc55|\ud83d\udc58|\ud83c\udfbd|\ud83d\udc58|\ud83d\udc56|\ud83d\udc60|\ud83d\udc61|\ud83d\udc62|\ud83d\udc5f|\ud83d\udc5e|\ud83d\udc52|\ud83c\udfa9|\ud83c\udf93|\ud83d\udc51|⛑\ufe0f|\ud83d\udc53|\ud83d\udd76\ufe0f|\ud83c\udf02|\ud83d\udc5b|\ud83d\udc5d|\ud83d\udc5c|\ud83d\udcbc|\ud83c\udf92|\ud83d\udecd\ufe0f|\ud83d\uded2|\ud83c\udfad|\ud83c\udfa6|\ud83c\udfa8|\ud83e\udd39|\ud83c\udf8a|\ud83c\udf89|\ud83c\udf88|\ud83c\udfa7|\ud83c\udfb7|\ud83c\udfba|\ud83c\udfb8|\ud83c\udfbb|\ud83e\udd41|\ud83c\udfb9|\ud83c\udfa4|\ud83c\udfb5|\ud83c\udfb6|\ud83c\udfbc|⚽|\ud83c\udfc0|\ud83c\udfc8|⚾|\ud83c\udfd0|\ud83c\udfc9|\ud83c\udfb1|\ud83c\udfbe|\ud83c\udff8|\ud83c\udfd3|\ud83c\udfcf|\ud83c\udfd1|\ud83c\udfd2|\ud83e\udd45|⛸\ufe0f|\ud83c\udfbf|\ud83e\udd4a|\ud83e\udd4b|⛳|\ud83c\udfb3|\ud83c\udff9|\ud83c\udfa3|\ud83c\udfaf|\ud83d\udeb5|\ud83c\udf96\ufe0f|\ud83c\udfc5|\ud83e\udd47|\ud83e\udd48|\ud83e\udd49|\ud83c\udfc6";

        private static string icon3 = "\ud83c\udf4f|\ud83c\udf4e|\ud83c\udf50|\ud83c\udf4a|\ud83c\udf4b|\ud83c\udf4c|\ud83c\udf49|\ud83c\udf47|\ud83c\udf53|\ud83c\udf48|\ud83e\udd5d|\ud83e\udd51|\ud83c\udf4d|\ud83c\udf52|\ud83c\udf51|\ud83c\udf46|\ud83e\udd52|\ud83e\udd55|\ud83c\udf36|\ud83c\udf3d|\ud83c\udf45|\ud83e\udd54|\ud83c\udf60|\ud83c\udf30|\ud83e\udd5c|\ud83c\udf6f|\ud83e\udd50|\ud83c\udf5e|\ud83e\udd56|\ud83e\uddc0|\ud83e\udd5a|\ud83c\udf73|\ud83e\udd53|\ud83c\udf64|\ud83c\udf57|\ud83c\udf56|\ud83c\udf55|\ud83c\udf2d|\ud83c\udf54|\ud83c\udf5f|\ud83e\udd59|\ud83c\udf2e|\ud83c\udf2f|\ud83e\udd57|\ud83e\udd58|\ud83c\udf5d|\ud83c\udf5c|\ud83c\udf72|\ud83c\udf63|\ud83c\udf71|\ud83c\udf5b|\ud83c\udf5a|\ud83c\udf59|\ud83c\udf58|\ud83c\udf62|\ud83c\udf61|\ud83c\udf67|\ud83c\udf68|\ud83c\udf66|\ud83e\udd5e|\ud83c\udf70|\ud83c\udf82|\ud83c\udf6e|\ud83c\udf6d|\ud83c\udf65|\ud83c\udf6c|\ud83c\udf6b|\ud83c\udf7f|\ud83c\udf69|\ud83c\udf6a|\ud83c\udf7c|\ud83e\udd5b|☕|\ud83c\udf75|\ud83c\udf76|\ud83c\udf7a|\ud83c\udf7b|\ud83e\udd42|\ud83c\udf77|\ud83e\udd43|\ud83c\udf78|\ud83c\udf79|\ud83c\udf7e|\ud83e\udd44|\ud83c\udf74|\ud83c\udf7d";

        private static string icon4 = "\ud83d\ude3a|\ud83d\ude38|\ud83d\ude39|\ud83d\ude3b|\ud83d\ude3c|\ud83d\ude3d|\ud83d\ude40|\ud83d\ude3f|\ud83d\ude3e|\ud83d\udc31|\ud83d\udc36|\ud83d\udc30|\ud83d\udc2d|\ud83d\udc39|\ud83e\udd8a|\ud83d\udc3b|\ud83d\udc3c|\ud83d\udc28|\ud83d\udc2f|\ud83e\udd81|\ud83d\udc2e|\ud83d\udc17|\ud83d\udc37|\ud83d\udc3d|\ud83d\udc38|\ud83d\udc35|\ud83d\ude48|\ud83d\ude49|\ud83d\ude4a|\ud83e\udd8d|\ud83d\udc3a|\ud83d\udc11|\ud83d\udc10|\ud83d\udc0f|\ud83d\udc34|\ud83e\udd84|\ud83e\udd8c|\ud83e\udd8f|\ud83e\udd85|\ud83d\udc24|\ud83d\udc23|\ud83d\udc25|\ud83d\udc14|\ud83d\udc13|\ud83e\udd83|\ud83d\udc26|\ud83e\udd86|\ud83e\udd87|\ud83e\udd89|\ud83d\udd4a\ufe0f|\ud83d\udc27|\ud83d\udc15|\ud83d\udc29|\ud83d\udc08|\ud83d\udc07|\ud83d\udc01|\ud83d\udc00|\ud83d\udc3f|\ud83d\udc12|\ud83d\udc16|\ud83d\udc06|\ud83d\udc05|\ud83d\udc03|\ud83d\udc02|\ud83d\udc04|\ud83d\udc0e|\ud83d\udc2a|\ud83d\udc2b|\ud83d\udc18|\ud83d\udc0a|\ud83d\udc22|\ud83d\udc20|\ud83d\udc1f|\ud83d\udc21|\ud83d\udc2c|\ud83e\udd88|\ud83d\udc33|\ud83d\udc0b|\ud83e\udd91|\ud83d\udc19|\ud83e\udd90|\ud83d\udc1a|\ud83e\udd80|\ud83e\udd82|\ud83e\udd8e|\ud83d\udc0d|\ud83d\udc1b|\ud83d\udc1c|\ud83d\udd77\ufe0f|\ud83d\udd78\ufe0f|\ud83d\udc1e|\ud83e\udd8b|\ud83d\udc1d|\ud83d\udc0c|\ud83d\udc32|\ud83d\udc09|\ud83d\udc3e";

        private static string icon5 = "\ud83c\udf3c|\ud83c\udf38|\ud83c\udf3a|\ud83c\udff5\ufe0f|\ud83c\udf3b|\ud83c\udf37|\ud83c\udf39|\ud83e\udd40|\ud83d\udc90|\ud83c\udf3e|\ud83c\udf8b|☘|\ud83c\udf40|\ud83c\udf43|\ud83c\udf42|\ud83c\udf41|\ud83c\udf31|\ud83c\udf3f|\ud83c\udf8d|\ud83c\udf35|\ud83c\udf34|\ud83c\udf33|\ud83c\udf33|\ud83c\udf84|\ud83c\udf44|\ud83c\udf0e|\ud83c\udf0d|\ud83c\udf0f|\ud83c\udf1c|\ud83c\udf1b|\ud83c\udf15|\ud83c\udf16|\ud83c\udf17|\ud83c\udf18|\ud83c\udf11|\ud83c\udf12|\ud83c\udf13|\ud83c\udf14|\ud83c\udf1a|\ud83c\udf1d|\ud83c\udf19|\ud83d\udcab|⭐|\ud83c\udf1f|✨|⚡|\ud83d\udd25|\ud83d\udca5|☄\ufe0f|\ud83c\udf1e|☀\ufe0f|\ud83c\udf24\ufe0f|⛅|\ud83c\udf25\ufe0f|\ud83c\udf26\ufe0f|☁\ufe0f|\ud83c\udf27\ufe0f|⛈\ufe0f|\ud83c\udf29\ufe0f|\ud83c\udf28\ufe0f|\ud83c\udf08|\ud83d\udca7|\ud83d\udca6|☂\ufe0f|☔|\ud83c\udf0a|\ud83c\udf2b|\ud83c\udf2a|\ud83d\udca8|❄|\ud83c\udf2c|⛄|☃\ufe0f";

        private static string icon6 = "\ud83d\ude97|\ud83d\ude95|\ud83d\ude99|\ud83d\ude8c|\ud83d\ude8e|\ud83c\udfce|\ud83d\ude93|\ud83d\ude91|\ud83d\ude92|\ud83d\ude90|\ud83d\ude9a|\ud83d\ude9b|\ud83d\ude9c|\ud83d\udef4|\ud83d\udeb2|\ud83d\udef5|\ud83c\udfcd|\ud83d\ude98|\ud83d\ude96|\ud83d\ude8d|\ud83d\ude94|\ud83d\udea8|\ud83d\udcba|✈|\ud83d\udeeb|\ud83d\udeec|\ud83d\udee9|\ud83d\ude81|\ud83d\ude80|\ud83d\udef0|\ud83d\udea1|\ud83d\udea0|\ud83d\ude9f|\ud83d\ude83|\ud83d\ude8b|\ud83d\ude9e|\ud83d\ude9d|\ud83d\ude84|\ud83d\ude85|\ud83d\ude88|\ud83d\ude82|\ud83d\ude86|\ud83d\ude8a|\ud83d\ude87|\ud83d\ude89|\ud83d\udef6|⛵|\ud83d\udee5|\ud83d\udea4|\ud83d\udea2|⛴|\ud83d\udef3|⚓|\ud83d\udea7|⛽|\ud83d\ude8f|\ud83d\udea6|\ud83d\udea5|\ud83d\udee3|\ud83d\udee4|\ud83c\udfd7|\ud83c\udfed|\ud83c\udfe0|\ud83c\udfe1|\ud83c\udfd8|\ud83c\udfda|\ud83c\udfe2|\ud83c\udfec|\ud83c\udfe4|\ud83c\udfe3|\ud83c\udfe5|\ud83c\udfe6|\ud83c\udfea|\ud83c\udfeb|\ud83c\udfe8|\ud83c\udfe9|\ud83c\udfdb|\ud83c\udff0|\ud83c\udfef|\ud83c\udfdf\ufe0f|⛪|\ud83d\udc92|\ud83d\udd4c|\ud83d\udd4d|\ud83d\udd4b|⛩|\ud83d\uddfc|\ud83d\uddff|\ud83d\uddfd|\ud83d\uddfa|\ud83c\udfaa|\ud83c\udfa0|\ud83c\udfa1|\ud83c\udfa2|⛲|⛱|\ud83c\udfd6|\ud83c\udfdd|\ud83c\udfd5|⛺|\ud83d\uddfe|⛰|\ud83c\udfd4|\ud83d\uddfb|\ud83c\udf0b|\ud83c\udfde|\ud83c\udfdc|\ud83c\udf05|\ud83c\udf04|\ud83c\udf91|\ud83c\udf20|\ud83c\udf87|\ud83c\udf86|\ud83c\udfd9|\ud83c\udf07|\ud83c\udf06|\ud83c\udf03|\ud83c\udf0c|\ud83c\udf09|\ud83c\udf01";

        private static string icon7 = "\ud83d\udcf1|\ud83d\udcf2|\ud83d\udcbb|\ud83d\udda5|⌨|\ud83d\udda8|\ud83d\uddb1|\ud83d\uddb2|\ud83d\udd79|\ud83c\udfae|\ud83d\udcbd|\ud83d\udcbe|\ud83d\udcbf|\ud83d\udcc0|\ud83d\udcfc|\ud83d\udcf7|\ud83d\udcf8|\ud83d\udcf9|\ud83c\udfa5|\ud83d\udcfd|\ud83c\udf9e|\ud83c\udfac|\ud83d\udcde|☎|\ud83d\udcdf|\ud83d\udce0|\ud83d\udcfa|\ud83d\udcfb|\ud83c\udf99|\ud83c\udf9a|\ud83c\udf9b|\ud83d\udce1|\ud83d\udce2|\ud83d\udce3|\ud83d\udd14|\ud83d\udca1|\ud83d\udd6f|\ud83d\udd26|\ud83d\udd0b|\ud83d\udd0c|⌚|⏱|⏲|⏰|\ud83d\udd70|⌛|⏳|\ud83d\udd2e|\ud83d\udc8e|\ud83c\udfb2|\ud83c\udfb0|\ud83d\udcb8|\ud83d\udcb5|\ud83d\udcb4|\ud83d\udcb6|\ud83d\udcb7|\ud83d\udcb0|\ud83d\udcb3|\ud83d\udcb2|\ud83d\udcb1|⚖|\ud83d\udd2b|\ud83d\udca3|\ud83d\udd2a|\ud83d\udde1|⚔|\ud83d\udee1|\ud83d\udeac|⚰|⚱|\ud83d\udddc\ufe0f|\ud83d\udd27|\ud83d\udd28|⚒|\ud83d\udee0|⛏|\ud83d\udd29|⚙|⛓|\ud83d\udc88|\ud83c\udf21|\ud83d\udc8a|\ud83d\udc89|⚗|\ud83d\udd2c|\ud83d\udd2d|\ud83d\udebf|\ud83d\udec1|\ud83d\udebd|\ud83d\udece|\ud83d\udd11|\ud83d\udddd|\ud83d\udeaa|\ud83d\udecb|\ud83d\udecf|\ud83d\uddbc|\ud83c\udffa|\ud83d\uddd1|\ud83d\udee2|\ud83d\udd73|\ud83c\udfee|\ud83c\udf8f|\ud83c\udf8e|\ud83c\udf90|\ud83c\udfab|\ud83c\udf9f\ufe0f|\ud83c\udf80|\ud83c\udf97\ufe0f|\ud83d\udcef|✉|\ud83d\udce9|\ud83d\udce8|\ud83d\udce7|\ud83d\udce6|\ud83d\udcea|\ud83d\udceb|\ud83d\udcec|\ud83d\udced|\ud83d\udcee|\ud83d\udce5|\ud83d\udce4|\ud83d\udcdc|\ud83d\udcc3|\ud83d\udcc4|\ud83d\udcd1|\ud83d\udcca|\ud83d\udcc8|\ud83d\udcc9|\ud83d\uddd2|\ud83d\udcc5|\ud83d\udcc6|\ud83d\uddd3|\ud83d\udcc7|\ud83d\uddc3|\ud83d\uddf3|\ud83d\uddc4|\ud83d\udccb|\ud83d\udcc1|\ud83d\udcc2|\ud83d\uddc2|\ud83d\udcd3|\ud83d\udcd4|\ud83d\udcd2|\ud83d\udcd5|\ud83d\udcd7|\ud83d\udcd8|\ud83d\udcd9|\ud83d\udcda|\ud83d\udcd6|\ud83d\uddde|\ud83d\udcf0|\ud83d\udcdd|✏|\ud83d\udd8a|\ud83d\udd8d|\ud83d\udd8c|\ud83d\udd8b|✒|\ud83d\udccc|\ud83d\udccd|\ud83d\udcce|\ud83d\udd87|\ud83d\udd16|\ud83c\udff7|\ud83d\udd17|\ud83d\udd0d|\ud83d\udd0e|\ud83d\udcd0|\ud83d\udccf|✂|\ud83d\udd12|\ud83d\udd13|\ud83d\udd0f|\ud83d\udd10";

        private static string icon8 = "\ud83d\ude42|\ud83d\ude00|\ud83d\ude04|\ud83d\ude06|\ud83d\ude05|\ud83d\ude02|\ud83e\udd23|\ud83d\ude0a|\ud83d\ude0c|\ud83d\ude09|\ud83d\ude0d|\ud83d\ude18|\ud83d\ude17|\ud83d\ude19|\ud83d\ude1a|\ud83e\udd17|\ud83d\ude33|\ud83d\ude43|\ud83d\ude1b|\ud83d\ude1d|\ud83d\ude1c|\ud83d\ude0b|\ud83e\udd24|\ud83e\udd13|\ud83d\ude0e";

        private static List<string> lstKey = new List<string> { "[r1]", "[r2]", "[r3]", "[r4]", "[r5]", "[r6]", "[r7]", "[r8]", "[d]", "[t]" };
        private static readonly Random _random = new Random();
        public static List<string> Shuffle(List<string> inputList, int count = -1)
        {
            List<string> shuffledList = new List<string>(inputList);
            int n = shuffledList.Count;
            for (int i = n - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                string temp = shuffledList[i];
                shuffledList[i] = shuffledList[j];
                shuffledList[j] = temp;
            }
            if (count == -1 || count > n)
            {
                count = n;
            }

            return shuffledList.Take(count).ToList();
        }
        public static void WriteLog(Exception A81E2315, string string_1 = "")
        {
            try
            {
                using StreamWriter streamWriter = new StreamWriter("log\\log.txt", append: true);
                streamWriter.WriteLine("-----------------------------------------------------------------------------");
                streamWriter.WriteLine("Date: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
                if (string_1 != "")
                {
                    streamWriter.WriteLine("Error: " + string_1);
                }
                streamWriter.WriteLine();
                if (A81E2315 != null)
                {
                    streamWriter.WriteLine("Type: " + A81E2315.GetType().FullName);
                    streamWriter.WriteLine("Message: " + A81E2315.Message);
                    streamWriter.WriteLine("StackTrace: " + A81E2315.StackTrace);
                    A81E2315 = A81E2315.InnerException;
                }
            }
            catch
            {
            }
        }
        public static List<string> ReadLinesFromFile(string filePath)
        {
            // Đọc toàn bộ nội dung file, tách ra từng dòng, loại bỏ dòng trống và trả về dạng List<string>
            return File.ReadAllText(filePath)
                .Split(new string[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries)
                .ToList();
        }
        public static bool IsAllDigits(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            foreach (char c in input)
            {
                if (!char.IsDigit(c))
                    return false;
            }
            return true;
        }
        public static void UpdateItemCount(TextBox sourceBox, Control targetControl, int splitMode = 0)
        {
            try
            {
                string originalText = targetControl.Text;
                List<string> items;
                if (splitMode != 0)
                {
                    items = sourceBox.Text
                        .Split(new string[] { "\n|\n" }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim())
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToList();
                }
                else
                {
                    items = sourceBox.Lines
                        .Select(x => x.Trim())
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToList();
                }
                targetControl.Text = Regex.Replace(originalText, @"\(\d*\)", $"({items.Count})");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UpdateItemCount error: {ex.Message}");
            }
        }
        public static string DecodeBase64(string base64String)
        {
            if (string.IsNullOrWhiteSpace(base64String))
                return string.Empty;

            try
            {
                byte[] bytes = Convert.FromBase64String(base64String);
                return Encoding.UTF8.GetString(bytes);
            }
            catch (FormatException)
            {
                // Chuỗi không phải Base64 hợp lệ
                return string.Empty;
            }
        }
        public static List<string> CloneList(List<string> source) =>
     source == null ? new List<string>() : source.Where(s => !string.IsNullOrEmpty(s)).ToList();
        public static string SpinText(string text)
        {
            string pattern = "{[^{}]*}";
            Match match = Regex.Match(text, pattern);
            while (match.Success)
            {
                string[] array = text.Substring(match.Index + 1, match.Length - 2).Split('|');
                text = text.Substring(0, match.Index) + array[random.Next(array.Length)] + text.Substring(match.Index + match.Length);
                match = Regex.Match(text, pattern);
            }
            text = ProcessString(text);
            return text;
        }
        private static string GetIcon(string type)
        {
            string result = "";
            List<string> list = new List<string>();
            try
            {
                switch (type)
                {
                    case "[r3]":
                        list = icon3.Split('|').ToList();
                        result = list[random.Next(0, list.Count)];
                        break;
                    case "[r8]":
                        list = icon8.Split('|').ToList();
                        result = list[random.Next(0, list.Count)];
                        break;
                    case "[t]":
                        result = DateTime.Now.ToString("HH:mm:ss");
                        break;
                    case "[r6]":
                        list = icon6.Split('|').ToList();
                        result = list[random.Next(0, list.Count)];
                        break;
                    case "[r7]":
                        list = icon7.Split('|').ToList();
                        result = list[random.Next(0, list.Count)];
                        break;
                    case "[r4]":
                        list = icon4.Split('|').ToList();
                        result = list[random.Next(0, list.Count)];
                        break;
                    case "[d]":
                        result = DateTime.Now.ToString("dd/MM/yyyy");
                        break;
                    case "[r5]":
                        list = icon5.Split('|').ToList();
                        result = list[random.Next(0, list.Count)];
                        break;
                    case "[r2]":
                        list = icon2.Split('|').ToList();
                        result = list[random.Next(0, list.Count)];
                        break;
                    case "[r1]":
                        list = icon1.Split('|').ToList();
                        result = list[random.Next(0, list.Count)];
                        break;
                }
            }
            catch
            {
                return result;
            }
            return result;
        }
        public static string ProcessString(string input)
        {
            string text = "";
            try
            {
                string text2 = "";
                for (int i = 0; i < lstKey.Count; i++)
                {
                    text2 = lstKey[i];
                    if (input.Contains(text2))
                    {
                        List<string> list = input.Split(new string[1] { text2 }, StringSplitOptions.None).ToList();
                        for (int j = 0; j < list.Count - 1; j++)
                        {
                            text = text + list[j] + GetIcon(text2);
                        }
                        text += list[list.Count - 1];
                        input = text;
                        text = "";
                    }
                }
                MatchCollection matchCollection = Regex.Matches(input, "\\[n(.*?)\\]");
                for (int k = 0; k < matchCollection.Count; k++)
                {
                    List<string> list2 = input.Split(new string[1] { matchCollection[k].Value }, StringSplitOptions.None).ToList();
                    for (int l = 0; l < list2.Count - 1; l++)
                    {
                        text = text + list2[l] + RandomString("0123456789", Convert.ToInt32(matchCollection[k].Groups[1].Value));
                    }
                    text += list2[list2.Count - 1];
                    input = text;
                    text = "";
                }
                matchCollection = Regex.Matches(input, "\\[s(.*?)\\]");
                for (int m = 0; m < matchCollection.Count; m++)
                {
                    List<string> list3 = input.Split(new string[1] { matchCollection[m].Value }, StringSplitOptions.None).ToList();
                    for (int n = 0; n < list3.Count - 1; n++)
                    {
                        text = text + list3[n] + RandomString(length: Convert.ToInt32(matchCollection[m].Groups[1].Value));
                    }
                    text += list3[list3.Count - 1];
                    input = text;
                    text = "";
                }
                matchCollection = Regex.Matches(input, "\\[q(.*?)\\]");
                for (int num = 0; num < matchCollection.Count; num++)
                {
                    List<string> list4 = input.Split(new string[1] { matchCollection[num].Value }, StringSplitOptions.None).ToList();
                    for (int num2 = 0; num2 < list4.Count - 1; num2++)
                    {
                        text = text + list4[num2] + GetNumber(matchCollection[num].Groups[1].Value);
                    }
                    text += list4[list4.Count - 1];
                    input = text;
                    text = "";
                }
                return input;
            }
            catch
            {
                return input;
            }
        }
        public static bool DeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                return true;
            }
            catch
            {
            }
            return false;
        }

        public static bool IsNumber(string pValue)
        {
            if (pValue == "")
            {
                return false;
            }
            for (int i = 0; i < pValue.Length; i++)
            {
                if (!char.IsDigit(pValue[i]))
                {
                    return false;
                }
            }
            return true;
        }
        private static string number = "0\ufe0f\u20e3|1\ufe0f\u20e3|2\ufe0f\u20e3|3\ufe0f\u20e3|4\ufe0f\u20e3|5\ufe0f\u20e3|6\ufe0f\u20e3|7\ufe0f\u20e3|8\ufe0f\u20e3|9\ufe0f\u20e3";
        private static string GetNumber(string input)
        {
            string text = "";
            try
            {
                string text2 = "";
                List<string> list = number.Split('|').ToList();
                for (int i = 0; i < input.Length; i++)
                {
                    text2 = input[i].ToString();
                    if (IsNumber(text2))
                    {
                        text2 = list[Convert.ToInt32(text2)];
                    }
                    text += text2;
                }
                return text;
            }
            catch
            {
                return text;
            }
        }
        public static int RandomValue(double min, double max)
        {
            if (min > max)
            {
                return _random.Next(Convert.ToInt32(max), Convert.ToInt32(min));
            }
            return _random.Next(Convert.ToInt32(min), Convert.ToInt32(max));
        }
        public static List<string> GetMedias(string folder)
        {
            string[] extensions = { ".jpg", ".jpeg", ".png", ".gif", ".mp4", ".mov", ".avi", ".mp3", ".wav" };

            var mediaFiles = Directory
                .EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)
                .Where(file => extensions.Contains(Path.GetExtension(file).ToLower()))
                .ToList();

          return mediaFiles;
        }
        public static string RandomString(string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789", int length = 0)
        {
            if (length == 0)
            {
                length = _random.Next(5, 50);
            }
            return new string(Enumerable.Repeat(chars, length)
               .Select(s => s[_random.Next(s.Length)]).ToArray());
        }
        public static string RandomPassword(int length = 12, bool word = true, bool digit = true, bool special = true)
        {
            if (length == 0)
            {
                return string.Empty;
            }
            string s = "";
            if (word)
            {
                s += "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            }
            if (digit)
            {
                s += "1234567890";
            }
            if (special)
            {
                s += "!@#$%^&*()_+";
            }
            if (s.Length == 0)
            {
                s = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            }
            StringBuilder res = new StringBuilder();
            while (0 < length--)
            {
                res.Append(s[_random.Next(s.Length)]);
            }
            return res.ToString();
        }
        public static string GetStringRandom(List<string> lines)
        {
            return lines[_random.Next(lines.Count)];
        }
        public static string ReplaceWithRandom(string input, int mode = 0, char mask = '*')
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            var sb = new StringBuilder(input.Length);

            foreach (char c in input)
            {
                if (c == mask)
                {
                    string replacement = mode switch
                    {
                        0 => RandomString("0123456789", 1),
                        1 => RandomString("abcdefghijklmnopqrstuvwxyz", 1),
                        2 => RandomString("abcdefghijklmnopqrstuvwxyz0123456789", 1),
                        _ => c.ToString() // fallback: giữ nguyên
                    };
                    sb.Append(replacement);
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
        public static string GetMonthNameFromNumber(string monthNumber)
        {
            var months = new Dictionary<string, string>
    {
        { "1", "january" },
        { "2", "february" },
        { "3", "march" },
        { "4", "april" },
        { "5", "may" },
        { "6", "june" },
        { "7", "july" },
        { "8", "august" },
        { "9", "september" },
        { "10", "october" },
        { "11", "november" },
        { "12", "december" }
    };

            return months.TryGetValue(monthNumber.TrimStart('0'), out var name)
                ? name
                : string.Empty;
        }

        private static readonly string[] SupportedExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp" };
        private static readonly Random random = new Random();
        public static bool ContainsAnyKeyword(string text, List<string> keywords)
        {
            try
            {
                foreach (var keyword in keywords)
                {
                    if (RemoveDiacritics(text).ToLower().Contains(RemoveDiacritics(keyword).ToLower()))
                    {
                        return true;
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
        public static string RemoveDiacritics(string input)
        {
            Regex regex = new Regex("\\p{IsCombiningDiacriticalMarks}+");
            string normalized = input.Normalize(NormalizationForm.FormD);
            return regex.Replace(normalized, string.Empty)
                        .Replace('đ', 'd')
                        .Replace('Đ', 'D');
        }
        public static string RemoveSpecialAndVietnameseChars(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            string normalized = input.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (char c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }
            string cleaned = Regex.Replace(sb.ToString(), "[^a-zA-Z0-9]", "");

            return cleaned;
        }
        public static string RandomPhoneVN()
        {
            string line = _random.Next(2) == 0 ? "84" : "0";
            line += vietnamPrefixes[_random.Next(vietnamPrefixes.Count)] + RandomString("0123456789", 7);
            return line;
        }
        public static string RandomPhoneUS()
        {
            string line = "1" + tollFreePrefixes[_random.Next(tollFreePrefixes.Count)] + RandomString("0123456789", 7);
            return line;
        }
        public static readonly List<string> tollFreePrefixes = new List<string>
{
    "800", "888", "877", "866", "855", "844", "833"
};
        public static string GetRandomImage(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                return "";

            var imageFiles = Directory.GetFiles(folderPath)
                .Where(file => SupportedExtensions.Contains(Path.GetExtension(file).ToLower()))
                .ToArray();

            if (imageFiles.Length == 0)
                return "";

            int index = random.Next(imageFiles.Length);
            return imageFiles[index];
        }
        public static List<string> GetIntersection(List<string> first, List<string> second)
        {
            if (first == null || second == null)
                return new List<string>();

            return first.Intersect(second).ToList();
        }
        public static List<string> GetDifference(List<string> first, List<string> second)
        {
            if (first == null) return new List<string>();
            if (second == null) return new List<string>(first);

            return first.Except(second).ToList();
        }
        public static string EscapeString(string str)
        {
            if (string.IsNullOrEmpty(str)) return str;
            return str.Replace("\\", "\\\\").Replace("'", "\\'");
        }
        public static List<string> JobServiceByPlatform(string platform)
        {
            return JobServices.GetTypeJobByPlatformt(platform);
        }
        public static string ExtractFacebookPostIdFromUrl(string url)
        {
            // Đảm bảo URL kết thúc bằng dấu "/"
            if (!url.EndsWith("/"))
                url += "/";

            // Các pattern để tìm ID trong URL Facebook
            List<string> patterns = new List<string>
    {
        "story_fbid=(.*?)&",
        "permalink/(.*?)/",
        "v=(.*?)&",
        "post_id=(.*?)&",
        "v=(.*?)/",
        "videos/(.*?)/",
        "videos/(.*?)\\?",
        "posts/(.*?)/",
        "posts/(.*?)\\?",
        "view_tray_pagination/(.*?)/",
        "fbid=(.*?)&",
        "multi_permalinks=(.*?)&"
    };

            string postId = "";

            foreach (var pattern in patterns)
            {
                // Ưu tiên tìm ID dạng số
                string numericPattern = pattern.Replace("(.*?)", "\\d+");
                var matchNumeric = Regex.Match(url, numericPattern);
                if (matchNumeric.Success && !matchNumeric.Value.StartsWith("0"))
                {
                    // Loại bỏ phần tiền tố và hậu tố trong match
                    postId = matchNumeric.Value;
                    var splits = pattern.Split(new[] { "(.*?)" }, StringSplitOptions.None);
                    if (splits.Length == 2)
                    {
                        postId = postId.Replace(splits[0], "").Replace(splits[1], "").Replace("?", "");
                    }
                    break;
                }
                // Nếu chưa có thì lấy group đầu tiên (chuỗi bất kỳ)
                var match = Regex.Match(url, pattern);
                if (match.Success)
                {
                    postId = match.Groups[1].Value;
                    if (!string.IsNullOrEmpty(postId) && !postId.Contains("&"))
                        break;
                }
            }

            // Nếu chưa tìm được ID mà URL là ảnh, thử lấy số cuối cùng trong đường dẫn
            if (string.IsNullOrEmpty(postId) && url.Contains("photos"))
            {
                var matches = Regex.Matches(url, "/\\d+/");
                if (matches.Count > 0)
                {
                    postId = matches[matches.Count - 1].Value.Replace("/", "");
                }
            }

            return postId;
        }
        public static readonly List<string> vietnamPrefixes = new List<string>
{
    "32", "33", "34", "35", "36", "37", "38", "39",
    "7", "76", "77", "78", "79",
    "81", "82", "83", "84", "85", "88",
    "56", "58",
    "59",
    "86", "96", "97", "98",
    "9", "93",
    "91", "94",
    "92",
    "99"
};
        public static readonly List<string> FirstnameVN = new List<string>
{
    "Nguyễn",
    "Trần",
    "Lê",
    "Phạm",
    "Hoàng",
    "Huỳnh",
    "Phan",
    "Vũ",
    "Võ",
    "Đặng",
    "Bùi",
    "Đỗ",
    "Hồ",
    "Ngô",
    "Dương",
    "Lý",
    "Cao",
    "Đinh",
    "Lưu",
    "Trương",
    "Tạ",
    "Mai",
    "Đào",
    "Nguyễn",
    "Trần",
    "Lê",
    "Phạm",
    "Hoàng",
    "Huỳnh",
    "Phan",
    "Vũ",
    "Võ",
    "Đặng",
    "Bùi",
    "Đỗ",
    "Hồ",
    "Ngô",
    "Dương",
    "Lý",
    "Cao",
    "Đinh",
    "Lưu",
    "Trương",
    "Khổng",
    "Quách",
    "Tô",
    "Lương",
    "Châu",
    "Tống",
    "Hàn",
    "Thái",
    "Kiều",
    "Tăng",
    "Mạc",
    "Triệu",
    "La",
    "Vương",
    "Uông",
    "Vi",
    "Từ",
    "Thạch"

};
        public static readonly List<string> LastnameVN = new List<string>
        {
     "Diệu Hương",
    "Khánh Ngân",
    "Ngọc Anh",
    "Thanh Hương",
    "Phương Anh",
    "Tuyết Mai",
    "Bảo Ngọc",
    "Thùy Linh",
    "Thu Trang",
    "Thảo Vy",
    "Minh Châu",
    "Quỳnh Anh",
    "Lan Chi",
    "Hồng Nhung",
    "Hoài An",
    "Mỹ Duyên",
    "Hải Yến",
    "Kim Ngân",
    "Nhật Lệ",
    "Ánh Tuyết",
    "Minh Quân",
    "Đức Anh",
    "Huy Hoàng",
    "Tuấn Anh",
    "Quang Minh",
    "Bảo Long",
    "Gia Huy",
    "Khánh Duy",
    "Anh Vũ",
    "Thanh Tùng",
    "Văn Khánh",
    "Hoàng Nam",
    "Công Thành",
    "Chí Bảo",
    "Hồng Phúc",
    "Duy Khang",
    "Tấn Phát",
    "Thành Đạt",
    "Việt Hoàng",
    "Anh Khoa",
    "Văn Thanh",
    "Minh Tuấn",
    "Quốc Huy",
    "Hoàng Nam",
    "Đức Hưng",
    "Thanh Tùng",
    "Minh Hùng",
    "Quang Duy",
    "Huy Hoàng",
    "Anh Tuấn",
    "Khắc Kiên",
    "Tấn Tài",
    "Quang Huy",
    "Trung Thành",
    "Duy Phương",
    "Tiến Thành",
    "Bảo Long",
    "Minh Khánh",
    "Khắc Tùng",
    "Đức Anh",
    "Thị Lan",
    "Thị Hương",
    "Thị Mai",
    "Thị Lan",
    "Thị Minh",
    "Thị Thu",
    "Thị Hồng",
    "Thị Bích",
    "Thị Kim",
    "Thị Yến",
    "Thị Thanh",
    "Thị Hồng",
    "Thị Phương",
    "Thị Thùy",
    "Thị Vân",
    "Thị Lan",
    "Thị Như",
    "Thị Bảo",
    "Thị Thu",
    "Thị Kim",
    "Vũ Minh",
    "Tùng Anh",
    "Bảo Trân",
    "Thanh Mai",
    "Quỳnh Anh",
    "Hồng Nhung",
    "Hải Yến",
    "Kim Ngọc",
    "Thảo Vy",
    "Quỳnh Chi",
    "Ngọc Anh",
    "Thanh Hương",
    "Phương Anh",
    "Tuyết Mai",
    "Bảo Ngọc",
    "Thùy Linh",
    "Thu Trang",
    "Thảo Vy",
    "Minh Châu",
    "Quỳnh Anh",
    "Lan Chi",
    "Hồng Nhung",
    "Hoài An",
    "Mỹ Duyên",
    "Hải Yến",
    "Kim Ngân",
    "Nhật Lệ",
    "Ánh Tuyết",
    "Quỳnh Lan",
    "Hoàng My",
    "Ngọc Lan",
    "Ngọc Hân",
    "Mai Anh",
    "Hồng Ngọc",
    "Thúy Quỳnh",
    "Thảo Nguyên",
    "Bích Ngọc",
    "Thanh Ngân",
    "Thư Kỳ",
    "Thiên Kim",
    "Hoàng Oanh",
    "Ngọc Liên",
    "Thảo Linh",
    "Thanh Bình",
    "Diễm My",
    "Diệu Linh",
    "Tường Vy",
    "Hoàng Phương",
    "Hà Linh",
    "Nguyễn Thanh",
    "Ngọc Diệp",
    "Hoàng Như",
    "Thúy Vân",
    "Tường Vi",
    "Quỳnh Mai",
    "Diễm Quỳnh",
    "Thanh Nhàn",
    "Thảo Hương",
    "Tâm Anh",
    "Bảo Tuyết",
    "Linh Chi",
    "Kim Liên",
    "Thành Hưng",
    "Bích Liên",
    "Trúc Ly",
    "Linh Trang",
    "Diệu Hương",
    "Kim Liên",
    "Thùy Linh",
    "Bích Tuyết",
    "Thu Thủy",
    "Quỳnh Dương",
    "Đoàn Thu",
    "Như Quỳnh",
    "Bích Thu",
    "Diệu Duyên",
    "Hữu Phước",
    "Minh Khang",
    "Ngọc Bích",
    "Thanh Tâm",
    "Phương Thảo",
    "Tuyết Lan",
    "Bảo Trân",
    "Thùy Dung",
    "Thu Hương",
    "Thảo Nhi",
    "Minh Thư",
    "Quỳnh Trang",
    "Lan Hương",
    "Hồng Hạnh",
    "Hoài Thương",
    "Mỹ Linh",
    "Hải Đăng",
    "Kim Anh",
    "Nhật Minh",
    "Ánh Dương",
    "Minh Tâm",
    "Đức Huy",
    "Huyền Trang",
    "Tuấn Kiệt",
    "Quang Huy",
    "Gia Bảo",
    "Khánh Linh",
    "Anh Thư",
    "Thanh Bình",
    "Diễm Hương",
    "Diệu Thảo",
    "Tường An",
    "Hoàng Dương",
    "Hà My",
    "Nguyễn Hương",
    "Ngọc Diễm",
    "Hoàng Lan",
    "Thúy Hằng",
    "Tường Vi",
    "Quỳnh Như",
    "Diễm Quỳnh",
    "Thanh Nhàn",
    "Thảo Hương",
    "Tâm Anh",
    "Bảo Tuyết",
    "Linh Chi",
    "Kim Liên",
    "Thành Hưng",
    "Bích Liên",
    "Trúc Ly",
    "Linh Trang",
    "Diệu Hương",
    "Kim Liên",
    "Thùy Linh",
    "Bích Tuyết",
    "Thu Thủy",
    "Quỳnh Dương",
    "Đoàn Thu",
    "Như Quỳnh",
    "Bích Thu",
    "Diệu Duyên",
    "Hữu Phước",
    "Minh Khang",
    "Ngọc Bích",
    "Thanh Tâm",
    "Phương Thảo",
    "Tuyết Lan",
    "Bảo Trân",
    "Thùy Dung",
    "Thu Hương",
    "Thảo Nhi",
    "Minh Thư",
    "Quỳnh Trang",
    "Lan Hương",
    "Hồng Hạnh",
    "Hoài Thương",
    "Mỹ Linh",
    "Hải Đăng",
    "Kim Anh",
    "Nhật Minh",
    "Ánh Dương",
    "Minh Tâm",
    "Đức Huy",
    "Huyền Trang",
    "Tuấn Kiệt",
    "Quang Huy",
    "Gia Bảo",
    "Khánh Linh",
    "Anh Thư",
    "Thanh Bình",
    "Diễm Hương",
    "Diệu Thảo",
    "Tường An",
    "Hoàng Dương",
    "Hà My",
    "Nguyễn Hương",
    "Ngọc Diễm",
    "Hoàng Lan",
    "Thúy Hằng",
    "Tường Vi",
    "Quỳnh Như",
    "Diễm Quỳnh",
    "Thanh Nhàn",
    "Thảo Hương",
    "Tâm Anh",
    "Bảo Tuyết",
    "Linh Chi",
    "Kim Liên",
    "Thành Hưng",
    "Bích Liên",
    "Trúc Ly",
    "Linh Trang",
    "Diệu Hương",
    "Kim Liên",
    "Thùy Linh",
    "Bích Tuyết",
    "Thu Thủy",
    "Quỳnh Dương",
    "Đoàn Thu",
    "Như Quỳnh",
    "Bích Thu",
    "Diệu Duyên",
    "Hữu Phước",
    "Minh Khang",
    "Ngọc Bích",
    "Thanh Tâm",
    "Phương Thảo",
    "Tuyết Lan",
    "Bảo Trân",
    "Thùy Dung",
    "Thu Hương",
    "Thảo Nhi",
    "Minh Thư",
    "Quỳnh Trang",
    "Lan Hương",
    "Hồng Hạnh",
    "Hoài Thương",
    "Mỹ Linh",
    "Hải Đăng",
    "Kim Anh",
    "Nhật Minh",
    "Ánh Dương",
    "Minh Tâm",
    "Đức Huy",
    "Huyền Trang",
    "Tuấn Kiệt",
    "Quang Huy",
    "Gia Bảo",
    "Khánh Linh",
    "Anh Thư",
    "Thanh Bình",
    "Diễm Hương",
    "Diệu Thảo",
    "Tường An",
    "Hoàng Dương",
    "Hà My",
    "Nguyễn Hương",
    "Ngọc Diễm",
    "Hoàng Lan",
    "Thúy Hằng",
    "Tường Vi",
    "Quỳnh Như",
    "Diễm Quỳnh",
    "Thanh Nhàn",
    "Thảo Hương",
    "Tâm Anh",
    "Bảo Tuyết",
    "Linh Chi",
    "Kim Liên",
    "Thành Hưng",
    "Bích Liên",
    "Trúc Ly",
    "Linh Trang",
    "Diệu Hương",
    "Kim Liên",
    "Thùy Linh",
    "Bích Tuyết",
    "Thu Thủy",
    "Quỳnh Dương",
    "Đoàn Thu",
    "Như Quỳnh",
    "Bích Thu",
    "Diệu Duyên",
    "Hữu Phước",
    "Minh Khang",
    "Ngọc Bích",
    "Thanh Tâm",
    "Phương Thảo",
    "Tuyết Lan",
    "Bảo Trân",
    "Thùy Dung",
    "Thu Hương",
    "Thảo Nhi",
    "Minh Thư",
    "Quỳnh Trang",
    "Lan Hương",
    "Hồng Hạnh",
    "Hoài Thương",
    "Mỹ Linh",
    "Hải Đăng",
    "Kim Anh",
    "Nhật Minh",
    "Ánh Dương",
    "Minh Tâm",
    "Đức Huy",
    "Huyền Trang",
    "Tuấn Kiệt",
    "Quang Huy",
    "Gia Bảo",
    "Khánh Linh",
    "Anh Thư",
    "Thanh Bình",
    "Diễm Hương",
    "Diệu Thảo",
    "Tường An",
    "Hoàng Dương",
    "Hà My",
    "Nguyễn Hương",
    "Ngọc Diễm",
    "Hoàng Lan",
    "Thúy Hằng",
    "Tường Vi",
    "Quỳnh Như",
    "Diễm Quỳnh",
    "Thanh Nhàn",
    "Thảo Hương",
    "Tâm Anh",
    "Bảo Tuyết",
    "Linh Chi",
    "Kim Liên",
    "Thành Hưng",
    "Bích Liên",
    "Trúc Ly",
    "Linh Trang",
    "Diệu Hương",
    "Kim Liên",
    "Thùy Linh",
    "Bích Tuyết",
    "Thu Thủy",
    "Quỳnh Dương",
    "Đoàn Thu",
    "Như Quỳnh",
    "Bích Thu",
    "Diệu Duyên",
    "Hữu Phước",
    "Minh Khang",
    "Ngọc Bích",
    "Thanh Tâm",
    "Phương Thảo",
    "Tuyết Lan",
    "Bảo Trân",
    "Thùy Dung",
    "Thu Hương",
    "Thảo Nhi",
    "Minh Thư",
    "Quỳnh Trang",
    "Lan Hương",
    "Hồng Hạnh",
    "Hoài Thương",
    "Mỹ Linh",
    "Hải Đăng",
    "Kim Anh",
    "Nhật Minh",
    "Ánh Dương",
    "Minh Tâm",
    "Đức Huy",
    "Huyền Trang",
    "Tuấn Kiệt",
    "Quang Huy",
    "Gia Bảo",
    "Khánh Linh",
    "Anh Thư",
    "Thanh Bình",
    "Diễm Hương",
    "Diệu Thảo",
    "Tường An",
    "Hoàng Dương",
    "Hà My",
    "Nguyễn Hương",
    "Ngọc Diễm",
    "Hoàng Lan",
    "Thúy Hằng",
    "Tường Vi",
    "Quỳnh Như",
    "Diễm Quỳnh",
    "Thanh Nhàn",
    "Thảo Hương",
    "Tâm Anh",
    "Bảo Tuyết",
    "Linh Chi",
    "Kim Liên",
    "Thành Hưng",
    "Bích Liên",
    "Trúc Ly",
    "Linh Trang",
    "Diệu Hương",
    "Kim Liên",
    "Thùy Linh",
    "Bích Tuyết",
    "Thu Thủy",
    "Quỳnh Dương",
    "Đoàn Thu",
    "Như Quỳnh",
    "Bích Thu",
    "Diệu Duyên",
    "Hữu Phước",
    "Minh Khang",
    "Ngọc Bích",
    "Thanh Tâm",
    "Phương Thảo",
    "Tuyết Lan",
    "Bảo Trân",
    "Thùy Dung",
    "Thu Hương",
    "Thảo Nhi",
    "Minh Thư",
    "Quỳnh Trang",
    "Lan Hương",
    "Hồng Hạnh",
    "Hoài Thương",
    "Mỹ Linh",
    "Hải Đăng",
    "Kim Anh",
    "Nhật Minh",
    "Ánh Dương",
    "Minh Tâm",
    "Đức Huy",
    "Huyền Trang",
    "Tuấn Kiệt",
    "Quang Huy",
    "Gia Bảo",
    "Khánh Linh",
    "Anh Thư",
    "Thanh Bình",
    "Diễm Hương",
    "Diệu Thảo",
    "Tường An",
    "Hoàng Dương",
    "Hà My",
    "Nguyễn Hương",
    "Ngọc Diễm",
    "Hoàng Lan",
    "Thúy Hằng",
    "Tường Vi",
    "Quỳnh Như",
    "Diễm Quỳnh",
    "Thanh Nhàn",
    "Thảo Hương",
    "Tâm Anh",
    "Bảo Tuyết",
    "Linh Chi",
    "Kim Liên",
    "Thành Hưng",
    "Bích Liên",
    "Trúc Ly",
    "Linh Trang",
    "Diệu Hương",
    "Kim Liên",
    "Thùy Linh",
    "Bích Tuyết",
    "Thu Thủy",
    "Quỳnh Dương",
    "Đoàn Thu",
    "Như Quỳnh",
    "Bích Thu",
    "Diệu Duyên",
    "Hữu Phước",
    "Minh Khang",
    "Ngọc Bích",
    "Thanh Tâm",
    "Phương Thảo",
    "Tuyết Lan",
    "Bảo Trân",
    "Thùy Dung",
    "Thu Hương",
    "Thảo Nhi",
    "Minh Thư",
    "Quỳnh Trang",
    "Lan Hương",
    "Hồng Hạnh",
    "Hoài Thương",
    "Mỹ Linh",
    "Văn",
    "Ngọc",
    "Thanh",
    "Hữu",
    "Minh",
    "Anh",
    "Tuấn",
    "Quốc",
    "Đức",
    "Gia",
    "Trọng",
    "Phúc",
    "Khánh",
    "Xuân",
    "Bảo",
    "Duy",
    "Công",
    "Chí",
    "Tấn",
    "Nhật",
    "Huy",
    "Kim",
    "Mỹ",
    "Phương",
    "Tuyết",
    "Quỳnh",
    "Hải",
    "Lam",
    "Thiên",
    "Diệu",
    "Hoàng",
    "Trung",
    "Anh",
    "Bảo",
    "Cường",
    "Đức",
    "Duy",
    "Hoàng",
    "Huy",
    "Hùng",
    "Khánh",
    "Khoa",
    "Long",
    "Minh",
    "Nam",
    "Nguyên",
    "Phúc",
    "Quang",
    "Sơn",
    "Tâm",
    "Thắng",
    "Thành",
    "Thiện",
    "Tiến",
    "Toàn",
    "Trung",
    "Tuấn",
    "Việt",
    "Vinh",
    "An",
    "Anh",
    "Bích",
    "Chi",
    "Diệp",
    "Dung",
    "Duyên",
    "Giang",
    "Hạnh",
    "Hoa",
    "Hương",
    "Khánh",
    "Lan",
    "Linh",
    "Ly",
    "Mai",
    "My",
    "Ngọc",
    "Nhung",
    "Nhi",
    "Oanh",
    "Phương",
    "Quỳnh",
    "Thảo",
    "Thư",
    "Thùy",
    "Trang",
    "Tuyết",
    "Vy",
    "Yến",
    "Diệu Hương",
    "Khánh Ngân",
    "Ngọc Anh",
    "Thanh Hương",
    "Phương Anh",
    "Tuyết Mai",
    "Bảo Ngọc",
    "Thùy Linh",
    "Thu Trang",
    "Thảo Vy",
    "Minh Châu",
    "Quỳnh Anh",
    "Lan Chi",
    "Hồng Nhung",
    "Hoài An",
    "Mỹ Duyên",
    "Hải Yến",
    "Kim Ngân",
    "Nhật Lệ",
    "Ánh Tuyết",
    "Minh Quân",
    "Đức Anh",
    "Huy Hoàng",
    "Tuấn Anh",
    "Quang Minh",
    "Bảo Long",
    "Gia Huy",
    "Khánh Duy",
    "Anh Vũ",
    "Thanh Tùng",
    "Văn Khánh",
    "Hoàng Nam",
    "Công Thành",
    "Chí Bảo",
    "Hồng Phúc",
    "Duy Khang",
    "Tấn Phát",
    "Thành Đạt",
    "Việt Hoàng",
    "Anh Khoa",
    "Văn Thanh",
    "Minh Tuấn",
    "Quốc Huy",
    "Hoàng Nam",
    "Đức Hưng",
    "Thanh Tùng",
    "Minh Hùng",
    "Quang Duy",
    "Huy Hoàng",
    "Anh Tuấn",
    "Khắc Kiên",
    "Tấn Tài",
    "Quang Huy",
    "Trung Thành",
    "Duy Phương",
    "Tiến Thành",
    "Bảo Long",
    "Minh Khánh",
    "Khắc Tùng",
    "Đức Anh",
    "Thị Lan",
    "Thị Hương",
    "Thị Mai",
    "Thị Lan",
    "Thị Minh",
    "Thị Thu",
    "Thị Hồng",
    "Thị Bích",
    "Thị Kim",
    "Thị Yến",
    "Thị Thanh",
    "Thị Hồng",
    "Thị Phương",
    "Thị Thùy",
    "Thị Vân",
    "Thị Lan",
    "Thị Như",
    "Thị Bảo",
    "Thị Thu",
    "Thị Kim",
    "Vũ Minh",
    "Tùng Anh",
    "Bảo Trân",
    "Thanh Mai",
    "Quỳnh Anh",
    "Hồng Nhung",
    "Hải Yến",
    "Kim Ngọc",
    "Thảo Vy",
    "Quỳnh Chi",
    "Ngọc Anh",
    "Thanh Hương",
    "Phương Anh",
    "Tuyết Mai",
    "Bảo Ngọc",
    "Thùy Linh",
    "Thu Trang",
    "Thảo Vy",
    "Minh Châu",
    "Quỳnh Anh",
    "Lan Chi",
    "Hồng Nhung",
    "Hoài An",
    "Mỹ Duyên",
    "Hải Yến",
    "Kim Ngân",
    "Nhật Lệ",
    "Ánh Tuyết",
    "Quỳnh Lan",
    "Hoàng My",
    "Ngọc Lan",
    "Ngọc Hân",
    "Mai Anh",
    "Hồng Ngọc",
    "Thúy Quỳnh",
    "Thảo Nguyên",
    "Bích Ngọc",
    "Thanh Ngân",
    "Thư Kỳ",
    "Thiên Kim",
    "Hoàng Oanh",
    "Ngọc Liên",
    "Thảo Linh",
    "Thanh Bình",
    "Diễm My",
    "Diệu Linh",
    "Tường Vy",
    "Hoàng Phương",
    "Hà Linh",
    "Nguyễn Thanh",
    "Ngọc Diệp",
    "Hoàng Như",
    "Thúy Vân",
    "Tường Vi",
    "Quỳnh Mai",
    "Diễm Quỳnh",
    "Thanh Nhàn",
    "Thảo Hương",
    "Tâm Anh",
    "Bảo Tuyết",
    "Linh Chi",
    "Kim Liên",
    "Thành Hưng",
    "Bích Liên",
    "Trúc Ly",
    "Linh Trang",
    "Diệu Hương",
    "Kim Liên",
    "Thùy Linh",
    "Bích Tuyết",
    "Thu Thủy",
    "Quỳnh Dương",
    "Đoàn Thu",
    "Như Quỳnh",
    "Bích Thu",
    "Diệu Duyên",
    "Hữu Phước",
    "Minh Khang",
    "Ngọc Bích",
    "Thanh Tâm",
    "Phương Thảo",
    "Tuyết Lan",
    "Bảo Trân",
    "Thùy Dung",
    "Thu Hương",
    "Thảo Nhi",
    "Minh Thư",
    "Quỳnh Trang",
    "Lan Hương",
    "Hồng Hạnh",
    "Hoài Thương",
    "Mỹ Linh",
    "Hải Đăng",
    "Kim Anh",
    "Nhật Minh",
    "Ánh Dương",
    "Minh Tâm",
    "Đức Huy",
    "Huyền Trang",
    "Tuấn Kiệt",
    "Quang Huy",
    "Gia Bảo",
    "Khánh Linh",
    "Anh Thư",
    "Thanh Bình",
    "Diễm Hương",
    "Diệu Thảo",
    "Tường An",
    "Hoàng Dương",
    "Hà My",
    "Nguyễn Hương",
    "Ngọc Diễm",
    "Hoàng Lan",
    "Thúy Hằng",
    "Tường Vi",
    "Quỳnh Như",
    "Diễm Quỳnh",
    "Thanh Nhàn",
    "Thảo Hương",
    "Tâm Anh",
    "Bảo Tuyết",
    "Linh Chi",
    "Kim Liên",
    "Thành Hưng",
    "Bích Liên",
    "Trúc Ly",
    "Linh Trang",
    "Diệu Hương",
    "Kim Liên",
    "Thùy Linh",
    "Bích Tuyết",
    "Thu Thủy",
    "Quỳnh Dương",
    "Đoàn Thu",
    "Như Quỳnh",
    "Bích Thu",
    "Diệu Duyên",
    "Hữu Phước",
    "Minh Khang",
    "Ngọc Bích",
    "Thanh Tâm",
    "Phương Thảo",
    "Tuyết Lan",
    "Bảo Trân",
    "Thùy Dung",
    "Thu Hương",
    "Thảo Nhi",
    "Minh Thư",
    "Quỳnh Trang",
    "Lan Hương",
    "Hồng Hạnh",
    "Hoài Thương",
    "Mỹ Linh",
    "Hải Đăng",
    "Kim Anh",
    "Nhật Minh",
    "Ánh Dương",
    "Minh Tâm",
    "Đức Huy",
    "Huyền Trang",
    "Tuấn Kiệt",
    "Quang Huy",
    "Gia Bảo",
    "Khánh Linh",
    "Anh Thư",
    "Thanh Bình",
    "Diễm Hương",
    "Diệu Thảo",
    "Tường An",
    "Hoàng Dương",
    "Hà My",
    "Nguyễn Hương",
    "Ngọc Diễm",
    "Hoàng Lan",
    "Thúy Hằng",
    "Tường Vi",
    "Quỳnh Như",
    "Diễm Quỳnh",
    "Thanh Nhàn",
    "Thảo Hương",
    "Tâm Anh",
    "Bảo Tuyết",
    "Linh Chi",
    "Kim Liên",
    "Thành Hưng",
    "Bích Liên",
    "Trúc Ly",
    "Linh Trang",
    "Diệu Hương",
    "Kim Liên",
    "Thùy Linh",
    "Bích Tuyết",
    "Thu Thủy",
    "Quỳnh Dương",
    "Đoàn Thu",
    "Như Quỳnh",
    "Bích Thu",
    "Diệu Duyên",
    "Hữu Phước",
    "Minh Khang",
    "Ngọc Bích",
    "Thanh Tâm",
    "Phương Thảo",
    "Tuyết Lan",
    "Bảo Trân",
    "Thùy Dung",
    "Thu Hương",
    "Thảo Nhi",
    "Minh Thư",
    "Quỳnh Trang",
    "Lan Hương",
    "Hồng Hạnh",
    "Hoài Thương",
    "Mỹ Linh",
    "Hải Đăng",
    "Kim Anh",
    "Nhật Minh",
    "Ánh Dương",
    "Minh Tâm",
    "Đức Huy",
    "Huyền Trang",
    "Tuấn Kiệt",
    "Quang Huy",
    "Gia Bảo",
    "Khánh Linh",
    "Anh Thư",
    "Thanh Bình",
    "Diễm Hương",
    "Diệu Thảo",
    "Tường An",
    "Hoàng Dương",
    "Hà My",
    "Nguyễn Hương",
    "Ngọc Diễm",
    "Hoàng Lan",
    "Thúy Hằng",
    "Tường Vi",
    "Quỳnh Như",
    "Diễm Quỳnh",
    "Thanh Nhàn",
    "Thảo Hương",
    "Tâm Anh",
    "Bảo Tuyết",
    "Linh Chi",
    "Kim Liên",
    "Thành Hưng",
    "Bích Liên",
    "Trúc Ly",
    "Linh Trang",
    "Diệu Hương",
    "Kim Liên",
    "Thùy Linh",
    "Bích Tuyết",
    "Thu Thủy",
    "Quỳnh Dương",
    "Đoàn Thu",
    "Như Quỳnh",
    "Bích Thu",
    "Diệu Duyên",
    "Hữu Phước",
    "Minh Khang",
    "Ngọc Bích",
    "Thanh Tâm",
    "Phương Thảo",
    "Tuyết Lan",
    "Bảo Trân",
    "Thùy Dung",
    "Thu Hương",
    "Thảo Nhi",
    "Minh Thư",
    "Quỳnh Trang",
    "Lan Hương",
    "Hồng Hạnh",
    "Hoài Thương",
    "Mỹ Linh",
    "Hải Đăng",
    "Kim Anh",
    "Nhật Minh",
    "Ánh Dương",
    "Minh Tâm",
    "Đức Huy",
    "Huyền Trang",
    "Tuấn Kiệt",
    "Quang Huy",
    "Gia Bảo",
    "Khánh Linh",
    "Anh Thư",
    "Thanh Bình",
    "Diễm Hương",
    "Diệu Thảo",
    "Tường An",
    "Hoàng Dương",
    "Hà My",
    "Nguyễn Hương",
    "Ngọc Diễm",
    "Hoàng Lan",
    "Thúy Hằng",
    "Tường Vi",
    "Quỳnh Như",
    "Diễm Quỳnh",
    "Thanh Nhàn",
    "Thảo Hương",
    "Tâm Anh",
    "Bảo Tuyết",
    "Linh Chi",
    "Kim Liên",
    "Thành Hưng",
    "Bích Liên",
    "Trúc Ly",
    "Linh Trang",
    "Diệu Hương",
    "Kim Liên",
    "Thùy Linh",
    "Bích Tuyết",
    "Thu Thủy",
    "Quỳnh Dương",
    "Đoàn Thu",
    "Như Quỳnh",
    "Bích Thu",
    "Diệu Duyên",
    "Hữu Phước",
    "Minh Khang",
    "Ngọc Bích",
    "Thanh Tâm",
    "Phương Thảo",
    "Tuyết Lan",
    "Bảo Trân",
    "Thùy Dung",
    "Thu Hương",
    "Thảo Nhi",
    "Minh Thư",
    "Quỳnh Trang",
    "Lan Hương",
    "Hồng Hạnh",
    "Hoài Thương",
    "Mỹ Linh",
    "Hải Đăng",
    "Kim Anh",
    "Nhật Minh",
    "Ánh Dương",
    "Minh Tâm",
    "Đức Huy",
    "Huyền Trang",
    "Tuấn Kiệt",
    "Quang Huy",
    "Gia Bảo",
    "Khánh Linh",
    "Anh Thư",
    "Thanh Bình",
    "Diễm Hương",
    "Diệu Thảo",
    "Tường An",
    "Hoàng Dương",
    "Hà My",
    "Nguyễn Hương",
    "Ngọc Diễm",
    "Hoàng Lan",
    "Thúy Hằng",
    "Tường Vi",
    "Quỳnh Như",
    "Diễm Quỳnh",
    "Thanh Nhàn",
    "Thảo Hương",
    "Tâm Anh",
    "Bảo Tuyết",
    "Linh Chi",
    "Kim Liên",
    "Thành Hưng",
    "Bích Liên",
    "Trúc Ly",
    "Linh Trang",
    "Diệu Hương",
    "Kim Liên",
    "Thùy Linh",
    "Bích Tuyết",
    "Thu Thủy",
    "Quỳnh Dương",
    "Đoàn Thu",
    "Như Quỳnh",
    "Bích Thu",
    "Diệu Duyên",
    "Hữu Phước",
    "Minh Khang",
    "Ngọc Bích",
    "Thanh Tâm",
    "Phương Thảo",
    "Tuyết Lan",
    "Bảo Trân",
    "Thùy Dung",
    "Thu Hương",
    "Thảo Nhi",
    "Minh Thư",
    "Quỳnh Trang",
    "Lan Hương",
    "Hồng Hạnh",
    "Hoài Thương",
    "Mỹ Linh",
    "Hải Đăng",
    "Kim Anh",
    "Nhật Minh",
    "Ánh Dương",
    "Minh Tâm",
    "Đức Huy",
    "Huyền Trang",
    "Tuấn Kiệt",
    "Quang Huy",
    "Gia Bảo",
    "Khánh Linh",
    "Anh Thư",
    "Thanh Bình",
    "Diễm Hương",
    "Diệu Thảo",
    "Tường An",
    "Hoàng Dương",
    "Hà My",
    "Nguyễn Hương",
    "Ngọc Diễm",
    "Hoàng Lan",
    "Thúy Hằng",
    "Tường Vi",
    "Quỳnh Như",
    "Diễm Quỳnh",
    "Thanh Nhàn",
    "Thảo Hương",
    "Tâm Anh",
    "Bảo Tuyết",
    "Linh Chi",
    "Kim Liên",
    "Thành Hưng",
    "Bích Liên",
    "Trúc Ly",
    "Linh Trang",
    "Diệu Hương",
    "Kim Liên",
    "Thùy Linh",
    "Bích Tuyết",
    "Thu Thủy",
    "Quỳnh Dương",
    "Đoàn Thu",
    "Như Quỳnh",
    "Bích Thu",
    "Diệu Duyên",
    "Hữu Phước",
    "Minh Khang",
    "Ngọc Bích",
    "Thanh Tâm",
    "Phương Thảo",
    "Tuyết Lan",
    "Bảo Trân",
    "Thùy Dung",
    "Thu Hương",
    "Thảo Nhi",
    "Minh Thư",
    "Quỳnh Trang",
    "Lan Hương",
    "Hồng Hạnh",
        };
        public static readonly List<string> LastnameRandom = new List<string>
        {
             "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez",
  "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson", "Thomas", "Taylor", "Moore", "Jackson", "Martin",
  "Lee", "Perez", "Thompson", "White", "Harris",
  "Do", "Nguyen", "Tran", "Le", "Pham", "Huynh", "Vo", "Dinh", "Hoang", "Lam", "Phan", "Bui", "Đang",
  "Suzuki", "Sato", "Takahashi", "Tanaka", "Watanabe", "Yamamoto", "Kobayashi", "Yoshida", "Yamada", "Sasaki",
  "Wang", "Li", "Zhang", "Liu", "Chen", "Yang", "Huang", "Zhao", "Wu", "Zhou",
  "Kim", "Lee", "Park", "Choi", "Jung", "Kang", "Jo", "Yoon", "Jang", "Han",
  "Khan", "Ali", "Singh", "Ahmed", "Kumar",
  "Garcia", "Martinez", "Rodriguez", "Hernandez", "Gonzalez", "Lopez", "Perez", "Sanchez", "Ramirez", "Torres",
  "Rahman", "Uddin", "Begum", "Hossain", "Islam",
  "Tan", "Zhou", "Xu", "Ma",
  "Inoue", "Nakamura", "Saito", "Hashimoto",
  "Jung", "Kang", "Oh", "Jang",
  "Akter", "Chowdhury", "Hasan", "Alam", "Mahmud", "Biswas", "Das", "Saha", "Sarkar", "Bhuiyan",
  "Al-Ghamdi", "Al-Qahtani", "Al-Dossari", "Al-Harbi", "Al-Jaziri", "Al-Maktoum", "Al-Nahyan", "Al-Saud", "Al-Hashimi", "Al-Farsi",
  "Al-Zahrani", "Al-Shammar", "Al-Otaibi", "Al-Khalifa", "Al-Thani", "Al-Sabah", "Al-Sharif", "Al-Khoury", "Daoud", "Haddad",
  "Miller", "Wilson", "Anderson", "Thomas", "Taylor", "Moore", "Jackson", "Martin", "Lee", "Perez",
  "Thompson", "White", "Harris", "Clark", "Lewis", "Robinson", "Walker", "Hall", "Young", "Allen",
  "Wright", "King", "Green", "Baker", "Adams", "Nelson", "Hill", "Roberts", "Campbell", "Stewart",
  "Collins", "Bailey", "Reed", "Kelly", "Howard", "Gray", "Cox", "Ford", "Perry", "Bennett",
  "Wood", "Jenkins", "Barnes", "Sanders", "Ross", "Morales", "Griffin", "Gutierrez", "Ruiz", "Diaz",
  "Peterson", "Fisher", "Hayes", "Long", "Reynolds", "James", "Murray", "Wagner", "Cole", "Sullivan",
  "Freeman", "Webb", "Tucker", "Jordan", "Rogers", "Crawford", "Nichols", "Monroe", "Mendoza", "Ferguson",
  "Simpson", "Hudson", "Hanson", "Arnold", "Atkins", "Little", "Weaver", "Francis", "Henry", "Curtis",
  "Stevens", "Hoffman", "Hunter", "Nash", "Gilbert", "Garrett", "Welch", "Byrd", "Nicholson", "Lyons",
  "Osborne", "Mccoy", "Powers", "Schultz", "Richards", "Russell", "Wheeler", "Hines", "Dunn", "West",
  "Stone", "Hart", "Saunders", "Griffith", "Willis", "Sharp", "Horton", "Bowman", "Dennis", "Watkins",
   "O'brien", "Mccarthy", "Kennedy", "Fitzpatrick", "Ryan", "O'neill", "Doherty", "Gallagher", "Connolly", "Walsh",
  "Mcdonald", "Hughes", "Griffin", "Murphy", "Kelly", "Byrne", "Doyle", "Brady", "Brennan", "Quinn",
  "Carroll", "O'connor", "O'reilly", "Farrell", "Daly", "O'sullivan", "O'rourke", "O'donnell", "O'hara", "O'malley",
  // Thêm nhiều họ Trung Đông/Ả Rập
  "Al-Masri",    // Phổ biến ở Ai Cập, Palestine, Jordan, Syria, Lebanon
  "Al-Khatib",   // Phổ biến ở Syria, Lebanon, Palestine
  "Al-Najjar",   // Phổ biến ở Palestine, Jordan, Syria, Lebanon
  "Al-Husseini", // Họ có liên quan đến dòng dõi của Tiên tri Muhammad
  "Al-Tamimi",   // Họ Ả Rập cổ
  "Al-Ansari",    // Họ có liên quan đến những người ủng hộ Tiên tri Muhammad ở Medina
  "Al-Ahmad",     // Phổ biến ở nhiều nước Ả Rập
  "Al-Khalil",    // Phổ biến ở Palestine
  "Al-Sayegh",    // Phổ biến ở Lebanon, Syria
  "Al-Dabbagh",   // Phổ biến ở Syria
  "Al-Sayed",     // Họ có liên quan đến dòng dõi của Tiên tri Muhammad
  "Al-Amin",      // Họ có nghĩa là "người trung thực"
  "Al-Fadl",      // Họ có nghĩa là "ân sủng"
  "Al-Hakim",     // Họ có nghĩa là "người khôn ngoan"
  "Al-Rashid",    // Họ có nghĩa là "người ngay thẳng"
  "Al-Basri",     // Họ có nguồn gốc từ thành phố Basra
  "Al-Baghdadi",  // Họ có nguồn gốc từ thành phố Baghdad
  "Al-Iraqi",     // Họ có nghĩa là "người Iraq"
  "Al-Misri",     // Họ có nghĩa là "người Ai Cập"
  "Al-Sham"
        };
        public static readonly List<string> FirstnameRandom = new List<string>
        {
            "Alice", "Bob", "Charlie", "David", "Eva", "Frank", "Grace", "Henry", "Ivy", "Jack",
  "Karen", "Liam", "Mia", "Noah", "Olivia", "Peter", "Quinn", "Rachel", "Sam", "Tina",
  "Uriel", "Victoria", "William", "Xavier", "Yara", "Zoe",
  "An", "Binh", "Chi", "Dung", "Huy", "Lan", "Minh", "Nhi", "Phuong", "Quang", "Thao", "Tuan", "Vy", "Xuan", "Yen",
  "Mohammed", "Fatima", "Ahmed", "Sara", "Maria", "Jose", "Sofia", "Juan",
  "Anna", "Ben", "Chloe", "Daniel", "Ella", "Felix", "Hannah", "Isaac", "Julia", "Kevin",
  "Lily", "Max", "Nora", "Oscar", "Paige", "Ryan", "Sophia", "Thomas", "Uma", "Victor",
  "Wendy", "Xena", "Yasin", "Zara",
  "Aisha", "Rahman", "Nur", "Shorif",
  "Ji-hoon", "Seo-yeon", "Min-jun", "Eun-ji",
  "Mohammad", "Mehedi", "Abu", "Imran", "Abdullah-al", "Nazmul", "Tanvir", "Saiful", "Masud", "Sohel",
  "Shamim", "Nahid", "Abir", "Sagor", "Nusrat",
  "Abdullah", "Ali", "Omar", "Khalid", "Yousef", "Faisal", "Majid", "Rami", "Tariq", "Zain",
  "Layla", "Noura", "Amira", "Jana", "Reem", "Sara", "Hana", "Yasmin", "Zahra", "Malak",
  "Ethan", "Isabella", "Jacob", "Madison", "Michael", "Emily", "Alexander", "Abigail", "Daniel", "Chloe",
  "Matthew", "Elizabeth", "Joseph", "Mia", "Andrew", "Sofia", "David", "Evelyn", "James", "Harper",
  "Benjamin", "Amelia", "Anthony", "Ella", "Nicholas", "Avery", "Joshua", "Scarlett", "Christopher", "Grace",
  "Dylan", "Victoria", "Ryan", "Riley", "Brandon", "Lily", "Christian", "Addison", "Jonathan", "Aubrey",
   "Gabriel", "Zoey", "Samuel", "Madison", "Nathan", "Penelope", "Zachary", "Layla", "Caleb", "Brooklyn",
  "Adrian", "Hazel", "Owen", "Eleanor", "Julian", "Aurora", "Isaac", "Stella", "Cameron", "Violet",
  "Jose", "Maria", "Carlos", "Isabella", "Miguel", "Sofia", "Javier", "Valentina", "Alejandro", "Camila",
  "Mateo", "Emma", "Sebastian", "Martina", "Santiago", "Olivia", "Angel", "Mia", "Samuel", "Sofia",
  "Diego", "Valentina", "Leonardo", "Camila", "Adrian", "Isabella", "Martin", "Sofia", "Nicolas", "Emma",
  "Lucas", "Olivia", "Joaquin", "Valentina", "Tomas", "Sofia", "Vicente", "Isabella", "Benjamin", "Emma",
  "Pedro", "Sofia", "Juan", "Valentina", "Pablo", "Isabella", "Andres", "Emma", "Felipe", "Olivia",
  "Javier", "Camila", "Manuel", "Martina", "Ignacio", "Valentina", "Cristobal", "Emma", "Matias", "Sofia",

  // Thêm nhiều tên Trung Đông/Ả Rập
  "Youssef",    // Nam (biến thể của Yousef)
  "Hassan",     // Nam
  "Mahmoud",    // Nam
  "Khaled",     // Nam (biến thể của Khalid)
  "Ibrahim",    // Nam
  "Mustafa",    // Nam
  "Ahmed",      // Nam (đã có, nhưng rất phổ biến)
  "Sami",       // Nam
  "Rayan",      // Nam
  "Karim",      // Nam
  "Salma",      // Nữ
  "Aya",        // Nữ
  "Lina",       // Nữ
  "Dina",       // Nữ
  "Hala",       // Nữ
  "Nadine",     // Nữ
  "Rania",      // Nữ
  "Farah",      // Nữ
  "Leila",      // Nữ (biến thể của Layla)
  "Joudi"       // Nữ
        };
        public static readonly List<string> Countries = new List<string>
{
    "Random",
    "AF | 93", // Afghanistan
    "AL | 355", // Albania
    "DZ | 213", // Algeria
    "AS | 1-684", // American Samoa
    "AD | 376", // Andorra
    "AO | 244", // Angola
    "AI | 1-264", // Anguilla
    "AG | 1-268", // Antigua and Barbuda
    "AR | 54", // Argentina
    "AM | 374", // Armenia
    "AW | 297", // Aruba
    "AU | 61", // Australia
    "AT | 43", // Austria
    "AZ | 994", // Azerbaijan
    "BS | 1-242", // Bahamas
    "BH | 973", // Bahrain
    "BD | 880", // Bangladesh
    "BB | 1-246", // Barbados
    "BY | 375", // Belarus
    "BE | 32", // Belgium
    "BZ | 501", // Belize
    "BJ | 229", // Benin
    "BM | 1-441", // Bermuda
    "BT | 975", // Bhutan
    "BO | 591", // Bolivia
    "BA | 387", // Bosnia and Herzegovina
    "BW | 267", // Botswana
    "BR | 55", // Brazil
    "IO | 246", // British Indian Ocean Territory
    "VG | 1-284", // British Virgin Islands
    "BN | 673", // Brunei
    "BG | 359", // Bulgaria
    "BF | 226", // Burkina Faso
    "BI | 257", // Burundi
    "KH | 855", // Cambodia
    "CM | 237", // Cameroon
    "CA | 1", // Canada
    "CV | 238", // Cape Verde
    "KY | 1-345", // Cayman Islands
    "CF | 236", // Central African Republic
    "TD | 235", // Chad
    "CL | 56", // Chile
    "CN | 86", // China
    "CX | 61", // Christmas Island
    "CC | 61", // Cocos Islands
    "CO | 57", // Colombia
    "KM | 269", // Comoros
    "CD | 243", // Congo (Democratic Republic of the)
    "CG | 242", // Congo (Republic of the)
    "CK | 682", // Cook Islands
    "CR | 506", // Costa Rica
    "HR | 385", // Croatia
    "CU | 53", // Cuba
    "CW | 599", // Curacao
    "CY | 357", // Cyprus
    "CZ | 420", // Czech Republic
    "DK | 45", // Denmark
    "DJ | 253", // Djibouti
    "DM | 1-767", // Dominica
    "DO | 1-809", // Dominican Republic
    "TL | 670", // East Timor
    "EC | 593", // Ecuador
    "EG | 20", // Egypt
    "SV | 503", // El Salvador
    "GQ | 240", // Equatorial Guinea
    "ER | 291", // Eritrea
    "EE | 372", // Estonia
    "ET | 251", // Ethiopia
    "FK | 500", // Falkland Islands
    "FO | 298", // Faroe Islands
    "FJ | 679", // Fiji
    "FI | 358", // Finland
    "FR | 33", // France
    "GF | 594", // French Guiana
    "PF | 689", // French Polynesia
    "GA | 241", // Gabon
    "GM | 220", // Gambia
    "GE | 995", // Georgia
    "DE | 49", // Germany
    "GH | 233", // Ghana
    "GI | 350", // Gibraltar
    "GR | 30", // Greece
    "GL | 299", // Greenland
    "GD | 1-473", // Grenada
    "GP | 590", // Guadeloupe
    "GU | 1-671", // Guam
    "GT | 502", // Guatemala
    "GG | 44-1481", // Guernsey
    "GN | 224", // Guinea
    "GW | 245", // Guinea-Bissau
    "GY | 592", // Guyana
    "HT | 509", // Haiti
    "HN | 504", // Honduras
    "HK | 852", // Hong Kong
    "HU | 36", // Hungary
    "IS | 354", // Iceland
    "IN | 91", // India
    "ID | 62", // Indonesia
    "IR | 98", // Iran
    "IQ | 964", // Iraq
    "IE | 353", // Ireland
    "IM | 44-1624", // Isle of Man
    "IL | 972", // Israel
    "IT | 39", // Italy
    "CI | 225", // Ivory Coast
    "JM | 1-876", // Jamaica
    "JP | 81", // Japan
    "JE | 44-1534", // Jersey
    "JO | 962", // Jordan
    "KZ | 7", // Kazakhstan
    "KE | 254", // Kenya
    "KI | 686", // Kiribati
    "KW | 965", // Kuwait
    "KG | 996", // Kyrgyzstan
    "LA | 856", // Laos
    "LV | 371", // Latvia
    "LB | 961", // Lebanon
    "LS | 266", // Lesotho
    "LR | 231", // Liberia
    "LY | 218", // Libya
    "LI | 423", // Liechtenstein
    "LT | 370", // Lithuania
    "LU | 352", // Luxembourg
    "MO | 853", // Macau
    "MK | 389", // North Macedonia (formerly Macedonia)
    "MG | 261", // Madagascar
    "MW | 265", // Malawi
    "MY | 60", // Malaysia
    "MV | 960", // Maldives
    "ML | 223", // Mali
    "MT | 356", // Malta
    "MH | 692", // Marshall Islands
    "MQ | 596", // Martinique
    "MR | 222", // Mauritania
    "MU | 230", // Mauritius
    "YT | 262", // Mayotte
    "MX | 52", // Mexico
    "FM | 691", // Micronesia
    "MD | 373", // Moldova
    "MC | 377", // Monaco
    "MN | 976", // Mongolia
    "ME | 382", // Montenegro
    "MS | 1-664", // Montserrat
    "MA | 212", // Morocco
    "MZ | 258", // Mozambique
    "MM | 95", // Myanmar
    "NA | 264", // Namibia
    "NR | 674", // Nauru
    "NP | 977", // Nepal
    "NL | 31", // Netherlands
    "NC | 687", // New Caledonia
    "NZ | 64", // New Zealand
    "NI | 505", // Nicaragua
    "NE | 227", // Niger
    "NG | 234", // Nigeria
    "NU | 683", // Niue
    "NF | 672", // Norfolk Island
    "KP | 850", // North Korea
    "MP | 1-670", // Northern Mariana Islands
    "NO | 47", // Norway
    "OM | 968", // Oman
    "PK | 92", // Pakistan
    "PW | 680", // Palau
    "PS | 970", // Palestine
    "PA | 507", // Panama
    "PG | 675", // Papua New Guinea
    "PY | 595", // Paraguay
    "PE | 51", // Peru
    "PH | 63", // Philippines
    "PN | 64", // Pitcairn
    "PL | 48", // Poland
    "PT | 351", // Portugal
    "PR | 1-787", // Puerto Rico
    "QA | 974", // Qatar
    "RE | 262", // Reunion
    "RO | 40", // Romania
    "RU | 7", // Russia
    "RW | 250", // Rwanda
    "BL | 590", // Saint Barthelemy
    "SH | 290", // Saint Helena
    "KN | 1-869", // Saint Kitts and Nevis
    "LC | 1-758", // Saint Lucia
    "MF | 590", // Saint Martin
    "PM | 508", // Saint Pierre and Miquelon
    "VC | 1-784", // Saint Vincent and the Grenadines
    "WS | 685", // Samoa
    "SM | 378", // San Marino
    "ST | 239", // Sao Tome and Principe
    "SA | 966", // Saudi Arabia
    "SN | 221", // Senegal
    "RS | 381", // Serbia
    "SC | 248", // Seychelles
    "SL | 232", // Sierra Leone
    "SG | 65", // Singapore
    "SX | 1-721", // Sint Maarten
    "SK | 421", // Slovakia
    "SI | 386", // Slovenia
    "SB | 677", // Solomon Islands
    "SO | 252", // Somalia
    "ZA | 27", // South Africa
    "KR | 82", // South Korea
    "SS | 211", // South Sudan
    "ES | 34", // Spain
    "LK | 94", // Sri Lanka
    "SD | 249", // Sudan
    "SR | 597", // Suriname
    "SJ | 47", // Svalbard and Jan Mayen
    "SZ | 268", // Swaziland (Eswatini)
    "SE | 46", // Sweden
    "CH | 41", // Switzerland
    "SY | 963", // Syria
    "TW | 886", // Taiwan
    "TJ | 992", // Tajikistan
    "TZ | 255", // Tanzania
    "TH | 66", // Thailand
    "TG | 228", // Togo
    "TK | 690", // Tokelau
    "TO | 676", // Tonga
    "TT | 1-868", // Trinidad and Tobago
    "TN | 216", // Tunisia
    "TR | 90", // Turkey
    "TM | 993", // Turkmenistan
    "TC | 1-649", // Turks and Caicos Islands
    "TV | 688", // Tuvalu
    "UG | 256", // Uganda
    "UA | 380", // Ukraine
    "AE | 971", // United Arab Emirates
    "GB | 44", // United Kingdom
    "US | 1", // United States
    "UY | 598", // Uruguay
    "VI | 1-340", // US Virgin Islands
    "UZ | 998", // Uzbekistan
    "VU | 678", // Vanuatu
    "VA | 379", // Vatican City
    "VE | 58", // Venezuela
    "VN | 84", // Viet Nam
    "WF | 681", // Wallis and Futuna
    "YE | 967", // Yemen
    "ZM | 260", // Zambia
    "ZW | 263" // Zimbabwe
};
    }
}
