using Sunny.Subd.Core.Models;
using System.Collections.Concurrent;

namespace Sunny.Subd.Core.Utils
{
    /// <summary>
    /// Pandora music streaming app — XPath selectors verified on real device
    /// Device: Samsung Galaxy J7 (SM-J730G), Android 9 (SDK 28)
    /// App: com.pandora.android v2504.1.1 (25041105)
    /// Resolution: 1080x1920 (portrait) / 1920x1080 (landscape)
    /// </summary>
    public static class XpathManagerPandora
    {
        private static readonly ConcurrentDictionary<XpathType, List<string>> _xpathGroups = new();

        static XpathManagerPandora()
        {
            // ── Welcome screen ────────────────────────────────────────────
            _xpathGroups.TryAdd(XpathType.PandoraWelcomeScreen, new List<string>
            {
                "//*[@resource-id='com.pandora.android:id/welcome_sign_up_button']",
                "//*[@resource-id='com.pandora.android:id/welcome_log_in_button']",
                "//*[@text='Sign Up for Free']",
                "//*[@text='Log In']",
            });

            // ── Login / Register screen fields ────────────────────────────
            _xpathGroups.TryAdd(XpathType.InputUserName, new List<string>
            {
                "//*[@resource-id='com.pandora.android:id/email_editText']",
                "//android.widget.EditText[@resource-id='com.pandora.android:id/email_editText']",
                "//*[@text='Email']",
            });

            _xpathGroups.TryAdd(XpathType.InputPassword, new List<string>
            {
                "//*[@resource-id='com.pandora.android:id/password_editText']",
                "//android.widget.EditText[@password='true']",
                "//*[@text='Password']",
            });

            // ── Login flow buttons ─────────────────────────────────────────
            _xpathGroups.TryAdd(XpathType.PandoraLoginButtons, new List<string>
            {
                "//*[@resource-id='com.pandora.android:id/welcome_log_in_button']",
                "//*[@resource-id='com.pandora.android:id/secondary_cta' and contains(@text, 'Log in')]",
                "//*[@resource-id='com.pandora.android:id/cta']",
                "//*[@text='Log In']",
                "//*[@text='Continue']",
            });

            // ── Main screen (logged in) ────────────────────────────────────
            _xpathGroups.TryAdd(XpathType.Success, new List<string>
            {
                "//*[@resource-id='com.pandora.android:id/bottom_navigation']",
                "//*[@resource-id='com.pandora.android:id/search_bar']",
                "//*[@resource-id='com.pandora.android:id/create_station_fab']",
                "//*[@text='For You']",
                "//*[@text='My Collection']",
                "//*[@text='Profile']",
                "//*[@text='Search']",
                "//*[@text='Browse']",
            });

            // ── FTUE / Onboarding screens ──────────────────────────────────
            _xpathGroups.TryAdd(XpathType.PandoraFTUE, new List<string>
            {
                "//*[@resource-id='com.pandora.android:id/create_station_fab']",
                "//*[@resource-id='com.pandora.android:id/create_station_title']",
                "//*[@text='Start listening']",
                "//*[contains(@content-desc, 'Create Station')]",
                "//*[@text='Start Listening']",
            });

            // ── Search UI ──────────────────────────────────────────────────
            _xpathGroups.TryAdd(XpathType.PandoraSearchElements, new List<string>
            {
                "//*[@resource-id='com.pandora.android:id/search_bar']",
                "//*[@resource-id='com.pandora.android:id/search_view']",
                "//*[@resource-id='com.pandora.android:id/search_src_text']",
                "//*[@resource-id='com.pandora.android:id/tab_search']",
                "//*[@text='Search']",
            });

            _xpathGroups.TryAdd(XpathType.PandoraSearchInput, new List<string>
            {
                "//*[@resource-id='com.pandora.android:id/search_src_text']",
                "//android.widget.EditText[@resource-id='com.pandora.android:id/search_src_text']",
                "//android.widget.EditText[contains(@resource-id, 'search')]",
                "//*[@resource-id='com.pandora.android:id/search_plate']/android.widget.EditText",
            });

            // ── Search result filter tabs (content-desc based) ────────────
            _xpathGroups.TryAdd(XpathType.PandoraSoundTab, new List<string>
            {
                "//*[@content-desc='Songs']",
                "//*[@content-desc='ALL']",
                "//*[@text='SONGS']",
                "//*[@text='ALL']",
            });

            // ── Song result items ──────────────────────────────────────────
            _xpathGroups.TryAdd(XpathType.PandoraSongResult, new List<string>
            {
                "//*[contains(@text, 'Song -')]",
                "//*[@text='Shape of You']",
                "//android.widget.TextView[contains(@text, 'Song')]",
                "//*[contains(@content-desc, 'Play')]",
                "//*[@text='Play']",
                "//*[@resource-id='com.pandora.android:id/collection_data_holder']/*[1]",
            });

            // ── Play button ────────────────────────────────────────────────
            _xpathGroups.TryAdd(XpathType.PandoraPlayButton, new List<string>
            {
                "//*[@content-desc='Play']",
                "//*[contains(@content-desc, 'Play')]",
                "//*[@resource-id='com.pandora.android:id/play_button']",
            });

            // ── Like / Thumbs up ───────────────────────────────────────────
            _xpathGroups.TryAdd(XpathType.PandoraLikeButton, new List<string>
            {
                "//*[contains(@content-desc, 'Thumbs Up')]",
                "//*[contains(@content-desc, 'Thumbs')]",
            });

            // ── Back navigation ────────────────────────────────────────────
            _xpathGroups.TryAdd(XpathType.PandoraBackButton, new List<string>
            {
                "//*[@resource-id='com.pandora.android:id/back_button']",
                "//*[@content-desc='Back Button']",
                "//*[@content-desc='Navigate up']",
            });

            // ── Popup / Dismiss ────────────────────────────────────────────
            _xpathGroups.TryAdd(XpathType.PandoraPopupDismiss, new List<string>
            {
                "//*[@resource-id='com.pandora.android:id/voice_callout_dismiss']",
                "//*[@content-desc='Dismiss']",
                "//*[@text='Not Now']",
                "//*[@text='Later']",
                "//*[@text='Skip']",
            });

            // ── Error states ───────────────────────────────────────────────
            _xpathGroups.TryAdd(XpathType.WrongPassword, new List<string>
            {
                "//*[contains(@text, 'Invalid email or password')]",
                "//*[contains(@text, 'Password must be at least')]",
                "//*[contains(@text, 'incorrect')]",
            });
        }

        public static List<string> Get(XpathType group) =>
            _xpathGroups.TryGetValue(group, out var list) ? list : new List<string>();

        public static List<string> Combine(params object[] groupsOrXpaths)
        {
            var result = new List<string>();
            foreach (var item in groupsOrXpaths)
            {
                if (item is XpathType groupName)
                    result.AddRange(Get(groupName));
                else if (item is IEnumerable<string> list)
                    result.AddRange(list);
            }
            return result;
        }

        public static void AddCustomGroup(XpathType key, List<string> xpaths) =>
            _xpathGroups[key] = xpaths;

        #region Debug helpers

        /// <summary>Dump UI XML to file via ATX agent for debugging.</summary>
        public static async Task<string> DumpDebugXml(AutoAndroid.ADBClient client, string tag)
        {
            try
            {
                string xml = client.GetXMLSource();
                if (string.IsNullOrEmpty(xml) || xml.Length < 100)
                    return "";

                string dir = Path.Combine(AppContext.BaseDirectory, "logs", "pandora_xml");
                Directory.CreateDirectory(dir);
                string file = Path.Combine(dir, $"ui_{tag}_{DateTime.Now:yyyyMMdd_HHmmss}.xml");
                await File.WriteAllTextAsync(file, xml);
                System.Diagnostics.Debug.WriteLine($"[Pandora] XML: {file} ({xml.Length} chars)");
                return file;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Pandora] XML dump err: {ex.Message}");
                return "";
            }
        }

        /// <summary>Try to get current UI XML via ATX agent; returns empty if unavailable.</summary>
        public static string TryGetXml(AutoAndroid.ADBClient client)
        {
            try
            {
                // ATX agent survives Pandora's anti-automation (raw uiautomator dump gets killed)
                string xml = client.GetXMLSource();
                if (!string.IsNullOrEmpty(xml) && xml.Length > 200)
                    return xml;
            }
            catch { }

            return "";
        }

        /// <summary>Log all visible text elements for quick debugging.</summary>
        public static void DebugDumpAllTexts(AutoAndroid.ADBClient client, string label)
        {
            try
            {
                string xml = TryGetXml(client);
                if (string.IsNullOrEmpty(xml)) return;

                var textNodes = client.FindElements(1, xml, "//*[@text]");
                var descNodes = client.FindElements(1, xml, "//*[@content-desc]");

                System.Diagnostics.Debug.WriteLine($"[Pandora] === {label}: {textNodes.Count} texts, {descNodes.Count} descs ===");

                foreach (var n in textNodes.Take(25))
                {
                    string t = n.Attributes?["text"]?.Value ?? "";
                    string b = n.Attributes?["bounds"]?.Value ?? "";
                    if (!string.IsNullOrWhiteSpace(t))
                        System.Diagnostics.Debug.WriteLine($"  [text] \"{t}\" | {b}");
                }
                foreach (var n in descNodes.Take(25))
                {
                    string d = n.Attributes?["content-desc"]?.Value ?? "";
                    string b = n.Attributes?["bounds"]?.Value ?? "";
                    if (!string.IsNullOrWhiteSpace(d))
                        System.Diagnostics.Debug.WriteLine($"  [desc] \"{d}\" | {b}");
                }
            }
            catch { }
        }

        #endregion
    }

    /// <summary>Structured element info for debug output.</summary>
    public class ElementInfo
    {
        public string XPath { get; set; } = "";
        public string Text { get; set; } = "";
        public string ContentDesc { get; set; } = "";
        public string Bounds { get; set; } = "";
        public string ClassName { get; set; } = "";
        public string ResourceId { get; set; } = "";
        public string Clickable { get; set; } = "";
    }
}
