using Newtonsoft.Json.Linq;

namespace Sunny.Subd.Core.Phone
{
    internal class PhoneIronsim
    {
        public static async Task<string> GetPhone(string token, string service)
        {
            string phoneNumber = string.Empty;
            string session = string.Empty;
            string error = string.Empty;
            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://ironsim.com/api/phone/new-session?token={token}&service={service}&network=1,2,3,6");
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();
                error = json.Trim();
                try
                {
                    JObject jObject = JObject.Parse(json);
                    phoneNumber = jObject["data"]?["phone_number"]?.ToString();
                    session = jObject["data"]?["session"]?.ToString();
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
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://ironsim.com/api/session/{session}/get-otp?token={token}");
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();
                error = json.Trim();
                try
                {
                    JObject jObject = JObject.Parse(json);
                    otp = jObject["data"]?["messages"]?[0]?["otp"]?.ToString();

                }
                catch
                {

                }

            }
            catch (Exception ex)
            {
                error = "ERROR" + ex.Message;
            }
            if (string.IsNullOrEmpty(otp))
            {
                return "ERROR" + error;
            }
            return otp;
        }
    }
}
