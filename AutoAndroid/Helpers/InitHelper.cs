using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AutoAndroid
{
    public class InitHelper
    {
        private readonly string _abi;
        private readonly string _sdk;

        public readonly static string ATX_APP_VERSION = "2.3.3";
        private readonly static string ATX_AGENT_VERSION = "0.10.0";
        private ADBClient _client;
        public int _port;

        private readonly static ReaderWriterLockSlim DownLock = new ReaderWriterLockSlim();
        public readonly static string CACHE_PATH = $"{AppContext.BaseDirectory}\\cache";
        private readonly static string ATX_LISTEN_ADDR = "127.0.0.1:7912";
        private readonly static string GITHUB_BASEURL = "https://github.com/openatx";
        private readonly static string GITHUB_DOWN_APK_PATH = "/android-uiautomator-server/releases/download/";
        private readonly static string GITHUB_DOWN_AGENT_PATH = "/atx-agent/releases/download/";
        public readonly static string ANDROID_LOCAL_TMP_PATH = "/data/local/tmp/";
        private readonly static string ATX_AGENT_PATH = "/data/local/tmp/atx-agent";
        public readonly static string[] ATX_APKS = new string[2] { "app-uiautomator", "app-uiautomator-test" };
        private readonly static Dictionary<string, string> ATX_AGENT_FILE_DICT = new Dictionary<string, string>() {
                { "armeabi-v7a", "atx-agent_{0}_linux_armv7.tar.gz" },
                { "arm64-v8a", "atx-agent_{0}_linux_arm64.tar.gz" },
                { "armeabi", "atx-agent_{0}_linux_armv6.tar.gz" },
                { "x86", "atx-agent_{0}_linux_386.tar.gz" },
                { "x86_64", "atx-agent_{0}_linux_386.tar.gz" },
            };
        private string ATX_AGENT_DOWN_URL
        {
            get
            {
                if (_abi == null)
                {
                    _client.LogHelper.Log("CPU not exists");
                    return string.Empty;
                }
                if (!ATX_AGENT_FILE_DICT.ContainsKey(_abi))
                {
                    _client.LogHelper.Log("CPU not support");
                    return string.Empty;
                }
                string file = ATX_AGENT_FILE_DICT[_abi];
                return $"{GITHUB_BASEURL}{GITHUB_DOWN_AGENT_PATH}{ATX_AGENT_VERSION}/{string.Format(file, ATX_AGENT_VERSION)}";
            }
        }
        private string ATX_AGENT_CAHCE_FILE
        {
            get
            {
                if (_abi == null)
                {
                    _client.LogHelper.Log("CPU not exists");
                    return string.Empty;
                }
                if (!ATX_AGENT_FILE_DICT.ContainsKey(_abi))
                {
                    _client.LogHelper.Log("CPU not support");
                    return string.Empty;
                }
                string file = string.Format(ATX_AGENT_FILE_DICT[_abi], ATX_AGENT_VERSION);
                return $"{CACHE_PATH}\\atx_agent/{ATX_AGENT_VERSION}/{file}";
            }
        }
        public InitHelper(ADBClient client)
        {
            _client = client;
            _port = client.Device.Port;
            _abi = client.GetProp("ro.product.cpu.abi").Trim();
            _sdk = client.GetProp("ro.build.version.sdk").Trim();
        }

        #region atx-agent
        public void SetupAtxAgent(bool restart = false)
        {
            if (CheckAtxAgentVersion() && !restart)
            {
                return;
            }
            _client.KillProcessByName("atx-agent");
            _client.Shell(ATX_AGENT_PATH, "server", "--stop");
            Thread.Sleep(500);
            if (IsAtxAgentOutdated())
            {
                GithubDown(ATX_AGENT_DOWN_URL, ATX_AGENT_CAHCE_FILE);
                string file = Path.GetDirectoryName(ATX_AGENT_CAHCE_FILE) + $"\\{_abi}\\atx-agent";
                if (!File.Exists(file))
                {
                    FileHelper.UnzipTgz(ATX_AGENT_CAHCE_FILE, Path.GetDirectoryName(file));
                }
                _client.RunTime($"PUSH {file}", () => _client.Push(file, ATX_AGENT_PATH));
            }
            _client.Shell(ATX_AGENT_PATH, "server", "--nouia", "-d", "--addr", ATX_LISTEN_ADDR);
            int size = 10;
            while (!CheckAtxAgentVersion())
            {
                size--;
                if (size <= 0)
                {
                    _client.LogHelper.Log($"Init atx-agent fail");
                    return;
                }
            }

        }

        public bool CheckAtxAgentVersion()
        {
            try
            {
                int port = _client.ForwardPort(7912, _port);
                if (port == -1)
                {
                    return false;
                }
                using (SocketHelper socket = SocketHelper.Create("127.0.0.1", port))
                {
                    var result = socket.HttpGet("/version");
                    if (result == null || result.Code != 200)
                    {
                        return false;
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                _client.LogHelper.Log(ex.Message);
            }
            return false;
        }

        private bool IsAtxAgentOutdated()
        {
            try
            {
                string version = _client.Shell(ATX_AGENT_PATH, "version");
                _client.LogHelper.Log($"AtxAgent version: {version}");
                if (version == "dev")
                {
                    return false;
                }
                var nv = ATX_AGENT_VERSION.Split('.');
                var ov = version.Split('.');
                if (nv[1] != ov[1])
                {
                    return true;
                }
                return int.Parse(ov[2]) < int.Parse(nv[2]);
            }
            catch (Exception)
            {
                return true;
            }
        }
        #endregion

        #region atx-app
        public void SetupAtxApp()
        {
            if (IsAtxAppOutdated())
            {
                _client.Shell("pm", "uninstall", "com.github.uiautomator");
                _client.Shell("pm", "uninstall", "com.github.uiautomator.test");
                foreach (string app in ATX_APKS)
                {
                    string tmp = $"{ANDROID_LOCAL_TMP_PATH}{app}.apk";
                    _client.Shell("rm", tmp);
                    string url = $"{GITHUB_BASEURL}{GITHUB_DOWN_APK_PATH}{ATX_APP_VERSION}/{app}.apk";
                    string file = $"{CACHE_PATH}apk/{ATX_APP_VERSION}/{app}.apk";
                    GithubDown(url, file);
                    _client.RunTime($"PUSH {file}", () => _client.Push(file, tmp));
                    _client.RunTime($"PUSH {file}", () => _client.Shell("pm", "install", "-r", tmp));
                }
            }
        }

        public bool IsAtxAppOutdated()
        {
            var apk_debug = _client.AppInfo("com.github.uiautomator");
            var apk_debug_test = _client.AppInfo("com.github.uiautomator.test");
            if (apk_debug == null || apk_debug_test == null)
            {
                return true;
            }
            if (apk_debug.VersionName != ATX_APP_VERSION)
            {
                return true;
            }
            if (apk_debug.Signature != apk_debug_test.Signature)
            {
                return true;
            }
            return false;
        }
        #endregion

        #region minicap
        public void SetupMinicap()
        {
            if (_abi == "x86")
            {
                _client.LogHelper.Log("abi:x86 not supported well, skip install minicap");
                return;
            }
            if (int.Parse(_sdk) > 30)
            {
                _client.LogHelper.Log("Android R (sdk:30) has no minicap resource");
                return;
            }
            string base_url = $"{GITHUB_BASEURL}/stf-binaries/raw/0.3.0/node_modules/@devicefarmer/minicap-prebuilt/prebuilt/";
            string result = _client.Shell("ls", "-a", "/data/local/tmp");
            var list = new List<string>(result.Split(' '));
            if (!list.Contains("minicap.so"))
            {
                string so_url = $"{base_url}{_abi}/lib/android-{_sdk}/minicap.so";
                string so_file = $"{CACHE_PATH}minicap/{_abi}/minicap.so";
                GithubDown(so_url, so_file);
                _client.RunTime($"PUSH {ANDROID_LOCAL_TMP_PATH}", () => _client.Push(so_file, $"{ANDROID_LOCAL_TMP_PATH}minicap.so"));
            }
            if (!list.Contains("minicap"))
            {
                string minicap_url = $"{base_url}{_abi}/bin/minicap";
                string minicap_file = $"{CACHE_PATH}minicap/{_abi}/minicap";
                GithubDown(minicap_url, minicap_file);
                _client.RunTime($"PUSH {ANDROID_LOCAL_TMP_PATH}", () => _client.Push(minicap_file, $"{ANDROID_LOCAL_TMP_PATH}minicap"));
            }
        }
        #endregion

        #region minitouch
        public void SetupMinitouch()
        {
            string result = _client.Shell("ls", "-a", "/data/local/tmp");
            var list = new List<string>(result.Split(' '));
            if (!list.Contains("minitouch"))
            {
                string base_url = $"{GITHUB_BASEURL}/stf-binaries/raw/0.3.0/node_modules/@devicefarmer/minitouch-prebuilt/prebuilt/{_abi.Trim()}/bin/minitouch";
                string minitouch_file = Path.Combine(CACHE_PATH, "minitouch", _abi.Trim(), "minitouch");
                GithubDown(base_url, minitouch_file);
                _client.RunTime($"SetupMinitouch ", () => _client.Push(minitouch_file, $"{ANDROID_LOCAL_TMP_PATH}minitouch"));
            }
        }
        #endregion

        #region 安装/卸载/重装
        public void Install()
        {

            SetupMinitouch();
            SetupMinicap();
            SetupAtxApp();
            SetupAtxAgent();
        }

        public void Reinstall(bool clear = false)
        {
            if (clear)
            {
                if (Directory.Exists(CACHE_PATH))
                {
                    try
                    {
                        Directory.Delete(CACHE_PATH, true);
                    }
                    catch (Exception ex)
                    {
                        _client.LogHelper.Log(ex.Message);
                    }
                }
            }
            Uninstall();
            Install();
        }

        public void Uninstall()
        {
            _client.KillProcessByName("atx-agent");
            _client.Shell(ATX_AGENT_PATH, "server", "--stop");
            _client.Shell("rm", ATX_AGENT_PATH);
            _client.Shell("rm", $"{ANDROID_LOCAL_TMP_PATH}minicap");
            _client.Shell("rm", $"{ANDROID_LOCAL_TMP_PATH}minicap.so");
            _client.Shell("rm", $"{ANDROID_LOCAL_TMP_PATH}minitouch");
            foreach (string app in ATX_APKS)
            {
                _client.Shell("rm", $"{ANDROID_LOCAL_TMP_PATH}{app}.apk");
            }
            _client.Shell("pm", "uninstall", "com.github.uiautomator");
            _client.Shell("pm", "uninstall", "com.github.uiautomator.test");
        }
        #endregion

        #region 下载
        private const string DOWN_UA = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

        /// <summary>
        /// Dùng curl.exe (có sẵn từ Windows 10 1803+) cho các CDN check JA3 TLS fingerprint.
        /// HttpClient của .NET dùng Schannel có fingerprint khác Chrome → bị Cloudflare/winudf reject 403.
        /// </summary>
        private static bool DownByCurl(string url, string tmpFile, string referer)
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "curl.exe",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                };
                psi.ArgumentList.Add("-sS");
                psi.ArgumentList.Add("-L");
                psi.ArgumentList.Add("--max-redirs"); psi.ArgumentList.Add("10");
                psi.ArgumentList.Add("--fail");
                psi.ArgumentList.Add("--retry"); psi.ArgumentList.Add("2");
                psi.ArgumentList.Add("-A"); psi.ArgumentList.Add(DOWN_UA);
                if (!string.IsNullOrEmpty(referer))
                {
                    psi.ArgumentList.Add("-H"); psi.ArgumentList.Add("Referer: " + referer);
                }
                psi.ArgumentList.Add("-o"); psi.ArgumentList.Add(tmpFile);
                psi.ArgumentList.Add(url);

                using (var p = System.Diagnostics.Process.Start(psi))
                {
                    p.WaitForExit(15 * 60 * 1000);
                    return p.ExitCode == 0 && File.Exists(tmpFile) && new FileInfo(tmpFile).Length > 0;
                }
            }
            catch
            {
                return false;
            }
        }

        public static void GithubDown(string url, string file)
        {
            DownLock.EnterWriteLock();
            try
            {
                // .NET Framework mặc định KHÔNG bật TLS 1.2. Nhiều CDN (Cloudflare, winudf, apkpure)
                // sẽ reject TLS 1.0/1.1 thẳng tay → biểu hiện ra ngoài là 403/handshake fail.
                try
                {
                    System.Net.ServicePointManager.SecurityProtocol |=
                        System.Net.SecurityProtocolType.Tls12 | (System.Net.SecurityProtocolType)3072 /* Tls13 */;
                }
                catch { }

                string path = Path.GetDirectoryName(file)?.Trim();
                if (!string.IsNullOrEmpty(path) && !Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                if (File.Exists(file))
                {
                    return;
                }

                string tmpFile = file + ".tmp";

                // Tự suy luận origin (cho Referer) từ host gốc.
                string originReferer = null;
                bool useCurl = false;
                try
                {
                    string host0 = new Uri(url).Host.ToLowerInvariant();
                    if (host0.Contains("apkpure.com") || host0.EndsWith("winudf.com"))
                    {
                        originReferer = "https://apkpure.com/";
                        useCurl = true; // apkpure/winudf check JA3 fingerprint, .NET HttpClient luôn 403
                    }
                    else if (host0.Contains("apkmirror.com"))
                    {
                        originReferer = "https://www.apkmirror.com/";
                        useCurl = true;
                    }
                    else if (host0.Contains("apkcombo"))
                    {
                        originReferer = "https://apkcombo.com/";
                        useCurl = true;
                    }
                }
                catch { }

                // Fallback bằng curl.exe cho các host check JA3 (CloudFlare bot detection).
                if (useCurl && DownByCurl(url, tmpFile, originReferer))
                {
                    File.Move(tmpFile, file);
                    return;
                }

                // TỰ follow redirect: HttpClient của .NET Framework có thể strip header
                // (User-Agent / Referer) khi redirect cross-host → CDN nhận diện bot và trả 403.
                using (var handler = new System.Net.Http.HttpClientHandler
                {
                    AllowAutoRedirect = false,
                    UseCookies = true,
                    CookieContainer = new System.Net.CookieContainer(),
                    AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
                })
                using (var client = new System.Net.Http.HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromMinutes(15);

                    string currentUrl = url;
                    string currentReferer = originReferer;
                    System.Net.Http.HttpResponseMessage response = null;

                    for (int hop = 0; hop < 10; hop++)
                    {
                        var req = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, currentUrl);
                        // Dùng TryAddWithoutValidation để tránh parser .NET im lặng drop UA.
                        req.Headers.TryAddWithoutValidation("User-Agent", DOWN_UA);
                        req.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,application/octet-stream;q=0.9,image/avif,image/webp,*/*;q=0.8");
                        req.Headers.TryAddWithoutValidation("Accept-Language", "vi-VN,vi;q=0.9,en-US;q=0.8,en;q=0.7");
                        req.Headers.TryAddWithoutValidation("Upgrade-Insecure-Requests", "1");
                        req.Headers.TryAddWithoutValidation("Sec-Fetch-Dest", "document");
                        req.Headers.TryAddWithoutValidation("Sec-Fetch-Mode", "navigate");
                        req.Headers.TryAddWithoutValidation("Sec-Fetch-Site", "none");
                        req.Headers.TryAddWithoutValidation("Sec-Fetch-User", "?1");
                        if (!string.IsNullOrEmpty(currentReferer))
                        {
                            req.Headers.TryAddWithoutValidation("Referer", currentReferer);
                        }

                        response = client.SendAsync(req, System.Net.Http.HttpCompletionOption.ResponseHeadersRead).Result;

                        int status = (int)response.StatusCode;
                        if (status >= 300 && status < 400 && response.Headers.Location != null)
                        {
                            Uri next = response.Headers.Location.IsAbsoluteUri
                                ? response.Headers.Location
                                : new Uri(new Uri(currentUrl), response.Headers.Location);

                            currentReferer = currentUrl; // Referer = URL trước
                            currentUrl = next.ToString();
                            response.Dispose();
                            continue;
                        }

                        break;
                    }

                    using (response)
                    {
                        if (response == null || !response.IsSuccessStatusCode)
                        {
                            int code = response == null ? 0 : (int)response.StatusCode;
                            throw new Exception($"Download fail HTTP {code} - finalUrl={currentUrl}");
                        }

                        string contentType = response.Content.Headers.ContentType?.MediaType ?? "";
                        if (contentType.Contains("text/html"))
                        {
                            throw new Exception($"URL trả về HTML thay vì file: {contentType} - finalUrl={currentUrl}");
                        }

                        long? expectedSize = response.Content.Headers.ContentLength;

                        using (var stream = response.Content.ReadAsStreamAsync().Result)
                        using (var fs = new FileStream(tmpFile, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            byte[] buffer = new byte[81920];
                            int read;
                            long totalRead = 0;
                            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                fs.Write(buffer, 0, read);
                                totalRead += read;
                            }

                            if (expectedSize.HasValue && totalRead != expectedSize.Value)
                            {
                                throw new Exception($"Tải không đủ: {totalRead}/{expectedSize.Value} bytes");
                            }
                        }
                    }
                }

                File.Move(tmpFile, file);
            }
            catch (Exception ex)
            {
                if (File.Exists(file))
                    File.Delete(file);
                if (File.Exists(file + ".tmp"))
                    File.Delete(file + ".tmp");
                throw; // ném lại để caller biết, thay vì nuốt im lặng
            }
            finally
            {
                DownLock.ExitWriteLock();
            }
        }
        #endregion
    }
}
