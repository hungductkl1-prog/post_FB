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
        public static void GithubDown(string url, string file)
        {
            DownLock.EnterWriteLock();
            try
            {
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

                using (var handler = new System.Net.Http.HttpClientHandler
                {
                    AllowAutoRedirect = true,
                    MaxAutomaticRedirections = 10
                })
                using (var client = new System.Net.Http.HttpClient(handler))
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                    client.Timeout = TimeSpan.FromMinutes(10);

                    using (var response = client.GetAsync(url, System.Net.Http.HttpCompletionOption.ResponseHeadersRead).Result)
                    {
                        response.EnsureSuccessStatusCode();

                        string contentType = response.Content.Headers.ContentType?.MediaType ?? "";
                        if (contentType.Contains("text/html"))
                        {
                            throw new Exception($"URL trả về HTML thay vì file: {contentType}");
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
            }
            finally
            {
                DownLock.ExitWriteLock();
            }
        }
        #endregion
    }
}
