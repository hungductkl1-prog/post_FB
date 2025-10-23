using Telegram.BotAPI;
using Telegram.BotAPI.AvailableMethods;
namespace Sunny.Subd.Core.Telegram
{
    public class TelegramBotServices
    {
        public static TelegramBotServices BotTelegram { get; set; }
        private static TelegramBotClient Bot { get; set; }
        public TelegramBotServices(string token)
        {
            Bot = new TelegramBotClient(token);
        }
        public async Task<string> SendMessageAsync(long chatId, string message)
        {
            try
            {
               var msg = await Bot.SendMessageAsync(chatId, message);
                return $"Gửi thành công: MessageId = {msg.MessageId}";
            }
            catch (Exception ex)
            {
                return $"Gửi thất bại: {ex.Message}";
            }
        }
    }
}
