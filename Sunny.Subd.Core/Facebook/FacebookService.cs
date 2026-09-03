using AutoAndroid;
using Sunny.Subd.Core.Models;
using Sunny.Subd.Core.Services;
using Sunny.Subd.Core.Utils;
using Sunny.Subdy.Common.API.Captchas;
using Sunny.Subdy.Common.API.Mail;
using Sunny.Subdy.Common.Models;
using Sunny.Subdy.Data.Models;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;
using static Sunny.Subd.Core.Utils.XpathHelper;

namespace Sunny.Subd.Core.Facebook
{
    public class FacebookService : IFacebookService
    {
        private Stopwatch Stopwatch = new Stopwatch();
        private CancellationToken _ct;
        private ADBClient _client;
        private Account _account; private string _sate = string.Empty;
        private MainService _main;
        private void CheckStop(int second)
        {
            if (Stopwatch.ElapsedMilliseconds > second * 1000)
            {
                throw new SubdyExtension(SubdyEnum.Stop, "Đã quá thời gian thực hiện thao tác đăng nhập.");
            }
            if (_ct.IsCancellationRequested)
            {
                throw new SubdyExtension(SubdyEnum.Stop, "Bạn đã dừng thực hiện việc thao tác.");
            }
        }

        private void SetStatus(string status, int color, string logDetail = null)
        {
            if (!string.IsNullOrEmpty(_sate))
            {
                status = $"[{_sate}] - ({status})";
            }
            if (_account != null)
            {
                _account.Status = status;
                _account.RecentInteraction = DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy");
                _account.ColorType = color;
            }
            if (_client?.Device != null)
            {
                _client.Device.Status = status;
                _client.Device.TypeColor = color;
            }
            if (!string.IsNullOrEmpty(logDetail) && _client != null)
            {
                _client.LogHelper.SUCCESS(logDetail);
            }
        }
        public async Task<SubdyExtension> Login(ADBClient client, Account account, CancellationToken ct, int timeout, MainService main)
        {
            SubdyEnum subyEnum = SubdyEnum.None;
            string message = "Lỗi trong quá trình đăng nhập tài khoản!";
            try
            {
                _sate = "Đăng nhập Facebook";
                _client = client ?? throw new ArgumentNullException(nameof(client), "ADBClient cannot be null");
                _account = account ?? throw new ArgumentNullException(nameof(account), "Account cannot be null");
                _ct = ct;
                _main = main;
                Stopwatch.Restart();
                string _case = string.Empty;
                while (true)
                {
                    CheckStop(timeout);
                    SetStatus("Đang tìm cửa sổ đăng nhập...", 2);
                    _case = client.FindElement("", FacebookHander.GetActiAccountFacebook(), 60);
                    if (string.IsNullOrEmpty(_case))
                    {
                        client.AppStart(FacebookHander.Package(PlatformModel.Facebook), true, true, true);
                        continue;
                    }
                    SetStatus("Đang xử lý...", 2, logDetail: $"[FacebookService.Login] case={_case}");
                    switch (_case)
                    {
                        case var c when XpathManagerFacebook.Get(XpathType.Loading).Contains(c): continue;
                        case var c when XpathManagerFacebook.Get(XpathType.CP282).Contains(c):
                            subyEnum = SubdyEnum.CP_282;
                            message = $"Tài khoản bị checkpoint 282 [{ExtractReadable(c)}]";
                            throw new SubdyExtension(subyEnum, message);
                        case var c when XpathManagerFacebook.Get(XpathType.CP956).Contains(c):
                            subyEnum = SubdyEnum.CP_956;
                            message = $"Tài khoản bị checkpoint 956 [{ExtractReadable(c)}]";
                            throw new SubdyExtension(subyEnum, message);
                        case var c when XpathManagerFacebook.Get(XpathType.Captcha).Contains(c):
                            {
                                bool check = false;
                                for (int i = 0; i < 5; i++)
                                {
                                    check = await HandleCaptchaAsync();

                                    if (check)
                                    {
                                        break;
                                    }
                                }
                                if (check)
                                {
                                    continue;
                                }
                                subyEnum = SubdyEnum.Captcha;
                                message = $"Tài khoản bị yêu cầu captcha [{ExtractReadable(c)}]";
                                throw new SubdyExtension(subyEnum, message);
                            }

                        case var c when XpathManagerFacebook.Get(XpathType.Block).Contains(c):
                            if (c == $"//*[contains(@text, \"Dismiss\")]")
                            {
                                client.ElementWithAttributes(c, 1);
                                continue;
                            }
                            subyEnum = SubdyEnum.Block;
                            message = $"Tài khoản bị chặn [{ExtractReadable(c)}]";
                            throw new SubdyExtension(subyEnum, message);
                        case var c when XpathManagerFacebook.Get(XpathType.WrongPassword).Contains(c):
                            subyEnum = SubdyEnum.WrongPassword;
                            message = $"Sai mật khẩu hoặc tài khoản [{ExtractReadable(c)}]";
                            throw new SubdyExtension(subyEnum, message);
                        case var c when XpathManagerFacebook.Get(XpathType.Logout).Contains(c):
                            subyEnum = SubdyEnum.LogOut;
                            message = $"Tài khoản bị đăng xuất [{ExtractReadable(c)}]";
                            throw new SubdyExtension(subyEnum, message);
                        case var c when XpathManagerFacebook.Get(XpathType.Success).Contains(c):
                            if (client.ElementWithAttributes(new List<string> { "//*[@content-desc=\"Something went wrong\"]", "//*[@content-desc=\"try again\"]" }, 2, "", false))
                            {
                                client.StopApp("com.facebook.katana");
                                client.Delay(2);
                                client.AppStart("com.facebook.katana");
                                continue;
                            }
                            subyEnum = SubdyEnum.Success;
                            message = $"Đăng nhập thành công [{ExtractReadable(c)}]";
                            return new SubdyExtension(subyEnum, message);
                        case var c when XpathManagerFacebook.Get(XpathType.InputUserName).Contains(c):
                            await ImportUid();
                            break;
                        case var c when XpathManagerFacebook.Get(XpathType.InputPassword).Contains(c):
                            await ImportPassword();
                            break;
                        case var c when XpathManagerFacebook.Get(XpathType.TowFA).Contains(c):
                            await Import2FA();
                            break;
                        case var c when XpathManagerFacebook.Get(XpathType.NavigationButton).Contains(c):
                            client.ElementWithAttributes(c, 1);
                            break;
                        case var c when XpathManagerFacebook.Get(XpathType.CashApp).Contains(c):

                            await main.SessionExpired();
                            break;
                        case var c when XpathManagerFacebook.Get(XpathType.No_Internet).Contains(c):
                            await HandleNoInternet();
                            break;
                    }
                    client.Delay(3);

                }
            }
            finally
            {
                Stopwatch.Stop();
            }




            throw new SubdyExtension(subyEnum, message);
        }
        private async Task ImportUid()
        {
            _sate = "Nhập tài khoản";
            SetStatus("Đang chọn đăng nhập tài khoản khác...", 2);
            _client.ElementWithAttributes(new List<string> { "//*[@text=\"Log into another account\"]", "//*[@text=\"Use another profile\"]" }, 3);
            string uid = _account.Uid_Email;
            var elements = _client.FindElements(10, "", "//*[@class='android.widget.EditText']");
            if (!elements.Any()) return;
            SetStatus("Đang nhập tên đăng nhập...", 2);
            // Facebook hiện chỉ có 1 EditText ở màn nhập username; password ở màn tiếp theo
            _client.SendTextSlow("//*[@class='android.widget.EditText']", uid, xml: elements[0].OuterXml);
            SetStatus("Đang xác nhận...", 2);
            if (elements.Count >= 2)
            {
                _client.SendTextSlow("//*[@class='android.widget.EditText']", _account.Password, xml: elements[1].OuterXml);
            }
            _client.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.NavigationButton));
            SetStatus("Đợi phản hồi từ Facebook...", 2);
            _client.Delay(5);
            return;
        }
        private async Task ImportPassword()
        {
            _sate = "Nhập mật khẩu";
            SetStatus("Đang nhập mật khẩu...", 2);
            _client.SendTextSlow("//*[@class='android.widget.EditText']", _account.Password);
            SetStatus("Đang xác nhận...", 2);
            _client.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.NavigationButton));
            SetStatus("Đợi phản hồi từ Facebook...", 2);
            _client.Delay(7);
            return;
        }
        private async Task Import2FA()
        {
            _sate = "Xác thực 2 bước";
            SetStatus("Đang chuẩn bị xác thực 2 bước...", 2);
            if (string.IsNullOrEmpty(_account.TowFA) && !string.IsNullOrEmpty(_account.Email) && !string.IsNullOrEmpty(_account.MailClientId)
                && !string.IsNullOrEmpty(_account.MailRefreshToken) && !string.IsNullOrEmpty(_account.PassMail))
            {
                string element_email = _client.FindElement("", new List<string> { "//*[@content-desc='Try another way']", "//*[@content-desc=\"Check your email\"]" }, 10);
                if (element_email == "//*[@content-desc='Try another way']")
                {
                    _client.ElementWithAttributes("//*[@content-desc='Try another way']", 10);
                    _client.Delay(3);
                    _client.ElementWithAttributes(new List<string> {
                   $"//*[contains(@content-desc, \"Email, We’ll send a code to\")]",
                    $"//*[contains(@content-desc, \"Email\")]"
                }, 10);
                    _client.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.NavigationButton));
                }
                SetStatus("Đang lấy mã xác thực từ email (dongvanfb)...", 2);
                _client.ElementWithAttributes("//*[@content-desc=\"Get a new code\"]", 10);
                _client.Delay(5);
                string code_mail = await GetEmailCodeFromDongVanAsync(80);
                if (string.IsNullOrEmpty(code_mail))
                {
                    SetStatus("Không lấy được mã xác thực email, chuyển tài khoản khác.", 3);
                    throw new SubdyExtension(SubdyEnum.LogOut, "[FacebookService.Import2FA] Không lấy được mã xác thực email từ dongvanfb sau 80s.");
                }

                SetStatus($"Đang nhập mã xác thực email: {code_mail}", 2);
                _client.SendTextSlow("//*[@class='android.widget.EditText']", code_mail);
                _client.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.NavigationButton));
                SetStatus("Đợi phản hồi từ Facebook...", 2);
                _client.Delay(7);
                return;
            }
            if (string.IsNullOrEmpty(_account.TowFA))
            {


                SetStatus("Tài khoản không có mã 2FA để nhập.", 2);
                throw new SubdyExtension(SubdyEnum.LogOut, "[FacebookService.Import2FA] Tài khoản không có mã 2FA, không thể xác thực.");
            }
            string element = _client.FindElement("", new List<string> { "//*[@content-desc='Try another way']", "//*[@text=\"OK\"]", "//*[@class='android.widget.EditText']" }, 10);
            if (element == "//*[@content-desc='Try another way']")
            {
                _client.ElementWithAttributes(element, 10);

                _client.ElementWithAttributes(new List<string> {
                    "//*[@content-desc='Authentication app, Get a code from your authentication app.']",
                    "//*[@text=\"Authentication app\"]",
                    "//*[@text=\"Get a code from your authentication app.\"]"
                }, 10);
                _client.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.NavigationButton));
            }
            else if (element == "//*[@text=\"OK\"]")
            {
                _client.ElementWithAttributes("//*[@text=\"OK\"]", 10);
            }
            SetStatus("Đang lấy mã xác thực...", 2);
            string code = FacebookHander.GetCodeTowFA(_account.TowFA);
            SetStatus("Đang nhập mã xác thực...", 2);
            _client.SendTextSlow("//*[@class='android.widget.EditText']", code);
            SetStatus("Đang xác nhận mã...", 2);
            _client.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.NavigationButton));
            SetStatus("Đợi phản hồi từ Facebook...", 2);
            _client.Delay(7);
            return;
        }
        private async Task<string> GetEmailCodeFromDongVanAsync(int timeoutSeconds)
        {
            var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            string lastError = string.Empty;
            int attempt = 0;
            while (DateTime.UtcNow < deadline)
            {
                CheckStop(int.MaxValue / 1000);
                attempt++;
                DongVanFbCodeResult result;
                try
                {
                    result = await DongVanFbClient.GetFacebookCodeAsync(
                        _account.Email, _account.MailRefreshToken, _account.MailClientId, _ct);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    SetStatus($"Lỗi gọi dongvanfb (lần {attempt}): {ex.Message}", 3);
                    _client.Delay(5);
                    continue;
                }

                if (result.Status && !string.IsNullOrEmpty(result.Code))
                {
                    return result.Code;
                }

                lastError = string.IsNullOrEmpty(result.Error) ? "Không có mã mới" : result.Error;
                SetStatus($"Chưa có mã (lần {attempt}): {lastError}", 2);
                _client.Delay(5);
            }
            if (!string.IsNullOrEmpty(lastError))
            {
                _client?.LogHelper?.SUCCESS($"[FacebookService.GetEmailCodeFromDongVanAsync] timeout. lastError={lastError}");
            }
            return string.Empty;
        }
        public Task<SubdyExtension> Reaction(ADBClient client, Account account, string type, CancellationToken ct)
        {
            throw new NotImplementedException();
        }
        public async Task<SubdyExtension> HanderAccount(ADBClient client, Account account, int timeout, CancellationToken ct, MainService main)
        {
            _client = client;
            _account = account;
            _ct = ct;
            _main = main;
            SetStatus("Đang kiểm tra trạng thái tài khoản...", 2);
            _sate = "Kiểm tra trạng thái tài khoản";
            string _case = string.Empty;
            SubdyEnum subyEnum = SubdyEnum.None;
            string message = "Lỗi trong quá trình kiểm tra tài khoản!";
            const int MAX_RELOGIN_ATTEMPTS = 2;
            int reloginAttempts = 0;
            while (true)
            {
                if (client.IsRunningApp(FacebookHander.Package(PlatformModel.Facebook)) == false && !client.ElementWithAttributes("//*[@text=\"Close app\"]"))
                {
                    client.AppStart(FacebookHander.Package(PlatformModel.Facebook), true, true, true);
                    client.Delay(5);
                    continue;
                }
                _case = client.FindElement("", FacebookHander.GetActiAccountFacebook(), timeout);
                if (string.IsNullOrEmpty(_case))
                {
                    subyEnum = SubdyEnum.Success;
                    message = $"Không tìm thấy case phù hợp...";
                    return new SubdyExtension(subyEnum, message);
                }
                SetStatus("Đang kiểm tra...", 2, logDetail: $"[FacebookService.HanderAccount] case={_case}");
                switch (_case)
                {
                    case var c when XpathManagerFacebook.Get(XpathType.Loading).Contains(c): continue;
                    case var c when XpathManagerFacebook.Get(XpathType.CP282).Contains(c):
                        subyEnum = SubdyEnum.CP_282;
                        message = $"Tài khoản bị checkpoint 282 [{ExtractReadable(c)}]";
                        throw new SubdyExtension(subyEnum, message);
                    case var c when XpathManagerFacebook.Get(XpathType.CP956).Contains(c):
                        subyEnum = SubdyEnum.CP_956;
                        message = $"Tài khoản bị checkpoint 956 [{ExtractReadable(c)}]";
                        throw new SubdyExtension(subyEnum, message);
                    case var c when XpathManagerFacebook.Get(XpathType.Captcha).Contains(c):
                        if (await HandleCaptchaAsync())
                        {
                            continue;
                        }
                        subyEnum = SubdyEnum.Captcha;
                        message = $"Tài khoản bị yêu cầu captcha [{ExtractReadable(c)}]";
                        throw new SubdyExtension(subyEnum, message);
                    case var c when XpathManagerFacebook.Get(XpathType.Block).Contains(c):
                        subyEnum = SubdyEnum.Block;
                        message = $"Tài khoản bị chặn [{ExtractReadable(c)}]";
                        throw new SubdyExtension(subyEnum, message);
                    case var c when XpathManagerFacebook.Get(XpathType.WrongPassword).Contains(c):
                        subyEnum = SubdyEnum.WrongPassword;
                        message = $"Sai mật khẩu hoặc tài khoản [{ExtractReadable(c)}]";
                        throw new SubdyExtension(subyEnum, message);
                    case var c when XpathManagerFacebook.Get(XpathType.Logout).Contains(c):
                    case var c2 when XpathManagerFacebook.Get(XpathType.InputUserName).Contains(c2):
                    case var c3 when XpathManagerFacebook.Get(XpathType.InputPassword).Contains(c3):
                    case var c4 when XpathManagerFacebook.Get(XpathType.TowFA).Contains(c4):
                        if (reloginAttempts >= MAX_RELOGIN_ATTEMPTS)
                        {
                            subyEnum = SubdyEnum.LogOut;
                            message = $"Tài khoản bị đăng xuất [{ExtractReadable(_case)}]. Đã thử đăng nhập lại {reloginAttempts} lần nhưng thất bại.";
                            throw new SubdyExtension(subyEnum, message);
                        }
                        reloginAttempts++;
                        SetStatus($"Phát hiện bị đăng xuất [{ExtractReadable(_case)}], đang đăng nhập lại (lần {reloginAttempts}/{MAX_RELOGIN_ATTEMPTS})...", 2,
                            logDetail: $"[FacebookService.HanderAccount] Relogin attempt {reloginAttempts}/{MAX_RELOGIN_ATTEMPTS}, case={_case}");
                        await main.SessionExpired();
                        var reloginResult = await Login(client, account, ct, timeout, main);
                        if (reloginResult?.SubdyEnum == SubdyEnum.Success)
                        {
                            await main.ExtractAndUpdateAuthenticationInfoAsync();
                        }
                        _sate = "Kiểm tra trạng thái tài khoản";
                        break;
                    case var c when XpathManagerFacebook.Get(XpathType.Success).Contains(c):
                        subyEnum = SubdyEnum.Success;
                        message = $"Đăng nhập thành công [{ExtractReadable(c)}]";
                        return new SubdyExtension(subyEnum, message);
                    case var c when XpathManagerFacebook.Get(XpathType.NavigationButton).Contains(c):
                        client.ElementWithAttributes(c, 1);
                        break;
                    case var c when XpathManagerFacebook.Get(XpathType.CashApp).Contains(c):
                        SetStatus($"Phát hiện CashApp [{ExtractReadable(c)}], đang đăng nhập lại...", 2,
                            logDetail: $"[FacebookService.HanderAccount] CashApp detected, case={c}");
                        await main.SessionExpired();
                        await Login(client, account, ct, timeout, main);
                        await main.ExtractAndUpdateAuthenticationInfoAsync();
                        break;
                    case var c when XpathManagerFacebook.Get(XpathType.No_Internet).Contains(c):
                        SetStatus($"Mất kết nối internet [{ExtractReadable(c)}], đang xử lý...", 2,
                            logDetail: $"[FacebookService.HanderAccount] No_Internet detected, case={c}");
                        await HandleNoInternet();
                        break;
                }

            }
        }

        public Task<Dictionary<string, string>> GetInfo(ADBClient client)
        {
            throw new NotImplementedException();
        }

        public Task<Dictionary<string, string>> UpateInfo(ADBClient client, string fullename, string bio, string username)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Khi gặp case Captcha (image-based "Enter the characters you see") thì:
        /// 1) Lấy bounds của ảnh captcha từ XML hiện tại.
        /// 2) Chụp screenshot, crop theo bounds rồi gửi base64 lên service giải captcha (cap.guru).
        /// 3) Nhập kết quả vào EditText và bấm Continue.
        /// Trả về true nếu giải + nhập thành công, false nếu thất bại để caller có thể throw SubdyExtension như cũ.
        /// </summary>
        private async Task<bool> HandleCaptchaAsync()
        {
            try
            {
                _sate = "Giải captcha";
                SetStatus("Đang chuẩn bị giải captcha...", 2);

                var setting = _main?._settingGeneral;
                if (setting == null)
                {
                    SetStatus("Không có cấu hình chung để lấy key captcha.", 3);
                    return false;
                }

                string key = setting.GetValuesFromInputString("textBoxCaptchaKey");
                if (string.IsNullOrEmpty(key))
                {
                    SetStatus("Chưa cấu hình key captcha trong Cài đặt chung.", 3);
                    return false;
                }

                // Hiện chỉ hỗ trợ cap.guru cho image captcha.
                string site = GuruCaptchaClient.Url;

                // 1. Lấy bounds của ảnh captcha (ImageView nằm ngay dưới text "Enter the characters you see").
                string xml = _client.GetXMLSource();
                string? base64Image = CropCaptchaImage(xml);
                if (string.IsNullOrEmpty(base64Image))
                {
                    SetStatus("Không xác định được vùng ảnh captcha.", 3);
                    return false;
                }

                // 2. Gửi ảnh lên service giải captcha + poll token.
                SetStatus("Đang gửi ảnh captcha lên service...", 2);
                string id = await CaptchaService.GetIdImageCaptcha(site, key, base64Image);
                if (string.IsNullOrEmpty(id) || id.Contains("error"))
                {
                    SetStatus($"Tạo id captcha thất bại: {id}", 3);
                    return false;
                }

                int timeoutSec = 180;
                var sw = Stopwatch.StartNew();
                string token = string.Empty;
                while (sw.ElapsedMilliseconds < timeoutSec * 1000)
                {
                    CheckStop(timeoutSec);
                    string result = await CaptchaService.GetTokenCaptchaV2(site, key, id);
                    if (!string.IsNullOrEmpty(result) && !result.Contains("error"))
                    {
                        token = result;
                        break;
                    }
                    SetStatus($"Đợi kết quả captcha ({sw.Elapsed.TotalSeconds:F0}/{timeoutSec}s)...", 2);
                    _client.Delay(3);
                }

                if (string.IsNullOrEmpty(token))
                {
                    SetStatus("Hết thời gian chờ kết quả captcha.", 3);
                    return false;
                }

                // 3. Nhập kết quả vào EditText và bấm Continue.
                SetStatus($"Đang nhập captcha: {token}", 2);
                _client.SendTextADB("//*[@class='android.widget.EditText']", token);
                _client.Delay(2);
                _client.ElementWithAttributes("//*[@content-desc=\"Continue\"]", 5);
                _client.Delay(5);
                return true;
            }
            catch (SubdyExtension)
            {
                throw;
            }
            catch (Exception ex)
            {
                SetStatus($"Lỗi giải captcha: {ex.Message}", 3,
                    logDetail: $"[FacebookService.HandleCaptchaAsync] {ex}");
                return false;
            }
        }

        /// <summary>
        /// Tìm bounds ImageView của captcha trong XML và crop từ screenshot ra base64 PNG.
        /// Fallback: nếu không match được ImageView thì lấy vùng dưới text "Enter the characters you see"
        /// đến trước EditText.
        /// </summary>
        private string? CropCaptchaImage(string xml)
        {
            Bitmap? screen = null;
            Bitmap? cropped = null;
            try
            {
                screen = _client.Screenshot();
                if (screen == null || screen.Width <= 0 || screen.Height <= 0) return null;

                Rectangle? rect = TryGetCaptchaBoundsFromXml(xml);

                // Bounds từ XML có thể theo toạ độ device thật, screenshot có thể đã scale →
                // map lại tỉ lệ theo screen.Width/Height từ DeviceServices nếu có sai khác.
                if (rect.HasValue)
                {
                    rect = ScaleRectToScreen(rect.Value, screen.Width, screen.Height);
                }

                Rectangle crop = rect ?? new Rectangle(0, screen.Height / 6, screen.Width, screen.Height / 4);
                crop.Intersect(new Rectangle(0, 0, screen.Width, screen.Height));
                if (crop.Width <= 1 || crop.Height <= 1) return null;

                // Dùng DrawImage thay vì Bitmap.Clone(rect, pixelFormat) — Clone hay throw
                // OutOfMemoryException khi PixelFormat của source là Indexed/Format8bppIndexed
                // (ATX/Appium screenshot đôi khi trả về vậy) hoặc khi rect chạm rìa do scale.
                cropped = new Bitmap(crop.Width, crop.Height, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(cropped))
                {
                    g.DrawImage(screen, new Rectangle(0, 0, crop.Width, crop.Height),
                        crop, GraphicsUnit.Pixel);
                }

                using var ms = new MemoryStream();
                cropped.Save(ms, ImageFormat.Png);
                return Convert.ToBase64String(ms.ToArray());
            }
            catch (Exception ex)
            {
                _client?.LogHelper?.Log($"[CropCaptchaImage] {ex.Message}");
                return null;
            }
            finally
            {
                cropped?.Dispose();
                screen?.Dispose();
            }
        }

        private static Rectangle ScaleRectToScreen(Rectangle rect, int screenWidth, int screenHeight)
        {
            // Nếu rect đã nằm trong screen thì giữ nguyên.
            if (rect.Right <= screenWidth && rect.Bottom <= screenHeight)
                return rect;

            // Suy luận device resolution dựa trên bounds: lấy max của Right/Bottom so với screen.
            // Đây là heuristic — đa số trường hợp ImageView có bounds ~ full width thì
            // Right ≈ deviceWidth. Tránh chia 0.
            int deviceWidth = Math.Max(rect.Right, screenWidth);
            int deviceHeight = Math.Max(rect.Bottom, screenHeight);
            if (deviceWidth <= 0 || deviceHeight <= 0) return rect;

            double sx = (double)screenWidth / deviceWidth;
            double sy = (double)screenHeight / deviceHeight;
            return new Rectangle(
                (int)(rect.X * sx),
                (int)(rect.Y * sy),
                (int)(rect.Width * sx),
                (int)(rect.Height * sy));
        }

        private Rectangle? TryGetCaptchaBoundsFromXml(string xml)
        {
            if (string.IsNullOrEmpty(xml)) return null;

            // Thử lấy ImageView trong cùng cây với text "Enter the characters you see" / "characters you see".
            var candidates = new[]
            {
                "//*[contains(@text, 'Enter the characters you see')]/following::*[@class='android.widget.ImageView'][1]",
                "//*[contains(@content-desc, 'Enter the characters you see')]/following::*[@class='android.widget.ImageView'][1]",
                "//*[@class='android.widget.ImageView' and (contains(@resource-id,'captcha') or contains(@content-desc,'captcha'))]",
            };

            foreach (var xpath in candidates)
            {
                var bounds = _client.FindBounds(xml, xpath, 0);
                if (bounds != null && bounds.Count > 0)
                {
                    var rect = ParseBounds(bounds[0]);
                    if (rect.HasValue) return rect;
                }
            }
            return null;
        }

        private static Rectangle? ParseBounds(string bounds)
        {
            if (string.IsNullOrEmpty(bounds)) return null;
            var match = Regex.Match(bounds, @"\[(\d+),(\d+)\]\[(\d+),(\d+)\]");
            if (!match.Success) return null;
            int x1 = int.Parse(match.Groups[1].Value);
            int y1 = int.Parse(match.Groups[2].Value);
            int x2 = int.Parse(match.Groups[3].Value);
            int y2 = int.Parse(match.Groups[4].Value);
            return new Rectangle(x1, y1, x2 - x1, y2 - y1);
        }

        private async Task HandleNoInternet()
        {
            _sate = "Xử lý mất kết nối internet";
            SetStatus("Phát hiện mất kết nối internet, đang xử lý...", 2);
            int maxRetry = 5;
            for (int i = 1; i <= maxRetry; i++)
            {
                SetStatus($"Tắt wifi, thử lần {i}/{maxRetry}...", 2);
                await _client.DisableWifi();
                _client.Delay(10);

                SetStatus($"Bật wifi, thử lần {i}/{maxRetry}...", 2);
                await _client.EnableWifi();
                _client.Delay(20);

                SetStatus($"Kiểm tra IP lần {i}/{maxRetry}...", 2);
                string ip = await _client.GetIp();
                if (!string.IsNullOrEmpty(ip))
                {
                    SetStatus($"Đã kết nối lại internet, IP: {ip}", 2);
                    return;
                }
            }

            SetStatus("Thử wifi 5 lần thất bại, đang khởi động lại thiết bị...", 2);
            _client.RebootAndWaitForDeviceReady();
            _client.Delay(30);

            string ipAfterReboot = await _client.GetIp();
            if (!string.IsNullOrEmpty(ipAfterReboot))
            {
                SetStatus($"Sau khởi động lại đã có internet, IP: {ipAfterReboot}", 2);
                return;
            }

            SetStatus("Không có internet sau khởi động lại, dừng luồng thiết bị.", 3);
            throw new SubdyExtension(SubdyEnum.No_Internet, "[FacebookService.HandleNoInternet] Thiết bị không có internet sau khi thử wifi 5 lần và khởi động lại.");
        }
    }
}
