using Newtonsoft.Json.Linq;

namespace Sunny.Subd.Core.Email
{
    public class Shopvia1s
    {
        public static async Task<string> GetEmail(string token)
        {
            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, "http://shopvia1s.com/api/buy_product");
                var form = new MultipartFormDataContent
        {
            { new StringContent("buyProduct"), "action" },
            { new StringContent("22691"), "id" },
            { new StringContent("1"), "amount" },
            { new StringContent(token), "api_key" }
        };

                request.Content = form;
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();
                var obj = JObject.Parse(json);
                if ((string)obj["status"] == "success")
                {
                    var dataArray = obj["data"] as JArray;
                    foreach (var item in dataArray)
                    {
                        return item.ToString();
                    }
                }
            }
            catch
            {

            }
            return string.Empty;
        }
        public static async Task<string> GetCode(string email, string refresh_token, string client_id)
        {
            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, "https://tools.dongvanfb.net/api/get_code_oauth2");
                var content = new StringContent("{\r\n    \"email\": \"" + email + "\",\r\n    \"refresh_token\": \"" + refresh_token + "\",\r\n    \"client_id\": \"" + client_id + "\",\r\n    \"type\": \"facebook\"\r\n}", null, "application/json");
                request.Content = content;
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();
                var obj = JObject.Parse(json);
                if (Convert.ToBoolean(obj["status"]))
                {
                    return (string)obj["code"];
                }
            }
            catch
            {
            }
            return string.Empty;
        }
    }
}
