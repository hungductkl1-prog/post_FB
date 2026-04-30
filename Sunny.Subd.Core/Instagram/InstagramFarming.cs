using AutoAndroid;
using Sunny.Subd.Core.Facebook;
using Sunny.Subd.Core.Models;
using Sunny.Subd.Core.Services;
using Sunny.Subd.Core.Utils;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Json;
using Sunny.Subdy.Common.Models;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;
using System.Diagnostics;

namespace Sunny.Subd.Core.Instagram
{
    /// <summary>
    /// Mirror của FacebookFarming cho Instagram. Dùng cùng kiến trúc:
    /// - Sequence các ScriptAction theo ByOrder.
    /// - Mỗi action loop soLuong lần với delay between.
    /// Khác FacebookFarming: dùng package com.instagram.android, intent IG riêng,
    /// xpath/UI element của IG. Phase 1 chỉ hỗ trợ 10 IGXXXX type đã định nghĩa
    /// trong InstagramFarmingType.
    /// </summary>
    public class InstagramFarming
    {
        private readonly ADBClient _client;
        private readonly Account _account;
        private readonly ConfigModel _config;
        private readonly MainService _mainService;
        private readonly ScriptContext _scriptContext;
        private readonly ScriptActionContext _scriptActionContext;
        private readonly JsonHelper _configKichBan;
        private readonly Dictionary<string, object> _setting = new();
        private readonly Stopwatch _stopwatch = new();
        private Script _script;

        private const string IG_PACKAGE = "com.instagram.android";

        public InstagramFarming(MainService mainService)
        {
            _client = mainService._client;
            _account = mainService._account;
            _mainService = mainService;
            _config = mainService._config;
            _scriptContext = new ScriptContext();
            _scriptActionContext = new ScriptActionContext();
            _configKichBan = SettingsTool.GetSettings($"fQuanLyKichBan_{mainService._platform}", true);
        }

        private bool Stop()
        {
            if (_setting.ContainsKey("timeoutTaiKhoan")
                && _stopwatch.Elapsed >= TimeSpan.FromMinutes(Convert.ToInt32(_setting["timeoutTaiKhoan"])))
            {
                _mainService.SetStatus($"Đã quá {_setting["timeoutTaiKhoan"]} phút cho tài khoản này!", 2);
                return true;
            }
            if (_setting.ContainsKey("timeoutKichBan")
                && _stopwatch.Elapsed >= TimeSpan.FromMinutes(Convert.ToInt32(_setting["timeoutKichBan"])))
            {
                _mainService.SetStatus($"Đã quá {_setting["timeoutKichBan"]} phút cho kịch bản này!", 2);
                return true;
            }
            return false;
        }

        public async Task ExecuteAsync()
        {
            string scriptName = _account?.NameScript?.Trim() ?? "";
            if (string.IsNullOrEmpty(scriptName))
            {
                var all = _scriptContext.GetByPlatform(_mainService._platform) ?? new List<Script>();
                _script = all.FirstOrDefault(s => !ScriptNames.IsBuiltIn(s.Name));
            }
            else
            {
                _script = _scriptContext.GetByName(scriptName, _mainService._platform);
            }

            if (_script == null)
            {
                _mainService.SetStatus("Không tồn tại kịch bản.", 1);
                return;
            }

            var actions = _scriptActionContext.GetByScriptId(_script.Id);
            if (actions == null || !actions.Any())
            {
                _mainService.SetStatus("Không có hành động nào cả.", 1);
                return;
            }

            actions = actions.OrderBy(x => x.ByOrder).ToList();
            if (_configKichBan.GetBooleanValue("checkBox1"))
            {
                actions = actions.OrderBy(_ => Guid.NewGuid()).ToList();
            }
            if (_configKichBan.GetBooleanValue("checkBox2"))
            {
                int take = SubdyHelper.RandomValue(_configKichBan.GetIntType("numericUpDown5", 1), _configKichBan.GetIntType("numericUpDown4", 5));
                if (actions.Count > take) actions = actions.Take(take).ToList();
            }
            if (_configKichBan.GetBooleanValue("checkBox3"))
            {
                _setting["timeoutTaiKhoan"] = SubdyHelper.RandomValue(_configKichBan.GetIntType("numericUpDown7", 60), _configKichBan.GetIntType("numericUpDown6", 120));
            }
            if (_configKichBan.GetBooleanValue("checkBox4"))
            {
                _setting["timeoutKichBan"] = SubdyHelper.RandomValue(_configKichBan.GetIntType("numericUpDown9", 60), _configKichBan.GetIntType("numericUpDown8", 120));
            }

            _stopwatch.Restart();
            _mainService._sate = "Tải kịch bản";

            for (int i = 1; i <= actions.Count; i++)
            {
                var action = actions[i - 1];
                if (Stop()) return;

                _mainService._sate = $"Thực hiện {i}/{actions.Count}: {action.Name}";
                _mainService.SetStatus("Đang thực hiện...", 0);
                _mainService.SetStatus("Đang kiểm tra tài khoản...", 2);
                await _mainService._facebookService.HanderAccount(_client, _account, 5, _mainService._ct, _mainService);
                try
                {
                    await StartAction(action);
                }
                catch
                {
                    // Lỗi đã được dispatch handler ghi log; vẫn chạy tiếp action kế.
                }
            }
        }

        public async Task StartAction(ScriptAction action)
        {
            string error = "Thành công";
            try
            {
                var json = new JsonHelper(action.Json, true);
                EnsureInstagramOpen();

                switch (action.Type)
                {
                    case InstagramFarmingType.IGXemReel:
                        await IGXemReel(json, action);
                        break;
                    case InstagramFarmingType.IGXemStory:
                        await IGXemStory(json, action);
                        break;
                    case InstagramFarmingType.IGTuongTacNewfeed:
                        await IGTuongTacNewfeed(json, action);
                        break;
                    case InstagramFarmingType.IGDangBai:
                        await IGDangBai(json, action);
                        break;
                    case InstagramFarmingType.IGDangReel:
                        await IGDangReel(json, action);
                        break;
                    case InstagramFarmingType.IGDangStory:
                        await IGDangStory(json, action);
                        break;
                    case InstagramFarmingType.IGFollow:
                        await IGFollow(json, action);
                        break;
                    case InstagramFarmingType.IGUnfollow:
                        await IGUnfollow(json, action);
                        break;
                    case InstagramFarmingType.IGNhanTin:
                        await IGNhanTin(json, action);
                        break;
                    case InstagramFarmingType.IGCapNhatThongTin:
                        await IGCapNhatThongTin(json, action);
                        break;
                    default:
                        error = $"Action type [{action.Type}] chưa được hỗ trợ trên Instagram.";
                        break;
                }
            }
            catch (Exception ex)
            {
                SubdyExtension extension = ex is SubdyExtension sx
                    ? sx
                    : new SubdyExtension(SubdyEnum.Error, ex.Message);

                if (extension.SubdyEnum == SubdyEnum.LogOut)
                {
                    await _mainService._facebookService.Login(_client, _account, _mainService._ct, 180, _mainService);
                }
                error = $"Thất bại ({extension.Message})";
                if (extension.SubdyEnum != SubdyEnum.JobFail)
                {
                    throw extension;
                }
            }
            finally
            {
                int wait = SubdyHelper.RandomValue(
                    _configKichBan.GetIntType("numericUpDown2", 5),
                    _configKichBan.GetIntType("numericUpDown1", 30));
                await _mainService.DelayMessageAsync(
                    wait,
                    $"Đã chạy hành động {action.Name}.{error}." + " Đợi {time} giây để qua hành động tiếp theo...",
                    2);
                _mainService.SetStatus($"Đã chạy xong hành động {action.Name} - {error}", 0);
            }
        }

        // ---------- Helpers chung ----------

        private void EnsureInstagramOpen()
        {
            if (!_client.Package(IG_PACKAGE, 1))
            {
                _client.AppStart(IG_PACKAGE, true, true, true);
                _client.Delay(3);
            }
        }

        /// <summary>Đọc cặp from/to soLuong + delay từ JSON (do fIGActionStub lưu).</summary>
        private (int soLuong, int delayFrom, int delayTo) ReadStubConfig(JsonHelper json)
        {
            int soLuong = SubdyHelper.RandomValue(
                json.GetIntType("nudSoLuongFrom", 5),
                json.GetIntType("nudSoLuongTo", 10) + 1);
            int delayFrom = json.GetIntType("nudDelayFrom", 5);
            int delayTo = json.GetIntType("nudDelayTo", 10);
            return (soLuong, delayFrom, delayTo);
        }

        private async Task DelayBetween(int delayFrom, int delayTo, string actionName, int idx, int total)
        {
            int sec = SubdyHelper.RandomValue(delayFrom, delayTo + 1);
            await _mainService.DelayMessageAsync(
                sec,
                $"({idx}/{total}) {actionName} - đợi {{time}} giây trước thao tác tiếp theo...",
                2);
        }

        private void OpenInstagramHome()
        {
            _client.Shell($"am start -a android.intent.action.MAIN -c android.intent.category.LAUNCHER -n {IG_PACKAGE}/.activity.MainTabActivity");
            _client.Delay(2);
        }

        private void OpenReelTab()
        {
            // Mở thẳng reel tab qua deeplink; nếu thất bại swipe để cuộn reel.
            _client.Shell($"am start -a android.intent.action.VIEW -d \"https://www.instagram.com/reels/\" -p {IG_PACKAGE}");
            _client.Delay(2);
        }

        private void OpenStoryTray()
        {
            // IG không có deeplink mở trực tiếp tray story; mở app + tap story đầu tiên ở feed.
            EnsureInstagramOpen();
            OpenInstagramHome();
        }

        private void OpenProfile()
        {
            _client.ElementWithAttributes("//*[@resource-id=\"" + IG_PACKAGE + ":id/profile_tab\"]", 5);
        }

        private void GoBackHome()
        {
            _client.Shell("input keyevent 4");
            _client.Delay(1);
        }

        // ---------- 10 action handlers ----------

        private async Task IGXemReel(JsonHelper json, ScriptAction action)
        {
            var (soLuong, df, dt) = ReadStubConfig(json);
            EnsureInstagramOpen();
            OpenReelTab();

            for (int i = 1; i <= soLuong; i++)
            {
                if (Stop()) return;
                _mainService.SetStatus($"Xem reel {i}/{soLuong}", 2);
                // Vuốt lên để qua reel tiếp theo (giả lập view).
                _client.SwipeByPercent(50, 80, 50, 20, 800, 1, SubdyHelper.RandomValue(200, 800));
                if (i < soLuong) await DelayBetween(df, dt, action.Name, i, soLuong);
            }
        }

        private async Task IGXemStory(JsonHelper json, ScriptAction action)
        {
            var (soLuong, df, dt) = ReadStubConfig(json);
            EnsureInstagramOpen();
            OpenStoryTray();

            // Tap vào ô story đầu tiên trong tray (vùng top feed).
            _client.SwipeByPercent(15, 18, 15, 18, 50, 1, 200);
            _client.Delay(2);

            for (int i = 1; i <= soLuong; i++)
            {
                if (Stop()) return;
                _mainService.SetStatus($"Xem story {i}/{soLuong}", 2);
                // Tap phải để chuyển story tiếp theo.
                _client.SwipeByPercent(85, 50, 85, 50, 50, 1, 200);
                if (i < soLuong) await DelayBetween(df, dt, action.Name, i, soLuong);
            }
            GoBackHome();
        }

        private async Task IGTuongTacNewfeed(JsonHelper json, ScriptAction action)
        {
            var (soLuong, df, dt) = ReadStubConfig(json);
            EnsureInstagramOpen();
            OpenInstagramHome();

            for (int i = 1; i <= soLuong; i++)
            {
                if (Stop()) return;
                _mainService.SetStatus($"Tương tác newfeed {i}/{soLuong}", 2);
                // Like bằng double-tap giữa màn hình.
                _client.SwipeByPercent(50, 45, 50, 45, 50, 2, 80);
                _client.Delay(1);
                // Cuộn xuống bài tiếp.
                _client.SwipeByPercent(50, 75, 50, 25, 700, 1, SubdyHelper.RandomValue(300, 1000));
                if (i < soLuong) await DelayBetween(df, dt, action.Name, i, soLuong);
            }
        }

        private async Task IGDangBai(JsonHelper json, ScriptAction action)
        {
            // Phase 1: stub. Yêu cầu nguyên liệu (ảnh/caption) chưa có trong fIGActionStub.
            var (soLuong, df, dt) = ReadStubConfig(json);
            _mainService.SetStatus(
                "IGDangBai: cần bổ sung UI chọn ảnh/caption (chưa hỗ trợ ở Phase 1).",
                1);
            await Task.CompletedTask;
        }

        private async Task IGDangReel(JsonHelper json, ScriptAction action)
        {
            _mainService.SetStatus(
                "IGDangReel: cần bổ sung UI chọn video/caption (chưa hỗ trợ ở Phase 1).",
                1);
            await Task.CompletedTask;
        }

        private async Task IGDangStory(JsonHelper json, ScriptAction action)
        {
            _mainService.SetStatus(
                "IGDangStory: cần bổ sung UI chọn ảnh/video (chưa hỗ trợ ở Phase 1).",
                1);
            await Task.CompletedTask;
        }

        private async Task IGFollow(JsonHelper json, ScriptAction action)
        {
            var (soLuong, df, dt) = ReadStubConfig(json);
            EnsureInstagramOpen();

            for (int i = 1; i <= soLuong; i++)
            {
                if (Stop()) return;
                _mainService.SetStatus($"Follow {i}/{soLuong} - mở Explore", 2);
                _client.Shell($"am start -a android.intent.action.VIEW -d \"https://www.instagram.com/explore/\" -p {IG_PACKAGE}");
                _client.Delay(3);
                // Tap vào item gợi ý đầu tiên.
                _client.SwipeByPercent(25, 30, 25, 30, 50, 1, 200);
                _client.Delay(2);
                // Tap nút Follow (text "Follow" trên profile).
                _client.ElementWithAttributes("//*[@text=\"Follow\"]", 3);
                GoBackHome();
                if (i < soLuong) await DelayBetween(df, dt, action.Name, i, soLuong);
            }
        }

        private async Task IGUnfollow(JsonHelper json, ScriptAction action)
        {
            var (soLuong, df, dt) = ReadStubConfig(json);
            EnsureInstagramOpen();
            OpenProfile();
            _client.Delay(2);

            // Mở danh sách Following.
            _client.ElementWithAttributes("//*[@content-desc=\"following\"]", 3);
            _client.Delay(2);

            for (int i = 1; i <= soLuong; i++)
            {
                if (Stop()) return;
                _mainService.SetStatus($"Unfollow {i}/{soLuong}", 2);
                // Tap nút Following của user đầu tiên trong list rồi confirm.
                _client.ElementWithAttributes("//*[@text=\"Following\"]", 3);
                _client.Delay(1);
                _client.ElementWithAttributes("//*[@text=\"Unfollow\"]", 3);
                if (i < soLuong) await DelayBetween(df, dt, action.Name, i, soLuong);
            }
            GoBackHome();
        }

        private async Task IGNhanTin(JsonHelper json, ScriptAction action)
        {
            _mainService.SetStatus(
                "IGNhanTin: cần bổ sung UI chọn người nhận + nội dung (chưa hỗ trợ ở Phase 1).",
                1);
            await Task.CompletedTask;
        }

        private async Task IGCapNhatThongTin(JsonHelper json, ScriptAction action)
        {
            _mainService.SetStatus(
                "IGCapNhatThongTin: cần bổ sung UI nhập avatar/bio (chưa hỗ trợ ở Phase 1).",
                1);
            await Task.CompletedTask;
        }
    }
}
