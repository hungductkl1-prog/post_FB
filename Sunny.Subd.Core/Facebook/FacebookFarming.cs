using AutoAndroid;
using Sunny.Subd.Core.Models;
using Sunny.Subd.Core.Services;
using Sunny.Subd.Core.Utils;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Json;
using Sunny.Subdy.Common.Logs;
using Sunny.Subdy.Common.Models;
using Sunny.Subdy.Common.Services;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;
using System;
using System.Data;
using System.Diagnostics;
using System.IO.Packaging;
using System.Linq;
using System.Runtime;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using File = System.IO.File;

namespace Sunny.Subd.Core.Facebook
{
    public class FacebookFarming
    {
        internal Dictionary<string, List<string>> dictionary_9 = null;
        internal Dictionary<string, List<string>> C8289C88 = null;
        internal Dictionary<string, List<string>> dictionary_7 = null;
        internal Dictionary<string, List<string>> dictionary_11 = null;
        internal Dictionary<string, List<string>> dictionary_12 = null;
        internal Dictionary<string, List<string>> dictionary_6 = null;
        internal Dictionary<string, List<string>> C42A3EA1 = null;
        internal Dictionary<string, List<string>> C50FA08A = null;
        internal Dictionary<string, List<string>> dictionary_10 = null;
        internal Dictionary<string, List<string>> F605BBA9 = null;
        internal Dictionary<string, List<string>> dictionary_5 = null;
        internal Dictionary<string, List<string>> dictionary_0 = null;
        internal Dictionary<string, List<string>> dictionary_2 = null;
        internal Dictionary<string, List<string>> dictionary_4 = null;
        internal Dictionary<string, List<string>> dictionary_8 = null;
        internal Dictionary<string, List<string>> E2AC9E1F = null;
        internal Dictionary<string, List<string>> CAB1A00C = null;
        internal Dictionary<string, Dictionary<string, int>> D80EF33F = null;
        internal object D739380E = new object();
        internal Dictionary<string, List<string>> B68B3D0D = null;

        internal Dictionary<string, List<string>> C292E829 = null;
        internal object object_2 = new object();




        public static readonly Dictionary<string, List<string>> _data = new();
        private ADBClient _client;
        private Account _account;
        private ConfigModel _config;
        private Script _script;
        private ScriptContext _scriptContext;
        private ScriptActionContext _scriptActionContext;
        private MainService _mainService;
        private JsonHelper _configKichBan;
        private Dictionary<string, object> setting = new Dictionary<string, object>();
        // [TIME-LIMIT v25] Đồng hồ RIÊNG của lớp này đã BỎ. Hạn mức thời gian nay đo từ MỐC
        // MainService bấm lúc BẮT ĐẦU CHUẨN BỊ tài khoản (xem AccountLimitElapsed) nên TÍNH CẢ
        // thời gian chuẩn bị acc. Trước v25 lớp này tự Restart đồng hồ ở đầu ExecuteAsync — tức SAU
        // các bước xóa dữ liệu/đổi thiết bị/đổi proxy/restore/login — nên thời gian chuẩn bị bị loại.
        // QUY TRÌNH GIỮ NGUYÊN, chỉ đổi mốc tính thời gian.
        private TimeSpan LimitElapsed => _mainService.AccountLimitElapsed;
        // Thư mục đích upload (/sdcard/pictures) chỉ cần bảo đảm TỒN TẠI một lần cho mỗi
        // thiết bị/lần chạy. Các story sau bỏ qua 3 lệnh mkdir qua ADB (~0.3-1s/story).
        private bool _remoteUploadFolderEnsured = false;
        public FacebookFarming(MainService mainService)
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
            if (setting.ContainsKey("timeoutTaiKhoan"))
            {
                if (LimitElapsed >= TimeSpan.FromMinutes(Convert.ToInt32(setting["timeoutTaiKhoan"])))
                {
                    _mainService.SetStatus($"Đã quá {setting["timeoutTaiKhoan"]} phút cho tài khoản này!", 2);
                    AutoAndroid.RunHistoryLog.Note(_client?.Device?.Serial ?? "?",
                        $"[TIMEOUT] uid={_account?.Uid} | giới hạn thời gian mỗi tài khoản ({setting["timeoutTaiKhoan"]} phút, TÍNH CẢ chuẩn bị acc) đã hết sau {Math.Round(LimitElapsed.TotalMinutes, 1)} phút -> đổi tài khoản khác.");
                    return true;
                }
            }
            if (setting.ContainsKey("timeoutKichBan"))
            {
                if (LimitElapsed >= TimeSpan.FromMinutes(Convert.ToInt32(setting["timeoutKichBan"])))
                {
                    _mainService.SetStatus($"Đã quá {setting["timeoutKichBan"]} phút cho kịch bản này!", 2);
                    AutoAndroid.RunHistoryLog.Note(_client?.Device?.Serial ?? "?",
                        $"[TIMEOUT] uid={_account?.Uid} | giới hạn thời gian mỗi kịch bản ({setting["timeoutKichBan"]} phút, TÍNH CẢ chuẩn bị acc) đã hết sau {Math.Round(LimitElapsed.TotalMinutes, 1)} phút -> đổi tài khoản khác.");
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// Kịch bản ĐƯỢC CHỌN có chứa hành động "Kháng spam" (HDKhangSpam) không.
        /// MainService gọi trước khi mở app Facebook + Login(): tài khoản kháng 282 ĐANG bị
        /// checkpoint nên Login() sẽ ném SubdyExtension(CP_282) và huỷ cả vòng account TRƯỚC khi
        /// tới StartAction -> HDKhangSpam không bao giờ chạy (không inject cookie, không mở Chrome).
        /// HDKhangSpam tự login bằng cookie trong Chrome nên bước login qua app là thừa.
        /// Resolve script BẰNG ĐÚNG logic của ExecuteAsync để kết quả luôn khớp kịch bản sẽ chạy.
        /// </summary>
        public bool ScriptHasKhangSpam()
        {
            try
            {
                string scriptName = _account?.NameScript?.Trim() ?? "";
                Script script;
                if (string.IsNullOrEmpty(scriptName))
                {
                    var all = _scriptContext.GetByPlatform(_mainService._platform) ?? new List<Script>();
                    script = all.FirstOrDefault(s => !string.Equals(s.Name, "FarmXu", StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    script = _scriptContext.GetByName(scriptName, _mainService._platform);
                }
                if (script == null) return false;

                List<ScriptAction> actions = _scriptActionContext.GetByScriptId(script.Id);
                return actions != null && actions.Any(a => a.Type == FacebookFarmingType.HDKhangSpam);
            }
            catch
            {
                // Không đọc được script thì giữ hành vi cũ (login qua app bình thường).
                return false;
            }
        }

        public async Task ExecuteAsync()
        {
            string scriptName = _account?.NameScript?.Trim() ?? "";
            if (string.IsNullOrEmpty(scriptName))
            {
                // Không gán script → chạy script đầu tiên (không phải FarmXu) trong list
                var all = _scriptContext.GetByPlatform(_mainService._platform) ?? new List<Script>();
                _script = all.FirstOrDefault(s => !string.Equals(s.Name, "FarmXu", StringComparison.OrdinalIgnoreCase));
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
            List<ScriptAction> actions = _scriptActionContext.GetByScriptId(_script.Id);

            if (actions == null || !actions.Any())
            {
                _mainService.SetStatus("Không có hành động nào cả.", 1);
                return;
            }
            actions = actions.OrderBy(x => x.ByOrder).ToList();
            if (_configKichBan.GetBooleanValue("checkBox1"))
            {
                actions = actions.OrderBy(x => Guid.NewGuid()).ToList();
            }
            // ── GIỚI HẠN THỜI GIAN CHẠY (fix 2026-09-16: mapping lệch sau khi UI đổi tên control) ──
            // UI "Quản lý kịch bản" HIỆN dùng: checkBox2 = "Giới hạn thời gian chạy mỗi tài khoản"
            // (nudTaiKhoanFrom/To), checkBox3 = "Giới hạn thời gian chạy mỗi kịch bản" (nudKichBanFrom/To).
            // Backend CŨ đọc sai tên (checkBox3/4 + numericUpDown6..9, là control của bản UI trước)
            // nên KHÔNG BAO GIỜ áp time limit; tệ hơn, nhánh checkBox2 cũ cắt cụt actions còn
            // RandomValue(1,5) hành động -> tích "Giới hạn thời gian" lại LÀM HỎNG kịch bản.
            // Cơ chế ép limit đã có sẵn: Stop() so thời gian đã chạy với 2 key này ở ĐẦU mỗi vòng
            // action VÀ ngay TRONG các hành động dài (gate v21); quá hạn -> return -> MainService
            // đổi sang tài khoản kế. Giữ fallback numericUpDown* cho config cũ; RandomValue cận trên
            // LOẠI TRỪ nên +1 để [from..to] BAO GỒM cả 'to'.
            if (_configKichBan.GetBooleanValue("checkBox2"))
            {
                int tkFrom = _configKichBan.GetIntType("nudTaiKhoanFrom", _configKichBan.GetIntType("numericUpDown7", 40));
                int tkTo = _configKichBan.GetIntType("nudTaiKhoanTo", _configKichBan.GetIntType("numericUpDown6", 60));
                setting["timeoutTaiKhoan"] = SubdyHelper.RandomValue(tkFrom, tkTo + 1);
            }
            if (_configKichBan.GetBooleanValue("checkBox3"))
            {
                int kbFrom = _configKichBan.GetIntType("nudKichBanFrom", _configKichBan.GetIntType("numericUpDown9", 5));
                int kbTo = _configKichBan.GetIntType("nudKichBanTo", _configKichBan.GetIntType("numericUpDown8", 10));
                setting["timeoutKichBan"] = SubdyHelper.RandomValue(kbFrom, kbTo + 1);
            }
            // [TIME-LIMIT v25] KHÔNG Restart đồng hồ ở đây nữa. Mốc bấm giờ do MainService đặt lúc
            // BẮT ĐẦU CHUẨN BỊ tài khoản (_swAccountLimit.Restart() ngay sau _account.Running = true),
            // nên hạn mức TÍNH CẢ thời gian chuẩn bị acc. Dòng ghi dưới đây chỉ để truy vết phần
            // hạn mức đã bị chuẩn bị acc ăn mất (logging-only, không đổi hành vi).
            AutoAndroid.RunHistoryLog.Note(_client?.Device?.Serial ?? "?",
                $"[TIMEOUT] uid={_account?.Uid} | bắt đầu chạy kịch bản; chuẩn bị acc đã dùng {Math.Round(LimitElapsed.TotalMinutes, 1)} phút"
                + (setting.ContainsKey("timeoutTaiKhoan") ? $" trong hạn mức {setting["timeoutTaiKhoan"]} phút mỗi tài khoản." : "."));
            _mainService._sate = "Tải kịch bản";

            for (int i = 1; i <= actions.Count; i++)
            {
                var action = actions[i - 1];
                if (Stop())
                    return;
                _mainService._sate = $"Thực hiện {i}/{actions.Count}: {action.Name}";
                _mainService.SetStatus($"Đang thực hiện...", 0);
                _mainService.SetStatus("Đang kiểm tra tài khoản...", 2);
                // Hành động "Kháng spam" (gỡ checkpoint 282) chủ đích chạy TRÊN tài khoản ĐANG bị
                // checkpoint 282. HanderAccount ném SubdyExtension(CP_282) ngay ngoài try/catch -> sẽ
                // huỷ cả vòng lặp account trước khi tới StartAction. Nên bỏ qua precheck này; HDKhangSpam
                // tự đăng nhập qua Chrome bằng cookie của _account và tự xử lý màn 282.
                if (action.Type != FacebookFarmingType.HDKhangSpam)
                {
                    await _mainService._facebookService.HanderAccount(_client, _account, 5, _mainService._ct, _mainService);
                }
                try
                {
                    await StartAction(action);
                }
                catch
                {

                }
                await _mainService.ExtractAndUpdateAuthenticationInfoAsync();
            }
      
        }

        public async Task StartAction(ScriptAction action)
        {
            // KHÔNG default "Thành công" nữa (bug user 2026-09-15: action bail sớm return 0 — vd thư
            // mục ảnh 282 hết ảnh, chưa mở Chrome — vẫn bị finally báo "Thành công" vì error chỉ đổi
            // khi có exception). Mặc định RỖNG; mỗi case tự báo kết quả THẬT của nó ở finally dưới.
            string error = string.Empty;
            try
            {
                JsonHelper jsonHelper = new JsonHelper(action.Json, true);
                lock (Globals.Lock)
                {
                    if (!_data.ContainsKey($"{action.Id}_txtComments"))
                    {
                        _data[$"{action.Id}_txtComments"] = jsonHelper.GetValuesList("txtComments", jsonHelper.GetBooleanValue("ckbMultilineComment"));
                    }
                    if (!_data.ContainsKey($"{action.Id}_txtUids"))
                    {
                        _data[$"{action.Id}_txtUids"] = jsonHelper.GetValuesList("txtUids");
                    }
                    if (!_data.ContainsKey($"{action.Id}_txtLinks"))
                    {
                        _data[$"{action.Id}_txtLinks"] = jsonHelper.GetValuesList("txtLinks", jsonHelper.GetBooleanValue("rbNganCachKyTu"));
                    }
                    if (!_data.ContainsKey($"{action.Id}_txtPathAnh"))
                    {
                        string folder = jsonHelper.GetValue("txtPathAnh");
                        if (Directory.Exists(folder))
                        {
                            _data[$"{action.Id}_txtPathAnh"] = SubdyHelper.GetMedias(folder);
                        }

                    }
                    if (!_data.ContainsKey($"{action.Id}_txtPathImageComment"))
                    {
                        string folder = jsonHelper.GetValue("txtPathImageComment");
                        if (Directory.Exists(folder))
                        {
                            _data[$"{action.Id}_txtPathImageComment"] = SubdyHelper.GetMedias(folder);
                        }

                    }
                }

                switch (action.Type)
                {
                    case FacebookFarmingType.HDDocThongBao:
                        await HDDocThongBao(jsonHelper, action);
                        break;
                    case FacebookFarmingType.HDXemReel:
                        await HDXemReel(jsonHelper, action);
                        break;
                    case FacebookFarmingType.HDXemStory:
                        HDXemStory(0, "", jsonHelper, action.Name);
                        break;
                    case FacebookFarmingType.HDXemWatch:
                        await HDXemWatch(jsonHelper, action);
                        break;
                    case FacebookFarmingType.HDTuongTacNewfeed:
                        await HDTuongTacNewfeed(jsonHelper, action);
                        break;
                    case FacebookFarmingType.HDTuongTacBanBe:
                        await HDTuongTacBanBe(jsonHelper, action);
                        break;
                    case FacebookFarmingType.HDTuongTacNhom:
                        await HDTuongTacNhom(jsonHelper, action);
                        break;
                    case FacebookFarmingType.HDTuongTacPage:
                        await HDTuongTacPage(jsonHelper, action);
                        break;
                    case FacebookFarmingType.HDTuongTacWall:
                        await HDTuongTacWall(jsonHelper, action);
                        break;
                    case FacebookFarmingType.HDTuongTacEvent:
                        await HDTuongTacEvent(jsonHelper, action);
                        break;
                    case FacebookFarmingType.HDTuongTacBaiViet:
                        await HDTuongTacBaiViet(jsonHelper, action);
                        break;
                    case FacebookFarmingType.HDTuongTacVideoLivestream:
                        await HDTuongTacLivestream(jsonHelper, action);
                        break;
                    case FacebookFarmingType.HDGuiLoiMoiKetBan:
                        await HDGuiLoiMoiKetBan(jsonHelper, action);
                        break;
                    case FacebookFarmingType.HDXacNhanKetBan:
                        await HDXacNhanKetBan(jsonHelper, action);
                        break;
                    case FacebookFarmingType.HDHuyKetBan:
                        await HDHuyKetBan(jsonHelper, action);
                        break;
                    case FacebookFarmingType.HDThamGiaNhom:
                        await HDThamGiaNhom(jsonHelper, action);
                        break;
                    case FacebookFarmingType.HDRoiNhom:
                        HDRoiNhom(0, "", jsonHelper, action.Name);
                        break;
                    case FacebookFarmingType.HDTaoNhom:
                        HDTaoNhom(0, "", jsonHelper, action.Name);
                        break;
                    case FacebookFarmingType.HDTaoPage:
                        HDTaoPage(0, "", jsonHelper, action.Name);
                        break;
                    case FacebookFarmingType.HDDangBaiTuong:
                        await HDDangBaiTuong(jsonHelper, action);
                        break;
                    case FacebookFarmingType.HDDangBaiNhom:
                        await HDDangBaiNhom(jsonHelper, action);
                        break;
                    case FacebookFarmingType.HDShareBaiNangCao:
                        await HDShareBaiNangCao(jsonHelper, action);
                        break;
                    case FacebookFarmingType.HDDangReel:
                        await HDDangReel(jsonHelper, action);
                        break;
                    case FacebookFarmingType.HDDangStory:
                        await HDDangStory(jsonHelper, action);
                        break;
                    case FacebookFarmingType.HDDanhGiaPage:
                        HDDanhGiaPage(0, "", jsonHelper, action.Id.ToString(), action.Name);
                        break;
                    case FacebookFarmingType.HDBuffFollowUID:
                        HDBuffFollowUID(0, "", jsonHelper, action.Name);
                        break;
                    case FacebookFarmingType.HDMoiBanBeLikePage:
                        HDMoiBanBeLikePage(0, "", jsonHelper, action.Name);
                        break;
                    case FacebookFarmingType.HDMoiBanBeVaoNhom:
                        HDMoiBanBeVaoNhom(0, "", jsonHelper, action.Name);
                        break;
                    case FacebookFarmingType.HDDoiTen:
                        {
                            int status = 0;
                            HDDoiTen(ref status, 0, "", jsonHelper, action.Name);
                        }
                        break;
                    case FacebookFarmingType.HDDoiMatKhau:
                        {
                            int status = 0;
                            HDDoiMatKhau(ref status, 0, "", jsonHelper, action.Name);
                        }
                        break;
                    case FacebookFarmingType.HDOnOff2FA:
                        HDOnOff2FA(0, "", jsonHelper, action.Name);
                        break;
                    case FacebookFarmingType.HDXoaSdt:
                        HDXoaSdt(0, "", action.Name);
                        break;
                    case FacebookFarmingType.HDAddMail:
                        {
                            int resultCode = 0, primaryStatus = 0, addMailStatus = 0;
                            HDAddMail(ref resultCode, ref primaryStatus, ref addMailStatus, 0, "", jsonHelper, action.Id.ToString(), action.Name);
                        }
                        break;
                    case FacebookFarmingType.HDCapNhatThongTin:
                        {
                            string updatedFields = "";
                            HDCapNhatThongTin(ref updatedFields, 0, "", jsonHelper, action.Name);
                        }
                        break;
                    case FacebookFarmingType.HDDangXuatThietBiCu:
                        HDDangXuatThietBiCu(0, "", action.Name);
                        break;
                    case FacebookFarmingType.HDXoaThietBiTinCay:
                        HDXoaThietBiTinCay(0, "", action.Name);
                        break;
                    case FacebookFarmingType.HDBatCheDoChuyenNghiep:
                        HDBatCheDoChuyenNghiep(0, "", action.Name);
                        break;
                    case FacebookFarmingType.HDDongBoDanhBa:
                        HDDongBoDanhBa(0, "", jsonHelper, action.Id.ToString(), action.Name);
                        break;
                    case FacebookFarmingType.HDTimKiemGoogle:
                        break;
                    case FacebookFarmingType.HDNhanTinBanBe:
                        break;
                    case FacebookFarmingType.HDUpAvatar:
                        await HDUpAvatar(jsonHelper, action);
                        break;
                    case FacebookFarmingType.HDUpCover:
                        await HDUpCover(jsonHelper, action);
                        break;
                    case FacebookFarmingType.HDNghiGiaiLao:
                        HDNghiGiaiLao(0, "", jsonHelper, action.Name);
                        break;
                    case FacebookFarmingType.HDDangBaiPage:
                        HDDangBaiPage(0, "", jsonHelper, action.Name, action.Id.ToString());
                        break;
                    case FacebookFarmingType.HDBuffLikePage:
                        HDBuffLikePage(0, "", jsonHelper, action.Name);
                        break;

                    // Sub-mode (Phase C)
                    case FacebookFarmingType.HDKetBanGoiY:
                        {
                            int successCount = 0;
                            HDKetBanGoiY(ref successCount, 0, "", jsonHelper, action.Name);
                        }
                        break;
                    case FacebookFarmingType.HDKetBanTheoTuKhoa:
                        HDKetBanTheoTuKhoa(0, "", jsonHelper, action.Name);
                        break;

                    // Form 2B (Phase C)
                    case FacebookFarmingType.HDSpamBanBe:
                        HDSpamBanBe(0, "", jsonHelper, action.Name, action.Id.ToString());
                        break;
                    case FacebookFarmingType.HDSpamNhom:
                        HDSpamNhom(0, "", jsonHelper, action.Name, action.Id.ToString());
                        break;
                    case FacebookFarmingType.HDSpamBaiViet:
                        HDSpamBaiViet(0, "", jsonHelper, action.Name, action.Id.ToString());
                        break;
                    case FacebookFarmingType.HDSpamNewfeed:
                        HDSpamNewfeed(0, "", jsonHelper, action.Name);
                        break;
                    case FacebookFarmingType.HDXoaReel:
                        HDXoaReel(0, "", jsonHelper, action.Name, "", "");
                        break;
                    case FacebookFarmingType.HDVerifyAccount:
                        {
                            int verifyResult = 0;
                            HDVerifyAccount(ref verifyResult, 0, "", jsonHelper, action.Id.ToString(), action.Name);
                        }
                        break;
                    case FacebookFarmingType.HDCauHinhTaiKhoan:
                        HDCauHinhTaiKhoan(0, "", action.Name);
                        break;
                    case FacebookFarmingType.HDKhangSpam:
                        {
                            // Bắt kết quả THẬT (1 = kháng thành công) để finally KHÔNG báo nhầm
                            // "Thành công" khi action bail sớm (hết ảnh / thiếu key / cookie die...).
                            int khangResult = await HDKhangSpam(jsonHelper, action);
                            error = khangResult == 1 ? "Thành công" : "Không hoàn tất (xem log)";
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                SubdyExtension extension;
                if (ex is not SubdyExtension subdyEx)
                {
                    extension = new SubdyExtension(SubdyEnum.Error, ex.Message);
                }
                else
                {
                    extension = subdyEx;
                }

                if (extension.SubdyEnum == SubdyEnum.LogOut)
                {
                    var loginResult = await _mainService._facebookService.Login(_client, _account, _mainService._ct, 400, _mainService);
                    if (loginResult?.SubdyEnum == SubdyEnum.Success)
                    {
                        error = string.Empty;
                        return;
                    }
                }
                error = $"Thất bại ({extension.Message})";
                if (extension.SubdyEnum != SubdyEnum.JobFail)
                {
                    throw extension;
                }

            }
            finally
            {
                // Case KHÔNG trả kết quả (hành động thường) giữ nguyên hành vi cũ "Thành công";
                // HDKhangSpam luôn ghi error THẬT ở trên nên không bị mask nữa.
                string errShown = string.IsNullOrEmpty(error) ? "Thành công" : error;
                await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(_configKichBan.GetIntType("numericUpDown2", 5), _configKichBan.GetIntType("numericUpDown1", 15)), $"Đã chạy hành động {action.Name}.{errShown}." + " Đợi {time} giây để qua hành động tiếp theo...", 2);
                _mainService.SetStatus($"Đã chạy xong hành động {action.Name} - {errShown} ", 0);
            }



        }

        private void SetStatusAccount(int accountId, string status, int delay = 0)
        {
            _mainService.SetStatus(status, 0);
        }

        private void SetStatusAccount(int accountId, string format, int time, int delay = 0)
        {
            _mainService.SetStatus(format.Replace("{time}", time.ToString()), 0);
        }
        public async Task<int> HDDangBaiTuong(JsonHelper settings, ScriptAction action)
        {
            int soLuongBaiViet = SubdyHelper.RandomValue(settings.GetIntType("C913DC8A"), settings.GetIntType("F391713F") + 1);
            int delayFrom = settings.GetIntType("nudKhoangCachFrom");
            int delayTo = settings.GetIntType("nudKhoangCachTo");
            bool isText = settings.GetBooleanValue("A2009002");
            bool isBackgroud = settings.GetBooleanValue("BF165C93");
            bool deleteContent = settings.GetBooleanValue("ckbXoaNguyenLieuDaDung");
            bool isHastag = settings.GetBooleanValue("checkBox2");
            List<string> hastags = settings.GetValuesList("textBox1");
            int hastagFrom = settings.GetIntType("numericUpDown2");
            int hastagTo = settings.GetIntType("numericUpDown1");
            bool removeContent = settings.GetBooleanValue("checkBox1");
            bool isLink = settings.GetBooleanValue("checkBox3");
            List<string> links = settings.GetValuesList("textBox2");
            bool isMedia = settings.GetBooleanValue("ckbAnh");
            int mediaFrom = settings.GetIntType("nudSoLuongAnhFrom");
            int mediaTo = settings.GetIntType("nudSoLuongAnhTo");
            bool deleteMedia = settings.GetBooleanValue("checkBox4");
            bool isNeuBat = settings.GetBooleanValue("checkBox5");
            bool isFollower = settings.GetBooleanValue("checkBox7");
            bool isComment = settings.GetBooleanValue("checkBox6");
            int commentFrom = settings.GetIntType("nudCommentFrom");
            int commentTo = settings.GetIntType("nudCommentTo");
            bool isNeuBatComment = settings.GetBooleanValue("ckbTagNeuBat");
            bool isFollowerComment = settings.GetBooleanValue("checkBox12");
            bool isCommentMedia = settings.GetBooleanValue("checkBox11");
            bool isDeleteMediaComment = settings.GetBooleanValue("checkBox10");
            bool isDeleteComent = settings.GetBooleanValue("checkBox9");
            bool isTrungLapComent = settings.GetBooleanValue("checkBox8");

            int countPost = 1;
            int refail = 0;
            while (!_mainService._ct.IsCancellationRequested)
            {
                if (countPost > soLuongBaiViet) break;

                List<string> medias = new List<string>();
                string content = string.Empty;
                string link = string.Empty;
                List<string> hastag = new List<string>();
                if (isText && _data.ContainsKey($"{action.Id}_txtLinks") && _data[$"{action.Id}_txtLinks"].Any())
                {
                    lock (Globals.Lock)
                    {
                        content = SubdyHelper.GetStringRandom(_data[$"{action.Id}_txtLinks"]);
                        if (removeContent)
                        {
                            _data[$"{action.Id}_txtLinks"].Remove(content);
                        }
                        if (deleteContent)
                        {
                            var context = new ScriptActionContext();
                            settings.DeleteValue("txtLinks", content);
                            action.Json = settings.GetJsonString();
                            context.Update(action);
                        }
                    }
                }
                if (isHastag && hastags.Any())
                {
                    hastag = SubdyHelper.Shuffle(hastags, SubdyHelper.RandomValue(hastagFrom, hastagTo));
                }
                if (isLink)
                {
                    link = SubdyHelper.GetStringRandom(links);
                }
                if (isMedia && _data.ContainsKey($"{action.Id}_txtPathAnh") && _data[$"{action.Id}_txtPathAnh"].Any())
                {
                    medias = SubdyHelper.Shuffle(_data[$"{action.Id}_txtPathAnh"], SubdyHelper.RandomValue(mediaFrom, mediaTo));
                    if (deleteMedia)
                    {
                        foreach (var media in medias)
                        {
                            _data[$"{action.Id}_txtPathAnh"].Remove(media);
                        }
                    }
                }
                try
                {
                    if (!_client.ElementWithAttributes("//*[@content-desc=\"Make a post on Facebook\"]", 1, click: false))
                    {
                        _mainService.SetStatus($"({countPost}/{soLuongBaiViet}), Đang về trang chủ...", 2);
                        OpenFacebookTimeline();
                        _client.Delay(2);
                    }
                    int tickCount = Environment.TickCount;
                    int num6 = 300;
                    while (!_mainService._ct.IsCancellationRequested)
                    {
                        string xml = _client.GetXMLSource();
                        string xpath = _client.FindElement(xml, new List<string> {
                            "//*[@content-desc=\"Make a post on Facebook\"]",
                            "//*[@text=\"Create post\"]"
                        }, 1);
                        string text4;
                        switch (xpath)
                        {
                            case "//*[@content-desc=\"Make a post on Facebook\"]":
                                _mainService.SetStatus("Đang mở khung đăng bài...", 2, logDetail: $"[Farming.DangBai] tap={xpath}");
                                _client.ElementWithAttributes(xpath, 5, xml);
                                break;
                            default:
                                _mainService.SetStatus($"({countPost}/{soLuongBaiViet}), Đang lướt tìm nút đăng bài...", 2);
                                if (ScrollScreen(-1))
                                {
                                    //  kiem tra dang nhap
                                    int num7 = Login();
                                    if (num7 == 1 || num7 == 0)
                                    {
                                        break;
                                    }
                                    return countPost;
                                }
                                break;
                            case "//*[@text=\"Create post\"]":
                                if (_client.ElementWithAttributes("//*[@content-desc=\"Choose privacy Friends\"]"))
                                {
                                    _client.ElementWithAttributes("//*[@text=\"Public\"]");
                                    if (!_client.ElementWithAttributes("//*[@text=\"Done\"]"))
                                    {
                                        _client.ElementWithAttributes("//*[@content-desc=\"Back\"]");
                                        _client.Delay(2);
                                    }
                                }
                                _client.ElementWithAttributes("//*[@class='android.widget.EditText']");
                                _client.Delay(3);
                                _client.ADBKeyboardService.ClearInputWithADBKeyboard();
                                if (!string.IsNullOrEmpty(content))
                                {
                                    _mainService.SetStatus($"({countPost}/{soLuongBaiViet}), Đang nhập nội dung bài viết...", 2);
                                    content = SubdyHelper.SpinText(content);
                                    _client.Delay(2);

                                    _client.ADBKeyboardService.Input(content, false);
                                    _client.Delay(2);
                                    _client.ADB.Shell("input keyevent 62");
                                    _client.Delay(2);
                                    if (isBackgroud)
                                    {
                                        _mainService.SetStatus($"({countPost}/{soLuongBaiViet}), Đang tìm màu nền...", 2);
                                        var list3 = _client.FindBounds("", "//*[contains(@content-desc,\", background\")]", 0);
                                        if (list3.Count > 2)
                                        {
                                            var list4 = _client.GetAttributeValuesFromXmlNodes("", "//*[contains(@content-desc,\", background\")]");
                                            list4.RemoveAt(list4.Count - 1);
                                            _mainService.SetStatus($"({countPost}/{soLuongBaiViet}), Đang chọn màu nền...", 2);
                                            var point = ParseCoordinate(list4[SubdyHelper.RandomValue(0, list4.Count)]);
                                            _client.Click(point.X, point.Y);
                                        }
                                    }
                                    if (isHastag && hastag.Any())
                                    {
                                        _mainService.SetStatus($"({countPost}/{soLuongBaiViet}), Đang nhập hashtag...", 2);
                                        foreach (var item in hastag)
                                        {
                                            string text = "";
                                            if (!item.StartsWith("#"))
                                            {
                                                text = "#";
                                            }
                                            text += item;
                                            bool check = false;
                                            foreach (char c in item)
                                            {
                                                _client.ADBKeyboardService.Input(c.ToString(), false);
                                                var nodes = _client.FindElementsNotToLower(0, "", "//node[contains(@class, 'android.widget.Button') and @content-desc and string-length(@content-desc) > 0 and " +
                               $"contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), '{text}') and " +
                               $"contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'posts') and " +
                               "@visible-to-user='true']");
                                                if (!nodes.Any())
                                                {
                                                    continue;
                                                }
                                                var info = _client.ExtractNodeInfo(nodes.FirstOrDefault().OuterXml);
                                                if (!info.ContainsKey("bounds")) continue;
                                                var point = new RectangleArea(info["bounds"]).RandomPoint();
                                                _client.Click(point.X, point.Y);
                                                _client.ADB.Shell("input keyevent 62");
                                                check = true;
                                                break;
                                            }
                                            if (!check)
                                            {
                                                for (int i = 0; i < text.Length; i++)
                                                {
                                                    _client.ATX.Press(PressKey.Delete);
                                                }
                                            }
                                        }
                                    }
                                }
                                if (isLink && !string.IsNullOrEmpty(link))
                                {
                                    _mainService.SetStatus($"({countPost}/{soLuongBaiViet}), Đang nhập liên kết...", 2);
                                    _client.ADBKeyboardService.Input(link, false);
                                    _client.ADB.Shell("input keyevent 62");
                                    if (_client.ElementWithAttributes("//*[contains(@content-desc,'Shared Link')]", 10, click: false))
                                    {
                                        for (int i = 0; i < link.Length; i++)
                                        {
                                            _client.ATX.Press(PressKey.Delete);
                                        }
                                    }
                                }
                                if (isNeuBat)
                                {
                                    _mainService.SetStatus($"Đang gắn thẻ nổi bật...", 2);
                                    List<string> neubats = new List<string> { "@highlight", "@neu" };
                                    foreach (var item in neubats)
                                    {
                                        bool click = false;
                                        var count = item.Length;
                                        _client.ADB.Shell("input keyevent 62");
                                        foreach (char c in item)
                                        {
                                            _client.ADBKeyboardService.Input(c.ToString(), false);
                                            if (_client.ElementWithAttributes(new List<string> { "//*[@text=\"@highlight\"]", "//*[@text=\"@nêu bật\"]" }, 0))
                                            {
                                                click = true;
                                                break;
                                            }
                                        }
                                        if (!click)
                                        {
                                            for (int i = 0; i < count + 1; i++)
                                            {
                                                _client.Shell("input keyevent KEYCODE_DEL");
                                                Thread.Sleep(50);
                                            }
                                        }
                                        if (click)
                                        {
                                            break;
                                        }

                                    }
                                }
                                if (isFollower)
                                {
                                    _mainService.SetStatus($"Đang gắn thẻ người theo dõi...", 2);

                                    List<string> neubats = new List<string> { "@followers", "@nguoi" };
                                    foreach (var item in neubats)
                                    {
                                        bool click = false;
                                        var count = item.Length;
                                        _client.ADB.Shell("input keyevent 62");
                                        foreach (char c in item)
                                        {
                                            _client.ADBKeyboardService.Input(c.ToString(), false);
                                            if (_client.ElementWithAttributes(new List<string> { "//*[@text=\"@followers\"]", "//*[@text=\"@người theo dõi\"]" }, 0))
                                            {
                                                click = true;
                                                break;
                                            }
                                        }
                                        if (!click)
                                        {
                                            for (int i = 0; i < count + 1; i++)
                                            {
                                                _client.Shell("input keyevent KEYCODE_DEL");
                                                Thread.Sleep(50);
                                            }
                                        }
                                        if (click)
                                        {
                                            break;
                                        }

                                    }
                                }
                                if (isMedia && medias.Any())
                                {
                                    UploadMediaFiles(medias);
                                    _client.ElementWithAttributes("//*[@content-desc='Photo/video']", 5);
                                    _mainService.SetStatus($"({countPost}/{soLuongBaiViet}), Đang tải ảnh/video lên...", 2);
                                    bool flag8 = false;
                                    for (int j = 0; j < 5; j++)
                                    {
                                        xml = _client.GetXMLSource();
                                        xpath = _client.FindElement(xml, new List<string> { "//*[@text='ALLOW']", "//*[@content-desc='Allow access']", "//*[@content-desc='Choose layout']", "//*[contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'photo') or contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'video')]" }, 5);

                                        if (!(xpath == "//*[@content-desc='Choose layout']") && !(xpath == "//*[contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'photo') or contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'video')]"))
                                        {
                                            if (xpath != "")
                                            {
                                                _mainService.SetStatus($"({countPost}/{soLuongBaiViet}), Đang mở khung đăng bài...", 2, logDetail: $"[Farming.DangBai] tap={xpath}");
                                                _client.ElementWithAttributes(xpath);
                                            }
                                            else
                                            {
                                                ScrollScreen(-1);
                                            }
                                            _client.Delay(1);
                                            continue;
                                        }
                                        if (!_client.ElementWithAttributes("//*[contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'photo') or contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'video')]", 1, "", false))
                                        {
                                            break;
                                        }
                                        if (_client.ElementWithAttributes("//*[@content-desc='Select multiple']", 1, xml))
                                        {
                                            _client.Delay(2);
                                        }
                                        List<string> list4 = _client.GetAttributeValuesFromXmlNodes("", "//*[contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'photo') or contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'video')]/parent::*[@selected='false']");
                                        for (int k = 0; k < medias.Count; k++)
                                        {
                                            for (int l = 0; l < medias.Count; l++)
                                            {
                                                while (list4.Count == 0)
                                                {
                                                    _mainService.SetStatus($"({countPost}/{soLuongBaiViet}), Đang lướt tìm nút đăng bài...", 2);
                                                    if (ScrollScreen())
                                                    {
                                                        break;
                                                    }
                                                    list4 = _client.GetAttributeValuesFromXmlNodes("", "//*[contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'photo') or contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'video')]/parent::*[@selected='false']");
                                                }
                                                if (list4.Count == 0)
                                                {
                                                    break;
                                                }
                                                string text5 = list4.OrderBy((string F911C492) => Guid.NewGuid()).FirstOrDefault();
                                                list4.Remove(text5);
                                                var point = new RectangleArea(text5).GetCenterPoint();
                                                _client.Click(point.X, point.Y);
                                            }
                                            if (list4.Count == 0 || _client.GetAttributeValuesFromXmlNodes("", "//*[contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'photo') or contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'video')]/parent::*[@selected='true']").Count >= 5)
                                            {
                                                break;
                                            }
                                        }
                                        _client.ElementWithAttributes("//*[@content-desc=\"Next\"]");
                                        _client.Delay(10);
                                        flag8 = true;
                                        break;
                                    }
                                    if (!flag8)
                                    {
                                        _client.Shell("input keyevent 4");
                                    }
                                }
                                _mainService.SetStatus($"({countPost}/{soLuongBaiViet}), Đang mở khung đăng bài...", 2, logDetail: $"[Farming.DangBai] tap={xpath}");
                                _client.ElementWithAttributes("//*[@content-desc='NEXT'][@enabled='true']");
                                int tickCount1 = Environment.TickCount;
                                await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(3, 6), $"({countPost}/{soLuongBaiViet}), Tap Post, " + "đợi" + " {time}s...", 2);
                                while (!_mainService._ct.IsCancellationRequested)
                                {
                                    if (ContainsAnyKeyword("", "android.widget.ProgressBar", "Row showing that your post is", "Sharing", "Uploading", "Finishing up", "Updating", "Posting"))
                                    {
                                        break;
                                    }
                                    _client.ElementWithAttributes(new List<string> { "//*[@content-desc=\"OK\"]", "//*[@content-desc=\"Share now\"]" });
                                    if (Environment.TickCount - tickCount1 < 30 * 1000)
                                    {
                                        continue;
                                    }
                                    break;
                                }
                                _mainService.SetStatus($"({countPost}/{soLuongBaiViet})" + "đợi" + " post success...", 2);
                                if (WaitForPostComplete(300))
                                {
                                    if (isComment)
                                    {
                                        _mainService.SetStatus($"({countPost}/{soLuongBaiViet}), " + " Bình luận ...", 2);
                                        DeplinkFacebook("fb://profile");
                                        tickCount1 = Environment.TickCount;
                                        while (!_mainService._ct.IsCancellationRequested)
                                        {
                                            if (!_client.ElementWithAttributes("//*[@content-desc=\"Profile Picture\"]", 5, "", false))
                                            {
                                                ScrollScreen(-1);
                                            }
                                            else
                                            {
                                                string imageComment = "";
                                                string comment = string.Empty;
                                                if (_data.ContainsKey($"{action.Id}_txtComments"))
                                                {
                                                    lock (Globals.Lock)
                                                    {
                                                        var contents = _data[$"{action.Id}_txtComments"];
                                                        if (contents.Any())
                                                        {
                                                            comment = SubdyHelper.GetStringRandom(contents);
                                                            if (!settings.GetBooleanValue("checkBox5"))
                                                            {
                                                                contents.Remove(content);
                                                                _data[$"{action.Id}_txtComments"] = contents;
                                                            }
                                                            if (settings.GetBooleanValue("checkBox4"))
                                                            {
                                                                var context = new ScriptActionContext();
                                                                settings.DeleteValue("txtComments", content);
                                                                action.Json = settings.GetJsonString();
                                                                context.Update(action);
                                                            }
                                                        }

                                                    }
                                                    comment = SubdyHelper.SpinText(comment);
                                                }
                                                lock (Globals.Lock)
                                                {
                                                    var images = _data[$"{action.Id}_txtPathImageComment"];
                                                    imageComment = SubdyHelper.GetStringRandom(images);
                                                    if (isDeleteMediaComment)
                                                    {
                                                        _data[$"{action.Id}_txtPathImageComment"].Remove(imageComment);
                                                    }
                                                }
                                                if (!string.IsNullOrEmpty(comment) || File.Exists(imageComment))
                                                {
                                                    List<string> reactionTypes = new List<string> { "Like", "Love", "Care", "Haha" };
                                                    TapReaction(SubdyHelper.GetStringRandom(reactionTypes));
                                                    string message = await CommentAction(comment, imageComment, isNeuBatComment, isFollowerComment);
                                                    await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(3, 7), $"{message}, đợi {{time}}s...", 2);
                                                    if (isDeleteMediaComment)
                                                    {
                                                        File.Delete(imageComment);
                                                    }
                                                    break;
                                                }
                                                break;
                                            }
                                            if (Environment.TickCount - tickCount1 < 30 * 1000)
                                            {
                                                continue;
                                            }
                                            break;
                                        }
                                    }

                                    await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(delayFrom, delayTo), $"({countPost}/{soLuongBaiViet}), " + "đợi" + " {time}s...", 2);
                                    countPost++;
                                }
                                break;
                        }
                        Thread.Sleep(1000);
                        if (Environment.TickCount - tickCount < num6 * 1000)
                        {
                            continue;
                        }
                        break;
                    }
                }
                finally
                {

                }
            }

            return countPost;
        }
        public async Task<int> HDUpCover(JsonHelper settings, ScriptAction action)
        {
            int targetCount = 1;
            int delayFrom = settings.GetIntType("nudKhoangCachFrom", 5);
            int delayTo = settings.GetIntType("nudKhoangCachTo", 10);

            int successCount = 0;
            int failCount = 0;

            while (!_mainService._ct.IsCancellationRequested)
            {
                if (successCount >= targetCount) break;

                int result = await HDUpCoverOld(0, "", settings, action.Name);
                if (result == 1)
                {
                    successCount++;
                }
                else
                {
                    failCount++;
                }

                if (successCount < targetCount)
                {
                    int delay = SubdyHelper.RandomValue(delayFrom, delayTo + 1);
                    await Task.Delay(delay * 1000);
                }
            }

            return successCount;
        }

        public async Task<int> HDUpCoverOld(int accountId, string statusPrefix, JsonHelper jsonHelper, string actionName)
        {
            bool isSuccess = false;
            string folderPath = jsonHelper.GetValue("txtPathFolder");
            bool shouldDeleteUsedPhoto = jsonHelper.GetBooleanValue("ckbXoaAnhDaDung");

            if (Directory.GetFiles(folderPath).Length != 0)
            {
                string status = statusPrefix + "Đang" + " " + actionName + ": ";
                SetStatusAccount(accountId, status + "Đang chạy...");
                try
                {
                    int photoDisabledCount = 0;
                    int maxPhotoDisabled = 3;
                    int tapToRetryCount = 0;
                    int maxTapToRetry = 6;

                    while (OpenFacebookLink(accountId, status, "fb://profile_edit"))
                    {
                        string photoPath = "";
                        if (shouldDeleteUsedPhoto)
                        {
                            lock (D739380E)
                            {
                                photoPath = Directory.GetFiles(folderPath)
                                    .OrderBy(_ => Guid.NewGuid())
                                    .FirstOrDefault();
                                if (string.IsNullOrEmpty(photoPath))
                                {
                                    break;
                                }
                                UploadMediaFiles(new List<string> { photoPath });
                                SubdyHelper.DeleteFile(photoPath);
                                goto SelectPhoto;
                            }
                        }
                        photoPath = Directory.GetFiles(folderPath)
                            .OrderBy(_ => Guid.NewGuid())
                            .FirstOrDefault();

                        if (!string.IsNullOrEmpty(photoPath))
                        {
                            UploadMediaFiles(new List<string> { photoPath });
                            goto SelectPhoto;
                        }
                        break;

                    SelectPhoto:
                        string xmlSource = "";
                        int startTick = Environment.TickCount;
                        bool photoSelected = false;
                        do
                        {
                            xmlSource = _client.GetXMLSource();
                            string foundXPath = _client.FindElement(xmlSource, new List<string> {
                        "//*[@class='android.widget.ProgressBar']",
                        "//*[@text='Tap to retry']",
                        "//*[contains(@content-desc,'cover photo')]",
                        "//*[@text='ALLOW' or @content-desc='ALLOW']",
                        "//*[@text='SAVE' or @content-desc='SAVE']",
                        "(//*[contains(@content-desc, 'Photo taken') or contains(@text, 'Photo taken')])[1]",
                        "//*[@content-desc='Photo. Disabled.']"
                    }, 1);

                            if (foundXPath == "//*[@content-desc='Photo. Disabled.']")
                            {
                                if (photoDisabledCount >= maxPhotoDisabled)
                                {
                                    goto EndMethod;
                                }
                                photoDisabledCount++;
                                _client.ElementWithAttributes("//*[@content-desc='Back']", 1, xmlSource);
                                goto WaitLoop;
                            }
                            else if (foundXPath.Contains("cover photo"))
                            {
                                goto SaveCover;
                            }
                            else if (foundXPath == "//*[@text='ALLOW' or @content-desc='ALLOW']" ||
                                     foundXPath == "//*[@text='SAVE' or @content-desc='SAVE']")
                            {
                                goto SaveCover;
                            }
                            else if (foundXPath == "//*[@class='android.widget.ProgressBar']")
                            {
                                SetStatusAccount(accountId, status + "Loading...");
                            }
                            else if (foundXPath == "//*[@text='Tap to retry']")
                            {
                                if (tapToRetryCount >= maxTapToRetry)
                                {
                                    break;
                                }
                                tapToRetryCount++;
                                ScrollScreen(-1);
                            }
                            else if (foundXPath == "(//*[contains(@content-desc, 'Photo taken') or contains(@text, 'Photo taken')])[1]")
                            {
                                var photoElements = _client.FindBounds("", foundXPath, 1);
                                if (photoElements.Count > 1)
                                {
                                    photoElements = photoElements.GetRange(0, photoElements.Count - 1);
                                }
                                string selectedPhoto = photoElements.OrderBy(_ => Guid.NewGuid()).FirstOrDefault();
                                var point = new RectangleArea(selectedPhoto).GetCenterPoint();
                                _client.Click(point.X, point.Y);
                                photoSelected = true;
                            }
                            else
                            {
                                SetStatusAccount(accountId, status + "Scroll...");
                                await _mainService._facebookService.HanderAccount(_client, _account, 5, _mainService._ct, _mainService);
                            }
                            goto WaitLoop;

                        SaveCover:
                            if (!(foundXPath.Contains("cover photo") && photoSelected))
                            {
                                SetStatusAccount(accountId, status + "Tap " + foundXPath + "...");
                                _client.ElementWithAttributes(foundXPath, 1, xmlSource);
                                goto WaitLoop;
                            }
                            isSuccess = true;
                            break;

                        WaitLoop:
                            _client.Delay(2);
                            continue;
                        }
                        while (Environment.TickCount - startTick < 300000);
                        break;
                    EndMethod:;
                    }
                }
                catch
                {
                    // Optionally log error here
                }
            }
            return isSuccess ? 1 : 0;
        }
        public int HDSpamNhom(int accountId, string statusPrefix, JsonHelper jsonHelper, string actionName, string dataKey)
        {
            //method_84
            int minValue = jsonHelper.GetIntType("nudSoLuongUidFrom");
            int num = jsonHelper.GetIntType("nudSoLuongUidTo");
            int num2 = jsonHelper.GetIntType("nudSoLuongBaiVietFrom");
            int num3 = jsonHelper.GetIntType("nudSoLuongBaiVietTo");
            int f988D70A = jsonHelper.GetIntType("nudDelayFrom");
            int e234B = jsonHelper.GetIntType("nudDelayTo");
            bool bool_ = jsonHelper.GetBooleanValue("ckbInteract");
            string c23CDF0B = jsonHelper.GetValuesFromInputString("typeReaction");
            bool bool_2 = jsonHelper.GetBooleanValue("ckbComment");
            List<string> f1808BA = jsonHelper.GetValuesList("txtComment");
            if (f1808BA == null || f1808BA.Count == 0)
            {
                f1808BA = jsonHelper.GetValuesList("typeNganCach");
            }
            bool bool_3 = jsonHelper.GetBooleanValue("ckbAnh");
            string string_ = jsonHelper.GetValuesFromInputString("txtPathAnh");
            f1808BA = f1808BA.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            try
            {
                string text = statusPrefix + "Đang" + " " + actionName + ": ";
                int num4 = SubdyHelper.RandomValue(minValue, num + 1);
                int num5 = 0;
                while (num5 < num4)
                {
                    while (true)
                    {
                        SetStatusAccount(accountId, text + $"({num5 + 1}/{num4}), Go to Group...");
                        if (!OpenFacebookLink(accountId, text, "fb://faceweb/f?href=https://m.facebook.com/groups_browse/your_groups/"))
                        {
                            break;
                        }
                        SetStatusAccount(accountId, text + $"({num5 + 1}/{num4}), Find Group...");
                        List<string> list = _client.FindBounds("", "//android.view.View[starts-with(@content-desc,\"group image link\") or starts-with(@text,\"group image link\")]", 3);
                        SetStatusAccount(accountId, text + $"({num5 + 1}/{num4}), Find Group: " + list.Count);
                        if (list.Count == 0)
                        {
                            break;
                        }
                        SetStatusAccount(accountId, text + $"({num5 + 1}/{num4}), Scroll...");
                        int num6 = 0;
                        if (list.Count >= 9)
                        {
                            while (!ScrollScreen())
                            {
                                num6++;
                                if (num6 >= 5)
                                {
                                    break;
                                }
                            }
                            int num7 = SubdyHelper.RandomValue(0, ((num6 < 1) ? 1 : num6) - 1);
                            for (int i = 0; i < num7; i++)
                            {
                                if (ScrollScreen(-1))
                                {
                                    break;
                                }
                            }
                            SetStatusAccount(accountId, text + $"({num5 + 1}/{num4}), Find Group...");
                            list = _client.FindBounds("", "//android.view.View[starts-with(@content-desc,\"group image link\") or starts-with(@text,\"group image link\")]");
                        }
                        SetStatusAccount(accountId, text + $"({num5 + 1}/{num4}), Tap Group...");
                        var point = new RectangleArea(SubdyHelper.GetStringRandom(list)).GetCenterPoint();
                        _client.Click(point.X, point.Y);
                        _client.Delay(1);
                        switch (Login())
                        {
                            case 1:
                                break;
                            case 0:
                                goto IL_031e;
                            default:
                                goto end_IL_00cd;
                        }
                        continue;
                    IL_031e:
                        ScrollFeedAndInteract(accountId, text + $"({num5 + 1}/{num4}), ", num2, num3, bool_, c23CDF0B, num2, num3, bool_2, num2, num3, f1808BA, enableShare: false, 0, 0, 1, bool_3, string_);
                        SetStatusAccount(accountId, text + $"({num5 + 1}/{num4}), delay {{time}}s...", SubdyHelper.RandomValue(f988D70A, e234B));
                        num5++;
                        goto IL_0399;
                    }
                    break;
                IL_0399:;
                }
            end_IL_00cd:;
            }
            catch
            {
            }
            return 0;
        }
        public async Task<int> HDTuongTacBaiViet(JsonHelper settings, ScriptAction action)
        {//CC03DE3E
            int totalSeconds = SubdyHelper.RandomValue(settings.GetIntType("numericUpDown2"), settings.GetIntType("numericUpDown1"));

            int delayFrom = settings.GetIntType("nudTimeFrom");
            int delayTo = settings.GetIntType("nudTimeTo");


            bool shouldInteract = settings.GetBooleanValue("ckbInteract");
            int interactCount = SubdyHelper.RandomValue(settings.GetIntType("nudInteractFrom", 1), settings.GetIntType("nudInteractTo", 1));
            List<string> reactionTypes = new List<string> { "Like", "Love", "Care", "Haha", "Wow", "Sad", "Angry" };
            if (!settings.GetBooleanValue("ckbLike"))
            {
                reactionTypes.Remove("Like");
            }
            if (!settings.GetBooleanValue("ckbTym"))
            {
                reactionTypes.Remove("Love");
            }
            if (!settings.GetBooleanValue("ckbThuong"))
            {
                reactionTypes.Remove("Care");
            }
            if (!settings.GetBooleanValue("ckbHaha"))
            {
                reactionTypes.Remove("Haha");
            }
            if (!settings.GetBooleanValue("ckbWow"))
            {
                reactionTypes.Remove("Wow");
            }
            if (!settings.GetBooleanValue("ckbBuon"))
            {
                reactionTypes.Remove("Sad");
            }
            if (!settings.GetBooleanValue("ckbGian"))
            {
                reactionTypes.Remove("Angry");
            }
            List<string> reactions = new List<string>();
            if (reactionTypes.Any())
            {
                for (int i = 0; i < interactCount; i++)
                {
                    reactions.Add(SubdyHelper.GetStringRandom(reactionTypes));
                }
            }

            bool shouldShareWall = settings.GetBooleanValue("ckbShareWall");
            int shareCount = SubdyHelper.RandomValue(settings.GetIntType("nudShareWallFrom", 1), settings.GetIntType("nudShareWallTo", 1));



            bool shouldComment = settings.GetBooleanValue("ckbComment");
            int commentCount = SubdyHelper.RandomValue(settings.GetIntType("nudCommentFrom", 1), settings.GetIntType("nudCommentTo", 1));


            Dictionary<string, string> keyValues = new Dictionary<string, string>();
            string type = "ChiDinh";
            if (settings.GetBooleanValue("ckbTuKhoa"))
            {
                type = "TuKhoa";
            }
            switch (type)
            {
                case "ChiDinh":
                    {
                        var urls = settings.GetValuesList("txtLinks");
                        if (!urls.Any())
                        {
                            throw new SubdyExtension(SubdyEnum.Error, "Không có link chỉ định");
                        }
                        foreach (var url in urls)
                        {
                            if (!keyValues.ContainsKey(url))
                            {
                                keyValues.Add(url, url);
                            }
                        }
                        break;
                    }
                case "TuKhoa":
                    {
                        var urls = settings.GetValuesList("txtLinks");
                        if (!urls.Any())
                        {
                            throw new SubdyExtension(SubdyEnum.Error, "Không có keyword");
                        }
                        if (!SearchOnFacebook(SubdyHelper.GetStringRandom(urls), "All"))
                        {
                            throw new SubdyExtension(SubdyEnum.JobFail, "Không tìm kiếm được bài viết");
                        }

                        string xpath = "//node[contains(@class,'android.widget.ImageView') and string-length(@content-desc) > 0 and @visible-to-user='true'" +
                                  "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'discover'))" +
                                  "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'notification'))" +
                                  "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'ringer'))" +
                                  "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'member'))" +
                                  "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'back'))" +
                                  "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'clear text'))" +
                              "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'search results'))]";
                        var nodes = _client.FindElementsNotToLower(15, "", xpath);
                        if (!nodes.Any())
                        {

                            throw new SubdyExtension(SubdyEnum.JobFail, "Không tìm được page.");
                        }
                        foreach (var node in nodes)
                        {
                            var info = _client.ExtractNodeInfo(node.OuterXml);
                        }

                        break;
                    }
            }
            List<string> old = new List<string>();
            int count = 1;
            while (!_mainService._ct.IsCancellationRequested)
            {
                // [TIME-LIMIT v21] Ép giới hạn thời gian NGAY TRONG hành động dài (mỗi vòng):
                // quá timeoutTaiKhoan/timeoutKichBan -> break -> ExecuteAsync đổi tài khoản. KHÔNG đụng logic FB.
                if (Stop()) break;
                if (count > totalSeconds)
                {
                    break;
                }
                switch (type)
                {
                    case "ChiDinh":
                        {
                            if (!keyValues.Any())
                            {
                                count = totalSeconds + 1;
                                continue;
                            }
                            var firt = keyValues.First();
                            if (!DeplinkFacebook(firt.Key).Contains("dat=fb://profile"))
                            {
                                continue;
                            }
                            await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(5, 10), $"({count}/{totalSeconds}) Go to post {firt.Key}" + ", đợi {time}s...", 2);
                            keyValues.Remove(firt.Key);
                            break;
                        }
                    case "TuKhoa":
                        {
                            if (!keyValues.Any())
                            {

                                string xpath = "//node[contains(@class,'android.widget.ImageView') and string-length(@content-desc) > 0 and @visible-to-user='true'" +
                                          "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'discover'))" +
                                          "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'notification'))" +
                                          "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'ringer'))" +
                                          "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'member'))" +
                                          "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'back'))" +
                                          "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'clear text'))" +
                                      "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'search results'))]";
                                var nodes = _client.FindElementsNotToLower(15, "", xpath);
                                if (!nodes.Any())
                                {
                                    throw new SubdyExtension(SubdyEnum.JobFail, "Không tìm được page đã thích");
                                }
                                bool allExist = true;
                                foreach (var node in nodes)
                                {
                                    var info = _client.ExtractNodeInfo(node.OuterXml);
                                    if (!old.Contains(info["content-desc"]))
                                    {
                                        allExist = false;
                                        keyValues.Add(info["content-desc"], info["bounds"]);
                                    }
                                    old.Add(info["content-desc"]);
                                }
                                if (allExist)
                                {
                                    ScrollScreen(1, 1);
                                    count++;
                                    continue;
                                }
                            }

                            if (!keyValues.Any())
                            {
                                count = totalSeconds + 1;
                                continue;
                            }
                            var firt = keyValues.First();
                            var point = new RectangleArea(firt.Value).GetCenterPoint();
                            _client.ADB.Shell($"input tap {point.X} {point.Y}");
                            await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(5, 10), $"({count}/{totalSeconds}) Go to post {firt.Key}" + ", đợi {time}s...", 2);
                            keyValues.Remove(firt.Key);
                            break;
                        }
                }

                int time = SubdyHelper.RandomValue(delayTo, delayFrom);
                await _mainService.DelayMessageAsync(time, $"Xem post, đợi {{time}}s...", 2);
                if (shouldInteract && reactions.Any())
                {
                    var message = TapReaction(reactions[0]);
                    await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(3, 7), $"{message}, đợi {{time}}s...", 2);
                    reactions.RemoveAt(0);
                }
                if (shouldShareWall && shareCount > 0)
                {
                    var message = TapShareNewfeed("");
                    await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(3, 7), $"{message}, đợi {{time}}s...", 2);
                    shareCount--;
                }
                if (shouldComment && commentCount > 0)
                {
                    string image = "";
                    string content = "";
                    if (_data.ContainsKey($"{action.Id}_txtComments"))
                    {
                        lock (Globals.Lock)
                        {
                            var contents = _data[$"{action.Id}_txtComments"];
                            if (contents.Any())
                            {
                                content = SubdyHelper.GetStringRandom(contents);
                                if (!settings.GetBooleanValue("checkBox5"))
                                {
                                    contents.Remove(content);
                                    _data[$"{action.Id}_txtComments"] = contents;
                                }
                                if (settings.GetBooleanValue("checkBox4"))
                                {
                                    var context = new ScriptActionContext();
                                    settings.DeleteValue("txtComments", content);
                                    action.Json = settings.GetJsonString();
                                    context.Update(action);
                                }
                            }

                        }
                        content = SubdyHelper.SpinText(content);
                    }
                    if (settings.GetBooleanValue("ckbAnh") && _data.ContainsKey($"{action.Id}_txtPathAnh") && _data[$"{action.Id}_txtPathAnh"].Any())
                    {
                        lock (Globals.Lock)
                        {
                            var images = _data[$"{action.Id}_txtPathAnh"];
                            image = SubdyHelper.GetStringRandom(images);
                            if (settings.GetBooleanValue("checkBox3"))
                            {
                                images.Remove(image);
                                File.Delete(image);
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(content) || File.Exists(image))
                    {
                        string message = await CommentAction(content, image, settings.GetBooleanValue("ckbTagNeuBat"), settings.GetBooleanValue("checkBox2"));
                        await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(3, 7), $"{message}, đợi {{time}}s...", 2);
                    }
                    commentCount--;
                }
                _client.ATX.Press(PressKey.Back);
                _client.Delay(2);
                count++;
            }
            int result = 0;
            return result;
        }
        public async Task<int> HDDangBaiNhom(JsonHelper settings, ScriptAction action)
        {
            int soLuongBaiViet = SubdyHelper.RandomValue(settings.GetIntType("C913DC8A"), settings.GetIntType("F391713F") + 1);
            int delayFrom = settings.GetIntType("nudKhoangCachFrom");
            int delayTo = settings.GetIntType("nudKhoangCachTo");
            bool isText = settings.GetBooleanValue("A2009002");
            bool isBackgroud = settings.GetBooleanValue("BF165C93");
            bool deleteContent = settings.GetBooleanValue("ckbXoaNguyenLieuDaDung");
            bool isHastag = settings.GetBooleanValue("checkBox2");
            List<string> hastags = settings.GetValuesList("textBox1");
            int hastagFrom = settings.GetIntType("numericUpDown2");
            int hastagTo = settings.GetIntType("numericUpDown1");
            bool removeContent = settings.GetBooleanValue("checkBox1");
            bool isLink = settings.GetBooleanValue("checkBox3");
            List<string> links = settings.GetValuesList("textBox2");
            bool isMedia = settings.GetBooleanValue("ckbAnh");
            int mediaFrom = settings.GetIntType("nudSoLuongAnhFrom");
            int mediaTo = settings.GetIntType("nudSoLuongAnhTo");
            bool deleteMedia = settings.GetBooleanValue("checkBox4");
            bool isNeuBat = settings.GetBooleanValue("checkBox5");
            bool isFollower = settings.GetBooleanValue("checkBox7");
            bool isComment = settings.GetBooleanValue("checkBox6");
            int commentFrom = settings.GetIntType("nudCommentFrom");
            int commentTo = settings.GetIntType("nudCommentTo");
            bool isNeuBatComment = settings.GetBooleanValue("ckbTagNeuBat");
            bool isFollowerComment = settings.GetBooleanValue("checkBox12");
            bool isCommentMedia = settings.GetBooleanValue("checkBox11");
            bool isDeleteMediaComment = settings.GetBooleanValue("checkBox10");
            bool isDeleteComent = settings.GetBooleanValue("checkBox9");
            bool isTrungLapComent = settings.GetBooleanValue("checkBox8");
            bool isDangAnDanh = settings.GetBooleanValue("checkBox13");
            string type = "DeXuat";
            if (settings.GetBooleanValue("radioButton4"))
            {
                type = "ChiDinh";
            }
            else
            {
                DeplinkFacebook("fb://faceweb/f?href=https://m.facebook.com/groups_browse/your_groups/");
            }

            int countPost = 1;
            int refail = 0;
            List<string> olds = new List<string>();
            while (!_mainService._ct.IsCancellationRequested)
            {
                if (countPost > soLuongBaiViet && refail < 6) break;
                if (type == "DeXuat")
                {
                    string xpath = "//node[contains(@class,'android.widget.Button') and @content-desc " +
                                    "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'create a group')) " +
                                    "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'sort groups'))" +
                                    "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'your groups'))" +
                                    "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'posts'))" +
                                    "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'discover'))" +
                                    "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'manage'))" +
                                    "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'most visited'))]";
                    var friends = _client.FindElementsNotToLower(10, "", xpath);
                    if (!friends.Any())
                    {
                        DeplinkFacebook("fb://faceweb/f?href=https://m.facebook.com/groups_browse/your_groups/");
                        refail++;
                        continue;
                    }
                    bool check = false;
                    foreach (var item in friends)
                    {
                        var info = _client.ExtractNodeInfo(item.OuterXml);
                        if (info.ContainsKey("bounds") && !olds.Contains(info["content-desc"]))
                        {
                            check = true;
                            olds.Add(info["content-desc"]);
                            var point = new RectangleArea(info["bounds"]).GetCenterPoint();
                            if (!_client.Click(point.X, point.Y))
                            {
                                check = false;
                                continue;
                            }
                            _mainService.SetStatus($"({countPost}/{soLuongBaiViet}), Go to group {info["content-desc"]}...", 2);
                            break;
                        }
                        if (check) break;
                    }
                    if (!check)
                    {
                        ScrollScreen(1, 1);
                        refail++;
                        continue;
                    }
                    refail = 0;
                }
                else if (type == "ChiDinh")
                {
                    string uid = "";
                    lock (Globals.Lock)
                    {
                        if (!_data[$"{action.Id}_txtLinks"].Any())
                        {
                            break;
                        }
                        uid = SubdyHelper.GetStringRandom(_data[$"{action.Id}_txtLinks"]);
                        if (settings.GetBooleanValue("checkBox14"))
                        {
                            _data[$"{action.Id}_txtLinks"].Remove(uid);
                        }
                    }
                    DeplinkFacebook($"fb://group/{uid}");
                    _mainService.SetStatus($"({countPost}/{soLuongBaiViet}), Go to group {uid}...", 2);
                }
                if (!_client.ElementWithAttributes("//*[@content-desc=\"Write something...\"]", 20, click: true))
                {
                    refail++;
                    continue;
                }
                List<string> medias = new List<string>();
                string content = string.Empty;
                string link = string.Empty;
                List<string> hastag = new List<string>();
                if (isText && _data.ContainsKey($"{action.Id}_txtLinks") && _data[$"{action.Id}_txtLinks"].Any())
                {
                    lock (Globals.Lock)
                    {
                        content = SubdyHelper.GetStringRandom(_data[$"{action.Id}_txtLinks"]);
                        if (removeContent)
                        {
                            _data[$"{action.Id}_txtLinks"].Remove(content);
                        }
                        if (deleteContent)
                        {
                            var context = new ScriptActionContext();
                            settings.DeleteValue("txtLinks", content);
                            action.Json = settings.GetJsonString();
                            context.Update(action);
                        }
                    }
                }
                if (isHastag && hastags.Any())
                {
                    hastag = SubdyHelper.Shuffle(hastags, SubdyHelper.RandomValue(hastagFrom, hastagTo));
                }
                if (isLink)
                {
                    link = SubdyHelper.GetStringRandom(links);
                }
                if (isMedia && _data.ContainsKey($"{action.Id}_txtPathAnh") && _data[$"{action.Id}_txtPathAnh"].Any())
                {
                    medias = SubdyHelper.Shuffle(_data[$"{action.Id}_txtPathAnh"], SubdyHelper.RandomValue(mediaFrom, mediaTo));
                    if (deleteMedia)
                    {
                        foreach (var media in medias)
                        {
                            _data[$"{action.Id}_txtPathAnh"].Remove(media);
                        }
                    }
                }
                try
                {

                    int tickCount = Environment.TickCount;
                    int num6 = 300;
                    while (!_mainService._ct.IsCancellationRequested)
                    {
                        string xml = _client.GetXMLSource();
                        string xpath = _client.FindElement(xml, new List<string> {
                            "//*[@content-desc='POST'][@enabled='true']",
                            "//*[@content-desc='POST'][@enabled='false']"
                        }, 1);
                        string text4;
                        switch (xpath)
                        {
                            case "//*[@content-desc=\"Make a post on Facebook\"]":
                                _mainService.SetStatus("Đang mở khung đăng bài...", 2, logDetail: $"[Farming.DangBai] tap={xpath}");
                                _client.ElementWithAttributes(xpath, 5, xml);
                                break;
                            default:
                                _mainService.SetStatus($"({countPost}/{soLuongBaiViet}), Đang lướt tìm nút đăng bài...", 2);
                                if (ScrollScreen(-1))
                                {
                                    //  kiem tra dang nhap
                                    int num7 = Login();
                                    if (num7 == 1 || num7 == 0)
                                    {
                                        break;
                                    }
                                    return countPost;
                                }
                                break;
                            case "//*[@content-desc='POST'][@enabled='false']":
                                if (isDangAnDanh)
                                {
                                    if (_client.ElementWithAttributes("//*[@content-desc=\"Participate anonymously\"]"))
                                    {
                                        _client.ElementWithAttributes("//*[@content-desc=\"Got it\"]");
                                        _mainService.SetStatus($"({countPost}/{soLuongBaiViet}), Đăng ẩn danh...", 2);
                                    }
                                }

                                _client.ElementWithAttributes("//*[@class='android.widget.EditText']");
                                _client.Delay(3);
                                _client.ADBKeyboardService.ClearInputWithADBKeyboard();
                                if (!string.IsNullOrEmpty(content))
                                {
                                    _mainService.SetStatus($"({countPost}/{soLuongBaiViet}), Đang nhập nội dung bài viết...", 2);
                                    content = SubdyHelper.SpinText(content);
                                    _client.Delay(2);

                                    _client.ADBKeyboardService.Input(content, false);
                                    _client.Delay(2);
                                    _client.ADB.Shell("input keyevent 62");
                                    _client.Delay(2);
                                    if (isBackgroud)
                                    {
                                        _mainService.SetStatus($"({countPost}/{soLuongBaiViet}), Đang tìm màu nền...", 2);
                                        var list3 = _client.FindBounds("", "//*[contains(@content-desc,\", background\")]", 0);
                                        if (list3.Count > 2)
                                        {
                                            var list4 = _client.GetAttributeValuesFromXmlNodes("", "//*[contains(@content-desc,\", background\")]");
                                            list4.RemoveAt(list4.Count - 1);
                                            _mainService.SetStatus($"({countPost}/{soLuongBaiViet}), Đang chọn màu nền...", 2);
                                            var point = ParseCoordinate(list4[SubdyHelper.RandomValue(0, list4.Count)]);
                                            _client.Click(point.X, point.Y);
                                        }
                                    }
                                    if (isHastag && hastag.Any())
                                    {
                                        _mainService.SetStatus($"({countPost}/{soLuongBaiViet}), Đang nhập hashtag...", 2);
                                        foreach (var item in hastag)
                                        {
                                            string text = "";
                                            if (!item.StartsWith("#"))
                                            {
                                                text = "#";
                                            }
                                            text += item;
                                            bool check = false;
                                            foreach (char c in item)
                                            {
                                                _client.ADBKeyboardService.Input(c.ToString(), false);
                                                var nodes = _client.FindElementsNotToLower(0, "", "//node[contains(@class, 'android.widget.Button') and @content-desc and string-length(@content-desc) > 0 and " +
                               $"contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), '{text}') and " +
                               $"contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'posts') and " +
                               "@visible-to-user='true']");
                                                if (!nodes.Any())
                                                {
                                                    continue;
                                                }
                                                var info = _client.ExtractNodeInfo(nodes.FirstOrDefault().OuterXml);
                                                if (!info.ContainsKey("bounds")) continue;
                                                var point = new RectangleArea(info["bounds"]).RandomPoint();
                                                _client.Click(point.X, point.Y);
                                                _client.ADB.Shell("input keyevent 62");
                                                check = true;
                                                break;
                                            }
                                            if (!check)
                                            {
                                                for (int i = 0; i < text.Length; i++)
                                                {
                                                    _client.ATX.Press(PressKey.Delete);
                                                }
                                            }
                                        }
                                    }
                                }
                                if (isLink && !string.IsNullOrEmpty(link))
                                {
                                    _mainService.SetStatus($"({countPost}/{soLuongBaiViet}), Đang nhập liên kết...", 2);
                                    _client.ADBKeyboardService.Input(link, false);
                                    _client.ADB.Shell("input keyevent 62");
                                    if (_client.ElementWithAttributes("//*[contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'shared link')]", 10, click: false))
                                    {
                                        for (int i = 0; i < link.Length; i++)
                                        {
                                            _client.ATX.Press(PressKey.Delete);
                                        }
                                    }
                                }
                                if (isNeuBat)
                                {
                                    _mainService.SetStatus($"Đang gắn thẻ nổi bật...", 2);
                                    List<string> neubats = new List<string> { "@highlight", "@neu" };
                                    foreach (var item in neubats)
                                    {
                                        bool click = false;
                                        var count = item.Length;
                                        _client.ADB.Shell("input keyevent 62");
                                        foreach (char c in item)
                                        {
                                            _client.ADBKeyboardService.Input(c.ToString(), false);
                                            if (_client.ElementWithAttributes(new List<string> { "//*[@text=\"@highlight\"]", "//*[@text=\"@nêu bật\"]" }, 0))
                                            {
                                                click = true;
                                                break;
                                            }
                                        }
                                        if (!click)
                                        {
                                            for (int i = 0; i < count + 1; i++)
                                            {
                                                _client.Shell("input keyevent KEYCODE_DEL");
                                                Thread.Sleep(50);
                                            }
                                        }
                                        if (click)
                                        {
                                            break;
                                        }

                                    }
                                }
                                if (isFollower)
                                {
                                    _mainService.SetStatus($"Đang gắn thẻ người theo dõi...", 2);

                                    List<string> neubats = new List<string> { "@followers", "@nguoi" };
                                    foreach (var item in neubats)
                                    {
                                        bool click = false;
                                        var count = item.Length;
                                        _client.ADB.Shell("input keyevent 62");
                                        foreach (char c in item)
                                        {
                                            _client.ADBKeyboardService.Input(c.ToString(), false);
                                            if (_client.ElementWithAttributes(new List<string> { "//*[@text=\"@followers\"]", "//*[@text=\"@người theo dõi\"]" }, 0))
                                            {
                                                click = true;
                                                break;
                                            }
                                        }
                                        if (!click)
                                        {
                                            for (int i = 0; i < count + 1; i++)
                                            {
                                                _client.Shell("input keyevent KEYCODE_DEL");
                                                Thread.Sleep(50);
                                            }
                                        }
                                        if (click)
                                        {
                                            break;
                                        }

                                    }
                                }
                                if (isMedia && medias.Any())
                                {
                                    UploadMediaFiles(medias);
                                    _client.ElementWithAttributes("//*[@content-desc='Photo/video']", 5);
                                    _mainService.SetStatus($"({countPost}/{soLuongBaiViet}), Đang tải ảnh/video lên...", 2);
                                    bool flag8 = false;
                                    for (int j = 0; j < 5; j++)
                                    {
                                        xml = _client.GetXMLSource();
                                        xpath = _client.FindElement(xml, new List<string> { "//*[@text='ALLOW']", "//*[@content-desc='Allow access']", "//*[@content-desc='Choose layout']", "//*[contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'photo') or contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'video')]" }, 5);

                                        if (!(xpath == "//*[@content-desc='Choose layout']") && !(xpath == "//*[contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'photo') or contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'video')]"))
                                        {
                                            if (xpath != "")
                                            {
                                                _mainService.SetStatus($"({countPost}/{soLuongBaiViet}), Đang mở khung đăng bài...", 2, logDetail: $"[Farming.DangBai] tap={xpath}");
                                                _client.ElementWithAttributes(xpath);
                                            }
                                            else
                                            {
                                                ScrollScreen(-1);
                                            }
                                            _client.Delay(1);
                                            continue;
                                        }
                                        if (!_client.ElementWithAttributes("//*[contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'photo') or contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'video')]", 1, "", false))
                                        {
                                            break;
                                        }
                                        if (_client.ElementWithAttributes("//*[@content-desc='Select multiple']", 1, xml))
                                        {
                                            _client.Delay(2);
                                        }
                                        List<string> list4 = _client.GetAttributeValuesFromXmlNodes("", "//*[contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'photo') or contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'video')]/parent::*[@selected='false']");
                                        for (int k = 0; k < medias.Count; k++)
                                        {
                                            for (int l = 0; l < medias.Count; l++)
                                            {
                                                while (list4.Count == 0)
                                                {
                                                    _mainService.SetStatus($"({countPost}/{soLuongBaiViet}), Đang lướt tìm nút đăng bài...", 2);
                                                    if (ScrollScreen())
                                                    {
                                                        break;
                                                    }
                                                    list4 = _client.GetAttributeValuesFromXmlNodes("", "//*[contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'photo') or contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'video')]/parent::*[@selected='false']");
                                                }
                                                if (list4.Count == 0)
                                                {
                                                    break;
                                                }
                                                string text5 = list4.OrderBy((string F911C492) => Guid.NewGuid()).FirstOrDefault();
                                                list4.Remove(text5);
                                                var point = new RectangleArea(text5).GetCenterPoint();
                                                _client.Click(point.X, point.Y);
                                            }
                                            if (list4.Count == 0 || _client.GetAttributeValuesFromXmlNodes("", "//*[contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'photo') or contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'video')]/parent::*[@selected='true']").Count >= 5)
                                            {
                                                break;
                                            }
                                        }
                                        flag8 = _client.ElementWithAttributes("//*[@content-desc=\"Next\"]");
                                        _client.Delay(10);
                                        break;
                                    }
                                    if (!flag8)
                                    {
                                        _client.Shell("input keyevent 4");
                                    }
                                }
                                break;
                            case "//*[@content-desc='POST'][@enabled='true']":
                                {
                                    _mainService.SetStatus($"({countPost}/{soLuongBaiViet}), Đang mở khung đăng bài...", 2, logDetail: $"[Farming.DangBai] tap={xpath}");
                                    _client.ElementWithAttributes(xpath);
                                    int tickCount1 = Environment.TickCount;
                                    await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(3, 6), $"({countPost}/{soLuongBaiViet}), Tap Post, " + "đợi" + " {time}s...", 2);
                                    while (!_mainService._ct.IsCancellationRequested)
                                    {
                                        if (ContainsAnyKeyword("", "android.widget.ProgressBar", "Row showing that your post is", "Sharing", "Uploading", "Finishing up", "Updating", "Posting"))
                                        {
                                            break;
                                        }
                                        _client.ElementWithAttributes(new List<string> { "//*[@content-desc=\"OK\"]", "//*[@content-desc=\"Share now\"]" });
                                        if (Environment.TickCount - tickCount1 < 30 * 1000)
                                        {
                                            continue;
                                        }
                                        break;
                                    }
                                    _mainService.SetStatus($"({countPost}/{soLuongBaiViet})" + "đợi" + " post success...", 2);
                                    if (WaitForPostComplete(300))
                                    {
                                        if (isComment)
                                        {
                                            _mainService.SetStatus($"({countPost}/{soLuongBaiViet}), " + " Bình luận ...", 2);
                                            tickCount1 = Environment.TickCount;
                                            while (!_mainService._ct.IsCancellationRequested)
                                            {
                                                if (!_client.ElementWithAttributes("//*[contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'just now')]", 5, "", true))
                                                {
                                                    ScrollScreen(-1);
                                                }
                                                else
                                                {
                                                    string imageComment = "";
                                                    string comment = string.Empty;
                                                    if (_data.ContainsKey($"{action.Id}_txtComments"))
                                                    {
                                                        lock (Globals.Lock)
                                                        {
                                                            var contents = _data[$"{action.Id}_txtComments"];
                                                            if (contents.Any())
                                                            {
                                                                comment = SubdyHelper.GetStringRandom(contents);
                                                                if (!settings.GetBooleanValue("checkBox5"))
                                                                {
                                                                    contents.Remove(content);
                                                                    _data[$"{action.Id}_txtComments"] = contents;
                                                                }
                                                                if (settings.GetBooleanValue("checkBox4"))
                                                                {
                                                                    var context = new ScriptActionContext();
                                                                    settings.DeleteValue("txtComments", content);
                                                                    action.Json = settings.GetJsonString();
                                                                    context.Update(action);
                                                                }
                                                            }

                                                        }
                                                        comment = SubdyHelper.SpinText(comment);
                                                    }
                                                    lock (Globals.Lock)
                                                    {
                                                        var images = _data[$"{action.Id}_txtPathImageComment"];
                                                        imageComment = SubdyHelper.GetStringRandom(images);
                                                        if (isDeleteMediaComment)
                                                        {
                                                            _data[$"{action.Id}_txtPathImageComment"].Remove(imageComment);
                                                        }
                                                    }
                                                    if (!string.IsNullOrEmpty(comment) || File.Exists(imageComment))
                                                    {
                                                        List<string> reactionTypes = new List<string> { "Like", "Love", "Care", "Haha" };
                                                        TapReaction(SubdyHelper.GetStringRandom(reactionTypes));
                                                        string message = await CommentAction(comment, imageComment, isNeuBatComment, isFollowerComment);
                                                        await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(3, 7), $"{message}, đợi {{time}}s...", 2);
                                                        if (isDeleteMediaComment)
                                                        {
                                                            File.Delete(imageComment);
                                                        }
                                                        break;
                                                    }
                                                    break;
                                                }
                                                if (Environment.TickCount - tickCount1 < 30 * 1000)
                                                {
                                                    continue;
                                                }
                                                break;
                                            }
                                        }

                                        await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(delayFrom, delayTo), $"({countPost}/{soLuongBaiViet}), " + "đợi" + " {time}s...", 2);
                                        countPost++;
                                    }
                                    break;
                                }
                        }
                        Thread.Sleep(1000);
                        if (Environment.TickCount - tickCount < num6 * 1000)
                        {
                            continue;
                        }
                        break;
                    }
                }
                finally
                {

                }
            }

            return countPost;
        }
        public async Task<int> HDDangReel(JsonHelper settings, ScriptAction action)
        {
            //method_54
            int soLuongBaiViet = SubdyHelper.RandomValue(settings.GetIntType("C913DC8A"), settings.GetIntType("F391713F") + 1);
            int delayFrom = settings.GetIntType("nudKhoangCachFrom");
            int delayTo = settings.GetIntType("nudKhoangCachTo");

            bool isText = settings.GetBooleanValue("A2009002");
            bool deleteContent = settings.GetBooleanValue("ckbXoaNguyenLieuDaDung");
            bool isHastag = settings.GetBooleanValue("checkBox2");
            List<string> hastags = settings.GetValuesList("textBox1");
            int hastagFrom = settings.GetIntType("numericUpDown2");
            int hastagTo = settings.GetIntType("numericUpDown1");
            bool removeContent = settings.GetBooleanValue("checkBox1");
            bool isLink = settings.GetBooleanValue("checkBox3");
            List<string> links = settings.GetValuesList("textBox2");


            bool deleteMedia = settings.GetBooleanValue("checkBox4");

            bool isComment = settings.GetBooleanValue("checkBox6");
            int commentFrom = settings.GetIntType("nudCommentFrom");
            int commentTo = settings.GetIntType("nudCommentTo");
            bool isNeuBatComment = settings.GetBooleanValue("ckbTagNeuBat");
            bool isFollowerComment = settings.GetBooleanValue("checkBox12");
            bool isCommentMedia = settings.GetBooleanValue("checkBox11");
            bool isDeleteMediaComment = settings.GetBooleanValue("checkBox10");
            bool isDeleteComent = settings.GetBooleanValue("checkBox9");
            bool isTrungLapComent = settings.GetBooleanValue("checkBox8");

            int countPost = 1;
            int refail = 0;

            while (!_mainService._ct.IsCancellationRequested)
            {
                if (countPost > soLuongBaiViet) break;

                string medias = "";
                string content = string.Empty;
                string link = string.Empty;
                List<string> hastag = new List<string>();
                if (isText && _data.ContainsKey($"{action.Id}_txtLinks") && _data[$"{action.Id}_txtLinks"].Any())
                {
                    lock (Globals.Lock)
                    {
                        content = SubdyHelper.GetStringRandom(_data[$"{action.Id}_txtLinks"]);
                        if (removeContent)
                        {
                            _data[$"{action.Id}_txtLinks"].Remove(content);
                        }
                        if (deleteContent)
                        {
                            var context = new ScriptActionContext();
                            settings.DeleteValue("txtLinks", content);
                            action.Json = settings.GetJsonString();
                            context.Update(action);
                        }
                    }
                }
                if (isHastag && hastags.Any())
                {
                    hastag = SubdyHelper.Shuffle(hastags, SubdyHelper.RandomValue(hastagFrom, hastagTo));
                }
                if (isLink)
                {
                    link = SubdyHelper.GetStringRandom(links);
                }
                lock (Globals.Lock)
                {
                    if (_data.ContainsKey($"{action.Id}_txtPathAnh") && _data[$"{action.Id}_txtPathAnh"].Any())
                    {

                        medias = SubdyHelper.GetStringRandom(_data[$"{action.Id}_txtPathAnh"]);
                        if (deleteMedia)
                        {
                            _data[$"{action.Id}_txtPathAnh"].Remove(medias);
                        }
                    }
                }
                if (!File.Exists(medias)) continue;
                try
                {
                    _mainService.SetStatus($"({countPost}/{soLuongBaiViet}), Đang tải ảnh/video lên...", 2);
                    UploadMediaFiles(new List<string> { medias });
                    DeplinkFacebook("fb://profile");
                    int tickCount = Environment.TickCount;
                    int num6 = 300;
                    while (!_mainService._ct.IsCancellationRequested)
                    {
                        string xml = _client.GetXMLSource();
                        string xpath = _client.FindElement(xml, new List<string> {
                            "//*[@content-desc=\"Reel\"]",
                            "//*[@content-desc=\"Create reel\"]",
                            "//*[@content-desc=\"Share now\"]"
                        }, 1);
                        string text4;
                        switch (xpath)
                        {
                            case "//*[@content-desc=\"Reel\"]":
                                _mainService.SetStatus("Đang mở khung đăng bài...", 2, logDetail: $"[Farming.DangBai] tap={xpath}");
                                _client.ElementWithAttributes(xpath, 5, xml);
                                break;
                            default:
                                _mainService.SetStatus($"({countPost}/{soLuongBaiViet}), Đang lướt tìm nút đăng bài...", 2);
                                if (ScrollScreen(1, 1))
                                {
                                    //  kiem tra dang nhap
                                    int num7 = Login();
                                    if (num7 == 1 || num7 == 0)
                                    {
                                        break;
                                    }
                                    return countPost;
                                }
                                break;
                            case "//*[@content-desc=\"Share now\"]":
                                {
                                    _client.ElementWithAttributes("//*[@class='android.widget.EditText']");
                                    _client.Delay(3);
                                    _client.ADBKeyboardService.ClearInputWithADBKeyboard();
                                    if (!string.IsNullOrEmpty(content))
                                    {
                                        _mainService.SetStatus($"({countPost}/{soLuongBaiViet}), Đang nhập nội dung bài viết...", 2);
                                        content = SubdyHelper.SpinText(content);
                                        _client.Delay(2);

                                        _client.ADBKeyboardService.Input(content, false);
                                        _client.Delay(2);
                                        _client.ADB.Shell("input keyevent 62");
                                        _client.Delay(2);
                                        if (isHastag && hastag.Any())
                                        {
                                            _mainService.SetStatus($"({countPost}/{soLuongBaiViet}), Đang nhập hashtag...", 2);
                                            foreach (var item in hastag)
                                            {
                                                string text = "";
                                                if (!item.StartsWith("#"))
                                                {
                                                    text = "#";
                                                }
                                                text += item;
                                                bool check = false;
                                                foreach (char c in item)
                                                {
                                                    _client.ADBKeyboardService.Input(c.ToString(), false);
                                                    var nodes = _client.FindElementsNotToLower(0, "", "//node[contains(@class, 'android.widget.Button') and @content-desc and string-length(@content-desc) > 0 and " +
                                   $"contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), '{text}') and " +
                                   $"contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'posts') and " +
                                   "@visible-to-user='true']");
                                                    if (!nodes.Any())
                                                    {
                                                        continue;
                                                    }
                                                    var info = _client.ExtractNodeInfo(nodes.FirstOrDefault().OuterXml);
                                                    if (!info.ContainsKey("bounds")) continue;
                                                    var point = new RectangleArea(info["bounds"]).RandomPoint();
                                                    _client.Click(point.X, point.Y);
                                                    _client.ADB.Shell("input keyevent 62");
                                                    check = true;
                                                    break;
                                                }
                                                if (!check)
                                                {
                                                    for (int i = 0; i < text.Length; i++)
                                                    {
                                                        _client.ATX.Press(PressKey.Delete);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    if (_client.ElementWithAttributes("//android.view.View[@text=\"Friends\"]"))
                                    {
                                        _client.ElementWithAttributes("//*[@content-desc=\"Public\"]", 10);
                                        _client.ElementWithAttributes("//*[@content-desc=\"Done\"]", 10);
                                    }
                                    _client.ElementWithAttributes("//*[@content-desc='Share now'][@enabled='true']");
                                    int tickCount1 = Environment.TickCount;
                                    await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(3, 6), $"({countPost}/{soLuongBaiViet}), Tap Post, " + "đợi" + " {time}s...", 2);
                                    while (!_mainService._ct.IsCancellationRequested)
                                    {
                                        if (ContainsAnyKeyword("", "android.widget.ProgressBar", "Row showing that your post is", "Sharing", "Uploading", "Finishing up", "Updating", "Posting"))
                                        {
                                            break;
                                        }
                                        if (Environment.TickCount - tickCount1 < 30 * 1000)
                                        {
                                            continue;
                                        }
                                        break;
                                    }
                                    _mainService.SetStatus($"({countPost}/{soLuongBaiViet})" + "đợi" + " reel success...", 2);
                                    if (WaitForPostComplete(300))
                                    {
                                        if (isComment)
                                        {
                                            _mainService.SetStatus($"({countPost}/{soLuongBaiViet}), " + " Bình luận ...", 2);
                                            tickCount1 = Environment.TickCount;
                                            while (!_mainService._ct.IsCancellationRequested)
                                            {
                                                if (!_client.ElementWithAttributes("//*[@content-desc=\"Share\"]", 5, "", false))
                                                {
                                                    ScrollScreen(-1);
                                                }
                                                else
                                                {
                                                    string imageComment = "";
                                                    string comment = string.Empty;
                                                    if (_data.ContainsKey($"{action.Id}_txtComments"))
                                                    {
                                                        lock (Globals.Lock)
                                                        {
                                                            var contents = _data[$"{action.Id}_txtComments"];
                                                            if (contents.Any())
                                                            {
                                                                comment = SubdyHelper.GetStringRandom(contents);
                                                                if (!settings.GetBooleanValue("checkBox5"))
                                                                {
                                                                    contents.Remove(content);
                                                                    _data[$"{action.Id}_txtComments"] = contents;
                                                                }
                                                                if (settings.GetBooleanValue("checkBox4"))
                                                                {
                                                                    var context = new ScriptActionContext();
                                                                    settings.DeleteValue("txtComments", content);
                                                                    action.Json = settings.GetJsonString();
                                                                    context.Update(action);
                                                                }
                                                            }

                                                        }
                                                        comment = SubdyHelper.SpinText(comment);
                                                    }
                                                    lock (Globals.Lock)
                                                    {
                                                        var images = _data[$"{action.Id}_txtPathImageComment"];
                                                        imageComment = SubdyHelper.GetStringRandom(images);
                                                        if (isDeleteMediaComment)
                                                        {
                                                            _data[$"{action.Id}_txtPathImageComment"].Remove(imageComment);
                                                        }
                                                    }
                                                    if (!string.IsNullOrEmpty(comment) || File.Exists(imageComment))
                                                    {
                                                        List<string> reactionTypes = new List<string> { "Like", "Love", "Care", "Haha" };
                                                        TapReaction(SubdyHelper.GetStringRandom(reactionTypes));
                                                        string message = await CommentAction(comment, imageComment, isNeuBatComment, isFollowerComment);
                                                        await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(3, 7), $"{message}, đợi {{time}}s...", 2);
                                                        if (isDeleteMediaComment)
                                                        {
                                                            File.Delete(imageComment);
                                                        }
                                                        break;
                                                    }
                                                    break;
                                                }
                                                if (Environment.TickCount - tickCount1 < 30 * 1000)
                                                {
                                                    continue;
                                                }
                                                break;
                                            }
                                        }

                                        await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(delayFrom, delayTo), $"({countPost}/{soLuongBaiViet}), " + "đợi" + " {time}s...", 2);
                                        countPost++;
                                    }
                                    break;
                                }
                            case "//*[@content-desc=\"Create reel\"]":
                                if (!_client.ElementWithAttributes("//*[contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'video')]", 1, "", true))
                                {
                                    break;
                                }
                                _client.ElementWithAttributes("//*[@content-desc=\"Next\"]");
                                _client.Delay(10);
                                break;
                        }
                        Thread.Sleep(1000);
                        if (Environment.TickCount - tickCount < num6 * 1000)
                        {
                            continue;
                        }
                        break;
                    }
                }
                finally
                {

                }
            }

            return countPost;
        }
        public int HDKetBanGoiY(ref int successCount, int accountId, string statusPrefix, JsonHelper jsonHelper, string actionName)
        {
            //method_65
            int minValue = jsonHelper.GetIntType("nudSoLuongFrom");
            int num = jsonHelper.GetIntType("nudSoLuongTo");
            int minValue2 = jsonHelper.GetIntType("nudDelayFrom");
            int num2 = jsonHelper.GetIntType("nudDelayTo");
            int num3 = 0;
            try
            {
                int num4 = SubdyHelper.RandomValue(minValue, num + 1);
                if (num4 != 0)
                {
                    string text = statusPrefix + "Đang" + " " + actionName + ": ";
                    while (true)
                    {
                    IL_048e:
                        SetStatusAccount(accountId, text + "Goto Friend Suggest...");
                        if (!OpenFacebookLink(accountId, text, "fb://requests"))
                        {
                            break;
                        }
                        string text2 = "";
                        int num5 = 0;
                        int num6 = 3;
                        int tickCount = Environment.TickCount;
                        do
                        {
                            text2 = _client.GetXMLSource();
                            string text3 = _client.FindElement(text2, new List<string>
                    {
                        "//android.widget.ProgressBar", "//*[@text='Tap to retry']", "//*[@content-desc='Suggestions']", "//*[contains(@content-desc,'as a friend') or contains(@text,'as a friend')]", "//*[@text='No Suggestions Available']", "//*[@content-desc='Search for a friend']", "//*[@content-desc='Upload contacts']", "//*[@content-desc='ok' or @text='ok']", "//*[@content-desc='CONFIRM' or @text='CONFIRM']", "//*[@content-desc='Cancel' or @text='Cancel']",
                        "//*[@content-desc='Requests']", "//*[contains(@content-desc,'Profile picture')]"
                    }, 1);
                            string text4 = text3;
                            string text5 = text4;

                            // Bắt đầu thay toàn bộ kiểm tra num7 thành kiểm tra text5
                            if (text5 == "//*[@content-desc='Requests']")
                            {
                                goto IL_04e9;
                            }
                            else if (text5 == "//*[@content-desc='Cancel' or @text='Cancel']")
                            {
                                goto IL_019d;
                            }
                            else if (text5 == "//*[@content-desc='Suggestions']")
                            {
                                goto IL_019d;
                            }
                            else if (text5 == "//*[@content-desc='Upload contacts']")
                            {
                                goto IL_04e9;
                            }
                            else if (text5 == "//*[@content-desc='ok' or @text='ok']")
                            {
                                goto IL_019d;
                            }
                            else if (text5 == "//*[@content-desc='CONFIRM' or @text='CONFIRM']")
                            {
                                goto IL_019d;
                            }
                            else if (text5 == "//*[@content-desc='Search for a friend']")
                            {
                                goto IL_04e9;
                            }
                            else if (text5 == "//*[@text='No Suggestions Available']")
                            {
                                goto IL_04e9;
                            }
                            else if (text5 == "//*[contains(@content-desc,'as a friend') or contains(@text,'as a friend')]")
                            {
                                successCount = 1;
                                List<string> list = _client.FindBounds(text2, text3, 1);
                                string dB31F = SubdyHelper.GetStringRandom(list);
                                Point point_ = new RectangleArea(dB31F).GetCenterPoint();
                                SetStatusAccount(accountId, text + $"({num3}/{num4}), Tap Add Friend...");
                                if (_client.Click(point_.X, point_.Y))
                                {
                                    num3++;
                                    if (num3 >= num4)
                                    {
                                        SetStatusAccount(accountId, text + $"({num3}/{num4}): Done!");
                                        break;
                                    }
                                    SetStatusAccount(accountId, text + $"({num3}/{num4}), " + "đợi" + " {time}s...", SubdyHelper.RandomValue(minValue2, num2 + 1));
                                    tickCount = Environment.TickCount;
                                }
                                goto IL_01cf;
                            }
                            else if (text5 == "//android.widget.ProgressBar")
                            {
                                SetStatusAccount(accountId, text + "Loading...");
                                goto IL_01cf;
                            }
                            else if (text5 == "//*[@text='Tap to retry']")
                            {
                                if (num5 >= num6)
                                {
                                    break;
                                }
                                num5++;
                                ScrollScreen(-1);
                                goto IL_01cf;
                            }
                            else if (text5 == "//*[contains(@content-desc,'Profile picture')]")
                            {
                                _client.Shell("input keyevent 4");
                                goto IL_01cf;
                            }
                            // Nếu không khớp bất kỳ trường hợp nào
                            SetStatusAccount(accountId, text + "Scroll...");
                            if (ScrollScreen(-1))
                            {
                                int num8 = Login();
                                if (num8 == 1)
                                {
                                    goto IL_048e;
                                }
                                if (num8 != 0)
                                {
                                    break;
                                }
                            }
                            goto IL_01cf;

                        IL_04e9:
                            successCount = 2;
                            break;
                        IL_019d:
                            SetStatusAccount(accountId, text + "Tap " + text3 + "...");
                            _client.ElementWithAttributes(text3, 1, text2);
                            goto IL_01cf;
                        IL_01cf:
                            _client.Delay(2);
                        }
                        while (Environment.TickCount - tickCount < 60000);
                        break;
                    }
                }
            }
            catch
            {
                num3 = -1;
            }
            return num3;
        }
        public int HDSpamBanBe(int accountId, string statusPrefix, JsonHelper jsonHelper, string actionName, string dataKey)
        {
            int minValue = jsonHelper.GetIntType("nudSoLuongUidFrom");
            int num = jsonHelper.GetIntType("nudSoLuongUidTo");
            int num2 = jsonHelper.GetIntType("nudSoLuongBaiVietFrom");
            int num3 = jsonHelper.GetIntType("nudSoLuongBaiVietTo");
            int f988D70A = jsonHelper.GetIntType("nudDelayFrom");
            int e234B = jsonHelper.GetIntType("nudDelayTo");
            bool bool_ = jsonHelper.GetBooleanValue("ckbInteract");
            string c23CDF0B = jsonHelper.GetValue("typeReaction");
            bool bool_2 = jsonHelper.GetBooleanValue("ckbComment");
            List<string> f1808BA = jsonHelper.GetValuesList("txtComment", jsonHelper.GetIntType("typeNganCach"));
            bool bool_3 = jsonHelper.GetBooleanValue("ckbAnh");
            string string_ = jsonHelper.GetValue("txtPathAnh");
            f1808BA = f1808BA.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            try
            {
                string text = statusPrefix + "Đang" + " " + actionName + ": ";
                int num4 = SubdyHelper.RandomValue(minValue, num + 1);
                int num5 = 0;
                while (num5 < num4)
                {
                    while (true)
                    {
                        SetStatusAccount(accountId, text + $"({num5 + 1}/{num4}), Go to Friend...");
                        if (!OpenFacebookLink(accountId, text, "fb://friends"))
                        {
                            break;
                        }
                        SetStatusAccount(accountId, text + $"({num5 + 1}/{num4}), Find Friend...");
                        List<string> list = _client.FindBounds("", "//android.view.View[contains(@content-desc,\", profile picture\") or contains(@text,\", profile picture\")]", 3);
                        SetStatusAccount(accountId, text + $"({num5 + 1}/{num4}), Find Friend: " + list.Count);
                        if (list.Count == 0)
                        {
                            break;
                        }
                        SetStatusAccount(accountId, text + $"({num5 + 1}/{num4}), Scroll...");
                        int num6 = 0;
                        if (list.Count >= 6)
                        {
                            while (!ScrollScreen())
                            {
                                num6++;
                                if (num6 >= 5)
                                {
                                    break;
                                }
                            }
                            int num7 = SubdyHelper.RandomValue(0, ((num6 < 1) ? 1 : num6) - 1);
                            for (int i = 0; i < num7; i++)
                            {
                                if (ScrollScreen(-1))
                                {
                                    break;
                                }
                            }
                            SetStatusAccount(accountId, text + $"({num5 + 1}/{num4}), Find Friend...");
                            list = _client.FindBounds("", "//android.view.View[contains(@content-desc,\", profile picture\") or contains(@text,\", profile picture\")]", 3);
                        }
                        SetStatusAccount(accountId, text + $"({num5 + 1}/{num4}), Tap Friend...");
                        var point = new RectangleArea(SubdyHelper.GetStringRandom(list)).GetCenterPoint();
                        _client.Click(point.X, point.Y);
                        _client.Delay(3);
                        switch (Login())
                        {
                            case 1:
                                break;
                            case 0:
                                goto IL_031d;
                            default:
                                goto end_IL_00cd;
                        }
                        continue;
                    IL_031d:
                        ScrollFeedAndInteract(accountId, text + $"({num5 + 1}/{num4}), ", num2, num3, bool_, c23CDF0B, num2, num3, bool_2, num2, num3, f1808BA, enableShare: false, 0, 0, 1, bool_3, string_);
                        SetStatusAccount(accountId, text + $"({num5 + 1}/{num4}), delay {{time}}s...", SubdyHelper.RandomValue(f988D70A, e234B));
                        num5++;
                        goto IL_0398;
                    }
                    break;
                IL_0398:;
                }
            end_IL_00cd:;
            }
            catch
            {
            }
            return 0;
        }
        public int HDBuffFollowUID(int accountId, string statusPrefix, JsonHelper jsonHelper, string actionName)
        {
            JsonHelper f72FAFBC = new JsonHelper();
            f72FAFBC.AddValue("id", (object)jsonHelper.GetValue("txtUid"));
            string f5036F8D = statusPrefix + "Đang" + " " + actionName + ": ";
            return FollowProfileAndReport(accountId, f5036F8D, f72FAFBC).isSuccess ? 1 : 0;
        }
        public int HDXemStory(int accountId, string statusPrefix, JsonHelper settings, string actionName)
        {
            int minStoryCount = settings.GetIntType("nudSoLuongFrom");
            int maxStoryCount = settings.GetIntType("nudSoLuongTo");
            bool shouldInteract = settings.GetBooleanValue("ckbInteract");
            string reactionType = settings.GetValue("typeReaction");
            bool shouldComment = settings.GetBooleanValue("ckbComment");
            List<string> comments = settings.GetValuesList("txtComment");
            string commentText = "";
            int watchTime = SubdyHelper.RandomValue(minStoryCount, maxStoryCount);
            try
            {
                string status = statusPrefix + "Đang" + " " + actionName + ": ";
                while (true)
                {
                    // [TIME-LIMIT v21] Ép giới hạn thời gian NGAY TRONG hành động xem story (mỗi vòng tìm story):
                    // quá timeoutTaiKhoan/timeoutKichBan -> thoát tới end_Main -> return -> ExecuteAsync đổi tài khoản. KHÔNG đụng logic FB.
                    if (Stop()) goto end_Main;
                IL_Restart:
                    SetStatusAccount(accountId, status + "Đang về trang chủ...");
                    OpenFacebookTimeline();
                    _client.Delay(3);
                    switch (Login())
                    {
                        case 0:
                            {
                                string xml = "";
                                SetStatusAccount(accountId, status + "Find Story...");
                                int startTick = Environment.TickCount;
                                int timeoutSeconds = 10;
                                do
                                {
                                    xml = _client.GetXMLSource();
                                    string storyXPath = _client.FindElement(xml, new List<string>
                            {
                                "//*[@content-desc='Create a reel']/parent::*/parent::*/parent::*/parent::*/parent::*/child::*[1]/child::*[1]",
                                "//*[contains(@content-desc,\"'s story\")]"
                            }, 1);
                                    if (storyXPath == "//*[contains(@content-desc,\"'s story\")]")
                                    {
                                        _client.ElementWithAttributes(storyXPath, 1, xml);
                                        int storyStartTick = Environment.TickCount;
                                        while (Environment.TickCount - storyStartTick < watchTime * 1000)
                                        {
                                            switch (Login())
                                            {
                                                case 0:
                                                    {
                                                        SetStatusAccount(accountId, status + "Xem Story, " + "đợi" + " {time}s...", SubdyHelper.RandomValue(4, 8));
                                                        xml = _client.GetXMLSource();
                                                        string seeVideoXPath = _client.FindBounds(xml, "//android.view.ViewGroup[@content-desc=\" See Video \"]", 1).FirstOrDefault();
                                                        if (!string.IsNullOrEmpty(seeVideoXPath))
                                                        {
                                                            SetStatusAccount(accountId, status + "Tap See Video...");
                                                            var point = new RectangleArea(seeVideoXPath).GetCenterPoint();
                                                            _client.Click(point.X, point.Y);
                                                        }
                                                        if (shouldComment)
                                                        {
                                                            SetStatusAccount(accountId, status + "Find Reply to...");
                                                            string replyXPath = _client.FindBounds(xml, new List<string>
                                                    {
                                                        "//*[contains(@content-desc,\"Reply to\")]",
                                                        "//*[contains(@content-desc,'comment on the story')]"
                                                    }, 1).LastOrDefault();
                                                            if (!string.IsNullOrEmpty(replyXPath))
                                                            {
                                                                commentText = SubdyHelper.GetStringRandom(comments);
                                                                commentText = SubdyHelper.SpinText(commentText);
                                                                SetStatusAccount(accountId, status + "Tap Reply to...");
                                                                var point = new RectangleArea(replyXPath).GetCenterPoint();
                                                                _client.Click(point.X, point.Y);
                                                                _client.Delay(1);
                                                                if (_client.ElementWithAttributes("//android.widget.EditText", 3, "", false))
                                                                {
                                                                    SetStatusAccount(accountId, status + "Nhập dữ liệu...");
                                                                    _client.SendTextSlow("//android.widget.EditText", commentText);
                                                                    SetStatusAccount(accountId, status + "Tap Send...");
                                                                    _client.ElementWithAttributes("//*[@content-desc=\"SEND\"]");
                                                                    _client.Delay(1);
                                                                    if (_client.ElementWithAttributes("//android.widget.EditText", 1, "", false))
                                                                    {
                                                                        _client.Shell("input keyevent 4");
                                                                        _client.Delay(2);
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    _client.Shell("input keyevent 4");
                                                                    _client.Delay(2);
                                                                }
                                                            }
                                                        }
                                                        SetStatusAccount(accountId, status + "Find Like...");
                                                        string likeXPath = _client.FindBounds("", "//*[contains(@content-desc, \"Like\")]", 1).FirstOrDefault();
                                                        if (!string.IsNullOrEmpty(likeXPath))
                                                        {
                                                            Point point = new RectangleArea(likeXPath).GetCenterPoint();
                                                            // Extract coordinates for the second point
                                                            string[] coords = likeXPath.Split(new string[] { "[", ",", "]" }, StringSplitOptions.RemoveEmptyEntries);
                                                            Point point2 = new RectangleArea($"[35,{coords[1]}][65,{coords[3]}]").GetCenterPoint();
                                                            if (shouldInteract)
                                                            {
                                                                SetStatusAccount(accountId, status + "Tap Reaction...");
                                                                _client.ATXSwipe(point.X, point.Y, point2.X, point2.Y);
                                                                _client.Delay(1);
                                                                if (!string.IsNullOrEmpty(reactionType))
                                                                {
                                                                    char rc = reactionType[SubdyHelper.RandomValue(0, reactionType.Length - 1)];
                                                                    int rv;
                                                                    if (int.TryParse(rc.ToString(), out rv))
                                                                        ReactToPost((rv + 1).ToString());
                                                                    else
                                                                        ReactToPost();
                                                                }
                                                                else ReactToPost();
                                                                _client.Delay(1);
                                                                _client.ATXSwipe(point2.X, point2.Y, point.X, point.Y);
                                                                _client.Delay(1);
                                                            }
                                                            _client.ATXSwipe(point.X, point.Y / 2, point2.X, point2.Y / 2);
                                                        }
                                                        continue;
                                                    }
                                                case 1:
                                                    break;
                                                default:
                                                    goto end_Main;
                                            }
                                            goto IL_Restart;
                                        }
                                        break;
                                    }
                                    if (storyXPath == "//*[@content-desc='Create a reel']/parent::*/parent::*/parent::*/parent::*/parent::*/child::*[1]/child::*[1]")
                                    {
                                        _client.ElementWithAttributes(storyXPath, 1, xml);
                                        continue;
                                    }
                                }
                                while (Environment.TickCount - startTick < timeoutSeconds * 1000);
                                goto end_Main;
                            }
                        case 1:
                            break;
                        default:
                            goto end_Main;
                    }
                }
            end_Main:;
            }
            catch
            {
                // You may log the exception here if needed.
            }
            return watchTime;
        }
        public async Task<int> HDXemWatch(JsonHelper settings, ScriptAction action)
        {
            int totalSeconds = SubdyHelper.RandomValue(settings.GetIntType("numericUpDown2"), settings.GetIntType("numericUpDown1"));

            int delayFrom = settings.GetIntType("nudTimeFrom");
            int delayTo = settings.GetIntType("nudTimeTo");


            bool shouldInteract = settings.GetBooleanValue("ckbInteract");
            int interactCount = SubdyHelper.RandomValue(settings.GetIntType("nudInteractFrom", 1), settings.GetIntType("nudInteractTo", 1));
            List<string> reactionTypes = new List<string> { "Like", "Love", "Care", "Haha", "Wow", "Sad", "Angry" };
            if (!settings.GetBooleanValue("ckbLike"))
            {
                reactionTypes.Remove("Like");
            }
            if (!settings.GetBooleanValue("ckbTym"))
            {
                reactionTypes.Remove("Love");
            }
            if (!settings.GetBooleanValue("ckbThuong"))
            {
                reactionTypes.Remove("Care");
            }
            if (!settings.GetBooleanValue("ckbHaha"))
            {
                reactionTypes.Remove("Haha");
            }
            if (!settings.GetBooleanValue("ckbWow"))
            {
                reactionTypes.Remove("Wow");
            }
            if (!settings.GetBooleanValue("ckbBuon"))
            {
                reactionTypes.Remove("Sad");
            }
            if (!settings.GetBooleanValue("ckbGian"))
            {
                reactionTypes.Remove("Angry");
            }
            List<string> reactions = new List<string>();
            if (reactionTypes.Any())
            {
                for (int i = 0; i < interactCount; i++)
                {
                    reactions.Add(SubdyHelper.GetStringRandom(reactionTypes));
                }
            }

            bool shouldShareWall = settings.GetBooleanValue("ckbShareWall");
            int shareCount = SubdyHelper.RandomValue(settings.GetIntType("nudShareWallFrom", 1), settings.GetIntType("nudShareWallTo", 1));



            bool shouldComment = settings.GetBooleanValue("ckbComment");
            int commentCount = SubdyHelper.RandomValue(settings.GetIntType("nudCommentFrom", 1), settings.GetIntType("nudCommentTo", 1));



            string type = "DeXuat";
            if (settings.GetBooleanValue("ckbChiDinh"))
            {
                type = "ChiDinh";
            }
            else if (settings.GetBooleanValue("ckbTuKhoa"))
            {
                type = "TuKhoa";
            }
            switch (type)
            {
                case "DeXuat":
                    {
                        if (!DeplinkFacebook("fb://watch").Contains("dat=fb://watch"))
                        {
                            throw new SubdyExtension(SubdyEnum.Error, "Mở video thất bại");
                        }
                        break;
                    }
                case "ChiDinh":
                    {
                        string url = SubdyHelper.GetStringRandom(settings.GetValuesList("txtLinks"));
                        if (string.IsNullOrEmpty(url))
                        {
                            throw new SubdyExtension(SubdyEnum.Error, "Không có link chỉ định");
                        }
                        if (!DeplinkFacebook(url).Contains(".IntentUriHandler"))
                        {
                            throw new SubdyExtension(SubdyEnum.Error, "Mở video thất bại");
                        }
                        break;
                    }
                case "TuKhoa":
                    {
                        if (!DeplinkFacebook("fb://watch").Contains("dat=fb://watch"))
                        {
                            throw new SubdyExtension(SubdyEnum.Error, "Mở video thất bại");
                        }
                        string search = SubdyHelper.GetStringRandom(settings.GetValuesList("txtLinks"));
                        _mainService.SetStatus($"Tìm kiếm {search}...", 2);
                        _client.Delay(3);
                        _client.ElementWithAttributes("//*[@content-desc=\"Search\"]");

                        _client.Delay(2);
                        _client.ElementWithAttributes("//*[@class=\"android.widget.EditText\"]", 15);
                        _client.SendTextSlow("//*[@class=\"android.widget.EditText\"]", search);
                        _client.ATX.Press(PressKey.Enter);
                        WaitForPostComplete(60);
                        var bounds = _client.FindBounds("", "//*[@class=\"android.widget.RelativeLayout\"]", 10);
                        if (bounds.Any())
                        {
                            var point = new RectangleArea(bounds.FirstOrDefault()).GetCenterPoint();
                            _client.Click(point.X, point.Y);
                        }


                        break;
                    }
            }
            int tickCount = Environment.TickCount;
            while (!_mainService._ct.IsCancellationRequested)
            {
                // [TIME-LIMIT v21] Ép giới hạn thời gian NGAY TRONG hành động xem video (mỗi vòng):
                // quá timeoutTaiKhoan/timeoutKichBan -> break -> ExecuteAsync đổi tài khoản. KHÔNG đụng logic FB.
                if (Stop()) break;


                await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(delayTo, delayFrom), $"Xem video, đợi {{time}}s...", 2);
                if (shouldInteract && reactions.Any())
                {
                    var message = TapReaction(reactions[0]);
                    await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(3, 7), $"{message}, đợi {{time}}s...", 2);
                    reactions.RemoveAt(0);
                }
                if (shouldShareWall && shareCount > 0)
                {
                    var message = TapShareNewfeed("");
                    await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(3, 7), $"{message}, đợi {{time}}s...", 2);
                    shareCount--;
                }
                if (shouldComment && commentCount > 0)
                {
                    string image = "";
                    string content = "";
                    if (_data.ContainsKey($"{action.Id}_txtComments"))
                    {
                        lock (Globals.Lock)
                        {
                            var contents = _data[$"{action.Id}_txtComments"];
                            if (contents.Any())
                            {
                                content = SubdyHelper.GetStringRandom(contents);
                                if (!settings.GetBooleanValue("checkBox5"))
                                {
                                    contents.Remove(content);
                                    _data[$"{action.Id}_txtComments"] = contents;
                                }
                                if (settings.GetBooleanValue("checkBox4"))
                                {
                                    var context = new ScriptActionContext();
                                    settings.DeleteValue("txtComments", content);
                                    action.Json = settings.GetJsonString();
                                    context.Update(action);
                                }
                            }

                        }
                        content = SubdyHelper.SpinText(content);
                    }
                    if (settings.GetBooleanValue("ckbAnh") && _data.ContainsKey($"{action.Id}_txtPathAnh") && _data[$"{action.Id}_txtComments"].Any())
                    {
                        lock (Globals.Lock)
                        {
                            var images = _data[$"{action.Id}_txtPathAnh"];
                            image = SubdyHelper.GetStringRandom(images);
                            if (settings.GetBooleanValue("checkBox3"))
                            {
                                images.Remove(image);
                                File.Delete(image);
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(content) || File.Exists(image))
                    {
                        string message = await CommentAction(content, image, settings.GetBooleanValue("ckbTagNeuBat"), settings.GetBooleanValue("checkBox2"));
                        await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(3, 7), $"{message}, đợi {{time}}s...", 2);
                    }
                    commentCount--;
                }


                if (Environment.TickCount - tickCount >= totalSeconds * 1000)
                {
                    break;
                }
                ScrollScreen(1, 2);
            }
            int result = 0;
            return result;
        }
        public async Task<int> HDXemReel(JsonHelper settings, ScriptAction action)
        {
            int totalSeconds = SubdyHelper.RandomValue(settings.GetIntType("numericUpDown2"), settings.GetIntType("numericUpDown1"));

            int delayFrom = settings.GetIntType("nudTimeFrom");
            int delayTo = settings.GetIntType("nudTimeTo");


            bool shouldInteract = settings.GetBooleanValue("ckbInteract");
            int interactCount = SubdyHelper.RandomValue(settings.GetIntType("nudInteractFrom", 1), settings.GetIntType("nudInteractTo", 1));
            List<string> reactionTypes = new List<string> { "Like", "Love", "Care", "Haha", "Wow", "Sad", "Angry" };
            if (!settings.GetBooleanValue("ckbLike"))
            {
                reactionTypes.Remove("Like");
            }
            if (!settings.GetBooleanValue("ckbTym"))
            {
                reactionTypes.Remove("Love");
            }
            if (!settings.GetBooleanValue("ckbThuong"))
            {
                reactionTypes.Remove("Care");
            }
            if (!settings.GetBooleanValue("ckbHaha"))
            {
                reactionTypes.Remove("Haha");
            }
            if (!settings.GetBooleanValue("ckbWow"))
            {
                reactionTypes.Remove("Wow");
            }
            if (!settings.GetBooleanValue("ckbBuon"))
            {
                reactionTypes.Remove("Sad");
            }
            if (!settings.GetBooleanValue("ckbGian"))
            {
                reactionTypes.Remove("Angry");
            }
            List<string> reactions = new List<string>();
            if (reactionTypes.Any())
            {
                for (int i = 0; i < interactCount; i++)
                {
                    reactions.Add(SubdyHelper.GetStringRandom(reactionTypes));
                }
            }

            bool shouldShareWall = settings.GetBooleanValue("ckbShareWall");
            int shareCount = SubdyHelper.RandomValue(settings.GetIntType("nudShareWallFrom", 1), settings.GetIntType("nudShareWallTo", 1));



            bool shouldComment = settings.GetBooleanValue("ckbComment");
            int commentCount = SubdyHelper.RandomValue(settings.GetIntType("nudCommentFrom", 1), settings.GetIntType("nudCommentTo", 1));



            string type = "DeXuat";
            if (settings.GetBooleanValue("ckbChiDinh"))
            {
                type = "ChiDinh";
            }
            else if (settings.GetBooleanValue("ckbTuKhoa"))
            {
                type = "TuKhoa";
            }
            switch (type)
            {
                case "DeXuat":
                    {
                        if (!OpenReel($"https://www.facebook.com/reel/{SubdyHelper.RandomString("123456789", SubdyHelper.RandomValue(6, 16))}"))
                        {
                            throw new SubdyExtension(SubdyEnum.Error, "Mở reels thất bại");
                        }
                        break;
                    }
                case "ChiDinh":
                    {
                        string url = SubdyHelper.GetStringRandom(settings.GetValuesList("txtLinks"));
                        if (string.IsNullOrEmpty(url))
                        {
                            throw new SubdyExtension(SubdyEnum.Error, "Không có link chỉ định");
                        }
                        if (OpenReel(url))
                        {
                            if (!OpenReel($"https://www.facebook.com/reel/{SubdyHelper.RandomString(length: SubdyHelper.RandomValue(6, 16))}"))
                            {
                                throw new SubdyExtension(SubdyEnum.Error, $"Mở reels thất bại {url}");
                            }
                        }
                        break;
                    }
                case "TuKhoa":
                    {
                        if (!OpenReel($"https://www.facebook.com/reel/{SubdyHelper.RandomString("123456789", SubdyHelper.RandomValue(6, 16))}"))
                        {
                            throw new SubdyExtension(SubdyEnum.Error, "Mở reels thất bại");
                        }
                        string search = SubdyHelper.GetStringRandom(settings.GetValuesList("txtLinks"));
                        _mainService.SetStatus($"Tìm kiếm {search}...", 2);
                        _client.Delay(3);
                        _client.ElementWithAttributes("//*[@content-desc=\"Search\"]");

                        _client.Delay(2);
                        _client.ElementWithAttributes("//*[@class=\"android.widget.EditText\"]", 15);
                        _client.SendTextSlow("//*[@class=\"android.widget.EditText\"]", search);
                        _client.ATX.Press(PressKey.Enter);
                        WaitForPostComplete(60);
                        var bounds = _client.FindBounds("", "//*[@class=\"android.widget.RelativeLayout\"]", 10);
                        if (bounds.Any())
                        {
                            var point = new RectangleArea(bounds.FirstOrDefault()).GetCenterPoint();
                            _client.Click(point.X, point.Y);
                        }


                        break;
                    }
            }
            int tickCount = Environment.TickCount;
            while (!_mainService._ct.IsCancellationRequested)
            {
                // [TIME-LIMIT v21] Ép giới hạn thời gian NGAY TRONG hành động xem reel (mỗi vòng):
                // quá timeoutTaiKhoan/timeoutKichBan -> break -> ExecuteAsync đổi tài khoản. KHÔNG đụng logic FB.
                if (Stop()) break;


                await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(delayTo, delayFrom), $"Xem reel, đợi {{time}}s...", 2);
                if (shouldInteract && reactions.Any())
                {
                    var message = TapReaction(reactions[0]);
                    await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(3, 7), $"{message}, đợi {{time}}s...", 2);
                    reactions.RemoveAt(0);
                }
                if (shouldShareWall && shareCount > 0)
                {
                    var message = TapShareNewfeed("");
                    await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(3, 7), $"{message}, đợi {{time}}s...", 2);
                    shareCount--;
                }
                if (shouldComment && commentCount > 0)
                {
                    string image = "";
                    string content = "";
                    if (_data.ContainsKey($"{action.Id}_txtComments"))
                    {
                        lock (Globals.Lock)
                        {
                            var contents = _data[$"{action.Id}_txtComments"];
                            if (contents.Any())
                            {
                                content = SubdyHelper.GetStringRandom(contents);
                                if (!settings.GetBooleanValue("checkBox5"))
                                {
                                    contents.Remove(content);
                                    _data[$"{action.Id}_txtComments"] = contents;
                                }
                                if (settings.GetBooleanValue("checkBox4"))
                                {
                                    var context = new ScriptActionContext();
                                    settings.DeleteValue("txtComments", content);
                                    action.Json = settings.GetJsonString();
                                    context.Update(action);
                                }
                            }

                        }
                        content = SubdyHelper.SpinText(content);
                    }
                    if (settings.GetBooleanValue("ckbAnh") && _data.ContainsKey($"{action.Id}_txtPathAnh") && _data[$"{action.Id}_txtComments"].Any())
                    {
                        lock (Globals.Lock)
                        {
                            var images = _data[$"{action.Id}_txtPathAnh"];
                            image = SubdyHelper.GetStringRandom(images);
                            if (settings.GetBooleanValue("checkBox3"))
                            {
                                images.Remove(image);
                                File.Delete(image);
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(content) || File.Exists(image))
                    {
                        string message = await CommentAction(content, image, settings.GetBooleanValue("ckbTagNeuBat"), settings.GetBooleanValue("checkBox2"));
                        await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(3, 7), $"{message}, đợi {{time}}s...", 2);
                    }
                    commentCount--;
                }


                if (Environment.TickCount - tickCount >= totalSeconds * 1000)
                {
                    break;
                }
                ScrollScreen(1, 2);
            }
            int result = 0;
            return result;
        }
        public async Task<int> HDTuongTacNewfeed(JsonHelper settings, ScriptAction action)
        {
            // Thời gian lướt tổng (giây) — dùng numericUpDown2 (from) đến numericUpDown1 (to)
            int totalFrom = Math.Max(1, settings.GetIntType("numericUpDown2", 60));
            int totalTo = Math.Max(totalFrom, settings.GetIntType("numericUpDown1", 300));
            int totalSeconds = SubdyHelper.RandomValue(totalFrom, totalTo);

            // Thời gian dừng giữa mỗi lần action — dùng chính dải tổng để không phụ thuộc trường đã bỏ
            int delayFrom = Math.Max(1, settings.GetIntType("nudTimeFrom", 10));
            int delayTo = Math.Max(delayFrom, settings.GetIntType("nudTimeTo", 30));

            // Delay trước/sau khi lướt newfeed — trước đây hardcode 20s + 20s (tổng 40s).
            int delayTruoc = Math.Max(0, settings.GetIntType("nudDelayTruoc", 20));
            int delaySau = Math.Max(0, settings.GetIntType("nudDelaySau", 20));


            bool shouldInteract = settings.GetBooleanValue("ckbInteract");
            int interactCount = SubdyHelper.RandomValue(settings.GetIntType("nudInteractFrom", 1), settings.GetIntType("nudInteractTo", 1));
            List<string> reactionTypes = new List<string> { "Like", "Love", "Care", "Haha", "Wow", "Sad", "Angry" };
            if (!settings.GetBooleanValue("ckbLike"))
            {
                reactionTypes.Remove("Like");
            }
            if (!settings.GetBooleanValue("ckbTym"))
            {
                reactionTypes.Remove("Love");
            }
            if (!settings.GetBooleanValue("ckbThuong"))
            {
                reactionTypes.Remove("Care");
            }
            if (!settings.GetBooleanValue("ckbHaha"))
            {
                reactionTypes.Remove("Haha");
            }
            if (!settings.GetBooleanValue("ckbWow"))
            {
                reactionTypes.Remove("Wow");
            }
            if (!settings.GetBooleanValue("ckbBuon"))
            {
                reactionTypes.Remove("Sad");
            }
            if (!settings.GetBooleanValue("ckbGian"))
            {
                reactionTypes.Remove("Angry");
            }
            List<string> reactions = new List<string>();
            if (reactionTypes.Any())
            {
                for (int i = 0; i < interactCount; i++)
                {
                    reactions.Add(SubdyHelper.GetStringRandom(reactionTypes));
                }
            }

            bool shouldShareWall = settings.GetBooleanValue("ckbShareWall");
            int shareCount = SubdyHelper.RandomValue(settings.GetIntType("nudShareWallFrom", 1), settings.GetIntType("nudShareWallTo", 1));

            // Bỏ tính năng gửi lời mời kết bạn/follow — không còn trong UI mới
            bool follow = settings.GetBooleanValue("checkBox1");
            int followCount = follow
                ? SubdyHelper.RandomValue(settings.GetIntType("nudTuKhoaFrom", 1), settings.GetIntType("nudTuKhoaTo", 1))
                : 0;

            bool shouldComment = settings.GetBooleanValue("ckbComment");
            int commentCount = SubdyHelper.RandomValue(settings.GetIntType("nudCommentFrom", 1), settings.GetIntType("nudCommentTo", 1));


            string notifyTabXPath = _client.FindElement("", new List<string> { "//*[@content-desc=\"Go to profile\"]", "//*[contains(@content-desc, \"Home, tab\")]" }, 1);
            if (string.IsNullOrEmpty(notifyTabXPath))
            {
                OpenFacebookTimeline();
            }

            // Xem bài viết trước khi bắt đầu lướt newsfeed (delay cấu hình được).
            if (delayTruoc > 0)
                await _mainService.DelayMessageAsync(delayTruoc, "Xem bài viết trước khi lướt, đợi {time}s...", 2);

            int tickCount = Environment.TickCount;
            while (!_mainService._ct.IsCancellationRequested)
            {
                // [TIME-LIMIT v21] Ép giới hạn thời gian NGAY TRONG hành động lướt newfeed (mỗi vòng):
                // quá timeoutTaiKhoan/timeoutKichBan -> break -> ExecuteAsync đổi tài khoản. KHÔNG đụng logic FB.
                if (Stop()) break;
              //  await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(delayFrom, delayTo), $"Xem bài viết, đợi {{time}}s...", 2);
                if (follow && followCount > 0)
                {
                    if (_client.ElementWithAttributes(new List<string> { "//*[@content-desc=\"People you may know\"]", "//*[@text=\"People you may know\"]", }, 1, click: false))
                    {
                        var message = TapAddFriendAndFollow();
                        await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(3, 7), $"{message}, đợi {{time}}s...", 2);
                        followCount--;
                    }

                }

                if (shouldInteract && reactions.Any())
                {
                    var message = TapReaction(reactions[0]);
                    await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(3, 7), $"{message}, đợi {{time}}s...", 2);
                    reactions.RemoveAt(0);
                }
                if (shouldShareWall && shareCount > 0)
                {
                    var message = TapShareNewfeed("");
                    await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(3, 7), $"{message}, đợi {{time}}s...", 2);
                    shareCount--;
                }
                if (shouldComment && commentCount > 0)
                {
                    string image = "";
                    string content = "";
                    if (_data.ContainsKey($"{action.Id}_txtComments"))
                    {
                        lock (Globals.Lock)
                        {
                            var contents = _data[$"{action.Id}_txtComments"];
                            if (contents.Any())
                            {
                                content = SubdyHelper.GetStringRandom(contents);
                                if (!settings.GetBooleanValue("checkBox5"))
                                {
                                    contents.Remove(content);
                                    _data[$"{action.Id}_txtComments"] = contents;
                                }
                                if (settings.GetBooleanValue("checkBox4"))
                                {
                                    var context = new ScriptActionContext();
                                    settings.DeleteValue("txtComments", content);
                                    action.Json = settings.GetJsonString();
                                    context.Update(action);
                                }
                            }

                        }
                        content = SubdyHelper.SpinText(content);
                    }
                    if (settings.GetBooleanValue("ckbAnh") && _data.ContainsKey($"{action.Id}_txtPathAnh") && _data[$"{action.Id}_txtPathAnh"].Any())
                    {
                        lock (Globals.Lock)
                        {
                            var images = _data[$"{action.Id}_txtPathAnh"];
                            image = SubdyHelper.GetStringRandom(images);
                            if (settings.GetBooleanValue("checkBox3"))
                            {
                                images.Remove(image);
                                File.Delete(image);
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(content) || File.Exists(image))
                    {
                        string message = await CommentAction(content, image, settings.GetBooleanValue("ckbTagNeuBat"), settings.GetBooleanValue("checkBox2"));
                        await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(3, 7), $"{message}, đợi {{time}}s...", 2);
                    }
                    commentCount--;
                }



                if (Environment.TickCount - tickCount >= totalSeconds * 1000)
                {
                    break;
                }
                ScrollScreen(1, SubdyHelper.RandomValue(5, 30), SubdyHelper.RandomValue(200, 800));
            }

            // Dừng sau khi lướt xong trước khi backup (delay cấu hình được).
            if (delaySau > 0)
                await _mainService.DelayMessageAsync(delaySau, "Lướt xong, đợi {time}s trước khi backup...", 2);

            int result = 0;
            return result;
        }
        private string TapAddFriendAndFollow()
        {
            var xpath = _client.FindElement("", new List<string> { "//*[@text=\"Add friend\"]", "//*[@content-desc=\"Add friend\"]", "//*[@text=\"Follow\"]", "//*[@content-desc=\"Follow\"]" }, 10);
            if (string.IsNullOrEmpty(xpath))
            {
                return "Không tìm thấy nút thêm bạn bè/theo dõi...";
            }
            if (!_client.ElementWithAttributes(xpath, 3))
            {
                return "Khong thao tac duoc nut them ban be/theo doi...";
            }
            if (xpath.Contains("Follow"))
            {
                return "Follow thành công...";
            }
            else if (xpath.Contains("friend"))
            {
                return "Đã gửi lời mới kết bạn thành công...";
            }
            return "Không tìm thấy nút thêm bạn bè/theo dõi...";
        }
        private string TapReaction(string reation)
        {
            _mainService.SetStatus("Find reation...", 2);
        ReFail:
            for (int i = 0; i < 10; i++)
            {
                string dump = _client.GetXMLSource();
                if (string.IsNullOrEmpty(dump))
                {
                    continue;
                }
                dump = dump.ToLower();
                if (!dump.Contains("share") && !dump.Contains("like"))
                {
                    ScrollScreen(1, 1);
                    continue;
                }
                if (dump.Contains(", pressed. double tap and hold"))
                {
                    return $"Đã làm {reation} đó trước.";
                }
                break;
            }
            var element = _client.FindElement("", new List<string> { "//*[@content-desc=\"Tap to open more options\"]", "//*[contains(@content-desc, 'Like button')]", "//*[contains(@content-desc, 'Like. Double')]" }, 5);
            if (element == "//*[@content-desc=\"Tap to open more options\"]")
            {
                _client.ElementWithAttributes(element, 5);
                _client.ElementWithAttributes("//*[@content-desc=\"Hide\"]", 5);
                goto ReFail;
            }
            var elementLike = _client.FindPoint(element, 15);
            string type = reation.ToLower();
            if (elementLike != null && elementLike != System.Drawing.Point.Empty)
            {
                string num = reation.ToLower();

                _client.LongClick(elementLike.X, elementLike.Y, 1000);
                if (!type.Contains("like") && !string.IsNullOrEmpty(type))
                {
                    num = char.ToUpper(type[0]) + (type.Length > 1 ? type.Substring(1) : "");
                }
                if (_client.ElementWithAttributes($"//*[@content-desc='{num}']", 3))
                {
                    return $"Đã làm {reation} thành công.";
                }
            }
            return $"Không tìm thấy nút {reation}.";
        }
        private string TapShareNewfeed(string content)
        {
            _mainService.SetStatus("Find share...", 2);
            for (int i = 0; i < 10; i++)
            {
                string dump = _client.GetXMLSource();
                if (string.IsNullOrEmpty(dump))
                {
                    continue;
                }
                dump = dump.ToLower();
                if (!dump.Contains("share") && !dump.Contains("like"))
                {
                    ScrollScreen(1, 1);
                    continue;
                }
                break;
            }
            var element = _client.FindElement("", new List<string> { "//*[@content-desc=\"Share\"]" }, 5);
            if (element == "//*[@content-desc=\"Share\"]")
            {
                _client.ElementWithAttributes(element);
                if (_client.ElementWithAttributes("//*[@content-desc=\"Share now\"]", 10, click: false))
                {
                    if (!string.IsNullOrEmpty(content))
                    {
                        _client.SendTextSlow("//*[@class=\"android.widget.EditText\"]", content);
                    }
                    if (_client.ElementWithAttributes("//*[@content-desc=\"Share now\"]"))
                    {
                        return $"Đã share lên trang cá nhân thành công.";
                    }
                }
            }
            return $"Không tìm thấy nút share.";
        }
        private async Task<string> CommentAction(string content, string image, bool neubat, bool follower)
        {
            var medias = new List<string>();
            bool needsBack = false;
            List<string> foundComments = new List<string>();
            string commentText = content;

            _mainService.SetStatus("Find Comment...", 2);
            string pageSource = _client.GetXMLSource();

            foundComments = _client.FindBounds(pageSource, new List<string>
    {
        "//*[@content-desc=\"Comment Button\"]",
        "//*[@content-desc=\"Answer Button\"]",
        "//*[@text='Answer']",
        "//*[@text=\"Comment\"]",
        "//*[@content-desc=\"Comment\"]",
        "//*[@resource-id='composerInput']",
        "//*[@text='Write a comment…']"
    }, 1);

            _mainService.SetStatus($"Find Comment: {foundComments.Count}", 2);
            if (foundComments.Count <= 0)
                return "Không tìm được nút bình luận";

            needsBack = !_client.ElementWithAttributes("//*[@text='Write a comment…']", 1, pageSource, false);

            Point commentPoint = new RectangleArea(foundComments.First()).GetCenterPoint();
            _mainService.SetStatus($"Tap Comment...", 2);
            if (!_client.Click(commentPoint.X, commentPoint.Y))
                return "Không tìm được nút bình luận";

            _client.Delay(2);
            int attempt;
            for (attempt = 0; attempt < 2; attempt++)
            {
                _mainService.SetStatus($"Find EditText...", 2);
                if (!_client.ElementWithAttributes("//*[@class='android.widget.EditText']", 5, pageSource, false))
                {
                    if (!_client.ElementWithAttributes("//*[@content-desc=\"Comment input box\"]"))
                        break;

                    continue;
                }

                if (!string.IsNullOrEmpty(content))
                {
                    _mainService.SetStatus("Nhập dữ liệu...", 2);
                    _client.SendTextSlow("//*[@class='android.widget.EditText']", commentText);
                    _client.Delay(2);
                }

                if (File.Exists(image))
                {
                    medias = UploadMediaFiles(new List<string> { image });
                    _mainService.SetStatus("Find Camera...", 2);
                    if (_client.ElementWithAttributes("//*[@content-desc='Show photos and videos']", 5, ""))
                    {
                        _mainService.SetStatus("Select image...", 2);
                        for (int j = 0; j < 10; j++)
                        {
                            string pageSrc = _client.GetXMLSource();
                            string elementXPath = _client.FindElement(pageSrc, new List<string>
            {
                "//*[@text='Allow']",
                "//*[@text='Enable gallery access']",
                "//*[@content-desc='Photo' or @content-desc='Video']",
                "//*[contains(@content-desc,'Photo') or contains(@content-desc,'Video')]"
            }, 1);

                            if (elementXPath == "//*[@content-desc='Photo' or @content-desc='Video']" || elementXPath == "//*[contains(@content-desc,'Photo') or contains(@content-desc,'Video')]")
                            {
                                List<string> photos = _client.FindBounds("", "//*[@content-desc='Photo' or @content-desc='Video']/parent::*[@selected='false']", 1);

                                if (!photos.Any())
                                {
                                    photos = _client.FindBounds("", "//*[contains(@content-desc,'Photo') or contains(@content-desc,'Video')]/parent::*[@selected='false']", 1);
                                }

                                if (photos.Count > 1)
                                    photos = photos.GetRange(1, photos.Count - 1);

                                if (photos.Count > 0)
                                {
                                    string selectedPhoto = photos.OrderBy(x => Guid.NewGuid()).Last();
                                    var point = new RectangleArea(selectedPhoto).GetCenterPoint();
                                    _client.Click(point.X, point.Y);
                                    break;
                                }
                            }
                            else if (!string.IsNullOrEmpty(elementXPath))
                            {
                                _mainService.SetStatus("Tap " + elementXPath + "...", 2);
                                _client.ElementWithAttributes(elementXPath, 1, pageSrc);
                            }

                            _client.Delay(1);
                        }

                        _client.ATX.Press(PressKey.Back);
                    }
                }
                if (neubat)
                {
                    _mainService.SetStatus($"Đang gắn thẻ nổi bật...", 2);
                    List<string> neubats = new List<string> { "@highlight", "@neu" };
                    foreach (var item in neubats)
                    {

                        var count = item.Length;
                        _client.SendTextSlow("//*[@class='android.widget.EditText']", " ", clear: false);
                        foreach (char c in item)
                        {
                            _client.SendTextSlow("//*[@class='android.widget.EditText']", c.ToString(), clear: false);
                            Thread.Sleep(100); // delay 100ms giữa mỗi ký tự cho tự nhiên hơn
                        }
                        Thread.Sleep(4000);
                        if (item == "@highlight" && _client.ElementWithAttributes("//*[@text=\"@highlight\"]"))
                        {
                            break;
                        }
                        if (item == "@neu" && _client.ElementWithAttributes("//*[@text=\"@nêu bật\"]"))
                        {
                            break;
                        }
                        for (int i = 0; i < item.Length; i++)
                        {
                            _client.Shell("input keyevent KEYCODE_DEL");
                            Thread.Sleep(50);
                        }
                    }
                }
                if (follower)
                {
                    _mainService.SetStatus($"Đang gắn thẻ người theo dõi...", 2);
                    List<string> neubats = new List<string> { "@followers", "@người", };
                    foreach (var item in neubats)
                    {
                        var count = item.Length;
                        _client.SendTextSlow("//*[@class='android.widget.EditText']", " ", clear: false);
                        foreach (char c in item)
                        {
                            _client.SendTextSlow("//*[@class='android.widget.EditText']", c.ToString(), clear: false);
                            Thread.Sleep(100); // delay 100ms giữa mỗi ký tự cho tự nhiên hơn
                        }
                        Thread.Sleep(4000);
                        if (item == "@followers" && _client.ElementWithAttributes("//*[@text=\"@followers\"]"))
                        {
                            break;
                        }
                        if (item == "@người" && _client.ElementWithAttributes("//*[@text=\"@người theo dõi\"]"))
                        {
                            break;
                        }
                        for (int i = 0; i < item.Length; i++)
                        {
                            _client.Shell("input keyevent KEYCODE_DEL");
                            Thread.Sleep(50);
                        }
                    }
                }
                _client.Delay(1);


                _mainService.SetStatus("Tap Send...", 2);
                pageSource = "";
                string sendButtonXPath = _client.FindElement("", new List<string> { "//*[@content-desc=\"Send\"]", "//*[@text='Post']" }, 15);
                if (_client.ElementWithAttributes(sendButtonXPath, 5, pageSource))
                {
                    await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(5, 20), "Tap Send, " + "đợi" + " {time}s...", 2);
                    _client.ElementWithAttributes("//*[@content-desc='Just now']", 120, click: false);
                    _client.ATX.Press(PressKey.Back);
                    _client.Delay(2);
                    _client.ATX.Press(PressKey.Back);
                    if (medias.Any())
                    {
                        DeleteMediaFiles(medias);
                    }
                    return $"Đã bình luận nội dung: '{content.Substring(0, Math.Min(3, content.Length))}...' và image: '{image.Substring(0, Math.Min(3, image.Length))}...'";
                }
                break;
            }

            if (needsBack)
            {
                _mainService.SetStatus("Back...", 2);
                if (attempt == 1)
                    _client.Shell("input keyevent 4");
                else
                    _client.Shell("input keyevent 4");
                _client.Delay(3);

                _client.Delay(3);
            }
            else
            {
                pageSource = _client.GetXMLSource();
                if (_client.ElementWithAttributes("//*[@text='Write a comment…']", 1, pageSource, false) &&
                    _client.FindBounds(pageSource, new List<string>
                    {
                "//*[@content-desc=\"Comment Button\"]",
                "//*[@content-desc=\"Answer Button\"]",
                "//*[@text='Answer']",
                "//*[@text=\"Comment\"]",
                "//*[@content-desc=\"Comment\"]"
                    }, 1).Count == 0)
                {
                    ScrollScreen(-1);
                }
            }
            return "Bình luận thất bại";
        }


        public async Task<int> HDGuiLoiMoiKetBan(JsonHelper settings, ScriptAction action)
        {
            int friendCountFrom = settings.GetIntType("nudTimeFrom");
            int friendCountTo = settings.GetIntType("nudTimeTo");
            int totalRequests = SubdyHelper.RandomValue(friendCountFrom, friendCountTo);


            int delayFrom = settings.GetIntType("numericUpDown4");
            int delayTo = settings.GetIntType("numericUpDown3");

            bool follow = settings.GetBooleanValue("checkBox1");

            int follow_FriendFrom = settings.GetIntType("numericUpDown1");
            int follow_FriendTo = settings.GetIntType("numericUpDown2");
            int totalFollow_Friend = 0;


            var keywords = settings.GetValuesList("txtLinks");

            string type = "DeXuat";
            if (settings.GetBooleanValue("ckbChiDinh"))
            {
                type = "ChiDinh";
            }
            else if (settings.GetBooleanValue("ckbTuKhoa"))
            {
                type = "TuKhoa";
            }
            else if (settings.GetBooleanValue("radioButton1"))
            {
                type = "Groups";
            }
            List<string> old = new List<string>();
            Dictionary<string, string> keyValues = new Dictionary<string, string>();
            switch (type)
            {
                case "DeXuat":
                    {
                        if (!DeplinkFacebook("fb://friends").Contains("dat=fb://friends"))
                        {
                            throw new SubdyExtension(SubdyEnum.JobFail, "Không tìm kiếm được friends");
                        }
                        string xpath = "//node[contains(@class, 'android.widget.Button') and @content-desc and string-length(@content-desc) > 0 and " +
                            "contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'add') and " +
                            "contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'as a friend') and " +
                            "@visible-to-user='true']";
                        var nodes = _client.FindElementsNotToLower(15, "", xpath);
                        if (!nodes.Any())
                        {
                            throw new SubdyExtension(SubdyEnum.JobFail, "Không tìm được friends");
                        }
                        break;
                    }
                case "Groups":
                    {
                        if (!keywords.Any())
                        {
                            throw new SubdyExtension(SubdyEnum.JobFail, "Không có uid group chỉ định");
                        }
                        break;
                    }
                case "ChiDinh":
                    {
                        if (!keywords.Any())
                        {
                            throw new SubdyExtension(SubdyEnum.JobFail, "Không có uid friends chỉ định");
                        }
                        break;
                    }
                case "TuKhoa":
                    {
                        var urls = settings.GetValuesList("txtLinks");
                        if (!urls.Any())
                        {
                            throw new SubdyExtension(SubdyEnum.Error, "Không có keyword");
                        }
                        if (!SearchOnFacebook(SubdyHelper.GetStringRandom(urls), "Pages"))
                        {
                            throw new SubdyExtension(SubdyEnum.JobFail, "Không tìm kiếm được page");
                        }
                        string xpath = "//node[contains(@class,'android.view.ViewGroup') and @content-desc and string-length(@text) > 0 and @visible-to-user='true'" +
                             "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'followers'))]";
                        var nodes = _client.FindElementsNotToLower(15, "", xpath);
                        if (!nodes.Any())
                        {
                            throw new SubdyExtension(SubdyEnum.JobFail, "Không tìm được page.");
                        }

                        break;
                    }
            }
            int count = 1;
            while (!_mainService._ct.IsCancellationRequested)
            {
                // [TIME-LIMIT v21] Ép giới hạn thời gian NGAY TRONG hành động gửi lời mời kết bạn (mỗi vòng):
                // quá timeoutTaiKhoan/timeoutKichBan -> break -> ExecuteAsync đổi tài khoản. KHÔNG đụng logic FB.
                if (Stop()) break;

                if (count > totalRequests)
                {
                    break;
                }
                switch (type)
                {
                    case "DeXuat":
                        {
                            string xpath = "//node[contains(@class, 'android.widget.Button') and @content-desc and string-length(@content-desc) > 0 and " +
                             "contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'add') and " +
                             "contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'as a friend') and " +
                             "@visible-to-user='true']";
                            var nodes = _client.FindElementsNotToLower(15, "", xpath);
                            if (!nodes.Any())
                            {
                                ScrollScreen(1, 1);
                                count++;
                                continue;
                            }
                            var info = _client.ExtractNodeInfo(nodes.First().OuterXml);
                            var point = new RectangleArea(info["bounds"]).GetCenterPoint();
                            _client.ADB.Shell($"input tap {point.X} {point.Y}");
                            await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(delayFrom, delayTo), $"({count}/{totalRequests}) {info["content-desc"]}" + ", đợi {time}s...", 2);
                            count++;
                            break;
                        }
                    case "Groups":
                        {
                            totalFollow_Friend = SubdyHelper.RandomValue(follow_FriendFrom, follow_FriendTo);
                            if (!keywords.Any())
                            {
                                count = totalRequests + 1;
                                continue;
                            }
                            string keyword = SubdyHelper.GetStringRandom(keywords);
                            if (string.IsNullOrEmpty(keyword))
                            {
                                count = totalRequests + 1;
                                continue;
                            }
                            DeplinkFacebook($"fb://group/{keyword}");
                            keywords.Remove(keyword);
                            await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(5, 10), $"({count}/{totalRequests}) Go to group {keyword}" + ", đợi {time}s...", 2);
                            ScrollScreen(-1, 1);
                            var nodes = _client.FindElementsNotToLower(10, "", "//node[contains(@class, 'android.widget.Button') and @content-desc and string-length(@content-desc) > 0 and " +
                             "contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'group') and " +
                             "contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'members') and " +
                             "@visible-to-user='true']");
                            if (!nodes.Any())
                            {
                                continue;
                            }
                            var info = _client.ExtractNodeInfo(nodes.First().OuterXml);
                            string status = $"({count}/{totalRequests}) {info["content-desc"]}";
                            var point = new RectangleArea(info["bounds"]).GetCenterPoint();
                            _client.ADB.Shell($"input tap {point.X} {point.Y}");
                            Thread.Sleep(3000);
                            bool check = false;
                            int tickCount = Environment.TickCount;
                            while (!_mainService._ct.IsCancellationRequested)
                            {
                                if (_client.ElementWithAttributes("//*[@content-desc=\"See all\"]", 2))
                                {
                                    check = true;
                                    break;
                                }
                                ScrollScreen(1, 1);
                                if (Environment.TickCount - tickCount >= 60 * 1000)
                                {
                                    break;
                                }
                            }
                            if (!check)
                            {
                                break;
                            }
                            int refail = 0;
                            while (!_mainService._ct.IsCancellationRequested)
                            {
                                if (count > totalRequests || totalFollow_Friend <= 0 || refail > 5)
                                {
                                    break;
                                }
                                if (_client.ElementWithAttributes("//*[@content-desc=\"Add friend\"]", 2))
                                {
                                    refail = 0;
                                    await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(delayFrom, delayTo), $"({count}/{totalRequests}) Thêm bạn mới trong {info["content-desc"]} " + ", đợi {time}s...", 2);
                                    count++;
                                    totalFollow_Friend--;
                                }
                                else
                                {
                                    ScrollScreen(1, 1);
                                    refail++;
                                }

                            }



                            break;
                        }
                    case "ChiDinh":
                        {
                            if (!keywords.Any())
                            {
                                count = totalRequests + 1;
                                continue;
                            }
                            string uid = SubdyHelper.GetStringRandom(keywords);
                            keywords.Remove(uid);
                            if (string.IsNullOrEmpty(uid))
                            {
                                continue;
                            }
                            DeplinkFacebook($"fb://profile/{uid}");
                            int refail = 0;
                            while (!_mainService._ct.IsCancellationRequested)
                            {
                                if (count > totalRequests || refail > 5)
                                {
                                    break;
                                }
                                if (_client.ElementWithAttributes("//*[@content-desc=\"Add friend\"]", 5))
                                {
                                    await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(5, 10), $"({count}/{totalRequests}) Gửi lời mời kết bạn với {uid} " + ", đợi {time}s...", 2);
                                    count++;
                                    refail = 0;
                                    continue;
                                }
                                else
                                {
                                    ScrollScreen(-1, 1);
                                    refail++;
                                }

                            }


                            break;
                        }
                    case "TuKhoa":
                        {
                            totalFollow_Friend = SubdyHelper.RandomValue(follow_FriendFrom, follow_FriendTo);
                            if (!keywords.Any())
                            {
                                count = totalRequests + 1;
                                continue;
                            }
                            string keyword = SubdyHelper.GetStringRandom(keywords);
                            if (string.IsNullOrEmpty(keyword))
                            {
                                count = totalRequests + 1;
                                continue;
                            }
                            SearchOnFacebook(keyword, "People");
                            keywords.Remove(keyword);
                            await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(5, 10), $"({count}/{totalRequests}) Search group {keyword}" + ", đợi {time}s...", 2);
                            int refail = 0;
                            List<string> xpaths = new List<string>
                            {
                                "//*[@content-desc=\"Add friend\"]",

                            };
                            if (follow)
                            {
                                xpaths.Add("//*[@content-desc=\"Follow\"]");
                            }
                            while (!_mainService._ct.IsCancellationRequested)
                            {
                                if (count > totalRequests || totalFollow_Friend <= 0 || refail > 5)
                                {
                                    break;
                                }
                                if (_client.ElementWithAttributes(xpaths, 2))
                                {
                                    refail = 0;
                                    await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(delayFrom, delayTo), $"({count}/{totalRequests}) Thêm bạn mới với từ khóa {keyword} " + ", đợi {time}s...", 2);
                                    count++;
                                    totalFollow_Friend--;
                                }
                                else
                                {
                                    if (_client.ElementWithAttributes("//*[@content-desc=\"Profile Picture\"]", 1, "", false))
                                    {
                                        refail++;
                                        _client.ATX.Press(PressKey.Back);
                                        continue;
                                    }
                                    ScrollScreen(1, 1);
                                    refail++;
                                }

                            }
                            break;
                        }
                }
            }
            return count;
        }
        public async Task<int> HDTuongTacBanBe(JsonHelper settings, ScriptAction action)
        {
            int count = SubdyHelper.RandomValue(settings.GetIntType("nudTuKhoaFrom"), settings.GetIntType("nudTuKhoaTo"));

            int delayFrom = settings.GetIntType("nudTimeFrom");
            int delayTo = settings.GetIntType("nudTimeTo");


            bool shouldInteract = settings.GetBooleanValue("ckbInteract");
            int interactCount = SubdyHelper.RandomValue(settings.GetIntType("nudInteractFrom", 1), settings.GetIntType("nudInteractTo", 1));
            List<string> reactionTypes = new List<string> { "Like", "Love", "Care", "Haha", "Wow", "Sad", "Angry" };
            if (!settings.GetBooleanValue("ckbLike"))
            {
                reactionTypes.Remove("Like");
            }
            if (!settings.GetBooleanValue("ckbTym"))
            {
                reactionTypes.Remove("Love");
            }
            if (!settings.GetBooleanValue("ckbThuong"))
            {
                reactionTypes.Remove("Care");
            }
            if (!settings.GetBooleanValue("ckbHaha"))
            {
                reactionTypes.Remove("Haha");
            }
            if (!settings.GetBooleanValue("ckbWow"))
            {
                reactionTypes.Remove("Wow");
            }
            if (!settings.GetBooleanValue("ckbBuon"))
            {
                reactionTypes.Remove("Sad");
            }
            if (!settings.GetBooleanValue("ckbGian"))
            {
                reactionTypes.Remove("Angry");
            }
            List<string> reactions = new List<string>();
            if (reactionTypes.Any())
            {
                for (int i = 0; i < interactCount; i++)
                {
                    reactions.Add(SubdyHelper.GetStringRandom(reactionTypes));
                }
            }

            bool shouldShareWall = settings.GetBooleanValue("ckbShareWall");
            int shareCount = SubdyHelper.RandomValue(settings.GetIntType("nudShareWallFrom", 1), settings.GetIntType("nudShareWallTo", 1));



            bool shouldComment = settings.GetBooleanValue("ckbComment");
            int commentCount = SubdyHelper.RandomValue(settings.GetIntType("nudCommentFrom", 1), settings.GetIntType("nudCommentTo", 1));


            if (!DeplinkFacebook("fb://friends").Contains("dat=fb://friends"))
            {
                throw new SubdyExtension(SubdyEnum.Error, "Mở bạn bè thất bại");
            }
            if (!_client.ElementWithAttributes(new List<string> { "//*[@content-desc=\"Your friends\"]" }, 15))
            {
                throw new SubdyExtension(SubdyEnum.Error, "Không tìm thấy nút Your friends");
            }
            _mainService.SetStatus("Tìm kiếm bạn bè...", 2);
            var friends = _client.FindBounds("", "//*[@class='android.widget.ImageView']/parent::*[@visible-to-user='true']", 15);
            if (!friends.Any())
            {
                throw new SubdyExtension(SubdyEnum.Error, "Không tìm thấy bạn bè nào cả");
            }
            int tickCount = Environment.TickCount;
            int countFriends = 0;
            while (!_mainService._ct.IsCancellationRequested)
            {
                // [TIME-LIMIT v21] Ép giới hạn thời gian NGAY TRONG hành động tương tác bạn bè (mỗi vòng):
                // quá timeoutTaiKhoan/timeoutKichBan -> break -> ExecuteAsync đổi tài khoản. KHÔNG đụng logic FB.
                if (Stop()) break;
                if (countFriends >= count)
                {
                    break;
                }
                friends = _client.FindBounds("", "//*[@class='android.widget.ImageView']/parent::*[@visible-to-user='true']", 15);
                if (!friends.Any())
                {
                    break;
                }
                foreach (var friend in friends)
                {
                    if (countFriends >= count)
                    {
                        break;
                    }

                    var point = new RectangleArea(friend).GetCenterPoint();
                    if (!_client.Click(point.X, point.Y))
                    {
                        continue;
                    }
                    countFriends++;
                    await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(delayTo, delayFrom), $"Xem bài viết, đợi {{time}}s...", 2);
                    ScrollScreen(1, 3);

                    if (shouldInteract && reactions.Any())
                    {
                        var message = TapReaction(reactions[0]);
                        await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(3, 7), $"{message}, đợi {{time}}s...", 2);
                        reactions.RemoveAt(0);
                    }
                    if (shouldShareWall && shareCount > 0)
                    {
                        var message = TapShareNewfeed("");
                        await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(3, 7), $"{message}, đợi {{time}}s...", 2);
                        shareCount--;
                    }
                    if (shouldComment && commentCount > 0)
                    {
                        string image = "";
                        string content = "";
                        if (_data.ContainsKey($"{action.Id}_txtComments"))
                        {
                            lock (Globals.Lock)
                            {
                                var contents = _data[$"{action.Id}_txtComments"];
                                if (contents.Any())
                                {
                                    content = SubdyHelper.GetStringRandom(contents);
                                    if (!settings.GetBooleanValue("checkBox5"))
                                    {
                                        contents.Remove(content);
                                        _data[$"{action.Id}_txtComments"] = contents;
                                    }
                                    if (settings.GetBooleanValue("checkBox4"))
                                    {
                                        var context = new ScriptActionContext();
                                        settings.DeleteValue("txtComments", content);
                                        action.Json = settings.GetJsonString();
                                        context.Update(action);
                                    }
                                }

                            }
                            content = SubdyHelper.SpinText(content);
                        }
                        if (settings.GetBooleanValue("ckbAnh") && _data.ContainsKey($"{action.Id}_txtPathAnh") && _data[$"{action.Id}_txtComments"].Any())
                        {
                            lock (Globals.Lock)
                            {
                                var images = _data[$"{action.Id}_txtPathAnh"];
                                image = SubdyHelper.GetStringRandom(images);
                                if (settings.GetBooleanValue("checkBox3"))
                                {
                                    images.Remove(image);
                                    File.Delete(image);
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(content) || File.Exists(image))
                        {
                            string message = await CommentAction(content, image, settings.GetBooleanValue("ckbTagNeuBat"), settings.GetBooleanValue("checkBox2"));
                            await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(3, 7), $"{message}, đợi {{time}}s...", 2);
                        }
                        commentCount--;
                    }


                    _client.ATX.Press(PressKey.Back);
                }
                ScrollScreen(1, 2);
            }
            int result = 0;
            return result;

        }
        public int HDBatCheDoChuyenNghiep(int accountId, string statusPrefix, string actionName)
        {
            string text = statusPrefix + "Đang" + " " + actionName + ": ";
            SetStatusAccount(accountId, text + "Đang chạy...");
            bool flag = false;
            try
            {
                while (OpenFacebookLink(accountId, text, "fb://profile"))
                {
                    int num = 60;
                    string text2 = "";
                    string text3 = "";
                    for (int tickCount = Environment.TickCount; Environment.TickCount - tickCount < num * 1000; SubdyHelper.RandomValue(0, 2))
                    {
                        text2 = "";
                        string text5 = _client.FindElement("", new List<string> { "//*[@content-desc='More']", "//*[@content-desc='Turn on']" }, 5);
                        if (!(text5 == "//*[@content-desc='More']"))
                        {
                            if (!(text5 == "//*[@content-desc='Turn on']"))
                            {
                                Bitmap f608D = _client.Screenshot();
                                if (_client.IsImageMatch("dataimage\\turnonpromode", f608D))
                                {
                                    _client.FindAndClickImage("dataimage\\turnonpromode", f608D);
                                    continue;
                                }
                                if (_client.IsImageMatch("dataimage\\turnoffpromode", f608D))
                                {
                                    flag = true;
                                    break;
                                }
                                if (ExtractTexts(text2).Count == 1)
                                {
                                    _client.ElementWithAttributes(ExtractTexts(text2).First(), 1, text2);
                                    continue;
                                }
                                SetStatusAccount(accountId, text + "Scroll...");
                                if (ScrollScreen(-1))
                                {
                                    switch (Login())
                                    {
                                        default:
                                            goto end_IL_004d;
                                        case 0:
                                            continue;
                                        case 1:
                                            break;
                                    }
                                    goto IL_0242;
                                }
                            }
                            else
                            {
                                SetStatusAccount(accountId, text + "Tap " + Regex.Match(text3, "'(.*?)'").Groups[1].Value + "...");
                                _client.ElementWithAttributes(text3, 1, text2);
                                WaitForPostComplete(10);
                            }
                        }
                        else
                        {
                            if (_client.ElementWithAttributes("//*[starts-with(@text,'Professional mode') or starts-with(@content-desc,'Professional mode')]", 1, "", false))
                            {
                                flag = true;
                                break;
                            }
                            SetStatusAccount(accountId, text + "Tap " + Regex.Match(text3, "'(.*?)'").Groups[1].Value + "...");
                            _client.ElementWithAttributes(text3, 1, text2);
                        }
                    }
                    break;
                IL_0242:;
                }
            end_IL_004d:;
            }
            catch
            {
            }
            return flag ? 1 : 0;
        }
        public int HDNghiGiaiLao(int accountId, string statusPrefix, JsonHelper settings, string actionName)
        {
            SetStatusAccount(accountId, statusPrefix + string.Format("Đang {0}, đợi {time}s...", actionName), SubdyHelper.RandomValue(settings.GetIntType("nudDelayFrom"), settings.GetIntType("nudDelayTo") + 1));
            return 1;
        }
        public int HDSpamNewfeed(int accountId, string statusPrefix, JsonHelper settings, string actionName)
        {
            int num = settings.GetIntType("nudSoLuongBaiVietFrom");
            int num2 = settings.GetIntType("nudSoLuongBaiVietTo");
            int f = settings.GetIntType("nudDelayFrom");
            int fC = settings.GetIntType("nudDelayTo");
            bool bool_ = settings.GetBooleanValue("ckbInteract");
            string c23CDF0B = settings.GetValue("typeReaction");
            bool bool_2 = settings.GetBooleanValue("ckbComment");
            List<string> f1808BA = settings.GetValuesList("txtComment", settings.GetIntType("typeNganCach"));
            bool bool_3 = settings.GetBooleanValue("ckbAnh");
            string string_ = settings.GetValue("txtPathAnh");
            f1808BA = SubdyHelper.CloneList(f1808BA);
            try
            {
                string dE812B2A = statusPrefix + "Đang" + " " + actionName + ": ";
                OpenFacebookTimeline();
                _client.Delay(2);
                ScrollFeedAndInteract(accountId, dE812B2A, num, num2, bool_, c23CDF0B, num, num2, bool_2, num, num2, f1808BA, enableShare: false, 0, 0, 1, bool_3, string_, f, fC);
            }
            catch
            {
            }
            return 0;
        }
        public async Task<int> HDUpAvatar(JsonHelper settings, ScriptAction action)
        {
            int targetCount = 1;
            int delayFrom = settings.GetIntType("nudKhoangCachFrom", 5);
            int delayTo = settings.GetIntType("nudKhoangCachTo", 10);

            int successCount = 0;
            int failCount = 0;

            while (!_mainService._ct.IsCancellationRequested)
            {
                if (successCount >= targetCount) break;

                int result = await HDUpAvatarOld(0, "", settings, action.Name);
                if (result == 1)
                {
                    successCount++;
                }
                else
                {
                    failCount++;
                }

                if (successCount < targetCount)
                {
                    int delay = SubdyHelper.RandomValue(delayFrom, delayTo + 1);
                    await Task.Delay(delay * 1000);
                }
            }

            return successCount;
        }

        public async Task<int> HDUpAvatarOld(int accountId, string statusPrefix, JsonHelper settings, string actionName)
        {
            bool isSuccess = false;
            string folderPath = settings.GetValue("txtPathFolder");
            bool shouldDeleteUsedPhoto = settings.GetBooleanValue("ckbXoaAnhDaDung");

            if (Directory.GetFiles(folderPath).Length != 0)
            {
                string status = statusPrefix + "Đang" + " " + actionName + ": ";
                SetStatusAccount(accountId, status + "Đang chạy...");
                try
                {
                    int photoDisabledCount = 0;
                    int maxPhotoDisabled = 3;
                    int tapToRetryCount = 0;
                    int maxTapToRetry = 6;

                    while (OpenFacebookLink(accountId, status, "fb://profile_edit"))
                    {
                        string photoPath = "";
                        if (shouldDeleteUsedPhoto)
                        {
                            lock (object_2)
                            {
                                photoPath = Directory.GetFiles(folderPath)
                                    .OrderBy(_ => Guid.NewGuid())
                                    .FirstOrDefault();
                                if (string.IsNullOrEmpty(photoPath))
                                {
                                    break;
                                }
                                UploadMediaFiles(new List<string> { photoPath });
                                SubdyHelper.DeleteFile(photoPath);
                                goto SelectPhoto;
                            }
                        }
                        photoPath = Directory.GetFiles(folderPath)
                            .OrderBy(_ => Guid.NewGuid())
                            .FirstOrDefault();

                        if (!string.IsNullOrEmpty(photoPath))
                        {
                            UploadMediaFiles(new List<string> { photoPath });
                            goto SelectPhoto;
                        }
                        break;

                    SelectPhoto:
                        string xmlSource = "";
                        int startTick = Environment.TickCount;
                        bool photoSelected = false;
                        do
                        {
                            xmlSource = _client.GetXMLSource();
                            string foundXPath = _client.FindElement(xmlSource, new List<string> {
                        "//*[@class='android.widget.ProgressBar']",
                        "//*[@text='Tap to retry']",
                        "//*[@content-desc='Profile picture, Button']",
                        "//*[@text='ALLOW' or @content-desc='ALLOW']",
                        "//*[@text='SAVE' or @content-desc='SAVE']",
                        "//*[@content-desc='Photo']",
                        "(//*[contains(@content-desc, 'Photo taken') or contains(@text, 'Photo taken')])[1]",
                        "//*[@content-desc='Photo. Disabled.']"
                    }, 1);

                            if (foundXPath == "//*[@content-desc='Photo. Disabled.']")
                            {
                                if (photoDisabledCount >= maxPhotoDisabled)
                                {
                                    goto EndMethod;
                                }
                                photoDisabledCount++;
                                _client.ElementWithAttributes("//*[@content-desc='Back']", 1, xmlSource);
                                goto WaitLoop;
                            }
                            else if (foundXPath == "//*[@content-desc='Profile picture, Button']")
                            {
                                goto SaveAvatar;
                            }
                            else if (foundXPath == "//*[@text='ALLOW' or @content-desc='ALLOW']" ||
                                     foundXPath == "//*[@text='SAVE' or @content-desc='SAVE']")
                            {
                                goto SaveAvatar;
                            }
                            else if (foundXPath == "//*[@class='android.widget.ProgressBar']")
                            {
                                SetStatusAccount(accountId, status + "Loading...");
                            }
                            else if (foundXPath == "//*[@text='Tap to retry']")
                            {
                                if (tapToRetryCount >= maxTapToRetry)
                                {
                                    break;
                                }
                                tapToRetryCount++;
                                ScrollScreen(-1);
                            }
                            else if (foundXPath == "(//*[contains(@content-desc, 'Photo taken') or contains(@text, 'Photo taken')])[1]")
                            {
                                var photoElements = _client.FindBounds("", foundXPath, 1);
                                if (photoElements.Count > 1)
                                {
                                    photoElements = photoElements.GetRange(0, photoElements.Count - 1);
                                }
                                string selectedPhoto = photoElements.OrderBy(_ => Guid.NewGuid()).FirstOrDefault();
                                var point = new RectangleArea(selectedPhoto).GetCenterPoint();
                                _client.Click(point.X, point.Y);
                                photoSelected = true;
                            }
                            else
                            {
                                if (_client.ElementWithAttributes("//*[@text='CAMERA ROLL' or @content-desc='CAMERA ROLL']", 1, xmlSource, false) &&
                                    !_client.ElementWithAttributes("//*[@content-desc='Live camera']", 1, xmlSource, false))
                                {
                                    _client.ElementWithAttributes("//*[@content-desc='Back']", 5, xmlSource);
                                }
                                else
                                {
                                    await _mainService._facebookService.HanderAccount(_client, _account, 5, _mainService._ct, _mainService);
                                }
                            }
                            goto WaitLoop;

                        SaveAvatar:
                            if (!(foundXPath == "//*[@content-desc='Profile picture, Button']" && photoSelected))
                            {
                                SetStatusAccount(accountId, status + "Tap " + foundXPath + "...");
                                _client.ElementWithAttributes(foundXPath, 1, xmlSource);
                                goto WaitLoop;
                            }
                            isSuccess = true;
                            break;

                        WaitLoop:
                            _client.Delay(2);
                            continue;
                        }
                        while (Environment.TickCount - startTick < 300000);
                        break;
                    EndMethod:;
                    }
                }
                catch
                {
                    // Optionally log error here
                }
            }
            return isSuccess ? 1 : 0;
        }

        /// <summary>
        /// Hành động "Kháng spam" — gỡ checkpoint 282 qua trình duyệt Chrome.
        /// Luồng: đọc cấu hình -> yêu cầu key cap.guru -> consume-and-delete 1 ảnh kháng nghị
        /// -> inject cookie Chrome -> mở m.facebook.com trong Chrome -> giải captcha (nếu có)
        /// -> drive picker upload ảnh -> đợi nút "Gửi" enabled -> tap Gửi -> xác nhận
        /// "đã gửi đơn kháng nghị". Cơ chế thiết bị nặng nằm trong <see cref="KhangSpam282"/>
        /// (file riêng, using System.Drawing/Microsoft.Data.Sqlite gọn, tránh đụng file 14k dòng này).
        /// Trả về 1 khi thành công, 0 khi lỗi mềm (không throw -> không phá vòng lặp account).
        /// </summary>
        public async Task<int> HDKhangSpam(JsonHelper settings, ScriptAction action)
        {
            string status = $"Đang {action.Name}: ";
            Action<string, int, string> report = (msg, color, log) => _mainService.SetStatus(msg, color, log);

            // (1) Đọc cấu hình folder ảnh (default H:\anhgo282) + delay.
            string folderPath = settings.GetValue("txtPathFolder", @"H:\anhgo282");
            if (string.IsNullOrWhiteSpace(folderPath)) folderPath = @"H:\anhgo282";

            // (2) BẮT BUỘC key cap.guru (user chốt "Yêu cầu cấu hình cap.guru"). Rỗng -> báo lỗi mềm, return.
            string captchaKey = _mainService._settingGeneral.GetValuesFromInputString("textBoxCaptchaKey");
            if (string.IsNullOrWhiteSpace(captchaKey))
            {
                SetStatusAccount(0, status + "Chưa cấu hình key captcha (cap.guru) trong Cài đặt chung!");
                _mainService.SetStatus(status + "Chưa cấu hình key captcha (cap.guru).", 3);
                return 0;
            }

            if (!Directory.Exists(folderPath))
            {
                SetStatusAccount(0, status + $"Không tìm thấy thư mục ảnh: {folderPath}");
                return 0;
            }

            try
            {
                // (3) Consume-and-delete 1 ảnh ngẫu nhiên (user chốt: lấy bất kỳ, dùng xong xoá ngay tránh nhầm ảnh).
                string photoPath = "";
                string deviceFileName = "";
                string deviceRemotePath = "";
                lock (object_2)
                {
                    photoPath = Directory.GetFiles(folderPath)
                        .Where(f => !f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                        .OrderBy(_ => Guid.NewGuid())
                        .FirstOrDefault();
                    if (string.IsNullOrEmpty(photoPath))
                    {
                        SetStatusAccount(0, status + "Thư mục ảnh kháng nghị rỗng.");
                        return 0;
                    }
                    // Đẩy ảnh lên /sdcard/pictures + broadcast MediaStore (dùng row broadcast).
                    // Giữ tên file TRÊN DEVICE để luồng picker tap đúng tile trong DocumentsUI.
                    var pushed = UploadMediaFiles(new List<string> { photoPath });
                    deviceFileName = pushed != null && pushed.Count > 0 ? Path.GetFileName(pushed[0]) : "";
                    deviceRemotePath = pushed != null && pushed.Count > 0 ? pushed[0] : "";
                    // Xoá file nguồn NGAY sau khi dùng (consume-and-delete).
                    SubdyHelper.DeleteFile(photoPath);
                }

                // (4) Inject cookie Chrome (login bằng cookie đã lưu của TÀI KHOẢN ĐANG CHẠY).
                // Cookie lấy từ _account.Cookie = cột cookie CỦA ĐÚNG nick này trong DB (KHÔNG phải
                // cookie test hard-code). Truyền _account.Uid để InjectChromeCookies đối chiếu c_user
                // và CẢNH BÁO nếu cookie trong DB là cookie cũ/test của nick khác (user 2026-09-15 #2).
                SetStatusAccount(0, status + "Đang đăng nhập Chrome bằng cookie...");
                if (!KhangSpam282.InjectChromeCookies(_client, _account.Cookie, report, _account.Uid))
                {
                    SetStatusAccount(0, status + "Đăng nhập Chrome thất bại (inject cookie).");
                    return 0;
                }

                // (5) Mở Facebook trong Chrome. Tài khoản đang bị 282 sẽ tự redirect sang màn checkpoint.
                SetStatusAccount(0, status + "Đang mở checkpoint trong Chrome...");
                KhangSpam282.OpenFacebookInChrome(_client, "https://m.facebook.com/");
                _client.Delay(6);

                // (6) Chạy luồng kháng nghị: captcha -> picker upload -> Gửi -> xác nhận.
                // Truyền deviceFileName để picker tap ĐÚNG tile (text = tên file đã push).
                // RunAppealFlow trả AppealOutcome (KHÔNG bool) để caller phân biệt 3 nhánh ghi chú:
                //   Success   -> up ảnh + gửi đơn THÀNH CÔNG
                //   CookieDie -> cookie chết, hiện màn đăng nhập, KHÔNG vào được bước up ảnh
                //   Failed    -> timeout 360s / captcha sai nhiều / picker lỗi...
                SetStatusAccount(0, status + "Đang xử lý màn kháng nghị 282...");
                AppealOutcome outcome = AppealOutcome.Failed;
                try
                {
                    outcome = await KhangSpam282.RunAppealFlow(_client, captchaKey, report, deviceFileName);
                }
                finally
                {
                    // (7) Dọn ảnh KHÁNG NGHỊ TRÊN ĐIỆN THOẠI sau khi kháng xong (thành công hay thất bại).
                    // Nếu không xoá, mỗi lần kháng đẩy thêm 1 ảnh vào /sdcard/pictures -> đầy bộ nhớ device.
                    CleanupAppealImageOnDevice(deviceRemotePath, deviceFileName, report);
                }

                if (outcome == AppealOutcome.Success)
                {
                    SetStatusAccount(0, status + "Đã gửi đơn kháng nghị 282, chờ FB review.");
                    _mainService.SetStatus(status + "Đã gửi đơn kháng nghị 282.", 0);

                    // (REQ 2 — user 2026-09-15 #2) Acc up ảnh kháng THÀNH CÔNG -> ghi chú
                    // "đã up ảnh thành công" để user lọc/dò lại kết quả từng nick.
                    SetAccountNoteSafe("đã up ảnh thành công");

                    // (REQ 1 — user 2026-09-15 #1) CHUYỂN bước xoá toàn bộ Chrome (như mới)
                    // XUỐNG CUỐI: SAU khi up ảnh kháng thành công -> delay 10s -> pm clear Chrome.
                    // Trước đây pm clear chạy ở ĐẦU InjectChromeCookies (gây First-Run mỗi lần +
                    // rủi ro kẹt); giờ để cuối: vừa sạch Chrome sẵn cho nick kế, vừa không phá luồng
                    // đang chạy. Delay 10s theo đúng yêu cầu để FB kịp ghi nhận đơn trước khi wipe.
                    SetStatusAccount(0, status + "Up ảnh thành công, chờ 10s rồi xoá sạch Chrome...");
                    _client.Delay(10);
                    KhangSpam282.FullResetChrome(_client);

                    return 1;
                }

                if (outcome == AppealOutcome.CookieDie)
                {
                    // (REQ 3 — user 2026-09-15 #3) Cookie CHẾT, không login được -> KHÔNG vào
                    // được bước up ảnh. Ghi chú "cookie die" để user lọc acc. Bỏ qua acc này:
                    // vòng account tự advance sang nick khác (GetAccount đã lấy acc khỏi pool).
                    SetAccountNoteSafe("cookie die");
                    SetStatusAccount(0, status + "Cookie die, không vào được màn up ảnh — bỏ qua acc này.");
                    _mainService.SetStatus(status + "Cookie die, bỏ qua acc.", 3);
                    return 0;
                }

                SetStatusAccount(0, status + "Không hoàn tất được đơn kháng nghị 282.");
                _mainService.SetStatus(status + "Không hoàn tất được đơn kháng nghị 282.", 3);
                return 0;
            }
            catch (Exception ex)
            {
                _mainService.SetStatus(status + $"Lỗi: {ex.Message}", 3, $"[HDKhangSpam] {ex}");
                return 0;
            }
        }

        /// <summary>
        /// Ghi chú tài khoản đang chạy (user 2026-09-15 #2/#3): "đã up ảnh thành công" / "cookie die".
        /// Theo đúng pattern UpdateStoryStats: set _account.Note rồi persist AccountContext.Update,
        /// bọc try/catch để lỗi DB KHÔNG phá vòng account. _account là CÙNG object MainService giữ
        /// nên `finally` của MainService (_accountContext.Update(_account)) cũng mang theo Note này.
        /// </summary>
        private void SetAccountNoteSafe(string note)
        {
            try
            {
                if (_account == null) return;
                _account.Note = note;
                new AccountContext().Update(_account);
            }
            catch
            {
            }
        }

        /// <summary>
        /// Xoá ảnh kháng nghị ĐÃ push lên điện thoại (/sdcard/pictures/…) sau khi luồng kháng
        /// kết thúc — cả thành công lẫn thất bại/lỗi. Nếu không xoá, mỗi lần kháng cộng thêm 1 ảnh
        /// vào device và dần đầy bộ nhớ (user yêu cầu 2026-09-15).
        /// Ngoài `rm` + broadcast MediaStore (DeleteMediaFiles), xoá luôn ROW trong MediaStore bằng
        /// `content delete` vì UploadMediaFiles đã `content insert` — nếu chỉ rm, row cũ còn sót sẽ
        /// hiện tile "0 B / Jan 1, 1970" trong picker các lần sau (bẫy MediaStore đã ghi nhận).
        /// </summary>
        private void CleanupAppealImageOnDevice(string remotePath, string deviceFileName, Action<string, int, string> report)
        {
            if (string.IsNullOrWhiteSpace(remotePath)) return;
            try
            {
                DeleteMediaFiles(new List<string> { remotePath });
                // Xoá row MediaStore trỏ tới file vừa rm để tile biến mất khỏi DocumentsUI.
                _client.Shell($"content delete --uri content://media/external/images/media " +
                              $"--where \"_data='{remotePath.Trim()}'\"");
                report($"Đã xoá ảnh kháng nghị trên điện thoại: {deviceFileName}", 2, null);
            }
            catch (Exception ex)
            {
                // Dọn dẹp thất bại KHÔNG được phá kết quả kháng nghị — chỉ log.
                report($"Không xoá được ảnh trên điện thoại ({deviceFileName}): {ex.Message}", 3,
                       $"[HDKhangSpam.Cleanup] {ex}");
            }
        }

        public int HDDangBaiPage(int accountId, string statusPrefix, JsonHelper settings, string actionName, string pageKey)
        {
            int postCountFrom = settings.GetIntType("nudSoLuongFrom", 1);
            int postCountTo = settings.GetIntType("nudSoLuongTo", 1);
            int intervalFrom = settings.GetIntType("nudKhoangCachFrom");
            int intervalTo = settings.GetIntType("nudKhoangCachTo");
            int groupType = settings.GetIntType("typeNhom");
            bool autoRemoveUid = settings.GetBooleanValue("ckbTuDongXoaUid");
            settings.GetBooleanValue("ckbChiDangNhomKKD");
            bool isTextPost = settings.GetBooleanValue("ckbVanBan");
            bool useBackground = settings.GetBooleanValue("ckbUseBackground");
            bool removeIngredientUsed = settings.GetBooleanValue("ckbXoaNguyenLieuDaDung");
            bool useHashtag = settings.GetBooleanValue("ckbHashtag");
            List<string> hashtagList = settings.GetValuesList("txtHashtag");
            int hashtagFrom = settings.GetIntType("nudSoHashtagFrom", 1);
            int hashtagTo = settings.GetIntType("nudSoHashtagTo", 1);
            bool hasPhoto = settings.GetBooleanValue("ckbAnh");
            string photoFolderPath = settings.GetValue("txtPathAnh");
            bool removePhotoUsed = settings.GetBooleanValue("ckbXoaAnhDaDang");
            int photoCountFrom = settings.GetIntType("nudSoLuongAnhFrom");
            int photoCountTo = settings.GetIntType("nudSoLuongAnhTo");
            int successCount = 0;
            try
            {
                string status = statusPrefix + "Đang" + " " + actionName + ": ";
                if (dictionary_5[pageKey].Count != 0)
                {
                    List<string> ingredients = new List<string>();
                    if (!removeIngredientUsed)
                    {
                        ingredients = SubdyHelper.CloneList(dictionary_5[pageKey]);
                    }
                    List<string> pageList = new List<string>();
                    if (groupType == 0)
                    {
                        SetStatusAccount(accountId, status + "Scan page...");
                        string pageProxy = SafeParseHelper.SafeSplit(GetFacebookTokenAndCookies(), '|', 1);
                        pageList = GetSuggestedPageIds(pageProxy, "Proxy", 30);
                    }
                    else
                    {
                        pageList = SubdyHelper.CloneList(C292E829[pageKey]);
                    }
                    if (pageList.Count != 0)
                    {
                        int targetPostCount = SubdyHelper.RandomValue(postCountFrom, postCountTo + 1);
                        string xmlSource = "";
                        string foundXPath = "";
                        string pageId = "";
                        int attemptCount = 0;
                        while (attemptCount < targetPostCount + 5 && successCount < targetPostCount && pageList.Count != 0)
                        {
                            if (groupType == 1 && autoRemoveUid)
                            {
                                lock (C292E829)
                                {
                                    if (C292E829[pageKey].Count == 0)
                                    {
                                        break;
                                    }
                                    int index = SubdyHelper.RandomValue(0, C292E829[pageKey].Count);
                                    pageId = C292E829[pageKey][index];
                                    C292E829[pageKey].RemoveAt(index);
                                    goto PageLoop;
                                }
                            }
                            if (pageList.Count != 0)
                            {
                                int index2 = SubdyHelper.RandomValue(0, pageList.Count);
                                pageId = pageList[index2];
                                pageList.RemoveAt(index2);
                                goto PageLoop;
                            }
                            break;

                        PageLoop:
                            try
                            {
                                while (true)
                                {
                                MainLoop:
                                    SetStatusAccount(accountId, status + $"({successCount + 1}/{targetPostCount}), Go to page {pageId}...");
                                    if (!OpenFacebookLink(accountId, status, "fb://page/" + pageId))
                                    {
                                        break;
                                    }
                                    bool foundPostArea = false;
                                    int startTick = Environment.TickCount;
                                    int timeoutSeconds = 300;
                                    do
                                    {
                                        xmlSource = _client.GetXMLSource();
                                        foundXPath = _client.FindElement(xmlSource, new List<string>
                                {
                                    "//*[@content-desc='Overview']", "//*[@content-desc='Create a post']", "//android.view.ViewGroup[starts-with(@content-desc, \"Write something\")]", "//android.view.ViewGroup[@content-desc='Discussion']", "//android.view.ViewGroup[@content-desc='Cancel request']", "//android.view.ViewGroup[@content-desc=\"Submit\"]", "//android.widget.EditText[@text='Help your video stand out with a title']", "//*[@content-desc='POST'][@enabled='true']", "//*[@content-desc='SHARE'][@enabled='true']", "//android.widget.EditText",
                                    "//android.view.View[@content-desc='Public']"
                                }, 1);

                                        // Replace hash-based switch with direct string match
                                        if (foundXPath == "//*[@content-desc='Create a post'" ||
                                            foundXPath == "//android.view.ViewGroup[starts-with(@content-desc, \"Write something\")]" ||
                                            foundXPath == "//android.view.ViewGroup[@content-desc='Discussion']" ||
                                            foundXPath == "//*[@content-desc='Overview'" ||
                                            foundXPath == "//android.view.View[@content-desc='Public']")
                                        {
                                            SetStatusAccount(accountId, status + "Tap " + foundXPath + "...");
                                            _client.ElementWithAttributes(foundXPath, 1, xmlSource);
                                            _client.Delay(2);
                                            continue;
                                        }
                                        else if (foundXPath == "//android.widget.EditText[@text='Help your video stand out with a title']")
                                        {
                                            _client.SendTextSlow(foundXPath, " ");
                                            _client.Delay(2);
                                            continue;
                                        }
                                        else if (foundXPath == "//android.widget.EditText")
                                        {
                                            if (isTextPost)
                                            {
                                                string postContent = "";
                                                if (!removeIngredientUsed)
                                                {
                                                    if (ingredients.Count == 0)
                                                    {
                                                        ingredients = SubdyHelper.CloneList(dictionary_5[pageKey]);
                                                    }
                                                    postContent = ingredients.OrderBy(_ => Guid.NewGuid()).FirstOrDefault();
                                                    ingredients.Remove(postContent);
                                                }
                                                else
                                                {
                                                    lock (dictionary_5)
                                                    {
                                                        if (dictionary_5[pageKey].Count != 0)
                                                        {
                                                            int idx = SubdyHelper.RandomValue(0, dictionary_5[pageKey].Count);
                                                            postContent = dictionary_5[pageKey][idx];
                                                            dictionary_5[pageKey].RemoveAt(idx);
                                                        }
                                                    }
                                                }
                                                if (useHashtag)
                                                {
                                                    postContent += "\n";
                                                    hashtagList = settings.GetValuesList("txtHashtag");
                                                    int hashtagCount = SubdyHelper.RandomValue(hashtagFrom, hashtagTo);
                                                    for (int l = 0; l < hashtagCount; l++)
                                                    {
                                                        if (hashtagList.Count == 0)
                                                        {
                                                            break;
                                                        }
                                                        string hashtag = hashtagList.OrderBy(_ => Guid.NewGuid()).First();
                                                        hashtagList.Remove(hashtag);
                                                        postContent = postContent + hashtag + " ";
                                                    }
                                                    postContent = postContent.Trim();
                                                }
                                                postContent = SubdyHelper.SpinText(postContent);
                                                if (postContent.Trim() != "")
                                                {
                                                    _client.Delay(1);
                                                    SetStatusAccount(accountId, status + $"({successCount + 1}/{targetPostCount}), " + "Nhập dữ liệu...");
                                                    _client.SendTextSlow("//android.widget.EditText", postContent);
                                                    _client.Delay(1);
                                                    _client.Shell("input keyenvet 62");
                                                    _client.Delay(1);
                                                    if (useBackground)
                                                    {
                                                        SetStatusAccount(accountId, status + $"({successCount + 1}/{targetPostCount}), Tap Background...");
                                                        var backgrounds = _client.FindBounds("", "//android.view.ViewGroup[contains(@content-desc,\", background\")]");
                                                        if (backgrounds.Count > 2)
                                                        {
                                                            backgrounds.RemoveAt(backgrounds.Count - 1);
                                                            var point = new RectangleArea(SubdyHelper.GetStringRandom(backgrounds)).GetCenterPoint();
                                                            _client.Click(point.X, point.Y);
                                                        }
                                                    }
                                                }
                                            }
                                            if (hasPhoto)
                                            {
                                                int photoCount = SubdyHelper.RandomValue(photoCountFrom, photoCountTo + 1);
                                                if (UploadMedia(accountId, status + $"({successCount + 1}/{targetPostCount}), ", _client, photoFolderPath, photoCount, removePhotoUsed))
                                                {
                                                    _client.ElementWithAttributes("//android.view.ViewGroup[@content-desc='Photo/video']");
                                                    for (int i = 0; i < 10; i++)
                                                    {
                                                        xmlSource = _client.GetXMLSource();
                                                        foundXPath = _client.FindElement(xmlSource, new List<string> { "//android.widget.Button[@text='ALLOW']", "//android.view.ViewGroup[@content-desc='Allow access']", "//android.view.ViewGroup[@content-desc='Choose layout']", "//android.view.ViewGroup/android.view.ViewGroup[@content-desc='Photo' or @content-desc='Video']" }, 1);
                                                        if (!(foundXPath == "//android.view.ViewGroup[@content-desc='Choose layout']") && !(foundXPath == "//android.view.ViewGroup/android.view.ViewGroup[@content-desc='Photo' or @content-desc='Video']"))
                                                        {
                                                            if (foundXPath != "")
                                                            {
                                                                SetStatusAccount(accountId, status + "Tap " + foundXPath + "...");
                                                                _client.ElementWithAttributes(foundXPath, 1, xmlSource);
                                                            }
                                                            else
                                                            {
                                                                ScrollScreen(-1);
                                                            }
                                                            _client.Delay(1);
                                                            continue;
                                                        }
                                                        if (!_client.ElementWithAttributes("//android.view.ViewGroup/android.view.ViewGroup[@content-desc='Photo' or @content-desc='Video']", 1, xmlSource, false))
                                                        {
                                                            break;
                                                        }
                                                        if (_client.ElementWithAttributes("//android.view.ViewGroup[@content-desc='Select multiple']", 1, xmlSource))
                                                        {
                                                            _client.Delay(2);
                                                        }
                                                        var photoElements = _client.FindBounds("", "//android.view.ViewGroup/android.view.ViewGroup[@content-desc='Photo' or @content-desc='Video']/parent::*[@selected='false']", 1);
                                                        for (int j = 0; j < 5; j++)
                                                        {
                                                            for (int k = 0; k < photoCount; k++)
                                                            {
                                                                while (photoElements.Count == 0)
                                                                {
                                                                    SetStatusAccount(accountId, status + "Scroll...");
                                                                    if (ScrollScreen())
                                                                    {
                                                                        break;
                                                                    }
                                                                    photoElements = _client.FindBounds("", "//android.view.ViewGroup/android.view.ViewGroup[@content-desc='Photo' or @content-desc='Video']/parent::*[@selected='false']", 1);
                                                                }
                                                                if (photoElements.Count == 0)
                                                                {
                                                                    break;
                                                                }
                                                                string selectedPhoto = photoElements.OrderBy(_ => Guid.NewGuid()).FirstOrDefault();
                                                                photoElements.Remove(selectedPhoto);
                                                                var point = new RectangleArea(selectedPhoto).GetCenterPoint();
                                                                _client.Click(point.X, point.Y);
                                                            }
                                                            if (photoElements.Count == 0 || _client.FindBounds("", "//android.view.ViewGroup/android.view.ViewGroup[@content-desc='Photo' or @content-desc='Video']/parent::*[@selected='true']", 1).Count >= photoCount)
                                                            {
                                                                break;
                                                            }
                                                        }
                                                        _client.ElementWithAttributes("//android.view.ViewGroup[@content-desc='NEXT']", 1, "");
                                                        break;
                                                    }
                                                }
                                            }
                                            _client.Delay(2);
                                            continue;
                                        }
                                        else if (foundXPath == "//*[@content-desc='POST'][@enabled='true']" ||
                                                 foundXPath == "//*[@content-desc='SHARE'][@enabled='true']")
                                        {
                                            SetStatusAccount(accountId, status + "Tap " + foundXPath + "...");
                                            _client.ElementWithAttributes(foundXPath, 1, xmlSource);
                                            _client.Delay(2);
                                            _client.ElementWithAttributes(foundXPath, 10, "");
                                            SetStatusAccount(accountId, status + $"({successCount + 1}/{targetPostCount}), Tap Share, " + "đợi {time}s...", SubdyHelper.RandomValue(3, 6));
                                            SetStatusAccount(accountId, status + $"({successCount + 1}/{targetPostCount}), " + "đợi post success...");
                                            ScrollScreen(-1);
                                            _client.Delay(2);
                                            if (WaitForPostComplete(hasPhoto ? 300 : 60))
                                            {
                                                successCount++;
                                                if (successCount < targetPostCount)
                                                {
                                                    SetStatusAccount(accountId, status + $"({successCount + 1}/{targetPostCount}), " + "đợi {time}s...", SubdyHelper.RandomValue(intervalFrom, intervalTo + 1));
                                                    break;
                                                }
                                                return successCount;
                                            }
                                            break;
                                        }
                                        else if (foundXPath == "//android.view.ViewGroup[@content-desc='Cancel request']" ||
                                                 foundXPath == "//android.view.ViewGroup[@content-desc=\"Submit\"]")
                                        {
                                            break;
                                        }
                                        if (ContainsAnyKeyword(xmlSource, "This content isn't available", "When this happens, it's usually because the owner only shared it with a small group of people, changed who can see it or it's been deleted.", "Reload page"))
                                        {
                                            break;
                                        }
                                        SetStatusAccount(accountId, status + $"({successCount + 1}/{targetPostCount}), Scroll...");
                                        bool scrolled = false;
                                        if ((!foundPostArea) ? ScrollScreen(-1) : ScrollScreen())
                                        {
                                            switch (Login())
                                            {
                                                case 1:
                                                    break;
                                                case 0:
                                                    goto MainLoop;
                                                default:
                                                    return successCount;
                                            }
                                            continue;
                                        }
                                        _client.Delay(2);
                                        continue;
                                    }
                                    while (Environment.TickCount - startTick < timeoutSeconds * 1000);
                                    break;
                                }
                            }
                            catch
                            {
                            }
                            attemptCount++;
                        }
                    }
                }
            }
            catch
            {
            }
            return successCount;
        }
        public int HDKetBanTheoTuKhoa(int accountId, string statusPrefix, JsonHelper settings, string actionName)
        {
            List<string> keywords = settings.GetValuesList("txtTuKhoa");
            int requestCountFrom = settings.GetIntType("nudSoLuongFrom");
            int requestCountTo = settings.GetIntType("nudSoLuongTo");
            int delayFrom = settings.GetIntType("nudDelayFrom");
            int delayTo = settings.GetIntType("nudDelayTo");
            int perKeywordFrom = settings.GetIntType("nudSoLuongKetBanMoiTuKhoaFrom", 1);
            int perKeywordTo = settings.GetIntType("nudSoLuongKetBanMoiTuKhoaTo", 2);
            int successCount = 0;
            try
            {
                string status = statusPrefix + "đợi " + actionName + ": ";
                int targetCount = SubdyHelper.RandomValue(requestCountFrom, requestCountTo + 1);
                if (targetCount != 0)
                {
                    keywords = SubdyHelper.CloneList(keywords); // Shuffle list
                    while (keywords.Count > 0)
                    {
                        string keyword = SubdyHelper.GetStringRandom(keywords);
                        keywords.Remove(keyword);
                        keyword = SubdyHelper.SpinText(keyword);
                        int sentPerKeyword = 0;
                        int perKeywordTarget = SubdyHelper.RandomValue(perKeywordFrom, perKeywordTo + 1);
                        while (true)
                        {
                        RestartSearch:
                            SetStatusAccount(accountId, status + "Search People...");
                            if (!SearchOnFacebook(keyword, "People"))
                            {
                                break;
                            }
                            switch (Login())
                            {
                                case 0:
                                    {
                                        SetStatusAccount(accountId, status + "Find Add Friend...");
                                        List<string> addFriendElements = _client.FindBounds("", new List<string> {
                                    "//androidx.recyclerview.widget.RecyclerView/android.view.ViewGroup/android.view.ViewGroup/child::*[2]",
                                    "//androidx.recyclerview.widget.RecyclerView/android.view.ViewGroup/android.view.ViewGroup/android.view.ViewGroup/child::*[3]",
                                    "//android.view.ViewGroup[@content-desc=\"Add Friend\"]"
                                }, 5);
                                        addFriendElements = _client.FilterElementsByMaxLeftCoordinate(addFriendElements);
                                        SetStatusAccount(accountId, status + "Search People: " + addFriendElements.Count);
                                        for (int i = 0; i < perKeywordTarget + 10; i++)
                                        {
                                            switch (Login())
                                            {
                                                case 0:
                                                    break;
                                                case 1:
                                                    goto RestartSearch;
                                                default:
                                                    goto EndMethod;
                                            }

                                            if (addFriendElements.Count == 0)
                                            {
                                                for (int j = 0; j < 5; j++)
                                                {
                                                    SetStatusAccount(accountId, status + $"({successCount}/{targetCount}), Scroll...");
                                                    if (ScrollScreen(1, 2))
                                                    {
                                                        break;
                                                    }
                                                    SetStatusAccount(accountId, status + $"({successCount}/{targetCount}), Find Add Friend...");
                                                    addFriendElements = _client.FindBounds("", new List<string> {
                                                "//androidx.recyclerview.widget.RecyclerView/android.view.ViewGroup/android.view.ViewGroup/child::*[2]",
                                                "//androidx.recyclerview.widget.RecyclerView/android.view.ViewGroup/android.view.ViewGroup/android.view.ViewGroup/child::*[3]",
                                                "//android.view.ViewGroup[@content-desc=\"Add Friend\"]"
                                            }, 5);
                                                    addFriendElements = _client.FilterElementsByMaxLeftCoordinate(addFriendElements);
                                                    SetStatusAccount(accountId, status + $"({successCount}/{targetCount}), Find Add Friend: " + addFriendElements.Count);
                                                    if (addFriendElements.Count > 0)
                                                    {
                                                        break;
                                                    }
                                                }
                                                if (addFriendElements.Count == 0)
                                                {
                                                    break;
                                                }
                                            }
                                            string addFriendXPath = SubdyHelper.GetStringRandom(addFriendElements);
                                            addFriendElements.Remove(addFriendXPath);
                                            if (string.IsNullOrEmpty(addFriendXPath))
                                            {
                                                continue;
                                            }
                                            SetStatusAccount(accountId, status + $"({successCount}/{targetCount}), Tap Friend...");
                                            var point = new RectangleArea(addFriendXPath).GetCenterPoint();
                                            if (!_client.Click(point.X, point.Y))
                                            {
                                                continue;
                                            }
                                            successCount++;
                                            if (successCount < targetCount)
                                            {
                                                sentPerKeyword++;
                                                if (sentPerKeyword >= perKeywordTarget)
                                                {
                                                    break;
                                                }
                                                SetStatusAccount(accountId, status + $"({successCount}/{targetCount}), " + "đợi {time}s...", SubdyHelper.RandomValue(delayFrom, delayTo + 1));
                                                continue;
                                            }
                                            goto EndMethod;
                                        }
                                        break;
                                    }
                                case 1:
                                    continue;
                                default:
                                    goto EndMethod;
                            }
                            goto FinishKeyword;
                        }
                        break;
                    FinishKeyword:;
                    }
                }
            EndMethod:;
            }
            catch
            {
                successCount = -1;
            }
            return successCount;
        }
        private async Task<int> HDDocThongBao(JsonHelper settings, ScriptAction action)
        {
            int notifyCountFrom = settings.GetIntType("nudSoLuongFrom");
            int notifyCountTo = settings.GetIntType("nudSoLuongTo");
            int delayFrom = settings.GetIntType("nudDelayFrom");
            int delayTo = settings.GetIntType("nudDelayTo");
            bool readRandom = settings.GetBooleanValue("ckbReadRandom", true);
            int readCount = 0;
            int targetCount = SubdyHelper.RandomValue(notifyCountFrom, notifyCountTo);
            try
            {
                string status = "";
                while (true)
                {
                RestartNotify:
                    _mainService.SetStatus(status + "Go to Notify...", 2);
                    if (!OpenNotificationTab())
                    {
                        break;
                    }
                    List<string> notifications = new List<string>();
                    for (; readCount < targetCount; _mainService.SetStatus(status + $"({readCount}/{targetCount}), Back...", 2), _client.Shell("input keyevent 4"))
                    {
                        string selectedNotify;
                        await _mainService._facebookService.HanderAccount(_client, _account, 3, _mainService._ct, _mainService);
                        switch (Login())
                        {
                            case 0:
                                _mainService.SetStatus(status + $"({readCount}/{targetCount}), Check notification list...", 2);
                                if (notifications.Count != 0)
                                {
                                    goto SelectNotify;
                                }
                                if (readCount <= 0)
                                {
                                    goto FetchNotify;
                                }
                                if (!ScrollScreen(1, 2))
                                {
                                    _client.Delay(3);
                                    goto FetchNotify;
                                }
                                goto EndMethod;
                            case 1:
                                break;
                            default:
                                goto EndMethod;
                            SelectNotify:
                                selectedNotify = "";
                                selectedNotify = (!readRandom ? notifications.FirstOrDefault() : SubdyHelper.GetStringRandom(notifications));
                                notifications.Remove(selectedNotify);
                                _mainService.SetStatus(status + $"({readCount}/{targetCount}), Tap Notify...", 2);
                                var point = new RectangleArea(selectedNotify).GetCenterPoint();
                                _client.Click(point.X, point.Y);
                                await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(2, 4), status + $"({readCount}/{targetCount}), Tap Notify, " + "đợi {time}s...", 2);
                                readCount++;
                                _mainService.SetStatus(status + $"({readCount}/{targetCount}), Scroll...", 2);
                                await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(delayFrom, delayTo), status + $"({readCount}/{targetCount}), Scroll, " + "đợi {time}s...", 2);
                                await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(1, 3), status + $"({readCount}/{targetCount}), " + "đợi {time}s...", 2);
                                if (_client.ElementWithAttributes("//*[@text='This Was Me']") && _client.ElementWithAttributes("//*[@text='Continue']", 10, ""))
                                {
                                    _client.ElementWithAttributes("//*[@text='Done']", 10, "", false);
                                }
                                continue;
                            FetchNotify:
                                if (settings.GetBooleanValue("ckbXoaThongBaoSpam"))
                                {
                                    RemoveSpamNotifications();
                                }
                                notifications = _client.GetChildNodeValuesFromXml("", "//*[@resource-id='android:id/list']", timeoutInSeconds: 10);
                                if (notifications.Count != 0)
                                {
                                    notifications.RemoveAt(0);
                                    goto SelectNotify;
                                }
                                goto EndMethod;
                        }

                        goto RestartNotify;
                    }
                    break;
                }
            EndMethod:;
            }
            catch
            {
            }
            return readCount;
        }
        public int HDOnOff2FA(int accountId, string statusPrefix, JsonHelper settings, string actionName)
        {
            int result = 2;
            string password = "";
            if (string.IsNullOrEmpty(password))
            {
                SetStatusAccount(accountId, statusPrefix + "Không có password");
            }
            else
            {
                string status = statusPrefix + "Đang " + actionName + ": ";
                SetStatusAccount(accountId, status + "Đang chạy...");
                // Ưu tiên đọc theo control mới (radio button). Fallback sang dạng cũ typeOnOff2FA/neuDaCo2FA nếu chưa có.
                bool enable2FA = settings.GetBooleanValue("rbBat2FA");
                bool disable2FA = settings.GetBooleanValue("rbTat2FA");
                if (!enable2FA && !disable2FA)
                {
                    enable2FA = settings.GetIntType("typeOnOff2FA") == 1;
                    disable2FA = settings.GetIntType("typeOnOff2FA") == 0;
                }
                // 0 = Sẽ không bật 2FA, 1 = Giữ 2FA cũ và thêm 2FA mới, 2 = Xóa 2FA cũ và thêm 2FA mới
                int ifAlreadyHas2FA;
                if (settings.GetBooleanValue("rbXoa2FACu")) ifAlreadyHas2FA = 2;
                else if (settings.GetBooleanValue("rbGiu2FACu")) ifAlreadyHas2FA = 1;
                else if (settings.GetBooleanValue("F12647B1")) ifAlreadyHas2FA = 0;
                else ifAlreadyHas2FA = settings.GetIntType("neuDaCo2FA");
                int tapToRetryCount = 0;
                int maxTapToRetry = 6;

                while (OpenFacebookLink(accountId, status, "fb://facewebmodal/f?href=https://m.facebook.com/security/2fac/settings/"))
                {
                    string secret = "";
                    string xmlSource = "";
                    int codeErrorCount = 0;
                    int maxCodeError = 0;
                    int startTick = Environment.TickCount;

                    do
                    {
                        xmlSource = _client.GetXMLSource();
                        string foundXPath = _client.FindElement(xmlSource, new List<string> {
                    "//android.widget.ProgressBar",
                    "//*[@text='Tap to retry']",
                    "//*[@text='Turn off']",
                    "//*[@text='Enter confirmation code']",
                    "//*[@text='Enter Password']//android.widget.EditText",
                    "//*[@text='Set up app on a different device']",
                    "//*[@text='Two-factor authentication is on']",
                    "//*[@text='Use authentication app']"
                }, 1);

                        // Direct string matching for XPath
                        if (foundXPath == "//*[@text='Two-factor authentication is on']")
                        {
                            if (enable2FA)
                            {
                                if (!string.IsNullOrEmpty(secret))
                                {
                                    //  method_114(accountId, "cFa2", secret, "fa2");
                                    //update
                                    result = 1;
                                    break;
                                }
                                if (ifAlreadyHas2FA == 0)
                                {
                                    result = 4;
                                    break;
                                }
                                if (ifAlreadyHas2FA == 1 && _client.ElementWithAttributes("//*[@content-desc='Add a new app']", 1, xmlSource, false))
                                {
                                    _client.ElementWithAttributes("//*[@content-desc='Add a new app']", 1, xmlSource);
                                }
                                else if (ifAlreadyHas2FA == 2 && _client.ElementWithAttributes("//*[@text='Turn off']", 1, xmlSource, false))
                                {
                                    _client.ElementWithAttributes("//*[@text='Turn off']", 1, xmlSource);
                                }
                                else
                                {
                                    _client.ElementWithAttributes("//*[@text='Authentication app']", 1, xmlSource);
                                }
                            }
                            else if (disable2FA)
                            {
                                _client.ElementWithAttributes("//*[@text='Turn off']", 1, xmlSource);
                            }
                            goto AfterAction;
                        }
                        else if (foundXPath == "//*[@content-desc='Add a new app']")
                        {
                            goto UseAuthApp;
                        }
                        else if (foundXPath == "//*[@text='Use authentication app']")
                        {
                            goto UseAuthApp;
                        }
                        else if (foundXPath == "//*[@text='Turn off']")
                        {
                            goto UseAuthApp;
                        }
                        else if (foundXPath == "//*[@text='Enter confirmation code']")
                        {
                            if (ContainsAnyKeyword(xmlSource, "This code isn't right. Please try again"))
                            {
                                codeErrorCount++;
                                if (codeErrorCount > maxCodeError)
                                {
                                    result = 5;
                                    break;
                                }
                            }
                            string totp = FacebookHander.GetCodeTowFA(secret);
                            _client.SendTextADB("//android.widget.EditText", totp);
                            _client.Delay(1);
                            _client.ATX.Press(PressKey.Enter);
                            _client.Delay(2);
                            _client.ATX.Press(PressKey.Enter);
                            goto AfterAction;
                        }
                        else if (foundXPath == "//*[@text='Enter Password']//android.widget.EditText")
                        {
                            if (ContainsAnyKeyword(xmlSource, "The password you entered was incorrect"))
                            {
                                result = 3;
                                break;
                            }
                            _client.SendTextSlow(foundXPath, password);
                            _client.Delay(2);
                            _client.ElementWithAttributes("//*[@text='Continue']", 3, "");
                            goto AfterAction;
                        }
                        else if (foundXPath == "//*[@text='Set up app on a different device']")
                        {
                            secret = Regex.Match(xmlSource, "secret%3D(.*?)%").Groups[1].Value;
                            if (string.IsNullOrEmpty(secret))
                            {
                                secret = _client.GetAttributeValuesFromXmlNodes(xmlSource, "//*[@text='Or enter this code into your authentication app']/parent::*/child::*[last()]", "text").FirstOrDefault();
                                if (secret == null)
                                {
                                    secret = "";
                                }
                            }
                            if (!string.IsNullOrEmpty(secret))
                            {
                                ScrollScreen(1, 2);
                                _client.ElementWithAttributes("//*[@text='Continue']", 3, "");
                            }
                            goto AfterAction;
                        }
                        else if (foundXPath == "//android.widget.ProgressBar")
                        {
                            SetStatusAccount(accountId, status + "Loading...");
                        }
                        else if (foundXPath == "//*[@text='Tap to retry']")
                        {
                            if (tapToRetryCount >= maxTapToRetry)
                            {
                                break;
                            }
                            tapToRetryCount++;
                            ScrollScreen(-1);
                            goto AfterAction;
                        }
                        else
                        {
                            SetStatusAccount(accountId, status + "Scroll...");
                            if (ScrollScreen())
                            {
                                switch (Login())
                                {
                                    case 0:
                                        break;
                                    case 1:
                                        goto EndMethod;
                                    default:
                                        goto EndMethod;
                                }
                            }
                            goto AfterAction;
                        }

                    UseAuthApp:
                        if (!(foundXPath == "//*[@text='Use authentication app']" && disable2FA))
                        {
                            SetStatusAccount(accountId, status + "Tap " + foundXPath + "...");
                            _client.ElementWithAttributes(foundXPath, 1, xmlSource);
                            goto AfterAction;
                        }
                        //method_114(accountId, "cFa2", "", "fa2");
                        //update
                        result = 6;
                        break;

                    AfterAction:
                        _client.Delay(2);
                        continue;

                    }
                    while (Environment.TickCount - startTick < 300000);
                    break;

                EndMethod:;
                }
            }
            return result;
        }
        public int HDDoiTen(ref int status, int accountId, string statusPrefix, JsonHelper settings, string actionName)
        {
            string password = "";
            if (!string.IsNullOrEmpty(password))
            {
                List<string> lastNames = new List<string>();
                List<string> middleNames = new List<string>();
                List<string> firstNames = new List<string>();
                int nameType = settings.GetIntType("typeDatTen");

                if (nameType == 1)
                {
                    lastNames = settings.GetValuesList("lstHo");
                    middleNames = settings.GetValuesList("lstTenDem");
                    firstNames = settings.GetValuesList("lstTen");
                }
                else if (settings.GetIntType("typeTenRandom") == 0)
                {
                    lastNames = SubdyHelper.LastnameVN;
                    middleNames = new List<string>();
                    firstNames = SubdyHelper.FirstnameVN;
                }
                else
                {
                    lastNames = SubdyHelper.LastnameRandom;
                    middleNames = new List<string>();
                    firstNames = SubdyHelper.FirstnameRandom;
                }

                string newLastName = lastNames.OrderBy(_ => Guid.NewGuid()).First();
                string newMiddleName = middleNames.Count > 0 ? middleNames.OrderBy(_ => Guid.NewGuid()).First() : "";
                string newFirstName = firstNames.OrderBy(_ => Guid.NewGuid()).First();
                string statusText = statusPrefix + "Đang " + actionName + ": ";
                SetStatusAccount(accountId, statusText + "Đang chạy...");
                status = 2;
                int tapToRetryCount = 0;
                int maxTapToRetry = 6;

                while (OpenFacebookLink(accountId, statusText, "fb://settings"))
                {
                    string foundXPath = "";
                    int startTick = Environment.TickCount;
                    do
                    {
                        string xmlSource = _client.GetXMLSource();
                        foundXPath = _client.FindElement(xmlSource, new List<string>
                {
                    "//android.widget.ProgressBar",
                    "//*[@text='Tap to retry']",
                    "//*[@content-desc='Personal and account information' or @text='Personal information']",
                    "//*[contains(@text, \"You can't change your name on Facebook right now\")]",
                    "//*[@text='Something went wrong. We're working on getting it fixed as soon as we can.']",
                    "//*[@text='Review Change']",
                    "//*[@text='Incorrect password.']",
                    "//*[@text='Save changes']",
                    "//*[@text='Name']",
                    "//*[@text='Preview Your New Name']"
                }, 1);

                        // Direct string matching for XPath
                        switch (foundXPath)
                        {
                            case "//*[contains(@text, \"You can't change your name on Facebook right now\")]":
                                status = 4;
                                break;
                            case "//*[@content-desc='Personal and account information' or @text='Personal information']":
                            case "//*[@text='Name']":
                                if (_client.ElementWithAttributes("//*[@text='Name']", 1, xmlSource, false))
                                {
                                    foundXPath = "//*[@text='Name']";
                                }
                                _client.ElementWithAttributes(foundXPath, 1, xmlSource);
                                _client.Delay(2);
                                continue;
                            case "//*[@text='Review Change']":
                                _client.SendTextSlow("(//android.widget.EditText)[1]", newLastName);
                                _client.Delay(1);
                                _client.SendTextSlow("(//android.widget.EditText)[2]", newMiddleName);
                                _client.Delay(1);
                                _client.SendTextSlow("(//android.widget.EditText)[3]", newFirstName);
                                _client.Delay(1);
                                _client.ElementWithAttributes(foundXPath, 1, xmlSource);
                                _client.Delay(3);
                                continue;
                            case "//*[@text='Preview Your New Name']":
                                status = 1;
                                break;
                            case "//*[@text='Save changes']":
                                _client.SendTextSlow("//android.widget.EditText", password);
                                _client.Delay(1);
                                _client.ElementWithAttributes(foundXPath, 1, xmlSource);
                                _client.Delay(2);
                                continue;
                            case "//*[@text='Something went wrong. We're working on getting it fixed as soon as we can.']":
                                status = 5;
                                break;
                            case "//*[@text='Incorrect password.']":
                                status = 3;
                                break;
                            case "//android.widget.ProgressBar":
                                SetStatusAccount(accountId, statusText + "Loading...");
                                _client.Delay(2);
                                continue;
                            case "//*[@text='Tap to retry']":
                                if (tapToRetryCount >= maxTapToRetry)
                                {
                                    break;
                                }
                                tapToRetryCount++;
                                ScrollScreen(-1);
                                _client.Delay(2);
                                continue;
                            default:
                                int loginResult = Login();
                                if (loginResult == 1)
                                {
                                    break;
                                }
                                if (loginResult != 0)
                                {
                                    break;
                                }
                                _client.Delay(2);
                                continue;
                        }
                        break;
                    }
                    while (Environment.TickCount - startTick < 300000);
                    break;
                }
            }
            return 1;
        }
        public int HDRoiNhom(int accountId, string statusPrefix, JsonHelper settings, string actionName)
        {
            int groupCountFrom = settings.GetIntType("nudSoLuongFrom");
            int groupCountTo = settings.GetIntType("nudSoLuongTo");
            int delayFrom = settings.GetIntType("nudDelayFrom");
            int delayTo = settings.GetIntType("nudDelayTo");
            int leaveType = settings.GetIntType("typeRoiNhom");
            bool requireApproval = settings.GetBooleanValue("ckbDieuKienKiemDuyet");
            bool requireMemberCondition = settings.GetBooleanValue("ckbDieuKienThanhVien");
            int maxMembers = settings.GetIntType("nudThanhVienToiDa");
            bool requireKeyword = settings.GetBooleanValue("ckbDieuKienTuKhoa");
            List<string> keywords = settings.GetValuesList("txtTuKhoa");
            List<string> keepGroupIds = settings.GetValuesList("txtIDNhomGiuLai");
            int leftCount = 0;
            int targetCount = SubdyHelper.RandomValue(groupCountFrom, groupCountTo + 1);

            try
            {
                string status = statusPrefix + "Đang " + actionName + ": ";
                if (leaveType == 0 && keepGroupIds.Count == 0)
                {
                    DeplinkFacebook("fb://groups_targeted_tab");
                    if (_client.ElementWithAttributes("//*[@content-desc='Settings']", 10, ""))
                    {
                        while (true)
                        {
                            SetStatusAccount(accountId, status + $"({leftCount}/{targetCount})...");
                            bool foundLeaveGroup = false;
                            while (true)
                            {
                                string xmlSource = "";
                                string leaveXPath = _client.FindElement("", new List<string> {
                            "//*[starts-with(@content-desc,'Membership')]",
                            "//*[starts-with(@content-desc,\"You're a member of\")]",
                            "(//*[@text='LEAVE GROUP'])[last()]"
                        }, 10);
                                if (!string.IsNullOrEmpty(leaveXPath))
                                {
                                    _client.ElementWithAttributes(leaveXPath, 1, xmlSource);
                                    if (leaveXPath == "(//*[@text='LEAVE GROUP'])[last()]")
                                    {
                                        foundLeaveGroup = true;
                                        break;
                                    }
                                }
                                else
                                {
                                    if (!_client.ElementWithAttributes("//android.widget.ProgressBar", 1, "", false))
                                        break;
                                    _client.ATX.Press(PressKey.Back);
                                }
                            }
                            if (foundLeaveGroup)
                            {
                                leftCount++;
                                if (leftCount < targetCount)
                                {
                                    SetStatusAccount(accountId, status + $"({leftCount}/{targetCount}), " + "đợi {time}s...", SubdyHelper.RandomValue(delayFrom, delayTo + 1));
                                    _client.ATX.Press(PressKey.Back);
                                    continue;
                                }
                                SetStatusAccount(accountId, status + $"({leftCount}/{targetCount}), Done!");
                                break;
                            }
                            break;
                        }
                    }
                }
                else
                {
                    SetStatusAccount(accountId, status + "Scan groups...");
                    var tokenParts = GetFacebookTokenAndCookies().Split('|');
                    string fbToken = tokenParts.Length > 1 ? tokenParts[1] : "";
                    List<string> rawGroups = GetUserGroups(fbToken, "Proxy", 30, requireApproval);

                    // Filter groups according to settings
                    List<string> groups;
                    if (leaveType == 0)
                    {
                        groups = rawGroups.Select(e => SafeParseHelper.SafeSplit(e, '|', 0)).Where(s => !string.IsNullOrEmpty(s)).ToList();
                    }
                    else
                    {
                        var filteredGroups = new List<string>();
                        if (requireApproval)
                        {
                            filteredGroups.AddRange(
                                rawGroups.Where(g => { var p = g.Split('|'); return p.Length > 3 && p[3].ToLower() == "true"; })
                                         .Select(g => SafeParseHelper.SafeSplit(g, '|', 0)));
                        }
                        if (requireMemberCondition)
                        {
                            filteredGroups.AddRange(
                                rawGroups.Where(g => { var p = g.Split('|'); return p.Length > 2 && int.TryParse(p[2], out int mc) && mc < maxMembers; })
                                         .Select(g => SafeParseHelper.SafeSplit(g, '|', 0)));
                        }
                        if (requireKeyword)
                        {
                            var lowerKeywords = keywords.ConvertAll(k => k.ToLower());
                            filteredGroups.AddRange(
                                rawGroups.Where(g => SubdyHelper.ContainsAnyKeyword(g.Split('|')[1], lowerKeywords)).Select(g => g.Split('|')[0]));
                        }
                        groups = filteredGroups.Distinct().ToList();
                    }

                    if (keepGroupIds.Count > 0)
                    {
                        groups = groups.Except(keepGroupIds).ToList();
                    }

                    SetStatusAccount(accountId, status + "Scan groups: " + groups.Count);
                    while (leftCount < targetCount && groups.Count > 0)
                    {
                        string groupId = groups[SubdyHelper.RandomValue(0, groups.Count)];
                        groups.Remove(groupId);
                        while (true)
                        {
                            SetStatusAccount(accountId, status + $"({leftCount}/{targetCount}), Go to Group {groupId}...");
                            if (!OpenFacebookLink(accountId, status, "fb://group/" + groupId))
                                break;

                            SetStatusAccount(accountId, status + $"({leftCount}/{targetCount}), Rời nhóm...");

                            string xmlSource = "";
                            int tapToRetryCount = 0;
                            int maxTapToRetry = 6;
                            int startTick = Environment.TickCount;
                            do
                            {
                                xmlSource = _client.GetXMLSource();
                                string foundXPath = _client.FindElement(xmlSource, new List<string>
                        {
                            "//android.widget.ProgressBar",
                            "//*[@text='Tap to retry']",
                            "//*[@content-desc='Reload page']",
                            "//*[@content-desc='Delete Invite' or @text='Delete Invite']",
                            "//*[starts-with(@content-desc, 'joined')]",
                            "//*[starts-with(@content-desc, 'followed')]",
                            "//*[starts-with(@content-desc,'Leave ') or starts-with(@text,'Leave ')]",
                            "//*[starts-with(@content-desc,'Unfollow ') or starts-with(@text,'Unfollow ')]",
                            "//*[starts-with(@content-desc,'You have left this group')]",
                            "//*[starts-with(@content-desc, 'Join ')]",
                            "//*[@content-desc='Follow group']",
                            "//*[@content-desc='manage group']"
                        }, 1);

                                switch (foundXPath)
                                {
                                    case "//*[@content-desc='Reload page']":
                                        if (tapToRetryCount >= maxTapToRetry) break;
                                        tapToRetryCount++;
                                        SetStatusAccount(accountId, status + $"({leftCount}/{targetCount}), Tap " + foundXPath + "...");
                                        _client.ElementWithAttributes(foundXPath, 1, xmlSource);
                                        _client.Delay(2);
                                        continue;
                                    case "//*[starts-with(@content-desc,'Leave ') or starts-with(@text,'Leave ')]":
                                    case "//*[starts-with(@content-desc,'Unfollow ') or starts-with(@text,'Unfollow ')]":
                                        _client.ElementWithAttributes(foundXPath, 1, xmlSource);
                                        _client.Delay(2);
                                        continue;
                                    case "//*[starts-with(@content-desc, 'joined')]":
                                    case "//*[starts-with(@content-desc, 'followed')]":
                                    case "//*[@content-desc='Delete Invite' or @text='Delete Invite']":
                                        SetStatusAccount(accountId, status + $"({leftCount}/{targetCount}), Tap " + foundXPath + "...");
                                        _client.ElementWithAttributes(foundXPath, 1, xmlSource);
                                        _client.Delay(2);
                                        continue;
                                    case "//android.widget.ProgressBar":
                                        SetStatusAccount(accountId, status + "Loading...");
                                        _client.Delay(2);
                                        continue;
                                    case "//*[@text='Tap to retry']":
                                        if (tapToRetryCount >= maxTapToRetry) break;
                                        tapToRetryCount++;
                                        ScrollScreen(-1);
                                        _client.Delay(2);
                                        continue;
                                    case "//*[starts-with(@content-desc,'You have left this group')]":
                                    case "//*[starts-with(@content-desc, 'Join ')]":
                                    case "//*[@content-desc='Follow group']":
                                    case "//*[@content-desc='manage group']":
                                        break;
                                    default:
                                        if (!ContainsAnyKeyword(xmlSource, "An error occurred leaving this group, please try again"))
                                        {
                                            SetStatusAccount(accountId, status + $"({leftCount}/{targetCount}), Scroll...");
                                            if (!ScrollScreen())
                                                break;
                                            int loginResult = Login();
                                            if (loginResult == 1) continue;
                                            if (loginResult != 0) break;
                                        }
                                        _client.Delay(2);
                                        continue;
                                }
                                break;
                            }
                            while (Environment.TickCount - startTick < 60000);

                            leftCount++;
                            if (leftCount < targetCount)
                            {
                                SetStatusAccount(accountId, status + $"({leftCount}/{targetCount}), " + "đợi {time}s...", SubdyHelper.RandomValue(delayFrom, delayTo + 1));
                                continue;
                            }
                            SetStatusAccount(accountId, status + $"({leftCount}/{targetCount}), Done!");
                            break;
                        }
                    }
                }
            }
            catch
            {
            }
            return leftCount;
        }
        public int HDBuffLikePage(int accountId, string statusPrefix, JsonHelper settings, string actionName)
        {
            JsonHelper f72FAFBC = new JsonHelper();
            f72FAFBC.AddValue("id", (object)settings.GetValue("txtUid"));
            f72FAFBC.AddValue("isLikePage", true);
            string string_ = statusPrefix + "Đang " + actionName + ": ";
            return ProcessPageAction(accountId, string_, f72FAFBC).isSuccess ? 1 : 0;
        }
        public int HDTaoNhom(int accountId, string statusPrefix, JsonHelper settings, string actionName)
        {
            string statusMessage = statusPrefix + "Đang " + actionName + ": ";
            SetStatusAccount(accountId, statusMessage + "Đang chạy...");

            List<string> groupNames = settings.GetValuesList("txtTenNhom");
            int groupCountFrom = settings.GetIntType("nudSoLuongFrom");
            int groupCountTo = settings.GetIntType("nudSoLuongTo");
            int totalGroupsToCreate = SubdyHelper.RandomValue(groupCountFrom, groupCountTo);
            int createdCount = 0;
            int retryCount = 0;
            int maxRetry = 6;

            while (true)
            {
            RestartGroupCreation:
                SetStatusAccount(accountId, statusMessage + $"({createdCount + 1}/{totalGroupsToCreate})...");
                DeplinkFacebook("fb://groups_targeted_tab");
                _client.Delay(2);
                string xmlSource = "";
                int startTick = Environment.TickCount;
                while (true)
                {
                    xmlSource = _client.GetXMLSource();
                    string foundXPath = _client.FindElement(xmlSource, new List<string>
            {
                "//android.widget.ProgressBar",
                "//*[@text='Tap to retry']",
                "//*[@content-desc='Your groups']",
                "//android.widget.EditText[@text='Name your group']",
                "//*[starts-with(@content-desc,'Public, Anyone can see')]",
                "//*[@content-desc='Create group'][@clickable='true']",
                "//*[@text='Invite members']",
                "//*[@content-desc='Open create options']",
                "//*[@content-desc='Close create options']/parent::*/child::*[1]",
                "//*[contains(@content-desc,'Create a Group')]"
            }, 1);

                    switch (foundXPath)
                    {
                        case "//android.widget.ProgressBar":
                            SetStatusAccount(accountId, statusMessage + "Loading...");
                            goto WaitDelay;
                        case "//*[@text='Tap to retry']":
                            if (retryCount >= maxRetry)
                                goto ReturnCreatedCount;
                            retryCount++;
                            ScrollScreen(-1);
                            goto WaitDelay;
                        case "//*[@content-desc='Your groups']":
                            if (_client.ElementWithAttributes("//*[@content-desc='Open create options']", 1, xmlSource, false))
                                foundXPath = "//*[@content-desc='Open create options']";
                            else if (_client.ElementWithAttributes("//*[@content-desc='Create actions entry point']", 1, xmlSource, false))
                                foundXPath = "//*[@content-desc='Create actions entry point']";
                            else if (_client.ElementWithAttributes("//*[@content-desc='Create group']", 1, xmlSource, false))
                                foundXPath = "//*[@content-desc='Create group']";

                            SetStatusAccount(accountId, statusMessage + "Tap " + foundXPath + "...");
                            _client.ElementWithAttributes(foundXPath, 1, xmlSource);
                            goto WaitDelay;
                        case "//android.widget.EditText[@text='Name your group']":
                            string groupName = SubdyHelper.ReplaceWithRandom(SubdyHelper.GetStringRandom(groupNames), (2));
                            _client.SendTextSlow(foundXPath, groupName);
                            _client.Delay(1);
                            _client.ElementWithAttributes("//*[@content-desc='Choose privacy']", 1, xmlSource);
                            goto WaitDelay;
                        case "//*[starts-with(@content-desc,'Public, Anyone can see')]":
                            SetStatusAccount(accountId, statusMessage + "Tap " + foundXPath + "...");
                            _client.ElementWithAttributes(foundXPath, 1, xmlSource);
                            _client.Delay(1);
                            _client.ElementWithAttributes("//*[starts-with(@content-desc,'Done')]", 1, xmlSource);
                            goto WaitDelay;
                        case "//*[@content-desc='Create group'][@clickable='true']":
                        case "//*[@content-desc='Open create options']":
                        case "//*[@content-desc='Close create options']/parent::*/child::*[1]":
                        case "//*[contains(@content-desc,'Create a Group')]":
                            SetStatusAccount(accountId, statusMessage + "Tap " + foundXPath + "...");
                            _client.ElementWithAttributes(foundXPath, 1, xmlSource);
                            goto WaitDelay;
                        case "//*[@text='Invite members']":
                            createdCount++;
                            if (createdCount < totalGroupsToCreate)
                                break; // Tạo tiếp nhóm mới
                            goto ReturnCreatedCount;
                        default:
                            SetStatusAccount(accountId, statusMessage + "Scroll...");
                            if (ScrollScreen())
                            {
                                switch (Login())
                                {
                                    case 0:
                                        break;
                                    case 1:
                                        goto RestartGroupCreation;
                                    default:
                                        goto ReturnCreatedCount;
                                }
                            }
                            goto WaitDelay;
                    }
                    break;

                WaitDelay:
                    _client.Delay(2);
                    if (Environment.TickCount - startTick < 300000)
                        continue;
                    goto ReturnCreatedCount;
                }
            }

        ReturnCreatedCount:
            return createdCount;
        }
        public async Task<int> HDTuongTacEvent(JsonHelper settings, ScriptAction action)
        {
            int count = SubdyHelper.RandomValue(settings.GetIntType("nudTuKhoaFrom"), settings.GetIntType("nudTuKhoaTo"));
            var eventLinks = settings.GetValuesList("txtLinks");
            bool doInterested = settings.GetBooleanValue("ckbQuanTam");
            bool doJoin = settings.GetBooleanValue("ckbThamGia");
            bool doInviteFriends = settings.GetBooleanValue("ckbMoiBanBe");
            int inviteFrom = settings.GetIntType("nudMoiBanBeFrom", 1);
            int inviteTo = settings.GetIntType("nudMoiBanBeTo", 1);
            int delayFrom = settings.GetIntType("nudDelayFrom", 3);
            int delayTo = settings.GetIntType("nudDelayTo", 5);
            int invitedCount = 0;
            string type = "DeXuat";
            if (settings.GetBooleanValue("ckbChiDinh"))
            {
                type = "ChiDinh";
            }
            switch (type)
            {
                case "DeXuat":
                    DeplinkFacebook("fb://events");
                    string xpath = "//node[contains(@class,'android.view.ViewGroup') and @content-desc and string-length(@text) > 0 and @visible-to-user='true'" +
                             "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'tab'))" +
                             "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'create'))" +
                              "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'search'))" +
                              "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'going'))" +
                               "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'interested'))" +
                             "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'events'))]";
                    var nodes = _client.FindElementsNotToLower(15, "", xpath);
                    if (!nodes.Any())
                    {
                        throw new SubdyExtension(SubdyEnum.JobFail, "Không tìm được events đề xuất");
                    }
                    break;
                case "ChiDinh":
                    eventLinks = SubdyHelper.CloneList(eventLinks);
                    if (eventLinks.Count == 0)
                    {
                        throw new SubdyExtension(SubdyEnum.JobFail, "Vui lòng nhập link sự kiện");
                    }
                    break;
            }
            int countTarget = 1;
            Dictionary<string, string> keyValues = new Dictionary<string, string>();
            List<string> old = new List<string>();
            while (!_mainService._ct.IsCancellationRequested)
            {
                // [TIME-LIMIT v21] Ép giới hạn thời gian NGAY TRONG hành động tương tác sự kiện (mỗi vòng):
                // quá timeoutTaiKhoan/timeoutKichBan -> break -> ExecuteAsync đổi tài khoản. KHÔNG đụng logic FB.
                if (Stop()) break;
                if (countTarget >= count)
                {
                    break;
                }
                switch (type)
                {
                    case "DeXuat":
                        {
                            if (!keyValues.Any())
                            {
                                string xpath = "//node[contains(@class,'android.view.ViewGroup') and @content-desc and string-length(@text) > 0 and @visible-to-user='true'" +
                             "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'tab'))" +
                             "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'create'))" +
                              "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'search'))" +
                              "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'going'))" +
                               "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'interested'))" +
                             "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'events'))]";
                                var nodes = _client.FindElementsNotToLower(15, "", xpath);
                                if (!nodes.Any())
                                {
                                    throw new SubdyExtension(SubdyEnum.JobFail, "Không tìm được page đã thích");
                                }
                                bool allExist = true;
                                foreach (var node in nodes)
                                {
                                    var info = _client.ExtractNodeInfo(node.OuterXml);
                                    if (!old.Contains(info["content-desc"]))
                                    {
                                        allExist = false;
                                        keyValues.Add(info["content-desc"], info["bounds"]);
                                    }
                                    old.Add(info["content-desc"]);
                                }
                                if (allExist)
                                {
                                    ScrollScreen(1, 1);
                                    countTarget++;
                                    continue;
                                }
                            }

                            if (!keyValues.Any())
                            {
                                countTarget = count + 1;
                                continue;
                            }
                            var firt = keyValues.First();
                            var point = new RectangleArea(firt.Value).GetCenterPoint();
                            _client.ADB.Shell($"input tap {point.X} {point.Y}");
                            await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(5, 10), $"({countTarget}/{count}) Go to event {firt.Key}" + ", đợi {time}s...", 2);
                            keyValues.Remove(firt.Key);
                            break;
                        }
                    case "ChiDinh":
                        {
                            if (!eventLinks.Any())
                            {
                                countTarget = count + 1;
                                continue;
                            }
                            var firt = SubdyHelper.GetStringRandom(eventLinks);
                            if (!DeplinkFacebook(firt).Contains("IntentUriHandler"))
                            {
                                continue;
                            }
                            await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(5, 10), $"({countTarget}/{count}) Go to event {firt}" + ", đợi {time}s...", 2);

                            eventLinks.Remove(firt);
                            break;
                        }
                }
                int inviteTarget = SubdyHelper.RandomValue(inviteFrom, inviteTo + 1);

                try
                {

                    int retry = 0;
                    int maxRetry = 3;
                    int startTick = Environment.TickCount;
                    int maxSeconds = 300;

                    while (!_mainService._ct.IsCancellationRequested)
                    {
                        string xmlSource = _client.GetXMLSource();
                        string foundXPath = _client.FindElement(xmlSource, new List<string> {
                    "//*[@content-desc='Event Ended']",
                    "//*[@content-desc='Interested']",
                    "//*[@content-desc='Invite']",
                    "//*[@content-desc='Add event']"
                }, 1);

                        switch (foundXPath)
                        {
                            default:
                                if (_client.ElementWithAttributes("//android.widget.ProgressBar", 1, xmlSource, false))
                                {
                                    _mainService.SetStatus("Loading...", 2);
                                }
                                else if (_client.ElementWithAttributes("//*[@text='Tap to retry']", 1, xmlSource, false))
                                {
                                    if (retry >= maxRetry)
                                        break;
                                    retry++;
                                    ScrollScreen(-1);
                                }
                                else
                                {
                                    if (ScrollScreen())
                                    {
                                        maxSeconds = 1;
                                        break;
                                    }
                                }
                                goto AfterAction;
                            case "//*[@content-desc='Add event']":
                                _client.ATX.Press(PressKey.Back);
                                goto AfterAction;
                            case "//*[@content-desc='Invite']":
                                _mainService.SetStatus($"Invite ({invitedCount + 1}/{inviteTarget})...", 2);
                                _client.ElementWithAttributes(foundXPath, 1, xmlSource);
                                invitedCount++;
                                await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(delayFrom, delayTo + 1), $"Invite ({invitedCount}/{inviteTarget}), delay {{time}}s...", 2);
                                if (invitedCount > inviteTarget)
                                {
                                    _mainService.SetStatus($"Invite ({invitedCount + 1}/{inviteTarget}) done!", 2);
                                    break;
                                }
                                goto AfterAction;
                            //case "//*[@content-desc='Share']":
                            //    _mainService.SetStatus("Tap " + foundXPath + "...", 2);
                            //    _client.ElementWithAttributes(foundXPath, 1, xmlSource);
                            //    goto AfterAction;
                            case "//*[@content-desc='Interested']":
                                if (doInterested)
                                {
                                    _mainService.SetStatus("Quan tâm...", 2);
                                    _client.ElementWithAttributes(foundXPath, 1, xmlSource);
                                    _client.Delay(2);
                                    doInterested = false;
                                }
                                if (doJoin)
                                {
                                    _mainService.SetStatus("Tham gia...", 2);
                                    _client.ElementWithAttributes("//*[@content-desc='Going']", 1, xmlSource);
                                    _client.Delay(2);
                                    doJoin = false;
                                }
                                if (doInviteFriends)
                                {
                                    _client.ElementWithAttributes("//*[@content-desc='More']", 1, xmlSource);
                                    goto AfterAction;
                                }
                                break;
                        }
                        break;
                    AfterAction:
                        if (Environment.TickCount - startTick <= maxSeconds * 1000)
                        {
                            goto ContinueLoop;
                        }
                        break;
                    ContinueLoop:
                        _client.Delay(1);
                    }
                    _client.ATX.Press(PressKey.Back);
                    countTarget++;
                }
                catch
                {
                }
            }


            return invitedCount;
        }
        public async Task<int> HDThamGiaNhom(JsonHelper settings, ScriptAction action)
        {
            var keywords = settings.GetValuesList("txtLinks");
            int minGroups = settings.GetIntType("nudSoLuongFrom");
            int maxGroups = settings.GetIntType("nudSoLuongTo");
            int delayFrom = settings.GetIntType("nudDelayFrom");
            int delayTo = settings.GetIntType("nudDelayTo");
            bool autoAnswerQuestions = settings.GetBooleanValue("ckbTuDongTraLoiCauHoi");
            var answerList = settings.GetValuesList("txtCauTraLoi");
            int joinedCount = 1;
            string note = string.Empty;
            string type = "Dexuat";
            if (settings.GetBooleanValue("ckbChiDinh"))
            {
                if (!keywords.Any())
                {
                    throw new SubdyExtension(SubdyEnum.JobFail, "Không có uid groups");
                }
                type = "ChiDinh";
            }
            else if (settings.GetBooleanValue("ckbTuKhoa"))
            {
                if (!keywords.Any())
                {
                    throw new SubdyExtension(SubdyEnum.JobFail, "Không có keywords");
                }
                type = "TuKhoa";
                note = SubdyHelper.GetStringRandom(keywords);
                _mainService.SetStatus($"Search group {note}...", 2);
                SearchOnFacebook(note, "Groups");
            }
            else
            {
                DeplinkFacebook("fb://faceweb/f?href=https://m.facebook.com/groups_browse/your_groups/");
                _client.ElementWithAttributes("//*[@content-desc=\"Discover\"]", 20);
            }
            int totalCount = SubdyHelper.RandomValue(minGroups, maxGroups);
            int refail = 0;
            while (!_mainService._ct.IsCancellationRequested)
            {
                // [TIME-LIMIT v21] Ép giới hạn thời gian NGAY TRONG hành động tham gia nhóm (mỗi vòng):
                // quá timeoutTaiKhoan/timeoutKichBan -> break -> ExecuteAsync đổi tài khoản. KHÔNG đụng logic FB.
                if (Stop()) break;
                if (joinedCount > totalCount || refail > 5) break;
                switch (type)
                {
                    default:
                        {
                            string xpath =
                                "//node[contains(@class,'android.widget.Button') " +
                                "and contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'join') " +
                                "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'create a group')) " +
                                "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'sort groups')) " +
                                "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'your groups')) " +
                                "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'posts')) " +
                                "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'discover')) " +
                                "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'manage')) " +
                                "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'most visited'))]";
                            var groups = _client.FindElementsNotToLower(20, "", xpath);
                            if (!groups.Any())
                            {
                                ScrollScreen(1, 2);
                                refail++;
                                continue;
                            }
                            refail = 0;
                            foreach (var group in groups)
                            {
                                if (joinedCount > totalCount || refail > 5) break;
                                var info = _client.ExtractNodeInfo(group.OuterXml);
                                var point = new RectangleArea(info["bounds"]).RandomPoint();
                                _client.ADB.Shell($"input tap {point.X} {point.Y}");
                                await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(delayFrom, delayTo), $"{joinedCount}/{totalCount} {info["content-desc"]}" + ", đợi {time}s...", 2);
                                joinedCount++;
                            }
                            ScrollScreen(1, 2);
                            refail++;
                            break;
                        }
                    case "TuKhoa":
                        {
                            string xpath =
                                "//node[contains(@class,'android.view.View') " +
                                "and contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'join')]";
                            var groups = _client.FindElementsNotToLower(20, "", xpath);
                            if (!groups.Any())
                            {
                                ScrollScreen(1, 1);
                                refail++;
                                continue;
                            }
                            refail = 0;
                            foreach (var group in groups)
                            {
                                if (joinedCount > totalCount || refail > 5) break;
                                var info = _client.ExtractNodeInfo(group.OuterXml);
                                _client.ElementWithAttributes("//*[@content-desc=\"Join\"]", 1, group.OuterXml);
                                await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(delayFrom, delayTo), $"{joinedCount}/{totalCount} {info["content-desc"]}" + ", đợi {time}s...", 2);
                                joinedCount++;
                            }
                            ScrollScreen(1, 1);
                            refail++;
                            break;
                        }
                    case "ChiDinh":
                        {
                            if (!keywords.Any())
                            {
                                joinedCount = totalCount + 1;
                                continue;
                            }
                            string uid = SubdyHelper.GetStringRandom(keywords);
                            keywords.Remove(uid);
                            if (string.IsNullOrEmpty(uid))
                            {
                                continue;
                            }
                            DeplinkFacebook($"fb://group/{uid}");
                            List<string> xpaths = new List<string>
                            {
                              "//node[contains(@class,'android.widget.Button') and contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'joined')]",
                              "//node[contains(@class,'android.widget.Button') and contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'join')]",
                            };
                            var xpath = _client.FindElement("", xpaths, 15);
                            if (string.IsNullOrEmpty(xpath)) continue;
                            if (xpath == "//node[contains(@class,'android.widget.Button') and contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'joined')]")
                            {
                                await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(delayFrom, delayTo), $"{joinedCount}/{totalCount} Đã tham gia group {uid} trước đó" + ", đợi {time}s...", 2);
                                continue;
                            }
                            else if (xpath == "//node[contains(@class,'android.widget.Button') and contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'join')]")
                            {
                                _client.ElementWithAttributes(xpath);
                                await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(delayFrom, delayTo), $"{joinedCount}/{totalCount} Đã tham gia group {uid}" + ", đợi {time}s...", 2);
                                joinedCount++;
                            }


                            break;
                        }
                }
            }

            return joinedCount;
        }
        public int HDMoiBanBeLikePage(int accountId, string statusPrefix, JsonHelper settings, string actionName)
        {
            int result = 0;
            try
            {
                string statusText = statusPrefix + "Đang " + actionName + ": ";
                List<string> uidList = settings.GetValuesList("txtUid");
                int minCount = settings.GetIntType("nudSoLuongFrom");
                int maxCount = settings.GetIntType("nudSoLuongTo");
                string pageUid = uidList[accountId % uidList.Count];
                int inviteCount = SubdyHelper.RandomValue(minCount, maxCount);
                SetStatusAccount(accountId, statusText + "Go to Page " + pageUid + "...");

                if (pageUid.StartsWith("1000"))
                {
                    // Profile page
                    if (OpenFacebookLink(accountId, statusText, "fb://profile/" + pageUid) &&
                        _client.ElementWithAttributes("//*[@content-desc='More']", 10, ""))
                    {
                        _client.Delay(2);
                        if (_client.FindAndClickImage("dataimage/invitefriends", null, 10))
                        {
                            _client.Delay(2);
                            if (_client.FindAndClickImage("dataimage/selectall", null, 10))
                            {
                                _client.Delay(2);
                                if (_client.FindAndClickImage("dataimage/sendinvites", null, 10))
                                {
                                    goto AfterInvite;
                                }
                            }
                        }
                    }
                }
                else if (OpenFacebookLink(accountId, statusText, "fb://page/" + pageUid + "/invite_friends_to_like_page"))
                {
                    SetStatusAccount(accountId, statusText + "Find checkbox...");
                    List<string> checkboxList = _client.FindBounds("", "//android.widget.CheckBox[@checked='false']", 1);
                    SetStatusAccount(accountId, statusText + "Find checkbox: " + checkboxList.Count);
                    if (checkboxList.Count != 0)
                    {
                        for (int i = 0; i < inviteCount; i++)
                        {
                            if (checkboxList.Count > 1)
                            {
                                string checkbox = checkboxList.First();
                                checkboxList.Remove(checkbox);
                                SetStatusAccount(accountId, statusText + $"{i + 1}/{inviteCount}, Tap checkbox...");
                                var point = new RectangleArea(checkbox).GetCenterPoint();
                                _client.Click(point.X, point.Y);
                                _client.Delay(1, 2);
                                continue;
                            }
                            SetStatusAccount(accountId, statusText + $"{i + 1}/{inviteCount}, Scroll...");
                            if (ScrollScreen())
                            {
                                break;
                            }
                            i--;
                            SetStatusAccount(accountId, statusText + $"{i + 1}/{inviteCount}, Find checkbox...");
                            checkboxList = _client.FindBounds("", "//android.widget.CheckBox[@checked='false']", 1);
                        }
                        SetStatusAccount(accountId, statusText + "Tap Invite...");
                        if (_client.ElementWithAttributes("//android.widget.ImageView[@content-desc=\"Invite selected friends\"]", 3, ""))
                        {
                            goto AfterInvite;
                        }
                    }
                }
                goto EndMethod;

            AfterInvite:
                SetStatusAccount(accountId, statusText + "Tap Invite, " + "đợi {time}s...", 3);
                SetStatusAccount(accountId, statusText + "Loading...");
                WaitForPostComplete(5);

            EndMethod:;
            }
            catch
            {
            }
            return result;
        }
        public int HDCapNhatThongTin(ref string updatedFields, int accountId, string statusPrefix, JsonHelper settings, string actionName)
        {
            // Flags for which fields to update
            bool updateBio = settings.GetBooleanValue("ckbBio");
            bool updateWork = settings.GetBooleanValue("ckbWork");
            bool updateHighSchool = settings.GetBooleanValue("ckbHighSchool");
            bool updateCollege = settings.GetBooleanValue("ckbCollege");
            bool updateCurrentCity = settings.GetBooleanValue("ckbCurrentCity");
            bool updateHometown = settings.GetBooleanValue("ckbHometown");
            bool updateRelationship = settings.GetBooleanValue("ckbRelationship");
            bool updateGender = settings.GetBooleanValue("ckbGender");
            bool updateBirthday = settings.GetBooleanValue("ckbBirthday");

            // Values for profile fields
            List<string> bioList = settings.GetValuesList("txtBio", settings.GetIntType("typeSplitBio"));
            List<string> workList = settings.GetValuesList("lstWork");
            List<string> highSchoolList = settings.GetValuesList("lstHighSchool");
            List<string> collegeList = settings.GetValuesList("lstCollege");
            List<string> currentCityList = settings.GetValuesList("lstCurrentCity");
            List<string> hometownList = settings.GetValuesList("lstHometown");
            string relationshipValue = SubdyHelper.GetStringRandom(settings.GetValue("cbbRelationship").Split('|').ToList());
            bool skipWhenHave = settings.GetBooleanValue("ckbSkipWhenHave");
            string genderValue = SubdyHelper.GetStringRandom(settings.GetValue("cbbGender").Split('|').ToList());
            List<string> dayList = settings.GetValuesList("lstDay");
            List<string> monthList = settings.GetValuesList("lstMonth");
            List<string> yearList = settings.GetValuesList("lstYear");

            string statusText = statusPrefix + "Đang " + actionName + ": ";
            SetStatusAccount(accountId, statusText + "Đang chạy...");

            // Update Bio
            if (updateBio)
            {
                int retry = 0, maxRetry = 6;
                SetStatusAccount(accountId, statusText + "Cập nhật Bio...");
                while (OpenFacebookLink(accountId, statusText, "fb://profile_edit"))
                {
                    bool hasChanged = false;
                    string xml = "";
                    int startTick = Environment.TickCount;
                    while (true)
                    {
                        xml = _client.GetXMLSource();
                        string found = _client.FindElement(xml, new List<string> {
                    "//android.widget.ProgressBar", "//*[@text='Tap to retry']",
                    "//*[contains(@content-desc, 'Tap to edit bio')]",
                    "//android.widget.EditText", "//*[@text='Edit']", "//*[@content-desc='Edit Bio']"
                }, 1);
                        switch (found)
                        {
                            case "//*[contains(@content-desc, 'Tap to edit bio')]":
                            case "//*[@text='Edit']":
                            case "//*[@content-desc='Edit Bio']":
                                if ((!skipWhenHave || found != "//*[@content-desc='Edit Bio']") && !hasChanged)
                                {
                                    if (found == "//*[@text='Edit']")
                                    {
                                        var rect = RectangleArea.FindOverlap(
                                            _client.FindBounds(xml, "//*[@text='Bio']", 1).First(),
                                            _client.FindBounds(xml, found, 1)
                                        );
                                        if (rect != null)
                                            _client.Click(rect.GetCenterPoint().X, rect.GetCenterPoint().Y);
                                    }
                                    else
                                    {
                                        _client.ElementWithAttributes(found, 1, xml);
                                    }
                                    goto AfterAction;
                                }
                                goto EndBio;
                            case "//android.widget.ProgressBar":
                                SetStatusAccount(accountId, statusText + "Loading...");
                                break;
                            case "//*[@text='Tap to retry']":
                                if (retry >= maxRetry) break;
                                retry++;
                                ScrollScreen(-1);
                                break;
                            case "//android.widget.EditText":
                                string bio = SubdyHelper.SpinText(bioList.OrderBy(_ => Guid.NewGuid()).First());
                                _client.SendTextSlow(found, bio);
                                _client.Delay(3);
                                _client.ElementWithAttributes("//*[@text='Save' or @content-desc='Save']", 1, xml);
                                hasChanged = true;
                                updatedFields = "Bio-";
                                break;
                            default:
                                int loginResult = Login();
                                if (loginResult == 1) break;
                                if (loginResult != 0) break;
                                break;
                        }
                    AfterAction:
                        _client.Delay(2);
                        if (Environment.TickCount - startTick < 300000) continue;
                        break;
                    }
                }
            EndBio:;
            }

            // Update Work
            if (updateWork)
            {
                int retry = 0, maxRetry = 6;
                SetStatusAccount(accountId, statusText + "Cập nhật Work...");
                while (ContainsAnyKeyword("", "Edit Profile", "Edit Details") ||
                    OpenFacebookLink(accountId, statusText, "fb://profile_edit"))
                {
                    bool hasChanged = false;
                    string xml = "";
                    int startTick = Environment.TickCount;
                    while (true)
                    {
                        xml = _client.GetXMLSource();
                        string found = _client.FindElement(xml, new List<string> {
                    "//android.widget.ProgressBar", "//*[@text='Tap to retry']", "//*[@text='Edit']",
                    "//*[@text='Add']", "//*[@text='Add work']", "//*[@text='Workplace Name']",
                    "//android.widget.EditText[@text='Select workplace']", "//*[@text='Edit Details']",
                    "//*[@text='Save' or @content-desc='Save'][@enabled='true']"
                }, 1);
                        switch (found)
                        {
                            case "//*[@text='Save' or @content-desc='Save'][@enabled='true']":
                                _client.ElementWithAttributes(found, 1, xml);
                                updatedFields += "Work-";
                                hasChanged = true;
                                goto AfterWork;
                            case "//android.widget.EditText[@text='Select workplace']":
                                _client.SendTextSlow(found, workList.OrderBy(_ => Guid.NewGuid()).First());
                                _client.Delay(2);
                                var workOptions = _client.FindBounds("", "//android.widget.ScrollView/android.view.ViewGroup/android.view.ViewGroup", 10);
                                if (workOptions.Count > 0)
                                {
                                    if (workOptions.Count == 1 && ContainsAnyKeyword("", "No Results Found"))
                                        goto EndWork;
                                    var point = new RectangleArea(workOptions.OrderBy(_ => Guid.NewGuid()).First()).GetCenterPoint();
                                    _client.Click(point.X, point.Y);
                                }
                                goto AfterWork;
                            case "//*[@text='Add work']":
                            case "//*[@text='Edit']":
                            case "//*[@text='Add']":
                                if ((!skipWhenHave || found != "//*[@text='Add work']" || !_client.ElementWithAttributes("//*[starts-with(@text,'Works at')]", 1, xml, false)) && !hasChanged)
                                {
                                    var rect = RectangleArea.FindOverlap(
                                        _client.FindBounds(xml, "//*[@text='Details']", 1).FirstOrDefault(),
                                        _client.FindBounds(xml, found, 1)
                                    );
                                    if (rect != null)
                                        _client.Click(rect.GetCenterPoint().X, rect.GetCenterPoint().Y);
                                    else
                                        ScrollScreen();
                                }
                                goto AfterWork;
                            case "//*[@text='Tap to retry']":
                                if (retry >= maxRetry) break;
                                retry++;
                                ScrollScreen(-1);
                                break;
                            case "//android.widget.ProgressBar":
                                SetStatusAccount(accountId, statusText + "Loading...");
                                break;
                            default:
                                if (!ScrollScreen()) goto AfterWork;
                                int loginResult = Login();
                                if (loginResult == 1) continue;
                                if (loginResult != 0) break;
                                break;
                        }
                    AfterWork:
                        _client.Delay(2);
                        if (Environment.TickCount - startTick < 300000) continue;
                        break;
                    }
                }
            EndWork:;
            }

            // Update High School
            if (updateHighSchool)
            {
                int retry = 0, maxRetry = 6;
                SetStatusAccount(accountId, statusText + "Cập nhật High School...");
                while (ContainsAnyKeyword("", "Edit Profile", "Edit Details") ||
                    OpenFacebookLink(accountId, statusText, "fb://profile_edit"))
                {
                    bool hasChanged = false;
                    string xml = "";
                    int startTick = Environment.TickCount;
                    while (true)
                    {
                        xml = _client.GetXMLSource();
                        string found = _client.FindElement(xml, new List<string> {
                    "//android.widget.ProgressBar", "//*[@text='Tap to retry']", "//*[@text='Edit']",
                    "//*[@text='Add']", "//*[@text='High School Name (Required)']",
                    "//*[@text='Add a high school' or @text='Add high school']",
                    "//android.widget.EditText[@text='Select school']", "//*[@text='Edit Details']",
                    "//*[@text='Save' or @content-desc='Save'][@enabled='true']"
                }, 1);
                        switch (found)
                        {
                            case "//*[@text='Save' or @content-desc='Save'][@enabled='true']":
                                _client.ElementWithAttributes(found, 1, xml);
                                updatedFields += "HighSchool-";
                                hasChanged = true;
                                goto AfterHighSchool;
                            case "//android.widget.EditText[@text='Select school']":
                                _client.SendTextSlow(found, highSchoolList.OrderBy(_ => Guid.NewGuid()).First());
                                _client.Delay(2);
                                var hsOptions = _client.FindBounds("", "//android.widget.ScrollView/android.view.ViewGroup/android.view.ViewGroup", 10);
                                if (hsOptions.Count > 0)
                                {
                                    if (hsOptions.Count == 1 && ContainsAnyKeyword("", "No Results Found"))
                                        goto EndHighSchool;
                                    var point = new RectangleArea(hsOptions.OrderBy(_ => Guid.NewGuid()).First()).GetCenterPoint();
                                    _client.Click(point.X, point.Y);
                                }
                                goto AfterHighSchool;
                            case "//*[@text='Add a high school' or @text='Add high school']":
                            case "//*[@text='Edit']":
                            case "//*[@text='Add']":
                                if ((!skipWhenHave || found != "//*[@text='Add a high school' or @text='Add high school']" || !_client.ElementWithAttributes("//*[starts-with(@text,'Studied at')]", 1, xml, false)) && !hasChanged)
                                {
                                    var rect = RectangleArea.FindOverlap(
                                        _client.FindBounds(xml, "//*[@text='Details']", 1).FirstOrDefault(),
                                        _client.FindBounds(xml, found, 1)
                                    );
                                    if (rect != null)
                                        _client.Click(rect.GetCenterPoint().X, rect.GetCenterPoint().Y);
                                    else
                                        ScrollScreen();
                                }
                                goto AfterHighSchool;
                            case "//*[@text='Tap to retry']":
                                if (retry >= maxRetry) break;
                                retry++;
                                ScrollScreen(-1);
                                break;
                            case "//android.widget.ProgressBar":
                                SetStatusAccount(accountId, statusText + "Loading...");
                                break;
                            default:
                                if (!ScrollScreen()) goto AfterHighSchool;
                                int loginResult = Login();
                                if (loginResult == 1) continue;
                                if (loginResult != 0) break;
                                break;
                        }
                    AfterHighSchool:
                        _client.Delay(2);
                        if (Environment.TickCount - startTick < 300000) continue;
                        break;
                    }
                }
            EndHighSchool:;
            }

            // Update College
            if (updateCollege)
            {
                int retry = 0, maxRetry = 6;
                SetStatusAccount(accountId, statusText + "Cập nhật College...");
                while (ContainsAnyKeyword("", "Edit Profile", "Edit Details") ||
                    OpenFacebookLink(accountId, statusText, "fb://profile_edit"))
                {
                    bool hasChanged = false;
                    string xml = "";
                    int startTick = Environment.TickCount;
                    while (true)
                    {
                        xml = _client.GetXMLSource();
                        string found = _client.FindElement(xml, new List<string> {
                    "//android.widget.ProgressBar", "//*[@text='Tap to retry']", "//*[@text='Edit']",
                    "//*[@text='Add']", "//*[@text='College Name (Required)']",
                    "//*[@text='Add a college' or @text='Add college']",
                    "//android.widget.EditText[@text='Select school']", "//*[@text='Edit Details']",
                    "//*[@text='Save' or @content-desc='Save'][@enabled='true']"
                }, 1);
                        switch (found)
                        {
                            case "//*[@text='Save' or @content-desc='Save'][@enabled='true']":
                                _client.ElementWithAttributes(found, 1, xml);
                                updatedFields += "College-";
                                hasChanged = true;
                                goto AfterCollege;
                            case "//android.widget.EditText[@text='Select school']":
                                _client.SendTextSlow(found, collegeList.OrderBy(_ => Guid.NewGuid()).First());
                                _client.Delay(2);
                                var collegeOptions = _client.FindBounds("", "//android.widget.ScrollView/android.view.ViewGroup/android.view.ViewGroup", 10);
                                if (collegeOptions.Count > 0)
                                {
                                    if (collegeOptions.Count == 1 && ContainsAnyKeyword("", "No Results Found"))
                                        goto EndCollege;
                                    var point = new RectangleArea(collegeOptions.OrderBy(_ => Guid.NewGuid()).First()).GetCenterPoint();
                                    _client.Click(point.X, point.Y);
                                }
                                goto AfterCollege;
                            case "//*[@text='Add a college' or @text='Add college']":
                            case "//*[@text='Edit']":
                            case "//*[@text='Add']":
                                if ((!skipWhenHave || found != "//*[@text='Add a college' or @text='Add college']" || !_client.ElementWithAttributes("//*[starts-with(@text,'Studied at')]", 1, "", false)) && !hasChanged)
                                {
                                    var rect = RectangleArea.FindOverlap(
                                        _client.FindBounds(xml, "//*[@text='Details']", 1).FirstOrDefault(),
                                        _client.FindBounds(xml, found, 1)
                                    );
                                    if (rect != null)
                                        _client.Click(rect.GetCenterPoint().X, rect.GetCenterPoint().Y);
                                    else
                                        ScrollScreen();
                                }
                                goto AfterCollege;
                            case "//*[@text='Tap to retry']":
                                if (retry >= maxRetry) break;
                                retry++;
                                ScrollScreen(-1);
                                break;
                            case "//android.widget.ProgressBar":
                                SetStatusAccount(accountId, statusText + "Loading...");
                                break;
                            default:
                                if (!ScrollScreen()) goto AfterCollege;
                                int loginResult = Login();
                                if (loginResult == 1) continue;
                                if (loginResult != 0) break;
                                break;
                        }
                    AfterCollege:
                        _client.Delay(2);
                        if (Environment.TickCount - startTick < 300000) continue;
                        break;
                    }
                }
            EndCollege:;
            }

            // Update Current City
            if (updateCurrentCity)
            {
                int retry = 0, maxRetry = 6;
                SetStatusAccount(accountId, statusText + "Cập nhật Current City...");
                while (ContainsAnyKeyword("", "Edit Profile", "Edit Details") ||
                    OpenFacebookLink(accountId, statusText, "fb://profile_edit"))
                {
                    string city = currentCityList.OrderBy(_ => Guid.NewGuid()).First();
                    bool hasChanged = false;
                    string xml = "";
                    int startTick = Environment.TickCount;
                    while (true)
                    {
                        xml = _client.GetXMLSource();
                        string found = _client.FindElement(xml, new List<string> {
                    "//android.widget.ProgressBar", "//*[@text='Tap to retry']", "//*[@text='Edit']",
                    "//*[@text='Add']", "//*[@text='Add current city']",
                    "//android.widget.TextView[contains(@text,'Lives in')]",
                    "//*[@text='Edit current city']", "//android.widget.EditText[@text='Select current city']"
                }, 1);
                        switch (found)
                        {
                            case "//android.widget.EditText[@text='Select current city']":

                                _client.SendTextSlow(found, city);
                                _client.Delay(2);
                                var cityOptions = _client.FindBounds("", "//android.widget.ScrollView/android.view.ViewGroup/android.view.ViewGroup", 10);
                                if (cityOptions.Count > 0)
                                {
                                    if (cityOptions.Count == 1 && ContainsAnyKeyword("", "No Results Found"))
                                        goto EndCurrentCity;
                                    var point = new RectangleArea(cityOptions.OrderBy(_ => Guid.NewGuid()).First()).GetCenterPoint();
                                    _client.Click(point.X, point.Y);
                                }
                                goto AfterCurrentCity;
                            case "//*[@text='Save' or @content-desc='Save'][@enabled='true']":
                                _client.ElementWithAttributes(found, 5, xml);
                                hasChanged = true;
                                updatedFields += "Current City-";
                                goto AfterCurrentCity;
                            case "//*[@text='Add current city']":
                            case "//*[@text='Edit current city']":
                                if (city != "")
                                {
                                    _client.ElementWithAttributes("//*[@text='Save' or @content-desc='Save'][@enabled='true']", 1, xml);
                                    hasChanged = true;
                                    updatedFields += "Current City-";
                                }
                                else if (_client.ElementWithAttributes("//*[@text='Add Current City (Required)']", 1, xml, false))
                                {
                                    _client.ElementWithAttributes("//*[@text='Add Current City (Required)']", 1, xml);
                                }
                                else if (found == "//*[@text='Add current city']" || found == "//*[@text='Edit current city']")
                                {
                                    _client.ElementWithAttributes(found, 1, xml);
                                }
                                goto AfterCurrentCity;
                            case "//android.widget.TextView[contains(@text,'Lives in')]":
                                if (!skipWhenHave && !hasChanged)
                                {
                                    var rect = RectangleArea.FindOverlap(
                                        _client.FindBounds(xml, found, 1).First(),
                                        _client.FindBounds(xml, "//*[@content-desc='Edit']", 1)
                                    );
                                    if (rect != null)
                                        _client.Click(rect.GetCenterPoint().X, rect.GetCenterPoint().Y);
                                }
                                goto AfterCurrentCity;
                            case "//*[@text='Tap to retry']":
                                if (retry >= maxRetry) break;
                                retry++;
                                ScrollScreen(-1);
                                break;
                            case "//android.widget.ProgressBar":
                                SetStatusAccount(accountId, statusText + "Loading...");
                                break;
                            default:
                                if (!ScrollScreen()) goto AfterCurrentCity;
                                int loginResult = Login();
                                if (loginResult == 1) continue;
                                if (loginResult != 0) break;
                                break;
                        }
                    AfterCurrentCity:
                        _client.Delay(1);
                        if (Environment.TickCount - startTick < 300000) continue;
                        break;
                    }
                }
            EndCurrentCity:;
            }

            // Update Hometown
            if (updateHometown)
            {
                int retry = 0, maxRetry = 6;
                SetStatusAccount(accountId, statusText + "Cập nhật Hometown...");
                while (ContainsAnyKeyword("", "Edit Profile", "Edit Details") ||
                    OpenFacebookLink(accountId, statusText, "fb://profile_edit"))
                {
                    string hometown = "";
                    bool hasChanged = false;
                    string xml = "";
                    int startTick = Environment.TickCount;
                    while (true)
                    {
                        xml = _client.GetXMLSource();
                        string found = _client.FindElement(xml, new List<string> {
                    "//android.widget.ProgressBar", "//*[@text='Tap to retry']", "//*[@text='Edit']",
                    "//*[@text='Add']", "//*[@text='Add hometown']",
                    "//android.widget.TextView[contains(@text,'From')]",
                    "//*[@text='Edit hometown']", "//android.widget.EditText[@text='Select hometown']"
                }, 1);
                        switch (found)
                        {
                            case "//android.widget.EditText[@text='Select hometown']":
                                hometown = hometownList.OrderBy(_ => Guid.NewGuid()).First();
                                _client.SendTextSlow(found, hometown);
                                _client.Delay(2);
                                var hometownOptions = _client.FindBounds("", "//android.widget.ScrollView/android.view.ViewGroup/android.view.ViewGroup", 10);
                                if (hometownOptions.Count > 0)
                                {
                                    if (hometownOptions.Count == 1 && ContainsAnyKeyword("", "No Results Found"))
                                        goto EndHometown;
                                    var point = new RectangleArea(hometownOptions.OrderBy(_ => Guid.NewGuid()).First()).GetCenterPoint();
                                    _client.Click(point.X, point.Y);
                                }
                                goto AfterHometown;
                            case "//*[@text='Save' or @content-desc='Save'][@enabled='true']":
                                _client.ElementWithAttributes(found, 1, xml);
                                hasChanged = true;
                                updatedFields += "Hometown-";
                                goto AfterHometown;
                            case "//*[@text='Add hometown']":
                            case "//*[@text='Edit hometown']":
                                if (hometown != "")
                                {
                                    _client.ElementWithAttributes("//*[@text='Save' or @content-desc='Save'][@enabled='true']", 1, xml);
                                    hasChanged = true;
                                    updatedFields += "Hometown-";
                                }
                                else if (_client.ElementWithAttributes("//*[@text='Hometown Name (Required)']", 1, xml, false))
                                {
                                    _client.ElementWithAttributes("//*[@text='Hometown Name (Required)']", 1, xml);
                                }
                                else if (found == "//*[@text='Add hometown']" || found == "//*[@text='Edit hometown']")
                                {
                                    _client.ElementWithAttributes(found, 1, xml);
                                }
                                goto AfterHometown;
                            case "//android.widget.TextView[contains(@text,'From')]":
                                if (!skipWhenHave && !hasChanged)
                                {
                                    var rect = RectangleArea.FindOverlap(
                                        _client.FindBounds(xml, found, 1).First(),
                                        _client.FindBounds(xml, "//*[@content-desc='Edit']", 1)
                                    );
                                    if (rect != null)
                                        _client.Click(rect.GetCenterPoint().X, rect.GetCenterPoint().Y);
                                }
                                goto AfterHometown;
                            case "//*[@text='Tap to retry']":
                                if (retry >= maxRetry) break;
                                retry++;
                                ScrollScreen(-1);
                                break;
                            case "//android.widget.ProgressBar":
                                SetStatusAccount(accountId, statusText + "Loading...");
                                break;
                            default:
                                if (!ScrollScreen()) goto AfterHometown;
                                int loginResult = Login();
                                if (loginResult == 1) continue;
                                if (loginResult != 0) break;
                                break;
                        }
                    AfterHometown:
                        _client.Delay(2);
                        if (Environment.TickCount - startTick < 300000) continue;
                        break;
                    }
                }
            EndHometown:;
            }

            // Update Relationship
            if (updateRelationship)
            {
                int retry = 0, maxRetry = 6;
                SetStatusAccount(accountId, statusText + "Cập nhật Relationship...");
                while (ContainsAnyKeyword("", "Edit Profile", "Edit Details") ||
                    OpenFacebookLink(accountId, statusText, "fb://profile_edit"))
                {
                    bool flagSaved = false;
                    bool relationshipSelected = false;
                    string xml = "";
                    int startTick = Environment.TickCount;
                    while (true)
                    {
                        xml = _client.GetXMLSource();
                        string found = _client.FindElement(xml, new List<string>
                {
                    "//android.widget.ProgressBar", "//*[@text='Tap to retry']", "//*[@text='Edit']", "//*[@text='Add']",
                    "//*[@text='Add a relationship status']", "//*[@text='Relationship Status']", "//*[@text=\"Single\"]",
                    "//*[@text=\"In a relationship\"]", "//*[@text=\"Engaged\"]", "//*[@text=\"Married\"]", "//*[@text=\"In a civil union\"]",
                    "//*[@text=\"In a domestic partnership\"]", "//*[@text=\"In an open relationship\"]", "//*[@text=\"It's complicated\"]",
                    "//*[@text=\"Separated\"]", "//*[@text=\"Divorced\"]", "//*[@text=\"Widowed\"]"
                }, 1);
                        switch (found)
                        {
                            case "//*[@text='Relationship Status']":
                                if (_client.ElementWithAttributes("//*[@text='Save' or @content-desc='Save']", 1, xml, false))
                                {
                                    _client.ElementWithAttributes(found, 1, xml);
                                }
                                else
                                {
                                    if (!_client.ElementWithAttributes("//*[@text=\"" + relationshipValue + "\"]", 1, xml, false))
                                    {
                                        for (int i = 0; i < 5; i++)
                                        {
                                            if (ScrollScreen()) break;
                                            xml = "";
                                            if (_client.ElementWithAttributes("//*[@text=\"" + relationshipValue + "\"]", 1, xml)) break;
                                        }
                                    }
                                    if (_client.ElementWithAttributes("//*[@text=\"" + relationshipValue + "\"]", 1, xml))
                                    {
                                        relationshipSelected = true;
                                    }
                                }
                                break;
                            case "//*[@text='Add a relationship status']":
                                _client.ElementWithAttributes(found, 1, xml);
                                break;
                            case "//*[@text='Edit']":
                            case "//*[@text='Add']":
                                if (!flagSaved)
                                {
                                    var rect = RectangleArea.FindOverlap(
                                        _client.FindBounds(xml, "//*[@text='Details']", 1).FirstOrDefault(),
                                        _client.FindBounds(xml, found, 1)
                                    );
                                    if (rect != null)
                                        _client.Click(rect.GetCenterPoint().X, rect.GetCenterPoint().Y);
                                    else
                                        ScrollScreen();
                                }
                                break;
                            case "//*[@text='Single']":
                            case "//*[@text='In a relationship']":
                            case "//*[@text='Engaged']":
                            case "//*[@text='Married']":
                            case "//*[@text='In a civil union']":
                            case "//*[@text='In a domestic partnership']":
                            case "//*[@text='In an open relationship']":
                            case "//*[@text=\"It's complicated\"]":
                            case "//*[@text='Separated']":
                            case "//*[@text='Divorced']":
                            case "//*[@text='Widowed']":
                                if (!flagSaved)
                                {
                                    if (relationshipSelected)
                                    {
                                        _client.ElementWithAttributes("//*[@text='Save' or @content-desc='Save'][@enabled='true']", 1, xml);
                                        flagSaved = true;
                                        updatedFields += "Relationship-";
                                    }
                                    else
                                    {
                                        var rect = RectangleArea.FindOverlap(
                                            _client.FindBounds(xml, found, 1).First(),
                                            _client.FindBounds(xml, "//*[@content-desc='Edit']", 1)
                                        );
                                        if (rect != null)
                                            _client.Click(rect.GetCenterPoint().X, rect.GetCenterPoint().Y);
                                        else
                                            _client.ElementWithAttributes(found, 1, xml);
                                    }
                                }
                                break;
                            case "//*[@text='Tap to retry']":
                                if (retry >= maxRetry) break;
                                retry++;
                                ScrollScreen(-1);
                                break;
                            case "//android.widget.ProgressBar":
                                SetStatusAccount(accountId, statusText + "Loading...");
                                break;
                            default:
                                if (!ScrollScreen()) break;
                                int loginResult = Login();
                                if (loginResult == 1) continue;
                                if (loginResult != 0) break;
                                break;
                        }
                        _client.Delay(2);
                        if (Environment.TickCount - startTick < 300000) continue;
                        break;
                    }
                }
            }

            // Update Birthday/Gender
            if (updateGender || updateBirthday)
            {
                int retry = 0, maxRetry = 6;
                SetStatusAccount(accountId, statusText + "Cập nhật Birthday/Gender...");
                while (OpenFacebookLink(accountId, statusText, "fb://profile_edit"))
                {
                    string xml = "";
                    int startTick = Environment.TickCount;
                    do
                    {
                        xml = _client.GetXMLSource();
                        string found = _client.FindElement(xml, new List<string> {
                    "//android.widget.ProgressBar", "//*[@text='Tap to retry']", "//*[@text='Edit Your About Info']",
                    "//*[@text='Basic info']", "//*[@text='Birthday']/parent::*/child::*[2]/child::*/child::*"
                }, 1);
                        switch (found)
                        {
                            case "//*[@text='Edit Your About Info']":
                                SetStatusAccount(accountId, statusText + "Tap " + found + "...");
                                _client.ElementWithAttributes(found, 1, xml);
                                break;
                            case "//*[@text='Basic info']":
                                var rect = RectangleArea.FindOverlap(
                                    _client.FindBounds(xml, found, 1).First(),
                                    _client.FindBounds(xml, new List<string> { "//*[@text='Edit']" }, 1)
                                );
                                if (rect != null)
                                    _client.Click(rect.GetCenterPoint().X, rect.GetCenterPoint().Y);
                                break;
                            case "//*[@text='Birthday']/parent::*/child::*[2]/child::*/child::*":
                                if (updateBirthday && !ContainsAnyKeyword(xml, "There is a limit to how many times you can change your birthday"))
                                {
                                    string month = monthList.OrderBy(_ => Guid.NewGuid()).FirstOrDefault();
                                    if (!string.IsNullOrEmpty(month))
                                    {
                                        _client.ElementWithAttributes(found + "[1]/child::*", 1, xml);
                                        _client.Delay(2);
                                        month = month.TrimStart('0');
                                        if (month == "12")
                                        {
                                            ScrollScreen();
                                            _client.Delay(2);
                                        }
                                        _client.ElementWithAttributes("//*[@text='" + SubdyHelper.GetMonthNameFromNumber(month) + "']");
                                        _client.Delay(1);
                                    }
                                    string day = dayList.OrderBy(_ => Guid.NewGuid()).FirstOrDefault();
                                    if (!string.IsNullOrEmpty(day))
                                    {
                                        _client.ElementWithAttributes(found + "[2]/child::*");
                                        _client.Delay(2);
                                        day = day.TrimStart('0');
                                        if (!_client.ElementWithAttributes("//*[@text='" + day + "']"))
                                        {
                                            ScrollScreen(-1, 3);
                                            _client.Delay(2);
                                            for (int i = 0; i < 3; i++)
                                            {
                                                if (!_client.ElementWithAttributes("//*[@text='" + day + "']"))
                                                {
                                                    ScrollScreen();
                                                    _client.Delay(2);
                                                    continue;
                                                }
                                                _client.Delay(1);
                                                break;
                                            }
                                        }
                                    }
                                    string year = yearList.OrderBy(_ => Guid.NewGuid()).FirstOrDefault();
                                    if (!string.IsNullOrEmpty(year))
                                    {
                                        _client.ElementWithAttributes("//*[@text='Birth Year']/parent::*/child::*[2]/child::*/child::*/child::*");
                                        _client.Delay(2);
                                        if (!_client.ElementWithAttributes("//*[@text='" + year + "']"))
                                        {
                                            ScrollScreen(1, 5);
                                            _client.Delay(2);
                                            for (int j = 0; j < 5; j++)
                                            {
                                                if (!_client.ElementWithAttributes("//*[@text='" + year + "']"))
                                                {
                                                    ScrollScreen(-1);
                                                    _client.Delay(2);
                                                    continue;
                                                }
                                                _client.Delay(1);
                                                break;
                                            }
                                        }
                                    }
                                    _client.ElementWithAttributes("//*[@text='I confirm that I am ']", 10, "");
                                    //  method_114(accountId, "cBirthday", month + "/" + day + "/" + year, "birthday");
                                    updatedFields += "Birthday-";
                                }
                                if (updateGender)
                                {
                                    _client.ElementWithAttributes("//*[@text='" + genderValue + "']", 10, "");
                                    updatedFields += "Gender-";
                                }
                                for (int k = 0; k < 3; k++)
                                {
                                    if (_client.ElementWithAttributes("//*[@text='Save']")) break;
                                    ScrollScreen();
                                    _client.Delay(2);
                                }
                                _client.ElementWithAttributes("//*[@text='Cancel']", 60);
                                break;
                            case "//*[@text='Tap to retry']":
                                if (retry >= maxRetry) break;
                                retry++;
                                ScrollScreen(-1);
                                break;
                            case "//android.widget.ProgressBar":
                                SetStatusAccount(accountId, statusText + "Loading...");
                                break;
                            default:
                                if (!ScrollScreen()) break;
                                int loginResult = Login();
                                if (loginResult == 1) continue;
                                if (loginResult != 0) break;
                                break;
                        }
                        _client.Delay(2);
                    } while (Environment.TickCount - startTick < 300000);
                    break;
                }
            }

            return 1;
        }
        public async Task<int> HDTuongTacWall(JsonHelper settings, ScriptAction action)
        {
            int totalSeconds = SubdyHelper.RandomValue(settings.GetIntType("nudTimeFrom"), settings.GetIntType("nudTimeTo"));

            bool shouldInteract = settings.GetBooleanValue("ckbInteract");
            int interactCount = SubdyHelper.RandomValue(settings.GetIntType("nudInteractFrom", 1), settings.GetIntType("nudInteractTo", 1));
            List<string> reactionTypes = new List<string> { "Like", "Love", "Care", "Haha", "Wow", "Sad", "Angry" };
            if (!settings.GetBooleanValue("ckbLike"))
            {
                reactionTypes.Remove("Like");
            }
            if (!settings.GetBooleanValue("ckbTym"))
            {
                reactionTypes.Remove("Love");
            }
            if (!settings.GetBooleanValue("ckbThuong"))
            {
                reactionTypes.Remove("Care");
            }
            if (!settings.GetBooleanValue("ckbHaha"))
            {
                reactionTypes.Remove("Haha");
            }
            if (!settings.GetBooleanValue("ckbWow"))
            {
                reactionTypes.Remove("Wow");
            }
            if (!settings.GetBooleanValue("ckbBuon"))
            {
                reactionTypes.Remove("Sad");
            }
            if (!settings.GetBooleanValue("ckbGian"))
            {
                reactionTypes.Remove("Angry");
            }
            List<string> reactions = new List<string>();
            if (reactionTypes.Any())
            {
                for (int i = 0; i < interactCount; i++)
                {
                    reactions.Add(SubdyHelper.GetStringRandom(reactionTypes));
                }
            }

            bool shouldShareWall = settings.GetBooleanValue("ckbShareWall");
            int shareCount = SubdyHelper.RandomValue(settings.GetIntType("nudShareWallFrom", 1), settings.GetIntType("nudShareWallTo", 1));


            bool shouldComment = settings.GetBooleanValue("ckbComment");
            int commentCount = SubdyHelper.RandomValue(settings.GetIntType("nudCommentFrom", 1), settings.GetIntType("nudCommentTo", 1));


            DeplinkFacebook("fb://profile/");

            int tickCount = Environment.TickCount;
            while (!_mainService._ct.IsCancellationRequested)
            {
                // [TIME-LIMIT v21] Ép giới hạn thời gian NGAY TRONG hành động tương tác wall (mỗi vòng):
                // quá timeoutTaiKhoan/timeoutKichBan -> break -> ExecuteAsync đổi tài khoản. KHÔNG đụng logic FB.
                if (Stop()) break;
                await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(5, 10), $"Xem bài viết, đợi {{time}}s...", 2);

                if (shouldInteract && reactions.Any())
                {
                    var message = TapReaction(reactions[0]);
                    await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(3, 7), $"{message}, đợi {{time}}s...", 2);
                    reactions.RemoveAt(0);
                }
                if (shouldShareWall && shareCount > 0)
                {
                    var message = TapShareNewfeed("");
                    await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(3, 7), $"{message}, đợi {{time}}s...", 2);
                    shareCount--;
                }
                if (shouldComment && commentCount > 0)
                {
                    string image = "";
                    string content = "";
                    if (_data.ContainsKey($"{action.Id}_txtComments"))
                    {
                        lock (Globals.Lock)
                        {
                            var contents = _data[$"{action.Id}_txtComments"];
                            if (contents.Any())
                            {
                                content = SubdyHelper.GetStringRandom(contents);
                                if (!settings.GetBooleanValue("checkBox5"))
                                {
                                    contents.Remove(content);
                                    _data[$"{action.Id}_txtComments"] = contents;
                                }
                                if (settings.GetBooleanValue("checkBox4"))
                                {
                                    var context = new ScriptActionContext();
                                    settings.DeleteValue("txtComments", content);
                                    action.Json = settings.GetJsonString();
                                    context.Update(action);
                                }
                            }

                        }
                        content = SubdyHelper.SpinText(content);
                    }
                    if (settings.GetBooleanValue("ckbAnh") && _data.ContainsKey($"{action.Id}_txtPathAnh") && _data[$"{action.Id}_txtComments"].Any())
                    {
                        lock (Globals.Lock)
                        {
                            var images = _data[$"{action.Id}_txtPathAnh"];
                            image = SubdyHelper.GetStringRandom(images);
                            if (settings.GetBooleanValue("checkBox3"))
                            {
                                images.Remove(image);
                                File.Delete(image);
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(content) || File.Exists(image))
                    {
                        string message = await CommentAction(content, image, settings.GetBooleanValue("ckbTagNeuBat"), settings.GetBooleanValue("checkBox2"));
                        await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(3, 7), $"{message}, đợi {{time}}s...", 2);
                    }
                    commentCount--;
                }

                if (Environment.TickCount - tickCount >= totalSeconds * 1000)
                {
                    break;
                }
                ScrollScreen(1, 1);
            }
            int result = 0;
            return result;
        }
        public int HDCauHinhTaiKhoan(int accountId, string statusPrefix, string actionName)
        {
            string statusText = statusPrefix + "Đang " + actionName + ": ";
            SetStatusAccount(accountId, statusText + "Đang chạy...");
            int result = 2;
            int retry = 0;
            int maxRetry = 6;

            while (OpenFacebookLink(accountId, statusText, "fb://facewebmodal/f?href=https://m.facebook.com/settings/subscribe/?settings_tracking=mbasic_footer_link%3Asettings_3_0_pecs&_rdr"))
            {
                string xml = "";
                int startTick = Environment.TickCount;
                do
                {
                    // Set all checkboxes to Public if not already set
                    List<string> publicCheckboxes = _client.FindBounds("", "//android.view.View[@resource-id='m_check_list_aria_label']/parent::*/*[@text='Public']", 1);
                    foreach (string item in publicCheckboxes)
                    {
                        var point = new RectangleArea(item).GetCenterPoint();
                        _client.Click(point.X, point.Y);
                    }

                    xml = _client.GetXMLSource();
                    string found = _client.FindElement(xml, new List<string>
            {
                "//android.widget.ProgressBar",
                "//*[@text='Tap to retry']",
                "//android.view.View[@resource-id='m_check_list_aria_label']/parent::*/*[@text='Public']",
                "//android.widget.CheckBox[@text='I understand I could lose access to my account']",
                "//android.widget.Button[@text='Remove phone'][@focused='false']",
                "//android.widget.Button[@text='Remove phone'][@focused='true']",
                "//android.widget.Button[@text='Add Number']"
            }, 1);

                    switch (found)
                    {
                        case "//android.widget.ProgressBar":
                            SetStatusAccount(accountId, statusText + "Loading...");
                            break;
                        case "//*[@text='Tap to retry']":
                            if (retry >= maxRetry)
                                break;
                            retry++;
                            ScrollScreen(-1);
                            break;
                        case "//android.view.View[@resource-id='m_check_list_aria_label']/parent::*/*[@text='Public']":
                            _client.ElementWithAttributes(found, 1, xml);
                            break;
                        case "//android.widget.CheckBox[@text='I understand I could lose access to my account']":
                            _client.ElementWithAttributes("//android.widget.CheckBox[@text='I understand I could lose access to my account'][@checked='false']", 1, xml);
                            _client.Delay(2);
                            _client.ElementWithAttributes("//android.widget.Button[@text='Remove Number']", 1, xml);
                            break;
                        case "//android.widget.Button[@text='Remove phone'][@focused='false']":
                            // Just wait or scroll if needed
                            break;
                        case "//android.widget.Button[@text='Remove phone'][@focused='true']":
                            if (xml.Contains("Incorrect password"))
                            {
                                result = 3;
                                break;
                            }
                            break;
                        case "//android.widget.Button[@text='Add Number']":
                            result = 1;
                            break;
                        default:
                            SetStatusAccount(accountId, statusText + "Scroll...");
                            if (ScrollScreen())
                            {
                                int loginRes = Login();
                                if (loginRes == 1)
                                    break;
                                if (loginRes != 0)
                                    break;
                            }
                            break;
                    }

                    _client.Delay(2);

                    if (result == 1 || result == 3)
                        break;

                } while (Environment.TickCount - startTick < 300000);
                break;
            }

            return result;
        }
        public async Task<int> HDHuyKetBan(JsonHelper settings, ScriptAction action)
        {

            int minConfirm = settings.GetIntType("nudSoLuongFrom");
            int maxConfirm = settings.GetIntType("nudSoLuongTo");
            int minDelay = settings.GetIntType("nudDelayFrom");
            int maxDelay = settings.GetIntType("nudDelayTo");
            int confirmedCount = 1;
            string type = "NgauNhien";
            if (settings.GetBooleanValue("C2854635"))
            {
                type = "ChiDinh";
            }
            List<string> keywords = settings.GetValuesList("txtLinks");

            try
            {
                int targetCount = SubdyHelper.RandomValue(minConfirm, maxConfirm + 1);
                int refail = 0;
                while (!_mainService._ct.IsCancellationRequested)
                {
                    if (confirmedCount > targetCount)
                    {
                        break;
                    }
                    if (type == "NgauNhien")
                    {
                        DeplinkFacebook("fb://friends/");
                        if (!_client.ElementWithAttributes("//*[@content-desc=\"Your friends\"]", 20, "", true))
                        {
                            return 0;
                        }
                        Thread.Sleep(4000);
                        if (
                        _client.ElementWithAttributes("//*[@content-desc='More options' and @visible-to-user='true']", 2) &&
                        _client.ElementWithAttributes("//*[contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'unfriend') and @visible-to-user='true']", 10) &&
                        _client.ElementWithAttributes("//*[contains(translate(@text, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'confirm') and @visible-to-user='true']", 10)
                        )
                        {

                            await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(minDelay, maxDelay), $"({confirmedCount}/{targetCount}) Đã hủy kết bạn" + ", đợi {time}s...", 2);
                            confirmedCount++;
                        }
                        else if (_client.ElementWithAttributes("//*[@content-desc=\"Profile Picture\"]", 2, "", false))
                        {
                            _client.ATX.Press(PressKey.Back);
                        }
                        else
                        {
                            ScrollScreen(1, 1);
                            refail++;
                        }
                    }
                    else
                    {
                        if (!keywords.Any())
                        {
                            return confirmedCount;
                        }
                        string keyword = SubdyHelper.GetStringRandom(keywords);
                        keywords.Remove(keyword);
                        if (string.IsNullOrEmpty(keyword))
                        {
                            continue;
                        }
                        List<string> xpaths = new List<string>
                        {
                            "//*[@content-desc=\"Profile Picture\"]",
                            "//*[(@class='android.widget.Button') and " +
                            "(contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'friends') " +
                            "or contains(translate(@text, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'friends'))]",
                            "//*[@content-desc=\"Unfriend\"]",
                            "//*[@content-desc=\"Add friend\"]",
                        };
                        DeplinkFacebook($"fb://profile/{keyword}");
                        await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(5, 10), $"({confirmedCount}/{targetCount}) Go to profile {keyword}" + ", đợi {time}s...", 2);
                        string message = "";
                        while (!_mainService._ct.IsCancellationRequested && refail < 7)
                        {
                            var xpath = _client.FindElement("", xpaths, 2);
                            switch (xpath)
                            {
                                case "//*[@content-desc=\"Profile Picture\"]":
                                    {
                                        refail = 0;
                                        xpaths.Remove(xpath);
                                        continue;
                                    }
                                case "//*[(@class='android.widget.Button') and " +
                            "(contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'friends') " +
                            "or contains(translate(@text, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'friends'))]":
                                    {
                                        refail = 0;
                                        var points = _client.FindBounds("", xpath, 2);
                                        if (!points.Any())
                                        {
                                            refail++;
                                            break;

                                        }
                                        var point = new RectangleArea(points.Last()).RandomPoint();
                                        _client.ADB.Shell($"input tap {point.X} {point.Y}");
                                        Thread.Sleep(2000);
                                        xpaths.Remove(xpath);
                                        continue;
                                    }
                                case "//*[@content-desc=\"Unfriend\"]":
                                    {
                                        refail = 0;
                                        _client.ElementWithAttributes(xpath);
                                        _client.ElementWithAttributes("//*[@text=\"CONFIRM\"]", 10);
                                        message = $"({confirmedCount}/{targetCount}) Đã hủy kết bạn với uid: {keyword}";
                                        confirmedCount++;
                                        break;
                                    }
                                case "//*[@content-desc=\"Add friend\"]":
                                    {
                                        refail = 0;
                                        message = $"({confirmedCount}/{targetCount}) Đã hủy kết bạn với uid: {keyword} này trước đó";
                                        break;
                                    }
                                default:
                                    {
                                        ScrollScreen(-1, 1);
                                        refail++;
                                        break;
                                    }
                            }
                            if (!string.IsNullOrEmpty(message))
                            {
                                await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(5, 10), $"({confirmedCount}/{targetCount}) {message}" + ", đợi {time}s...", 2);
                                break;
                            }
                        }

                    }


                }
            }
            catch
            {
                confirmedCount = -1;
            }
            return confirmedCount;
        }
        public int HDMoiBanBeVaoNhom(int accountId, string statusPrefix, JsonHelper settings, string actionName)
        {
            // Get group ID
            string groupId = SubdyHelper.GetStringRandom(settings.GetValuesList("txtIdGroup"));
            int typeInvite = settings.GetIntType("typeInvite");
            int quantityFrom = settings.GetIntType("nudSoLuongFrom");
            int quantityTo = settings.GetIntType("nudSoLuongTo");
            int delayFrom = settings.GetIntType("nudDelayFrom");
            int delayTo = settings.GetIntType("nudDelayTo");

            int result = 1;
            int invitedCount = 0;
            int targetCount = SubdyHelper.RandomValue(quantityFrom, quantityTo);

            try
            {
                string statusText = statusPrefix + "Đang " + actionName + ": ";
                SetStatusAccount(accountId, statusText + "Go to Group " + groupId + "...");

                if (OpenFacebookLink(accountId, statusText, "fb://group/" + groupId))
                {
                    ScrollScreen(-1);
                    _client.Delay(1);
                    SetStatusAccount(accountId, statusText + "Find Invite Members...");
                    _client.ElementWithAttributes("//*[@content-desc='Invite Members' or starts-with(@content-desc,'invite others to join')]", 10, "");
                    SetStatusAccount(accountId, statusText + "Tap Invite Members...");
                    SetStatusAccount(accountId, statusText + "Find Invite...");
                    List<string> inviteElements = _client.FindBounds("", "//android.view.ViewGroup[@content-desc=\"Invite\"]", 3);
                    if (inviteElements.Count > 0)
                    {
                        string inviteXPath = "";
                        for (int i = 0; i < targetCount + 10; i++)
                        {
                            if (inviteElements.Count == 0)
                            {
                                SetStatusAccount(accountId, statusText + $"({invitedCount}/{targetCount}), Scroll...");
                                if (ScrollScreen())
                                {
                                    break;
                                }
                                SetStatusAccount(accountId, statusText + $"({invitedCount}/{targetCount}), Find Invite...");
                                inviteElements = _client.FindBounds("", "//android.view.ViewGroup[@content-desc=\"Invite\"]", 3);
                                if (inviteElements.Count == 0)
                                {
                                    break;
                                }
                            }
                            inviteXPath = inviteElements.LastOrDefault();
                            inviteElements.Remove(inviteXPath);
                            SetStatusAccount(accountId, statusText + $"({invitedCount}/{targetCount}), Tap Invite...");
                            var point = new RectangleArea(inviteXPath).GetCenterPoint();
                            _client.Click(point.X, point.Y);
                            invitedCount++;
                            if (invitedCount < targetCount)
                            {
                                SetStatusAccount(accountId, statusText + $"({invitedCount}/{targetCount}), " + "đợi {time}s...", SubdyHelper.RandomValue(delayFrom, delayTo + 1));
                                continue;
                            }
                            SetStatusAccount(accountId, statusText + $"({invitedCount}/{targetCount}), Done!");
                            break;
                        }
                    }
                }
            }
            catch
            {
                result = 0;
            }
            return result;
        }
        public int HDTaoPage(int accountId, string statusPrefix, JsonHelper settings, string actionName)
        {
            int createdPages = 0;
            try
            {
                string statusText = statusPrefix + "Đang " + actionName + ": ";
                SetStatusAccount(accountId, statusText + "Đang chạy...");
                int fromCount = settings.GetIntType("nudSoLuongFrom");
                int toCount = settings.GetIntType("nudSoLuongTo");
                int targetCount = SubdyHelper.RandomValue(fromCount, toCount);
                int delayFrom = settings.GetIntType("nudDelayFrom", 3);
                int delayTo = settings.GetIntType("nudDelayTo", 5);
                List<string> namePool = new List<string>(settings.GetValuesList("txtTenNhom"));
                List<string> categoryPool = new List<string>(settings.GetValuesList("txtCatagory"));
                List<string> pageNames = new List<string>(namePool);
                List<string> categories = new List<string>(categoryPool);

                if (pageNames.Count > 0)
                {
                    int retry = 0;
                    int maxRetry = 3;
                    while (true)
                    {
                        SetStatusAccount(accountId, statusText + $"({createdPages + 1}/{targetCount})...");
                        DeplinkFacebook("fb://pagestab");
                        _client.Delay(2);

                        int retryInner = 0;
                        int maxRetryInner = 5;
                        string xml = "";
                        int startTick = Environment.TickCount;

                        do
                        {
                            xml = _client.GetXMLSource();
                            string found = _client.FindElement(xml, new List<string>
                    {
                        "//android.widget.ProgressBar",
                        "//*[@text='Tap to retry']",
                        "//*[@content-desc='Create']",
                        "//*[@content-desc='Get Started']",
                        "//android.widget.EditText[@text='Page name']",
                        "//*[@content-desc='Next']",
                        "//*[@content-desc='Skip']"
                    }, 1);

                            switch (found)
                            {
                                case "//android.widget.ProgressBar":
                                    SetStatusAccount(accountId, statusText + $"({createdPages + 1}/{targetCount}), Loading...");
                                    break;
                                case "//*[@text='Tap to retry']":
                                    if (retry >= maxRetry) break;
                                    retry++;
                                    ScrollScreen(-1);
                                    break;
                                case "//*[@content-desc='Create']":
                                    if (ContainsAnyKeyword(xml, "Cannot create Page") || retryInner >= maxRetryInner)
                                        break;
                                    retryInner++;
                                    if (_client.ElementWithAttributes("//android.widget.EditText[@text='Search for categories']", 1, xml, false))
                                    {
                                        string category = "";
                                        if (categories.Count == 0)
                                            categories = new List<string>(categoryPool);
                                        if (categories.Count > 0)
                                        {
                                            category = categories.First();
                                            categories.RemoveAt(0);
                                        }
                                        if (category == "")
                                            category = SubdyHelper.RandomString(length: 1);
                                        _client.SendTextSlow("//android.widget.EditText[@text='Search for categories']", category);
                                        _client.Delay(2);
                                        _client.ElementWithAttributes("//android.widget.EditText/parent::*/following-sibling::*[1]/child::*");
                                        _client.Delay(2);
                                        xml = _client.GetXMLSource();
                                    }
                                    SetStatusAccount(accountId, statusText + $"({createdPages + 1}/{targetCount}), Tap {found}...");
                                    _client.ElementWithAttributes(found, 1, xml);
                                    break;
                                case "//*[@content-desc='Get Started']":
                                    SetStatusAccount(accountId, statusText + "Tap " + found + "...");
                                    _client.ElementWithAttributes(found, 1, xml);
                                    break;
                                case "//android.widget.EditText[@text='Page name']":
                                    if (pageNames.Count == 0)
                                        pageNames = new List<string>(namePool);
                                    string pageName = pageNames.First();
                                    pageNames.RemoveAt(0);
                                    pageName = SubdyHelper.ReplaceWithRandom(pageName);
                                    _client.SendTextSlow(found, pageName);
                                    break;
                                case "//*[@content-desc='Next']":
                                    if (!_client.ElementWithAttributes("//android.widget.ImageView", 1, xml, false))
                                    {
                                        if (_client.FindElement(xml, new List<string>
                                {
                                    "//*[@content-desc='Describe what your Page is about']",
                                    "//*[@content-desc='Edit action button']"
                                }, 1) != "")
                                        {
                                            createdPages++;
                                            SetStatusAccount(accountId, statusText + $"({createdPages}/{targetCount}), " + "đợi {time}s...", SubdyHelper.RandomValue(delayFrom, delayTo + 1));
                                            if (createdPages >= targetCount)
                                                break;
                                            continue;
                                        }
                                        SetStatusAccount(accountId, statusText + $"({createdPages + 1}/{targetCount}), Tap {found}...");
                                        _client.ElementWithAttributes(found, 1, xml);
                                    }
                                    break;
                                case "//*[@content-desc='Skip']":
                                    createdPages++;
                                    if (createdPages >= targetCount)
                                        break;
                                    continue;
                                default:
                                    SetStatusAccount(accountId, statusText + $"({createdPages + 1}/{targetCount}), Scroll...");
                                    if (ScrollScreen())
                                    {
                                        int loginResult = Login();
                                        if (loginResult == 0)
                                            break;
                                        if (loginResult == 1)
                                            continue;
                                    }
                                    break;
                            }
                            _client.Delay(2);
                        } while (Environment.TickCount - startTick < 300000);
                        if (createdPages >= targetCount)
                            break;
                    }
                }
            }
            catch (Exception)
            {
                // Optionally log error
            }
            return createdPages;
        }
        public int HDXoaReel(int accountId, string statusPrefix, JsonHelper settings, string actionName, string param1, string param2)
        {
            int fromCount = settings.GetIntType("nudSoLuongFrom", 1);
            int toCount = settings.GetIntType("nudSoLuongTo", 1);
            int delayFrom = settings.GetIntType("nudKhoangCachFrom");
            int delayTo = settings.GetIntType("nudKhoangCachTo");
            int deletedCount = 0;
            string xmlSource = "";
            string foundElement = "";

            try
            {
                string statusText = statusPrefix + "Đang " + actionName + ": ";
                int deleteTarget = SubdyHelper.RandomValue(fromCount, toCount + 1);

                for (int i = 0; i < deleteTarget + 5 && deletedCount < deleteTarget; i++)
                {
                    SetStatusAccount(accountId, statusText + $"({deletedCount + 1}/{deleteTarget})...");
                    try
                    {
                        while (true)
                        {
                            OpenReel("");
                            _client.Delay(2, 3);
                            int loginStatus = Login();
                            if (loginStatus == 1)
                                break;
                            if (loginStatus != 0)
                                goto EndDeleteReels;

                            int tapNavigateReelProfile = 0;
                            int startTick = Environment.TickCount;
                            int timeoutSec = 300;
                            do
                            {
                                SetStatusAccount(accountId, statusText + $"({deletedCount + 1}/{deleteTarget})...");
                                xmlSource = _client.GetXMLSource();
                                foundElement = _client.FindElement(xmlSource, new List<string>
                        {
                            "//*[@content-desc='Navigate to your Reels profile']",
                            "//*[@content-desc='View suggested entities']",
                            "//*[@content-desc='Delete']",
                            "//*[@text='Are you sure you want to remove your reel from Facebook?']"
                        }, 1);

                                switch (foundElement)
                                {
                                    case "//*[@content-desc='Navigate to your Reels profile']":
                                        if (tapNavigateReelProfile > 0)
                                        {
                                            _client.ElementWithAttributes("//*[@content-desc='Menu']", 1, xmlSource);
                                            break;
                                        }
                                        tapNavigateReelProfile++;
                                        SetStatusAccount(accountId, statusText + "Tap " + foundElement + "...");
                                        _client.ElementWithAttributes(foundElement, 1, xmlSource);
                                        break;

                                    case "//*[@content-desc='Delete']":
                                        _client.ElementWithAttributes(foundElement, 1, xmlSource);
                                        break;

                                    case "//*[@text='Are you sure you want to remove your reel from Facebook?']":
                                        _client.ElementWithAttributes("//*[@text='DELETE']", 1, xmlSource);
                                        _client.Delay(2);
                                        WaitForPostComplete(30);
                                        deletedCount++;
                                        if (deletedCount < deleteTarget)
                                        {
                                            SetStatusAccount(accountId, statusText + $"({deletedCount}/{deleteTarget}), " + "đợi {time}s...", SubdyHelper.RandomValue(delayFrom, delayTo + 1));
                                            break;
                                        }
                                        goto EndDeleteReels;

                                    case "//*[@content-desc='View suggested entities']":
                                        if (_client.ElementWithAttributes("//*[starts-with(@content-desc,'Reel,')]", 10, ""))
                                        {
                                            break;
                                        }
                                        goto EndDeleteReels;

                                    default:
                                        SetStatusAccount(accountId, statusText + $"({deletedCount + 1}/{deleteTarget}), Scroll...");
                                        if (!ScrollScreen(-1))
                                        {
                                            break;
                                        }
                                        int loginCheck = Login();
                                        if (loginCheck != 1 && loginCheck != 0)
                                        {
                                            goto EndForLoop;
                                        }
                                        break;
                                }
                                _client.Delay(1);
                            }
                            while (Environment.TickCount - startTick < timeoutSec * 1000);

                        EndForLoop:
                            break;
                        }
                    }
                    catch
                    {
                        continue;
                    }
                    break;
                EndDeleteReels:
                    break;
                }
            }
            catch
            {
                // Ignore errors
            }
            return deletedCount;
        }
        public int JoinGroupsByUid(int accountId, string statusPrefix, JsonHelper settings, string groupKey, string actionName)
        {
            int fromCount = settings.GetIntType("nudSoLuongFrom");
            int toCount = settings.GetIntType("nudSoLuongTo");
            int delayFrom = settings.GetIntType("nudDelayFrom");
            int delayTo = settings.GetIntType("nudDelayTo");
            bool autoAnswerQuestions = settings.GetBooleanValue("ckbTuDongTraLoiCauHoi");
            List<string> answerList = settings.GetValuesList("txtCauTraLoi");
            bool autoRemoveUid = settings.GetBooleanValue("ckbTuDongXoaUid");

            int joinedCount = 0;
            int targetCount = SubdyHelper.RandomValue(fromCount, toCount);
            string currentUid = "";
            List<string> uidList = new List<string>();

            // If not auto remove UID, work on a copy of the shared list
            if (!autoRemoveUid)
            {
                uidList = SubdyHelper.CloneList(dictionary_0[groupKey]);
            }

            try
            {
                string statusText = statusPrefix + "Đang " + actionName + ": ";
                for (int i = 0; i < targetCount + 10; i++)
                {
                    if (!autoRemoveUid)
                    {
                        if (uidList.Count != 0)
                        {
                            currentUid = SubdyHelper.GetStringRandom(uidList);
                            uidList.Remove(currentUid);
                            goto JoinAttempt;
                        }
                        break;
                    }
                    // Auto remove UID: lock shared dictionary
                    lock (dictionary_0)
                    {
                        if (dictionary_0[groupKey].Count == 0)
                        {
                            break;
                        }
                        currentUid = SubdyHelper.GetStringRandom(dictionary_0[groupKey]);
                        dictionary_0[groupKey].Remove(currentUid);
                        goto JoinAttempt;
                    }
                JoinAttempt:
                    JsonHelper joinConfig = new JsonHelper();
                    joinConfig.AddValue("id", (object)currentUid);
                    joinConfig.AddValue("isAnswer", autoAnswerQuestions);
                    joinConfig.AddValueList("lstCauTraLoi", answerList);

                    // Try joining group by UID with/without answering questions
                    if (JoinGroupByUid(accountId, statusPrefix + $"Đang {actionName}: ({joinedCount}/{targetCount}), ", joinConfig).isSuccess)
                    {
                        joinedCount++;
                        if (joinedCount >= targetCount)
                        {
                            SetStatusAccount(accountId, statusText + $"({joinedCount}/{targetCount}), Done!");
                            break;
                        }
                        SetStatusAccount(accountId, statusText + $"({joinedCount}/{targetCount}), " + "đợi {time}s...", SubdyHelper.RandomValue(delayFrom, delayTo + 1));
                    }
                }
            }
            catch
            {
                // Ignore errors
            }
            return joinedCount;
        }
        public int HDDongBoDanhBa(int accountId, string statusPrefix, JsonHelper settings, string phoneBookKey, string actionName)
        {
            int fromCount = settings.GetIntType("nudSoLuongFrom");
            int toCount = settings.GetIntType("nudSoLuongTo");
            bool autoRemove = settings.GetBooleanValue("ckbTuDongXoa");
            bool autoAddFriend = settings.GetBooleanValue("ckbAutoAddFriend");
            int addFriendFrom = settings.GetIntType("nudSoLuongKetBanFrom");
            int addFriendTo = settings.GetIntType("nudSoLuongKetBanTo");
            int delayFrom = settings.GetIntType("nudDelayFrom");
            int delayTo = settings.GetIntType("nudDelayTo");
            int result = 0;
            string statusText = statusPrefix + "Đang " + actionName + ": ";
            SetStatusAccount(accountId, statusText + "Đang chạy...");

            try
            {
                int syncCount = SubdyHelper.RandomValue(fromCount, toCount);
                List<string> phoneList = new List<string>();
                if (!autoRemove)
                {
                    phoneList = F605BBA9[phoneBookKey].GetRange(0, syncCount);
                }
                else
                {
                    lock (F605BBA9)
                    {
                        if (F605BBA9[phoneBookKey].Count > 0)
                        {
                            if (syncCount > F605BBA9[phoneBookKey].Count)
                                syncCount = F605BBA9[phoneBookKey].Count;
                            phoneList = F605BBA9[phoneBookKey].GetRange(0, syncCount);
                            F605BBA9[phoneBookKey].RemoveRange(0, syncCount);
                        }
                    }
                }

                if (phoneList.Count > 0)
                {
                    SetStatusAccount(accountId, statusText + "Import danh bạ...");
                    _client.AppClear("com.android.providers.contacts");
                    _client.AppStart("com.google.android.contacts");

                    int total = phoneList.Count;
                    for (int i = 0; i < total; i++)
                    {
                        SetStatusAccount(accountId, statusText + $"Import danh bạ ({i + 1}/{total})...");
                        string contactName = $"{SubdyHelper.GetStringRandom(SubdyHelper.FirstnameVN)} {SubdyHelper.GetStringRandom(SubdyHelper.LastnameVN)}";
                        _client.Shell("am start -a android.intent.action.INSERT -t vnd.android.cursor.dir/contact -e name '" + contactName + "' -e phone " + phoneList[i].Replace(" ", ""));
                        _client.Delay(2);
                        _client.ElementWithAttributes("//*[@text='Save']", 5, "");
                    }

                    // Sync với Facebook
                    try
                    {
                        int retry = 0;
                        int maxRetry = 6;
                        bool finishedSync = false;

                        while (!finishedSync)
                        {
                            _client.Shell($"su -c am start -n {FacebookHander.Package(PlatformModel.Facebook)}/{FacebookHander.Package(PlatformModel.Facebook)}.settings.activity.SettingsActivity");
                            _client.Delay(3);

                            int loginStatus = Login();
                            if (loginStatus == 1)
                                break; // Không đăng nhập được, thoát vòng lặp
                            else if (loginStatus != 0)
                                return result; // Lỗi đăng nhập, kết thúc hàm

                            string xml = "";
                            int startTick = Environment.TickCount;
                            while (Environment.TickCount - startTick < 300000)
                            {
                                xml = _client.GetXMLSource();
                                string found = _client.FindElement(xml, new List<string>
                        {
                            "//android.widget.ProgressBar",
                            "//*[@text='Tap to retry']",
                            "//*[@content-desc='Continuous contacts upload, off, switch']",
                            "//*[@text='Allow']",
                            "//*[@content-desc='Continuous contacts upload, on, switch']",
                            "//*[@text='TURN OFF']",
                            "//*[@text='Get started']",
                            "//*[@text='ADD FRIEND']",
                            "//*[@resource-id='android:id/empty']"
                        }, 1);

                                switch (found)
                                {
                                    case "//android.widget.ProgressBar":
                                        SetStatusAccount(accountId, statusText + "Loading...");
                                        break;
                                    case "//*[@text='Tap to retry']":
                                        if (retry < maxRetry)
                                        {
                                            retry++;
                                            ScrollScreen(-1);
                                            _client.Delay(2);
                                            continue;
                                        }
                                        break;
                                    case "//*[@content-desc='Continuous contacts upload, off, switch']":
                                    case "//*[@text='Allow']":
                                    case "//*[@content-desc='Continuous contacts upload, on, switch']":
                                    case "//*[@text='TURN OFF']":
                                    case "//*[@text='Get started']":
                                        SetStatusAccount(accountId, statusText + "Tap " + found + "...");
                                        _client.ElementWithAttributes(found, 1, xml);
                                        _client.Delay(2);
                                        continue;
                                    case "//*[@text='ADD FRIEND']":
                                        if (!autoAddFriend)
                                            break;
                                        statusText = statusPrefix + "Đang " + actionName + ": Kết bạn ";
                                        int addTarget = SubdyHelper.RandomValue(addFriendFrom, addFriendTo + 1);
                                        int addCount = 0;
                                        for (int j = 0; j < addTarget + 10; j++)
                                        {
                                            SetStatusAccount(accountId, statusText + $"({addCount}/{addTarget}), Find Add Friend...");
                                            List<string> addElements = _client.FindBounds("", found, 10);
                                            SetStatusAccount(accountId, statusText + $"({addCount}/{addTarget}), Find Add Friend: " + addElements.Count);
                                            if (addElements.Count == 0)
                                            {
                                                SetStatusAccount(accountId, statusText + $"({addCount}/{addTarget}), Scroll...");
                                                if (ScrollScreen())
                                                {
                                                    SetStatusAccount(accountId, statusText + $"({addCount}/{addTarget}), Can't Scroll...");
                                                    break;
                                                }
                                                SetStatusAccount(accountId, statusText + $"({addCount}/{addTarget}), Find Add Friend...");
                                                addElements = _client.FindBounds("", found, 10);
                                                SetStatusAccount(accountId, statusText + $"({addCount}/{addTarget}), Find Add Friend: " + addElements.Count);
                                                if (addElements.Count == 0)
                                                {
                                                    break;
                                                }
                                            }
                                            string addXPath = SubdyHelper.GetStringRandom(addElements);
                                            SetStatusAccount(accountId, statusText + $"({addCount}/{addTarget}), Tap Add Friend...");
                                            var point = new RectangleArea(addXPath).GetCenterPoint();
                                            if (!string.IsNullOrEmpty(addXPath) && _client.Click(point.X, point.Y))
                                                addCount++;

                                            if (addCount < addTarget)
                                            {
                                                int time = SubdyHelper.RandomValue(delayFrom, delayTo + 1);
                                                SetStatusAccount(accountId, statusText + $"({addCount}/{addTarget}), đợi {time}s...");
                                                _client.Delay(time);

                                                int innerLoginStatus = Login();
                                                if (innerLoginStatus == 0)
                                                    continue;
                                                else if (innerLoginStatus == 1)
                                                    break;
                                                else
                                                    return result;
                                            }
                                            SetStatusAccount(accountId, statusText + $"({addCount}/{addTarget}): Done!");
                                            break; // Kết thúc vòng lặp khi đủ số lượng
                                        }
                                        break;
                                    case "//*[@resource-id='android:id/empty']":
                                        break;
                                    default:
                                        SetStatusAccount(accountId, statusText + "Scroll...");
                                        if (ScrollScreen())
                                        {
                                            int scrollLoginStatus = Login();
                                            if (scrollLoginStatus == 0)
                                            {
                                                _client.Delay(2);
                                                continue;
                                            }
                                            else if (scrollLoginStatus == 1)
                                            {
                                                finishedSync = true;
                                                break;
                                            }
                                        }
                                        _client.Delay(2);
                                        continue;
                                }
                                _client.Delay(2);
                            }
                            finishedSync = true;
                        }
                    }
                    catch
                    {
                        // Có thể log lỗi tại đây nếu cần
                    }
                }
            }
            catch
            {
                // Có thể log lỗi tại đây nếu cần
            }
            return result;
        }
        public int HDDangXuatThietBiCu(int accountId, string statusPrefix, string actionName)
        {
            string statusText = statusPrefix + "Đang " + actionName + ": ";
            SetStatusAccount(accountId, statusText + "Đang chạy...");
            bool isSuccess = false;
            try
            {
                // Mở trang quản lý phiên đăng nhập Facebook
                if (OpenFacebookLink(accountId, statusText,
                    "fb://facewebmodal/f?href=https://mbasic.facebook.com/settings/security_login/sessions/"))
                {
                    int timeoutSeconds = 60;
                    string buttonXPath = "";
                    string buttonResource = "";
                    bool tappedLogOut = false;
                    int startTick = Environment.TickCount;

                    while (Environment.TickCount - startTick < timeoutSeconds * 1000)
                    {
                        buttonXPath = "";
                        buttonResource = _client.FindElement(buttonXPath, new List<string> {
                    "//android.widget.Button[@text='Log out of all sessions']",
                    "//android.widget.Button[@text='Log out']"
                }, 1);

                        if (buttonResource == "//android.widget.Button[@text='Log out of all sessions']")
                        {
                            SetStatusAccount(accountId, statusText + "Tap " + Regex.Match(buttonResource, "'(.*?)'").Groups[1].Value + "...");
                            _client.ElementWithAttributes(buttonResource, 1, buttonXPath);
                        }
                        else if (buttonResource == "//android.widget.Button[@text='Log out']")
                        {
                            SetStatusAccount(accountId, statusText + "Tap " + Regex.Match(buttonResource, "'(.*?)'").Groups[1].Value + "...");
                            _client.ElementWithAttributes(buttonResource, 1, buttonXPath);
                            tappedLogOut = true;
                        }
                        else if (tappedLogOut)
                        {
                            _client.ElementWithAttributes("//android.widget.Button[@text='Log out']", 30);
                            WaitForPostComplete(30);
                            isSuccess = true;
                            break;
                        }
                        _client.Delay(2);
                    }
                }
            }
            catch
            {
                // Bạn có thể log lỗi ở đây nếu cần
            }
            return isSuccess ? 1 : 0;
        }
        public int HDDoiMatKhau(ref int status, int accountId, string statusPrefix, JsonHelper settings, string actionName)
        {
            // Lấy mật khẩu hiện tại từ cấu hình
            //   method_117(accountId, "cId");
            string oldPassword = (_account?.Password ?? "").Trim();
            if (string.IsNullOrEmpty(oldPassword))
            {
                status = 3;
                throw new SubdyExtension(SubdyEnum.JobFail, "Khong co mat khau hien tai trong tai khoan.");
            }

            // Sinh mật khẩu mới theo cấu hình. UI mới: rbMatKhauRandom + nudInteractFrom (số ký tự), A12E5D8C + txtMatKhauChiDinh (1 mật khẩu chỉ định).
            string newPassword = "";
            bool useRandom = settings.GetBooleanValue("rbMatKhauRandom");
            bool useChiDinh = settings.GetBooleanValue("A12E5D8C");
            if (!useRandom && !useChiDinh)
            {
                // Fallback dạng cũ
                useRandom = settings.GetIntType("typeMatKhau") == 0;
                useChiDinh = !useRandom;
            }
            if (useRandom)
            {
                int passwordLength = settings.GetIntType("nudInteractFrom", 10);
                if (passwordLength < 6) passwordLength = 10;
                newPassword = SubdyHelper.RandomString(length: passwordLength);
            }
            else
            {
                string candidate = (settings.GetValue("txtMatKhauChiDinh") ?? "").Trim();
                if (string.IsNullOrEmpty(candidate))
                {
                    // Fallback đọc danh sách mật khẩu cũ
                    var passwordList = settings.GetValuesList("txtMatKhau");
                    candidate = passwordList?.OrderBy(x => Guid.NewGuid()).FirstOrDefault() ?? "";
                }
                if (string.IsNullOrEmpty(candidate))
                {
                    status = 4;
                    throw new SubdyExtension(SubdyEnum.JobFail, "Chua cau hinh mat khau moi.");
                }

                if (candidate.Contains("*"))
                {
                    string[] parts = candidate.Split('*');
                    newPassword = parts[0];
                    for (int i = 1; i < parts.Length; i++)
                    {
                        newPassword += SubdyHelper.RandomString(length: 1);
                        newPassword += parts[i];
                    }
                }
                else
                {
                    newPassword = candidate;
                }
            }
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                status = 4;
                throw new SubdyExtension(SubdyEnum.JobFail, "Mat khau moi phai co it nhat 6 ky tu.");
            }
            if (newPassword == oldPassword)
            {
                newPassword += SubdyHelper.RandomString(length: 1);
            }

            // UI mới dùng B197EAA2 cho "Đăng xuất thiết bị cũ"; vẫn giữ fallback tên cũ ckbDangXuatThietBiCu
            bool useAccountCenter = settings.GetBooleanValue("ckbAccountCenter");
            bool logoutOldDevices = settings.GetBooleanValue("B197EAA2") || settings.GetBooleanValue("ckbDangXuatThietBiCu");
            string statusText = statusPrefix + "Đang " + actionName + ": ";
            SetStatusAccount(accountId, statusText + "Đang chạy...");
            status = 2;
            int retryScroll = 0;
            int maxRetryScroll = 6;

            // Mở trang đổi mật khẩu Facebook
            //string changePasswordLink = useAccountCenter
            //    ? "fb://facewebmodal/f?href=https://accountscenter.facebook.com/password_and_security/password/change/"
            //    : "fb://security_settings";
            string changePasswordLink = "am start -n com.facebook.katana/.IntentUriHandler \"fb://facewebmodal/f?href=https://accountscenter.facebook.com/password_and_security/password/change/\"";
            _client.Shell(changePasswordLink);
            _client.Delay(3);
            {
                string xmlSource = "";
                int editCount = 0;
                int tickStart = Environment.TickCount;
                bool done = false;

                while (Environment.TickCount - tickStart < 300000 && !done)
                {
                    xmlSource = _client.GetXMLSource();
                    if (ContainsAnyKeyword(xmlSource, "Your old password was incorrectly typed", "The password you entered was incorrect", "Incorrect password", "Enter a valid password and try again"))
                    {
                        status = 3;
                        done = true;
                        break;
                    }
                    if (ContainsAnyKeyword(xmlSource, "Password must differ from old password", "same as your old password"))
                    {
                        status = 5;
                        done = true;
                        break;
                    }
                    if (ContainsAnyKeyword(xmlSource, "Password changed", "Your password has been changed", "changed your password"))
                    {
                        status = 1;
                        _account.Password = newPassword;
                        new AccountContext().Update(_account);
                        done = true;
                        break;
                    }
                    string foundElement = _client.FindElement(xmlSource, new List<string> {
                "//android.widget.EditText",
                "//*[contains(@text,'Facebook')]",
                "//*[contains(@text,'Password and security')]",
                "//*[contains(@content-desc,'Password and security')]",
                "//*[@text='Change password']",
                "//*[@content-desc='Change password']",
                "//*[contains(@text,'Change password')]",
                "//*[contains(@content-desc,'Change password')]",
                "//*[@text='Log out of other devices?']",
                "//*[contains(@text,'Log out of other devices')]",
                "//*[contains(@text,\"WHERE YOU'RE LOGGED IN\")]",
                "//*[@text='Log out']",
                "//android.widget.ProgressBar",
                "//*[@text='Tap to retry']"
            }, 1);

                    switch (foundElement)
                    {
                        case "//*[contains(@text,'Facebook')]":
                        case "//*[contains(@text,'Password and security')]":
                        case "//*[contains(@content-desc,'Password and security')]":
                        case "//*[@text='Change password']":
                        case "//*[@content-desc='Change password']":
                        case "//*[contains(@text,'Change password')]":
                        case "//*[contains(@content-desc,'Change password')]":
                        case "//*[@text='Log out']":
                            SetStatusAccount(accountId, statusText + "Tap " + foundElement + "...");
                            _client.ElementWithAttributes(foundElement, 1, xmlSource);
                            if (foundElement == "//*[@text='Log out']")
                                done = true;
                            break;

                        case "//*[@text='Tap to retry']":
                            if (retryScroll < maxRetryScroll)
                            {
                                retryScroll++;
                                ScrollScreen(-1);
                            }
                            else
                            {
                                status = 4;
                                done = true;
                            }
                            break;

                        case "//android.widget.ProgressBar":
                            SetStatusAccount(accountId, statusText + "Loading...");
                            break;

                        case "//*[@text='Log out of other devices?']":
                        case "//*[contains(@text,'Log out of other devices')]":
                        case "//*[contains(@text,\"WHERE YOU'RE LOGGED IN\")]":
                            status = 1;
                            _account.Password = newPassword;
                            new AccountContext().Update(_account);
                            if (_client.ElementWithAttributes("//*[@text=\"WHERE YOU'RE LOGGED IN\"]", 1, xmlSource, false))
                            {
                                if (!_client.ElementWithAttributes("//*[@text='Log out of all sessions']", 1, xmlSource, false))
                                {
                                    bool foundLogoutAll = false;
                                    do
                                    {
                                        SetStatusAccount(accountId, statusText + "Scroll...");
                                        if (ScrollScreen())
                                            break;
                                        _client.Delay(2);
                                        xmlSource = "";
                                        foundLogoutAll = _client.ElementWithAttributes("//*[@text='Log out of all sessions']", 1, xmlSource, false);
                                    }
                                    while (!foundLogoutAll);
                                }
                                _client.ElementWithAttributes("//*[@text='Log out of all sessions']", 1, xmlSource);
                                done = true;
                            }
                            else
                            {
                                if (!logoutOldDevices)
                                {
                                    _client.ElementWithAttributes("//*[@text='stay logged in']", 1, xmlSource);
                                    _client.Delay(2);
                                    _client.ElementWithAttributes("//*[@text='Continue']", 1, xmlSource);
                                    done = true;
                                }
                                else
                                {
                                    _client.ElementWithAttributes("//*[@text='Review other devices']", 1, xmlSource);
                                    _client.Delay(2);
                                    _client.ElementWithAttributes("//*[@text='Continue']", 1, xmlSource);
                                }
                            }
                            break;

                        case "//android.widget.EditText":
                            if (editCount > 0)
                            {
                                var errorElement = _client.FindElement(xmlSource, new List<string> {
                            "//*[@text='Enter a valid password and try again.']",
                            "//*[@text='Your old password was incorrectly typed.']"
                        }, 1);
                                if (!string.IsNullOrEmpty(errorElement))
                                {
                                    status = 3;
                                    done = true;
                                }
                                else if (ContainsAnyKeyword(xmlSource, "Password must differ from old password"))
                                {
                                    status = 5;
                                    done = true;
                                }
                                // Nếu không có lỗi thì tiếp tục
                            }
                            else
                            {
                                editCount++;
                                _client.SendTextSlow("(//android.widget.EditText)[1]", oldPassword);
                                _client.Delay(1);
                                _client.SendTextSlow("(//android.widget.EditText)[2]", newPassword);
                                _client.Delay(1);
                                _client.SendTextSlow("(//android.widget.EditText)[3]", newPassword);
                                _client.Delay(1);
                                if (!_client.ElementWithAttributes("//*[contains(@text,'Save changes')]", 3, xmlSource)
                                    && !_client.ElementWithAttributes("//*[contains(@content-desc,'Save changes')]", 3, xmlSource)
                                    && !_client.ElementWithAttributes("//*[contains(@text,'Update Password')]", 3, xmlSource)
                                    && !_client.ElementWithAttributes("//*[contains(@content-desc,'Update Password')]", 3, xmlSource))
                                {
                                    _client.ATX.Press(PressKey.Enter);
                                }
                            }
                            break;

                        default:
                            int loginResult = Login();
                            if (loginResult == 1)
                            {
                                done = true;
                            }
                            else if (loginResult == 0)
                            {
                                // Tiếp tục vòng lặp, không làm gì
                            }
                            break;
                    }
                    _client.Delay(2);
                }
            }
            if (status == 1)
            {
                return 1;
            }
            if (status == 3)
            {
                throw new SubdyExtension(SubdyEnum.JobFail, "Mat khau hien tai khong dung.");
            }
            if (status == 5)
            {
                throw new SubdyExtension(SubdyEnum.JobFail, "Mat khau moi trung hoac qua giong mat khau cu.");
            }
            throw new SubdyExtension(SubdyEnum.JobFail, "Doi mat khau that bai.");
        }
        public int HDDanhGiaPage(int accountId, string statusPrefix, JsonHelper settings, string contentKey, string actionName)
        {
            bool autoRemoveContent = settings.GetBooleanValue("ckbTuDongXoaNoiDung");
            string reviewContent = "";

            // Lấy nội dung đánh giá từ dictionary theo config (tự động xóa hay không)
            if (CAB1A00C[contentKey].Count > 0)
            {
                if (!autoRemoveContent)
                {
                    reviewContent = CAB1A00C[contentKey][SubdyHelper.RandomValue(0, CAB1A00C[contentKey].Count)];
                }
                else
                {
                    lock (CAB1A00C)
                    {
                        int index = SubdyHelper.RandomValue(0, CAB1A00C[contentKey].Count);
                        reviewContent = CAB1A00C[contentKey][index];
                        CAB1A00C[contentKey].RemoveAt(index);
                    }
                }
            }

            // Chuẩn bị request đánh giá page
            JsonHelper reviewRequest = new JsonHelper();
            reviewRequest.AddValue("id", settings.GetValue("txtUid"));
            reviewRequest.AddValue("isLikePage", settings.GetBooleanValue("ckbInteract"));
            reviewRequest.AddValue("isReviewPage", true);
            reviewRequest.AddValue("content", reviewContent);

            string statusText = statusPrefix + "Đang " + actionName + ": ";
            var (success, errorMsg) = ProcessPageAction(accountId, statusText, reviewRequest);

            // Nếu thất bại và có cấu hình tự xóa, hoàn trả lại nội dung vào dictionary
            if (!success && autoRemoveContent)
            {
                lock (CAB1A00C)
                {
                    CAB1A00C[contentKey].Add(reviewContent);
                }
            }

            return success ? 1 : 0;
        }
        public async Task<int> HDTuongTacNhom(JsonHelper settings, ScriptAction action)
        {
            int count = SubdyHelper.RandomValue(settings.GetIntType("nudTuKhoaFrom"), settings.GetIntType("nudTuKhoaTo"));

            int delayFrom = settings.GetIntType("nudTimeFrom");
            int delayTo = settings.GetIntType("nudTimeTo");


            bool shouldInteract = settings.GetBooleanValue("ckbInteract");
            int interactCount = SubdyHelper.RandomValue(settings.GetIntType("nudInteractFrom", 1), settings.GetIntType("nudInteractTo", 1));
            List<string> reactionTypes = new List<string> { "Like", "Love", "Care", "Haha", "Wow", "Sad", "Angry" };
            if (!settings.GetBooleanValue("ckbLike"))
            {
                reactionTypes.Remove("Like");
            }
            if (!settings.GetBooleanValue("ckbTym"))
            {
                reactionTypes.Remove("Love");
            }
            if (!settings.GetBooleanValue("ckbThuong"))
            {
                reactionTypes.Remove("Care");
            }
            if (!settings.GetBooleanValue("ckbHaha"))
            {
                reactionTypes.Remove("Haha");
            }
            if (!settings.GetBooleanValue("ckbWow"))
            {
                reactionTypes.Remove("Wow");
            }
            if (!settings.GetBooleanValue("ckbBuon"))
            {
                reactionTypes.Remove("Sad");
            }
            if (!settings.GetBooleanValue("ckbGian"))
            {
                reactionTypes.Remove("Angry");
            }
            List<string> reactions = new List<string>();
            if (reactionTypes.Any())
            {
                for (int i = 0; i < interactCount; i++)
                {
                    reactions.Add(SubdyHelper.GetStringRandom(reactionTypes));
                }
            }

            bool shouldShareWall = settings.GetBooleanValue("ckbShareWall");
            int shareCount = SubdyHelper.RandomValue(settings.GetIntType("nudShareWallFrom", 1), settings.GetIntType("nudShareWallTo", 1));



            bool shouldComment = settings.GetBooleanValue("ckbComment");
            int commentCount = SubdyHelper.RandomValue(settings.GetIntType("nudCommentFrom", 1), settings.GetIntType("nudCommentTo", 1));


            if (!DeplinkFacebook("fb://faceweb/f?href=https://m.facebook.com/groups_browse/your_groups/").Contains("groups_browse/your_groups/"))
            {
                throw new SubdyExtension(SubdyEnum.Error, "Mở nhóm của bạn thất bại");
            }
            await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(5, 10), $"Tìm kiếm nhóm..., đợi {{time}}s...", 2);
            string xpath = "//node[contains(@class,'android.widget.Button') and @content-desc " +
                            "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'create a group')) " +
                            "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'sort groups'))" +
                            "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'your groups'))" +
                            "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'posts'))" +
                            "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'discover'))" +
                            "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'manage'))" +
                            "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'most visited'))]";
            var friends = _client.FindElementsNotToLower(10, "", xpath);
            // var friends = _client.FindBounds("", "//*[@class='android.widget.Button']/parent::*[@visible-to-user='true']", 15);
            if (!friends.Any())
            {
                throw new SubdyExtension(SubdyEnum.Error, "Không tìm thấy nhóm nào cả");
            }

            int countFriends = 0;
            while (!_mainService._ct.IsCancellationRequested)
            {
                // [TIME-LIMIT v21] Ép giới hạn thời gian NGAY TRONG hành động tương tác nhóm (mỗi vòng):
                // quá timeoutTaiKhoan/timeoutKichBan -> break -> ExecuteAsync đổi tài khoản. KHÔNG đụng logic FB.
                if (Stop()) break;
                if (countFriends >= count)
                {
                    break;
                }

                friends = _client.FindElementsNotToLower(10, "", xpath);// _client.FindBounds("", "//*[@class='android.widget.Button']/parent::*[@visible-to-user='true']", 15);
                if (!friends.Any())
                {
                    break;
                }
                if (!friends.Any())
                {
                    break;
                }
                foreach (var friend in friends)
                {
                    if (countFriends >= count)
                    {
                        break;
                    }
                    var node = _client.ExtractNodeInfo(friend.OuterXml);
                    if (node == null || !node.ContainsKey("content-desc") || !node.ContainsKey("bounds"))
                    {
                        continue;
                    }
                    var point = new RectangleArea(node["bounds"]).GetCenterPoint();
                    if (!_client.Click(point.X, point.Y))
                    {
                        continue;
                    }

                    countFriends++;
                    await _mainService.DelayMessageAsync(5, $"Go to Group {node["content-desc"]}, đợi {{time}}s...", 2);
                    int tickCount = Environment.TickCount;
                    int timeout = SubdyHelper.RandomValue(delayFrom, delayTo + 1);
                    while (Environment.TickCount - tickCount < timeout * 1000)
                    {
                        if (_mainService._ct.IsCancellationRequested)
                        {
                            break;
                        }
                        if (shouldInteract && reactions.Any())
                        {
                            var message = TapReaction(reactions[0]);
                            await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(3, 7), $"{message}, đợi {{time}}s...", 2);
                            reactions.RemoveAt(0);
                        }
                        if (shouldShareWall && shareCount > 0)
                        {
                            var message = TapShareNewfeed("");
                            await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(3, 7), $"{message}, đợi {{time}}s...", 2);
                            shareCount--;
                        }
                        if (shouldComment && commentCount > 0)
                        {
                            string image = "";
                            string content = "";
                            if (_data.ContainsKey($"{action.Id}_txtComments"))
                            {
                                lock (Globals.Lock)
                                {
                                    var contents = _data[$"{action.Id}_txtComments"];
                                    if (contents.Any())
                                    {
                                        content = SubdyHelper.GetStringRandom(contents);
                                        if (!settings.GetBooleanValue("checkBox5"))
                                        {
                                            contents.Remove(content);
                                            _data[$"{action.Id}_txtComments"] = contents;
                                        }
                                        if (settings.GetBooleanValue("checkBox4"))
                                        {
                                            var context = new ScriptActionContext();
                                            settings.DeleteValue("txtComments", content);
                                            action.Json = settings.GetJsonString();
                                            context.Update(action);
                                        }
                                    }

                                }
                                content = SubdyHelper.SpinText(content);
                            }
                            if (settings.GetBooleanValue("ckbAnh") && _data.ContainsKey($"{action.Id}_txtPathAnh") && _data[$"{action.Id}_txtComments"].Any())
                            {
                                lock (Globals.Lock)
                                {
                                    var images = _data[$"{action.Id}_txtPathAnh"];
                                    image = SubdyHelper.GetStringRandom(images);
                                    if (settings.GetBooleanValue("checkBox3"))
                                    {
                                        images.Remove(image);
                                        File.Delete(image);
                                    }
                                }
                            }

                            if (!string.IsNullOrEmpty(content) || File.Exists(image))
                            {
                                string message = await CommentAction(content, image, settings.GetBooleanValue("ckbTagNeuBat"), settings.GetBooleanValue("checkBox2"));
                                await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(3, 7), $"{message}, đợi {{time}}s...", 2);
                            }
                            commentCount--;
                        }

                        ScrollScreen(1, 1);
                    }

                    _client.ATX.Press(PressKey.Back);
                    Thread.Sleep(2000);
                }
                ScrollScreen(1, 2);
            }
            int result = 0;
            return result;
        }
        public async Task<int> HDShareBaiNangCao(JsonHelper settings, ScriptAction action)
        {
            int shareWallCount = 0;
            int shareGroupCount = 0;
            int shareFriendCount = 0;
            int countTaget = 1;
            int countPost = SubdyHelper.RandomValue(settings.GetIntType("C913DC8A", 1), settings.GetIntType("F391713F", 1));


            int delayFrom = settings.GetIntType("nudKhoangCachFrom");
            int delayTo = settings.GetIntType("nudKhoangCachTo");
            List<string> uidPosts = settings.GetValuesList("txtNotes");
            bool isContent = settings.GetBooleanValue("ckbVanBan");
            bool isNeuBat = settings.GetBooleanValue("checkBox6");
            bool isFollowers = settings.GetBooleanValue("checkBox7");
            bool isInterval = settings.GetBooleanValue("checkBox1");
            int viewFrom = settings.GetIntType("nudInteractFrom");
            int viewTo = settings.GetIntType("nudInteractTo");

            List<string> reactionTypes = new List<string> { "Like", "Love", "Care", "Haha", "Wow", "Sad", "Angry" };
            if (!settings.GetBooleanValue("ckbLike"))
            {
                reactionTypes.Remove("Like");
            }
            if (!settings.GetBooleanValue("ckbTym"))
            {
                reactionTypes.Remove("Love");
            }
            if (!settings.GetBooleanValue("ckbThuong"))
            {
                reactionTypes.Remove("Care");
            }
            if (!settings.GetBooleanValue("ckbHaha"))
            {
                reactionTypes.Remove("Haha");
            }
            if (!settings.GetBooleanValue("ckbWow"))
            {
                reactionTypes.Remove("Wow");
            }
            if (!settings.GetBooleanValue("ckbBuon"))
            {
                reactionTypes.Remove("Sad");
            }
            if (!settings.GetBooleanValue("ckbGian"))
            {
                reactionTypes.Remove("Angry");
            }
            List<string> reactions = new List<string>();
            if (reactionTypes.Any())
            {
                reactions.Add(SubdyHelper.GetStringRandom(reactionTypes));
            }
            if (!settings.GetBooleanValue("ckbInteract"))
            {
                reactions.Clear();
            }
            bool isComment = settings.GetBooleanValue("ckbComment");
            bool isNeuBatComment = settings.GetBooleanValue("ckbTagNeuBat");
            bool isFollowersComment = settings.GetBooleanValue("checkBox2");
            bool isCommentMedia = settings.GetBooleanValue("ckbAnh");
            bool deleteImage = settings.GetBooleanValue("checkBox3");
            bool shareNewfeed = settings.GetBooleanValue("ckbShareBaiLenTuong");
            bool shareGroup = settings.GetBooleanValue("ckbShareBaiLenNhom");
            int countGroup = SubdyHelper.RandomValue(settings.GetIntType("nudCountGroupFrom", 1), settings.GetIntType("F2374705", 1));
            List<string> groupNames = settings.GetValuesList("txtUids");
            bool shareFriend = settings.GetBooleanValue("checkBox9");
            int countFriend = SubdyHelper.RandomValue(settings.GetIntType("numericUpDown2", 1), settings.GetIntType("numericUpDown1", 1));
            List<string> friendNames = settings.GetValuesList("txtKeywords");


            while (!_mainService._ct.IsCancellationRequested)
            {
                if (countTaget > countPost || !uidPosts.Any()) break;
                string uidPost = SubdyHelper.GetStringRandom(uidPosts);
                uidPosts.Remove(uidPost);
                DeplinkFacebook($"fb://faceweb/f?href=https://www.facebook.com/{SubdyHelper.RandomString("0123456789", SubdyHelper.RandomValue(6, 20))}/posts/{uidPost}");
                Thread.Sleep(4000);
                int refail = 0;
                while (!_mainService._ct.IsCancellationRequested)
                {
                    if (refail > 7) break;
                    if (_client.ElementWithAttributes(new List<string> { "//*[@content-desc=\"Share\"]", "//*[@text=\"Share\"]" }, 1, "", false))
                    {
                        refail = 0;
                        break;
                    }
                    ScrollScreen(1, 1);
                    refail++;
                }
                if (refail == 0) continue;
                if (isInterval)
                {
                    await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(viewFrom, viewTo), $"Xem bài viết trước khi tương tác, đợi {{time}}s...", 2);
                    if (reactions.Any())
                    {
                        var message = TapReaction(SubdyHelper.GetStringRandom(reactions));
                        await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(3, 7), $"{message}, đợi {{time}}s...", 2);

                    }
                    if (isComment)
                    {
                        string image = "";
                        string content = "";
                        if (_data.ContainsKey($"{action.Id}_txtComments"))
                        {
                            lock (Globals.Lock)
                            {
                                var contents = _data[$"{action.Id}_txtComments"];
                                if (contents.Any())
                                {
                                    content = SubdyHelper.GetStringRandom(contents);
                                    if (!settings.GetBooleanValue("checkBox5"))
                                    {
                                        contents.Remove(content);
                                        _data[$"{action.Id}_txtComments"] = contents;
                                    }
                                    if (settings.GetBooleanValue("checkBox4"))
                                    {
                                        var context = new ScriptActionContext();
                                        settings.DeleteValue("txtComments", content);
                                        action.Json = settings.GetJsonString();
                                        context.Update(action);
                                    }
                                }

                            }
                            content = SubdyHelper.SpinText(content);
                        }
                        if (isCommentMedia && _data.ContainsKey($"{action.Id}_txtPathAnh") && _data[$"{action.Id}_txtPathAnh"].Any())
                        {
                            lock (Globals.Lock)
                            {
                                var images = _data[$"{action.Id}_txtPathAnh"];
                                image = SubdyHelper.GetStringRandom(images);
                                if (settings.GetBooleanValue("checkBox3"))
                                {
                                    images.Remove(image);
                                    File.Delete(image);
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(content) || File.Exists(image))
                        {
                            string message = await CommentAction(content, image, isNeuBatComment, isFollowersComment);
                            await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(3, 7), $"{message}, đợi {{time}}s...", 2);
                        }
                    }
                }

                string contentShare = "";
                if (isContent)
                {
                    if (_data.ContainsKey($"{action.Id}_txtLinks"))
                    {
                        lock (Globals.Lock)
                        {
                            var contents = _data[$"{action.Id}_txtLinks"];
                            if (contents.Any())
                            {
                                contentShare = SubdyHelper.GetStringRandom(contents);
                                if (settings.GetBooleanValue("ckbTuDongXoaNoiDung"))
                                {
                                    contents.Remove(contentShare);
                                    _data[$"{action.Id}_txtLinks"] = contents;
                                    var context = new ScriptActionContext();
                                    settings.DeleteValue("txtLinks", contentShare);
                                    action.Json = settings.GetJsonString();
                                    context.Update(action);
                                }
                            }

                        }
                        contentShare = SubdyHelper.SpinText(contentShare);
                    }
                }
                if (shareNewfeed)
                {
                    if (!_client.ElementWithAttributes(new List<string> { "//*[@content-desc=\"Share\"]", "//*[@text=\"Share\"]" }, 10, "", true))
                    {
                        continue;
                    }
                    _client.ElementWithAttributes(new List<string> { "//*[@class=\"android.widget.EditText\"]", "//*[@text=\"Say something about this…\"]" }, 10, "", true);
                    Thread.Sleep(4000);
                    if (!string.IsNullOrEmpty(contentShare))
                    {
                        _client.ADBKeyboardService.Input(contentShare);
                        _client.ADB.Shell("input keyevent 62");
                    }

                    if (isNeuBat)
                    {
                        _mainService.SetStatus($"Đang gắn thẻ nổi bật...", 2);
                        List<string> neubats = new List<string> { "@highlight", "@neu" };
                        foreach (var item in neubats)
                        {
                            bool click = false;
                            var count = item.Length;
                            _client.ADB.Shell("input keyevent 62");
                            foreach (char c in item)
                            {
                                _client.ADBKeyboardService.Input(c.ToString(), false);
                                if (_client.ElementWithAttributes(new List<string> { "//*[@text=\"@highlight\"]", "//*[@text=\"@nêu bật\"]" }, 0))
                                {
                                    click = true;
                                    break;
                                }
                            }
                            if (!click)
                            {
                                for (int i = 0; i < count + 1; i++)
                                {
                                    _client.Shell("input keyevent KEYCODE_DEL");
                                    Thread.Sleep(50);
                                }
                            }
                            if (click)
                            {
                                break;
                            }

                        }
                    }
                    if (isFollowers)
                    {
                        _mainService.SetStatus($"Đang gắn thẻ người theo dõi...", 2);

                        List<string> neubats = new List<string> { "@followers", "@nguoi" };
                        foreach (var item in neubats)
                        {
                            bool click = false;
                            var count = item.Length;
                            _client.ADB.Shell("input keyevent 62");
                            foreach (char c in item)
                            {
                                _client.ADBKeyboardService.Input(c.ToString(), false);
                                if (_client.ElementWithAttributes(new List<string> { "//*[@text=\"@followers\"]", "//*[@text=\"@người theo dõi\"]" }, 0))
                                {
                                    click = true;
                                    break;
                                }
                            }
                            if (!click)
                            {
                                for (int i = 0; i < count + 1; i++)
                                {
                                    _client.Shell("input keyevent KEYCODE_DEL");
                                    Thread.Sleep(50);
                                }
                            }
                            if (click)
                            {
                                break;
                            }

                        }
                    }
                    _client.ElementWithAttributes(new List<string> { "//*[@text=\"Share now\"]", "//*[@content-desc=\"Share now\"]" }, 10, "", true);
                    await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(5, 10), $"Đã share bài viết {uidPost} lên tường, đợi {{time}}s...", 2);
                }
                if (shareGroup && countGroup > 0)
                {
                    if (!_client.ElementWithAttributes(new List<string> { "//*[@content-desc=\"Share\"]", "//*[@text=\"Share\"]" }, 10, "", true))
                    {
                        continue;
                    }
                    if (_client.ElementWithAttributes(new List<string> { "//*[@content-desc=\"Group\"]", "//*[@text=\"Group\"]" }, 10, "", true))
                    {
                        string search = SubdyHelper.GetStringRandom(groupNames);

                        if (!string.IsNullOrEmpty(search))
                        {
                            _client.ElementWithAttributes(new List<string> { "//*[@class=\"android.widget.EditText\"]", "//*[@text=\"Search\"]" }, 10, "", true);
                            _client.ADBKeyboardService.Input(search);
                            Thread.Sleep(2000);
                            string xpath = "//node[contains(@class, 'android.view.ViewGroup') and @content-desc and string-length(@text) > 0 and " +
                             "contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'group title') and " +
                             "@visible-to-user='true']";
                            var nodes = _client.FindElementsNotToLower(10, "", xpath);
                            if (nodes.Any())
                            {
                                int countGroupsShare = 1;
                                List<string> olds = new List<string>();
                                while (!_mainService._ct.IsCancellationRequested)
                                {
                                    if (refail > 5 || countGroupsShare > countGroup)
                                    {
                                        break;
                                    }
                                    nodes = _client.FindElementsNotToLower(10, "", xpath);
                                    if (!nodes.Any())
                                    {
                                        ScrollScreen(1, 1);
                                        refail++;
                                        continue;
                                    }

                                    foreach (var node in nodes)
                                    {
                                        var info = _client.ExtractNodeInfo(node.OuterXml);
                                        if (info.ContainsKey("bounds") && !olds.Contains(info["content-desc"]))
                                        {
                                            refail = 0;
                                            var point = new RectangleArea(info["bounds"]).RandomPoint();
                                            _client.Click(point.X, point.Y);
                                            olds.Add(info["content-desc"]);

                                            _client.ElementWithAttributes(new List<string> { "//*[@class=\"android.widget.EditText\"]", "//*[@text=\"Write something…\"]" }, 10, "", true);
                                            Thread.Sleep(4000);
                                            if (!string.IsNullOrEmpty(contentShare))
                                            {
                                                _client.ADBKeyboardService.Input(contentShare);
                                                _client.ADB.Shell("input keyevent 62");
                                            }
                                            if (isNeuBat)
                                            {
                                                _mainService.SetStatus($"Đang gắn thẻ nổi bật...", 2);
                                                List<string> neubats = new List<string> { "@highlight", "@neu" };
                                                foreach (var item in neubats)
                                                {
                                                    bool click = false;
                                                    var count = item.Length;
                                                    _client.ADB.Shell("input keyevent 62");
                                                    foreach (char c in item)
                                                    {
                                                        _client.ADBKeyboardService.Input(c.ToString(), false);
                                                        if (_client.ElementWithAttributes(new List<string> { "//*[@text=\"@highlight\"]", "//*[@text=\"@nêu bật\"]" }, 0))
                                                        {
                                                            click = true;
                                                            break;
                                                        }
                                                    }
                                                    if (!click)
                                                    {
                                                        for (int i = 0; i < count + 1; i++)
                                                        {
                                                            _client.Shell("input keyevent KEYCODE_DEL");
                                                            Thread.Sleep(50);
                                                        }
                                                    }
                                                    if (click)
                                                    {
                                                        break;
                                                    }

                                                }
                                            }
                                            if (isFollowers)
                                            {
                                                _mainService.SetStatus($"Đang gắn thẻ người theo dõi...", 2);

                                                List<string> neubats = new List<string> { "@followers", "@nguoi" };
                                                foreach (var item in neubats)
                                                {
                                                    bool click = false;
                                                    var count = item.Length;
                                                    _client.ADB.Shell("input keyevent 62");
                                                    foreach (char c in item)
                                                    {
                                                        _client.ADBKeyboardService.Input(c.ToString(), false);
                                                        if (_client.ElementWithAttributes(new List<string> { "//*[@text=\"@followers\"]", "//*[@text=\"@người theo dõi\"]" }, 0))
                                                        {
                                                            click = true;
                                                            break;
                                                        }
                                                    }
                                                    if (!click)
                                                    {
                                                        for (int i = 0; i < count + 1; i++)
                                                        {
                                                            _client.Shell("input keyevent KEYCODE_DEL");
                                                            Thread.Sleep(50);
                                                        }
                                                    }
                                                    if (click)
                                                    {
                                                        break;
                                                    }

                                                }
                                            }
                                            _client.ElementWithAttributes(new List<string> { "//*[@text=\"POST\"]", "//*[@content-desc=\"POST\"]" }, 10, "", true);
                                            await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(5, 10), $"Đã share bài viết {uidPost} vào nhóm {info["content-desc"]}, đợi {{time}}s...", 2);
                                            countGroupsShare++;
                                        }
                                    }
                                    ScrollScreen(1, 1);
                                    refail++;
                                }
                            }

                        }
                        _client.ATX.Press(PressKey.Back);

                    }
                }
                if (shareFriend && countFriend > 0)
                {
                    if (!_client.ElementWithAttributes(new List<string> { "//*[@content-desc=\"Share\"]", "//*[@text=\"Share\"]" }, 10, "", true))
                    {
                        continue;
                    }
                    if (_client.ElementWithAttributes(new List<string> { "//*[@content-desc=\"Send in Messenger\"]", "//*[@text=\"Send in Messenger\"]" }, 10, "", false))
                    {
                        string xpath = "//node[contains(@class, 'android.widget.Button') and @content-desc and string-length(@content-desc) > 0 and " +
  "contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'group title') and " +
  "@visible-to-user='true']";
                        var nodes = _client.FindElementsNotToLower(10, "", xpath);
                        if (nodes.Any())
                        {
                            int countGroupsShare = 1;
                            List<string> olds = new List<string>();
                            while (!_mainService._ct.IsCancellationRequested)
                            {
                                if (refail > 5 || countGroupsShare > countGroup)
                                {
                                    break;
                                }
                                nodes = _client.FindElementsNotToLower(10, "", xpath);
                                if (!nodes.Any())
                                {
                                    ScrollScreen(1, 1);
                                    refail++;
                                    continue;
                                }

                                foreach (var node in nodes)
                                {
                                    var info = _client.ExtractNodeInfo(node.OuterXml);
                                    if (info.ContainsKey("bounds") && !olds.Contains(info["content-desc"]))
                                    {
                                        refail = 0;
                                        var point = new RectangleArea(info["bounds"]).RandomPoint();
                                        _client.Click(point.X, point.Y);
                                        olds.Add(info["content-desc"]);

                                        _client.ElementWithAttributes(new List<string> { "//*[@class=\"android.widget.EditText\"]", "//*[@text=\"Write something…\"]" }, 10, "", true);
                                        Thread.Sleep(4000);
                                        if (!string.IsNullOrEmpty(contentShare))
                                        {
                                            _client.ADBKeyboardService.Input(contentShare);
                                            _client.ADB.Shell("input keyevent 62");
                                        }
                                        _client.ElementWithAttributes(new List<string> { "//*[@text=\"POST\"]", "//*[@content-desc=\"POST\"]" }, 10, "", true);
                                        await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(5, 10), $"Đã share bài viết {uidPost} vào nhóm {info["content-desc"]}, đợi {{time}}s...", 2);

                                    }
                                }
                                ScrollScreen(1, 1);
                                refail++;
                            }
                        }
                        _client.ATX.Press(PressKey.Back);

                    }
                }
            }



            return shareWallCount + shareGroupCount + shareFriendCount;
        }
        public int JoinSuggestedFacebookGroups(int accountId, string statusPrefix, JsonHelper settings, string actionTitle)
        {
            int minGroups = settings.GetIntType("nudSoLuongFrom");
            int maxGroups = settings.GetIntType("nudSoLuongTo");
            int minDelay = settings.GetIntType("nudDelayFrom");
            int maxDelay = settings.GetIntType("nudDelayTo");
            bool autoAnswerQuestions = settings.GetBooleanValue("ckbTuDongTraLoiCauHoi");
            List<string> answerList = settings.GetValuesList("txtCauTraLoi");
            int joinedCount = 0;
            try
            {
                string statusText = statusPrefix + "Đang " + actionTitle + ": ";
                int targetCount = SubdyHelper.RandomValue(minGroups, maxGroups + 1);
                if (targetCount != 0)
                {
                    while (true)
                    {
                        SetStatusAccount(accountId, statusText + "Go to Nhóm gợi ý...");
                        if (!OpenFacebookLink(accountId, statusText, "fb://faceweb/f?href=https://m.facebook.com/groups_browse/see_all/?category_id=212609529249058"))
                        {
                            break;
                        }
                        List<string> joinElements = new List<string>();
                        for (int i = 0; i < targetCount + 10; i++)
                        {
                            string joinElement;
                            string submitElement;
                            switch (Login())
                            {
                                case 0:
                                    SetStatusAccount(accountId, statusText + $"({joinedCount}/{targetCount}), Find Join...");
                                    joinElements = _client.FindBounds("", "//android.view.ViewGroup[@content-desc='Join']", 5);
                                    SetStatusAccount(accountId, statusText + $"({joinedCount}/{targetCount}), Find Join: " + joinElements.Count);
                                    if (joinElements.Count != 0)
                                    {
                                        goto FoundJoinElement;
                                    }
                                    for (int j = 0; j < 10; j++)
                                    {
                                        SetStatusAccount(accountId, statusText + $"({joinedCount}/{targetCount}), Scroll...");
                                        if (ScrollScreen())
                                        {
                                            break;
                                        }
                                        SetStatusAccount(accountId, statusText + $"({joinedCount}/{targetCount}), Find Join...");
                                        joinElements = _client.FindBounds("", "//android.view.ViewGroup[@content-desc='Join']", 5);
                                        SetStatusAccount(accountId, statusText + $"({joinedCount}/{targetCount}), Find Join: " + joinElements.Count);
                                        if (joinElements.Count > 0)
                                        {
                                            break;
                                        }
                                    }
                                    if (joinElements.Count != 0)
                                    {
                                        goto FoundJoinElement;
                                    }
                                    goto finish;
                                case 1:
                                    break;
                                default:
                                    goto finish;
                                FoundJoinElement:
                                    joinElement = SubdyHelper.GetStringRandom(joinElements);
                                    joinElements.Remove(joinElement);
                                    SetStatusAccount(accountId, statusText + $"({joinedCount}/{targetCount}), Tap Join...");
                                    var point = new RectangleArea(joinElement).GetCenterPoint();
                                    if (!_client.Click(point.X, point.Y))
                                    {
                                        continue;
                                    }
                                    submitElement = "";
                                    SetStatusAccount(accountId, statusText + $"({joinedCount}/{targetCount}), Find Submit...");
                                    if (_client.ElementWithAttributes("//android.view.ViewGroup[@content-desc=\"Submit\"]", 5, submitElement))
                                    {
                                        if (autoAnswerQuestions)
                                        {
                                            AutoAnswerGroupQuestions(accountId, statusText + $"({joinedCount}/{targetCount}), ", answerList);
                                        }
                                        else
                                        {
                                            SetStatusAccount(accountId, statusText + $"({joinedCount}/{targetCount}), Back...");
                                            if (!_client.ElementWithAttributes("//android.view.ViewGroup[@content-desc=\"Back\"]", 1, submitElement))
                                            {
                                                _client.ATX.Press(PressKey.Back);
                                            }
                                            submitElement = "";
                                            SetStatusAccount(accountId, statusText + $"({joinedCount}/{targetCount}), Find Exit...");
                                            if (_client.ElementWithAttributes("//android.widget.TextView[@text=\"Exit without answering?\"]"))
                                            {
                                                SetStatusAccount(accountId, statusText + $"({joinedCount}/{targetCount}), Tap Exit...");
                                                _client.ElementWithAttributes("//android.widget.Button[@text=\"EXIT\"]", 1, submitElement);
                                                _client.Delay(1, 2);
                                            }
                                        }
                                        SetStatusAccount(accountId, statusText + $"({joinedCount}/{targetCount}), đợi {{time}}s...", SubdyHelper.RandomValue(minDelay, maxDelay + 1));
                                    }
                                    if (_client.FindBounds("", new List<string> { "//android.view.ViewGroup[@content-desc=\"Cancel request\"]", "//android.widget.Button[@content-desc=\"Member tools\"]" }, 1).Count > 0)
                                    {
                                        SetStatusAccount(accountId, statusText + $"({joinedCount}/{targetCount}), Back...");
                                        _client.ATX.Press(PressKey.Back);
                                    }
                                    joinedCount++;
                                    if (joinedCount < targetCount)
                                    {
                                        continue;
                                    }
                                    SetStatusAccount(accountId, statusText + $"({joinedCount}/{targetCount}), Done!");
                                    goto finish;
                            }
                            goto retryLoop;
                        retryLoop:
                            continue;
                        }
                        break;
                    finish:
                        break;
                    }
                }
            }
            catch
            {
                joinedCount = -1;
            }
            return joinedCount;
        }
        public int HDAddMail(ref int resultCode, ref int primaryStatus, ref int addMailStatus, int accountId, string statusPrefix, JsonHelper settings, string groupKey, string actionTitle)
        {
            string password = "";
            if (string.IsNullOrEmpty(password))
            {
                resultCode = 8; // Password missing
                return 0;
            }

            // GetAccountProperty(accountId, "cId");
            string statusText = statusPrefix + "Đang " + actionTitle + ": ";
            SetStatusAccount(accountId, statusText + "Đang chạy...");

            bool shouldAddMail = settings.GetBooleanValue("ckbAddMail");
            int addMailType = settings.GetIntType("typeAddMail");
            int mailType = settings.GetIntType("typeMail");
            List<string> mailDomains = settings.GetValuesList("lstMailDomain");
            List<string> domainList = settings.GetValuesList("lstDomain");
            bool setPrimaryMail = shouldAddMail && settings.GetBooleanValue("ckbSetPrimaryMail");
            bool shouldRemoveMail = settings.GetBooleanValue("ckbRemoveMail");

            string selectedMailDomain = mailDomains.Count > 0
                ? mailDomains.OrderBy(_ => Guid.NewGuid()).First()
                : "";
            string selectedDomain = domainList.Count > 0
                ? domainList.OrderBy(_ => Guid.NewGuid()).First()
                : "";

            // Remove mail logic
            if (shouldRemoveMail)
            {
                addMailStatus = 2;
                int retryRemove = 0;
                int maxRetryRemove = 6;

                while (OpenFacebookLink(accountId, statusText, "fb://notification_settings_email"))
                {
                    int tickCount = Environment.TickCount;
                    string xmlSource = "";
                    int menuCount = 0;
                    while (true)
                    {
                        xmlSource = _client.GetXMLSource();
                        string element = _client.FindElement(xmlSource, new List<string> {
                        "//android.widget.ProgressBar", "//*[@text='Tap to retry']", "//*[@content-desc='menu']", "//*[@text='Remove']", "//*[@text='YES']", "//*[@content-desc='+ ADD EMAIL']"
                }, 1);

                        switch (element)
                        {
                            case "//*[@text='Remove']":
                            case "//*[@text='YES']":
                                SetStatusAccount(accountId, statusText + "Tap " + element + "...");
                                _client.ElementWithAttributes(element, 1, xmlSource);
                                break;
                            case "//*[@text='Tap to retry']":
                                if (retryRemove < maxRetryRemove)
                                {
                                    retryRemove++;
                                    ScrollScreen(-1);
                                }
                                break;
                            case "//android.widget.ProgressBar":
                                SetStatusAccount(accountId, statusText + "Loading...");
                                if (!WaitForPostComplete(15))
                                {
                                    _client.ATX.Press(PressKey.Back);

                                }
                                break;
                            case "//*[@content-desc='menu']":
                                menuCount = _client.FindElements(0, xmlSource, element).Count;
                                SetStatusAccount(accountId, statusText + "Tap " + element + "...");
                                _client.ElementWithAttributes(element, 1, xmlSource);
                                _client.Delay(1);
                                _client.ElementWithAttributes("//*[@text='Make primary']", 10, "");
                                break;
                            case "//*[@content-desc='+ ADD EMAIL']":
                                addMailStatus = 1;
                                break;
                            default:
                                SetStatusAccount(accountId, statusText + "Scroll...");
                                if (ScrollScreen())
                                {
                                    int loginResult = Login();
                                    if (loginResult == 1) break;
                                    if (loginResult != 0) return 0;
                                    List<string> keywords = ExtractTexts();
                                    if (keywords.Count == 2 && keywords.Contains("back") && string.Join("|", keywords).Contains("email"))
                                    {
                                        _client.ATX.Press(PressKey.Back);
                                        break;
                                    }
                                }
                                break;
                        }
                        _client.Delay(2);
                        if (Environment.TickCount - tickCount >= 600000)
                            break;
                    }
                }
            }

            // Add mail logic
            if (shouldAddMail && primaryStatus != 1)
            {
                resultCode = 2;
                string email = "";
                string emailPassword = "";
                string xmlSource = "";
                string confirmCode = "";
                bool confirmed = false;
                int resendCount = 0, maxResend = 2;
                int retryCount = 0, maxRetry = 6;

                if (addMailType == 0)
                {
                    lock (dictionary_6)
                    {
                        while (email == "")
                        {
                            if (dictionary_6[groupKey].Count > 0)
                            {
                                int index = new Random().Next(0, dictionary_6[groupKey].Count);
                                string[] arr = dictionary_6[groupKey][index].Split('|');
                                if (arr.Length > 1)
                                {
                                    email = arr[0].Trim().ToLower();
                                    emailPassword = arr[1].Trim();
                                }
                                dictionary_6[groupKey].RemoveAt(index);
                            }
                            else
                            {
                                resultCode = 7;
                                return 0;
                            }
                        }
                    }
                }
                else if (addMailType == 1)
                {
                    email = SubdyHelper.RemoveDiacritics(
                        SubdyHelper.FirstnameVN.OrderBy(_ => Guid.NewGuid()).First()
                        + SubdyHelper.LastnameVN.OrderBy(_ => Guid.NewGuid()).First()
                    ).Replace(" ", "").ToLower() + SubdyHelper.RandomString(length: 6) + Regex.Match(selectedMailDomain, "@\\w+.\\w+").Value;
                }
                else if (addMailType == 2)
                {
                    //email = new GeneratorEmail { string_1 = selectedDomain }.E28B883B();
                }

                while (OpenFacebookLink(accountId, statusText, "https://m.facebook.com/settings/email/add"))
                {
                    int tickCount = Environment.TickCount;
                    do
                    {
                        xmlSource = _client.GetXMLSource();
                        string element = _client.FindElement(xmlSource, new List<string> {
                    "//android.widget.ProgressBar", "//*[@text='Tap to retry']", "//android.widget.Button[@text='Add Email']", "//*[@text='Confirm Email Address' or @content-desc='Confirm Email Address']", "//*[@text='Enter Confirmation Code']", "//*[@content-desc='Next']", "//*[@text='Make primary']", "//*[contains(@text,'please re-enter your password')]/parent::*//android.widget.EditText"
                }, 1);

                        switch (element)
                        {
                            case "//android.widget.Button[@text='Add Email']":
                                _client.SendTextSlow("//android.widget.EditText", email);
                                _client.Delay(1);
                                _client.SendTextSlow("//*[contains(@text,'please enter your Facebook password')]/parent::*//android.widget.EditText", password);
                                _client.Delay(1);
                                _client.ElementWithAttributes(element, 1, xmlSource);
                                break;
                            case "//*[@text='Confirm Email Address' or @content-desc='Confirm Email Address']":
                                SetStatusAccount(accountId, statusText + "Tap " + element + "...");
                                _client.ElementWithAttributes(element, 1, xmlSource);
                                break;
                            case "//*[@text='Enter Confirmation Code']":
                                string otp = "";
                                if (_client.ElementWithAttributes("//*[@text='Confirm']/parent::*/parent::*//android.widget.EditText", 1, xmlSource, false))
                                {
                                    SetStatusAccount(accountId, statusText + "Get otp...");
                                    switch (mailType)
                                    {
                                        //case 2: otp = new GeneratorEmail(email).method_0(0, 120); break;
                                        //case 1: otp = EmailHelper.D8097D8F(0, selectedMailDomain, email); break;
                                        //case 0:
                                        //    otp = ImapHelper.GetOtpFromMail(0, email, emailPassword, 60, "", "");
                                        //    if (otp == "") otp = EmailHelper.smethod_2("https://volamtuan.pro", 0, email, emailPassword);
                                        //    break;
                                    }
                                    if (otp == "not connect" || otp == "fail")
                                    {
                                        resultCode = 5;
                                        break;
                                    }
                                    otp = Regex.Match(otp, "c=(.*?)&").Groups[1].Value;
                                    if (string.IsNullOrEmpty(otp))
                                    {
                                        resultCode = 4;
                                        break;
                                    }
                                    SetStatusAccount(accountId, statusText + "Get otp: " + otp);
                                    _client.SendTextSlow("//*[@text='Confirm']/parent::*/parent::*//android.widget.EditText", otp);
                                    _client.Delay(1);
                                    _client.ElementWithAttributes("//*[@text='Confirm']", 1, xmlSource);
                                    confirmed = true;
                                }
                                break;
                            case "//android.widget.ProgressBar":
                                SetStatusAccount(accountId, statusText + "Loading...");
                                if (!WaitForPostComplete(15))
                                {
                                    _client.ATX.Press(PressKey.Back);
                                }
                                break;
                            case "//*[contains(@text,'please re-enter your password')]/parent::*//android.widget.EditText":
                                if (!ContainsAnyKeyword(xmlSource, "The password you entered was incorrect"))
                                {
                                    _client.SendTextSlow("//android.widget.EditText", password);
                                    _client.Delay(1);
                                    _client.ElementWithAttributes("//*[@text='Continue']", 1, xmlSource);
                                }
                                else resultCode = 3;
                                break;
                            case "//*[@text='Tap to retry']":
                                if (retryCount < maxRetry)
                                {
                                    retryCount++;
                                    ScrollScreen(-1);
                                }
                                break;
                        }
                        _client.Delay(2);
                    }
                    while (Environment.TickCount - tickCount < 600000);
                }

                // Set as primary mail
                if (confirmed)
                {
                    resultCode = 1;
                    // method_114(accountId, "cEmail", email, "email");
                    //   method_114(accountId, "cPassMail", emailPassword, "passmail");
                    if (setPrimaryMail)
                    {
                        primaryStatus = 2;
                        if (OpenFacebookLink(accountId, statusText, "fb://notification_settings_email"))
                        {
                            if (_client.ElementWithAttributes("//*[@content-desc='menu']", 10, "") && _client.ElementWithAttributes("//*[@text='Make primary']", 10, ""))
                            {
                                _client.Delay(2, 3);
                                primaryStatus = 1;
                            }
                            else
                            {
                                primaryStatus = 1;
                                addMailStatus = 1;
                            }
                        }
                    }
                }
            }

            return 0;
        }
        public async Task<int> HDDangStory(JsonHelper settings, ScriptAction action)
        {
            int targetCount = SubdyHelper.RandomValue(settings.GetIntType("C913DC8A", 1), settings.GetIntType("F391713F", 3) + 1);
            int delayFrom = settings.GetIntType("nudKhoangCachFrom", 5);
            int delayTo = settings.GetIntType("nudKhoangCachTo", 10);
            bool isPublic = settings.GetBooleanValue("ckbPublic");
            bool isText = settings.GetBooleanValue("rbDangText");
            bool isMedia = settings.GetBooleanValue("rbDangAnhVideo");
            bool isMusic = settings.GetBooleanValue("rbDangNhac");
            if (!isText && !isMedia && !isMusic) isText = true;

            int postType = isText ? 0 : (isMusic ? 1 : 2);

            bool deleteContent = settings.GetBooleanValue("ckbXoaNguyenLieuDaDung");
            bool removeContent = !settings.GetBooleanValue("checkBox1");
            bool deleteMedia = settings.GetBooleanValue("checkBox4");

            bool musicRandom = settings.GetBooleanValue("radioButton3");
            List<string> musicKeyword = settings.GetValuesList("textBox1");
            List<string> musicQueue = new List<string>(musicKeyword);

            bool musicCoAnh = isMusic && settings.GetBooleanValue("ckbCoAnh");
            bool deleteMusicAnh = settings.GetBooleanValue("ckbXoaAnhDaDang");
            List<string> musicImagePool = new List<string>();
            if (musicCoAnh)
            {
                string musicImgFolder = settings.GetValue("txtPathAnhNhac");
                if (Directory.Exists(musicImgFolder))
                {
                    musicImagePool = SubdyHelper.GetMedias(musicImgFolder);
                }
            }

            bool useBackground = settings.GetBooleanValue("ckbSuDungBackground");
            bool tagNeuBat = settings.GetBooleanValue("ckbTagNeuBat");
            bool tagMoiNguoi = settings.GetBooleanValue("ckbTagMoiNguoi");

            int successCount = 0;
            int refail = 0;
            int failCount = 0;
            Stopwatch shareStopwatch = new Stopwatch();
            int shareIntervalMilliseconds = 0;
            bool hasSharedStory = false;
            UpdateStoryStats(successCount, failCount);

            while (!_mainService._ct.IsCancellationRequested)
            {
                if (successCount >= targetCount) break;
                if (refail > 5) break;

                // Khoảng cách cấu hình là: BẮT ĐẦU bấm Share story trước → BẮT ĐẦU
                // dựng story kế tiếp. Không chờ Facebook tải/hoàn tất story trước và không
                // cộng dồn thời gian upload, chọn nhạc hay dựng UI story hiện tại.
                if (hasSharedStory && shareIntervalMilliseconds > 0)
                {
                    int elapsedMilliseconds = shareStopwatch.IsRunning
                        ? (int)Math.Min(int.MaxValue, shareStopwatch.ElapsedMilliseconds)
                        : 0;
                    int remainingMilliseconds = shareIntervalMilliseconds - elapsedMilliseconds;
                    if (remainingMilliseconds > 0)
                    {
                        int remainingSeconds = (remainingMilliseconds + 999) / 1000;
                        _mainService.SetStatus($"({successCount + 1}/{targetCount}), Đợi {remainingSeconds}s để bắt đầu dựng story...", 2);
                        await Task.Delay(remainingMilliseconds, _mainService._ct);
                    }
                }

                string content = string.Empty;
                if (isText && _data.ContainsKey($"{action.Id}_txtLinks") && _data[$"{action.Id}_txtLinks"].Any())
                {
                    lock (Globals.Lock)
                    {
                        content = SubdyHelper.GetStringRandom(_data[$"{action.Id}_txtLinks"]);
                        if (removeContent)
                        {
                            _data[$"{action.Id}_txtLinks"].Remove(content);
                        }
                        if (deleteContent)
                        {
                            var context = new ScriptActionContext();
                            settings.DeleteValue("txtLinks", content);
                            action.Json = settings.GetJsonString();
                            context.Update(action);
                        }
                    }
                }

                string mediaPath = string.Empty;
                if (isMedia)
                {
                    lock (Globals.Lock)
                    {
                        if (_data.ContainsKey($"{action.Id}_txtPathAnh") && _data[$"{action.Id}_txtPathAnh"].Any())
                        {
                            mediaPath = SubdyHelper.GetStringRandom(_data[$"{action.Id}_txtPathAnh"]);
                            if (deleteMedia)
                            {
                                _data[$"{action.Id}_txtPathAnh"].Remove(mediaPath);
                            }
                        }
                    }
                    if (string.IsNullOrEmpty(mediaPath) || !File.Exists(mediaPath))
                    {
                        refail++;
                        failCount++;
                        UpdateStoryStats(successCount, failCount);
                        continue;
                    }
                }

                string musicImagePath = string.Empty;
                if (isMusic && musicCoAnh)
                {
                    lock (Globals.Lock)
                    {
                        if (musicImagePool.Any())
                        {
                            musicImagePath = SubdyHelper.GetStringRandom(musicImagePool);
                            if (deleteMusicAnh)
                            {
                                musicImagePool.Remove(musicImagePath);
                            }
                        }
                    }
                }

                try
                {
                    List<string> fileMedia = new List<string>();
                    _mainService.SetStatus($"({successCount + 1}/{targetCount}), Mở Facebook...", 2);
                    if (isMedia)
                    {
                        _mainService.SetStatus($"({successCount + 1}/{targetCount}), Upload media...", 2);
                        fileMedia.AddRange(UploadMediaFiles(new List<string> { mediaPath }));
                    }
                    else if (isMusic && musicCoAnh && !string.IsNullOrEmpty(musicImagePath) && File.Exists(musicImagePath))
                    {
                        _mainService.SetStatus($"({successCount + 1}/{targetCount}), Upload ảnh nhạc...", 2);
                        fileMedia.AddRange(UploadMediaFiles(new List<string> { musicImagePath }));
                    }
                    // Chỉ mở Timeline khi CHƯA ở đó. Sau khi Share story trước, app đã
                    // được đưa về Timeline; upload media không đổi app foreground nên lần
                    // mở này thường thừa → bỏ deeplink + reload (~3-5s/story).
                    if (IsOnFacebookTimeline())
                    {
                        _mainService.SetStatus($"({successCount + 1}/{targetCount}), Đã ở Timeline...", 2);
                    }
                    else
                    {
                        OpenFacebookTimeline();
                    }

                    int tickCount = Environment.TickCount;
                    int timeoutSeconds = 300;
                    bool hasClickedAddToStory = false;
                    bool postSuccess = false;
                    bool textFilled = false;
                    bool musicSelected = false;
                    bool privacyConfigured = false;

                    List<string> xpaths;
                    if (postType == 0)
                    {
                        // THỨ TỰ LIST = THỨ TỰ ƯU TIÊN: ADBClient.FindElement trả về XPATH KHỚP
                        // ĐẦU TIÊN theo thứ tự list. 'Add to story' là THẺ "+" bấm được thật, phải
                        // đứng TRƯỚC 'Stories' trần — vì 'Stories' còn là TIÊU ĐỀ MỤC trên feed và
                        // khớp cả node KHUNG chứa khay story (ElementWithAttributes chọn node diện
                        // tích lớn nhất) -> đứng trước thì nó THẮNG và tool tap vào tiêu đề/khung
                        // thay vì thẻ tạo story = "tab nhầm chỗ".
                        // CHỈ DỜI XUỐNG, KHÔNG XOÁ: màn nào không có 'Add to story' thì FindElement
                        // rơi xuống 'Stories' y như hành vi cũ, không mất lưới an toàn.
                        xpaths = new List<string>
                        {
                            "//*[@content-desc='Add to story']",
                            "//*[@text='Stories']",
                            "//*[@content-desc='Stories']",
                            "//*[@content-desc='Stories']//androidx.recyclerview.widget.RecyclerView/child::*/child::*",
                            "//*[@class='androidx.recyclerview.widget.RecyclerView']/descendant::android.widget.Button[@clickable='true' and string-length(@content-desc)>0]",
                            "//*[@content-desc='Start a Text story']",
                            "//android.widget.EditText[@content-desc='Text field']",
                            "//*[@text='Privacy' or @content-desc='Privacy']",
                            "//*[@text='Public' or @content-desc='Public']",
                            "//*[@class='android.widget.Button' and (starts-with(@text,'Share') or starts-with(@content-desc,'Share'))]"
                        };
                        if (useBackground)
                        {
                            xpaths.Insert(3, "//*[@content-desc='Select background']");
                            xpaths.Insert(4, "//*[contains(@content-desc,', background')]");
                            xpaths.Insert(5, "//*[@content-desc='Close background styles tray']");
                        }
                    }
                    else if (postType == 1)
                    {
                        // THỨ TỰ LIST = THỨ TỰ ƯU TIÊN (FindElement trả xpath khớp ĐẦU TIÊN).
                        // 'Create story' / 'Add to story' là nút bấm được thật -> đứng TRƯỚC
                        // 'Stories' trần (tiêu đề mục / khung chứa khay story) để không tap nhầm.
                        // CHỈ DỜI XUỐNG, KHÔNG XOÁ: giữ nguyên lưới an toàn như cũ.
                        xpaths = new List<string>
                        {
                            "//*[@text='Create story' or @content-desc='Create story']",
                            "//*[@content-desc='Add to story']",
                            "//*[@text='Stories']",
                            "//*[@content-desc='Stories']",
                            "//*[@content-desc='Stories']//androidx.recyclerview.widget.RecyclerView/child::*/child::*",
                            "//*[@class='androidx.recyclerview.widget.RecyclerView']/descendant::android.widget.Button[@clickable='true' and string-length(@content-desc)>0]",
                            "//*[@content-desc='Start a Music story']",
                             "//*[@text='Music' or @content-desc='Music']",
                            "//*[@class='android.widget.EditText' and (starts-with(@text,'Search') or starts-with(@content-desc,'Search'))]",
                            "//*[@content-desc='Song preview']",
                            "//*[contains(@text, 'Settings') or contains(@content-desc, 'Settings')]",
                            "//*[@text='Done']",
                            "//*[@text='Privacy' or @content-desc='Privacy']",
                            "//*[@text='Public' or @content-desc='Public']",
                            "//*[@class='android.widget.Button' and (starts-with(@text,'Text') or starts-with(@content-desc,'Text'))]",
                            "//*[@class='android.widget.Button' and (starts-with(@text,'Share') or starts-with(@content-desc,'Share'))]",
                            "//*[@content-desc=\"Finishing up…\"]",
                            "//android.widget.ProgressBar"
                        };
                        if (musicCoAnh)
                        {
                            // Chỉ số 3 -> 1 vì nhóm "mở khay story" đã được sắp lại. QUAN HỆ ƯU TIÊN
                            // GIỮ NGUYÊN Y HỆT bản cũ: CreateStory > Photo[last] > PhotoTakenOn >
                            // AddTo (bộ chọn ảnh vẫn đứng TRÊN 'Add to story' — nếu để AddTo lên
                            // trước, dump màn gallery còn sót node nền 'Add to story' sẽ thắng và
                            // tool tap lại khay story thay vì chọn ảnh). Thay đổi DUY NHẤT so với
                            // trước: 'Stories' trần rơi XUỐNG DƯỚI tất cả (đó chính là fix).
                            xpaths.Insert(1, "(//*[contains(@content-desc, 'Photo taken on') or contains(@text, 'Photo taken on')])[1]");
                            xpaths.Insert(1, "(//*[@content-desc='Photo'])[last()]");
                        }
                    }
                    else
                    {
                        // THỨ TỰ LIST = THỨ TỰ ƯU TIÊN (FindElement trả xpath khớp ĐẦU TIÊN).
                        // 'Create story' / 'Add to story' là nút bấm được thật -> đứng TRƯỚC
                        // 'Stories' trần (tiêu đề mục / khung chứa khay story) để không tap nhầm.
                        // CHỈ DỜI XUỐNG, KHÔNG XOÁ: giữ nguyên lưới an toàn như cũ.
                        xpaths = new List<string>
                        {
                            "//*[@text='Create story' or @content-desc='Create story']",
                            "//*[@content-desc='Add to story']",
                            "//*[@text='Stories']",
                            "//*[@content-desc='Stories']",
                            "//*[@content-desc='Stories']//androidx.recyclerview.widget.RecyclerView/child::*/child::*",
                            "//*[@content-desc='Photo' or @content-desc='Video']/*[@content-desc='Photo' or @content-desc='Video']",
                            "//*[@text='Privacy' or @content-desc='Privacy']",
                            "//*[@text='Public' or @content-desc='Public']",
                            "//*[@class='android.widget.Button' and (starts-with(@text,'Share') or starts-with(@content-desc,'Share'))]",
                            "//android.widget.ProgressBar"
                        };
                    }
                    bool isFirstLoop = true;
                    // Đứng TRƯỚC NavigationButton: trên màn "pay or consent" phần tử
                    // khớp đầu của NavigationButton là nút Continue mờ (vô tác dụng).
                    xpaths.AddRange(XpathManagerFacebook.Get(XpathType.MetaAdsConsent));
                    xpaths.AddRange(XpathManagerFacebook.Get(XpathType.NavigationButton));
                    while (!_mainService._ct.IsCancellationRequested)
                    {
                        if (Environment.TickCount - tickCount >= timeoutSeconds * 1000) break;

                        string xmlSource = _client.GetXMLSource();

                        // Quét & bấm nút Dismiss TRƯỚC khi khớp list story. List cục bộ bên trên
                        // (Stories / Create story / Privacy / Public / //android.widget.ProgressBar …)
                        // đứng TRƯỚC MetaAdsConsent và NavigationButton, và dialog Facebook là cửa sổ
                        // chồng lên màn nền nên dump bắt được node của CẢ màn nền -> một node nền khớp
                        // trước sẽ thắng, tool bấm vào nền, dialog nuốt cú chạm, lặp vô hạn và KHÔNG
                        // BAO GIỜ tới lượt xpath Dismiss. Truyền lại xmlSource nên KHÔNG tốn thêm dump.
                        if (FacebookHander.TryClickAnyDismiss(_client, xmlSource))
                        {
                            _client.Delay(1);
                            continue;
                        }

                        string foundElement = _client.FindElement(xmlSource, xpaths, 1);


                        switch (foundElement)
                        {
                            case "//*[@text='Stories']":
                            case"//*[@content-desc='Stories']":
                            case "//*[@text='Create story' or @content-desc='Create story']":
                            case "//*[@content-desc='Add to story']":
                                hasClickedAddToStory = true;
                                _mainService.SetStatus($"({successCount + 1}/{targetCount}), Tap Add to story...", 2);
                                _client.ElementWithAttributes(foundElement, 1, xmlSource);
                                break;
                            case var c when XpathManagerFacebook.Get(XpathType.MetaAdsConsent).Contains(c):
                                await FacebookHander.TryHandleMetaAdsConsentAsync(_client);
                                break;
                            case var c when XpathManagerFacebook.Get(XpathType.NavigationButton).Contains(c):
                                _client.ElementWithAttributes(c, 5);
                                break;
                            case "//*[@content-desc='Stories']//androidx.recyclerview.widget.RecyclerView/child::*/child::*":
                                {
                                    var firstDesc = _client.GetAttributeValuesFromXmlNodes(xmlSource, "(" + foundElement + ")[1]", "content-desc").FirstOrDefault() ?? "";
                                    if (firstDesc.ToLower().Contains("music"))
                                        _client.ElementWithAttributes("(" + foundElement + ")[last()]", 1, xmlSource);
                                    else
                                        _client.ElementWithAttributes("(" + foundElement + ")[1]", 1, xmlSource);
                                    break;
                                }
                            case "//*[@class='androidx.recyclerview.widget.RecyclerView']/descendant::android.widget.Button[@clickable='true' and string-length(@content-desc)>0]":
                                {
                                    var firstDesc = _client.GetAttributeValuesFromXmlNodes(xmlSource, "(" + foundElement + ")[1]", "content-desc").FirstOrDefault() ?? "";
                                    if (firstDesc.ToLower().Contains("music"))
                                        _client.ElementWithAttributes("(" + foundElement + ")[last()]", 1, xmlSource);
                                    else
                                        _client.ElementWithAttributes("(" + foundElement + ")[1]", 1, xmlSource);
                                    break;
                                }

                            case "//*[@content-desc='Start a Text story']":
                            case "//*[@content-desc='Start a Music story']":
                            case "//*[@text='Music' or @content-desc='Music']":
                                _mainService.SetStatus($"({successCount + 1}/{targetCount}), Tap {foundElement}...", 2);
                                _client.ElementWithAttributes(foundElement, 1, xmlSource);
                                break;

                            case "//*[@content-desc='Select background']":
                                _client.ElementWithAttributes(foundElement, 1, xmlSource);
                                break;

                            case "//*[contains(@content-desc,', background')]":
                                {
                                    var bgBounds = _client.FindBounds(xmlSource, foundElement, 1);
                                    if (bgBounds.Any())
                                    {
                                        var bg = new RectangleArea(bgBounds.OrderBy(_ => Guid.NewGuid()).First()).GetCenterPoint();
                                        _client.Click(bg.X, bg.Y);
                                        _client.Delay(2);
                                    }
                                    _client.ElementWithAttributes("//*[@content-desc='Close background styles tray']", 1, xmlSource);
                                    break;
                                }

                            case "//*[@content-desc='Close background styles tray']":
                                _client.ElementWithAttributes(foundElement, 1, xmlSource);
                                break;
                            case "//*[@class='android.widget.Button' and (starts-with(@text,'Text') or starts-with(@content-desc,'Text'))]":
                                {
                                    if (isText && !hasClickedAddToStory)
                                    {
                                        _mainService.SetStatus($"({successCount + 1}/{targetCount}), Tap {foundElement}...", 2);
                                        _client.ElementWithAttributes(foundElement, 1, xmlSource);
                                        _mainService.SetStatus($"({successCount + 1}/{targetCount}), Nhập nội dung...", 2);
                                        string spinText = SubdyHelper.SpinText(content);
                                        _client.SendTextSlow("//*[@class='android.widget.EditText']", spinText);
                                        _client.Delay(2);
                                        _client.ElementWithAttributes("(//android.widget.Button[@content-desc='Back']/parent::*/child::*)[last()]", 1, xmlSource);
                                        textFilled = true;
                                    }
                                    break;
                                }

                            case "//android.widget.EditText[@content-desc='Text field']":
                                if (!textFilled && !string.IsNullOrWhiteSpace(content))
                                {
                                    _mainService.SetStatus($"({successCount + 1}/{targetCount}), Nhập nội dung...", 2);
                                    string spinText = SubdyHelper.SpinText(content);
                                    _client.SendTextSlow(foundElement, spinText);
                                    _client.Delay(2);
                                    _client.ElementWithAttributes("(//android.widget.Button[@content-desc='Back']/parent::*/child::*)[last()]", 1, xmlSource);
                                    textFilled = true;
                                }
                                break;
                            case "(//*[contains(@content-desc, 'Photo taken on') or contains(@text, 'Photo taken on')])[1]":
                            case "(//*[@content-desc='Photo'])[last()]":
                                _mainService.SetStatus($"({successCount + 1}/{targetCount}), Chọn ảnh kèm nhạc...", 2);
                                _client.ElementWithAttributes(foundElement, 1, xmlSource);
                                break;
                            case "//*[@class='android.widget.EditText' and (starts-with(@text,'Search') or starts-with(@content-desc,'Search'))]":
                                if (!musicRandom && musicQueue.Any() && !musicSelected)
                                {
                                    int searchAttempt = 0;
                                    while (searchAttempt < 3 && !_client.ElementWithAttributes("//*[@content-desc='Song preview']", 10, "", false))
                                    {
                                        searchAttempt++;
                                        if (musicQueue.Count == 0) musicQueue = new List<string>(musicKeyword);
                                        string musicText = musicQueue.OrderBy(_ => Guid.NewGuid()).FirstOrDefault() ?? "";
                                        if (string.IsNullOrWhiteSpace(musicText)) break;
                                        musicText = SubdyHelper.SpinText(musicText);

                                        _mainService.SetStatus($"({successCount + 1}/{targetCount}), Tìm nhạc: {musicText}...", 2);
                                        _client.ElementWithAttributes(foundElement, 1, xmlSource);
                                        _client.SendTextSlow(foundElement, musicText + " ");
                                        _client.ATX.Press(PressKey.Enter);
                                        // Bỏ Delay(3) cứng: ElementWithAttributes bên dưới tự poll node
                                        // 'Music Track' tối đa 10s (chờ theo UI-state + fallback).
                                        if (_client.ElementWithAttributes("(//*[contains(@content-desc, 'Music Track') or contains(@text, 'Music Track')])[1]", 10, ""))
                                        {
                                            // Bỏ Delay(3) cứng: lệnh ngay sau tự poll node 'Music' tối đa 10s.
                                            _client.ElementWithAttributes("//*[@text='Music' or @content-desc='Music']", 10, "");
                                            _mainService.SetStatus($"({successCount + 1}/{targetCount}), Chon kieu Album Art...", 2);
                                            ClickRandomAlbumArtStyle();
                                            _client.ElementWithAttributes("//*[@class='android.widget.Button' and (starts-with(@text,'Done') or starts-with(@content-desc,'Done'))]", 10, "");
                                            // Bỏ Delay(3) cứng: MoveMusicStickerRandom tự poll node
                                            // 'Music sticker' tối đa 10s — node này chỉ có ở màn dựng
                                            // story nên chính là tín hiệu đã chuyển cảnh xong.
                                            MoveMusicStickerRandom();
                                            break;
                                        }
                                    }
                                    musicSelected = true;
                                }
                                else
                                {
                                    _client.ElementWithAttributes("//*[@content-desc='Song preview']", 120, "", false);
                                    musicSelected = true;
                                }
                                break;
                            case "//*[@content-desc='Song preview']":
                                {
                                    _mainService.SetStatus($"({successCount + 1}/{targetCount}), Chọn bài hát...", 2);
                                    var songBounds = _client.FindBounds(xmlSource, foundElement, 1);
                                    if (songBounds.Any())
                                    {
                                        var songPt = new RectangleArea(songBounds.First()).GetCenterPoint();
                                        _client.Click(Math.Max(songPt.X - 500, 50), songPt.Y);
                                    }
                                    for (int i = 0; i < 60; i++)
                                    {
                                        _client.Delay(2);
                                        if (_client.GetXMLSource() != xmlSource) break;
                                    }
                                    _client.ElementWithAttributes("//*[@text='Music' or @content-desc='Music']", 10, "");
                                    _mainService.SetStatus($"({successCount + 1}/{targetCount}), Chon kieu Album Art...", 2);
                                    ClickRandomAlbumArtStyle();
                                    _client.ElementWithAttributes("//*[@class='android.widget.Button' and (starts-with(@text,'Done') or starts-with(@content-desc,'Done'))]", 10, "");
                                    // Bỏ Delay(3) cứng: MoveMusicStickerRandom tự poll node 'Music
                                    // sticker' tối đa 10s (chờ theo UI-state + fallback).
                                    MoveMusicStickerRandom();
                                    break;
                                }
                            case "//*[@text='Done']":
                                _mainService.SetStatus($"({successCount + 1}/{targetCount}), Tap Done...", 2);
                                _client.ElementWithAttributes(foundElement, 1, xmlSource);
                                if (isMusic)
                                {
                                    // Bỏ Delay(3) cứng: MoveMusicStickerRandom tự poll node 'Music
                                    // sticker' tối đa 10s (chờ theo UI-state + fallback).
                                    MoveMusicStickerRandom();
                                }
                                break;
                            case "//*[contains(@text, 'Settings') or contains(@content-desc, 'Settings')]":
                                if (!isPublic)
                                {
                                    break;
                                }
                                // LUỒNG CŨ (chờ xpath 'Privacy'/'Public' có label) CHỈ còn đúng với
                                // FB bản cũ. Bản mới sheet KHÔNG label -> phải bấm row Privacy THEO
                                // HÌNH HỌC, nếu không tool kẹt: mở sheet -> chờ 10s+10s hụt -> Back
                                // systemui ĐÓNG sheet -> lặp tới timeout, không bao giờ tới 'Share'.
                                _mainService.SetStatus($"({successCount + 1}/{targetCount}), Tap Public...", 2);
                                if (!OpenStoryPrivacySheetByGeometry())
                                {
                                    // Fallback bản FB cũ: gear mở thẳng màn privacy có label.
                                    _client.ElementWithAttributes(foundElement, 1, xmlSource);
                                    _client.Delay(2);
                                    _client.ElementWithAttributes("//*[@text='Privacy' or @content-desc='Privacy']", 10, "");
                                    _client.Delay(2);
                                }
                                _client.ElementWithAttributes("//*[@text='Public' or @content-desc='Public']", 10, "");
                                _client.Delay(2);
                                var xpathsPublic = new List<string> { "//*[@text='SAVE' or @content-desc='SAVE']", "//*[@text='CHANGE' or @content-desc='CHANGE']", "//*[@text='CHANGE' or @text='SAVE'] or @content-desc='CHANGE'] or @content-desc='SAVE']", "//*[@text='Go to setting' or @content-desc='Go to setting']", "//*[@text='Add Public option' or @content-desc='Add Public option']" };
                                for(int i = 0; i < 3; i++)
                                {
                                    if (_client.ElementWithAttributes(xpathsPublic, 5, ""))
                                    {
                                        _client.Delay(2);
                                        continue;
                                    }
                                    _client.Delay(5);
                                }
                                _client.ElementWithAttributes("//*[@content-desc='Back']", 10, "");
                                _client.Delay(2);
                                // ĐÃ chốt privacy -> RÚT xpath bánh răng KHỎI list: sheet hết label
                                // nên nếu giữ, mỗi vòng lặp tool lại MỞ sheet và không bao giờ rơi
                                // xuống 'Share' (gear đứng TRƯỚC Share trong list ưu tiên).
                                privacyConfigured = true;
                                xpaths.Remove("//*[contains(@text, 'Settings') or contains(@content-desc, 'Settings')]");
                                WaitForPostComplete(60);
                                break;
                            case "//*[@text='Privacy' or @content-desc='Privacy']":
                                if (!isPublic)
                                {
                                    break;
                                }
                                if (privacyConfigured)
                                {
                                    // Privacy đã chốt ở vòng trước; node label sót lại chỉ khiến
                                    // tool mở lại màn privacy vô ích -> rút xpath khỏi list.
                                    xpaths.Remove(foundElement);
                                    break;
                                }
                                _mainService.SetStatus($"({successCount + 1}/{targetCount}), Tap Public...", 2);
                                _client.ElementWithAttributes(foundElement, 1, xmlSource);
                                _client.Delay(2);
                                if (!_client.ElementWithAttributes("//*[@text='Public' or @content-desc='Public']", 5, ""))
                                {
                                    // Sheet bản FB mới KHÔNG label: bấm row Privacy theo hình học.
                                    OpenStoryPrivacySheetByGeometry();
                                }
                                _client.ElementWithAttributes("//*[@text='Public' or @content-desc='Public']", 10, "");
                                _client.Delay(2);
                                _client.ElementWithAttributes(new List<string> { "//*[@text='SAVE' or @content-desc='SAVE']", "//*[@text='CHANGE' or @content-desc='CHANGE']", "//*[@text='CHANGE' or @text='SAVE'] or @content-desc='CHANGE'] or @content-desc='SAVE']" }, 5, xmlSource);
                                _client.Delay(2);
                                _client.ElementWithAttributes("//*[@content-desc='Back']", 10, "");
                                _client.Delay(2);
                                privacyConfigured = true;
                                xpaths.Remove("//*[contains(@text, 'Settings') or contains(@content-desc, 'Settings')]");
                                xpaths.Remove(foundElement);
                                WaitForPostComplete(60);
                                break;
                            case "//*[@text='Public' or @content-desc='Public']":
                                if (!isPublic)
                                {
                                    break;
                                }
                                _client.ElementWithAttributes("//*[@text='CHANGE' or @text='SAVE']", 5, "");
                                _client.ElementWithAttributes("//*[@content-desc='Back']", 1, xmlSource);
                                privacyConfigured = true;
                                xpaths.Remove("//*[contains(@text, 'Settings') or contains(@content-desc, 'Settings')]");
                                WaitForPostComplete(60);
                                break;
                            case "//*[@content-desc=\"Share\"]":
                            case "//*[@class='android.widget.Button' and (starts-with(@text,'Share') or starts-with(@content-desc,'Share'))]":
                                // Không chờ K ở đây. Khoảng cách "Share story này → bắt đầu dựng
                                // story kế tiếp" được canh duy nhất ở ĐẦU vòng lặp (shareStopwatch),
                                // nên mốc K luôn là khoảnh khắc gửi tap Share, không phải lúc sắp bấm.
                                successCount++;
                                _mainService.SetStatus($"({successCount + 1}/{targetCount}), Tap Share...", 2);
                                // Mốc của khoảng cách là KHOẢNH KHẮC bắt đầu gửi tap Share story này.
                                shareIntervalMilliseconds = SubdyHelper.RandomValue(
                                    Math.Max(0, Math.Min(delayFrom, delayTo)),
                                    Math.Min(999999, Math.Max(delayFrom, delayTo)) + 1) * 1000;
                                shareStopwatch.Restart();
                                _client.ElementWithAttributes(foundElement, 1, xmlSource);
                                hasSharedStory = true;
                                _client.Delay(2);
                                if (_client.ElementWithAttributes("//*[@class='android.widget.Button' and (starts-with(@text,'NOT NOW') or starts-with(@content-desc,'NOT NOW'))]", 10, ""))
                                {
                                    await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(3, 6), $"({successCount + 1}/{targetCount}), Đợi {{time}}s...", 2);
                                }
                                postSuccess = true;
                                if (hasClickedAddToStory)
                                {
                                    OpenFacebookTimeline();
                                    // Bỏ Delay(3) cứng: chờ Timeline ổn định theo UI-state,
                                    // trả về ngay khi màn hình ngừng tải (fallback 3s).
                                    WaitForUiStable(3);
                                }
                                //WaitForPostComplete(isMedia ? 300 : 60);
                                break;
                            case "//*[@content-desc=\"Finishing up…\"]":
                            case "//android.widget.ProgressBar":
                                _mainService.SetStatus($"({successCount + 1}/{targetCount}), Loading...", 2);
                                WaitForPostComplete(60);
                                break;
                            default:
                                _mainService.SetStatus($"({successCount + 1}/{targetCount}), Scroll...", 2);
                                _client.Delay(2);
                                if (!_client.IsRunningApp(PlatformModel.Facebook))
                                {
                                    await _mainService._facebookService.HanderAccount(_client, _account, 1, _mainService._ct, _mainService);
                                    isFirstLoop = false;
                                }

                                break;
                        }
                        if (postSuccess || !isFirstLoop) break;
                        if (!string.IsNullOrEmpty(foundElement) && foundElement != "//android.widget.ProgressBar")
                        {
                            xpaths.Remove(foundElement);
                        }
                        // Bỏ Delay(3) cứng: nhịp chờ giữa các lần poll UI giờ trả về NGAY
                        // khi màn hình ngừng thay đổi (fallback tối đa 3s).
                        WaitForUiStable(3);
                    }

                    if (postSuccess)
                    {
                        refail = 0;
                        if (isMedia && deleteMedia && File.Exists(mediaPath))
                        {
                            try { File.Delete(mediaPath); } catch { }
                        }
                        if (isMusic && musicCoAnh && deleteMusicAnh && File.Exists(musicImagePath))
                        {
                            try { File.Delete(musicImagePath); } catch { }
                        }
                        UpdateStoryStats(successCount, failCount);
                        // KHÔNG chờ K thêm ở đây: khoảng cách "Share → dựng story kế" đã được
                        // canh ở ĐẦU vòng lặp dựa trên mốc shareStopwatch (khoảnh khắc bấm Share).
                        DeleteMediaFiles(fileMedia);
                    }
                    else
                    {
                        refail++;
                        failCount++;
                        DeleteMediaFiles(fileMedia);
                        UpdateStoryStats(successCount, failCount);
                    }
                }
                catch
                {
                    refail++;
                    failCount++;
                    UpdateStoryStats(successCount, failCount);
                }
            }

            return successCount;
        }
        public int HDXoaSdt(int accountId, string statusPrefix, string actionTitle)
        {
            string password = "";
            if (string.IsNullOrEmpty(password))
            {
                SetStatusAccount(accountId, statusPrefix + "Không có password");
                return 0;
            }

            string statusText = statusPrefix + "Đang " + actionTitle + ": ";
            SetStatusAccount(accountId, statusText + "Đang chạy...");

            int result = 2; // Default: unknown error
            int retryScroll = 0;
            int maxScrollRetry = 2;
            int retryTapToRetry = 0;
            int maxTapToRetry = 6;

            while (OpenFacebookLink(
                accountId,
                statusText,
                "fb://facewebmodal/f?href=https://mbasic.facebook.com/settings/sms/?ref_component=mbasic_bookmark&ref_page=XMenuController"))
            {
                string xmlSource = "";
                int tickCount = Environment.TickCount;
                while (true)
                {
                    xmlSource = _client.GetXMLSource();
                    string elementXPath = _client.FindElement(xmlSource, new List<string>
            {
                "//android.widget.ProgressBar",
                "//*[@text='Tap to retry']",
                "//*[@text='Remove']",
                "//android.widget.CheckBox[@text='I understand I could lose access to my account']",
                "//*[@text='Remove phone'][@focused='false']",
                "//*[@text='Remove phone'][@focused='true']"
            }, 1);

                    switch (elementXPath)
                    {
                        case "//*[@text='Remove']":
                            if (!xmlSource.Contains("you can't delete the primary phone number if we don't have other contact info for you. Please add another phone or add an email address to your account"))
                            {
                                SetStatusAccount(accountId, statusText + "Tap " + elementXPath + "...");
                                _client.ElementWithAttributes(elementXPath, 1, xmlSource);
                                goto AfterAction;
                            }
                            result = 4; // Can't remove the only contact info
                            break;

                        default:
                            if (!ContainsAnyKeyword(xmlSource, "Add a Mobile Number", "Add Number", "Add phone number"))
                            {
                                if (!ContainsAnyKeyword(xmlSource, "This content is no longer available"))
                                {
                                    SetStatusAccount(accountId, statusText + "Scroll...");
                                    if (ScrollScreen())
                                    {
                                        switch (Login())
                                        {
                                            case 0: break;
                                            case 1: goto EndLoop;
                                            default: goto EndLoop;
                                        }
                                    }
                                    goto AfterAction;
                                }
                                goto ContentNotAvailable;
                            }
                            result = 1; // No phone to remove
                            break;

                        case "//*[@text='Tap to retry']":
                            if (retryTapToRetry >= maxTapToRetry)
                            {
                                break;
                            }
                            retryTapToRetry++;
                            ScrollScreen(-1);
                            goto AfterAction;

                        case "//android.widget.ProgressBar":
                            SetStatusAccount(accountId, statusText + "Loading...");
                            goto AfterAction;

                        case "//*[@text='Remove phone'][@focused='true']":
                            if (xmlSource.Contains("Incorrect password"))
                            {
                                result = 3; // Incorrect password
                                break;
                            }
                            goto AfterAction;

                        case "//*[@text='Remove phone'][@focused='false']":
                            _client.SendTextSlow("//android.widget.EditText", password);
                            _client.Delay(2);
                            SetStatusAccount(accountId, statusText + "Tap " + elementXPath + "...");
                            _client.ElementWithAttributes(elementXPath, 1, xmlSource);
                            goto AfterAction;

                        case "//android.widget.CheckBox[@text='I understand I could lose access to my account']":
                            _client.ElementWithAttributes("//android.widget.CheckBox[@text='I understand I could lose access to my account'][@checked='false']", 1, xmlSource);
                            _client.Delay(2);
                            _client.ElementWithAttributes("//*[@text='Remove Number']", 1, xmlSource);
                            goto AfterAction;

                        AfterAction:
                            _client.Delay(2);
                            if (Environment.TickCount - tickCount < 300000)
                            {
                                continue;
                            }
                            break;
                    }
                    break;
                }
                break;

            ContentNotAvailable:
                if (retryScroll < maxScrollRetry)
                {
                    retryScroll++;
                    continue;
                }
                result = 5; // Content not available after retries
                break;

            EndLoop:
                break;
            }
            return result;
        }
        public int HDVerifyAccount(ref int verifyResult, int accountId, string statusPrefix, JsonHelper settings, string groupKey, string actionTitle)
        {
            // Get account id and prepare status
            //  GetAccountProperty(accountId, "cId");
            string userWallId = "uid";
            string statusText = statusPrefix + "Đang " + actionTitle + ": ";
            SetStatusAccount(accountId, statusText + "Đang chạy...");

            int mailType = settings.GetIntType("typeMail");
            List<string> mailDomains = settings.GetValuesList("lstMailDomain");

            if (mailType != 1 || mailDomains.Count != 0)
            {
                string selectedDomain = mailDomains.OrderBy(_ => Guid.NewGuid()).First();
                verifyResult = 2;
                string email = "";
                string emailPassword = "";
                string xmlSource = "";
                int retryEditText = 0;
                int maxEditTextRetry = 2;
                int retryTapToRetry = 0;
                int maxTapToRetry = 6;

                while (true)
                {
                    int tickCount = Environment.TickCount;
                    do
                    {
                        xmlSource = _client.GetXMLSource();
                        string foundElement = _client.FindElement(xmlSource, new List<string>
                {
                    "//android.widget.ProgressBar",
                    "//*[@text='Tap to retry']",
                    "//*[@content-desc='Confirm by email']",
                    "//android.widget.EditText[@text='Email address']",
                    "//android.widget.EditText[@text='Confirmation code']",
                    "//android.widget.TextView[@text='save your login info']",
                    "//*[@content-desc='skip' or @text='skip']",
                    "//android.view.ViewGroup[@content-desc=\"Continue in English (US)\"]",
                    "//android.view.ViewGroup[@content-desc=\"Allow\"]",
                    "//com.android.packageinstaller,id/do_not_ask_checkbox",
                    "//android.widget.Button[@text=\"NEVER\"]",
                    "//*[@text='No thanks']",
                    "//*[@content-desc='I ACCEPT']",
                    "//*[@content-desc='Allow all cookies']",
                    "//*[@content-desc='deny' or @text='deny']"
                }, 1);

                        // Handle element found
                        switch (foundElement)
                        {
                            case "//android.widget.EditText[@text='Email address']":
                                if (mailType == 0)
                                {
                                    lock (dictionary_7)
                                    {
                                        while (string.IsNullOrEmpty(email))
                                        {
                                            if (dictionary_7[groupKey].Count != 0)
                                            {
                                                int idx = new Random().Next(0, dictionary_7[groupKey].Count);
                                                string[] arr = dictionary_7[groupKey][idx].Split('|');
                                                if (arr.Length > 1)
                                                {
                                                    email = arr[0].Trim().ToLower();
                                                    emailPassword = arr[1].Trim();
                                                }
                                                dictionary_7[groupKey].RemoveAt(idx);
                                            }
                                            else
                                            {
                                                verifyResult = 7; // Hết email
                                                return 0;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    //email = Common.A8AF5A8E(
                                    //    SetupFolder.smethod_3().OrderBy(_ => Guid.NewGuid()).First() +
                                    //    SetupFolder.F68AD388().OrderBy(_ => Guid.NewGuid()).First()
                                    //).Replace(" ", "").ToLower() + Common.CreateRandomNumber(6) + Regex.Match(selectedDomain, "@\\w+.\\w+").Value;
                                }
                                _client.SendTextSlow(foundElement, email);
                                _client.Delay(1);
                                string continueOrUpdateXPath = _client.FindElement(xmlSource, new List<string> { "//*[@text='Continue']", "//*[@text='Update email address']" }, 1);
                                _client.ElementWithAttributes(continueOrUpdateXPath, 1, xmlSource);
                                break;

                            case "//android.widget.EditText[@text='Confirmation code']":
                                if (_client.ElementWithAttributes("//android.widget.EditText[@text='Confirmation Code']", 1, xmlSource, false))
                                {
                                    SetStatusAccount(accountId, statusText + "Get otp...");
                                    string otp = "";
                                    int otpRetry = 0;
                                    while (otpRetry < 3)
                                    {
                                        //if (!EA98BF20.CheckLiveWall(userWallId).StartsWith("0|"))
                                        //{
                                        //    switch (mailType)
                                        //    {
                                        //        case 0:
                                        //            otp = ImapHelper.GetOtpFromMail(0, email, emailPassword, 60, "", "");
                                        //            if (string.IsNullOrEmpty(otp))
                                        //            {
                                        //                otp = EmailHelper.smethod_2("https://volamtuan.pro", 0, email, emailPassword);
                                        //            }
                                        //            break;
                                        //        case 1:
                                        //            otp = EmailHelper.D8097D8F(0, selectedDomain, email);
                                        //            break;
                                        //        case 2:
                                        //            otp = new GeneratorEmail(email).method_0(0, 120);
                                        //            break;
                                        //    }
                                        //    if (otp == "not connect" || otp == "fail")
                                        //    {
                                        //        verifyResult = 5; // OTP lỗi
                                        //        return 0;
                                        //    }
                                        //    if (!string.IsNullOrEmpty(otp)) break;
                                        //    otpRetry++;
                                        //    continue;
                                        //}
                                        verifyResult = 8; // Tường không hoạt động
                                        return 0;
                                    }
                                    otp = Regex.Match(otp, "c=(.*?)&").Groups[1].Value;
                                    if (string.IsNullOrEmpty(otp))
                                    {
                                        verifyResult = 4; // Không lấy được OTP
                                        return 0;
                                    }
                                    SetStatusAccount(accountId, statusText + "Get otp: " + otp);
                                    _client.SendTextSlow(foundElement, otp);
                                    _client.Delay(1);
                                    _client.ElementWithAttributes("//*[@text='Confirm']", 1, xmlSource);
                                }
                                else if (!string.IsNullOrEmpty(_client.GetAttributeValuesFromXmlNodes(xmlSource, "//android.widget.EditText", "text").FirstOrDefault()))
                                {
                                    if (retryEditText >= maxEditTextRetry)
                                    {
                                        verifyResult = 4;
                                        return 0;
                                    }
                                    retryEditText++;
                                    _client.ElementWithAttributes("//android.widget.EditText");
                                    _client.CLearText();
                                }
                                break;

                            case "//android.widget.TextView[@text='save your login info']":
                                _client.ElementWithAttributes("//android.widget.Button[@text='OK']", 1, xmlSource);
                                break;

                            case "//*[@text='Tap to retry']":
                                if (retryTapToRetry >= maxTapToRetry)
                                {
                                    break;
                                }
                                retryTapToRetry++;
                                ScrollScreen(-1);
                                break;

                            case "//android.widget.ProgressBar":
                                SetStatusAccount(accountId, statusText + "Loading...");
                                break;

                            case "//*[@content-desc='Confirm by email']":
                                _client.ElementWithAttributes(foundElement, 1, xmlSource);
                                break;

                            case "//*[@content-desc='skip' or @text='skip']":
                            case "//android.view.ViewGroup[@content-desc=\"Continue in English (US)\"]":
                            case "//android.view.ViewGroup[@content-desc=\"Allow\"]":
                            case "//com.android.packageinstaller,id/do_not_ask_checkbox":
                            case "//android.widget.Button[@text=\"NEVER\"]":
                            case "//*[@text='No thanks']":
                            case "//*[@content-desc='I ACCEPT']":
                            case "//*[@content-desc='Allow all cookies']":
                            case "//*[@content-desc='deny' or @text='deny']":
                                _client.ElementWithAttributes(foundElement, 1, xmlSource);
                                break;

                            default:
                                //if (!EA98BF20.CheckLiveWall(userWallId).StartsWith("0|"))
                                //{
                                //    if (_client.ContainsAnyKeyword(xmlSource, "Something\u00a0went\u00a0wrong. Please\u00a0try\u00a0again"))
                                //    {
                                //        _client.method_31("//android.widget.EditText");
                                //    }
                                //    else
                                //    {
                                //        if (_client.AF365B16(xmlSource))
                                //        {
                                //            verifyResult = 1;
                                //            method_114(accountId, "cEmail", email, "email");
                                //            method_114(accountId, "cPassMail", emailPassword, "passmail");
                                //            return 0;
                                //        }
                                //        SetStatusAccount(accountId, statusText + "Scroll...");
                                //        if (_client.ScrollScreen())
                                //        {
                                //            int loginResult = Login(_client, accountId, statusText);
                                //            if (loginResult == 1)
                                //            {
                                //                continue;
                                //            }
                                //            if (loginResult != 0)
                                //            {
                                //                break;
                                //            }
                                //        }
                                //    }
                                //}
                                //else
                                //{
                                //    verifyResult = 8;
                                //    return 0;
                                //}
                                break;
                        }
                        _client.Delay(2);

                    } while (Environment.TickCount - tickCount < 600000);
                    break;
                }
            }
            return 0;
        }
        public async Task<int> HDXacNhanKetBan(JsonHelper settings, ScriptAction action)
        {
            int minConfirm = settings.GetIntType("nudSoLuongFrom");
            int maxConfirm = settings.GetIntType("nudSoLuongTo");
            int minDelay = settings.GetIntType("nudDelayFrom");
            int maxDelay = settings.GetIntType("nudDelayTo");
            int confirmedCount = 1;

            try
            {
                DeplinkFacebook("fb://friends/requests/");
                if (_client.ElementWithAttributes("//*[@content-desc=\"See all\"]", 15, "", true))
                {
                    return 0;
                }
                int targetCount = SubdyHelper.RandomValue(minConfirm, maxConfirm + 1);
                int refail = 0;
                while (!_mainService._ct.IsCancellationRequested)
                {
                    // [TIME-LIMIT v21] Ép giới hạn thời gian NGAY TRONG hành động xác nhận kết bạn (mỗi vòng):
                    // quá timeoutTaiKhoan/timeoutKichBan -> break -> ExecuteAsync đổi tài khoản. KHÔNG đụng logic FB.
                    if (Stop()) break;
                    if (confirmedCount > targetCount || refail > 5)
                    {
                        break;
                    }
                    string xpath = "//node[contains(@class, 'android.widget.Button') and @content-desc and string-length(@content-desc) > 0 and " +
                           "contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'confirm') and " +
                           "contains(translate(@content-desc, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'friend request') and " +
                           "@visible-to-user='true']";
                    var nodes = _client.FindElementsNotToLower(15, "", xpath);
                    if (!nodes.Any())
                    {
                        ScrollScreen(1, 1);
                        refail++;
                        continue;
                    }
                    var info = _client.ExtractNodeInfo(nodes.First().OuterXml);
                    var point = new RectangleArea(info["bounds"]).GetCenterPoint();
                    _client.ADB.Shell($"input tap {point.X} {point.Y}");
                    await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(minDelay, maxDelay), $"({confirmedCount}/{targetCount}) {info["content-desc"]}" + ", đợi {time}s...", 2);
                    confirmedCount++;
                }
            }
            catch
            {
                confirmedCount = -1;
            }
            return confirmedCount;
        }
        public async Task<int> HDTuongTacPage(JsonHelper settings, ScriptAction action)
        {
            int totalSeconds = SubdyHelper.RandomValue(settings.GetIntType("numericUpDown2"), settings.GetIntType("numericUpDown1"));

            int delayFrom = settings.GetIntType("nudTimeFrom");
            int delayTo = settings.GetIntType("nudTimeTo");


            bool shouldInteract = settings.GetBooleanValue("ckbInteract");
            int interactCount = SubdyHelper.RandomValue(settings.GetIntType("nudInteractFrom", 1), settings.GetIntType("nudInteractTo", 1));
            List<string> reactionTypes = new List<string> { "Like", "Love", "Care", "Haha", "Wow", "Sad", "Angry" };
            if (!settings.GetBooleanValue("ckbLike"))
            {
                reactionTypes.Remove("Like");
            }
            if (!settings.GetBooleanValue("ckbTym"))
            {
                reactionTypes.Remove("Love");
            }
            if (!settings.GetBooleanValue("ckbThuong"))
            {
                reactionTypes.Remove("Care");
            }
            if (!settings.GetBooleanValue("ckbHaha"))
            {
                reactionTypes.Remove("Haha");
            }
            if (!settings.GetBooleanValue("ckbWow"))
            {
                reactionTypes.Remove("Wow");
            }
            if (!settings.GetBooleanValue("ckbBuon"))
            {
                reactionTypes.Remove("Sad");
            }
            if (!settings.GetBooleanValue("ckbGian"))
            {
                reactionTypes.Remove("Angry");
            }
            List<string> reactions = new List<string>();
            if (reactionTypes.Any())
            {
                for (int i = 0; i < interactCount; i++)
                {
                    reactions.Add(SubdyHelper.GetStringRandom(reactionTypes));
                }
            }

            bool shouldShareWall = settings.GetBooleanValue("ckbShareWall");
            int shareCount = SubdyHelper.RandomValue(settings.GetIntType("nudShareWallFrom", 1), settings.GetIntType("nudShareWallTo", 1));



            bool shouldComment = settings.GetBooleanValue("ckbComment");
            int commentCount = SubdyHelper.RandomValue(settings.GetIntType("nudCommentFrom", 1), settings.GetIntType("nudCommentTo", 1));


            Dictionary<string, string> keyValues = new Dictionary<string, string>();
            string type = "Default";
            if (settings.GetBooleanValue("radioButton1"))
            {
                type = "KhamPha";
            }
            else if (settings.GetBooleanValue("ckbChiDinh"))
            {
                type = "ChiDinh";
            }
            else if (settings.GetBooleanValue("ckbTuKhoa"))
            {
                type = "TuKhoa";
            }
            switch (type)
            {
                case "Default":
                    {
                        if (!SearchOnFacebook("pages on facebook", "All"))
                        {
                            throw new SubdyExtension(SubdyEnum.JobFail, "Không tìm kiếm được page");
                        }
                        _client.ElementWithAttributes("//*[@content-desc=\"Pages\"]");
                        var targetColor = Color.FromArgb(8, 8, 8);
                        Point point = Point.Empty;
                        for (int i = 0; i < 15; i++)
                        {
                            var filtereds = _client.FindColorCoordinates(targetColor, tolerance: 0);
                            if (!filtereds.Any())
                            {
                                Thread.Sleep(1000);
                                continue;
                            }
                            var filtered = filtereds.FindAll(r => r.Rx == 10 && (r.Ry == 2 || r.Ry == 3));
                            if (!filtered.Any())
                            {
                                Thread.Sleep(1000);
                                continue;
                            }
                            point = filtered.First().Center;
                            break;
                        }
                        if (point == Point.Empty)
                        {
                            throw new SubdyExtension(SubdyEnum.Error, "Không tìm kiếm được page");
                        }
                        _client.Click(point.X, point.Y);
                        string xpath = "//node[contains(@class,'android.view.ViewGroup') and @content-desc and string-length(@text) > 0 and @visible-to-user='true'" +
                             "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'changes to pages'))" +
                             "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'all the pages you like or follow'))]";
                        var nodes = _client.FindElementsNotToLower(15, "", xpath);
                        if (!nodes.Any())
                        {
                            throw new SubdyExtension(SubdyEnum.JobFail, "Không tìm được page đã thích");
                        }
                        break;
                    }
                case "KhamPha":
                    {
                        if (!SearchOnFacebook("pages on facebook", "All"))
                        {
                            throw new SubdyExtension(SubdyEnum.JobFail, "Không tìm kiếm được page");
                        }
                        _client.ElementWithAttributes("//*[@content-desc=\"Pages\"]");
                        var targetColor = Color.FromArgb(2, 5, 10);
                        Point point = Point.Empty;
                        for (int i = 0; i < 15; i++)
                        {
                            var filtereds = _client.FindColorCoordinates(targetColor, tolerance: 0);
                            if (!filtereds.Any())
                            {
                                Thread.Sleep(1000);
                                continue;
                            }
                            var filtered = filtereds.FindAll(r => r.Rx == 3 && (r.Ry == 2 || r.Ry == 3));
                            if (!filtered.Any())
                            {
                                Thread.Sleep(1000);
                                continue;
                            }
                            point = filtered.First().Center;
                            break;
                        }
                        if (point == Point.Empty)
                        {
                            throw new SubdyExtension(SubdyEnum.Error, "Không tìm kiếm được page");
                        }
                        _client.Click(point.X, point.Y);
                        string xpath = "//node[contains(@class,'android.view.ViewGroup') and @content-desc and string-length(@text) > 0 and @visible-to-user='true'" +
                             "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'people like this'))]";
                        var nodes = _client.FindElementsNotToLower(15, "", xpath);
                        if (!nodes.Any())
                        {
                            throw new SubdyExtension(SubdyEnum.JobFail, "Không tìm được page đã thích");
                        }

                        break;
                    }
                case "ChiDinh":
                    {
                        var urls = settings.GetValuesList("txtLinks");
                        if (!urls.Any())
                        {
                            throw new SubdyExtension(SubdyEnum.Error, "Không có link chỉ định");
                        }
                        foreach (var url in urls)
                        {
                            if (!keyValues.ContainsKey(url))
                            {
                                keyValues.Add(url, url);
                            }
                        }
                        break;
                    }
                case "TuKhoa":
                    {
                        var urls = settings.GetValuesList("txtLinks");
                        if (!urls.Any())
                        {
                            throw new SubdyExtension(SubdyEnum.Error, "Không có keyword");
                        }
                        if (!SearchOnFacebook(SubdyHelper.GetStringRandom(urls), "Pages"))
                        {
                            throw new SubdyExtension(SubdyEnum.JobFail, "Không tìm kiếm được page");
                        }
                        string xpath = "//node[contains(@class,'android.view.ViewGroup') and @content-desc and string-length(@text) > 0 and @visible-to-user='true'" +
                             "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'followers'))]";
                        var nodes = _client.FindElementsNotToLower(15, "", xpath);
                        if (!nodes.Any())
                        {
                            throw new SubdyExtension(SubdyEnum.JobFail, "Không tìm được page.");
                        }

                        break;
                    }
            }
            List<string> old = new List<string>();
            int count = 1;
            while (!_mainService._ct.IsCancellationRequested)
            {
                // [TIME-LIMIT v21] Ép giới hạn thời gian NGAY TRONG hành động dài (mỗi vòng):
                // quá timeoutTaiKhoan/timeoutKichBan -> break -> ExecuteAsync đổi tài khoản. KHÔNG đụng logic FB.
                if (Stop()) break;
                if (count > totalSeconds)
                {
                    break;
                }
                switch (type)
                {
                    case "Default":
                        {
                            if (!keyValues.Any())
                            {
                                string xpath = "//node[contains(@class,'android.view.ViewGroup') and @content-desc and string-length(@text) > 0 and @visible-to-user='true'" +
                                 "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'changes to pages'))" +
                                 "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'all the pages you like or follow'))]";
                                var nodes = _client.FindElementsNotToLower(15, "", xpath);
                                if (!nodes.Any())
                                {
                                    throw new SubdyExtension(SubdyEnum.JobFail, "Không tìm được page đã thích");
                                }
                                bool allExist = true;
                                foreach (var node in nodes)
                                {
                                    var info = _client.ExtractNodeInfo(node.OuterXml);
                                    if (!old.Contains(info["content-desc"]))
                                    {
                                        allExist = false;
                                        keyValues.Add(info["content-desc"], info["bounds"]);
                                    }
                                    old.Add(info["content-desc"]);
                                }
                                if (allExist)
                                {
                                    ScrollScreen(1, 1);
                                    count++;
                                    continue;
                                }
                            }

                            if (!keyValues.Any())
                            {
                                count = totalSeconds + 1;
                                continue;
                            }
                            var firt = keyValues.First();
                            var point = new RectangleArea(firt.Value).GetCenterPoint();
                            _client.ADB.Shell($"input tap {point.X} {point.Y}");
                            await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(5, 10), $"({count}/{totalSeconds}) Go to page {firt.Key}" + ", đợi {time}s...", 2);
                            keyValues.Remove(firt.Key);
                            break;
                        }
                    case "KhamPha":
                        {
                            if (!keyValues.Any())
                            {

                                string xpath = "//node[contains(@class,'android.view.ViewGroup') and @content-desc and string-length(@text) > 0 and @visible-to-user='true'" +
                                 "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'people like this'))]";
                                var nodes = _client.FindElementsNotToLower(15, "", xpath);
                                if (!nodes.Any())
                                {
                                    throw new SubdyExtension(SubdyEnum.JobFail, "Không tìm được page đã thích");
                                }
                                bool allExist = true;
                                foreach (var node in nodes)
                                {
                                    var info = _client.ExtractNodeInfo(node.OuterXml);
                                    if (!old.Contains(info["content-desc"]))
                                    {
                                        allExist = false;
                                        keyValues.Add(info["content-desc"], info["bounds"]);
                                    }
                                    old.Add(info["content-desc"]);
                                }
                                if (allExist)
                                {
                                    ScrollScreen(1, 1);
                                    count++;
                                    continue;
                                }
                            }
                            if (!keyValues.Any())
                            {
                                count = totalSeconds + 1;
                                continue;
                            }
                            var firt = keyValues.First();
                            var point = new RectangleArea(firt.Value).GetCenterPoint();
                            _client.ADB.Shell($"input tap {point.X} {point.Y}");
                            await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(5, 10), $"({count}/{totalSeconds}) Go to page {firt.Key}" + ", đợi {time}s...", 2);
                            keyValues.Remove(firt.Key);
                            break;
                        }
                    case "ChiDinh":
                        {
                            if (!keyValues.Any())
                            {
                                count = totalSeconds + 1;
                                continue;
                            }
                            var firt = keyValues.First();
                            if (!DeplinkFacebook($"fb://profile/{firt.Key}").Contains("dat=fb://profile"))
                            {
                                continue;
                            }
                            await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(5, 10), $"({count}/{totalSeconds}) Go to page {firt.Key}" + ", đợi {time}s...", 2);
                            keyValues.Remove(firt.Key);
                            break;
                        }
                    case "TuKhoa":
                        {
                            if (!keyValues.Any())
                            {
                                string xpath = "//node[contains(@class,'android.view.ViewGroup') and @content-desc and string-length(@text) > 0 and @visible-to-user='true'" +
                         "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'followers'))]";
                                var nodes = _client.FindElementsNotToLower(15, "", xpath);
                                if (!nodes.Any())
                                {
                                    throw new SubdyExtension(SubdyEnum.JobFail, "Không tìm được page đã thích");
                                }
                                bool allExist = true;
                                foreach (var node in nodes)
                                {
                                    var info = _client.ExtractNodeInfo(node.OuterXml);
                                    if (!old.Contains(info["content-desc"]))
                                    {
                                        allExist = false;
                                        keyValues.Add(info["content-desc"], info["bounds"]);
                                    }
                                    old.Add(info["content-desc"]);
                                }
                                if (allExist)
                                {
                                    ScrollScreen(1, 1);
                                    count++;
                                    continue;
                                }
                            }

                            if (!keyValues.Any())
                            {
                                count = totalSeconds + 1;
                                continue;
                            }
                            var firt = keyValues.First();
                            var point = new RectangleArea(firt.Value).GetCenterPoint();
                            _client.ADB.Shell($"input tap {point.X} {point.Y}");
                            await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(5, 10), $"({count}/{totalSeconds}) Go to page {firt.Key}" + ", đợi {time}s...", 2);
                            keyValues.Remove(firt.Key);
                            break;
                        }
                }


                int tickCount = Environment.TickCount;
                int time = SubdyHelper.RandomValue(delayTo, delayFrom);
                while (!_mainService._ct.IsCancellationRequested)
                {
                    await _mainService.DelayMessageAsync(3, $"Xem page, đợi {{time}}s...", 2);
                    if (shouldInteract && reactions.Any())
                    {
                        var message = TapReaction(reactions[0]);
                        await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(3, 7), $"{message}, đợi {{time}}s...", 2);
                        reactions.RemoveAt(0);
                    }
                    if (shouldShareWall && shareCount > 0)
                    {
                        var message = TapShareNewfeed("");
                        await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(3, 7), $"{message}, đợi {{time}}s...", 2);
                        shareCount--;
                    }
                    if (shouldComment && commentCount > 0)
                    {
                        string image = "";
                        string content = "";
                        if (_data.ContainsKey($"{action.Id}_txtComments"))
                        {
                            lock (Globals.Lock)
                            {
                                var contents = _data[$"{action.Id}_txtComments"];
                                if (contents.Any())
                                {
                                    content = SubdyHelper.GetStringRandom(contents);
                                    if (!settings.GetBooleanValue("checkBox5"))
                                    {
                                        contents.Remove(content);
                                        _data[$"{action.Id}_txtComments"] = contents;
                                    }
                                    if (settings.GetBooleanValue("checkBox4"))
                                    {
                                        var context = new ScriptActionContext();
                                        settings.DeleteValue("txtComments", content);
                                        action.Json = settings.GetJsonString();
                                        context.Update(action);
                                    }
                                }

                            }
                            content = SubdyHelper.SpinText(content);
                        }
                        if (settings.GetBooleanValue("ckbAnh") && _data.ContainsKey($"{action.Id}_txtPathAnh") && _data[$"{action.Id}_txtComments"].Any())
                        {
                            lock (Globals.Lock)
                            {
                                var images = _data[$"{action.Id}_txtPathAnh"];
                                image = SubdyHelper.GetStringRandom(images);
                                if (settings.GetBooleanValue("checkBox3"))
                                {
                                    images.Remove(image);
                                    File.Delete(image);
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(content) || File.Exists(image))
                        {
                            string message = await CommentAction(content, image, settings.GetBooleanValue("ckbTagNeuBat"), settings.GetBooleanValue("checkBox2"));
                            await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(3, 7), $"{message}, đợi {{time}}s...", 2);
                        }
                        commentCount--;
                    }
                    if (Environment.TickCount - tickCount >= time * 1000)
                    {
                        break;
                    }
                    ScrollScreen(1, 2);
                }
                _client.ATX.Press(PressKey.Back);
                _client.Delay(2);
                count++;
            }
            int result = 0;
            return result;
        }
        public int HDXoaThietBiTinCay(int accountId, string statusPrefix, string actionTitle)
        {
            string text = statusPrefix + "Đang " + actionTitle + ": ";
            SetStatusAccount(accountId, text + "Đang chạy...");
            RemoveTrustedDevices();
            return 1;
        }
        public int HDSpamBaiViet(int accountId, string statusPrefix, JsonHelper settings, string actionTitle, string groupKey)
        {
            int uidMin = settings.GetIntType("nudSoLuongUidFrom");
            int uidMax = settings.GetIntType("nudSoLuongUidTo");
            int postMin = settings.GetIntType("nudSoLuongBaiVietFrom");
            int postMax = settings.GetIntType("nudSoLuongBaiVietTo");
            int delayMin = settings.GetIntType("nudDelayFrom");
            int delayMax = settings.GetIntType("nudDelayTo");
            int idType = settings.GetIntType("typeID");
            bool doInteract = settings.GetBooleanValue("ckbInteract");
            string reactionType = settings.GetValue("typeReaction");
            bool doShareWall = settings.GetBooleanValue("ckbShareWall");
            bool doComment = settings.GetBooleanValue("ckbComment");
            List<string> comments = settings.GetValuesList("txtComment", settings.GetIntType("typeNganCach"));
            bool autoRemoveUid = settings.GetBooleanValue("ckbTuDongXoaUid");
            bool doImage = settings.GetBooleanValue("ckbAnh");
            string imagePath = settings.GetValue("txtPathAnh");
            bool doReel = settings.GetBooleanValue("ckbReel");

            List<string> uidList = new List<string>();
            if (!autoRemoveUid)
            {
                uidList = SubdyHelper.CloneList(dictionary_9[groupKey]);
            }

            try
            {
                string statusText = statusPrefix + "Đang " + actionTitle + ": ";
                int numTargets = SubdyHelper.RandomValue(uidMin, uidMax + 1);
                for (int i = 0; i < numTargets; i++)
                {
                    string targetUid = "";
                    if (autoRemoveUid)
                    {
                        lock (dictionary_9)
                        {
                            if (dictionary_9[groupKey].Count == 0)
                                break;
                            int index = SubdyHelper.RandomValue(0, dictionary_9[groupKey].Count);
                            targetUid = dictionary_9[groupKey][index];
                            dictionary_9[groupKey].RemoveAt(index);
                        }
                    }
                    else
                    {
                        if (uidList.Count != 0)
                        {
                            targetUid = uidList[SubdyHelper.RandomValue(0, uidList.Count)];
                            uidList.Remove(targetUid);
                        }
                        else
                        {
                            break;
                        }
                    }

                    // Build link for profile/group/page/reel
                    string fbLink = "";
                    switch (idType)
                    {
                        case 0:
                            fbLink = "fb://profile/" + targetUid;
                            break;
                        case 1:
                            fbLink = "fb://group/" + targetUid;
                            break;
                        case 2:
                            fbLink = (!targetUid.StartsWith("1000") ? "fb://page/" + targetUid : "fb://profile/" + targetUid);
                            break;
                    }

                    while (OpenFacebookLink(accountId, statusText, fbLink))
                    {
                        // Replace UID placeholder in comments
                        for (int j = 0; j < comments.Count; j++)
                        {
                            comments[j] = comments[j].Replace("@[uid:0]", "@[" + targetUid + ":0]");
                        }

                        // Handle reels
                        if (doReel)
                        {
                            int reelTabTries = 0;
                            int reelTabMaxTries = 4;
                            int tabTickCount = Environment.TickCount;
                            string xmlSource;
                            string foundTab;
                            while (true)
                            {
                                xmlSource = _client.GetXMLSource();
                                foundTab = _client.FindElement(xmlSource, new List<string>
                        {
                            "//*[@content-desc='Reels' or (@content-desc='Reels, tab' and @selected='false')]",
                            "//*[starts-with(@content-desc,'Reel,')]",
                            "//*[@content-desc='Posts, tab']"
                        }, 1);

                                switch (foundTab)
                                {
                                    case "//*[@content-desc='Reels' or (@content-desc='Reels, tab' and @selected='false')]":
                                        SetStatusAccount(accountId, statusText + $"({i + 1}/{numTargets}), Tap " + foundTab + "...");
                                        _client.ElementWithAttributes(foundTab, 1, xmlSource);
                                        break;
                                    case "//*[@content-desc='Posts, tab']":
                                        reelTabTries++;
                                        if (reelTabTries >= reelTabMaxTries)
                                            goto EndReelTabLoop;
                                        _client.ElementWithAttributes("//*[@content-desc='Posts, tab']/parent::*/parent::*/child::*[last()]/child::*", 1, xmlSource);
                                        break;
                                    default:
                                        SetStatusAccount(accountId, statusText + $"({i + 1}/{numTargets}), Scroll...");
                                        if (ScrollScreen())
                                        {
                                            int loginResult = Login();
                                            if (loginResult == 1)
                                                goto EndReelTabLoop;
                                            if (loginResult != 0)
                                                goto EndReelTabLoop;
                                        }
                                        break;
                                }
                                if (Environment.TickCount - tabTickCount < 60000)
                                {
                                    _client.Delay(2);
                                    continue;
                                }
                                break;
                            }
                        EndReelTabLoop:
                            SetStatusAccount(accountId, statusText + $"({i + 1}/{numTargets}), Tap " + foundTab + "...");
                            _client.ElementWithAttributes(foundTab, 1, xmlSource);
                        }
                        else if (fbLink.Contains("page"))
                        {
                            _client.ElementWithAttributes("//*[contains(@content-desc,\"Posts, Tab\")]", 1, "");
                            _client.Delay(2);
                        }

                        // Spam interact
                        ScrollFeedAndInteract(
                            accountId,
                            statusText + $"({i + 1}/{numTargets}), ",
                            postMin,
                            postMax,
                            doInteract,
                            reactionType,
                            postMin,
                            postMax,
                            doComment,
                            postMin,
                            postMax,
                            comments,
                            doShareWall,
                            0, // share count min
                            0, // share count max
                            1, // interact image
                            doImage,
                            imagePath,
                            delayMin,
                            delayMax
                        );
                        break;
                    }
                }
            }
            catch
            {
                // Handle exception if needed
            }
            return 0;
        }
        public async Task<int> HDTuongTacLivestream(JsonHelper settings, ScriptAction action)
        {
            int totalSeconds = SubdyHelper.RandomValue(settings.GetIntType("nudTimeFrom"), settings.GetIntType("nudTimeTo"));

            int delayFrom = settings.GetIntType("nudTuKhoaFrom");
            int delayTo = settings.GetIntType("nudTuKhoaTo");

            bool shouldInteract = settings.GetBooleanValue("ckbInteract");
            int interactCount = SubdyHelper.RandomValue(settings.GetIntType("nudInteractFrom", 1), settings.GetIntType("nudInteractTo", 1));
            List<string> reactionTypes = new List<string> { "Like", "Love", "Care", "Haha", "Wow", "Sad", "Angry" };
            if (!settings.GetBooleanValue("ckbLike"))
            {
                reactionTypes.Remove("Like");
            }
            if (!settings.GetBooleanValue("ckbTym"))
            {
                reactionTypes.Remove("Love");
            }
            if (!settings.GetBooleanValue("ckbThuong"))
            {
                reactionTypes.Remove("Care");
            }
            if (!settings.GetBooleanValue("ckbHaha"))
            {
                reactionTypes.Remove("Haha");
            }
            if (!settings.GetBooleanValue("ckbWow"))
            {
                reactionTypes.Remove("Wow");
            }
            if (!settings.GetBooleanValue("ckbBuon"))
            {
                reactionTypes.Remove("Sad");
            }
            if (!settings.GetBooleanValue("ckbGian"))
            {
                reactionTypes.Remove("Angry");
            }
            List<string> reactions = new List<string>();
            if (reactionTypes.Any())
            {
                for (int i = 0; i < interactCount; i++)
                {
                    reactions.Add(SubdyHelper.GetStringRandom(reactionTypes));
                }
            }

            bool shouldShareWall = settings.GetBooleanValue("ckbShareWall");
            int shareCount = SubdyHelper.RandomValue(settings.GetIntType("nudShareWallFrom", 1), settings.GetIntType("nudShareWallTo", 1));



            bool shouldComment = settings.GetBooleanValue("ckbComment");
            int commentCount = SubdyHelper.RandomValue(settings.GetIntType("nudCommentFrom", 1), settings.GetIntType("nudCommentTo", 1));



            string type = "DeXuat";
            if (settings.GetBooleanValue("ckbChiDinh"))
            {
                type = "ChiDinh";
            }

            switch (type)
            {
                case "DeXuat":
                    {

                        if (!SearchOnFacebook("Live", "Videos"))
                        {
                            throw new SubdyExtension(SubdyEnum.JobFail, "Không tìm được video livestream");
                        }
                        string xpath = "//node[contains(@class,'android.widget.ImageView') and string-length(@content-desc) > 0 and @visible-to-user='true'" +
                                  "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'discover'))" +
                                  "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'notification'))" +
                                  "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'ringer'))" +
                                  "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'member'))" +
                                  "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'back'))" +
                                  "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'clear text'))" +
                              "and not(contains(translate(@content-desc,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'search results'))]";
                        var friends = _client.FindElementsNotToLower(20, "", xpath);
                        // var friends = _client.FindBounds("", "//*[@class='android.widget.Button']/parent::*[@visible-to-user='true']", 15);
                        if (!friends.Any())
                        {
                            throw new SubdyExtension(SubdyEnum.JobFail, "Không tìm được video livestream");
                        }
                        var friend = friends[SubdyHelper.RandomValue(0, friends.Count)];
                        var info = _client.ExtractNodeInfo(friend.OuterXml);
                        var point = new RectangleArea(info["bounds"]).GetCenterPoint();
                        _client.ADB.Shell($"input tap {point.X} {point.Y}");
                        _client.ElementWithAttributes("//*[@content-desc=\"Tap to join live\"]", 20);
                        await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(5, 10), $"Go to live {info["content-desc"]}" + " ,đợi {time}s...", 2);
                        break;
                    }
                case "ChiDinh":
                    {
                        string url = SubdyHelper.GetStringRandom(settings.GetValuesList("txtLinks"));
                        if (string.IsNullOrEmpty(url))
                        {
                            throw new SubdyExtension(SubdyEnum.JobFail, "Không có link chỉ định");
                        }
                        if (!DeplinkFacebook(url).Contains(".IntentUriHandler"))
                        {
                            throw new SubdyExtension(SubdyEnum.JobFail, "Mở video thất bại");
                        }
                        await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(5, 10), $"Go to live {url}" + " ,đợi {time}s...", 2);
                        break;
                    }
            }
            int tickCount = Environment.TickCount;
            while (!_mainService._ct.IsCancellationRequested)
            {

                if (shouldShareWall && shareCount > 0)
                {
                    var message = TapShareNewfeed("");
                    await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(3, 7), $"{message}, đợi {{time}}s...", 2);
                    shareCount--;
                }
                if (shouldInteract && reactions.Any())
                {
                    string message = string.Empty;
                    string xpath = $"//*[@content-desc=\"{reactions[0]}\"]";
                    message = $"Đã {reactions[0]} livestream thất bại";
                    for (int i = 0; i < 3; i++)
                    {
                        if (_client.ElementWithAttributes(xpath, 2))
                        {
                            message = $"Đã {reactions[0]} livestream";
                            break;
                        }
                        _client.Delay(1);
                        var bounds = _client.FindBounds("", "//*[@class=\"android.widget.HorizontalScrollView\"]", 2);
                        if (!bounds.Any())
                        {
                            continue;
                        }
                        var rec = new RectangleArea(bounds[0]);
                        _client.Swipe(rec.Right - 10, (rec.Top + rec.Bottom) / 2, rec.Left + 50, rec.Top, 10, 2);
                    }
                    await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(3, 7), $"{message}, đợi {{time}}s...", 2);
                    reactions.RemoveAt(0);
                }

                if (shouldComment && commentCount > 0)
                {
                    string content = "";
                    if (_data.ContainsKey($"{action.Id}_txtComments"))
                    {
                        lock (Globals.Lock)
                        {
                            var contents = _data[$"{action.Id}_txtComments"];
                            if (contents.Any())
                            {
                                content = SubdyHelper.GetStringRandom(contents);
                                if (!settings.GetBooleanValue("checkBox5"))
                                {
                                    contents.Remove(content);
                                    _data[$"{action.Id}_txtComments"] = contents;
                                }
                                if (settings.GetBooleanValue("checkBox4"))
                                {
                                    var context = new ScriptActionContext();
                                    settings.DeleteValue("txtComments", content);
                                    action.Json = settings.GetJsonString();
                                    context.Update(action);
                                }
                            }

                        }
                        content = SubdyHelper.SpinText(content);
                    }
                    if (!string.IsNullOrEmpty(content))
                    {
                        string message = await CommentAction(content, "", settings.GetBooleanValue("ckbTagNeuBat"), settings.GetBooleanValue("checkBox2"));
                        await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(3, 7), $"{message}, đợi {{time}}s...", 2);
                    }
                    commentCount--;
                }


                if (Environment.TickCount - tickCount >= totalSeconds * 1000)
                {
                    break;
                }
                await _mainService.DelayMessageAsync(SubdyHelper.RandomValue(delayTo, delayFrom), $"Xem live, đợi {{time}}s...", 2);
            }
            int result = 0;
            return result;
        }














        private void RemoveTrustedDevices()
        {
            _client.Shell($"am start -n {FacebookHander.Package(PlatformModel.Facebook)}/.IntentUriHandler \"https://m.facebook.com/settings/security/two_factor/devices/\"");
            string xmlSource = "";
            int startTick = Environment.TickCount;

            do
            {
                xmlSource = "";
                string foundElement = _client.FindElement(xmlSource, new List<string>
        {
            "//*[contains(@text,'No trusted devices')]",
            "//*[@text='Remove']"
        }, 10);

                if (!string.IsNullOrEmpty(foundElement) && foundElement != "//*[contains(@text,'No trusted devices')]")
                {
                    _client.ElementWithAttributes("//*[@text='Remove']", 1, xmlSource);
                    _client.Delay(2);
                    continue;
                }
                break;
            }
            while (Environment.TickCount - startTick < 120000);
        }
        public int WatchReels(int accountId, string statusPrefix, string actionName, int minWatchTime, int maxWatchTime, bool allowLike, string commentText, bool allowComment, List<string> videoList, bool uniquePerGroup, bool repeatMode, int minDelay, int maxDelay, string groupKey)
        {
            try
            {
                int likeCount = 0;
                int commentCount = 0;
                int interactionCount = 0;

                // Số lần like/comment tối đa trong một session
                int maxLikes = 1;
                int maxComments = repeatMode ? 99999 : 1;

                // Chuẩn hóa danh sách video
                videoList = SubdyHelper.CloneList(videoList); // smethod_18
                List<string> workingList = SubdyHelper.CloneList(videoList); // smethod_11
                List<string> backupList = new List<string>();

                if (string.IsNullOrEmpty(groupKey))
                {
                    backupList = SubdyHelper.CloneList(videoList);
                }

                // Thời gian xem mỗi video
                int watchDuration = SubdyHelper.RandomValue(minWatchTime, maxWatchTime);

                // Chuẩn bị mở Reel (VD: mở app, load giao diện)
                int openStatus = OpenFacebookFullscreenVideoAndCheckShare(accountId, statusPrefix, actionName);
                if (openStatus != 1)
                    return 0;

                // Bắt đầu xem video
                int startTick = Environment.TickCount;
                SetStatusAccount(accountId, $"{statusPrefix} Đang xem video...");

                do
                {
                    // Nếu có nhóm video
                    if (!string.IsNullOrEmpty(groupKey) && C50FA08A[groupKey].Count > 0)
                    {
                        string selectedVideo = "";

                        if (!uniquePerGroup)
                        {
                            // Lấy random, nhưng không xóa
                            selectedVideo = C50FA08A[groupKey]
                                .OrderBy(_ => Guid.NewGuid())
                                .FirstOrDefault();
                        }
                        else
                        {
                            // Lấy random và loại bỏ khỏi danh sách để tránh trùng
                            lock (C50FA08A)
                            {
                                int index = SubdyHelper.RandomValue(0, C50FA08A[groupKey].Count);
                                selectedVideo = C50FA08A[groupKey][index];
                                C50FA08A[groupKey].RemoveAt(index);
                            }
                        }

                        workingList.Clear();
                        workingList.Add(selectedVideo);
                    }

                    // Xem reel
                    InteractWithFacebookPost(
          accountId,
          statusPrefix,
          allowLike && likeCount < maxLikes,
          commentText,
          ref likeCount,
          allowComment && commentCount < maxComments,
          ref commentCount,
          workingList,
          ref backupList,
          false,
          ref interactionCount);

                    // Nếu còn trong quota comment thì đợi thêm trước khi sang video tiếp theo
                    if (allowComment && commentCount < maxComments)
                    {
                        SetStatusAccount(accountId,
                            $"{statusPrefix} Đang xem video, " +
                            $"đợi {{time}}s...",
                            SubdyHelper.RandomValue(minDelay, maxDelay + 1));
                        continue;
                    }

                    // Nếu hết quota thì chờ cho đến hết thời gian xem
                    int elapsedSeconds = (Environment.TickCount - startTick) / 1000;
                    SetStatusAccount(accountId,
                        $"{statusPrefix} Đang xem video {{time}}s...",
                        watchDuration - elapsedSeconds);

                    break;

                } while (Environment.TickCount - startTick < watchDuration * 1000);
            }
            catch
            {
                // TODO: log lỗi nếu cần
            }

            return 0;
        }
        private int OpenFacebookFullscreenVideoAndCheckShare(int accountId, string statusPrefix, string videoUrl)
        {
            int result = 0;
            string postId = SubdyHelper.ExtractFacebookPostIdFromUrl(videoUrl);
            if (!(postId == ""))
            {
                int num = 0;
                int maxTries = 5;
                string xmlSource = "";
                string shareElement = "";
                for (int i = 0; i < 3; i++)
                {
                    while (num < maxTries)
                    {
                        num++;
                        if (!OpenFacebookLink(accountId, statusPrefix, "fb://fullscreen_video/" + postId + "?loop=0"))
                        {
                            break;
                        }
                        for (int j = 1; j < 11; j++)
                        {
                            xmlSource = _client.GetXMLSource();
                            shareElement = _client.FindElement(xmlSource, new List<string> { "//*[@content-desc='SHARE' or @text='SHARE']" }, 1);
                            if (!(shareElement != ""))
                            {
                                if (j % 3 == 0)
                                {
                                    int loginResult = Login();
                                    if (loginResult == 1)
                                    {
                                        goto IL_0194;
                                    }
                                    if (loginResult != 0)
                                    {
                                        result = loginResult;
                                        goto IL_01bd;
                                    }
                                }
                                if (!GetCurrentActivity().Contains("com.facebook.video.activity.DeprecatedFullscreenVideoPlayerActivity"))
                                {
                                    if (num < maxTries)
                                    {
                                        num++;
                                        OpenFacebookLink(accountId, statusPrefix, "fb://fullscreen_video/" + postId + "?loop=0");
                                        continue;
                                    }
                                }
                                else
                                {
                                    List<string> source = _client.ExtractBoundsFromXml(0, xmlSource);
                                    if (source.Where((string str) => !str.Contains($"[{_client.GetScreenResolution().X},{_client.GetScreenResolution().Y}]")).Count() != 0)
                                    {
                                        _client.Delay(1);
                                        continue;
                                    }
                                    result = 1;
                                }
                            }
                            else
                            {
                                result = 1;
                            }
                            goto IL_01bd;
                        }
                        goto IL_01a3;
                    IL_0194:;
                    }
                    break;
                IL_01a3:;
                }
            }
            goto IL_01bd;
        IL_01bd:
            return result;
        }
        public void SearchReel(string keyword = "")
        {
            OpenReel("");
            // Kiểm tra có ô tìm kiếm và có từ khóa
            if (_client.ElementWithAttributes("//*[@content-desc='Search']", 5, "") &&
                !string.IsNullOrEmpty(keyword) && _client.ElementWithAttributes("//android.widget.EditText", 5, ""))
            {
                _client.SendTextSlow("//android.widget.EditText", keyword, 5);
                _client.Delay(1, 2); // Đợi sau khi nhập
                _client.ATX.Press(PressKey.Enter); // Có thể là nhấn nút tìm kiếm hoặc thao tác tiếp theo
                _client.Delay(2);// Chờ thêm
                WaitForPostComplete(60); // Chờ tải kết quả
            }
        }
        public int InteractWithPost(int accountId, string statusPrefix, string postId, int minWait, int maxWait, bool enableReaction, string reactionType, bool enableComment, List<string> comments, bool autoRemoveComment, bool enableImage, string imagePath, string groupKey = "")
        {
            try
            {
                SetStatusAccount(accountId, statusPrefix + "Mở bài viết...");
                int openResult = OpenFacebookPost(accountId, statusPrefix, postId);
                if (openResult == 1)
                {
                    SetStatusAccount(accountId, statusPrefix + "Xem bài viết, đợi {time}s...", SubdyHelper.RandomValue(minWait, maxWait + 1));
                    SetStatusAccount(accountId, statusPrefix + "Tương tác bài viết...");
                    int reactionCount = 0;
                    int commentCount = 0;
                    int extraStatus = 0;
                    List<string> interactedComments = new List<string>();
                    List<string> commentList = new List<string>();

                    // Chuẩn bị danh sách comment
                    if (string.IsNullOrEmpty(groupKey))
                    {
                        commentList = SubdyHelper.CloneList(comments);
                    }
                    else if (dictionary_8[groupKey].Count > 0)
                    {
                        string selectedComment = "";
                        if (!autoRemoveComment)
                        {
                            selectedComment = dictionary_8[groupKey].OrderBy(x => Guid.NewGuid()).FirstOrDefault();
                        }
                        else
                        {
                            lock (dictionary_8)
                            {
                                int index = SubdyHelper.RandomValue(0, dictionary_8[groupKey].Count);
                                selectedComment = dictionary_8[groupKey][index];
                                dictionary_8[groupKey].RemoveAt(index);
                            }
                        }
                        commentList.Add(selectedComment);
                    }

                    // Tương tác với bài viết, cuộn thêm nếu chưa comment hoặc react được
                    for (int i = 0; i < 5; i++)
                    {
                        HandleFacebookComment(
                            accountId,
                            statusPrefix,
                            enableReaction,
                            reactionType,
                            ref reactionCount,
                            enableComment,
                            ref commentCount,
                            commentList,
                            ref interactedComments,
                            false,
                            ref extraStatus,
                            1,
                            enableImage,
                            imagePath
                        );
                        if (reactionCount <= 0 && commentCount <= 0)
                        {
                            SetStatusAccount(accountId, statusPrefix + "Scroll...");
                            if (ScrollScreen() && !HandlePopups())
                            {
                                break;
                            }
                            continue;
                        }
                        break;
                    }
                }
            }
            catch
            {
                // Có thể log lỗi tại đây nếu cần
            }
            return 0;
        }
        private int OpenFacebookPost(int accountId, string statusText, string postUrl)
        {
            int result = 0;

            // Chuyển đổi URL theo định dạng đặc biệt nếu cần
            if (!postUrl.Contains("facebook.com/reel/"))
            {
                if (postUrl.Contains("photo") || postUrl.Contains("v="))
                {
                    string postId = SubdyHelper.ExtractFacebookPostIdFromUrl(postUrl);
                    if (SubdyHelper.IsAllDigits(postId))
                    {
                        postUrl = "https://m.facebook.com/" + postId;
                    }
                }
                else if (postUrl.Contains("groups"))
                {
                    string groupId = Regex.Match(postUrl, "groups/(.*?)/").Groups[1].Value;
                    if (string.IsNullOrEmpty(groupId))
                    {
                        groupId = Regex.Match(postUrl, "groups/(.*?)\\?").Groups[1].Value;
                    }
                    if (!string.IsNullOrEmpty(groupId))
                    {
                        string postId = SubdyHelper.ExtractFacebookPostIdFromUrl(postUrl);
                        if (!string.IsNullOrEmpty(postId))
                        {
                            postUrl = $"https://m.facebook.com/groups/{groupId}/permalink/{postId}/";
                        }
                    }
                }
                else if (postUrl.Contains("story_fbid"))
                {
                    string userId = Regex.Match(postUrl, "id=(\\d+)").Groups[1].Value;
                    if (!string.IsNullOrEmpty(userId))
                    {
                        string postId = SubdyHelper.ExtractFacebookPostIdFromUrl(postUrl);
                        if (!string.IsNullOrEmpty(postId))
                        {
                            postUrl = $"https://m.facebook.com/{userId}/posts/{postId}/";
                        }
                    }
                }
                else if (!postUrl.StartsWith("http"))
                {
                    postUrl = "https://m.facebook.com/" + postUrl;
                }
            }

            string xmlSource = _client.GetXMLSource();
            if (!_client.ElementWithAttributes("//*[@content-desc=\"Make a post on Facebook\"]", 1, xmlSource, false))
            {
                OpenFacebookTimeline();
                _client.Delay(2);
                xmlSource = _client.GetXMLSource();
            }
            string makePostDesc = _client.FindBounds(xmlSource, new List<string> { "//*[@content-desc=\"Make a post on Facebook\"]" }, 1).FirstOrDefault() ?? "";

            string foundElement = "";
            int retryTapToRetry = 0;
            int maxRetryTapToRetry = 3;
            int retryMakePostDesc = 0;
            int maxRetryMakePostDesc = 3;

            while (OpenFacebookLink(accountId, statusText, postUrl))
            {
                int tryCount = 1;
                bool breakOuterLoop = false;
                while (tryCount < 11)
                {
                    xmlSource = _client.GetXMLSource();
                    string currentMakePostDesc = _client.FindBounds(xmlSource, "//*[@content-desc=\"Make a post on Facebook\"]", 1).FirstOrDefault() ?? "";

                    if (string.IsNullOrEmpty(currentMakePostDesc) || currentMakePostDesc != makePostDesc)
                    {
                        foundElement = _client.FindElement(xmlSource, new List<string> {
                    "//*[@content-desc='Post Menu']",
                    "//*[@content-desc='More options' or @text='More options']",
                    "//*[@content-desc='More']",
                    "//*[@text='Tap to retry']",
                    "//*[starts-with(@content-desc,'Join ')]",
                    "//*[@content-desc='See what was used to create this reel']",
                    "//*[@content-desc='Reel. Swipe up to see more.']",
                    "(//*[@text='Stories'])[2]",
                    "//*[@content-desc='Like']"
                }, 1);

                        if (foundElement == "//*[starts-with(@content-desc,'Join ')]")
                        {
                            _client.ElementWithAttributes(foundElement, 1, xmlSource);
                            _client.Delay(2, 3);
                        }
                        else if (foundElement == "//*[@text='Tap to retry']")
                        {
                            if (retryTapToRetry >= maxRetryTapToRetry)
                            {
                                result = 2;
                                breakOuterLoop = true;
                                break;
                            }
                            retryTapToRetry++;
                            _client.ElementWithAttributes("//*[@text='Tap to retry']", 1, xmlSource);
                            _client.Delay(5);
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(foundElement))
                            {
                                result = 1;
                                breakOuterLoop = true;
                                break;
                            }
                            if (ContainsAnyKeyword(xmlSource, "The page you requested cannot be displayed right now"))
                            {
                                result = 2;
                                breakOuterLoop = true;
                                break;
                            }
                            if (ContainsAnyKeyword(xmlSource, "Tap to view story"))
                            {
                                result = 1;
                                breakOuterLoop = true;
                                break;
                            }
                        }

                        // Đăng nhập lại nếu cần
                        if (tryCount % 3 == 0)
                        {
                            int loginStatus = Login();
                            if (loginStatus == 1)
                            {
                                breakOuterLoop = true;
                                break;
                            }
                            if (loginStatus != 0)
                            {
                                result = loginStatus;
                                breakOuterLoop = true;
                                break;
                            }
                        }

                        // Cuộn màn hình nếu chưa vào được bài viết
                        if (!postUrl.Contains("/stories/") || !_client.ElementWithAttributes("(//*[@text='Stories'])[2]", 60, "", false))
                        {
                            ScrollScreen(-1);
                            _client.Delay(1);
                            tryCount++;
                            continue;
                        }
                        result = 1;
                        breakOuterLoop = true;
                        break;
                    }
                    else
                    {
                        retryMakePostDesc++;
                        if (retryMakePostDesc > maxRetryMakePostDesc)
                        {
                            result = 2;
                            breakOuterLoop = true;
                            break;
                        }
                    }
                    tryCount++;
                }
                if (breakOuterLoop) break;
            }

            return result;
        }
        private (bool isSuccess, string error) JoinGroupByUid(int accountId, string statusPrefix, JsonHelper settings)
        {
            string groupId = settings.GetValue("id");
            bool shouldAnswerQuestions = settings.GetBooleanValue("isAnswer");
            List<string> answerList = settings.GetValuesList("lstCauTraLoi");
            bool joinSuccess = false;
            string errorMsg = groupId + ": ";

            try
            {
                SetStatusAccount(accountId, statusPrefix + "Go to Group " + groupId + "...");
                if (!OpenFacebookLink(accountId, statusPrefix, "fb://group/" + groupId))
                {
                    errorMsg += "Lỗi mở link!";
                }
                else
                {
                    SetStatusAccount(accountId, statusPrefix + "Find Join...");
                    string xml = "";
                    string found = "";
                    for (int i = 0; i < 2; i++)
                    {
                        found = _client.FindElement(xml, new List<string>
                {
                    "//*[@content-desc=\"Cancel Request\"]",
                    "//*[@content-desc='Invite Members' or starts-with(@content-desc,'invite others to join')]",
                    "//*[starts-with(@content-desc, 'joined')]",
                    "//*[starts-with(@content-desc, 'Join ')]"
                }, 5);

                        switch (found)
                        {
                            case "//*[starts-with(@content-desc, 'Join ')]":
                                SetStatusAccount(accountId, statusPrefix + "Tap Join...");
                                _client.ElementWithAttributes("//*[starts-with(@content-desc, 'Join ')]", 1, xml);
                                _client.Delay(3);
                                if (shouldAnswerQuestions)
                                {
                                    xml = "";
                                    string answerFound = _client.FindElement(xml, new List<string>
                            {
                                "//android.view.ViewGroup[@content-desc=\"Answer Questions\"]",
                                "//android.view.ViewGroup[@content-desc=\"Cancel Request\"]",
                                "//*[@content-desc='Invite Members' or starts-with(@content-desc,'invite others to join')]",
                                "//android.view.ViewGroup[@content-desc=\"Submit\"]",
                                "//*[starts-with(@content-desc,'followed')]"
                            }, 5);

                                    if (answerFound == "//android.view.ViewGroup[@content-desc=\"Answer Questions\"]" ||
                                        answerFound == "//android.view.ViewGroup[@content-desc=\"Submit\"]")
                                    {
                                        if (answerFound == "//android.view.ViewGroup[@content-desc=\"Answer Questions\"]")
                                        {
                                            SetStatusAccount(accountId, statusPrefix + "Tap Answer...");
                                            _client.ElementWithAttributes(answerFound, 1, xml);
                                            _client.Delay(2);
                                        }
                                        AutoAnswerGroupQuestions(accountId, statusPrefix, answerList);
                                    }
                                }
                                joinSuccess = true;
                                goto EndJoin;
                            case "//*[@content-desc=\"Cancel Request\"]":
                                errorMsg += "Đã gửi yêu cầu vào nhóm trước đó!";
                                goto EndJoin;
                            case "//*[@content-desc='Invite Members' or starts-with(@content-desc,'invite others to join')]":
                            case "//*[starts-with(@content-desc, 'joined')]":
                                errorMsg += "Đã là thành viên của nhóm!";
                                goto EndJoin;
                        }
                        ScrollScreen(-1);
                        _client.Delay(1);
                    }
                }
            EndJoin:;
            }
            catch (Exception ex)
            {
                errorMsg += $"Exception: {ex}!";
            }
            return (joinSuccess, errorMsg);
        }
        public List<string> GetFriendUids(string cookie, string userAgent, string proxy, int timeout)
        {
            List<string> friendUids = new List<string>();
            try
            {
                // Initialize request object
                RequestXNet request = new RequestXNet(cookie, userAgent, proxy, timeout);

                // Extract user id from cookie
                string userId = Regex.Match(cookie, "c_user=(\\d+)").Groups[1].Value;

                // Get fb_dtsg token
                string helpPage = request.RequestGet("https://m.facebook.com/help");
                string fbDtsg = Regex.Match(helpPage, "fb_dtsg\" value=\"(.*?)\"").Groups[1].Value;

                // Query friend list via GraphQL
                string json = request.RequestPost(
                    "https://www.facebook.com/api/graphql",
                    $"q=me(){{friends}}&fb_dtsg={fbDtsg}"
                );

                JsonObject obj = JsonNode.Parse(json)!.AsObject();

                int friendCount = obj[userId]!["friends"]!["nodes"]!.AsArray().Count;
                for (int i = 0; i < friendCount; i++)
                {
                    friendUids.Add(obj[userId]!["friends"]!["nodes"]![i]!["id"]!.GetValue<string>());
                }
            }
            catch (Exception)
            {
                // Optionally log error
            }
            return friendUids;
        }
        public bool EnsureFriendsTab()
        {
            // Check if the Friends tab exists; if not, try to refresh/load it.
            if (!_client.ElementWithAttributes("//*[starts-with(@content-desc,'Friends, tab')]", 1, "", false))
            {
                OpenFacebookTimeline();
            }
            // Attempt to open the Friends tab if it's now present.
            if (_client.ElementWithAttributes("//*[starts-with(@content-desc,'Friends, tab')]", 5, ""))
            {
                // Check for presence of the "Your Friends" or "All friends" sections.
                return _client.ElementWithAttributes(new List<string> { "//*[@content-desc='Your Friends']", "//*[@content-desc='All friends']" }, 10, "");
            }
            return false;
        }

        private void AutoAnswerGroupQuestions(int accountId, string statusPrefix, List<string> answerList)
        {
            string xmlSource = "";
            bool agreedToRules = false;
            int attempt = 0;
            while (true)
            {
                if (attempt >= 10)
                    return;

                xmlSource = "";
                SetStatusAccount(accountId, statusPrefix + "Find EditText...");
                List<string> editTexts = _client.FindBounds(xmlSource, "//android.widget.EditText[@text=\"Write your answer...\"]", 1);
                if (editTexts.Count > 0)
                {
                    SetStatusAccount(accountId, statusPrefix + "Nhập dữ liệu...");
                    for (int i = 0; i < editTexts.Count; i++)
                    {
                        _client.SendTextSlow("(//android.widget.EditText[@text=\"Write your answer...\"])[1]", SubdyHelper.GetStringRandom(answerList));
                        _client.Delay(1, 2);
                    }
                }

                xmlSource = "";
                SetStatusAccount(accountId, statusPrefix + "Find checkbox...");
                List<string> checkboxes = _client.FindBounds(xmlSource, "//android.view.ViewGroup[@content-desc=\"You can choose multiple options\"]/parent::*/child::*", 1);
                if (checkboxes.Count > 0)
                {
                    SetStatusAccount(accountId, statusPrefix + "Check checkbox...");
                    foreach (string checkbox in checkboxes)
                    {
                        var point = new RectangleArea(checkbox).GetCenterPoint();
                        _client.Click(point.X, point.Y);
                    }
                }

                xmlSource = "";
                SetStatusAccount(accountId, statusPrefix + "Find radio...");
                List<string> radios = _client.FindBounds(xmlSource, "//android.view.ViewGroup[@content-desc=\"You can choose one option\"]/parent::*/child::*", 1);
                if (radios.Count > 0)
                {
                    SetStatusAccount(accountId, statusPrefix + "Check radio...");
                    foreach (string radio in radios)
                    {
                        var point = new RectangleArea(radio).GetCenterPoint();
                        _client.Click(point.X, point.Y);
                    }
                }

                if (!agreedToRules)
                {
                    xmlSource = "";
                    SetStatusAccount(accountId, statusPrefix + "Find Agree...");
                    List<string> agreeItems = _client.FindBounds(xmlSource, "//android.view.ViewGroup[starts-with(@content-desc,\"I agree to the group rules\")]", 1);
                    if (agreeItems.Count > 0)
                    {
                        agreedToRules = true;
                        SetStatusAccount(accountId, statusPrefix + "Check Agree...");
                        foreach (string agreeItem in agreeItems)
                        {
                            var point = new RectangleArea(agreeItem).GetCenterPoint();
                            _client.Click(point.X, point.Y);
                        }
                    }
                }

                SetStatusAccount(accountId, statusPrefix + "Scroll...");
                if (ScrollScreen())
                {
                    break;
                }
                attempt++;
            }

            SetStatusAccount(accountId, statusPrefix + "Tap Submit...");
            if (_client.ElementWithAttributes("//android.view.ViewGroup[@content-desc='Submit'][@clickable='true']", 1, ""))
            {
                _client.ElementWithAttributes("//android.view.ViewGroup[@content-desc='Submit'][@clickable='true']", 10);
            }
        }
        private (bool isSuccess, string errorMessage) ProcessPageAction(int accountId, string statusPrefix, JsonHelper settings)
        {
            string pageId = settings.GetValue("id");
            bool isLikePage = settings.GetBooleanValue("isLikePage");
            bool isReviewPage = settings.GetBooleanValue("isReviewPage");
            bool isSuccess = false;
            string message = pageId + ": ";
            try
            {
                if (isLikePage)
                {
                    (isSuccess, message) = LikeFacebookPage(accountId, statusPrefix, settings);
                }
                if (isReviewPage)
                {
                    (isSuccess, message) = ReviewFacebookPage(accountId, statusPrefix, settings);
                }
            }
            catch (Exception ex)
            {
                message += "Exception: " + ex.ToString() + "!";
            }
            return (isSuccess, message);
        }
        private (bool isSuccess, string errorMessage) LikeFacebookPage(int accountId, string statusPrefix, JsonHelper settings)
        {
            //method_31
            string pageId = settings.GetValue("id");
            bool isSuccess = false;
            string message = pageId + ": ";
            try
            {
                while (true)
                {
                    SetStatusAccount(accountId, statusPrefix + "Go to Page " + pageId + "...");
                    string fbLink = "fb://" + (pageId.StartsWith("1000") ? "profile" : "page") + "/" + pageId;
                    if (OpenFacebookLink(accountId, statusPrefix, fbLink))
                    {
                        SetStatusAccount(accountId, statusPrefix + "Like Page...");
                        switch (PerformLikeOrFollowAction(accountId, statusPrefix))
                        {
                            case -1:
                                break;
                            case 1:
                                isSuccess = true;
                                goto end_Like;
                            default:
                                goto end_Like;
                        }
                        continue;
                    }
                    message += "Lỗi mở link!";
                    break;
                }
            end_Like:;
            }
            catch (Exception ex)
            {
                message += "Exception: " + ex.ToString() + "!";
            }
            return (isSuccess, message);
        }

        public int PerformLikeOrFollowAction(int accountId, string statusPrefix)
        {
            string xmlSource = "";
            int tickCount = Environment.TickCount;
            int timeoutSeconds = 10;
            do
            {
                SetStatusAccount(accountId, statusPrefix + "Find Like...");
                xmlSource = _client.GetXMLSource();
                string actionXPath = _client.FindElement(xmlSource, new List<string>
        {
            "//android.view.ViewGroup[@content-desc='Liked']",
            "//android.view.ViewGroup[@content-desc='Following']",
            "//android.view.ViewGroup[@content-desc='Like']",
            "//android.view.ViewGroup[@content-desc='like button']/android.view.ViewGroup",
            "//android.view.ViewGroup[@content-desc='like button']",
            "//android.view.ViewGroup[@content-desc='Follow']"
        }, 1);

                switch (actionXPath)
                {
                    default:
                        SetStatusAccount(accountId, statusPrefix + "Scroll...");
                        if (ScrollScreen(-1))
                        {
                            switch (Login())
                            {
                                case 0:
                                    goto WaitAndRetry;
                                case 1:
                                    return -1;
                            }
                            break;
                        }
                        goto WaitAndRetry;
                    case "//android.view.ViewGroup[@content-desc='Like']":
                    case "//android.view.ViewGroup[@content-desc='like button']/android.view.ViewGroup":
                    case "//android.view.ViewGroup[@content-desc='like button']":
                    case "//android.view.ViewGroup[@content-desc='Follow']":
                        SetStatusAccount(accountId, statusPrefix + "Tap " + actionXPath + "...");
                        _client.ElementWithAttributes(actionXPath, 1, xmlSource);
                        SetStatusAccount(accountId, statusPrefix + "Tap Like, đợi {time}s...", 3);
                        return 1;
                    case "//android.view.ViewGroup[@content-desc='Liked']":
                    case "//android.view.ViewGroup[@content-desc='Following']":
                        break;
                }
                break;
            WaitAndRetry:
                _client.Delay(1);
            }
            while (Environment.TickCount - tickCount < timeoutSeconds * 1000);
            return 0;
        }
        public int InteractWithFacebookPost(int accountId, string statusPrefix, bool doLike, string likeParam, ref int likeCount, bool doComment, ref int commentCount, List<string> commentTemplates, ref List<string> commentQueue, bool doShare, ref int shareCount)
        {
            List<string> list = new List<string>();
            List<string> list2 = new List<string>();
            string commentText = "";

            // Like bài viết
            if (doLike)
            {
                SetStatusAccount(accountId, statusPrefix + "Find Like...");
                string likeElement = _client.FindBounds("", "//*[contains(@content-desc, \"Like\")]", 1).FirstOrDefault();
                if (!string.IsNullOrEmpty(likeElement))
                {
                    var likeCoords = likeElement.Split(new string[3] { "[", ",", "]" }, StringSplitOptions.RemoveEmptyEntries);
                    if (likeCoords.Length >= 4)
                    {
                        Point point = new RectangleArea(likeElement).GetCenterPoint();
                        Point point2 = new RectangleArea("[35," + likeCoords[1] + "][65," + likeCoords[3] + "]").GetCenterPoint();
                        SetStatusAccount(accountId, statusPrefix + "Tap Reaction...");
                        _client.Swipe(point.X, point.Y, point2.X, point2.Y);
                        _client.Delay(1, 1);

                        string d80AC = "";
                        if (!string.IsNullOrEmpty(likeParam))
                        {
                            char rc = likeParam[SubdyHelper.RandomValue(0, likeParam.Length - 1)];
                            int rv;
                            if (int.TryParse(rc.ToString(), out rv)) d80AC = (rv + 1).ToString();
                        }
                        ReactToPost(d80AC);
                        _client.Delay(1, 1);
                        _client.Swipe(point2.X, point2.Y, point.X, point.Y);
                        _client.Delay(1, 1);
                        likeCount++;
                    }
                    else
                    {
                        SetStatusAccount(accountId, statusPrefix + "Like bounds parse lỗi, bỏ qua...");
                    }
                }
            }

            // Comment bài viết
            if (doComment)
            {
                SetStatusAccount(accountId, statusPrefix + "Find Comment...");
                string xmlSource = _client.GetXMLSource();
                list = _client.FindBounds(xmlSource, new List<string> { "//*[@text=\"Write a comment…\"]" }, 1);
                if (list.Count > 0)
                {
                    Point commentPoint = new RectangleArea(list.Last()).GetCenterPoint();
                    if (commentQueue.Count == 0)
                    {
                        commentQueue = SubdyHelper.CloneList(commentTemplates);
                    }
                    commentText = commentQueue[SubdyHelper.RandomValue(0, commentQueue.Count - 1)];
                    commentQueue.Remove(commentText);
                    commentText = SubdyHelper.SpinText(commentText);

                    SetStatusAccount(accountId, statusPrefix + "Tap Comment...");
                    if (_client.Click(commentPoint.X, commentPoint.Y))
                    {
                        _client.Delay(1, 2);
                        SetStatusAccount(accountId, statusPrefix + "Find EditText...");
                        if (_client.ElementWithAttributes("//android.widget.EditText", 5, "", false))
                        {
                            SetStatusAccount(accountId, statusPrefix + "Nhập dữ liệu...");
                            _client.SendTextSlow("//android.widget.EditText", commentText);
                            _client.Delay(1, 2);
                            SetStatusAccount(accountId, statusPrefix + "Tap Send...");
                            if (_client.ElementWithAttributes("//*[@content-desc=\"Send\"]"))
                            {
                                SetStatusAccount(accountId, statusPrefix + "Tap Send, đợi {time}s...", SubdyHelper.RandomValue(3, 6));
                            }
                            commentCount++;
                        }
                        else
                        {
                            SetStatusAccount(accountId, statusPrefix + "Back...");
                            _client.ATX.Press(PressKey.Back);
                            _client.Delay(1, 2);
                        }
                    }
                }
            }

            // Share bài viết
            if (doShare)
            {
                SetStatusAccount(accountId, statusPrefix + "Find Share...");
                string xmlSource2 = _client.GetXMLSource();
                list2 = _client.FindBounds(xmlSource2, new List<string> { "//*[@content-desc=\"SHARE\"]" }, 1);
                if (list2.Count > 0)
                {
                    Point sharePoint = new RectangleArea(list2.Last()).GetCenterPoint();
                    SetStatusAccount(accountId, statusPrefix + "Tap Share...");
                    if (_client.Click(sharePoint.X, sharePoint.Y))
                    {
                        _client.Delay(1, 2);
                        SetStatusAccount(accountId, statusPrefix + "Find Post...");
                        if (_client.ElementWithAttributes("//android.widget.ImageButton[@content-desc=\"Write Post\"]", 5, "")
                            && _client.ElementWithAttributes("//android.widget.Button[@text =\"POST\"]", 5, ""))
                        {
                            SetStatusAccount(accountId, statusPrefix + "Tap Post, đợi {time}s...", SubdyHelper.RandomValue(1, 3));
                            shareCount++;
                        }
                        else
                        {
                            SetStatusAccount(accountId, statusPrefix + "Back...");
                            _client.ATX.Press(PressKey.Back);
                        }
                    }
                }
            }
            return 0;
        }
        private (bool isSuccess, string errorMessage) ReviewFacebookPage(int accountId, string statusPrefix, JsonHelper pageData)
        {
            string pageId = pageData.GetValue("id");
            string reviewContent = pageData.GetValue("content");
            bool isSuccess = false;
            string message = pageId + ": ";
            if (reviewContent.Trim() == "")
            {
                message += "Không có nội dung!";
            }
            else
            {
                try
                {
                    string xmlSource = "";
                    while (true)
                    {
                        SetStatusAccount(accountId, statusPrefix + "Go to Page " + pageId + "...");
                        string fbLink = "fb://" + (pageId.StartsWith("1000") ? "profile" : "page") + "/" + pageId;
                        if (OpenFacebookLink(accountId, statusPrefix, fbLink))
                        {
                            SetStatusAccount(accountId, statusPrefix + "Review Page...");
                            if (_client.FindElement("", new List<string> {
                        "//android.widget.TextView[contains(@content-desc,\"Reviews, Tab\")]",
                        "//*[@content-desc='Posts, tab']"
                    }, 1) == "" && !_client.ElementWithAttributes("//android.widget.Button[@content-desc=\"Cancel\"]", 1, xmlSource))
                            {
                                continue;
                            }
                            int clickReviewsTabTries = 0;
                            int clickYesTries = 0;
                            int clickHomeTabTries = 0;
                            int waitLoadTries = 0;
                            int maxWaitLoadTries = 1;
                            int tickCount = Environment.TickCount;
                            int timeoutSeconds = 120;
                            while (true)
                            {
                                xmlSource = _client.GetXMLSource();
                                string elementXPath = _client.FindElement(xmlSource, new List<string> {
                            "//*[contains(@content-desc,\"Reviews, Tab\")]",
                            "//*[contains(@content-desc, \"Home\")]",
                            "//*[@content-desc='About, tab']",
                            "//*[@content-desc='How are ratings calculated?']",
                            "//*[@content-desc=\"YES\"]",
                            "//android.widget.EditText[starts-with(@text,'What')]",
                            "//androidx.recyclerview.widget.RecyclerView/parent::*/parent::*/child::*",
                            "//androidx.viewpager.widget.ViewPager/android.widget.FrameLayout/android.widget.FrameLayout/android.view.ViewGroup/android.view.ViewGroup"
                        }, 1);

                                switch (elementXPath)
                                {
                                    case "//*[@content-desc='How are ratings calculated?']":
                                        if (_client.ElementWithAttributes("//*[@content-desc=\"YES\"]", 1, xmlSource, false))
                                        {
                                            if (clickYesTries < 2)
                                            {
                                                clickYesTries++;
                                                _client.ElementWithAttributes("//*[@content-desc=\"YES\"]", 1, xmlSource);
                                                break;
                                            }
                                            message += "Lỗi Click Yes!";
                                        }
                                        else
                                        {
                                            message += "Đã review trước đó!";
                                        }
                                        goto End;
                                    case "//*[contains(@content-desc,\"Reviews, Tab\")]":
                                        if (_client.ElementWithAttributes("//*[@content-desc=\"YES\"]", 1, xmlSource, false))
                                        {
                                            _client.ElementWithAttributes("//*[@content-desc=\"YES\"]", 1, xmlSource);
                                            break;
                                        }
                                        if (clickReviewsTabTries < 2)
                                        {
                                            SetStatusAccount(accountId, statusPrefix + "Tap " + elementXPath + "...");
                                            clickReviewsTabTries++;
                                            _client.ElementWithAttributes(elementXPath, 1, xmlSource);
                                            break;
                                        }
                                        message += "Không tìm thấy nút YES!";
                                        goto End;
                                    case "//android.widget.EditText[starts-with(@text,'What')]":
                                        int inputTries = 0;
                                        while (inputTries < 10)
                                        {
                                            _client.SendTextSlow("//android.widget.EditText", reviewContent);
                                            _client.Delay(1);
                                            if (!ContainsAnyKeyword("", "must be at least 25 characters"))
                                            {
                                                if (_client.ElementWithAttributes("(//androidx.viewpager.widget.ViewPager//android.view.ViewGroup)[last()]"))
                                                    break;
                                                _client.Delay(1);
                                                inputTries++;
                                                continue;
                                            }
                                            message = message + "Content < 25 ký tự (content: " + reviewContent + ")!";
                                            goto End;
                                        }
                                        break;
                                    case "//*[contains(@content-desc, \"Home\")]":
                                        string foundHome = _client.FindBounds(xmlSource, "//*[contains(@content-desc, \"Home\")]", 1).FirstOrDefault();
                                        if (string.IsNullOrEmpty(foundHome))
                                            break;
                                        if (clickHomeTabTries < 2)
                                        {
                                            clickHomeTabTries++;
                                            Point poit = new RectangleArea(foundHome).GetCenterPoint();
                                            _client.Swipe(poit.X + _client.GetScreenResolution().Y / 2, poit.Y, poit.X, poit.Y);
                                            break;
                                        }
                                        message += "Không tìm thấy nút Review Page!";
                                        goto End;
                                    case "//androidx.viewpager.widget.ViewPager/android.widget.FrameLayout/android.widget.FrameLayout/android.view.ViewGroup/android.view.ViewGroup":
                                        isSuccess = true;
                                        goto End;
                                    case "//*[@content-desc='About, tab']":
                                        _client.ElementWithAttributes(elementXPath, 1, xmlSource);
                                        _client.Delay(3);
                                        bool foundRateButton = false;
                                        for (int i = 0; i < 5; i++)
                                        {
                                            if (!_client.FindAndClickImage("dataimage\\rate") || !WaitForAnyKeyword(30, "", "How are ratings calculated"))
                                            {
                                                if (ScrollScreen(1, 1, 3000))
                                                    break;
                                                continue;
                                            }
                                            _client.ATX.Press(PressKey.Back);
                                            foundRateButton = true;
                                            break;
                                        }
                                        if (!foundRateButton)
                                        {
                                            message += "Không tìm thấy nút Review Page!";
                                            goto End;
                                        }
                                        break;
                                    case "//android.widget.ProgressBar":
                                        SetStatusAccount(accountId, statusPrefix + "Loading...");
                                        if (!WaitForPostComplete(60))
                                        {
                                            if (waitLoadTries < maxWaitLoadTries)
                                            {
                                                waitLoadTries++;
                                                goto ContinueOuterWhile;
                                            }
                                            message += "Không load được trang!";
                                            goto End;
                                        }
                                        break;
                                    case "//*[@content-desc=\"YES\"]":
                                        if (clickYesTries >= 2)
                                        {
                                            message += "Lỗi Click Yes!";
                                            goto End;
                                        }
                                        clickYesTries++;
                                        _client.ElementWithAttributes("//*[@content-desc=\"YES\"]", 1, xmlSource);
                                        break;
                                    case "//androidx.recyclerview.widget.RecyclerView/parent::*/parent::*/child::*":
                                        var point = new RectangleArea(_client.FindBounds(xmlSource, elementXPath, 1).LastOrDefault()).GetCenterPoint();
                                        _client.Click(point.X, point.Y);
                                        break;
                                    default:
                                        SetStatusAccount(accountId, statusPrefix + "Scroll...");
                                        if (ScrollScreen())
                                        {
                                            switch (Login())
                                            {
                                                case 1:
                                                    break;
                                                case 0:
                                                    break;
                                                default:
                                                    goto End;
                                            }
                                            goto ContinueOuterWhile;
                                        }
                                        break;
                                }

                                _client.Delay(1);
                                if (Environment.TickCount - tickCount >= timeoutSeconds * 1000)
                                {
                                    message += "Timeout!";
                                    break;
                                }
                                continue;

                            ContinueOuterWhile:
                                continue;
                            }
                            break;
                        }
                        message += "Lỗi mở link!";
                        break;
                    }
                End:;
                }
                catch (Exception ex)
                {
                    message = message + "Exception: " + ex.ToString() + "!";
                }
            }
            return (isSuccess, message);
        }

        public bool WaitForAnyKeyword(int timeoutSeconds, string xmlSource, params string[] keywords)
        {
            try
            {
                int tickCount = Environment.TickCount;
                while (true)
                {
                    if (xmlSource == "")
                    {
                        xmlSource = _client.GetXMLSource();
                    }
                    if (!ContainsAnyKeyword(xmlSource, keywords))
                    {
                        xmlSource = "";
                        if (Environment.TickCount - tickCount < timeoutSeconds * 1000)
                        {
                            _client.Delay(1);
                            continue;
                        }
                        break;
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {

            }
            return false;
        }





















        private bool RemoveSpamNotifications()
        {
            bool result = false;
            for (int i = 0; i < 5; i++)
            {
                string notifyImage = _client.FindImageRegion("dataimage\\282Notify");
                if (string.IsNullOrEmpty(notifyImage))
                {
                    break;
                }

                List<string> notificationElements = _client.FindBounds("", "//androidx.recyclerview.widget.RecyclerView[@resource-id='android:id/list']/child::*", 10);
                string targetNotification = "";
                foreach (var element in notificationElements)
                {
                    if (new RectangleArea(element).Intersects(notifyImage))
                    {
                        targetNotification = element;
                        break;
                    }
                }
                if (string.IsNullOrEmpty(targetNotification))
                {
                    break;
                }

                string manageSettingsElement = "";
                List<string> manageSettingsElements = _client.FindBounds("", "//*[@content-desc=\"Manage the notification's settings\"]", 10);
                foreach (var settingsElement in manageSettingsElements)
                {
                    if (new RectangleArea(targetNotification).Intersects(settingsElement))
                    {
                        manageSettingsElement = settingsElement;
                        break;
                    }
                }
                var point = new RectangleArea(manageSettingsElement).GetCenterPoint();
                _client.Click(point.X, point.Y);
                _client.ElementWithAttributes("//*[@content-desc='Remove this notification']", 10, "");
                result = true;
                _client.Delay(3);
            }
            return result;
        }
        public bool OpenNotificationTab()
        {
            _mainService.SetStatus("Open Notify...", 2);
            int startTick = Environment.TickCount;
            while (true)
            {
                if (Environment.TickCount - startTick < 30000)
                {
                    string xmlSource = _client.GetXMLSource();
                    if (_client.ElementWithAttributes(new List<string> { "//*[contains(@content-desc, \"Notifications\")]", "//*[contains(@content-desc, \"Notifications, tab\")]" }, 1, xmlSource, true))
                    {
                        break;
                    }
                    // Find the Notifications tab and tap it if found
                    string notifyTabXPath = _client.FindElement(xmlSource, new List<string> { "//*[contains(@content-desc, \"Notifications\")]", "//*[contains(@content-desc, \"Notifications, tab\")]" }, 1);
                    if (string.IsNullOrEmpty(notifyTabXPath))
                    {
                        OpenFacebookTimeline();
                    }
                    else
                    {
                        _client.ElementWithAttributes(notifyTabXPath, 1, xmlSource);
                    }
                    _client.Delay(2);
                    continue;
                }
                return false;
            }
            return true;
        }
        public int Login()
        {
            //method_22
            return 0;
        }
        private (bool isSuccess, string error) FollowProfileAndReport(int accountId, string statusPrefix, JsonHelper settings)
        {
            string rawId = settings.GetValue("id");
            bool isSuccess = false;
            string statusMessage = rawId + ": ";
            try
            {
                string userId = Regex.Match(rawId, "\\d+").Value;
                SetStatusAccount(accountId, statusPrefix + "Go to Profile " + userId + "...");
                if (!OpenFacebookLink(accountId, statusPrefix, "fb://profile/" + userId))
                {
                    statusMessage += "Failed to open profile link!";
                }
                else
                {
                    SetStatusAccount(accountId, statusPrefix + "Following user...");
                    string xmlSource = "";
                    int followAttempts = 0;
                    int startTick = Environment.TickCount;
                    int timeoutSeconds = 60;

                    while (true)
                    {
                        xmlSource = _client.GetXMLSource();
                        string foundXPath = _client.FindElement(xmlSource, new List<string> {
                    "//*[@content-desc='Follow']",
                    "//*[@content-desc='Following']",
                    "//*[@content-desc='More']"
                }, 10);
                        xmlSource = _client.GetXMLSource();
                        switch (foundXPath)
                        {
                            case "//*[@content-desc='Follow']":
                                if (followAttempts <= 2)
                                {
                                    followAttempts++;
                                    _client.ElementWithAttributes("//*[@content-desc='Follow']", 1, xmlSource);
                                    break;
                                }
                                statusMessage += "Facebook reported error when clicking Follow!";
                                goto endFollow;

                            case "//*[@content-desc='More']":
                                if (!_client.ElementWithAttributes("//*[@content-desc=\"More\"]", 1, xmlSource))
                                {
                                    break;
                                }
                                while (true)
                                {
                                    Bitmap bitmap = null;
                                    string imageResult = _client.FindMatchingImage(10, ref bitmap, new List<string> { "dataimage\\following", "dataimage\\follow" });
                                    if (imageResult == "dataimage\\following")
                                    {
                                        if (followAttempts > 0)
                                            isSuccess = true;
                                        statusMessage += "Already followed previously!";
                                        break;
                                    }
                                    else if (imageResult == "dataimage\\follow")
                                    {
                                        if (followAttempts <= 2)
                                        {
                                            followAttempts++;
                                            _client.IsImageMatch(imageResult, bitmap);
                                            _client.Delay(2);
                                            if (Environment.TickCount - startTick <= 30000)
                                                continue;
                                            goto delayLoop;
                                        }
                                        statusMessage += "Facebook reported error when clicking Follow!";
                                        break;
                                    }
                                    else
                                    {
                                        statusMessage += "Follow button not displayed!";
                                        break;
                                    }
                                }
                                goto endFollow;

                            case "//*[@content-desc='Following']":
                                if (followAttempts > 0)
                                    isSuccess = true;
                                statusMessage += "Already followed previously!";
                                goto endFollow;
                        }
                        goto delayLoop;

                    delayLoop:
                        _client.Delay(2);
                        if (Environment.TickCount - startTick > timeoutSeconds * 1000)
                        {
                            statusMessage += "Timeout!";
                            break;
                        }
                    }
                }
            endFollow:;
            }
            catch (Exception ex)
            {
                statusMessage += "Exception: " + ex.ToString() + "!";
            }
            return (isSuccess, statusMessage);
        }
        public int InteractWithPost(int accountId, string statusPrefix, JsonHelper settings, string actionDescription)
        {
            //B804088D
            List<string> postIds = settings.GetValuesList("txtIdPost");
            int minDelay = settings.GetIntType("nudTimeFrom");
            int maxDelay = settings.GetIntType("nudTimeTo");
            bool doInteract = settings.GetBooleanValue("ckbInteract");
            bool doShareWall = settings.GetBooleanValue("ckbShareWall");
            bool doComment = settings.GetBooleanValue("ckbComment");
            List<string> comments = settings.GetValuesList("txtComment", settings.GetIntType("typeNganCach"));
            doComment = comments.Count > 0;
            int result = 0;

            try
            {
                string status = statusPrefix + "Đang" + " " + actionDescription + ": ";
                if (postIds.Count == 0)
                {
                    //if (!Base.bool_0)
                    //    return 0;
                    goto InteractBlock;
                }

                string postId = postIds[accountId % postIds.Count];
                if (SubdyHelper.IsAllDigits(postId))
                {
                    postId = postId.StartsWith("1000") ? ("fb://profile/" + postId) : ("fb://group/" + postId);
                    bool opened = false;
                    int tryCount = 0;

                    while (tryCount < 3)
                    {
                        if (!OpenFacebookLink(accountId, status, postId))
                            break;

                        for (int i = 0; i < 20; i++)
                        {
                            string authorProfileBounds = _client.FindBounds("", "//*[@content-desc=\"Author's profile\"]", 1).FirstOrDefault();
                            if (!string.IsNullOrEmpty(authorProfileBounds))
                            {
                                Point p = new RectangleArea(authorProfileBounds).GetCenterPoint();
                                if (p.Y + 200 < _client.GetScreenResolution().Y)
                                {
                                    _client.Click(p.X, p.Y + 200);
                                    _client.Delay(3);
                                }
                            }
                            if (!GetCurrentActivity().Contains("com.facebook.fbshorts.viewer.activity.FbShortsViewerActivity"))
                            {
                                if (ScrollScreen())
                                    break;
                                _client.Delay(2);
                                continue;
                            }
                            opened = true;
                            break;
                        }
                        if (!opened && _client.ElementWithAttributes("//*[@content-desc='SHARE']", 1, "", false))
                        {
                            tryCount++;
                            continue;
                        }
                        if (!opened)
                            break;
                        goto InteractBlock;
                    }
                }
                else if (OpenFacebookLink(accountId, status, postId))
                {
                    goto InteractBlock;
                }

                goto EndBlock;

            InteractBlock:
                SetStatusAccount(accountId, status + "Xem, " + "đợi" + " {time}s...", SubdyHelper.RandomValue(minDelay, maxDelay + 1));
                SetStatusAccount(accountId, status + "Tương tác...");
                int interactResult = 0;
                int commentResult = 0;
                int shareResult = 0;
                List<string> usedComments = new List<string>();

                for (int j = 0; j < 5; j++)
                {
                    HandleFacebookComment(accountId, status, doInteract, "0",
                        ref interactResult, doComment, ref commentResult,
                        comments, ref usedComments, doShareWall, ref shareResult, 1);

                    if (interactResult <= 0 && commentResult <= 0 && (doInteract || doComment || doShareWall))
                    {
                        SetStatusAccount(accountId, status + "Scroll...");
                        if (ScrollScreen() && !HandlePopups())
                            break;
                        continue;
                    }
                    break;
                }

            EndBlock:
                ;
            }
            catch
            {
                result = -1;
            }
            return result;
        }
        private readonly object _fileLock = new object();
        private const string PostsFile = "posts.txt";
        public static List<string> GetSuggestedPageIds(string cookie, string proxy, int timeout)
        {
            //smethod_0
            var result = new List<string>();

            try
            {
                // Gửi request với cookie + proxy
                var request = new RequestXNet(cookie, "", proxy, timeout);
                string html = request.RequestGet("https://mbasic.facebook.com/pages/?viewallpywo=1");

                // Lấy list id từ page_suggestion_
                var suggestedIds = Regex.Matches(html, @"page_suggestion_(\d+)""")
                                        .Cast<Match>()
                                        .Select(m => m.Groups[1].Value)
                                        .ToList();

                // Lấy list id từ query string id=...
                var existingIds = Regex.Matches(html, @"id=(\d+)&")
                                       .Cast<Match>()
                                       .Select(m => m.Groups[1].Value)
                                       .ToList();

                // Kết quả = những id gợi ý nhưng chưa có trong danh sách id query
                result = suggestedIds.Except(existingIds).ToList();
            }
            catch
            {
                // giữ nguyên silent catch như bản gốc
            }

            return result;
        }
        private void AppendPostToFile(string postContent)
        {
            try
            {
                lock (_fileLock)
                {
                    File.AppendAllText(PostsFile, postContent + Environment.NewLine);
                }
            }
            catch (IOException ex)
            {
                // Ghi log hoặc xử lý khi có lỗi I/O
                Debug.WriteLine($"[ERROR] Cannot write to {PostsFile}: {ex.Message}");
            }
        }
        private int CreateGroups(int accountId, string prefixStatus, JsonHelper config, string actionName)
        {
            //B1B3FD1A
            string baseStatus = prefixStatus + "Đang" + " " + actionName + ": ";
            SetStatusAccount(accountId, baseStatus + "Đang chạy...");

            List<string> groupNames = config.GetValuesList("txtTenNhom");
            int minCount = config.GetIntType("nudSoLuongFrom");
            int maxCount = config.GetIntType("nudSoLuongTo");
            int targetCount = SubdyHelper.RandomValue(minCount, maxCount);

            int created = 0;
            int retryCount = 0;
            const int maxRetry = 6;

            while (created < targetCount)
            {
                SetStatusAccount(accountId, $"{baseStatus}({created + 1}/{targetCount})...");

                // Mở tab Groups
                DeplinkFacebook("fb://groups_targeted_tab");
                _client.Delay(2);

                int startTick = Environment.TickCount;

                while (true)
                {
                    string pageSource = _client.GetXMLSource();
                    string xpath = _client.FindElement(pageSource, new List<string>
            {
                "//android.widget.ProgressBar",
                "//*[@text='Tap to retry']",
                "//*[@content-desc='Your groups']",
                "//android.widget.EditText[@text='Name your group']",
                "//*[starts-with(@content-desc,'Public, Anyone can see')]",
                "//*[@content-desc='Create group'][@clickable='true']",
                "//*[@text='Invite members']",
                "//*[@content-desc='Open create options']",
                "//*[@content-desc='Close create options']/parent::*/child::*[1]",
                "//*[contains(@content-desc,'Create a Group')]"
            }, 1);

                    switch (xpath)
                    {
                        case "//android.widget.ProgressBar":
                            SetStatusAccount(accountId, baseStatus + "Loading...");
                            break;

                        case "//*[@text='Tap to retry']":
                            if (retryCount++ >= maxRetry)
                                return created;

                            ScrollScreen(-1);
                            break;

                        case "//*[@content-desc='Your groups']":
                            string createXpath = _client.ElementWithAttributes("//*[@content-desc='Open create options']", 1, pageSource, false)
                                ? "//*[@content-desc='Open create options']"
                                : _client.ElementWithAttributes("//*[@content-desc='Create actions entry point']", 1, pageSource, false)
                                    ? "//*[@content-desc='Create actions entry point']"
                                    : "//*[@content-desc='Create group']";

                            _client.ElementWithAttributes(createXpath, 1, pageSource);
                            break;

                        case "//android.widget.EditText[@text='Name your group']":
                            string groupName = SubdyHelper.ReplaceWithRandom(SubdyHelper.GetStringRandom(groupNames), 2);
                            _client.SendTextSlow(xpath, groupName);
                            _client.Delay(1);
                            _client.ElementWithAttributes("//*[@content-desc='Choose privacy']", 1, pageSource);
                            break;

                        case "//*[starts-with(@content-desc,'Public, Anyone can see')]":
                            _client.ElementWithAttributes(xpath, 1, pageSource);
                            _client.Delay(1);
                            _client.ElementWithAttributes("//*[starts-with(@content-desc,'Done')]");
                            break;

                        case "//*[@content-desc='Create group'][@clickable='true']":
                        case "//*[@content-desc='Open create options']":
                        case "//*[@content-desc='Close create options']/parent::*/child::*[1]":
                        case "//*[contains(@content-desc,'Create a Group')]":
                            _client.ElementWithAttributes(xpath, 1, pageSource);
                            break;

                        case "//*[@text='Invite members']":
                            created++;
                            if (created >= targetCount)
                                return created;
                            goto NextGroup; // ra ngoài để tạo nhóm tiếp theo

                        default:
                            SetStatusAccount(accountId, baseStatus + "Scroll...");
                            if (ScrollScreen())
                            {
                                int scrollResult = Login();
                                if (scrollResult == 1)
                                    continue; // lặp lại vòng while
                                else if (scrollResult != 0)
                                    return created;
                            }
                            break;
                    }

                    _client.Delay(2);

                    if (Environment.TickCount - startTick > 300000) // timeout 5 phút
                        return created;
                }

            NextGroup:
                continue;
            }

            return created;
        }
        public static List<string> GetUserGroups(string cookie, string proxy, int timeout, bool useGraphQL)
        {
            //smethod_1
            List<string> groups = new List<string>();
            try
            {
                string userId = Regex.Match(cookie, "c_user=(\\d+)").Groups[1].Value;
                RequestXNet request = new RequestXNet(cookie, "", proxy, timeout);

                if (useGraphQL)
                {
                    string html = request.RequestGet("https://mobile.facebook.com/help/");
                    string fb_dtsg = Regex.Match(html, "fb_dtsg\" value=\"(.*?)\"").Groups[1].Value;

                    string graphQL = $"q=nodes({userId}){{groups{{nodes{{id,name,viewer_post_status,visibility,group_member_profiles{{count}}}}}}}}&fb_dtsg={fb_dtsg}";
                    string response = request.RequestPost("https://www.facebook.com/api/graphql/", graphQL)
                                             .Replace("for (;;);", "");

                    JsonObject json = JsonNode.Parse(response)!.AsObject();
                    foreach (JsonNode? item in json[userId]!["groups"]!["nodes"]!.AsArray())
                    {
                        try
                        {
                            groups.Add(string.Format(
                                "{0}|{1}|{2}|{3}",
                                item["id"],
                                item["name"],
                                item["group_member_profiles"]!["count"],
                                item["viewer_post_status"]!.ToString() == "CAN_POST_WITHOUT_APPROVAL" ? "False" : "True"
                            ));
                        }
                        catch { }
                    }
                }
                else
                {
                    string html = request.RequestGet("https://mobile.facebook.com/help/");
                    string fb_dtsg_ag = Regex.Match(html, SubdyHelper.DecodeBase64("ImR0c2dfYWciOnsidG9rZW4iOiIoLio/KSI=")).Groups[1].Value;

                    string url = $"https://www.facebook.com/ajax/typeahead/first_degree.php?fb_dtsg_ag={fb_dtsg_ag}&filter%5B0%5D=group&viewer={userId}&__user={userId}&__a=1&__dyn=&__comet_req=0&jazoest=26581";
                    string response = request.RequestGet(url).Replace("for (;;);", "");

                    JsonObject json = JsonNode.Parse(response)!.AsObject();
                    foreach (JsonNode? item in json["payload"]!["entries"]!.AsArray())
                    {
                        try
                        {
                            groups.Add(string.Format(
                                "{0}|{1}|{2}",
                                item["uid"],
                                item["text"],
                                item["size"]
                            ));
                        }
                        catch { }
                    }
                }
            }
            catch { }

            return groups;
        }
        public bool OpenReel(string url)
        {
            _mainService.SetStatus("Open reel...", 2);
            int tickCount = Environment.TickCount;
            while (true)
            {
                if (ContainsAnyKeyword("", "Pick viewer content to show", "Navigate to your Reels profile"))
                {
                    return true;
                }
                string current = GetCurrentActivity();

                if (current.Contains("com.facebook.fbshorts") ||
                    current.Contains("com.facebook.katana/.immersiveactivity.ImmersiveActivity"))
                {
                    return true;
                }
                else
                {
                    _client.Shell(
                        $"am start -a android.intent.action.VIEW -d \"{url}\" {FacebookHander.Package(PlatformModel.Facebook)}"
                    );
                }
                if (Environment.TickCount - tickCount >= 60 * 1000)
                {
                    break;
                }
            }
            return false;

        }
        public bool StartActivity(string activityName)
        {
            //method_18
            try
            {
                // Chạy lệnh để mở activity (cần root: su -c)
                _client.Shell($"su -c am start -n {activityName}");
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public string GetFacebookTokenAndCookies()
        {
            //AF03B30E
            string accessToken = "";
            string cookies = "";
            string rawAuthData = "";

            try
            {
                for (int i = 0; i < 2; i++)
                {


                    rawAuthData = (i != 0)
                        ? _client.ADB.Shell($"cat data/data/{FacebookHander.Package(PlatformModel.Facebook)}/app_light_prefs/{FacebookHander.Package(PlatformModel.Facebook)}/authentication")
                        : _client.ADB.Shell($"su -c cat data/data/{FacebookHander.Package(PlatformModel.Facebook)}/app_light_prefs/{FacebookHander.Package(PlatformModel.Facebook)}/authentication");

                    if (!string.IsNullOrEmpty(rawAuthData))
                    {
                        try
                        {
                            accessToken = Regex.Match(rawAuthData, "EAAAAU\\S+").Value;
                            string unwanted = Regex.Match(accessToken, "\u0005(.*?)$").Value;
                            accessToken = accessToken.Replace(unwanted, "");
                        }
                        catch
                        {
                            // ignore parsing errors
                        }

                        string json = "{\"data\": [" + Regex.Match(rawAuthData, "\\[(.*?)\\]").Groups[1].Value + "]}";
                        JsonObject authJson = JsonNode.Parse(json)!.AsObject();

                        var authData = authJson["data"]!.AsArray();
                        for (int j = 0; j < authData.Count; j++)
                        {
                            cookies += authData[j]!["name"]!.GetValue<string>() + "=" +
                                       authData[j]!["value"]!.GetValue<string>() + ";";
                        }

                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            break;
                        }
                    }
                }
            }
            catch
            {
                // ignore errors
            }

            return accessToken + "|" + cookies;
        }
        public bool SearchOnFacebook(string query = "", string searchCategory = "people")
        {
            //method_71
            OpenFacebookTimeline();

            bool isOpened;
            if (!(isOpened = _client.ElementWithAttributes("//*[contains(@content-desc, \"Search\")]", 5, "")))
            {
                DeplinkFacebook("fb://search");
                isOpened = true;
            }

            if (isOpened && query != "" && _client.ElementWithAttributes("//*[@class=\"android.widget.EditText\"]"))
            {
                _client.SendTextSlow("//*[@class=\"android.widget.EditText\"]", query, 5);
                _client.Delay(2);
                _client.Shell("input keyevent 66"); // bấm nút Search
                _client.Delay(2);
                _client.ElementWithAttributes($"//*[@content-desc=\"{searchCategory} search results\"]", 10, "");
            }

            return isOpened;
        }
        public int ScrollFeedAndInteract(int accountId, string statusPrefix, int minScrollTime, int maxScrollTime, bool enableLike, string postSelector, int minLike, int maxLike, bool enableComment, int minComment, int maxComment, List<string> commentList, bool enableShare, int minShare, int maxShare, int maxActions = 0, bool specialMode = false, string specialData = "", int delayMin = -1, int delayMax = -1)
        {//method_72
            bool flag = GetCurrentActivity().Contains("com.facebook.fbshorts");
            if (maxActions == 0)
            {
                int int_11 = 0;
                int int_12 = 0;
                int FEB51E = 0;
                int num = 0;
                int num2 = 0;
                int num3 = 0;
                if (enableLike)
                {
                    num = SubdyHelper.RandomValue(minLike, maxLike);
                }
                commentList = commentList.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                List<string> C6832E1A = SubdyHelper.CloneList(commentList);
                if (enableComment)
                {
                    num2 = SubdyHelper.RandomValue(minComment, maxComment);
                }
                if (enableShare)
                {
                    num3 = SubdyHelper.RandomValue(minShare, maxShare);
                }
                int num4 = SubdyHelper.RandomValue(minScrollTime, maxScrollTime);
                int tickCount = Environment.TickCount;
                while (Environment.TickCount - tickCount < num4 * 1000)
                {
                    switch (Login())
                    {
                        case 0:
                            SetStatusAccount(accountId, statusPrefix + "Scroll...");
                            if (ScrollScreen(1, 2) && !HandlePopups())
                            {
                                if (!_client.ElementWithAttributes("//android.view.ViewGroup[@content-desc='Try it']", 1, "", false))
                                {
                                    break;
                                }
                                _client.Shell("input keyevent 4");
                            }
                            HandleFacebookComment(accountId, statusPrefix, enableLike && int_11 < num, postSelector, ref int_11, enableComment && int_12 < num2, ref int_12, commentList, ref C6832E1A, enableShare && FEB51E < num3, ref FEB51E);
                            SetStatusAccount(accountId, statusPrefix + "Delay {time}s...", SubdyHelper.RandomValue(2, 4));
                            continue;
                        case 1:
                            continue;
                    }
                    break;
                }
            }
            else
            {
                int num5 = SubdyHelper.RandomValue(minScrollTime, maxScrollTime);
                int int_13 = 0;
                int int_14 = 0;
                int FEB51E2 = 0;
                int num6 = 0;
                int num7 = 0;
                int num8 = 0;
                if (enableLike)
                {
                    num6 = num5;
                }
                commentList = commentList.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                List<string> C6832E1A2 = SubdyHelper.CloneList(commentList);
                if (enableComment)
                {
                    num7 = num5;
                }
                if (enableShare)
                {
                    num8 = num5;
                }
                int num9 = 1;
                for (int i = 0; i < num5 + 5; i++)
                {
                    int num10 = Login();
                    if (num10 == 1)
                    {
                        OpenFacebookTimeline();
                    }
                    else if (num10 != 0)
                    {
                        break;
                    }
                    bool flag2 = false;
                    if (flag)
                    {
                        if (i > 0)
                        {
                            flag2 = ScrollScreen();
                        }
                    }
                    else
                    {
                        flag2 = ScrollScreen(1, 2);
                    }
                    if (flag2 && !HandlePopups())
                    {
                        break;
                    }
                    HandleFacebookComment(accountId, statusPrefix + $"({num9}/{num5}) ", enableLike && int_13 < num6, postSelector, ref int_13, enableComment && int_14 < num7, ref int_14, commentList, ref C6832E1A2, enableShare && FEB51E2 < num8, ref FEB51E2, maxActions, specialMode, specialData);
                    if (int_13 >= num5 || int_14 >= num5 || FEB51E2 >= num5)
                    {
                        break;
                    }
                    num9 = new List<int> { int_13, int_14, FEB51E2 }.OrderBy((int int_0) => int_0).Last() + 1;
                    if (delayMin > -1 && delayMax > -1)
                    {
                        SetStatusAccount(accountId, statusPrefix + $"({num9}/{num5}), delay {{time}}s...", SubdyHelper.RandomValue(delayMin, delayMax));
                    }
                    else
                    {
                        _client.Delay(3);
                    }
                }
            }
            return 0;
        }
        public bool HandlePopups(string pageSource = "")
        {
            //method_68
            SetStatusAccount(1, "Check popup...");
            int retryCount = 0;
            int maxRetry = 1;

            while (true)
            {
                if (string.IsNullOrEmpty(pageSource))
                {
                    pageSource = _client.GetXMLSource();
                }

                List<string> texts = ExtractTexts(pageSource.ToLower());

                // Trường hợp có popup "profile picture" và "photo"
                if (texts.Count == 2 && texts.Contains("profile picture") && texts.Contains("photo"))
                {
                    ScrollScreen(-1);
                    _client.Delay(1);
                    pageSource = _client.GetXMLSource();
                }

                // Trường hợp có nút Continue
                if (_client.ElementWithAttributes("//android.widget.Button[@text='Continue']", 1, pageSource, false))
                {
                    RectangleArea btnContinue = new RectangleArea(_client.FindBounds(pageSource, "//android.widget.Button[@text='Continue']/parent::*[1]", 1).First());
                    if (btnContinue.Left - btnContinue.Left > 800)
                    {
                        ScrollScreen();
                        _client.Delay(1);
                        pageSource = _client.GetXMLSource();
                    }
                }

                // Popup với "Continue" + "No Thanks"
                if (_client.ElementWithAttributes("//*[@content-desc='Continue']", 1, pageSource, false) &&
                    _client.ElementWithAttributes("//*[@content-desc='No Thanks']", 1, pageSource, false))
                {
                    _client.ElementWithAttributes("//*[@content-desc='No Thanks']", 1, pageSource);
                    _client.Delay(2);
                    pageSource = _client.GetXMLSource();
                }

                // Popup "OK, Use Data" + "Go Back"
                if (_client.ElementWithAttributes("//*[@content-desc='OK, Use Data']", 1, pageSource, false) &&
                   _client.ElementWithAttributes("//*[@content-desc='Go Back']", 1, pageSource, false))
                {
                    _client.ElementWithAttributes("//*[@content-desc='OK, Use Data']", 1, pageSource);
                    _client.Delay(2);
                    pageSource = _client.GetXMLSource();
                }

                // Popup có nút Close
                string closeBtn = _client.FindBounds(pageSource, "//*[@content-desc='Close']", 1).FirstOrDefault();
                if (string.IsNullOrEmpty(closeBtn) || new RectangleArea(closeBtn).Top <= 2300)
                {
                    string foundElement = _client.FindElement(pageSource, new List<string>
            {
                "//*[@text='Tap to view story']",
                "//*[@content-desc='Close' or @text='CLOSE']",
                "//*[@content-desc='Dismiss' or @text='Dismiss']",
                "//*[@text='New! Post in this group without sharing your name.']",
                "//*[@content-desc='deny' or @text='deny']",
                "//*[@text='No thanks']",
                "//*[@content-desc='I ACCEPT']",
                "//*[@content-desc='Allow all cookies']",
                "//*[@text='Try again']",
                "//*[@text='Dismiss list?']",
                "//*[@content-desc='Unplug charger' or @text='Unplug charger']",
                "//*[@package='com.android.phone'][@text='CANCEL']",
                "//*[@text='Accidental touch protection']"
            }, 1);

                    if (!string.IsNullOrEmpty(foundElement))
                    {
                        if (!(foundElement == "//*[@content-desc='Close' or @text='CLOSE']") ||
                            !_client.ElementWithAttributes("//android.widget.ScrollView", 1, pageSource, false))
                        {
                            if (foundElement == "//*[@text='Try again']")
                            {
                                if (retryCount >= maxRetry)
                                {
                                    if (!_client.ElementWithAttributes("//*[@text='GO BACK']", 1, pageSource))
                                    {
                                        _client.Shell("input keyevent 4");
                                    }
                                }
                                else
                                {
                                    retryCount++;
                                    _client.ElementWithAttributes(foundElement, 1, pageSource);
                                }
                                _client.Delay(2);
                                pageSource = "";
                                continue;
                            }

                            if (foundElement == "//*[@content-desc='Unplug charger' or @text='Unplug charger']" ||
                                foundElement == "//*[@text='Accidental touch protection']")
                            {
                                _client.ElementWithAttributes("//*[@content-desc='OK' or @text='OK']", 1, pageSource);
                            }
                            else
                            {
                                switch (foundElement)
                                {
                                    case "//*[@text='Tap to view story']":
                                        return false;
                                    case "//*[@content-desc='Dismiss' or @text='Dismiss']":
                                        if (_client.ElementWithAttributes("//*[@content-desc='Show profile']", 1, pageSource, false))
                                        {
                                            return false;
                                        }
                                        _client.ElementWithAttributes(foundElement, 1, pageSource);
                                        break;
                                    case "//*[@text='Dismiss list?']":
                                        _client.ElementWithAttributes("//*[@text='CONFIRM']", 1, pageSource);
                                        break;
                                    default:
                                        _client.ElementWithAttributes(foundElement, 1, pageSource);
                                        break;
                                }
                            }
                        }
                        else
                        {
                            RectangleArea scrollView = new RectangleArea(_client.FindBounds(pageSource, "//android.widget.ScrollView", 1).First());
                            if (scrollView.Left != 0)
                            {
                                var size = _client.GetScreenResolution();
                                _client.Swipe((scrollView.Left + scrollView.Right) / 2, scrollView.Top, (scrollView.Left + scrollView.Right) / 2, (scrollView.Bottom < size.Y) ? scrollView.Bottom : (scrollView.Bottom - 10));
                            }
                            else
                            {
                                RectangleArea element = new RectangleArea(_client.FindBounds(pageSource, foundElement, 1).First());
                                if (element.Left == 0)
                                {
                                    _client.Shell("input keyevent 4");
                                }
                                else
                                {
                                    _client.ElementWithAttributes(foundElement, 1, pageSource);
                                }
                            }
                        }

                        _client.Delay(3);
                        return true;
                    }

                    // Một số popup khác
                    if (ContainsAnyKeyword(pageSource, "Review Your Data Settings"))
                    {
                        _client.ElementWithAttributes("//*[@content-desc='Get Started']", 1, pageSource);
                        pageSource = "";
                    }
                    else if (_client.ElementWithAttributes("//*[@content-desc='Accept and Continue']", 1, "", false))
                    {
                        _client.ElementWithAttributes("//*[@content-desc='Accept and Continue']", 1, pageSource);
                        pageSource = "";
                    }

                    if (string.IsNullOrEmpty(pageSource))
                    {
                        continue;
                    }

                    // Kiểm tra số lượng text
                    switch (ExtractTexts(pageSource).Distinct().Count())
                    {
                        case 1:
                            if (!ScrollScreen(-1))
                            {
                                pageSource = _client.GetXMLSource();
                                continue;
                            }
                            break;
                        case 0:
                            if (!string.IsNullOrEmpty(pageSource) && !GetCurrentActivity().Contains("pagerecommendations"))
                            {
                                _client.Shell("input keyevent 4");
                                return true;
                            }
                            break;
                    }
                    break;
                }

                return false;
            }
            return false;
        }

        public async Task<int> RunVideoFeedActions(int accountId, string statusPrefix, int minDuration, int maxDuration, bool enableLike, int minLike, int maxLikeRange, bool enableComment, int minComment, int maxCommentRange, List<string> commentList, bool enableShare, int minShare, int maxShareRange)
        {

            //E40A2E10
            int likeCount = 0;
            int commentCount = 0;
            int shareCount = 0;

            int likeTarget = 0;
            int commentTarget = 0;
            int shareTarget = 0;

            if (enableLike)
                likeTarget = SubdyHelper.RandomValue(minLike, maxLikeRange);

            commentList = commentList.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            List<string> preparedComments = SubdyHelper.CloneList(commentList);

            if (enableComment)
                commentTarget = SubdyHelper.RandomValue(minComment, maxCommentRange);

            if (enableShare)
                shareTarget = SubdyHelper.RandomValue(minShare, maxShareRange);

            int feedDuration = SubdyHelper.RandomValue(minDuration, maxDuration);
            int startTick = Environment.TickCount;

            while (Environment.TickCount - startTick < feedDuration * 1000)
            {
                //Check login
                //int actionResult = method_22(device, accountId, statusPrefix);
                //if (actionResult == 1 || actionResult != 0)
                //    break;

                SetStatusAccount(accountId, statusPrefix + "Scroll...");
                if (ScrollScreen(1, 2) && !HandlePopups())
                    break;

                SetStatusAccount(accountId, statusPrefix + "Delay {time}s...", SubdyHelper.RandomValue(3, 6));

                if (!(enableLike || enableComment || enableShare))
                    continue;

                SetStatusAccount(accountId, statusPrefix + "Tap Video...");
                if (_client.ElementWithAttributes("//android.view.ViewGroup[@content-desc=\"Video\"]"))
                {
                    if (!_client.ElementWithAttributes("//android.view.ViewGroup[@content-desc=\"More options\"]", 3, click: false))
                    {
                        InteractWithPost(accountId, statusPrefix,
                                   enableLike && likeCount < likeTarget, "", ref likeCount,
                                   enableComment && commentCount < commentTarget, ref commentCount,
                                   commentList, ref preparedComments,
                                   enableShare && shareCount < shareTarget, ref shareCount);
                    }
                    else
                    {
                        ScrollScreen();
                        HandleFacebookComment(accountId, statusPrefix,
                                    enableLike && likeCount < likeTarget, "0124", ref likeCount,
                                    enableComment && commentCount < commentTarget, ref commentCount,
                                    commentList, ref preparedComments,
                                    enableShare && shareCount < shareTarget, ref shareCount);
                    }
                    _client.Shell("input keyevent 4");
                    _client.Delay(2);
                }
            }

            return 0;
        }
        public int HandleFacebookComment(int accountId, string accountName, bool enableLike, string reactionType, ref int likeCount, bool enableComment, ref int commentCount, List<string> commentTemplates, ref List<string> commentPool, bool enableShare, ref int shareCount, int delaySeconds = 0, bool isRandomReaction = false, string customMessage = "")
        {
            // C13AAEBB
            // Kiểm tra có đang trong Facebook Shorts không
            bool isShorts = GetCurrentActivity().Contains("com.facebook.fbshorts");
            if (isShorts)
            {
                reactionType = "1"; // ép Like
                isRandomReaction = false;
            }

            string pageSource = _client.GetXMLSource();

            bool hasCommentBox = _client.ElementWithAttributes("//android.widget.EditText[@text='Write a comment…']", 1, click: false);

            bool hasCommentButton = _client.FindBounds(pageSource, new List<string>
                {
            "//*[@content-desc=\"Comment Button\"]",
            "//*[@content-desc=\"Answer Button\"]",
            "//*[@text='Answer']",
            "//*[@text=\"Comment\"]",
            "//*[@content-desc=\"Comment\"]"
                }, 1
            ).Count > 0;

            if (hasCommentBox && !hasCommentButton)
            {
                // Nếu có khung comment mà chưa có nút comment
                CommentAction(isShorts, accountId, accountName, enableLike, reactionType,
                      ref likeCount, enableComment, ref commentCount, commentTemplates, ref commentPool,
                      enableShare, ref shareCount, delaySeconds, isRandomReaction, customMessage);

                TapReaction(isShorts, accountId, accountName, enableLike, reactionType,
                    ref likeCount, enableComment, ref commentCount, commentTemplates, ref commentPool,
                    enableShare, ref shareCount, delaySeconds, isRandomReaction, customMessage);

                TapShare(isShorts, accountId, accountName, enableLike, reactionType,
                   ref likeCount, enableComment, ref commentCount, commentTemplates, ref commentPool,
                   enableShare, ref shareCount, delaySeconds, isRandomReaction, customMessage);
            }
            else
            {
                // Trường hợp còn lại

                TapReaction(isShorts, accountId, accountName, enableLike, reactionType,
                   ref likeCount, enableComment, ref commentCount, commentTemplates, ref commentPool,
                   enableShare, ref shareCount, delaySeconds, isRandomReaction, customMessage);

                CommentAction(isShorts, accountId, accountName, enableLike, reactionType,
                       ref likeCount, enableComment, ref commentCount, commentTemplates, ref commentPool,
                       enableShare, ref shareCount, delaySeconds, isRandomReaction, customMessage);


                TapShare(isShorts, accountId, accountName, enableLike, reactionType,
                       ref likeCount, enableComment, ref commentCount, commentTemplates, ref commentPool,
                       enableShare, ref shareCount, delaySeconds, isRandomReaction, customMessage);
            }

            return 0;
        }
        private void TapReaction(bool forceTap, int accountIndex, string statusPrefix, bool enableReaction, string reactionOption, ref int reactionCount, bool someFlag, ref int someCounter, List<string> commentList, ref List<string> usedComments, bool extraOption, ref int extraCounter, int mode = 0, bool dummyFlag = false, string extraData = "")
        {
            //method_74
            if (!enableReaction)
                return;

            SetStatusAccount(accountIndex, statusPrefix + "Find Like...");
            string pageSource = _client.GetXMLSource();

            // Tìm tất cả các nút Like/Reaction
            List<string> likeButtons = _client.FindBounds(pageSource, new List<string>
    {
        "//*[@content-desc=\"Like button. Double tap and hold to react.\"]",
        "//*[@content-desc=\"Like Button\"]",
        "//*[@content-desc='Like']",
        "//*[contains(@content-desc, ', pressed. Double tap and hold to change reaction.')]"
    }, 1);

            SetStatusAccount(accountIndex, statusPrefix + "Find Like: " + likeButtons.Count);
            if (likeButtons.Count <= 0 || (mode != 1 && SubdyHelper.RandomValue(1, 100) % 3 != 0))
                return;

            // Lấy nút Like phù hợp
            string targetButton = likeButtons.FirstOrDefault(x => new RectangleArea(x).Left == 0) ?? likeButtons.First();
            Point point = new RectangleArea(targetButton).GetCenterPoint();

            // Xác định phản ứng (reaction) từ chuỗi reactionOption
            string reactionCode = "";
            if (!string.IsNullOrEmpty(reactionOption))
            {
                int index = SubdyHelper.RandomValue(0, reactionOption.Length - 1);
                reactionCode = (Convert.ToInt32(reactionOption[index].ToString()) + 1).ToString();
            }

            SetStatusAccount(accountIndex, statusPrefix + "Tap Reaction...");
            bool reacted = false;

            if (forceTap)
            {
                // Chạm trực tiếp nếu không có reaction child
                if (!_client.ElementWithAttributes("//*[@content-desc='Like']/child::*", 1, pageSource, false))
                {
                    _client.Click(point.X, point.Y);
                }
                reacted = true;
            }
            else
            {
                _client.LongClick(point.X, point.Y, 1000);
                reacted = ReactToPost(reactionCode);
            }

            if (reacted)
            {
                reactionCount++;
                SetStatusAccount(accountIndex, statusPrefix + "Reaction, " + "đợi" + " {time}s...", SubdyHelper.RandomValue(1, 3));
            }
        }
        private void CommentAction(bool enableComment, int accountIndex, string statusPrefix, bool enableTextComment, string reactionIndex, ref int commentCount, bool enableImageComment, ref int imageCommentCount, List<string> commentPool, ref List<string> commentHistory, bool someFlag, ref int flagCounter, int specialMode = 0, bool extraOption = false, string extraData = "")
        {
            //method_75
            bool needsBack = false;
            List<string> foundComments = new List<string>();
            string commentText = "";

            if (!(enableTextComment || extraOption))
                return;

            SetStatusAccount(accountIndex, statusPrefix + "Find Comment...");
            string pageSource = _client.GetXMLSource();

            foundComments = _client.FindBounds(pageSource, new List<string>
    {
        "//*[@content-desc=\"Comment Button\"]",
        "//*[@content-desc=\"Answer Button\"]",
        "//*[@text='Answer']",
        "//*[@text=\"Comment\"]",
        "//*[@content-desc=\"Comment\"]",
        "//android.widget.EditText[@resource-id='composerInput']",
        "//android.widget.EditText[@text='Write a comment…']"
    }, 1);

            SetStatusAccount(accountIndex, statusPrefix + $"Find Comment: {foundComments.Count}");
            if (foundComments.Count <= 0)
                return;

            needsBack = !_client.ElementWithAttributes("//android.widget.EditText[@text='Write a comment…']", 1, pageSource, false);

            if (specialMode != 1 && SubdyHelper.RandomValue(1, 100) % 3 != 0)
                return;

            Point commentPoint = new RectangleArea(foundComments.First()).GetCenterPoint();
            SetStatusAccount(accountIndex, statusPrefix + "Tap Comment...");
            if (!_client.Click(commentPoint.X, commentPoint.Y))
                return;

            _client.Delay(2);
            int attempt;
            for (attempt = 0; attempt < 2; attempt++)
            {
                SetStatusAccount(accountIndex, statusPrefix + "Find EditText...");
                if (!_client.ElementWithAttributes("//android.widget.EditText", 5, pageSource, false))
                {
                    if (!_client.ElementWithAttributes("//android.widget.LinearLayout[@content-desc=\"Comment input box\"]"))
                        break;

                    continue;
                }

                if (enableTextComment)
                {
                    if (commentHistory.Count == 0)
                        commentHistory = SubdyHelper.CloneList(commentPool);

                    commentText = commentHistory[SubdyHelper.RandomValue(0, commentHistory.Count - 1)];
                    commentHistory.Remove(commentText);
                    commentText = SubdyHelper.SpinText(commentText);

                    SetStatusAccount(accountIndex, statusPrefix + "Nhập dữ liệu...");
                    _client.SendTextSlow("//android.widget.EditText", commentText);
                    _client.Delay(2);
                }

                if (extraOption && UploadMedia(accountIndex, statusPrefix, _client, extraData))
                {
                    SetStatusAccount(accountIndex, statusPrefix + "Find Camera...");
                    if (_client.ElementWithAttributes("//android.view.ViewGroup[@content-desc='Show photos and videos']", 5, ""))
                    {
                        SetStatusAccount(accountIndex, statusPrefix + "Select image...");
                        for (int j = 0; j < 10; j++)
                        {
                            string pageSrc = _client.GetXMLSource();
                            string elementXPath = _client.FindElement(pageSrc, new List<string>
            {
                "//android.widget.Button[@text='Allow']",
                "//android.widget.Button[@text='Enable gallery access']",
                "//android.view.ViewGroup/android.view.ViewGroup[@content-desc='Photo' or @content-desc='Video']"
            }, 1);

                            if (elementXPath == "//android.view.ViewGroup/android.view.ViewGroup[@content-desc='Photo' or @content-desc='Video']")
                            {
                                List<string> photos = _client.FindBounds("", "//android.view.ViewGroup/android.view.ViewGroup[@content-desc='Photo' or @content-desc='Video']/parent::*[@selected='false']", 1);

                                if (photos.Count > 1)
                                    photos = photos.GetRange(0, photos.Count - 1);

                                if (photos.Count > 0)
                                {
                                    string selectedPhoto = photos.OrderBy(x => Guid.NewGuid()).First();
                                    var point = new RectangleArea(selectedPhoto).GetCenterPoint();
                                    _client.Click(point.X, point.Y);
                                    break;
                                }
                            }
                            else if (!string.IsNullOrEmpty(elementXPath))
                            {
                                SetStatusAccount(accountIndex, statusPrefix + "Tap " + elementXPath + "...");
                                _client.ElementWithAttributes(elementXPath, 1, pageSrc);
                            }

                            _client.Delay(1);
                        }

                        _client.Shell("input keyevent 4");
                    }
                }

                SetStatusAccount(accountIndex, statusPrefix + "Tap Send...");
                pageSource = "";
                string sendButtonXPath = _client.FindElement("", new List<string> { "//*[@content-desc=\"Send\"]", "//*[@text='Post']" }, 5);
                if (_client.ElementWithAttributes(sendButtonXPath, 5, pageSource))
                {
                    SetStatusAccount(accountIndex, statusPrefix + "Tap Send, " + "đợi" + " {time}s...", SubdyHelper.RandomValue(3, 6));
                    _client.Delay(120);
                    _client.ElementWithAttributes("//androidx.recyclerview.widget.RecyclerView/child::*[1]//*[@content-desc='Just now']", 10);
                }

                imageCommentCount++;
                break;
            }

            if (needsBack)
            {
                SetStatusAccount(accountIndex, statusPrefix + "Back...");
                if (attempt == 1)
                    _client.Shell("input keyevent 4");
                else
                    _client.Shell("input keyevent 4");
                _client.Delay(3);

                _client.Delay(3);
            }
            else
            {
                pageSource = _client.GetXMLSource();
                if (_client.ElementWithAttributes("//android.widget.EditText[@text='Write a comment…']", 1, pageSource, false) &&
                    _client.FindBounds(pageSource, new List<string>
                    {
                "//*[@content-desc=\"Comment Button\"]",
                "//*[@content-desc=\"Answer Button\"]",
                "//*[@text='Answer']",
                "//*[@text=\"Comment\"]",
                "//*[@content-desc=\"Comment\"]"
                    }, 1).Count == 0)
                {
                    ScrollScreen(-1);
                }
            }
        }
        private void TapShare(bool someFlag, int accountIndex, string statusPrefix, bool dummyFlag, string extraData, ref int shareAttemptCount, bool anotherFlag, ref int counter, List<string> commentList, ref List<string> usedComments, bool enableShare, ref int shareSuccessCount, int mode = 0, bool dummyOption = false, string extraString = "")
        {
            //method_76
            if (!enableShare)
                return;

            // Xác định XPath của các nút Share
            List<string> shareXPaths = new List<string>
    {
        "//*[@content-desc=\"Share Button\"]",
        "//*[@text=\"Share\"]"
    };

            // Nếu đang ở Shorts (com.facebook.fbshorts) thì thêm XPath riêng
            string currentActivity = GetCurrentActivity();
            if (currentActivity.Contains("com.facebook.fbshorts"))
            {
                shareXPaths.Add("//*[@content-desc=\"Share\"]");
            }

            SetStatusAccount(accountIndex, statusPrefix + "Find Share...");
            string pageSource = _client.GetXMLSource();
            List<string> shareButtons = _client.FindBounds(pageSource, shareXPaths, 1);
            SetStatusAccount(accountIndex, statusPrefix + "Find Share: " + shareButtons.Count);

            if (shareButtons.Count <= 0 || (mode != 1 && SubdyHelper.RandomValue(1, 100) % 3 != 0))
                return;

            // Chạm vào nút Share đầu tiên
            Point point = new RectangleArea(shareButtons.First()).GetCenterPoint();
            SetStatusAccount(accountIndex, statusPrefix + "Tap Share...");
            if (!_client.Click(point.X, point.Y))
                return;

            _client.Delay(2);
            SetStatusAccount(accountIndex, statusPrefix + "Find Share Now...");

            string shareNowXPath = "";
            if (_client.ElementWithAttributes("//*[@content-desc=\"Share Now\"]", 5, "", false))
            {
                // Nếu chưa chọn đối tượng chia sẻ (Public)
                if (!_client.ElementWithAttributes("//*[@content-desc='Public']", 1, "", false))
                {
                    _client.ElementWithAttributes("//*[@content-desc='Write Post']/preceding-sibling::*[1]");

                    if (_client.ElementWithAttributes("//*[@text='Public' or starts-with(@content-desc, 'Public')]", 10, ""))
                    {
                        _client.ElementWithAttributes("//android.widget.CheckBox[@text='Set as default audience.'][@checked='false']");
                        _client.ElementWithAttributes("//*[@content-desc='Done' or @text='Done' or @text='CHANGE' or @text='SAVE']");
                    }
                    else
                    {
                        _client.Shell("input keyevent 4");
                    }

                    shareNowXPath = "";
                    _client.ElementWithAttributes("//*[@content-desc=\"Share Now\"]", 1, "", false);
                }

                // Chạm Share Now
                if (_client.ElementWithAttributes("//*[@content-desc=\"Share Now\"]"))
                {
                    shareSuccessCount++;
                    SetStatusAccount(accountIndex, statusPrefix + "Tap Share Now, " + "đợi" + " {time}s...", SubdyHelper.RandomValue(1, 3));
                }
            }
            else
            {
                SetStatusAccount(accountIndex, statusPrefix + "Back...");
                _client.Shell("input keyevent 4");
                _client.Delay(2);
            }
        }

        public int InteractWithPost(int accountId, string accountName, bool enableLike, string reactionPattern, ref int likeCount, bool enableComment, ref int commentCount, List<string> commentPool, ref List<string> randomizedComments, bool enableShare, ref int shareCount)
        {
            //method_73

            new List<string>();
            List<string> list = new List<string>();
            List<string> list2 = new List<string>();
            string text = "";
            if (enableLike)
            {
                SetStatusAccount(accountId, accountName + "Find Like...");
                string text2 = _client.FindBounds("", "//*[contains(@content-desc, \"Like\")]", 1).FirstOrDefault();
                if (!string.IsNullOrEmpty(text2))
                {
                    var coords2 = text2.Split(new string[3] { "[", ",", "]" }, StringSplitOptions.RemoveEmptyEntries);
                    if (coords2.Length >= 4)
                    {
                        Point point = new RectangleArea(text2).GetCenterPoint();
                        Point point2 = new RectangleArea("[35," + coords2[1] + "][65," + coords2[3] + "]").GetCenterPoint();
                        SetStatusAccount(accountId, accountName + "Tap Reaction...");
                        _client.Swipe(point.X, point.Y, point2.X, point2.Y);
                        _client.Delay(1);
                        string d80AC = "";
                        if (!string.IsNullOrEmpty(reactionPattern))
                        {
                            char rc = reactionPattern[SubdyHelper.RandomValue(0, reactionPattern.Length - 1)];
                            int rv;
                            if (int.TryParse(rc.ToString(), out rv)) d80AC = (rv + 1).ToString();
                        }
                        ReactToPost(d80AC);
                        _client.Delay(1);
                        _client.Swipe(point2.X, point2.Y, point.X, point.Y);
                        _client.Delay(1);
                        likeCount++;
                    }
                }
            }
            if (enableComment)
            {
                SetStatusAccount(accountId, accountName + "Find Comment...");
                string pageSrc = _client.GetXMLSource();
                list = _client.FindBounds(pageSrc, "//*[@text=\"Write a comment…\"]", 5);
                if (list.Count > 0)
                {
                    Point point3 = ParseCoordinate(list.Last());
                    if (randomizedComments.Count == 0)
                    {
                        randomizedComments = SubdyHelper.CloneList(commentPool);
                    }
                    text = randomizedComments[SubdyHelper.RandomValue(0, randomizedComments.Count - 1)];
                    randomizedComments.Remove(text);
                    text = SubdyHelper.SpinText(text);
                    SetStatusAccount(accountId, accountName + "Tap Comment...");
                    if (_client.Click(point3.X, point3.Y))
                    {
                        _client.Delay(2);
                        SetStatusAccount(accountId, accountName + "Find EditText...");
                        if (_client.ElementWithAttributes("//android.widget.EditText", 5, click: false))
                        {
                            SetStatusAccount(accountId, accountName + "Nhập dữ liệu...");
                            _client.SendTextSlow("//android.widget.EditText", text);
                            _client.Delay(2);
                            SetStatusAccount(accountId, accountName + "Tap Send...");
                            if (_client.ElementWithAttributes("//*[@content-desc=\"Send\"]"))
                            {
                                SetStatusAccount(accountId, accountName + "Tap Send, " + "đợi" + " {time}s...", SubdyHelper.RandomValue(3, 6));
                            }
                            commentCount++;
                        }
                        else
                        {
                            SetStatusAccount(accountId, accountName + "Back...");
                            _client.Shell("input keyevent 4");
                            _client.Delay(2);
                        }
                    }
                }
            }
            if (enableShare)
            {
                SetStatusAccount(accountId, accountName + "Find Share...");
                string a00A61A2 = _client.GetXMLSource();
                list2 = _client.FindBounds(a00A61A2, "//*[@content-desc=\"SHARE\"]", 1);
                if (list2.Count > 0)
                {
                    Point point4 = ParseCoordinate(list2.Last());
                    SetStatusAccount(accountId, accountName + "Tap Share...");
                    if (_client.Click(point4.X, point4.Y))
                    {
                        _client.Delay(2);
                        SetStatusAccount(accountId, accountName + "Find Post...");
                        if (_client.ElementWithAttributes("//android.widget.ImageButton[@content-desc=\"Write Post\"]") && _client.ElementWithAttributes("//android.widget.Button[@text =\"POST\"]"))
                        {
                            SetStatusAccount(accountId, accountName + "Tap Post, " + "đợi" + " {time}s...", SubdyHelper.RandomValue(1, 3));
                            shareCount++;
                        }
                        else
                        {
                            SetStatusAccount(accountId, accountName + "Back...");
                            _client.Shell("input keyevent 4");
                        }
                    }
                }
            }
            return 0;
        }
        private string GetCurrentActivity()
        {
            //method_15
            string result = "";
            try
            {
                string output = _client.Shell("dumpsys activity activities | findstr mResumedActivity");
                if (!string.IsNullOrEmpty(output))
                {
                    int iu0 = output.IndexOf("u0 ");
                    if (iu0 >= 0)
                    {
                        int startIndex = iu0 + 3;
                        if (startIndex <= output.Length)
                        {
                            int endIndex = output.IndexOf("}", startIndex);
                            if (endIndex > startIndex)
                            {
                                result = output.Substring(startIndex, endIndex - startIndex).Trim();
                            }
                        }
                    }
                }
            }
            catch
            {
            }
            return result;
        }
        public bool ReactToPost(string allowedReactions = "1|2|4|5")
        {
            //C628163C
            SetStatusAccount(1, "Reaction...");

            // Danh sách các reaction (theo index)
            List<string> reactions = new List<string>
    {
        "Like", "Love", "Care", "Haha", "Wow", "Sad", "Angry"
    };

            string chosenReaction = "";

            // Nếu input rỗng thì mặc định dùng "1|2|4|5" (Like, Love, Haha, Wow)
            if (string.IsNullOrEmpty(allowedReactions))
            {
                allowedReactions = "1|2|4|5";
            }

            // Tách danh sách reaction cho phép
            List<string> allowedList = allowedReactions.Split('|').ToList();

            if (allowedList.Count > 0)
            {
                // Chọn ngẫu nhiên 1 reaction trong danh sách cho phép
                string pickedIndex = SubdyHelper.GetStringRandom(allowedList); // smethod_8 -> PickRandom
                if (int.TryParse((pickedIndex ?? "").Trim(), out int idx))
                {
                    int zeroBased = idx - 1;
                    if (zeroBased >= 0 && zeroBased < reactions.Count)
                    {
                        chosenReaction = reactions[zeroBased];
                    }
                }
            }

            // Nếu không chọn được thì chọn random toàn bộ danh sách
            if (string.IsNullOrEmpty(chosenReaction))
            {
                chosenReaction = SubdyHelper.GetStringRandom(reactions);
            }

            // XPath để tìm nút reaction
            string xpath = $"//*[contains(@content-desc, \"{chosenReaction}\")]";

            // Click reaction
            return _client.ElementWithAttributes(xpath, 5);
        }
        public bool OpenFacebookLink(int accountId, string statusPrefix, string link)
        {
            //FA905AA0
            while (true)
            {
                // Mở link
                DeplinkFacebook(link);
                _client.Delay(2);

                // Nếu là Page
                if (link.StartsWith("fb://page/") && !link.Contains("invite_friends_to_like_page"))
                {
                    _client.Delay(3);

                    for (int i = 0; i < 5; i++)
                    {
                        string pageSource = "";
                        string matchedXpath = _client.FindElement("", new List<string>{
"//android.view.ViewGroup/android.widget.FrameLayout/android.view.View",
                        "//*[@content-desc='Create Action Button']",
                        "//*[@content-desc='Follow']",
                        "(//android.widget.FrameLayout/android.view.ViewGroup/android.view.ViewGroup/android.widget.LinearLayout/android.view.ViewGroup/android.view.ViewGroup)[last()]/child::*",
                        "//*[@content-desc='page cover photo']"
                            }, 5);

                        if (matchedXpath == "//android.view.ViewGroup/android.widget.FrameLayout/android.view.View")
                        {
                            string bounds = _client.FindBounds(pageSource, "//android.view.ViewGroup/android.widget.FrameLayout/android.view.View").FirstOrDefault();
                            if (new RectangleArea(bounds).Top >= 2000)
                            {
                                break;
                            }

                            RectangleArea rect = new RectangleArea(bounds);
                            RectangleArea target = new RectangleArea(rect.Left, 2000, rect.Right, 2000);
                            _client.Swipe(rect.GetCenterPoint().X, rect.GetCenterPoint().Y, target.GetCenterPoint().X, target.GetCenterPoint().Y);
                            continue;
                        }

                        if (!string.IsNullOrEmpty(matchedXpath))
                        {
                            break;
                        }

                        // Nếu giao diện hiện Back + Search
                        List<string> texts = ExtractTexts(pageSource);
                        if (texts.Count != 2 && !texts.Contains("back") && !texts.Contains("search"))
                        {
                            if (ScrollScreen())
                            {
                                break;
                            }
                            continue;
                        }
                        goto IL_015a;
                    }
                }

                // Nếu là Stories
                if (link.Contains("/stories/") && _client.ElementWithAttributes("(//*[@text='Stories'])[2]", 150, click: false))
                {
                    break;
                }
                if (link.Contains("profile_edit") && _client.ElementWithAttributes("//*[@text=\"Edit Profile\"]", 10, click: false))
                {
                    break;
                }
                // Kiểm tra trạng thái đăng nhập
                //switch (CheckLoginStatus(fbController, accountId, statusPrefix))
                //{
                //    case 1: // đang login
                //        break;
                //    case 0: // login ok
                //        return true;
                //    default: // lỗi login
                //        return false;
                //}
            IL_015a:
                _client.Shell("input keyevent 4");
                _client.Delay(2);
            }

            return true;
        }
        public List<string> ExtractTexts(string pageSource = "", int mode = 0)
        {
            //method_104
            if (string.IsNullOrEmpty(pageSource))
            {
                pageSource = _client.GetXMLSource().ToLower();
            }

            // Các pattern regex
            var textDoubleQuote = ExtractMatches(pageSource, "text=\"(.*?)\"");
            var contentDoubleQuote = ExtractMatches(pageSource, "content-desc=\"(.*?)\"");
            var textSingleQuote = ExtractMatches(pageSource, "text='(.*?)'");
            var contentSingleQuote = ExtractMatches(pageSource, "content-desc='(.*?)'");

            List<string> results = new List<string>();

            switch (mode)
            {
                case 0: // Lấy tất cả
                    results.AddRange(textDoubleQuote);
                    results.AddRange(contentDoubleQuote);
                    results.AddRange(textSingleQuote);
                    results.AddRange(contentSingleQuote);
                    break;

                case 1: // Chỉ lấy text
                    results.AddRange(textDoubleQuote);
                    results.AddRange(textSingleQuote);
                    break;

                case 2: // Chỉ lấy content-desc
                    results.AddRange(contentDoubleQuote);
                    results.AddRange(contentSingleQuote);
                    break;
            }

            return results;
        }
        private List<string> ExtractMatches(string input, string pattern)
        {
            List<string> results = new List<string>();
            try
            {
                MatchCollection matches = Regex.Matches(input, pattern);
                foreach (Match match in matches)
                {
                    string value = match.Groups[1].Value;
                    if (!string.IsNullOrEmpty(value))
                    {
                        results.Add(value);
                    }
                }
            }
            catch (Exception)
            {
                // Có thể log lỗi ở đây nếu cần thiết
            }
            return results;
        }
        internal Dictionary<string, List<string>> dictionary_3 = null;
        public string DeplinkFacebook(string url)
        {
            //method_69
            return _client.Shell("am start -n " + FacebookHander.Package(PlatformModel.Facebook) + "/.IntentUriHandler \"" + url + "\"");
        }
        // Phát hiện ĐANG ở Timeline/Home của Facebook. Tab "Home" luôn hiện trên
        // Timeline; các màn dựng story / soạn bài là immersive fullscreen nên KHÔNG
        // có tab bar này. Dùng để bỏ qua OpenFacebookTimeline (deeplink + reload) khi
        // không cần thiết. Trả về false nếu không chắc chắn (an toàn → sẽ mở lại).
        private bool IsOnFacebookTimeline()
        {
            try
            {
                if (!(_client?.IsRunningApp(PlatformModel.Facebook) ?? false))
                {
                    return false;
                }
                string homeTab = _client.FindElement("", new List<string>
                {
                    "//*[@content-desc='Home, tab']",
                    "//*[@content-desc='Home, Tab']",
                }, 1);
                return !string.IsNullOrEmpty(homeTab);
            }
            catch
            {
                return false;
            }
        }

        // Chờ UI "ổn định" thay vì ngủ cứng: poll XML nguồn, TRẢ VỀ NGAY khi hai lần
        // dump liên tiếp giống nhau (màn hình đã ngừng tải). maxSeconds là fallback an
        // toàn nếu UI cứ thay đổi. Tôn trọng nút DỪNG qua _client.Delay (ThrowIfStopped).
        private void WaitForUiStable(int maxSeconds = 3, int settleSeconds = 1)
        {
            try
            {
                int tickCount = Environment.TickCount;
                string previous = _client.GetXMLSource();
                while (Environment.TickCount - tickCount < maxSeconds * 1000)
                {
                    _client.Delay(settleSeconds);
                    string current = _client.GetXMLSource();
                    if (!string.IsNullOrEmpty(current) && current == previous)
                    {
                        return;
                    }
                    previous = current;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
            }
        }

        public void OpenFacebookTimeline()
        {
            //D7950A0F
            _mainService.SetStatus("Open Timeline...", 2);
            DeplinkFacebook("fb://dbl_login_activity");
            string text = "";
            for (int i = 0; i < 5; i++)
            {
                text = "";
                string text2 = _client.FindElement("", new List<string> { "//*[@content-desc=\"POST\"]", "//*[@text=\"Create post\"]", "//*[contains(@content-desc, \"Home, tab\")]", "//*[contains(@content-desc, \"Home, tab\")]" }, 3);
                if (!(text2 == ""))
                {
                    if (text2 == "//*[@content-desc=\"POST\"]" || text2 == "//*[@text=\"Create post\"]")
                    {
                        _client.ElementWithAttributes("//*[@content-desc=\"Back\"]", 5, text);
                    }
                    else
                    {
                        _client.ElementWithAttributes(text2, 5, text);
                    }
                    continue;
                }
                break;
            }
        }
        public bool PerformSwipe(string startCoordinates, string endCoordinates, int duration = 500, int direction = 1, int repeatCount = 1)
        {//method_53
            string beforeSource = _client.GetXMLSource();

            for (int i = 0; i < repeatCount; i++)
            {
                if (direction == 1)
                {
                    ExecuteSwipe(startCoordinates, endCoordinates, duration);
                }
                else
                {
                    ExecuteSwipe(endCoordinates, startCoordinates, duration);
                }
            }



            return _client.GetXMLSource() == beforeSource;
        }
        public System.Drawing.Point ParseCoordinate(string rectString)
        {
            try
            {
                // Tách chuỗi thành 2 phần: [x1,y1] và [x2,y2]
                string[] pairs = rectString.Split(new[] { "][" }, StringSplitOptions.RemoveEmptyEntries);

                if (pairs.Length != 2)
                    throw new FormatException($"Invalid rectangle format: {rectString}");

                // Bỏ [ ] rồi split tiếp
                string[] p1 = pairs[0].Replace("[", "").Replace("]", "").Split(',');
                string[] p2 = pairs[1].Replace("[", "").Replace("]", "").Split(',');

                if (p1.Length != 2 || p2.Length != 2)
                    throw new FormatException($"Invalid coordinate pairs: {rectString}");

                int x1 = Convert.ToInt32(p1[0]);
                int y1 = Convert.ToInt32(p1[1]);
                int x2 = Convert.ToInt32(p2[0]);
                int y2 = Convert.ToInt32(p2[1]);

                // Đảm bảo min/max
                int minX = Math.Min(x1, x2);
                int maxX = Math.Max(x1, x2);
                int minY = Math.Min(y1, y2);
                int maxY = Math.Max(y1, y2);

                // Random trong rect
                int x = SubdyHelper.RandomValue(minX, maxX + 1);
                int y = SubdyHelper.RandomValue(minY, maxY + 1);

                return new System.Drawing.Point(x, y);
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                return default(System.Drawing.Point); // (0,0)
            }
        }
        public bool ExecuteSwipe(string startCoordinate, string endCoordinate, int duration = 500)
        {
            Point startPoint = ParseCoordinate(startCoordinate);  // method_51
            Point endPoint = ParseCoordinate(endCoordinate);

            return _client.Swipe(startPoint.X, startPoint.Y, endPoint.X, endPoint.Y, duration); // method_50
        }
        public bool ScrollScreen(int direction = 1, int repeatCount = 1, int speed = 0)
        {//CE16082B  FB3ACF2E
            _mainService.SetStatus("Đang lướt tìm nút đăng bài...", 2);
            var screen = _client.GetScreenResolution();
            int screenHeight = screen.Y;  // method_13
            int screenWidth = screen.X;    // method_14

            // Tọa độ mặc định (scroll từ dưới lên trên)
            string endPoint = $"[{screenWidth / 4},{screenHeight / 4}][{screenWidth / 4 * 3},{screenHeight / 4 + 50}]";
            string startPoint = $"[{screenWidth / 4},{screenHeight / 4 * 3}][{screenWidth / 4 * 3},{screenHeight / 4 * 3 + 50}]";

            // Nếu direction = 2 → cuộn kiểu khác (giữa màn hình)
            if (direction == 2)
            {
                startPoint = $"[{screenWidth / 4},{screenHeight / 2}][{screenWidth / 2},{screenHeight / 2 + 50}]";
                direction = 1;
            }

            // Nếu chưa có speed → random
            if (speed == 0)
            {
                speed = SubdyHelper.RandomValue(2000000, 2560000) / screenHeight; // F639F68B
            }

            return PerformSwipe(startPoint, endPoint, speed, direction, repeatCount); // method_53
        }
        // Composer story bản FB mới (InspirationComposerActivity, đo live 21-09-2026 trên
        // 5200ef68feda15cf): sheet "Privacy / Save / Add AI Label" vẽ nhãn lên canvas React-Native
        // nên MỌI node trong sheet đều text="" content-desc="" (ViewGroup NAF="true") -> xpath
        // 'Privacy'/'Public' KHÔNG BAO GIỜ khớp, tool kẹt vòng lặp mở/đóng sheet tới timeout 300s.
        // Nhận diện BẰNG HÌNH HỌC: sheet = ViewGroup clickable desc='Close' (handle kéo) + con
        // LinearLayout; các ROW là ViewGroup clickable='true' NAF='true' full-width, thứ tự cố định
        // row1=Privacy, row2=Save, row3=Add AI Label (kèm Switch). Trả bounds của row thứ rowIndex.
        private string FindStorySheetRowBounds(string xmlSource, int rowIndex)
        {
            if (string.IsNullOrEmpty(xmlSource)) return null;
            try
            {
                var nodes = _client.GetAttributeValuesFromXmlNodes(xmlSource,
                    "//android.view.ViewGroup[@clickable='true' and @NAF='true']", "bounds");
                var rows = new List<string>();
                foreach (var b in nodes)
                {
                    if (string.IsNullOrEmpty(b)) continue;
                    var r = new RectangleArea(b);
                    // ROW sheet: full-width (trái ~0, phải ~màn hình) và cao ~150-300px.
                    // Loại node nền composer (nút tròn cạnh phải, khay sticker…) không full-width.
                    if (r.Left <= 5 && r.Right - r.Left >= 1000 && r.Bottom - r.Top >= 120 && r.Bottom - r.Top <= 400)
                        rows.Add(b);
                }
                rows.Sort((a, b2) => new RectangleArea(a).Top.CompareTo(new RectangleArea(b2).Top));
                return rowIndex < rows.Count ? rows[rowIndex] : null;
            }
            catch (Exception ex)
            {
                RunHistoryLog.Step(_client.Device?.Serial ?? "?", $"[StorySheet] FindStorySheetRowBounds lỗi: {ex.Message}");
                return null;
            }
        }

        // Mở sheet bằng bánh răng (desc='Story Settings Menu') rồi bấm row Privacy THEO HÌNH HỌC
        // (row 0) — xem ghi chú FindStorySheetRowBounds. Màn "Story privacy" mở ra CÓ label đầy đủ
        // (RadioButton 'Public'/'Friends'/'Custom') nên phần chọn Public + SAVE tái dụng luồng cũ.
        // Trả TRUE nếu đã bấm được row Privacy (màn privacy mở ra hoặc đã đổi cảnh).
        private bool OpenStoryPrivacySheetByGeometry()
        {
            try
            {
                if (!_client.ElementWithAttributes("//*[@content-desc='Story Settings Menu']", 5, ""))
                    return false;
                _client.Delay(2);
                string xmlSheet = _client.GetXMLSource();
                string rowBounds = FindStorySheetRowBounds(xmlSheet, 0);
                if (rowBounds == null)
                {
                    RunHistoryLog.Step(_client.Device?.Serial ?? "?", "[StorySheet] KHÔNG thấy row Privacy trong sheet (dump không có ViewGroup NAF full-width)");
                    return false;
                }
                var pt = new RectangleArea(rowBounds).GetCenterPoint();
                _mainService.SetStatus($"Tap row Privacy theo hình học {rowBounds}...", 2);
                _client.Click(pt.X, pt.Y);
                _client.Delay(2);
                return true;
            }
            catch (Exception ex)
            {
                RunHistoryLog.Step(_client.Device?.Serial ?? "?", $"[StorySheet] OpenStoryPrivacySheetByGeometry lỗi: {ex.Message}");
                return false;
            }
        }

        public bool WaitForPostComplete(int E40D7F04)
        {
            try
            {
                //method_30
                int tickCount = Environment.TickCount;
                while (true)
                {
                    if (ContainsAnyKeyword("", "android.widget.ProgressBar", "Row showing that your post is", "Sharing", "Uploading", "Finishing up", "Updating", "Posting"))
                    {
                        if (Environment.TickCount - tickCount < E40D7F04 * 1000)
                        {
                            _client.Delay(2);
                            continue;
                        }
                        break;
                    }
                    return true;
                }
            }
            catch (Exception exception_)
            {

            }
            return false;
        }
        public bool ContainsAnyKeyword(string pageSource, params string[] keywords)
        {
            //ED9CDB24
            if (pageSource == "")
            {
                pageSource = _client.GetXMLSource();
            }

            pageSource = pageSource.ToLower();
            int i = 0;

            while (true)
            {
                if (i < keywords.Length)
                {
                    if (pageSource.Contains(keywords[i].ToLower()))
                    {
                        break; // tìm thấy keyword
                    }
                    i++;
                    continue;
                }
                return false; // duyệt hết mà không thấy
            }
            return true;
        }

        private void UpdateStoryStats(int doneCount, int failCount)
        {
            try
            {
                if (_account == null)
                {
                    return;
                }

                _account.Note = $"Story: Done: {doneCount} - fail: {failCount}";
                new AccountContext().Update(_account);
            }
            catch
            {
            }
        }

        private bool ClickRandomAlbumArtStyle(int timeoutSeconds = 10)
        {
            string albumArtXPath = "//*[@class='android.widget.Button' and contains(@content-desc,'Album Art') and (@index='2' or @index='3' or @index='4' or @index='5')]";
            var albumArtBounds = _client.FindBounds("", albumArtXPath, timeoutSeconds);
            if (!albumArtBounds.Any())
            {
                return false;
            }

            var point = new RectangleArea(albumArtBounds.OrderBy(_ => Guid.NewGuid()).First()).GetCenterPoint();
            _client.Click(point.X, point.Y);
            _client.Delay(1);
            return true;
        }

        private bool MoveMusicStickerRandom(int timeoutSeconds = 10)
        {
            string musicStickerXPath = "//*[contains(@content-desc,'Music sticker') and not(contains(@content-desc,'preview'))]";
            var sticker = GetSmallestBounds(musicStickerXPath, timeoutSeconds);
            if (sticker == null)
            {
                return false;
            }

            var startPoint = sticker.GetCenterPoint();
            var screen = _client.GetScreenResolution();
            int stickerWidth = Math.Max(1, sticker.Right - sticker.Left);
            int stickerHeight = Math.Max(1, sticker.Bottom - sticker.Top);
            int minX = Math.Max(screen.X * 15 / 100, stickerWidth / 2 + 20);
            int maxX = Math.Max(minX + 1, Math.Min(screen.X * 85 / 100, screen.X - stickerWidth / 2 - 20));
            int minY = Math.Max(screen.Y * 25 / 100, stickerHeight / 2 + 20);
            int maxY = Math.Max(minY + 1, Math.Min(screen.Y * 70 / 100, screen.Y - stickerHeight / 2 - 20));
            int targetX = SubdyHelper.RandomValue(minX, maxX + 1);
            int targetY = SubdyHelper.RandomValue(minY, maxY + 1);

            //if (TryDragMusicSticker(startPoint.X, startPoint.Y, targetX, targetY)
            //    && IsMusicStickerMoved(musicStickerXPath, startPoint.X, startPoint.Y))
            //{
            //    return true;
            //}

            _client.Shell("input", "swipe", startPoint.X.ToString(), startPoint.Y.ToString(), targetX.ToString(), targetY.ToString(), "1800");
            _client.Delay(2);
            return IsMusicStickerMoved(musicStickerXPath, startPoint.X, startPoint.Y);
        }

        private RectangleArea? GetSmallestBounds(string xpath, int timeoutSeconds = 1)
        {
            var bounds = _client.FindBounds("", xpath, timeoutSeconds);
            if (!bounds.Any())
            {
                return null;
            }

            return bounds
                .Select(x => new RectangleArea(x))
                .Where(x => x.Right > x.Left && x.Bottom > x.Top)
                .OrderBy(x => (x.Right - x.Left) * (x.Bottom - x.Top))
                .FirstOrDefault();
        }

        private bool TryDragMusicSticker(int startX, int startY, int targetX, int targetY)
        {
            try
            {
                if (_client.ATX.Drag(startX, startY, targetX, targetY, 8))
                {
                    _client.Delay(2);
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        private bool IsMusicStickerMoved(string musicStickerXPath, int oldX, int oldY)
        {
            var sticker = GetSmallestBounds(musicStickerXPath);
            if (sticker == null)
            {
                return false;
            }

            var newPoint = sticker.GetCenterPoint();
            return Math.Abs(newPoint.X - oldX) > 25 || Math.Abs(newPoint.Y - oldY) > 25;
        }

        internal List<string> UploadMediaFiles(List<string> files)
        {
            List<string> remoteFiles = new List<string>();
            _mainService.SetStatus("Uploading media files...", 2);

            // Chỉ bảo đảm thư mục ĐÍCH (/sdcard/pictures) một lần cho mỗi lần chạy.
            // Upload luôn đẩy vào /sdcard/pictures nên dcim/camera và movies là mkdir thừa.
            if (!_remoteUploadFolderEnsured)
            {
                EnsureRemoteFolder("sdcard/pictures");
                _remoteUploadFolderEnsured = true;
            }

            foreach (string localFile in files)
            {
                string extension = Path.GetExtension(localFile).TrimStart('.').ToLower();
                string randomName = SubdyHelper.RandomString(length: 10).TrimEnd('.') + "." + extension;
                string remotePath = $"/sdcard/pictures/{randomName}";

                string mimeType = extension switch
                {
                    "jpg" or "jpeg" => "image/jpeg",
                    "png" => "image/png",
                    "mp4" => "video/mp4",
                    "mov" => "video/quicktime",
                    "gif" => "image/gif",
                    _ => "image/jpeg"
                };

                _client.Push(localFile, remotePath);

                // Đồng bộ MediaStore — không cần mở app
                _client.Shell($"am broadcast -a android.intent.action.MEDIA_SCANNER_SCAN_FILE " +
                              $"-d \"file://{remotePath}\"");
                _client.Shell($"content insert --uri content://media/external/images/media " +
                              $"--bind _data:s:{remotePath} " +
                              $"--bind mime_type:s:{mimeType} " +
                              $"--bind title:s:{Path.GetFileNameWithoutExtension(localFile)}");

                remoteFiles.Add(remotePath);
            }
            return remoteFiles;
        }
        internal void DeleteMediaFiles(List<string> remoteFiles)
        {
            _mainService.SetStatus("Deleting media files...", 2);

            foreach (string remoteFile in remoteFiles)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(remoteFile))
                        continue;

                    // Đảm bảo đường dẫn hợp lệ
                    string safePath = remoteFile.Trim();

                    // Xóa file
                    _client.Shell($"rm -f \"{safePath}\"");

                    // Gửi broadcast để cập nhật lại media database (ẩn file khỏi gallery)
                    _client.Shell($"am broadcast -a android.intent.action.MEDIA_SCANNER_SCAN_FILE -d \"file://{safePath}\"");
                }
                catch (Exception ex)
                {
                    _mainService.SetStatus($"Failed to delete {remoteFile}: {ex.Message}", 3);
                }
            }
        }

        private void EnsureRemoteFolder(string folderPath)
        {
            _client.Shell($"mkdir -p /{folderPath}");
        }

        internal object object_3 = new object();
        private bool UploadMedia(int accountId, string prefixStatus, ADBClient uploader, string sourcePath, int fileCount = 1, bool processEachFile = false)
        {
            try
            {
                //method_52
                bool result = false;
                SetStatusAccount(accountId, $"{prefixStatus} Upload media...");

                if (processEachFile)
                {
                    lock (object_3)
                    {
                        List<string> list = (from string_08 in Directory.GetFiles(sourcePath)
                                             where !string_08.EndsWith(".txt")
                                             select string_08).ToList();
                        if (list.Count > 0)
                        {
                            List<string> list2 = new List<string>();
                            string text = "";
                            for (int i = 0; i < fileCount; i++)
                            {
                                if (list.Count == 0)
                                {
                                    break;
                                }
                                text = list.OrderBy((string C8929B25) => Guid.NewGuid()).FirstOrDefault();
                                list.Remove(text);
                                list2.Add(text);
                            }
                            UploadMediaFiles(list2);
                            Thread.Sleep(2000);
                            for (int j = 0; j < list2.Count; j++)
                            {
                                SubdyHelper.DeleteFile(list2[j]);
                            }
                            result = true;
                        }
                    }
                }
                else
                {
                    List<string> list3 = new List<string>();
                    if (sourcePath.EndsWith(".mp4"))
                    {
                        list3.Add(sourcePath);
                    }
                    else
                    {
                        list3 = (from string_055 in Directory.GetFiles(sourcePath)
                                 where !string_055.EndsWith(".txt")
                                 select string_055).ToList();
                    }
                    if (list3.Count > 0)
                    {
                        List<string> list4 = new List<string>();
                        string text2 = "";
                        for (int k = 0; k < fileCount; k++)
                        {
                            if (list3.Count == 0)
                            {
                                break;
                            }
                            text2 = list3.OrderBy((string string_0) => Guid.NewGuid()).FirstOrDefault();
                            list3.Remove(text2);
                            list4.Add(text2);
                        }
                        UploadMediaFiles(list4);
                        Thread.Sleep(2000);
                        result = true;
                    }
                }
                SetStatusAccount(accountId, $"{prefixStatus} Upload media done!");
                return true;
            }
            catch (Exception ex)
            {
                SetStatusAccount(accountId, $"{prefixStatus} Upload media failed: {ex.Message}");
                return false;
            }
        }
    }
}
