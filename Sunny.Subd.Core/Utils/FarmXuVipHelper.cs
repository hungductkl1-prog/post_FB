using Sunny.Subdy.Common.Json;

namespace Sunny.Subd.Core.Utils
{
    public static class FarmXuVipHelper
    {
        private const string SettingName = "FarmXuVip_GoLike";
        private const string TokenKey = "token";

        public static string GetToken()
        {
            var js = SettingsTool.GetSettings(SettingName, true);
            return js.GetValue(TokenKey, "") ?? "";
        }

        public static void SaveToken(string token)
        {
            var js = SettingsTool.GetSettings(SettingName, true);
            js.AddOrUpdateProperty(TokenKey, token ?? "");
            js.SaveJsonToFile();
            SettingsTool.UpdateSetting(SettingName);
        }
    }
}
