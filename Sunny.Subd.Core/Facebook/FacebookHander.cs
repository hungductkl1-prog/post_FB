using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AutoAndroid;
using System.Text.Json.Nodes;
using OtpNet;
using Sunny.Subd.Core.Models;
using Sunny.Subd.Core.Utils;
using Sunny.Subdy.Common.Models;

namespace Sunny.Subd.Core.Facebook
{
    public class FacebookHander
    {
        public const string DOWNLOAD_FACEBOOK = "https://www.facebook.com/download/direct/fb4a/";
        public const string DOWNLOAD_INSTAGRAM = "https://d.apkpure.com/b/APK/com.instagram.android?version=latest";

        public const string DOWNLOAD_THREADS = "https://d.apkpure.com/b/APK/com.instagram.barcelona?version=latest";

        public static string DownloadUrl(string platform)
        {
            switch (platform)
            {
                case PlatformModel.Facebook:
                    return DOWNLOAD_FACEBOOK;
                case PlatformModel.Instagram:
                    return DOWNLOAD_INSTAGRAM;
                case PlatformModel.Threads:
                    return DOWNLOAD_THREADS;
                default:
                    throw new ArgumentException("Unsupported platform: " + platform);
            }
        }
        public static List<string> TypeLogin = new List<string>
        {
            "Uid|Password",
            "Email|Password",
        };
        public static string FilePath(string platform)
        {
            return Path.Combine(AppContext.BaseDirectory, "App", $"{platform}.apk");
        }
        public static string Package(string platform)
        {
            switch (platform)
            {
                case PlatformModel.Facebook:
                    return "com.facebook.katana";
                case "messenger":
                    return "com.facebook.orca";
                case "Instagram":
                    return "com.instagram.android";
                case PlatformModel.Threads:
                    return "com.instagram.barcelona";
                case PlatformModel.Pandora:
                    return "com.pandora.android";
                default:
                    throw new ArgumentException("Unsupported platform: " + platform);
            }
        }
        public static List<string> GetActiAccountFacebook()
        {
            var xpaths = XpathManagerFacebook.Combine
                (
                    XpathType.CP282,
                    XpathType.Loading,
                    XpathType.Captcha,
                    XpathType.CP956,
                    XpathType.Logout,
                    XpathType.WrongPassword,
                    XpathType.Block,
                    XpathType.Success,
                    XpathType.CashApp,
                    XpathType.TowFA,
                    XpathType.InputUserName,
                    XpathType.InputPassword,
                    // Đứng TRƯỚC NavigationButton: trên màn "pay or consent" phần tử
                    // khớp đầu của NavigationButton là nút Continue mờ (vô tác dụng).
                    XpathType.MetaAdsConsent,
                    XpathType.NavigationButton,
                    XpathType.No_Internet
                );
            return xpaths;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // QUÉT & BẤM NÚT "DISMISS" Ở BẤT KỲ MÀN NÀO — thêm 2026-09-08
        // ─────────────────────────────────────────────────────────────────────────
        // VẤN ĐỀ (vd màn "We suspect automated behavior on your account" chỉ có 1 nút Dismiss):
        // dialog Facebook là CỬA SỔ CHỒNG LÊN màn đang chạy, và uiautomator dump bắt được node
        // của CẢ màn nền phía sau. ADBClient.FindElement trả về XPATH KHỚP ĐẦU TIÊN THEO THỨ TỰ
        // LIST (ADBClient.cs:3514-3521), nên bất kỳ xpath nào đứng TRƯỚC nhóm NavigationButton mà
        // khớp một node của màn nền (Stories / Privacy / Public / //android.widget.ProgressBar …)
        // đều THẮNG. Tool bấm vào node nền đó, dialog nuốt cú chạm, vòng lặp quay lại và KHÔNG BAO
        // GIỜ tới lượt xpath Dismiss -> biểu hiện "đã thêm Dismiss rồi mà vẫn kẹt".
        //
        // HỆ QUẢ QUAN TRỌNG: KHÔNG THỂ sửa bằng cách dời Dismiss XUỐNG cạnh Save/Skip trong list.
        // Dời xuống = đứng SAU nhiều xpath hơn = ưu tiên THẤP hơn = càng khó được chọn. Save/Skip
        // bấm được chỉ vì trên những màn đó không có xpath nào đứng trước chúng khớp — không phải
        // vì vị trí của chúng "tốt".
        //
        // CÁCH SỬA: quét Dismiss TRƯỚC, độc lập hoàn toàn với thứ tự list, ở đầu mỗi vòng lặp.
        //
        // CHỌN NODE: ưu tiên node CLICKABLE, và trong số đó lấy node NHỎ NHẤT — nút thật luôn nhỏ
        // hơn thẻ/dialog chứa nó. CỐ Ý KHÔNG dùng luật "diện tích lớn nhất" của FindCenterByXpaths:
        // chính luật đó từng chọn ĐOẠN VĂN content-desc="By selecting Agree…" (area 184592,
        // clickable=false) THAY VÌ nút Button "Agree" (area 55776) và gây kẹt consent — xem ghi chú
        // ở MetaContinueExactXpaths bên dưới.
        public static readonly List<string> DismissAnyXpaths = new List<string>
        {
            // Dạng exact trước: engine có fallback KHÔNG phân biệt hoa/thường cho dạng
            // [@attr='value'] (TryFindBounds -> TryFindBoundsCaseInsensitive), nên khớp cả
            // "DISMISS"/"dismiss". Dạng contains() thì chỉ case-sensitive -> xếp sau làm lưới an toàn.
            "//*[@text=\"Dismiss\"]",
            "//*[@content-desc=\"Dismiss\"]",
            "//*[contains(@content-desc, \"Dismiss\")]",
            "//*[contains(@text, \"Dismiss\")]",
        };

        /// <summary>
        /// Bấm nút Dismiss nếu màn hiện tại có, KHÔNG phụ thuộc thứ tự list xpath.
        /// Gọi ở ĐẦU mỗi vòng lặp nhận diện, trước <c>FindElement</c>.
        /// Hỗ trợ cả 2 cơ chế:
        /// 1. Quét XPath (nếu Facebook xuất node Dismiss trong XML).
        /// 2. Nhận diện hình ảnh (nếu Facebook giấu node XML nhưng màn hình có cảnh báo "automated behavior" hoặc có chữ Dismiss).
        /// Trả về true nếu đã tìm thấy và bấm.
        /// </summary>
        /// <param name="xml">XML đã dump sẵn (truyền vào để KHÔNG tốn thêm một lần dump).</param>
        public static bool TryClickAnyDismiss(ADBClient client, string xml = "")
        {
            try
            {
                if (client == null) return false;
                if (string.IsNullOrEmpty(xml)) xml = client.GetXMLSource();
                if (string.IsNullOrEmpty(xml)) return false;

                bool hasDismiss = xml.IndexOf("Dismiss", StringComparison.OrdinalIgnoreCase) >= 0;
                bool hasAutomated = xml.IndexOf("automated behavior", StringComparison.OrdinalIgnoreCase) >= 0
                    || xml.IndexOf("suspect automated", StringComparison.OrdinalIgnoreCase) >= 0;

                // Nếu màn hình không hề có chữ Dismiss lẫn cảnh báo automated behavior -> thoát ngay,
                // không tốn công dựng XPathDocument hay chụp ảnh màn hình.
                if (!hasDismiss && !hasAutomated) return false;

                // LỚP 1: Quét XPath chuẩn nếu XML có chữ Dismiss
                if (hasDismiss)
                {
                    string hit = client.FindElement(xml, DismissAnyXpaths, 0);
                    if (!string.IsNullOrEmpty(hit))
                    {
                        System.Drawing.Point? center = FindSmallestClickableCenter(client, xml, hit);
                        if (center != null)
                        {
                            client.Click(center.Value.X, center.Value.Y);
                            return true;
                        }

                        if (client.ElementWithAttributes(hit, 1, xml, true))
                        {
                            return true;
                        }
                    }
                }

                // LỚP 2: CỨU NGUY BẰNG NHẬN DIỆN HÌNH ẢNH
                // Áp dụng khi:
                // - Màn hình có dialog "automated behavior" nhưng Facebook giấu hoàn toàn node nút bấm trong XML.
                // - Hoặc XML có chữ Dismiss nhưng XPath không lấy được tọa độ bấm.
                System.Drawing.Point? visualCenter = FindFacebookDismissButtonByImage(client);
                if (visualCenter != null)
                {
                    client?.LogHelper?.Log($"[Dismiss] Phát hiện nút xanh Dismiss bằng hình ảnh tại ({visualCenter.Value.X},{visualCenter.Value.Y}), thực hiện tap");
                    client.Click(visualCenter.Value.X, visualCenter.Value.Y);
                    return true;
                }

                return false;
            }
            catch
            {
                // Không bao giờ để việc quét Dismiss làm hỏng luồng chính.
                return false;
            }
        }

        /// <summary>
        /// Tâm của node NHỎ NHẤT trong số các node khớp xpath, ƯU TIÊN node clickable="true".
        /// Đọc bounds và clickable qua HAI lệnh gọi cùng SelectNodesWithCandidates để cùng thứ tự
        /// node (KHÔNG dùng GetBoundsValues: nó trả list.Distinct() nên chỉ số lệch khỏi list
        /// clickable -> ghép cặp sai node).
        /// </summary>
        private static System.Drawing.Point? FindSmallestClickableCenter(ADBClient client, string xml, string xpath)
        {
            try
            {
                List<string> boundsList = client.GetAttributeValuesFromXmlNodes(xml, xpath, "bounds");
                if (boundsList == null || boundsList.Count == 0) return null;
                List<string> clickList = client.GetAttributeValuesFromXmlNodes(xml, xpath, "clickable");
                bool aligned = clickList != null && clickList.Count == boundsList.Count;

                long bestArea = long.MaxValue;
                System.Drawing.Point? best = null;        // nhỏ nhất, không phân biệt clickable
                long bestClkArea = long.MaxValue;
                System.Drawing.Point? bestClk = null;     // nhỏ nhất trong nhóm clickable="true"

                for (int i = 0; i < boundsList.Count; i++)
                {
                    string bounds = boundsList[i];
                    if (string.IsNullOrEmpty(bounds)) continue;
                    var rect = new RectangleArea(bounds);
                    long w = rect.Right - rect.Left;
                    long h = rect.Bottom - rect.Top;
                    if (w <= 1 || h <= 1) continue;       // bỏ node suy biến 0px/1px (decoy)
                    long area = w * h;
                    System.Drawing.Point center = rect.GetCenterPoint();
                    if (area < bestArea)
                    {
                        bestArea = area;
                        best = center;
                    }
                    if (aligned && area < bestClkArea
                        && string.Equals(clickList[i], "true", StringComparison.OrdinalIgnoreCase))
                    {
                        bestClkArea = area;
                        bestClk = center;
                    }
                }

                if (bestClk != null) return bestClk;
                return best;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Nhận diện nút Dismiss màu xanh Facebook (#0866FF) trên màn hình bằng phân tích ảnh.
        /// Chụp màn hình qua client.Screenshot(), khóa bộ nhớ pixel và quét tìm khối chữ nhật màu xanh đặc trưng.
        /// Chỉ quét vùng nửa trên màn hình (Y từ 10% đến 45%) nơi dialog cảnh báo xuất hiện.
        /// </summary>
        private static System.Drawing.Point? FindFacebookDismissButtonByImage(ADBClient client)
        {
            try
            {
                using var screen = client.Screenshot();
                if (screen == null || screen.Width < 100 || screen.Height < 100) return null;

                int w = screen.Width;
                int h = screen.Height;

                int yMin = (int)(h * 0.10);
                int yMax = (int)(h * 0.45);
                int stepY = 2;
                int stepX = 4;

                var rect = new Rectangle(0, 0, w, h);
                var data = screen.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                try
                {
                    int stride = Math.Abs(data.Stride);
                    byte[] buffer = new byte[stride * h];
                    Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);

                    var denseRows = new List<int>();
                    int requiredBlueInRow = (int)((w * 0.35) / stepX);

                    for (int y = yMin; y < yMax; y += stepY)
                    {
                        int rowOffset = y * stride;
                        int blueCount = 0;
                        for (int x = (int)(w * 0.05); x < (int)(w * 0.95); x += stepX)
                        {
                            int pxOffset = rowOffset + (x * 4);
                            byte b = buffer[pxOffset];
                            byte g = buffer[pxOffset + 1];
                            byte r = buffer[pxOffset + 2];

                            // Màu xanh Facebook (#0866FF): R < 50, 75 <= G <= 155, B >= 200
                            if (r < 50 && g >= 75 && g <= 155 && b >= 200)
                            {
                                blueCount++;
                            }
                        }

                        if (blueCount >= requiredBlueInRow)
                        {
                            denseRows.Add(y);
                        }
                    }

                    if (denseRows.Count == 0) return null;

                    var chunks = new List<List<int>>();
                    var curChunk = new List<int> { denseRows[0] };
                    for (int i = 1; i < denseRows.Count; i++)
                    {
                        if (denseRows[i] - curChunk[curChunk.Count - 1] <= stepY * 2)
                        {
                            curChunk.Add(denseRows[i]);
                        }
                        else
                        {
                            chunks.Add(curChunk);
                            curChunk = new List<int> { denseRows[i] };
                        }
                    }
                    if (curChunk.Count > 0) chunks.Add(curChunk);

                    var validChunks = chunks.Where(c => (c[c.Count - 1] - c[0]) >= 35 && (c[c.Count - 1] - c[0]) <= 250).ToList();
                    if (validChunks.Count == 0) return null;

                    var bestChunk = validChunks.OrderByDescending(c => c.Count).First();
                    int topY = bestChunk[0];
                    int botY = bestChunk[bestChunk.Count - 1];

                    int minX = int.MaxValue;
                    int maxX = int.MinValue;
                    for (int i = 0; i < bestChunk.Count; i += 2)
                    {
                        int y = bestChunk[i];
                        int rowOffset = y * stride;
                        for (int x = 0; x < w; x += stepX)
                        {
                            int pxOffset = rowOffset + (x * 4);
                            byte b = buffer[pxOffset];
                            byte g = buffer[pxOffset + 1];
                            byte r = buffer[pxOffset + 2];
                            if (r < 50 && g >= 75 && g <= 155 && b >= 200)
                            {
                                if (x < minX) minX = x;
                                if (x > maxX) maxX = x;
                            }
                        }
                    }

                    int btnWidth = maxX - minX;
                    if (btnWidth < (int)(w * 0.40)) return null;

                    int cx = (minX + maxX) / 2;
                    int cy = (topY + botY) / 2;

                    return new System.Drawing.Point(cx, cy);
                }
                finally
                {
                    screen.UnlockBits(data);
                }
            }
            catch
            {
                return null;
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Hộp thoại Meta "pay or consent" (EU):
        //   com.facebook.katana/...consent.bloks.katana.ConsentFlowHostActivity
        // ─────────────────────────────────────────────────────────────────────────
        // CÁCH VƯỢT (đã xác minh TRỰC TIẾP trên máy 52006f60f4be7475, screenshot + XML):
        //   BƯỚC 1 — TÍCH vào thẻ "Use for free with ads" bằng HOLD-TAP (nhấn-giữ ~200ms).
        //   BƯỚC 2 — CUỘN danh sách consent (RecyclerView) xuống ĐÁY để nút Continue SÁNG lên.
        //   BƯỚC 3 — BẤM nút Continue THẬT (Button, content-desc="Continue").
        //
        // HAI BẪY đã khiến code cũ KẸT MÃI (sửa ở dưới):
        //   (a) CHỌN OPTION: node nhãn nhỏ text="Use for free with ads" [116,1470][612,1539]
        //       là View KHÔNG clickable (tâm ~364,1504) — tap tức thì vào đó KHÔNG chọn được
        //       radio. Phải tap vào THẺ lớn: node cha content-desc="Use for free with ads . …"
        //       [56,1409][1384,1908] (tâm ~720,1658) trùng vùng thẻ clickable [56,1416][1384,1908].
        //       => khớp theo CONTENT-DESC (lấy thẻ) chứ KHÔNG theo TEXT (lấy nhãn), và dùng
        //          LongClick (input swipe cùng điểm ~200ms) chứ KHÔNG Click tức thì.
        //   (b) NÚT CONTINUE: có node View SUY BIẾN 1px text="Continue" [610,2391][830,2392]
        //       đứng SAU nút Button thật content-desc="Continue" [56,2350][1384,2392] (tâm ~720,2371).
        //       Khớp @text="Continue" TRƯỚC sẽ bấm node 1px -> vô tác dụng.
        //       => khớp theo CONTENT-DESC TRƯỚC TEXT. (NavigationButton đã xếp content-desc trước
        //          nên bước 2 "How your information is used" vốn đã bấm đúng nút.)
        //
        // Continue chỉ bật sau khi cuộn hết nội dung, nên bước 2 là bắt buộc. Trả về true nếu
        // nhận diện đúng màn này và đã chạy chuỗi xử lý (kể cả khi chưa qua kịp — vòng lặp login
        // gọi lại sẽ bấm tiếp); false nếu không phải màn này.
        //
        // Xpath chọn THẺ option miễn phí — content-desc TRƯỚC (lấy thẻ lớn, không lấy nhãn nhỏ).
        private static readonly List<string> MetaFreeOptionXpaths = new List<string>
        {
            "//*[contains(@content-desc, \"Use for free with\")]",
            "//*[contains(@content-desc, \"Use free of charge with ads\")]",
            "//*[contains(@content-desc, \"Use for free with ads\")]",
            // Biến thể @text làm LƯỚI AN TOÀN: một số biến thể/locale chỉ đặt nhãn ở @text, khi đó
            // cả 3 xpath content-desc trên trượt hết -> FindCenterByXpaths trả null -> không chọn
            // được radio -> wrapper footer mãi enabled="false" -> KẸT.
            // Bắt buộc phải thêm ở đây vì FindCenterByXpaths -> GetBoundsValues ->
            // SelectNodesWithCandidates CHỈ có ExpandXPathCandidates, KHÔNG có
            // TryFindBoundsCaseInsensitive như đường ElementWithAttributes: thiếu @text là mất
            // trắng chứ không được engine bù hộ.
            // Thứ tự không quyết định kết quả: FindCenterByXpaths chọn DIỆN TÍCH LỚN NHẤT nên THẺ
            // lớn (area 845936) vẫn thắng nhãn nhỏ (area 44988) — đúng yêu cầu ở ghi chú trên.
            "//*[contains(@text, \"Use for free with\")]",
            "//*[contains(@text, \"Use free of charge with ads\")]",
            "//*[contains(@text, \"Use for free with ads\")]",
        };

        // Xpath nút Continue/Agree THẬT, BẬC 1 = khớp CHÍNH XÁC.
        //
        // Continue = bước 1 (pay-or-consent), Agree = bước 2 ("…agree to Meta using your info…").
        // Hai nút KHÔNG bao giờ cùng hiện; cả hai đều là footer cố định -> FindCenterByXpaths
        // (chọn diện tích lớn nhất) bấm đúng nút thật, loại node View decoy (area ~144).
        //
        // TẠI SAO PHẢI TÁCH BẬC 1 / BẬC 2 — LỖI KẸT THẬT SỰ, tìm ra 2026-09-08 khi mô phỏng lại
        // dump sống (consent_stuck/, device 52003dc05fab94ab):
        //   màn BƯỚC 2 (after_cont.xml) có node View MÔ TẢ
        //     content-desc="By selecting Agree, you consent to Meta processing your data for ads."
        //     bounds=[56,1144][1384,1283]  area=184592  clickable=FALSE
        //   còn nút Agree THẬT là
        //     Button content-desc="Agree"  bounds=[56,2350][1384,2392]  area=55776  clickable=TRUE
        //   Khi contains(@content-desc,"Agree") nằm CHUNG danh sách với nút thật, luật "diện tích
        //   lớn nhất" chọn ĐOẠN VĂN 184592 thay vì nút 55776 -> bấm vào chữ (clickable=false) ->
        //   không có tác dụng gì -> đốt hết 4 lượt rồi `return true` -> ĐỨNG IM = đúng triệu chứng
        //   KẸT đã báo. Lỗi này KHÔNG phụ thuộc độ phân giải hay ngôn ngữ máy (đoạn văn luôn to
        //   hơn nút), nên nó giải thích vì sao máy ở nơi khác kẹt.
        // Bậc 1 chỉ khớp CHÍNH XÁC nên đoạn văn không bao giờ lọt vào.
        private static readonly List<string> MetaContinueExactXpaths = new List<string>
        {
            "//*[@content-desc=\"Continue\"]",
            "//*[@content-desc=\"Agree\"]",
            // ── Màn BƯỚC 3 ("Your current experience", xác minh LIVE 2026-09-08): nút cần bấm là
            // OK footer [56,2336][1384,2392] area=74368, CHỈ mang content-desc (decoy View
            // [681,2377][759,2392] area=1170 mang cả text lẫn content-desc) -> @content-desc đặt
            // TRƯỚC @text, và largest-area chọn nút thật -> tâm (720,2364).
            "//*[@content-desc=\"OK\"]",
            "//*[@text=\"OK\"]",
        };

        // BẬC 2 = khớp CONTAINS, CHỈ chạy khi bậc 1 trắng hoàn toàn (biến thể nhãn dài kiểu
        // "Continue to Facebook", hoặc bản dịch có tiền tố/hậu tố). Giữ lại làm lưới an toàn,
        // nhưng bắt buộc phải đứng SAU bậc 1: chính nhóm contains này là thứ khớp nhầm đoạn văn
        // "By selecting Agree, you consent to Meta…" ở màn BƯỚC 2 (xem ghi chú trên).
        // Khi buộc phải dùng bậc 2, FindCenterByXpaths được gọi với preferClickable=true để ưu
        // tiên node clickable="true" — đoạn văn mô tả luôn clickable="false" nên bị loại.
        private static readonly List<string> MetaContinueLooseXpaths = new List<string>
        {
            "//*[contains(@content-desc, \"Continue\")]",
            "//*[contains(@content-desc, \"Agree\")]",
            "//*[contains(@content-desc, \"OK\")]",
            "//*[contains(@text, \"Continue\")]",
            "//*[contains(@text, \"Agree\")]",
        };

        // Nút cần bấm RIÊNG cho màn BƯỚC 3 ("Your current experience"). Tách khỏi
        // MetaContinueExactXpaths vì FindCenterByXpaths chọn DIỆN TÍCH LỚN NHẤT: trên màn đó
        // "Continue with personalized ads" (area 169984) to hơn OK footer (area 74368) nên danh
        // sách chung sẽ bấm nút LỰA CHỌN trong RecyclerView — nút đó CHƯA được xác minh là đóng
        // màn, còn OK footer ĐÃ xác minh LIVE 2026-09-08 (device 52003dc05fab94ab) đóng màn và
        // vào thẳng news feed. Nếu bấm nhầm nút không đóng màn, vòng lặp đốt hết 4 lần rồi vẫn
        // `return true` -> đúng triệu chứng KẸT đã báo.
        // @content-desc TRƯỚC @text: Button thật [56,2336][1384,2392] area=74368 chỉ mang
        // content-desc, còn decoy View [681,2377][759,2392] area=1170 mang cả hai. Largest-area
        // chọn Button thật -> tâm (720,2364), CHÍNH XÁC HƠN toạ độ (720,2384) mà NavigationButton
        // suy ra từ decoy (trước đây qua được màn này chỉ nhờ tâm decoy tình cờ lọt vào bounds
        // nút thật — ăn may về hình học).
        private static readonly List<string> MetaExperienceOkXpaths = new List<string>
        {
            "//*[@content-desc=\"OK\"]",
            "//*[@text=\"OK\"]",
        };

        // Nhãn nhận diện màn BƯỚC 3, dò thẳng trong XML (không cần xpath).
        //
        // PHẢI là CHUỖI DÀI ĐẶC HIỆU, không được dùng marker ngắn. Đã đếm trực tiếp trên 5 dump
        // sống (consent_stuck/, device 52003dc05fab94ab, 2026-09-08):
        //   "personalized ads" : MAN1=8  MAN2=3  MAN3=12   -> DÙNG SAI, khớp MAN1/MAN2
        //   "ad experience"    : MAN1=0  MAN2=3  MAN3=7    -> DÙNG SAI, khớp MAN2
        //   "Your current experience"        : 0/0/0/2  -> đúng, CHỈ MAN3
        //   "You can manage your ad experience" : 0/0/0/3 -> đúng, CHỈ MAN3
        //   "Continue with personalized ads"    : 0/0/0/3 -> đúng, CHỈ MAN3
        // Nếu dò sai (false positive) màn này sẽ dùng danh sách OK-only -> không thấy OK -> phải
        // nhờ fallback quay lại MetaContinueExactXpaths: vẫn chạy được nhưng thừa một vòng và mất đi
        // đúng cái lý do tách danh sách (tránh bấm "Continue with personalized ads" area 169984
        // lớn hơn OK footer area 74368).
        //
        // Ghi CẢ hai chính tả personalized/personalised vì Meta dùng Anh-Mỹ hoặc Anh-Anh tuỳ tài
        // khoản/IP (màn này từng hiện "PLN39.99/month" — Meta bản địa hoá theo QUỐC GIA TÀI KHOẢN,
        // KHÔNG theo locale điện thoại, nên không suy ra được từ cài đặt máy).
        // Bản dịch NGÔN NGỮ KHÁC (vd Ba Lan) sẽ không khớp hàm này — khi đó bậc 2/3 vẫn tự trả
        // về đúng nút OK, vì màn đó không có node nào content-desc chứa "Continue" to hơn.
        private static bool IsMetaExperienceScreen(string xml)
        {
            if (string.IsNullOrEmpty(xml)) return false;
            return xml.IndexOf("Your current experience", StringComparison.OrdinalIgnoreCase) >= 0
                || xml.IndexOf("You can manage your ad experience", StringComparison.OrdinalIgnoreCase) >= 0
                || xml.IndexOf("Continue with personalized ads", StringComparison.OrdinalIgnoreCase) >= 0
                || xml.IndexOf("Continue with personalised ads", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // Lấy tâm (center) của node khớp bất kỳ xpath nào trong danh sách, từ xml đã dump.
        // Chọn bounds lớn nhất (thay vì first-match) để robust theo CẤU TRÚC, không phụ thuộc
        // thứ tự document — loại cả node View 1px text="Continue" lẫn node nhãn nhỏ (cả hai đều
        // khớp contains(@content-desc,...) nhưng là phần tử con nhỏ bên trong thẻ/nút thật).
        //
        // preferClickable=true: ƯU TIÊN node clickable="true" (vẫn chọn diện tích lớn nhất trong
        // nhóm đó); chỉ khi KHÔNG có node nào clickable mới hạ xuống chọn diện tích lớn nhất trong
        // toàn bộ. Đây là lưới chặn ĐOẠN VĂN MÔ TẢ: trên màn "Agree", node View
        // content-desc="By selecting Agree, you consent to Meta…" có area=184592 nhưng
        // clickable=FALSE, to gấp 3 lần nút Agree thật (55776, clickable=TRUE) — nếu chỉ so diện
        // tích thì bấm vào đoạn văn, không có tác dụng, và tool KẸT.
        //
        // PHẢI là ƯU TIÊN chứ không phải BỘ LỌC CỨNG: MetaFreeOptionXpaths ở BƯỚC 1 cố tình nhắm
        // vào THẺ option (ViewGroup area=845936, clickable=FALSE) rồi LongClick — nếu loại hết node
        // không clickable thì bước 1 không còn gì để bấm. Vì thế tham số mặc định là false và chỉ
        // đường tìm NÚT footer mới bật true.
        private static System.Drawing.Point? FindCenterByXpaths(ADBClient client, string xml, List<string> xpaths, bool preferClickable = false)
        {
            if (string.IsNullOrEmpty(xml)) return null;
            long bestArea = 0;
            System.Drawing.Point? best = null;          // lớn nhất, không phân biệt clickable
            long bestClkArea = 0;
            System.Drawing.Point? bestClk = null;       // lớn nhất trong nhóm clickable="true"
            foreach (var xpath in xpaths)
            {
                try
                {
                    // Đọc bounds VÀ clickable qua HAI lệnh gọi cùng SelectNodesWithCandidates ->
                    // cùng thứ tự node. KHÔNG dùng GetBoundsValues ở đây: nó trả list.Distinct()
                    // nên chỉ số lệch khỏi list clickable -> ghép cặp sai node.
                    var boundsList = client.GetAttributeValuesFromXmlNodes(xml, xpath, "bounds");
                    if (boundsList == null || boundsList.Count == 0) continue;
                    var clickList = client.GetAttributeValuesFromXmlNodes(xml, xpath, "clickable");
                    // Node thiếu attribute clickable thì hai list lệch số phần tử -> bỏ ưu tiên,
                    // giữ đúng hành vi cũ (so diện tích) thay vì ghép cặp sai.
                    bool aligned = clickList != null && clickList.Count == boundsList.Count;

                    for (int i = 0; i < boundsList.Count; i++)
                    {
                        var bounds = boundsList[i];
                        if (string.IsNullOrEmpty(bounds)) continue;
                        var rect = new RectangleArea(bounds);
                        long area = (long)(rect.Right - rect.Left) * (rect.Bottom - rect.Top);
                        var center = rect.GetCenterPoint();
                        if (area > bestArea)
                        {
                            bestArea = area;
                            best = center;
                        }
                        if (aligned && area > bestClkArea
                            && string.Equals(clickList[i], "true", StringComparison.OrdinalIgnoreCase))
                        {
                            bestClkArea = area;
                            bestClk = center;
                        }
                    }
                }
                catch { /* xpath không hợp lệ -> thử xpath kế */ }
            }
            if (preferClickable && bestClk != null) return bestClk;
            return best;
        }

        // ── BIẾN THỂ consent MỚI (xác minh LIVE 2026-09-07, device 52006f60f4be7475): ──
        // Màn "Want to subscribe or continue using our products free of charge with ads?" hiển thị
        // HAI thẻ radio: "Subscribe to use without ads" và "Use free of charge with ads". KHÁC biến
        // thể cũ ("Use for free with ads" — thẻ clickable, chọn bằng HOLD-TAP vào tâm thẻ):
        //   • Thẻ ở đây là ViewGroup clickable="FALSE"; content-desc của THẺ chứa chuỗi
        //     "… . Radio button . Unselected" (chuyển thành "Selected" sau khi tích).
        //   • NÚT TRÒN (radio circle) KHÔNG có content-desc/text riêng -> KHÔNG xpath theo chữ được.
        //     Nó là ViewGroup rỗng ~70x70 (bọc vòng ~56x56) nằm mé PHẢI thẻ, thẳng hàng DỌC với
        //     node tiêu đề (content-desc == đúng nhãn option).
        //   • Chọn radio bằng TAP THƯỜNG vào tâm nút tròn (1289,1642) — HOLD-TAP KHÔNG cần.
        //   • Continue ở biến thể này enabled=true NGAY từ đầu (không phải cuộn mới bật như biến thể cũ).
        // Trả về tâm nút tròn của option mong muốn, hoặc null nếu không nhận diện được cấu trúc này
        // (-> caller fallback sang HOLD-TAP tâm thẻ của biến thể cũ).
        private static System.Drawing.Point? FindMetaRadioCircleCenter(ADBClient client, string xml, List<string> targetLabels)
        {
            if (string.IsNullOrEmpty(xml)) return null;
            try
            {
                var doc = new System.Xml.XmlDocument();
                doc.LoadXml(xml);
                var all = doc.SelectNodes("//node");
                if (all == null) return null;

                // Gom node: bounds + content-desc + text (đã trim), kèm kích thước màn hình suy ra
                // từ bounds lớn nhất (chính là root/window — trên máy đã xác minh là [0,0][1440,2392]).
                // Nhãn option có thể nằm ở content-desc (đa số UI Bloks) HOẶC text; một số biến
                // thể/locale chỉ đặt ở một trong hai nên phải đọc cả hai.
                var nodes = new List<(int l, int t, int r, int b, string cd, string tx)>();
                int maxR = 0, maxB = 0;
                foreach (System.Xml.XmlNode n in all)
                {
                    var ba = n.Attributes?["bounds"]?.Value;
                    if (string.IsNullOrEmpty(ba)) continue;
                    var rect = new RectangleArea(ba);
                    var cd = (n.Attributes?["content-desc"]?.Value ?? string.Empty).Trim();
                    var tx = (n.Attributes?["text"]?.Value ?? string.Empty).Trim();
                    nodes.Add((rect.Left, rect.Top, rect.Right, rect.Bottom, cd, tx));
                    if (rect.Right > maxR) maxR = rect.Right;
                    if (rect.Bottom > maxB) maxB = rect.Bottom;
                }
                if (maxR <= 0 || maxB <= 0) return null;

                // Ngưỡng kích thước nút tròn tính THEO TỈ LỆ màn hình, KHÔNG hardcode px.
                // Máy đã xác minh (52003dc05fab94ab): màn 1440x2392, density 560, vòng ngoài 70x70,
                // vòng trong 56x56. Khoảng cứng 40..110px cũ LOẠI SẠCH nút tròn trên máy density
                // thấp (vd density 280 -> vòng ~35px) -> hàm trả null -> fallback hold-tap tâm thẻ,
                // mà biến thể MỚI có ViewGroup clickable="false" nên hold-tap KHÔNG chọn được radio
                // -> wrapper footer mãi enabled="false" -> bấm Continue vô tác dụng -> KẸT.
                // 2%..9% chiều rộng = 28..129px trên màn 1440 (phủ cả 70 lẫn 56) và 14..64px trên
                // màn 720. Các kẹp dưới/trần (Math.Max) giữ hành vi cũ trên màn lớn.
                int radioMin = Math.Max(20, maxR * 2 / 100);
                int radioMax = Math.Max(60, maxR * 9 / 100);
                int skewMax = Math.Max(12, radioMax / 5);   // |w-h| tối đa: màn 1440 -> 25 (như cũ)
                int rowTol = Math.Max(24, maxB * 3 / 100);  // dung sai thẳng hàng dọc: 2392 -> 71

                foreach (var target in targetLabels)
                {
                    // (a) NODE TIÊU ĐỀ: nhãn == đúng target (trim). Lấy node DIỆN TÍCH NHỎ NHẤT
                    //     để tránh khớp nhầm THẺ (thẻ cũng chứa nhãn nhưng kèm " . Radio button …").
                    //     So KHÔNG phân biệt hoa/thường và chấp nhận cả @text: engine xpath có
                    //     fallback hoa/thường (TryFindBoundsCaseInsensitive) nhưng hàm này tự đối
                    //     chiếu chuỗi nên phải tự làm, không được engine bù hộ.
                    (int l, int t, int r, int b)? titleBox = null;
                    long titleArea = long.MaxValue;
                    foreach (var nd in nodes)
                    {
                        if (!string.Equals(nd.cd, target, StringComparison.OrdinalIgnoreCase)
                            && !string.Equals(nd.tx, target, StringComparison.OrdinalIgnoreCase)) continue;
                        long a = (long)(nd.r - nd.l) * (nd.b - nd.t);
                        if (a > 0 && a < titleArea) { titleArea = a; titleBox = (nd.l, nd.t, nd.r, nd.b); }
                    }
                    if (titleBox == null) continue;
                    var tb = titleBox.Value;
                    int titleCy = (tb.t + tb.b) / 2;

                    // (b) THẺ: node lớn nhất chứa cả nhãn VÀ "Radio button".
                    (int l, int t, int r, int b)? cardBox = null;
                    long cardArea = 0;
                    foreach (var nd in nodes)
                    {
                        // Chuỗi "… . Radio button . Unselected/Selected" Meta đặt ở content-desc;
                        // dò ở cả @text cho an toàn với biến thể khác.
                        bool hasLabel = nd.cd.IndexOf(target, StringComparison.OrdinalIgnoreCase) >= 0
                                     || nd.tx.IndexOf(target, StringComparison.OrdinalIgnoreCase) >= 0;
                        bool isRadio = nd.cd.IndexOf("Radio button", StringComparison.OrdinalIgnoreCase) >= 0
                                    || nd.tx.IndexOf("Radio button", StringComparison.OrdinalIgnoreCase) >= 0;
                        if (hasLabel && isRadio)
                        {
                            long a = (long)(nd.r - nd.l) * (nd.b - nd.t);
                            if (a > cardArea) { cardArea = a; cardBox = (nd.l, nd.t, nd.r, nd.b); }
                        }
                    }
                    var cb = cardBox ?? (tb.l, tb.t, tb.r, tb.b);

                    // (c) NÚT TRÒN: ViewGroup TRẦN (không content-desc VÀ không text), gần vuông
                    //     theo ngưỡng TỈ LỆ ở trên, tâm DỌC thẳng hàng tiêu đề (±rowTol), nằm TRONG
                    //     phạm vi ngang của thẻ.
                    //     Có 2 node lồng (vòng ngoài 70x70 + vòng trong 56x56) cùng tâm -> chọn DIỆN TÍCH
                    //     LỚN NHẤT (vòng ngoài) cho tâm ổn định; cả hai chung tâm nên kết quả như nhau.
                    System.Drawing.Point? best = null;
                    long bestArea = 0;
                    foreach (var nd in nodes)
                    {
                        if (nd.cd.Length != 0 || nd.tx.Length != 0) continue;
                        int w = nd.r - nd.l, h = nd.b - nd.t;
                        if (w < radioMin || w > radioMax || h < radioMin || h > radioMax
                            || Math.Abs(w - h) > skewMax) continue;
                        int cx = (nd.l + nd.r) / 2, cy = (nd.t + nd.b) / 2;
                        if (Math.Abs(cy - titleCy) > rowTol) continue;
                        if (nd.l < cb.l || nd.r > cb.r) continue;
                        long a = (long)w * h;
                        if (a > bestArea) { bestArea = a; best = new System.Drawing.Point(cx, cy); }
                    }
                    if (best != null) return best;
                }
            }
            catch { /* cấu trúc lạ -> trả null, caller fallback */ }
            return null;
        }

        // ─── NHẬN DIỆN BƯỚC 1 KHÔNG PHỤ THUỘC NGÔN NGỮ (2026-09-11) ────────────────────────
        // VÌ SAO CẦN: MỌI hàm nhận diện bước 1 ở trên đều so CHỮ TIẾNG ANH
        // ("Use free of charge with ads" / "Use for free with ads" / "Want to subscribe…").
        // Meta bản địa hoá màn này theo QUỐC GIA TÀI KHOẢN / IP — KHÔNG theo locale điện thoại
        // (đã thấy "€7.99/month" trên máy en_US, "PLN39.99" ở máy khác). Khi Meta trả bản dịch:
        //   IsMetaConsentPopup         -> false (7 marker đều tiếng Anh) => hook toàn cục KHÔNG chạy
        //   xpath MetaAdsConsent       -> false                         => gate 2 trượt
        //   FindMetaRadioCircleCenter  -> null , MetaFreeOptionXpaths -> null
        // => KHÔNG chọn được radio => wrapper footer mãi enabled="false" => Continue CHẾT
        // => 4 lượt bấm vô tác dụng => (trước đây) `return true` => KẸT IM LẶNG, không log.
        //
        // Các hàm dưới đây nhận diện bằng CẤU TRÚC / HÌNH HỌC, chỉ dựa vào những chuỗi
        // KHÔNG THỂ DỊCH:
        //   • `com.facebook.katana` / `.lite` — tên package, thuộc tính XML của uiautomator.
        //   • `enabled="false"`               — thuộc tính XML, không phải nhãn hiển thị.
        //   • "Radio button"                  — ROLE NAME do framework sinh ra, không phải chữ Meta dịch.
        //   • CHỮ SỐ trong thẻ trả phí        — giá tiền luôn chứa chữ số ở mọi ngôn ngữ.

        /// <summary>
        /// PRE-FILTER RẺ cho GATE 1 — chạy trên MỌI lần dump nên CHỈ được IndexOf, KHÔNG parse XML.
        /// Màn chọn radio consent có đúng 2 thẻ => "Radio button" xuất hiện >= 2 lần.
        /// </summary>
        internal static bool LooksLikeMetaRadioChoiceScreen(string xml)
        {
            if (string.IsNullOrEmpty(xml)) return false;
            int i1 = xml.IndexOf("Radio button", StringComparison.Ordinal);
            if (i1 < 0) return false;
            if (xml.IndexOf("Radio button", i1 + 12, StringComparison.Ordinal) < 0) return false;
            // CHỈ màn của Facebook mới được kích hoạt đường hình học — chặn false-positive từ
            // radio list của ứng dụng khác (Settings, form đăng ký…).
            return xml.IndexOf("com.facebook.katana", StringComparison.Ordinal) >= 0
                || xml.IndexOf("com.facebook.lite", StringComparison.Ordinal) >= 0;
        }

        // Node đã parse: bounds + content-desc + text + clickable/enabled. Dùng chung cho mọi
        // hàm hình học bên dưới (parse MỘT lần, tránh mỗi hàm tự LoadXml một lượt).
        private readonly struct UiNode
        {
            public readonly int L, T, R, B;
            public readonly string Cd, Tx;
            public readonly bool Clickable, Disabled;
            public UiNode(int l, int t, int r, int b, string cd, string tx, bool clickable, bool disabled)
            { L = l; T = t; R = r; B = b; Cd = cd; Tx = tx; Clickable = clickable; Disabled = disabled; }
            public int W => R - L;
            public int H => B - T;
            public long Area => (long)W * H;
            public int Cx => (L + R) / 2;
            public int Cy => (T + B) / 2;
            public bool Contains(int x, int y) => x >= L && x <= R && y >= T && y <= B;
            public bool ContainsRect(UiNode o) => o.L >= L && o.R <= R && o.T >= T && o.B <= B;
        }

        private static List<UiNode>? ParseUiNodes(string xml, out int maxR, out int maxB)
        {
            maxR = 0; maxB = 0;
            if (string.IsNullOrEmpty(xml)) return null;
            try
            {
                var doc = new System.Xml.XmlDocument();
                doc.LoadXml(xml);
                var all = doc.SelectNodes("//node");
                if (all == null) return null;
                var list = new List<UiNode>(all.Count);
                foreach (System.Xml.XmlNode n in all)
                {
                    var ba = n.Attributes?["bounds"]?.Value;
                    if (string.IsNullOrEmpty(ba)) continue;
                    var rect = new RectangleArea(ba);
                    list.Add(new UiNode(
                        rect.Left, rect.Top, rect.Right, rect.Bottom,
                        (n.Attributes?["content-desc"]?.Value ?? string.Empty).Trim(),
                        (n.Attributes?["text"]?.Value ?? string.Empty).Trim(),
                        string.Equals(n.Attributes?["clickable"]?.Value, "true", StringComparison.Ordinal),
                        string.Equals(n.Attributes?["enabled"]?.Value, "false", StringComparison.Ordinal)));
                    if (rect.Right > maxR) maxR = rect.Right;
                    if (rect.Bottom > maxB) maxB = rect.Bottom;
                }
                return list;
            }
            catch { return null; }
        }

        // LogHelper là INSTANCE của client -> bắt buộc gọi qua client (gọi tĩnh là CS0120).
        // Bọc try/catch: ghi log KHÔNG được phép làm hỏng luồng xử lý.
        private static void Log(ADBClient? client, string message)
        {
            try { client?.LogHelper?.Log(message); } catch { }
        }

        /// <summary>
        /// Tâm NÚT TRÒN của THẺ MIỄN PHÍ, suy ra THUẦN HÌNH HỌC — không cần bất kỳ nhãn tiếng Anh
        /// nào. Trả null khi KHÔNG ĐỦ bằng chứng (caller tuyệt đối không được đoán mò).
        ///
        /// Bằng chứng bắt buộc (mọi điều kiện đều locale-independent):
        ///  1. Package FB — thuộc tính XML của uiautomator, không dịch.
        ///  2. >= 2 NÚT TRÒN "RING+DOT": tâm CÓ >= 2 kích thước khác nhau (vòng ngoài 70x70 +
        ///     vòng trong 56x56 + chấm trong 42x42 cùng MỘT tâm — xác minh LIVE 2026-09-11 trên
        ///     520058f34d7c947b). ĐÂY là đặc điểm CHỈ radio button UI Bloks của Meta mới có;
        ///     chevron `>` cuối hàng "How to manage…" chỉ có MỘT kích thước 56x56 -> bị loại.
        ///  3. Mỗi nút tròn phải nằm TRONG MỘT THẺ option rộng (>= 50% màn) ở nửa PHẢI thẻ, và
        ///     thẻ có chiều cao <= 50% màn.
        ///  4. Các thẻ XẾP DỌC (không chồng lấn Y) và trùng trục X — layout radio list thật.
        ///  5. FOOTER rộng ở 25% đáy màn — bằng chứng đây là màn pay-or-consent chứ không phải
        ///     radio list thường trong Settings.
        ///
        /// Cách chọn THẺ MIỄN PHÍ: thẻ trả phí LUÔN in giá -> subtree chứa CHỮ SỐ; thẻ miễn phí
        /// không -> ưu tiên thẻ KHÔNG có chữ số (đúng ở mọi ngôn ngữ). Không phân biệt được bằng
        /// chữ số -> lấy THẺ THẤP HƠN: option miễn phí luôn nằm DƯỚI option subscribe
        /// (LIVE 2026-09-11, 520058f34d7c947b: nút tròn subscribe cy=1199, free cy=1726).
        /// </summary>
        internal static System.Drawing.Point? FindMetaFreeRadioCircleByGeometry(string xml)
        {
            if (string.IsNullOrEmpty(xml)) return null;
            // CHỈ xét màn của Facebook — tránh false-positive từ radio list của ứng dụng khác
            // (Settings, form đăng ký…). Package name là thuộc tính XML của uiautomator, không dịch.
            if (xml.IndexOf("com.facebook.katana", StringComparison.Ordinal) < 0
                && xml.IndexOf("com.facebook.lite", StringComparison.Ordinal) < 0) return null;

            var nodes = ParseUiNodes(xml, out int maxR, out int maxB);
            if (nodes == null || nodes.Count == 0 || maxR <= 0 || maxB <= 0) return null;

            // Ngưỡng TỈ LỆ, kế thừa đúng công thức của FindMetaRadioCircleCenter (không hardcode px:
            // máy density thấp có vòng chỉ ~35px, khoảng cứng 40..110 cũ loại sạch -> trả null -> KẸT).
            int radioMin = Math.Max(20, maxR * 2 / 100);
            int radioMax = Math.Max(60, maxR * 9 / 100);
            int skewMax = Math.Max(12, radioMax / 5);

            // (1) NÚT TRÒN TRẦN, gom theo TÂM -> giữ những tâm có >= 2 kích thước khác nhau
            //     (vòng ngoài + vòng trong + chấm trong).
            //     Kẻ giết false-positive: trên màn Agree, chevron `>` cuối hàng "How to manage
            //     whether you see personalized or less…" cũng là bare 56x56 ở nửa phải, nhưng
            //     chỉ MỘT kích thước -> bị loại. Radio button thật của Meta LUÔN lồng nhiều
            //     ViewGroup (70x70 bọc 56x56, có thêm 42x42 inner dot sau khi Selected) nên
            //     cùng tâm CÓ >= 2 kích thước khác nhau. (Xác minh LIVE 2026-09-11 trên cả 4 dump
            //     của 520058f34d7c947b: consent_now có 2 ring-dot centers, s2/s3/s4 có 0.)
            var sizeByCenter = new Dictionary<(int, int), HashSet<(int, int)>>();
            foreach (var nd in nodes)
            {
                if (nd.Cd.Length != 0 || nd.Tx.Length != 0) continue;
                if (nd.W < radioMin || nd.W > radioMax) continue;
                if (nd.H < radioMin || nd.H > radioMax) continue;
                if (Math.Abs(nd.W - nd.H) > skewMax) continue;
                var k = (nd.Cx, nd.Cy);
                if (!sizeByCenter.TryGetValue(k, out var set))
                { set = new HashSet<(int, int)>(); sizeByCenter[k] = set; }
                set.Add((nd.W, nd.H));
            }
            var centers = new List<(int cx, int cy, UiNode largest)>();
            foreach (var kv in sizeByCenter)
            {
                if (kv.Value.Count < 2) continue;                       // CHỈ ring+dot
                // Giữ node lớn nhất ở tâm đó (tâm ổn định nhất để so sánh).
                UiNode? biggest = null;
                foreach (var nd in nodes)
                {
                    if (nd.Cd.Length != 0 || nd.Tx.Length != 0) continue;
                    if (nd.Cx != kv.Key.Item1 || nd.Cy != kv.Key.Item2) continue;
                    if (nd.W < radioMin || nd.W > radioMax || nd.H < radioMin || nd.H > radioMax) continue;
                    if (biggest == null || nd.Area > biggest.Value.Area) biggest = nd;
                }
                if (biggest == null) continue;
                centers.Add((kv.Key.Item1, kv.Key.Item2, biggest.Value));
            }
            if (centers.Count < 2) return null;

            // (2) THẺ = node RỘNG NHỎ NHẤT chứa trọn nút tròn, với nút tròn ở NỬA PHẢI thẻ.
            //     Lấy NHỎ NHẤT để không nhận nhầm khung cha chứa cả hai thẻ.
            var cards = new List<(UiNode card, int cx, int cy)>();
            foreach (var (cx, cy, c) in centers)
            {
                UiNode? best = null;
                foreach (var nd in nodes)
                {
                    if (nd.Area <= c.Area) continue;                 // phải lớn hơn nút tròn
                    if (nd.W < maxR / 2) continue;                   // thẻ rộng >= 50% màn
                    if (nd.H > maxB / 2) continue;                   // loại khung cha / toàn màn
                    if (nd.L > cx || nd.R < cx || nd.T > cy || nd.B < cy) continue; // chứa tâm
                    if (cx < nd.L + nd.W / 2) continue;              // nút tròn phải ở nửa PHẢI
                    if (best == null || nd.Area < best.Value.Area) best = nd;
                }
                if (best == null) continue;
                cards.Add((best.Value, cx, cy));
            }
            if (cards.Count < 2) return null;

            // (3) Các thẻ XẾP DỌC, không chồng lấn Y, và trùng trục X.
            cards.Sort((a, b) => a.card.T.CompareTo(b.card.T));
            var stacked = new List<(UiNode card, int cx, int cy)>();
            foreach (var c in cards)
            {
                if (stacked.Count == 0) { stacked.Add(c); continue; }
                var prev = stacked[stacked.Count - 1];
                bool yOverlap = c.card.T < prev.card.B && prev.card.T < c.card.B;
                bool xOverlap = c.card.L < prev.card.R && prev.card.L < c.card.R;
                if (!yOverlap && xOverlap) stacked.Add(c);
            }
            if (stacked.Count < 2) return null;

            // (4) FOOTER ở đáy màn — bằng chứng đây là màn pay-or-consent chứ không phải form thường.
            bool hasFooter = false;
            foreach (var nd in nodes)
            {
                if (nd.W < maxR / 2) continue;
                if (nd.H > maxB / 4) continue;
                if (nd.Cy < maxB * 3 / 4) continue;
                hasFooter = true; break;
            }
            if (!hasFooter) return null;

            // (5) Chọn THẺ MIỄN PHÍ: bỏ thẻ có CHỮ SỐ (giá tiền), rồi lấy thẻ THẤP HƠN.
            (UiNode card, int cx, int cy)? target = null;
            foreach (var c in stacked)
            {
                if (CardHasDigit(nodes, c.card)) continue;
                if (target == null || c.card.T > target.Value.card.T) target = c;
            }
            if (target == null) target = stacked[stacked.Count - 1];  // lưới: thẻ thấp nhất
            return new System.Drawing.Point(target.Value.cx, target.Value.cy);
        }

        /// <summary>
        /// Tâm NÚT FOOTER (Continue / Agree / OK) suy ra HÌNH HỌC — không cần nhãn.
        /// Nút footer của Meta luôn là khối RỘNG gần sát đáy màn. Trên dump LIVE 2026-09-11
        /// hàm này trả về ĐÚNG toạ độ mà đường xpath tiếng Anh đã xác minh:
        ///   bước 2 `Button cd='Agree' [56,2182][1384,2336]` -> (720,2259)
        ///   bước 3 `Button cd='OK'    [56,2168][1384,2322]` -> (720,2245)
        /// Chọn node THẤP NHẤT (Cy lớn nhất) rồi mới tới DIỆN TÍCH LỚN NHẤT — luật diện tích
        /// đơn thuần từng chọn nhầm ĐOẠN VĂN mô tả to gấp 3 lần nút thật (lỗi kẹt 2026-09-08).
        /// </summary>
        internal static System.Drawing.Point? FindMetaFooterButtonByGeometry(string xml)
        {
            var nodes = ParseUiNodes(xml, out int maxR, out int maxB);
            if (nodes == null || nodes.Count == 0 || maxR <= 0 || maxB <= 0) return null;

            UiNode? best = null;
            foreach (var nd in nodes)
            {
                if (nd.Disabled) continue;                       // nút đang bị gate -> bấm cũng chết
                if (nd.W < maxR / 2) continue;                   // footer trải gần hết chiều ngang
                if (nd.H < 20 || nd.H > maxB / 6) continue;      // là MỘT nút, không phải cả khối
                if (nd.Cy < maxB * 3 / 4) continue;              // trong 25% ĐÁY màn
                if (!nd.Clickable && nd.Cd.Length == 0 && nd.Tx.Length == 0) continue; // node trơ
                if (best == null) { best = nd; continue; }
                int d = nd.Cy.CompareTo(best.Value.Cy);
                if (d > 0 || (d == 0 && nd.Area > best.Value.Area)) best = nd;
            }
            return best == null ? null : new System.Drawing.Point(best.Value.Cx, best.Value.Cy);
        }

        // Thẻ có chứa CHỮ SỐ ở node con nào không — chữ số chính là GIÁ TIỀN của option trả phí.
        private static bool CardHasDigit(List<UiNode> nodes, UiNode card)
        {
            foreach (var nd in nodes)
            {
                if (!card.ContainsRect(nd)) continue;
                if (HasDigit(nd.Cd) || HasDigit(nd.Tx)) return true;
            }
            return false;
        }

        private static bool HasDigit(string s)
        {
            for (int i = 0; i < s.Length; i++) if (char.IsDigit(s[i])) return true;
            return false;
        }

        /// <summary>
        /// Điểm sắp bấm có nằm TRONG một node `enabled="false"` RỘNG không?
        /// Nút Continue của màn bước 1 nằm trong wrapper `ViewGroup … enabled="false" …
        /// [56,2182][1384,2336]` cho tới khi một radio được chọn (xác minh LIVE 2026-09-11 trên
        /// 520058f34d7c947b: wrapper biến mất NGAY sau khi tích radio). Bấm khi wrapper còn
        /// disabled là BẤM CHẾT — đốt hết 4 lượt mà màn không đổi = triệu chứng KẸT im lặng.
        /// </summary>
        private static bool IsPointBlockedByDisabled(string xml, System.Drawing.Point p)
        {
            var nodes = ParseUiNodes(xml, out int maxR, out _);
            if (nodes == null || maxR <= 0) return false;
            foreach (var nd in nodes)
            {
                if (!nd.Disabled) continue;
                if (nd.W < maxR / 2) continue;
                if (nd.Contains(p.X, p.Y)) return true;
            }
            return false;
        }

        // Wrapper footer `enabled="false"` còn tồn tại không (= chưa radio nào được chọn).
        private static bool HasDisabledFooterWrapper(string xml)
        {
            var nodes = ParseUiNodes(xml, out int maxR, out int maxB);
            if (nodes == null || maxR <= 0 || maxB <= 0) return false;
            foreach (var nd in nodes)
            {
                if (!nd.Disabled) continue;
                if (nd.W < maxR / 2) continue;
                if (nd.H < 20 || nd.H > maxB / 4) continue;
                if (nd.Cy < maxB * 3 / 4) continue;
                return true;
            }
            return false;
        }

        // ─── Hook TOÀN CỤC: thẻ Meta consent "hiện bất chợt" ──────────────────────────────
        // Thẻ "Use free of charge with ads" KHÔNG chỉ hiện lúc login: Meta có thể bật nó ở
        // BẤT KỲ thời điểm nào của job. Mà 65/66 vòng lặp FindElement của FacebookFarming
        // KHÔNG có XpathType.MetaAdsConsent trong danh sách xpath -> vòng lặp không bao giờ
        // khớp -> poll tight-loop vô hạn, KHÔNG bấm gì, KHÔNG log gì (xác minh LIVE
        // 2026-09-10 trên 520058f34d7c947b: 159 dump/30s, 0 click, activity không đổi).
        //
        // Đăng ký MỘT LẦN từ Program.Main:
        //   ADBClient.GlobalPopupDetector    = FacebookHander.IsMetaConsentPopup;
        //   ADBClient.GlobalPopupInterceptor = FacebookHander.TryHandleMetaConsentPopup;
        // Hook nằm trong GetXMLSource — ĐIỂM THẮT mà mọi vòng lặp đều đi qua — nên vá được
        // cả 65 vòng lặp mà không phải sửa từng chỗ (job mới sau này cũng tự được bảo vệ).
        //
        // Các dấu hiệu nhận diện màn consent (mọi bước, mọi biến thể đã gặp).
        private static readonly string[] MetaConsentMarkers =
        {
            "Use free of charge with ads",
            "Use for free with",
            "Want to subscribe",
            "Subscribe to use without",
            "Continue with personalized ads",
            "Your current experience",
            "By selecting Agree, you consent to Meta",
        };

        /// <summary>
        /// Detector RẺ, KHÔNG tác động — chạy trên MỌI lần dump nên chỉ được đọc chuỗi.
        /// </summary>
        public static bool IsMetaConsentPopup(ADBClient client, string xml)
        {
            if (string.IsNullOrEmpty(xml)) return false;
            foreach (var marker in MetaConsentMarkers)
            {
                if (xml.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            // Meta bản địa hoá màn này theo QUỐC GIA ACC / IP -> 7 marker tiếng Anh trên có thể
            // TRƯỢT HẾT, hook toàn cục không bao giờ chạy (đúng nguyên nhân khiến máy ở nước
            // khác kẹt mà máy tại chỗ thì không). Pre-filter RẺ (chỉ IndexOf, KHÔNG parse XML)
            // cho màn chọn radio: >= 2 lần "Radio button" + package Facebook.
            // Chi tiết bằng chứng ở FindMetaFreeRadioCircleByGeometry.
            return LooksLikeMetaRadioChoiceScreen(xml);
        }

        /// <summary>
        /// Interceptor: nhận diện CHÍNH XÁC rồi dọn màn consent. Trả về TRUE nếu đã xử lý.
        /// Bản ĐỒNG BỘ vì hook trong GetXMLSource là hàm sync.
        /// </summary>
        public static bool TryHandleMetaConsentPopup(ADBClient client, string xml)
        {
            if (!IsMetaConsentPopup(client, xml)) return false;
            // Gate CHÍNH XÁC bằng xpath: marker có thể nằm trong node của MÀN NỀN khi dialog
            // chồng lớp (dump bắt node cả hai màn) nên phải chắc node consent thật sự hiện.
            if (!client.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.MetaAdsConsent), 1, xml, click: false))
            {
                // Xpath tiếng Anh trượt -> gate BẰNG HÌNH HỌC cho bản dịch. Hàm này đòi ĐỦ bằng
                // chứng (>= 2 thẻ xếp dọc, mỗi thẻ một nút tròn trần ở nửa phải, CÓ footer đáy
                // màn) nên không dễ dương tính giả; thiếu một điều kiện là trả null.
                if (FindMetaFreeRadioCircleByGeometry(xml) == null) return false;
                Log(client, "[PopupInterceptor] màn consent KHÔNG khớp chữ (Meta bản địa hoá?) -> nhận diện BẰNG HÌNH HỌC");
            }

            Log(client, "[PopupInterceptor] thẻ Meta consent xuất hiện ngoài luồng job -> dọn toàn cục");
            return TryHandleMetaAdsConsent(client);
        }

        /// <summary>
        /// Wrapper async giữ nguyên chữ ký cho các call site hiện có (FacebookService.Login,
        //  FacebookRegsiner.Agreement/ImportInfo, FacebookFarming story loop). Lõi thật là
        /// bản ĐỒNG BỘ bên dưới — dùng Thread.Sleep thay vì Task.Delay để hook toàn cục
        /// (gọi từ hàm sync GetXMLSource) không phải block-on-async, tránh deadlock nếu
        /// lỡ chạy trên thread có SynchronizationContext (UI thread WinForms).
        /// </summary>
        public static async Task<bool> TryHandleMetaAdsConsentAsync(ADBClient client)
        {
            await Task.CompletedTask;
            return TryHandleMetaAdsConsent(client);
        }

        public static bool TryHandleMetaAdsConsent(ADBClient client)
        {
            // Nhận diện màn trong MỘT lần dump.
            string xml = client.GetXMLSource();
            if (string.IsNullOrEmpty(xml)) return false;
            bool englishGate = client.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.MetaAdsConsent), 1, xml, click: false);
            // Gate tiếng Anh trượt KHÔNG có nghĩa là không phải màn consent: Meta bản địa hoá màn
            // này theo QUỐC GIA TÀI KHOẢN / IP, không theo locale điện thoại. Khi đó nhận diện
            // BẰNG HÌNH HỌC (đòi đủ bằng chứng — xem FindMetaFreeRadioCircleByGeometry).
            // `byGeometry` quyết định toàn bộ đường đi bên dưới: đường tiếng Anh đã xác minh LIVE
            // được giữ NGUYÊN, đường hình học chỉ chạy khi không còn cách nào khác.
            System.Drawing.Point? geoCircle = englishGate ? null : FindMetaFreeRadioCircleByGeometry(xml);
            if (!englishGate && geoCircle == null) return false;
            bool byGeometry = !englishGate;

            Log(client, $"[MetaConsent] BƯỚC 1 bắt đầu — gate={(byGeometry ? "HÌNH HỌC (Meta bản địa hoá)" : "xpath tiếng Anh")}, dump={xml.Length}B");

            // ── BƯỚC 1: TÍCH option miễn phí. ──
            // BIẾN THỂ MỚI (2026-09-07): nút TRÒN riêng -> TAP THƯỜNG vào tâm nút tròn là đủ
            // (không cần hold-tap, không cần cuộn vì Continue đã enabled sẵn).
            // BIẾN THỂ CŨ ("Use for free with ads"): thẻ clickable, phải HOLD-TAP vào TÂM THẺ.
            // Thử nhận diện nút tròn TRƯỚC; không thấy thì fallback sang hold-tap tâm thẻ.
            var radioCenter = FindMetaRadioCircleCenter(client, xml, new List<string>
            {
                "Use free of charge with ads",
                "Use for free with ads",
            });
            if (radioCenter == null)
                radioCenter = geoCircle ?? FindMetaFreeRadioCircleByGeometry(xml);  // lưới HÌNH HỌC
            bool tappedRadioCircle = false;   // biến thể MỚI (nút tròn riêng)
            bool tappedOptionCard = false;    // biến thể CŨ (hold-tap tâm thẻ) — biến thể DUY NHẤT cần cuộn
            if (radioCenter is System.Drawing.Point rc && rc.X > 0 && rc.Y > 0)
            {
                Log(client, $"[MetaConsent] BƯỚC 1: nút tròn radio tại ({rc.X},{rc.Y}) -> TAP THƯỜNG");
                client.Click(rc.X, rc.Y);   // TAP THƯỜNG vào nút tròn -> radio chuyển "Selected".
                tappedRadioCircle = true;
                Thread.Sleep(700);
            }
            else
            {
                var optionCenter = FindCenterByXpaths(client, xml, MetaFreeOptionXpaths);
                if (optionCenter is System.Drawing.Point oc && oc.X > 0 && oc.Y > 0)
                {
                    Log(client, $"[MetaConsent] BƯỚC 1: thẻ option (biến thể cũ) tại ({oc.X},{oc.Y}) -> HOLD-TAP 200ms");
                    // LongClick = `input swipe X Y X Y 200` (nhấn-giữ cùng điểm): cơ chế DUY NHẤT
                    // chọn được radio trên UI Bloks này (biến thể thẻ cũ) — tap tức thì KHÔNG ăn.
                    client.LongClick(oc.X, oc.Y, 200);
                    tappedOptionCard = true;
                    Thread.Sleep(700);
                }
                else
                {
                    Log(client, "[MetaConsent] BƯỚC 1: KHÔNG tìm được nút tròn lẫn thẻ option -> Continue sẽ bị gate, KHÔNG bấm mò");
                }
            }

            // ── BƯỚC 1b: XÁC MINH radio ĐÃ được chọn TRƯỚC khi bấm Continue. ──
            // Continue nằm trong wrapper `ViewGroup … enabled="false" … [56,2182][1384,2336]`
            // cho tới khi một option được chọn (xác minh LIVE 2026-09-11 trên 520058f34d7c947b:
            // wrapper BIẾN MẤT ngay sau khi tích radio (1289,1726)). Bấm Continue khi wrapper còn
            // disabled là BẤM CHẾT — đó chính là đường dẫn tới KẸT im lặng đã báo.
            if (tappedRadioCircle || tappedOptionCard)
            {
                string xmlSel = client.GetXMLSource();
                if (!string.IsNullOrEmpty(xmlSel) && HasDisabledFooterWrapper(xmlSel))
                {
                    var retry = FindMetaFreeRadioCircleByGeometry(xmlSel) ?? radioCenter;
                    if (retry is System.Drawing.Point rp && rp.X > 0 && rp.Y > 0)
                    {
                        Log(client, $"[MetaConsent] BƯỚC 1b: footer VẪN enabled=\"false\" -> radio chưa ăn, HOLD-TAP lại ({rp.X},{rp.Y})");
                        // Lần đầu có thể trúng BIẾN THỂ CŨ (thẻ clickable, chỉ ăn HOLD-TAP) hoặc
                        // trúng nhãn thay vì nút tròn -> giữ 200ms đúng cơ chế biến thể cũ.
                        client.LongClick(rp.X, rp.Y, 200);
                        Thread.Sleep(700);
                        string xmlSel2 = client.GetXMLSource();
                        if (!string.IsNullOrEmpty(xmlSel2))
                            Log(client, HasDisabledFooterWrapper(xmlSel2)
                                ? "[MetaConsent] BƯỚC 1b: footer VẪN disabled sau HOLD-TAP -> các lượt dưới sẽ KHÔNG bấm chết"
                                : "[MetaConsent] BƯỚC 1b: footer đã enabled sau HOLD-TAP -> radio ĐÃ được chọn");
                    }
                    else
                    {
                        Log(client, "[MetaConsent] BƯỚC 1b: footer VẪN enabled=\"false\" và không tìm lại được radio");
                    }
                }
                else
                {
                    Log(client, "[MetaConsent] BƯỚC 1b: footer đã enabled -> radio ĐÃ được chọn");
                }
            }

            // ── BƯỚC 2: CUỘN RecyclerView xuống ĐÁY để Continue SÁNG (bật). ──
            // CHỈ cuộn ở BIẾN THỂ CŨ (hold-tap tâm thẻ): ở biến thể đó Continue disabled cho tới
            // khi cuộn hết nội dung. Các trường hợp còn lại KHÔNG cần và cuộn chỉ tổ gây hại:
            //   • Biến thể MỚI: xác minh LIVE 2026-09-08 (52003dc05fab94ab) — wrapper footer đổi
            //     enabled="false" -> "true" NGAY sau khi tích radio (1289,1642), Continue sáng sẵn.
            //   • Màn BƯỚC 2 (Agree) và BƯỚC 3 (OK): nút nằm ở footer CỐ ĐỊNH, NGOÀI RecyclerView
            //     ([56,2350][1384,2392] vs [0,238][1440,2294]; [56,2336][1384,2392] vs [0,238][1440,2144])
            //     nên cuộn không bật thêm được gì.
            //   • Vào thẳng màn BƯỚC 2/3 từ vòng lặp ngoài: không có radio/thẻ nào để chọn.
            // Bỏ cuộn thừa còn tránh một rủi ro thật: SwipeUp dùng toạ độ THEO TỈ LỆ nên trên màn/
            // density khác có thể đẩy tiêu đề VÀ cả hai thẻ ra khỏi màn -> MetaAdsConsent hết khớp
            // -> break ở vòng dưới -> không bao giờ bấm Continue. Vòng retry bên dưới vẫn
            // SwipeUp(1) mỗi nhịp, đủ làm lưới an toàn cho biến thể cũ.
            if (tappedOptionCard)
            {
                client.SwipeUp(2, 500, 350);
                Thread.Sleep(600);
            }

            // ── BƯỚC 3: BẤM Continue THẬT, có verify + retry. ──
            // Mỗi lần chưa qua được màn: cuộn THÊM 1 nhịp (phòng khi 2 swipe đầu chưa tới đáy
            // -> Continue vẫn mờ, bấm vô ích) rồi mới tìm & bấm lại. Nút Continue là footer cố
            // định (bounds nằm NGOÀI RecyclerView [0,238][1440,2294]) nên luôn thấy — cuộn chỉ
            // để BẬT nó, không phải để hiện nó.
            bool cleared = false;
            for (int attempt = 0; attempt < 4; attempt++)
            {
                string xmlNow = client.GetXMLSource();
                // dump fail nhất thời (SF bận, uiautomator trả rỗng) -> THỬ LẠI, đừng bỏ cuộc:
                // `break` ở đây là một trong các đường dẫn tới KẸT im lặng (hàm vẫn `return true`).
                if (string.IsNullOrEmpty(xmlNow)) { Thread.Sleep(500); continue; }

                // "Màn consent còn hiện không?" — HAI đường:
                //   • TIẾNG ANH (đã xác minh LIVE): xpath MetaAdsConsent — giữ NGUYÊN hành vi cũ.
                //   • HÌNH HỌC (Meta bản địa hoá): cấu trúc 2 thẻ radio. CHỈ dùng khi gate tiếng
                //     Anh trượt ngay từ đầu (`byGeometry`), để không đụng vào đường đã xác minh.
                bool radioStillUp = LooksLikeMetaRadioChoiceScreen(xmlNow)
                                    && FindMetaFreeRadioCircleByGeometry(xmlNow) != null;
                bool consentStillUp = byGeometry
                    ? radioStillUp
                    : client.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.MetaAdsConsent), 1, xmlNow, click: false);
                // Đã qua màn consent bước 1? (không còn dấu hiệu nhận diện) -> xong. Bước 2
                // "How your information is used" do vòng lặp login ngoài xử lý (NavigationButton).
                if (!consentStillUp) { cleared = true; break; }

                if (byGeometry)
                {
                    // ── ĐƯỜNG HÌNH HỌC: không có chữ tiếng Anh nào để khớp. ──
                    // CHỈ hành động khi radioStillUp == true, tức CHẮC CHẮN còn trên màn consent —
                    // nhờ vậy FindMetaFooterButtonByGeometry không bao giờ bị đem ra đoán trên màn
                    // thường (thanh nav đáy của news feed cũng RỘNG và nằm SÁT ĐÁY màn).
                    var rcg = FindMetaFreeRadioCircleByGeometry(xmlNow);
                    var ftg = FindMetaFooterButtonByGeometry(xmlNow);
                    // ftg bỏ qua node enabled="false" nên khi footer còn bị gate thì ftg == null.
                    bool gated = ftg == null || HasDisabledFooterWrapper(xmlNow);
                    if (gated && rcg is System.Drawing.Point pg && pg.X > 0 && pg.Y > 0)
                    {
                        Log(client, $"[MetaConsent-HH] lượt {attempt + 1}/4: footer còn bị gate -> tích radio ({pg.X},{pg.Y}), KHÔNG bấm chết");
                        client.LongClick(pg.X, pg.Y, 200);
                        Thread.Sleep(900);
                        continue;
                    }
                    if (ftg is System.Drawing.Point fg && fg.X > 0 && fg.Y > 0)
                    {
                        Log(client, $"[MetaConsent-HH] lượt {attempt + 1}/4: bấm footer ({fg.X},{fg.Y}) bằng {(attempt == 0 ? "TAP" : "HOLD-TAP 180ms")}");
                        if (attempt == 0) client.Click(fg.X, fg.Y); else client.LongClick(fg.X, fg.Y, 180);
                        Thread.Sleep(1400);
                        continue;
                    }
                    Log(client, "[MetaConsent-HH] bước 1 đã qua nhưng không nhận diện được bước kế (bản dịch) -> giao vòng lặp login ngoài (NavigationButton)");
                    cleared = true;
                    break;
                }

                // Màn BƯỚC 3 ("Your current experience") phải bấm OK footer, KHÔNG bấm
                // "Continue with personalized ads": nút đó to hơn (169984 > 74368) nên nếu lọt vào
                // cùng danh sách thì luật "diện tích lớn nhất" sẽ trúng nó.
                //
                // 3 BẬC — bậc sau chỉ chạy khi bậc trước TRẮNG:
                //   1) khớp CHÍNH XÁC (OK-only ở màn bước 3; Continue/Agree/OK ở các màn khác)
                //   2) khớp CONTAINS kèm preferClickable — lưới cho biến thể nhãn dài
                //   3) riêng màn bước 3: quay lại Continue/Agree chính xác cho biến thể không có OK
                // preferClickable=true ở bậc 2 chính là thứ chặn đoạn văn mô tả "By selecting Agree,
                // you consent to Meta…" (clickable=false, area lớn hơn nút thật) — xem ghi chú ở
                // MetaContinueExactXpaths.
                bool isExperienceScreen = IsMetaExperienceScreen(xmlNow);
                var contCenter = FindCenterByXpaths(client, xmlNow,
                    isExperienceScreen ? MetaExperienceOkXpaths : MetaContinueExactXpaths);
                if (contCenter == null)
                    contCenter = FindCenterByXpaths(client, xmlNow, MetaContinueLooseXpaths, preferClickable: true);
                if (contCenter == null && isExperienceScreen)
                    contCenter = FindCenterByXpaths(client, xmlNow, MetaContinueExactXpaths);

                if (contCenter is System.Drawing.Point cc && cc.X > 0 && cc.Y > 0)
                {
                    // KHÔNG bấm khi nút còn nằm TRONG wrapper `enabled="false"`: bấm cũng vô tác
                    // dụng và ĐỐT hết 4 lượt -> KẸT im lặng. Tích lại radio rồi sang lượt sau.
                    if (IsPointBlockedByDisabled(xmlNow, cc))
                    {
                        var rc2 = FindMetaFreeRadioCircleByGeometry(xmlNow) ?? radioCenter;
                        if (rc2 is System.Drawing.Point p2 && p2.X > 0 && p2.Y > 0)
                        {
                            Log(client, $"[MetaConsent] lượt {attempt + 1}/4: nút ({cc.X},{cc.Y}) còn DISABLED -> tích lại radio ({p2.X},{p2.Y}), KHÔNG bấm chết");
                            client.LongClick(p2.X, p2.Y, 200);
                            Thread.Sleep(700);
                        }
                        else
                        {
                            Log(client, $"[MetaConsent] lượt {attempt + 1}/4: nút ({cc.X},{cc.Y}) còn DISABLED và không tìm lại được radio -> bỏ qua lượt, KHÔNG bấm chết");
                            Thread.Sleep(600);
                            client.SwipeUp(1, 500, 350);
                            Thread.Sleep(500);
                        }
                        continue;
                    }
                    Log(client, $"[MetaConsent] lượt {attempt + 1}/4: bấm nút ({cc.X},{cc.Y}) bằng {(attempt == 0 ? "TAP" : "HOLD-TAP 180ms")}{(isExperienceScreen ? " [màn BƯỚC 3 — OK-only]" : "")}");
                    if (attempt == 0)
                        client.Click(cc.X, cc.Y);          // lần đầu: tap thường nút Button thật
                    else
                        client.LongClick(cc.X, cc.Y, 180); // retry: hold-tap chắc ăn hơn
                }
                else
                {
                    Log(client, $"[MetaConsent] lượt {attempt + 1}/4: KHÔNG tìm được nút footer nào khớp chữ");
                }
                Thread.Sleep(1400);
                // Chưa qua -> cuộn thêm để chắc Continue đã bật cho lần bấm kế.
                // Ở màn BƯỚC 3 nút OK là footer NGOÀI RecyclerView nên cuộn vô hại;
                // ở BƯỚC 1 biến thể cũ thì cuộn chính là việc cần làm để bật Continue.
                client.SwipeUp(1, 500, 350);
                Thread.Sleep(500);
            }

            // ── XÁC MINH THẬT — KHÔNG `return true` mò. ──
            // Trước 2026-09-11 hàm trả TRUE VÔ ĐIỀU KIỆN ở đây: hết 4 lượt mà màn vẫn còn thì
            // caller vẫn tưởng đã dọn xong -> không retry, van an toàn (PopupMaxFailStreak) không
            // đếm, log không có gì -> ĐỨNG IM = đúng triệu chứng "kẹt" đã báo.
            // Nay trả về KẾT QUẢ ĐO ĐƯỢC: false là false, để hook hạ nhiệt và log nói rõ.
            Thread.Sleep(800);
            if (!cleared)
            {
                string xmlFinal = client.GetXMLSource();
                if (!string.IsNullOrEmpty(xmlFinal))
                {
                    bool stillUp = byGeometry
                        ? (LooksLikeMetaRadioChoiceScreen(xmlFinal) && FindMetaFreeRadioCircleByGeometry(xmlFinal) != null)
                        : client.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.MetaAdsConsent), 1, xmlFinal, click: false);
                    cleared = !stillUp;
                }
            }
            Log(client, cleared
                ? "[MetaConsent] XONG — màn consent đã qua (xác minh bằng dump cuối)"
                : "[MetaConsent] THẤT BẠI — màn consent VẪN còn sau 4 lượt; trả FALSE để van an toàn hạ nhiệt");
            return cleared;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // MÀN "QUIET MODE" CỦA FACEBOOK — 2 BƯỚC TUẦN TỰ — thêm 2026-09-13
        // ─────────────────────────────────────────────────────────────────────────
        // YÊU CẦU: vượt qua bước quiet mode. Bước ĐẦY ĐỦ là bấm "Manage quiet mode"
        // (mở màn con) RỒI bấm "End quiet mode" (tắt quiet mode) — HAI nút trên HAI
        // màn khác nhau, phải theo TRÌNH TỰ.
        //
        // VÌ SAO TRƯỚC ĐÂY KẸT: 4 xpath quiet mode nằm trong nhóm NavigationButton
        // (XpathManagerFacebook.cs ~150-154) và bị case chung `ElementWithAttributes(c, 1)`
        // xử lý — bấm ĐÚNG 1 nút mỗi vòng lặp, KHÔNG trình tự, KHÔNG verify. Trong
        // GetActiAccountFacebook, NavigationButton đứng SAU ~120 xpath (CP282/Loading/.../
        // MetaAdsConsent); chỉ cần một node màn nền khớp trước (đúng bẫy dialog-chồng-lớp
        // từng gây kẹt Dismiss — xem ghi chú đầu nhóm NavigationButton) là nút quiet mode
        // KHÔNG BAO GIỜ tới lượt -> tool đứng im ở màn này.
        //
        // CÁCH SỬA (mirror ĐÚNG pattern Meta consent): handler 2 bước chuyên biệt, nối vào
        // HOOK TOÀN CỤC (IsGlobalPopup/TryHandleGlobalPopup bên dưới). Hook chạy BÊN TRONG
        // ADBClient.GetXMLSource — ĐIỂM THẮT mà FindElement của MỌI vòng lặp đều đi qua
        // (ADBClient.cs:2229 + :3644) — nên nó dọn màn quiet TRƯỚC khi FindElement kịp khớp,
        // pre-empt hoàn toàn vấn đề first-match, và phủ cả ~65 vòng lặp mà không phải sửa
        // từng switch. 4 xpath trong NavigationButton được GIỮ làm lưới an toàn cho vòng
        // lặp cục bộ (hook lỡ hạ nhiệt sau 3 lần fail) — nhãn đặc hiệu nên không dương tính giả.
        //
        // Chọn node bằng FindCenterByXpaths(preferClickable:true): nút quiet mode có thể có
        // node View decoy 1px (như nút OK/Continue của consent) — ưu tiên node clickable
        // loại decoy, KHÔNG dùng largest-area thuần.
        private static readonly List<string> ManageQuietModeXpaths = new List<string>
        {
            // Dạng exact trước (engine có fallback hoa/thường cho [@attr='value']),
            // contains() sau làm lưới an toàn cho nhãn dài/nhúng.
            "//*[@content-desc=\"Manage quiet mode\"]",
            "//*[@text=\"Manage quiet mode\"]",
            "//*[contains(@content-desc, \"Manage quiet mode\")]",
            "//*[contains(@text, \"Manage quiet mode\")]",
        };
        private static readonly List<string> EndQuietModeXpaths = new List<string>
        {
            "//*[@content-desc=\"End quiet mode\"]",
            "//*[@text=\"End quiet mode\"]",
            "//*[contains(@content-desc, \"End quiet mode\")]",
            "//*[contains(@text, \"End quiet mode\")]",
        };

        /// <summary>
        /// Detector RẺ cho màn quiet mode — chạy trên MỌI lần dump nên CHỈ được đọc chuỗi
        /// (không parse XML, không screenshot). Nhãn ĐẶC HIỆU "Manage quiet mode"/"End quiet
        /// mode" — KHÔNG dùng "quiet" trần (dễ khớp chữ khác).
        /// </summary>
        public static bool IsQuietModePopup(string xml)
        {
            if (string.IsNullOrEmpty(xml)) return false;
            return xml.IndexOf("Manage quiet mode", StringComparison.OrdinalIgnoreCase) >= 0
                || xml.IndexOf("End quiet mode", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Interceptor TỰ-GATE cho quiet mode (chữ ký khớp Func&lt;ADBClient,string,bool&gt; để nối
        /// vào hook toàn cục). Trả FALSE nhanh nếu không phải màn quiet mode. Bản ĐỒNG BỘ vì
        /// hook trong GetXMLSource là hàm sync.
        /// </summary>
        public static bool TryHandleQuietModePopup(ADBClient client, string xml)
        {
            if (client == null || !IsQuietModePopup(xml)) return false;
            // Gate CHÍNH XÁC bằng toạ độ: marker chữ có thể nằm trong node MÀN NỀN khi dialog
            // chồng lớp (dump bắt node cả hai màn) nên phải chắc NÚT thật sự hiện và bấm được.
            bool hasManage = FindCenterByXpaths(client, xml, ManageQuietModeXpaths, preferClickable: true) != null;
            bool hasEnd = FindCenterByXpaths(client, xml, EndQuietModeXpaths, preferClickable: true) != null;
            if (!hasManage && !hasEnd) return false;
            Log(client, "[PopupInterceptor] màn Quiet mode xuất hiện ngoài luồng job -> dọn toàn cục");
            return TryHandleQuietMode(client, xml);
        }

        /// <summary>
        /// Wrapper async giữ nguyên chữ ký cho các call site cục bộ (nếu cần nối vào switch
        //  case sau này). Lõi thật là bản ĐỒNG BỘ bên dưới — Thread.Sleep thay vì Task.Delay để
        /// hook toàn cục (gọi từ hàm sync GetXMLSource) không block-on-async, tránh deadlock UI thread.
        /// </summary>
        public static async Task<bool> TryHandleQuietModeAsync(ADBClient client)
        {
            await Task.CompletedTask;
            return TryHandleQuietMode(client);
        }

        /// <summary>
        /// Lõi ĐỒNG BỘ: bấm "Manage quiet mode" RỒI "End quiet mode" theo trình tự, mỗi bước có
        /// retry có chặn (dump lại mỗi lượt, leo thang tap -> hold-tap), và XÁC MINH bằng dump
        /// cuối — KHÔNG `return true` mò (bài học từ consent stuck 2026-09-11: trả true vô điều
        //  kiện -> caller không retry, van an toàn không đếm, log im lặng = ĐỨNG IM).
        /// </summary>
        /// <param name="xml">XML đã dump sẵn (truyền vào để KHÔNG tốn thêm một lần dump cho gate).</param>
        public static bool TryHandleQuietMode(ADBClient client, string xml = "")
        {
            try
            {
                if (client == null) return false;
                if (string.IsNullOrEmpty(xml)) xml = client.GetXMLSource();
                if (string.IsNullOrEmpty(xml) || !IsQuietModePopup(xml)) return false;

                // ── BƯỚC 1: bấm "Manage quiet mode" để mở màn con có "End quiet mode". ──
                // Nếu màn ĐÃ có sẵn "End quiet mode" (một số biến thể gộp 1 màn) -> bỏ qua bước 1.
                Log(client, "[QuietMode] BƯỚC 1 — tìm & bấm \"Manage quiet mode\"");
                bool reachedEnd = false;
                for (int attempt = 0; attempt < 4 && !reachedEnd; attempt++)
                {
                    client.ThrowIfStopped();
                    string xmlNow = (attempt == 0) ? xml : client.GetXMLSource();
                    if (string.IsNullOrEmpty(xmlNow)) { Thread.Sleep(400); continue; }

                    // Đã thấy nút End -> BƯỚC 1 coi như xong, chuyển BƯỚC 2.
                    if (FindCenterByXpaths(client, xmlNow, EndQuietModeXpaths, preferClickable: true) != null)
                    {
                        reachedEnd = true;
                        break;
                    }
                    var c = FindCenterByXpaths(client, xmlNow, ManageQuietModeXpaths, preferClickable: true);
                    if (c == null) break;   // không thấy Manage lẫn End -> đã qua màn / không phải màn này
                    if (attempt == 0) client.Click(c.Value.X, c.Value.Y);
                    else client.LongClick(c.Value.X, c.Value.Y, 180);
                    Thread.Sleep(1200);     // chờ màn con tải
                }

                // ── BƯỚC 2: bấm "End quiet mode" để tắt quiet mode. ──
                Log(client, "[QuietMode] BƯỚC 2 — tìm & bấm \"End quiet mode\"");
                bool cleared = false;
                for (int attempt = 0; attempt < 4 && !cleared; attempt++)
                {
                    client.ThrowIfStopped();
                    string xmlNow = client.GetXMLSource();
                    if (string.IsNullOrEmpty(xmlNow)) { Thread.Sleep(400); continue; }

                    // Hết marker quiet mode -> đã tắt xong.
                    if (!IsQuietModePopup(xmlNow)) { cleared = true; break; }

                    var c = FindCenterByXpaths(client, xmlNow, EndQuietModeXpaths, preferClickable: true);
                    if (c == null)
                    {
                        // Vẫn còn marker quiet nhưng KHÔNG thấy nút End -> có thể bị đẩyกลับ
                        // màn "Manage quiet mode" (vd bấm End chưa ăn). Thử bấm lại Manage rồi continue.
                        var m = FindCenterByXpaths(client, xmlNow, ManageQuietModeXpaths, preferClickable: true);
                        if (m != null) { client.Click(m.Value.X, m.Value.Y); Thread.Sleep(1200); continue; }
                        break;
                    }
                    if (attempt == 0) client.Click(c.Value.X, c.Value.Y);
                    else client.LongClick(c.Value.X, c.Value.Y, 180);
                    Thread.Sleep(1200);
                }

                // ── XÁC MINH THẬT bằng dump cuối — không trả true mò. ──
                if (!cleared)
                {
                    Thread.Sleep(600);
                    client.ThrowIfStopped();
                    string xmlFinal = client.GetXMLSource();
                    if (!string.IsNullOrEmpty(xmlFinal)) cleared = !IsQuietModePopup(xmlFinal);
                }
                Log(client, cleared
                    ? "[QuietMode] XONG — đã bấm Manage quiet mode -> End quiet mode (xác minh dump cuối)"
                    : "[QuietMode] THẤT BẠI — màn quiet mode VẪN còn sau 4 lượt; trả FALSE để van an toàn hạ nhiệt");
                return cleared;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                Log(client, $"[QuietMode] lỗi: {ex.Message}");
                return false;
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // IXT "You're back on Facebook" — bottom sheet hiện sau khi acc được gỡ suspend.
        // ─────────────────────────────────────────────────────────────────────────
        // LIVE 2026-09-25 trên 5200ef68feda15cf: acc vừa được gỡ suspend thì FB mở
        // com.facebook.katana/...ixt.enrollmenttrigger.IXTEnrollmentActivity — bottom sheet
        // "You're back on Facebook / Your account is no longer suspended". Màn này GIẾT job:
        //   • Nội dung Litho/Bloks: dump KHÔNG có text/content-desc nào (subtree sheet chỉ là
        //     ViewGroup trần + RecyclerView) -> xpath tiếng Anh KHÔNG BAO GIỜ khớp, detector
        //     chuỗi cũng mù luôn.
        //   • Sheet KHÔNG cuộn (input swipe trong sheet không ăn — đã xác minh), nút duy nhất
        //     nằm dưới nếp gấp -> không tap target nào xuất hiện.
        //   • uiautomator dump bị KILL trên màn này (exit 137) — chỉ kênh ATX sống.
        // LỐI THOÁT xác minh LIVE: tap SCRIM (vùng tối phía trên sheet) — sheet dismiss như
        // bottom sheet cancelable thường, trả về FbMainTabActivity (feed). Back cũng thoát nhưng
        // để làm fallback thôi (scrim đúng ngữ nghĩa "dismiss" hơn, không rủi ro pop nhầm).
        //
        // Detector: prefilter chuỗi (id obfuscate của wrapper sheet) + HÌNH HỌC (handle + thân
        // sheet + wrapper disabled neo đáy) — xem IsIxtReinstatementPopup. KHÔNG dùng chuỗi đơn
        // thuần: id đó phủ gần mọi node FB nên feed thường cũng chứa nó. Vẫn thuần đọc, không shell.
        //
        // Interceptor: gate BẰNG ACTIVITY (IXTEnrollmentActivity) vì id obfuscate kia có thể bị
        // sheet Bloks khác dùng chung — tap scrim sai màn sẽ dismiss UI thật. Toạ độ tap suy
        // THUẦN HÌNH HỌC từ drag-handle (xem FindIxtScrimTapByGeometry). Xác minh bằng dump:
        // marker sheet phải BIẾN MẤT; không bao giờ trả true mò.

        private const string IxtSheetWrapperId = "com.facebook.katana:id/(name removed)";

        /// <summary>
        /// Detector cho màn IXT "You're back on Facebook". KHÔNG thể chỉ đọc chuỗi: id
        /// `com.facebook.katana:id/(name removed)` là id OBFUSCATE mà FB gán cho GẦN MỌI node
        /// (đo LIVE 2026-09-25: feed thường có ~25 node mang id này, kể cả một wrapper
        /// `enabled="false"` full-width `[0,632][1440,1281]`) -> detector chuỗi thuần sẽ bốc cháy
        /// trên MỌI dump của feed. Vì vậy: prefilter chuỗi (rẻ, loại 99% dump không phải FB-sheet)
        /// rồi HÌNH HỌC (handle + thân sheet + wrapper disabled neo ĐÁY màn — feed không có handle
        /// và wrapper của feed lơ lửng giữa màn chứ không neo đáy). Vẫn THUẦN đọc, không shell.
        /// </summary>
        public static bool IsIxtReinstatementPopup(string xml)
        {
            if (string.IsNullOrEmpty(xml)) return false;
            if (xml.IndexOf(IxtSheetWrapperId, StringComparison.Ordinal) < 0) return false;
            return FindIxtScrimTapByGeometry(xml) != null;
        }

        /// <summary>
        /// Điểm tap SCRIM phía trên sheet IXT, suy THUẦN HÌNH HỌC (sheet không nhãn). Trả null
        /// khi KHÔNG ĐỦ bằng chứng (caller tuyệt đối không đoán mò):
        ///  1. Handle TRẦN: node không text/desc, hẹp (W &lt;= 20% màn), mỏng (H &lt;= 3% màn),
        ///     nằm GIỮA trục X và ở dải giữa màn hình (20%..80% chiều cao) — đo LIVE
        ///     [650,1112][790,1126] trên màn 1440x2560.
        ///  2. Thân sheet: node RỘNG FULL màn bắt đầu NGAY dưới handle (trong 5% chiều cao).
        ///  3. Wrapper disabled full-width phải BỌC thân sheet — chứng tỏ đây đúng sheet IXT
        ///     (id marker) chứ không phải một View tình cờ giống handle ở màn khác.
        /// Điểm tap: (giữa màn, giữa dải scrim phía trên sheet) — vùng tối đã xác minh ăn tap.
        /// </summary>
        internal static System.Drawing.Point? FindIxtScrimTapByGeometry(string xml)
        {
            var nodes = ParseUiNodes(xml, out int maxR, out int maxB);
            if (nodes == null || nodes.Count == 0 || maxR <= 0 || maxB <= 0) return null;

            // (1) drag-handle: View trần, nhỏ, giữa màn. Lấy cái TRÊN CÙNG nếu có nhiều ứng viên.
            UiNode? handle = null;
            foreach (var nd in nodes)
            {
                if (nd.Tx.Length > 0 || nd.Cd.Length > 0) continue;
                if (nd.W > maxR / 5 || nd.H > maxB * 3 / 100 || nd.H < 2) continue;
                if (Math.Abs(nd.Cx - maxR / 2) > maxR / 20) continue;
                if (nd.Cy < maxB / 5 || nd.Cy > maxB * 4 / 5) continue;
                if (handle == null || nd.Cy < handle.Value.Cy) handle = nd;
            }
            if (handle == null) return null;

            // (2) thân sheet: full-width, mép trên sát dưới handle.
            int sheetTop = 0;
            foreach (var nd in nodes)
            {
                if (nd.W < maxR * 95 / 100) continue;
                if (nd.T < handle.Value.B || nd.T > handle.Value.B + maxB / 20) continue;
                if (sheetTop == 0 || nd.T < sheetTop) sheetTop = nd.T;
            }
            if (sheetTop == 0) return null;

            // (3) wrapper disabled full-width bọc thân sheet (= id marker của sheet IXT).
            bool wrapperOk = false;
            foreach (var nd in nodes)
            {
                if (!nd.Disabled || nd.W < maxR / 2) continue;
                if (nd.T > sheetTop + maxB / 50 || nd.B < maxB * 9 / 10) continue;
                wrapperOk = true;
                break;
            }
            if (!wrapperOk) return null;

            int y = sheetTop / 2;
            if (y < maxB / 20 || y >= handle.Value.T) return null;   // dải scrim quá mỏng -> bỏ
            return new System.Drawing.Point(maxR / 2, y);
        }

        /// <summary>
        /// Interceptor TỰ-GATE cho sheet IXT (chữ ký khớp Func&lt;ADBClient,string,bool&gt; để nối
        /// vào hook toàn cục). Trả FALSE nhanh nếu không phải màn IXT. Bản ĐỒNG BỘ vì hook trong
        /// GetXMLSource là hàm sync.
        /// </summary>
        public static bool TryHandleIxtReinstatementPopup(ADBClient client, string xml)
        {
            if (client == null || !IsIxtReinstatementPopup(xml)) return false;

            // Gate BẰNG ACTIVITY: id wrapper là id Bloks obfuscate nên sheet khác của FB có thể
            // dùng chung -> tap scrim sai màn sẽ dismiss UI thật. IXTEnrollmentActivity là màn
            // DUY NHẤT phép dismiss này thuộc về.
            string activity = string.Empty;
            try
            {
                var info = client.AppCurrent();
                if (info != null) activity = info.Activity ?? string.Empty;
            }
            catch { }
            if (activity.IndexOf("IXTEnrollmentActivity", StringComparison.OrdinalIgnoreCase) < 0) return false;

            var scrim = FindIxtScrimTapByGeometry(xml);
            if (scrim == null) return false;

            Log(client, "[PopupInterceptor] sheet IXT 'You're back on Facebook' -> tap scrim để dismiss");
            for (int attempt = 0; attempt < 3; attempt++)
            {
                client.ThrowIfStopped();
                client.Click(scrim.Value.X, scrim.Value.Y);
                Thread.Sleep(1200);
                string xmlNow = client.GetXMLSource();
                if (string.IsNullOrEmpty(xmlNow)) { Thread.Sleep(400); continue; }
                if (!IsIxtReinstatementPopup(xmlNow))
                {
                    Log(client, "[IXT] XONG — sheet đã dismiss (xác minh dump: hết wrapper id)");
                    return true;
                }
                // Scrim tap không ăn (biến thể sheet?) -> thử Back trước khi chịu thua.
                client.Shell("input keyevent 4");
                Thread.Sleep(1200);
                xmlNow = client.GetXMLSource();
                if (!string.IsNullOrEmpty(xmlNow) && !IsIxtReinstatementPopup(xmlNow))
                {
                    Log(client, "[IXT] XONG — sheet đã dismiss bằng Back (xác minh dump)");
                    return true;
                }
            }
            Log(client, "[IXT] THẤT BẠI — sheet VẪN còn sau 3 lượt; trả FALSE để van an toàn hạ nhiệt");
            return false;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // HOOK TOÀN CỤC GỘP — detector/interceptor cho Meta consent, quiet mode VÀ sheet IXT.
        // ─────────────────────────────────────────────────────────────────────────
        // ADBClient chỉ có MỘT cặp slot GlobalPopupDetector/GlobalPopupInterceptor (gán 1 lần
        // từ Program.Main của mỗi app — AutoAndroid KHÔNG reference Sunny.Subd.Core). Gán chồng
        // sẽ GHI ĐÈ slot trước. Vì vậy phải GỘP: detector OR cả ba, interceptor thử Meta consent
        // trước (tự-gate, trả false nhanh nếu không phải) rồi quiet mode rồi sheet IXT (đều
        // tự-gate). Mỗi handler con tự nhận diện CHÍNH XÁC nên không giẫm chân nhau; cả ba đều
        // hưởng chung van an toàn fail-streak/cooldown của RunGlobalPopupInterceptor.
        //
        // Cập nhật 2 Program.cs (LamToolAutoPhonePrime + Facebook-Farm-NewFeed-PostStory) trỏ vào
        // IsGlobalPopup / TryHandleGlobalPopup thay vì IsMetaConsentPopup / TryHandleMetaConsentPopup.

        /// <summary>Detector toàn cục gộp: consent Meta HOẶC quiet mode HOẶC sheet IXT. RẺ — chỉ đọc chuỗi.</summary>
        public static bool IsGlobalPopup(ADBClient client, string xml)
            => IsMetaConsentPopup(client, xml) || IsQuietModePopup(xml) || IsIxtReinstatementPopup(xml);

        /// <summary>
        /// Interceptor toàn cục gộp: thử Meta consent trước (tự-gate), rồi quiet mode (tự-gate),
        /// rồi sheet IXT (tự-gate). Trả TRUE nếu MỘT trong ba đã xử lý. Bản ĐỒNG BỘ (hook trong
        /// GetXMLSource là sync).
        /// </summary>
        public static bool TryHandleGlobalPopup(ADBClient client, string xml)
        {
            // TryHandleMetaConsentPopup tự gate bằng IsMetaConsentPopup + xpath/hình học -> màn
            // quiet mode / sheet IXT khiến nó trả false NGAY, không tốn thao tác tap nào.
            if (TryHandleMetaConsentPopup(client, xml)) return true;
            if (TryHandleQuietModePopup(client, xml)) return true;
            if (TryHandleIxtReinstatementPopup(client, xml)) return true;
            return false;
        }

        public static List<string> GetActiAccountInstagram()
        {
            var xpaths = XpathManagerInstagram.Combine
                (
                    XpathType.CP282,
                    XpathType.Loading,
                    XpathType.Captcha,
                    XpathType.CP956,
                    XpathType.Logout,
                    XpathType.Block,
                    XpathType.Success,
                    XpathType.CashApp,
                    XpathType.TowFA,
                    XpathType.InputUserName,
                    XpathType.InputPassword,
                    XpathType.NavigationButton
                );
            return xpaths;
        }
        public static string GetCodeTowFA(string _2FA)
        {
            string string_FA = _2FA.Replace("\r", "").Replace("\n", "").Replace(" ", "")
                             .ToString();
            try
            {
                WebClient webClient = new WebClient();
                string input = webClient.DownloadString("http://2fa.live/tok/" + string_FA);
                return Regex.Match(input, "token\":\"(\\d+)\"").Groups[1].Value;
            }
            catch
            {

            }
            byte[] secretKeyBytes = Base32Encoding.ToBytes(string_FA);

            var totp = new Totp(secretKeyBytes);
            var two_fa = totp.ComputeTotp();
            if (Convert.ToInt64(two_fa) > 5)
            {
                return two_fa;
            }
            return string.Empty;
        }
        public static string GetAuthenticationInfo(ADBClient client)
        {
            string token = "";
            string cookie = "";
            string uid = "";
            string catData = "";
            try
            {
                // [v29] Đọc file authentication của FB app qua ĐƯỜNG PROCESS (adb.exe) làm
                // CHÍNH, với đường TUYỆT ĐỐI + quote 'cat ...' (dạng đã kiểm chứng chạy trong
                // repo: ucManagerDevices/MaxChangeService đều quote). Socket giữ làm DỰ PHÒNG.
                // Bản cũ: socket `su -c cat data/...` (relative, KHÔNG quote) và bọc cả 4 lần
                // retry trong MỘT try — một lần socket timeout/hang (ReceiveTimeout 30s, xem v17)
                // nuốt hết retry rồi trả "||"; caller nuốt tiếp trong catch{} => KHÔNG lưu
                // cookie/token mà KHÔNG có log nào. Mỗi lần đọc giờ có try riêng + delay ngắn
                // (file auth được FB ghi trễ một nhịp sau khi login). GIỮ NGUYÊN contract trả
                // về `uid|token|cookie` và toàn bộ logic parse bên dưới.
                string pkg = Package(PlatformModel.Facebook);
                string authPath = "/data/data/" + pkg + "/app_light_prefs/" + pkg + "/authentication";
                for (int i = 0; i < 4; i++)
                {
                    if (i > 0) System.Threading.Thread.Sleep(600);
                    try { catData = client.ADB.Shell("su -c 'cat " + authPath + "'", 20); }
                    catch { catData = ""; }
                    if (string.IsNullOrEmpty(catData))
                    {
                        // Dự phòng: socket transport (đường cũ) nếu process trả rỗng.
                        try { catData = client.Shell("su -c cat " + authPath); }
                        catch { }
                    }
                    if (!string.IsNullOrEmpty(catData))
                    {
                        try
                        {
                            string rawText = Regex.Replace(catData, "[^\\u0020-\\u007E]", "|");
                            token = "EAA" + Regex.Match(rawText, "EAA(.*?)\\|").Groups[1].Value;

                        }
                        catch
                        {
                        }
                        try
                        {
                            uid = Regex.Match(catData, @"\""name\"":\""c_user\"",\""value\"":\""(?<cuser>\d+)").Groups["cuser"].Value;
                        }
                        catch
                        {

                        }
                        try
                        {
                            if (string.IsNullOrEmpty(uid))
                            {
                                string uidPattern = @"uid\s*\D*(\d{5,})";  // Bỏ qua ký tự lạ trước số UID

                                Match uidMatch = Regex.Match(catData, uidPattern);
                                if (uidMatch.Success)
                                {
                                    uid = uidMatch.Groups[1].Value;
                                }
                            }
                        }
                        catch
                        {

                        }
                        try
                        {
                            //string jsonCookies = Regex.Match(catData, @"session_cookies_string♥�(?<json>\[.*?\])♣").Groups["json"].Value;
                            string jsonCookies = Regex.Match(catData, @"session_cookies_string.*?(\[.*?\])").Groups[1].Value;

                            // Parse JSON
                            JsonArray cookies = JsonNode.Parse(jsonCookies)!.AsArray();

                            // Duyệt qua cookies và chọn các trường cần thiết
                            string cookieString = "";

                            foreach (var item in cookies)
                            {
                                string name = item["name"]?.ToString();
                                string value = item["value"]?.ToString();

                                // Chỉ giữ các cookie theo yêu cầu
                                if (name == "xs" || name == "fr" || name == "c_user" || name == "datr")
                                {
                                    cookieString += $"{name}={value}; ";
                                }
                            }
                            cookie = cookieString;
                        }
                        catch
                        {

                        }
                        try
                        {
                            if (string.IsNullOrEmpty(cookie))
                            {
                                string xs = string.Empty, fr = string.Empty, datr = string.Empty;
                                // Loại bỏ ký tự đặc biệt
                                string cleanInput = Regex.Replace(catData, @"[\u0000-\u001F]", "").Trim();

                                // Regex lấy UID
                                Match match = Regex.Match(cleanInput, @"session_key\s*=\s*([^\s]+)");
                                if (match.Success)
                                {
                                    xs = match.Groups[1].Value;
                                }
                                match = Regex.Match(cleanInput, @"fr\s*=\s*([^\s]+)");
                                if (match.Success)
                                {
                                    fr = match.Groups[1].Value;
                                }
                                match = Regex.Match(cleanInput, @"datr\s+([^\s]+)");
                                if (match.Success)
                                {
                                    datr = match.Groups[1].Value;
                                }
                                // Kiểm tra dữ liệu cần thiết
                                if (string.IsNullOrEmpty(uid))
                                    continue;

                                // Tạo cookie Facebook chuẩn
                                string fbCookie = $"c_user={uid}; xs={xs};" +
                                                  (!string.IsNullOrEmpty(fr) ? $" fr={fr};" : "") +
                                                  (!string.IsNullOrEmpty(datr) ? $" datr={datr};" : "");

                                cookie = fbCookie;
                            }
                        }
                        catch
                        {


                        }

                        if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(cookie) && !string.IsNullOrEmpty(uid))
                        {
                            break;
                        }
                    }
                }
            }
            catch
            {
            }
            return uid + "|" + token + "|" + cookie;
        }
        public static List<string> Regsiner_Facebook()
        {
            var xpaths = XpathManagerFacebook.Combine
                (
                    XpathType.CP282,
                    XpathType.Loading,
                    XpathType.Captcha,
                    XpathType.CP956,
                    XpathType.Logout,
                    XpathType.ExistEmail,
                    XpathType.Success,
                    XpathType.CashApp,
                    XpathType.Regsiner_Facebook,
                    XpathType.NavigationButton
                );
            return xpaths;
        }
        public static async Task SendImage(ADBClient client, string imagePath)
        {
            var s = client.Shell("content delete --uri content://media/external/images/media");
            client.Shell(" mkdir -p /sdcard/LT");
            client.Delay(2);

            string fileName = System.IO.Path.GetFileName(imagePath);
            var p = client.Push(imagePath, $"/sdcard/LT/{fileName}");

            client.Delay(1);
            client.Shell($"am broadcast -a android.intent.action.MEDIA_SCANNER_SCAN_FILE -d file:///sdcard/LT/{fileName}");
        }
        public static void DeleteImage(ADBClient client, string imagePath)
        {
            string fileName = System.IO.Path.GetFileName(imagePath);
            // Đường dẫn trên thiết bị Android
            string remotePath = $"/sdcard/LT/{fileName}";

            // Xóa tệp từ đường dẫn
            client.Shell($" rm {remotePath}");

            // Gửi broadcast để cập nhật thư viện phương tiện
            client.Shell($" am broadcast -a android.intent.action.MEDIA_SCANNER_SCAN_FILE -d file://{remotePath}");
        }
        public static async Task<string> GetUrlByObjectId(string object_id)
        {
            try
            {
                string url = $"https://www.facebook.com/{object_id}";
                var handler = new HttpClientHandler
                {
                    AllowAutoRedirect = false
                };
                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("");
                    client.DefaultRequestHeaders.Accept.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36 Edg/138.0.0.0");
                    client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9,vi;q=0.8");

                    HttpResponseMessage response = await client.GetAsync(url);

                    if (response.StatusCode == HttpStatusCode.Moved || response.StatusCode == HttpStatusCode.Found)
                    {
                        Uri redirectedUri = response.Headers.Location;

                        if (redirectedUri != null)
                        {
                            string redirectedUrl = redirectedUri.ToString();

                            if (!redirectedUrl.Contains("login") && url != redirectedUrl)
                            {
                                return redirectedUrl;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi: {ex.Message}");
            }
            return "";
        }

    }
}
