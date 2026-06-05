namespace Sunny.Subdy.Common.API.Captchas
{
    public class CaptchaService
    {
        public static List<string> SitesV2 = new List<string>
        {
            GuruCaptchaClient.Url,
        };
        public static async Task<string> Getbalance(string site, string key)
        {
            if (string.IsNullOrEmpty(site)) return "Vui long chọn site cần kiểm tra!";
            if (string.IsNullOrEmpty(key)) return "Token không được bỏ trống!";
            switch (site)
            {
                case GuruCaptchaClient.Url:
                    {
                        return await GuruCaptchaClient.Getbalance(key);
                    }
                default:
                    return $"Chưa hỗ trợ site {site} này!";
            }
        }
        public static async Task<string> GetIdCaptchaV2(string site, string key, string sitekey, string siteurl)
        {
            if (string.IsNullOrEmpty(site)) throw new Exception("error: Vui long chọn site cần kiểm tra!");
            if (string.IsNullOrEmpty(key)) throw new Exception("error: Token không được bỏ trống!");
            switch (site)
            {
                case GuruCaptchaClient.Url:
                    {
                        return await GuruCaptchaClient.GetIdCaptchaV2(key, sitekey, siteurl);
                    }
                default:
                    throw new Exception($"error: Chưa hỗ trợ site {site} này!");
            }
        }
        /// <summary>
        /// Gửi ảnh captcha (base64) tới site và nhận về captcha id.
        /// Poll token bằng <see cref="GetTokenCaptchaV2"/> với cùng id.
        /// </summary>
        public static async Task<string> GetIdImageCaptcha(string site, string key, string base64Image)
        {
            if (string.IsNullOrEmpty(site)) throw new Exception("error: Vui long chọn site cần kiểm tra!");
            if (string.IsNullOrEmpty(key)) throw new Exception("error: Token không được bỏ trống!");
            switch (site)
            {
                case GuruCaptchaClient.Url:
                    {
                        return await GuruCaptchaClient.GetIdImageCaptcha(key, base64Image);
                    }
                default:
                    throw new Exception($"error: Chưa hỗ trợ site {site} này!");
            }
        }
        public static async Task<string> GetTokenCaptchaV2(string site, string key, string id)
        {
            if (string.IsNullOrEmpty(site)) throw new Exception("error: Vui long chọn site cần kiểm tra!");
            if (string.IsNullOrEmpty(key)) throw new Exception("error: Token không được bỏ trống!");
            switch (site)
            {
                case GuruCaptchaClient.Url:
                    {
                        return await GuruCaptchaClient.GetTokenCaptchaV2(key, id);
                    }
                default:
                    throw new Exception($"error: Chưa hỗ trợ site {site} này!");
            }
        }
    }
}
