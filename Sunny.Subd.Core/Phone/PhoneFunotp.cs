using Newtonsoft.Json.Linq;
using Sunny.Subdy.Common.Logs;

namespace Sunny.Subd.Core.Phone
{
    public class PhoneFunotp
    {
        public static async Task<string> GetPhone(string token, string service)
        {
            string phoneNumber = string.Empty;
            string session = string.Empty;
            string error = string.Empty;
            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://funotp.com/api?action=number&service={service}&apikey={token}");
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();
                error = json.Trim();
                try
                {
                    JObject jObject = JObject.Parse(json);
                    phoneNumber = jObject["Result"]?["number"]?.ToString();
                    session = jObject["Result"]?["id"]?.ToString();
                }
                catch
                {

                }

            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
            if (string.IsNullOrEmpty(phoneNumber) && string.IsNullOrEmpty(session))
            {
                return "ERROR" + error;
            }
            return session + "|" + phoneNumber;
        }
        public static async Task<string> GetOTP(string token, string session)
        {
            string otp = string.Empty;
            string error = string.Empty;
            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://funotp.com/api?action=code&id={session}&apikey={token}");
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();
                error = json.Trim();
                try
                {
                    JObject jObject = JObject.Parse(json);
                    otp = jObject["Result"]?["otp"]?.ToString();

                }
                catch
                {

                }

            }
            catch (Exception ex)
            {
                error = "ERROR" + ex.Message;
                LogManager.Error(ex);
            }
            if (string.IsNullOrEmpty(otp))
            {
                return "ERROR" + error;
            }
            return otp;
        }
    }
}
