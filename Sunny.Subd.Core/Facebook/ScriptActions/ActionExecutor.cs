using AutoAndroid;
using Sunny.Subd.Core.Models;
using Sunny.Subdy.Common.Json;
using Sunny.Subdy.Data.Models;
using System.Diagnostics;

namespace Sunny.Subd.Core.Facebook.ScriptActions
{
    public class ActionExecutor
    {
        private readonly Dictionary<string, IActionHandler> _handlers;

        public ActionExecutor(IEnumerable<IActionHandler> handlers)
        {
            _handlers = handlers.ToDictionary(h => h.TypeAction, StringComparer.OrdinalIgnoreCase);
        }

        public async Task<SubdyExtension> ExecuteAsync(string typeAction, Account account, ADBClient device, JsonHelper settingScript, JsonHelper settingAction, JsonHelper settingGeneral)
        {
            if (_handlers.TryGetValue(typeAction, out var handler))
            {
                return await handler.ExecuteAsync(account, device,  settingScript,  settingAction,  settingGeneral);
            }
            else
            {
                Debug.WriteLine($"⚠️ Không tìm thấy handler cho {typeAction}");
            }
            return new SubdyExtension(SubdyEnum.None, $"Không tìm thấy handler cho {typeAction}");
        }
    }
}
