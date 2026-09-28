using AutoAndroid;
using Sunny.Subdy.Common.Logs;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sunny.Subd.Core.Proxies
{
    // Điều khiển proxy qua ứng dụng com.scheler.superproxy.
    //
    // ── VÌ SAO KHÔNG DÙNG TOẠ ĐỘ PIXEL NHƯ BẢN PORT ĐẦU ────────────────────────────
    // Bản đầu port nguyên hằng số pixel của gmailus.py (crop nút 48,1348 984x150;
    // toggle 540,1415). Các toạ độ đó ĐÚNG cho màn hình tool Python chạy, nhưng SAI
    // trên thiết bị của mình: máy bị SetSize() ép về 1440x2560, và đo LIVE 2026-09-26
    // trên 5200d7ad5a6315d5 cho thấy:
    //   - vùng crop 48,1348..1032,1498 rơi vào ô nhập "user4925" (nền be),
    //     màu trung bình R=230.1 G=220.3 B=203.3 → KHÔNG khớp đỏ/xanh
    //     → DetectState() luôn trả Unknown → CheckAndEnableAsync() luôn trả false
    //     → proxy KHÔNG BAO GIỜ được bật một cách chủ động.
    //   - tap "mở app" (730,445) rơi vào thẻ Default Profile; tap toggle (540,1415)
    //     rơi vào ô user → bấm vào chỗ vô nghĩa.
    //   - RIÊNG tap 554,2045 tình cờ nằm TRONG nút Start thật nên thỉnh thoảng bật/tắt
    //     proxy một cách NGẪU NHIÊN (đúng lúc đang mở app) → trạng thái proxy loạn.
    // Nút thật trên máy, dump uiautomator:
    //   desc='Start' clickable=true bounds=[56,1944][1384,2119]  (center 720,2031)
    //   desc='Stop'  clickable=true bounds=[56,1944][1384,2119]  (đổi desc khi đang chạy)
    // → Nhận diện bằng UI (content-desc Start/Stop) là độc lập độ phân giải, thay cho
    //   dò màu. Hằng số pixel cũ chỉ giữ làm fallback cuối khi dump UI rỗng.
    //
    // ── ĐỔI IP GIỮA CÁC ACC ────────────────────────────────────────────────────────
    // Nút này là công tắc ON/OFF của cùng một cấu hình proxy, KHÔNG tự đổi IP. Muốn có
    // IP mới cho acc kế tiếp thì phải TẮT rồi BẬT LẠI (proxy xin endpoint mới). Đo LIVE
    // trên 5200d7ad5a6315d5, hai lượt độc lập 2026-09-27:
    //   57.138.33.237 → 104.61.181.235 → 97.248.39.169 → 38.62.129.202
    //   104.187.24.60 → 184.93.70.92  → 9.244.132.89  → 150.228.211.43
    // IP đổi thật sau mỗi lần tắt/bật, không lần nào trùng. Vì vậy luồng đăng ký phải gọi
    // RestartAsync() cho MỖI acc — nếu chỉ "bật nếu đang tắt" thì mọi acc dùng chung một IP
    // và Facebook chặn đăng ký (đúng hiện tượng acc #4 kẹt ở màn "I agree").
    public class SuperProxyService
    {
        public const string Package = "com.scheler.superproxy";
        public const string LaunchComponent = "com.scheler.superproxy/.activity.MainActivity";

        // Nút công tắc thật. "Stop" = proxy ĐANG CHẠY, "Start" = proxy ĐANG TẮT.
        private static readonly List<string> XStop = new List<string>
        {
            "//*[@clickable=\"true\" and @content-desc=\"Stop\"]",
            "//*[@content-desc=\"Stop\"]",
        };
        private static readonly List<string> XStart = new List<string>
        {
            "//*[@clickable=\"true\" and @content-desc=\"Start\"]",
            "//*[@content-desc=\"Start\"]",
        };
        // Thanh tab dưới cùng — bấm về tab Proxies nếu app mở nhầm tab khác.
        private static readonly List<string> XProxiesTab = new List<string>
        {
            "//*[@clickable=\"true\" and contains(@content-desc,\"Proxies\")]",
            "//*[contains(@content-desc,\"Proxies\")]",
        };

        // Fallback khi dump UI không thấy nút nào (app chưa render / uiautomator bị kill).
        // KHÔNG dùng toạ độ cứng của bản Python (540,1415 — đúng cho layout 1080x2400 đời
        // cũ, trên máy mình đo được là rơi vào ô nhập user). Thay bằng toạ độ SUY THEO KÍCH
        // THƯỚC MÀN HÌNH THẬT: trên layout hiện tại nút chiếm [56,1944][1384,2119] của
        // 1440x2560 → tâm ngang giữa màn, tâm dọc ~79.3% chiều cao. Suy theo tỉ lệ nên còn
        // dùng được nếu máy khác độ phân giải.
        private const double ToggleYRatio = 0.793;

        private void TapToggleByRatio()
        {
            int w = 1080, h = 1920;
            try
            {
                string size = _client.Shell("wm size") ?? string.Empty;
                var m = System.Text.RegularExpressions.Regex.Match(size, @"(?:Override|Physical) size:\s*(\d+)x(\d+)");
                if (m.Success)
                {
                    w = int.Parse(m.Groups[1].Value);
                    h = int.Parse(m.Groups[2].Value);
                }
            }
            catch { /* giữ mặc định */ }

            int x = w / 2;
            int y = (int)(h * ToggleYRatio);
            _client.LogHelper?.Log($"[SuperProxy] Dự phòng: tap ({x},{y}) theo màn {w}x{h}.");
            _client.Shell($"input tap {x} {y}");
        }

        private readonly ADBClient _client;

        public SuperProxyService(ADBClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        // Mở app proxy, chờ nút công tắc hiện ra và đưa tab Proxies lên trước.
        private bool OpenAndWaitForButton()
        {
            _client.LogHelper?.SUCCESS(">>> [SuperProxy] Mở com.scheler.superproxy");
            _client.Shell($"am start -n {LaunchComponent}");

            // Poll thay cho sleep(11) cứng của bản Python: thoát ngay khi nút đã render.
            for (int i = 0; i < 40; i++)
            {
                if (ButtonVisible()) return true;
                Thread.Sleep(500);
            }

            // Chưa thấy: có thể đang ở tab khác → bấm tab Proxies rồi chờ thêm.
            _client.ElementWithAttributes(XProxiesTab, 3);
            for (int i = 0; i < 20; i++)
            {
                if (ButtonVisible()) return true;
                Thread.Sleep(500);
            }
            return false;
        }

        private bool ButtonVisible()
            => _client.ElementWithAttributes(XStop, 1, click: false)
            || _client.ElementWithAttributes(XStart, 1, click: false);

        // Trạng thái hiện tại: true = proxy đang chạy (nút hiển thị "Stop").
        // null = không xác định được (không thấy nút).
        private bool? CurrentState()
        {
            if (_client.ElementWithAttributes(XStop, 1, click: false)) return true;
            if (_client.ElementWithAttributes(XStart, 1, click: false)) return false;
            return null;
        }

        // Bấm công tắc và xác minh desc đã lật sang trạng thái đối diện.
        private bool TapToggleAndVerify(bool expectOn)
        {
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                bool clicked = expectOn
                    ? _client.ElementWithAttributes(XStart, 5)
                    : _client.ElementWithAttributes(XStop, 5);

                if (!clicked)
                {
                    // Không thấy node để bấm → fallback toạ độ suy theo màn hình (chỉ đường này).
                    _client.LogHelper?.Log("[SuperProxy] Không thấy nút công tắc bằng UI → thử toạ độ dự phòng.");
                    TapToggleByRatio();
                }

                // Proxy cần vài giây để bật tunnel / xin IP mới.
                Thread.Sleep(expectOn ? 6000 : 3000);

                bool? state = CurrentState();
                if (state == expectOn)
                {
                    return true;
                }
                _client.LogHelper?.Log($"[SuperProxy] Lần {attempt}: chưa đạt trạng thái {(expectOn ? "BẬT" : "TẮT")} (đang {(state == null ? "không rõ" : (state.Value ? "BẬT" : "TẮT"))}), thử lại...");
            }
            return false;
        }

        // Bảo đảm proxy ĐANG BẬT. Không đụng tới nếu đã bật (dùng cho lần chạy đầu).
        public async Task<bool> CheckAndEnableAsync()
        {
            try
            {
                if (!OpenAndWaitForButton())
                {
                    _client.LogHelper?.ERROR("[SuperProxy] Không thấy nút Start/Stop sau khi mở app.");
                    return false;
                }

                bool? state = CurrentState();
                if (state == true)
                {
                    _client.LogHelper?.SUCCESS("[SuperProxy] Proxy đã bật.");
                    return true;
                }

                _client.LogHelper?.Log("[SuperProxy] Proxy chưa bật -> đang bật lên...");
                bool ok = TapToggleAndVerify(true);
                _client.LogHelper?.Log(ok ? "[SuperProxy] Đã bật proxy." : "[SuperProxy] Bật proxy THẤT BẠI.");
                await Task.Yield();
                return ok;
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                _client.LogHelper?.ERROR($"[SuperProxy] lỗi: {ex.Message}");
                return false;
            }
        }

        // TẮT rồi BẬT LẠI để lấy IP MỚI. Gọi cho MỖI tài khoản đăng ký.
        public async Task<bool> RestartAsync()
        {
            try
            {
                if (!OpenAndWaitForButton())
                {
                    _client.LogHelper?.ERROR("[SuperProxy] Không thấy nút Start/Stop sau khi mở app.");
                    return false;
                }

                bool? state = CurrentState();
                if (state == true)
                {
                    _client.LogHelper?.Log("[SuperProxy] Tắt proxy để đổi IP...");
                    if (!TapToggleAndVerify(false))
                    {
                        _client.LogHelper?.Log("[SuperProxy] Không tắt được proxy, thử bật lại ngay.");
                    }
                }
                else if (state == null)
                {
                    _client.LogHelper?.Log("[SuperProxy] Không rõ trạng thái proxy, thử bật lại.");
                }

                // Nghỉ ngắn trước khi bật lại để nhà cung cấp proxy nhả endpoint cũ.
                Thread.Sleep(2000);

                _client.LogHelper?.Log("[SuperProxy] Bật lại proxy để lấy IP mới...");
                bool ok = TapToggleAndVerify(true);
                _client.LogHelper?.Log(ok
                    ? "[SuperProxy] Đã bật lại proxy (IP mới)."
                    : "[SuperProxy] Bật lại proxy THẤT BẠI.");
                await Task.Yield();
                return ok;
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                _client.LogHelper?.ERROR($"[SuperProxy] lỗi: {ex.Message}");
                return false;
            }
        }
    }
}