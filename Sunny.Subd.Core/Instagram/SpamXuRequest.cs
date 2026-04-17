using System.Text.Json.Nodes;
using Sunny.Subd.Core.Proxies;
using Sunny.Subd.Core.Utils;
using Sunny.Subdy.Common.API;
using Sunny.Subdy.Common.API.Jobs.VipIG;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Json;
using Sunny.Subdy.Common.Logs;
using Sunny.Subdy.Common.Models;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;
using System.Net;
using System.Text.RegularExpressions;

namespace Sunny.Subd.Core.Instagram
{
    public class SpamXuRequest
    {
        private readonly JsonHelper _settingScript, _settingGeneral;
        private readonly Account _account;
        private readonly CancellationToken _ct;
        private readonly VipIGClient _client = new();
        private readonly Dictionary<string, int> _jobWorking = new();
        private string _proxy = string.Empty;
        private string _cookieVipIG = string.Empty;
        private string _state = string.Empty;
        private readonly Dictionary<string, int> _typeJob = new();
        private readonly AccountContext _accountContext = new AccountContext();
        private InstagramRequest _instagramClient;
        public SpamXuRequest(Account account, JsonHelper settingGeneral, JsonHelper settingScript, CancellationToken ct)
        {
            _account = account;
            _settingGeneral = settingGeneral;
            _settingScript = settingScript;
            _ct = ct;

        }
        public async Task RunAsync()
        {
            try
            {
                await PrepareAccountAsync();
                await RunJobLoopAsync();
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, 1);
                _accountContext.Update(_account);
            }
        }
        private async Task PrepareAccountAsync()
        {
            if (string.IsNullOrEmpty(_account.TokenJob))
                throw new Exception("Không có token vipig.net");

            SetStatus("Đăng nhập vipig.net", 2);
            string accVipIG = await _client.LoginByToken(_account.TokenJob);
            if (string.IsNullOrEmpty(accVipIG))
                throw new Exception("Đăng nhập vipig.net lỗi.");

            _cookieVipIG = accVipIG.Split('|')[2];
            _account.Result = accVipIG.Split('|')[1];

            await ChangeProxyAsync();
            _instagramClient = new InstagramRequest(_account, _proxy);

        ReFail:
            bool isValid = false;
            string username = string.Empty;
            try
            {
                var (check, name, _) = await _instagramClient.ValidateInstagramCookie(_account.Cookie);
                isValid = check;
                if (!string.IsNullOrEmpty(name))
                {
                    username = name;
                }
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, 1);
                if (ex.Message.Contains("https://www.instagram.com/accounts/suspended"))
                {
                    _account.State = "DIE";
                    throw new Exception("Tài khoản bị checkpoint.");
                }
            }
            if (!isValid)
            {
                bool isLogin = _settingScript.GetBooleanValue("checkBox4", true);
                if (!isLogin)
                {
                    throw new Exception("Cookie không hợp lệ");
                }
                if (string.IsNullOrEmpty(_account.Uid) || string.IsNullOrEmpty(_account.Password))
                {
                    throw new Exception("Tài khoản không có username hoặc password.");
                }
                string value = await _instagramClient.Login();
                if (string.IsNullOrEmpty(value) || !value.Contains("|"))
                {
                    throw new Exception("Tài khoản đăng nhập thất bại.");
                }
                _account.Cookie = value.Split("|")[0];
                _account.Token = value.Split("|")[1];
                _accountContext.Update(_account);
                goto ReFail;
            }
            _account.Uid = username;
            await ConfigureAccountAsync();
        }
        private async Task ConfigureAccountAsync()
        {
            _account.Status = "Cấu hình tài khoản";
            bool configured;

            if (!_settingScript.GetBooleanValue("check_AddAccount", false))
            {
                configured = await _client.CauHinh(_account.Uid);
                if (!configured) throw new Exception("Cấu hình không hợp lệ");

                string idfb = Regex.Match(_account.Cookie, "ds_user_id=([^;]+)").Groups[1].Value;
                if (await _client.DatNick(idfb) != 1)
                    throw new Exception($"Cần thêm nick: {_account.FullName} vào trước khi chạy");
            }
            else
            {
                _account.Status = "Cấu hình tài khoản nhanh";
                if (!await _client.CauHinhNhanh(_account.Uid))
                    throw new Exception($"Cần thêm nick: {_account.FullName} vào trước khi chạy");
            }
        }
        private async Task RunJobLoopAsync()
        {
            if (_settingScript.GetBooleanValue("checkBox1", true)) _typeJob["like"] = 0;
            if (_settingScript.GetBooleanValue("checkBox13", true)) _typeJob["follow"] = 0;
            _jobWorking["faillientiep"] = 0;
            _jobWorking["tym_max"] = SubdyHelper.RandomValue(_settingScript.GetIntType("numericUpDown3", 100), _settingScript.GetIntType("numericUpDown4", 500));
            _jobWorking["tym"] = 0;
            _jobWorking["sub"] = 0;
            _jobWorking["sub_max"] = SubdyHelper.RandomValue(_settingScript.GetIntType("numericUpDown5", 100), _settingScript.GetIntType("numericUpDown2", 500));
            string csrf = await EnsureCsrfTokenAsync(_account.Cookie, _proxy);

            while (true)
            {
                await CheckStopConditionsAsync();
                var (isValid, _, _) = await _instagramClient.ValidateInstagramCookie(_account.Cookie);
                if (!isValid)
                    throw new Exception("Cookie không hợp lệ");
                string type = SubdyHelper.GetStringRandom(_typeJob.Keys.ToList());
                SetStatus($"Get jobs {type}", 2);

                List<JobModel> jobs = await _client.GetJobInstagram(type);
                if (jobs == null || jobs.Count == 0)
                {
                    SetStatus("Không có job nào để làm", 1);
                    if (_settingScript.GetBooleanValue("checkBox3"))
                        throw new Exception("Chuyển tài khoản khi hết job");
                    continue;
                }

                await HandleJobAsync(type, jobs, csrf);
            }
        }
        private async Task HandleJobAsync(string type, List<JobModel> jobs, string csrf)
        {
            string message = string.Empty;
            switch (type)
            {
                case "tym":
                    foreach (var task in jobs)
                    {
                        string result = await _instagramClient.Like(task.FromId, _account.Cookie, csrf);
                        var json = JsonNode.Parse(result)!.AsObject();
                        string status = json["status"]?.GetValue<string>();
                        if (status != "ok")
                        {
                            _jobWorking["faillientiep"]++;
                            SetStatus($"[{_jobWorking[type]}/{_jobWorking["tym_max"]}] TYM LỖI", 1);
                            message = $"[{_jobWorking[type]}/{_jobWorking["sub_max"]}] TYM LỖI";
                        }
                        else
                        {
                            _jobWorking["faillientiep"] = 0;
                            _jobWorking[type]++;
                            SetStatus("TYM THÀNH CÔNG", 2);
                            message = $"[{_jobWorking[type]}/{_jobWorking["sub_max"]}] TYM THÀNH CÔNG";
                            var reward = await _client.ClaimLikeReward(task.ObjectId);
                            SetStatus($"[{_jobWorking[type]}/{_jobWorking["tym_max"]}] {reward["mess"] ?? reward["error"]}", 2);
                            message = $"[{_jobWorking[type]}/{_jobWorking["tym_max"]}] {reward["mess"] ?? reward["error"]}";
                            _account.Result = await _client.GetBalance();
                        }
                        await CheckStopConditionsAsync();
                        await DelayMessageAsync(SubdyHelper.RandomValue(
                            _settingScript.GetIntType("nudJobDelayFrom", 5),
                            _settingScript.GetIntType("nudJobDelayTo", 10)
                        ), $"Delay tương tác tiếp theo. [{message}]", 2);
                    }
                    break;

                case "sub":
                    var batches = jobs.Select(t => t.JobId).Chunk(6);
                    foreach (var batch in batches)
                    {
                        if (batch.Length < 5) continue;
                        string idList = string.Join(",", batch);
                        bool hasError = false;
                        foreach (var id in batch)
                        {

                            try
                            {
                                string result = await _instagramClient.Follow(id, _account.Cookie, csrf);
                                var json = JsonNode.Parse(result)!.AsObject();
                                if (json["status"]?.GetValue<string>() != "ok")
                                {
                                    hasError = true;
                                    _jobWorking["faillientiep"]++;
                                    SetStatus($"[{_jobWorking[type]}/{_jobWorking["sub_max"]}] FOLLOW LỖI", 1);
                                    message = $"[{_jobWorking[type]}/{_jobWorking["sub_max"]}] FOLLOW LỖI";
                                    break;
                                }
                                _jobWorking["faillientiep"] = 0;
                                _jobWorking[type]++;
                                SetStatus($"[{_jobWorking[type]}/{_jobWorking["sub_max"]}] FOLLOW THÀNH CÔNG", 2);
                                message = $"[{_jobWorking[type]}/{_jobWorking["sub_max"]}] FOLLOW THÀNH CÔNG";
                            }
                            catch (Exception ex)
                            {
                                SetStatus($"Lỗi khi follow: {ex.Message}", 1);
                                message = $"Lỗi khi follow: {ex.Message}";
                                _jobWorking["faillientiep"]++;
                            }

                            await DelayMessageAsync(SubdyHelper.RandomValue(
                                _settingScript.GetIntType("nudJobDelayFrom", 5),
                                _settingScript.GetIntType("nudJobDelayTo", 10)
                            ), $"Delay tương tác tiếp theo. [{message}]", 2);
                        }
                        if (!hasError)
                        {
                            await _client.ClaimFollowReward(idList);
                            _account.Result = await _client.GetBalance();
                        }
                        await CheckStopConditionsAsync();
                    }
                    break;
            }
        }
        private async Task<string> EnsureCsrfTokenAsync(string cookie, string proxy)
        {
            string csrf = _instagramClient.GetCsrfToken(cookie);
            if (!string.IsNullOrEmpty(csrf)) return csrf;

            string refreshed = await _instagramClient.GetCookie(cookie);
            csrf = _instagramClient.GetCsrfToken(refreshed);
            if (string.IsNullOrEmpty(csrf))
                throw new Exception("Lấy csrf token thất bại");

            return csrf;
        }
        private async Task CheckStopConditionsAsync()
        {
            if (_ct.IsCancellationRequested)
                throw new Exception("Dừng");

            int maxtotal = _jobWorking["tym_max"] + _jobWorking["sub_max"];
            int total = _jobWorking["tym"] + _jobWorking["sub"];

            if (_jobWorking["faillientiep"] >= _settingScript.GetIntType("numericUpDown6", 10))
            {
                throw new Exception($"Dừng tài khoản khi làm thất bại liên tiếp {_jobWorking["faillientiep"]} job");
            }

            if (total % 6 == 0)
            {
                string message = $"[{_account.Uid}] Trừ 1 xu đang chạy vipig.net.";
                (string status, string coin) = LamToolClient.SubtractBalance(Globals.User.UserName, 1, message);

                if (status.Contains("error", StringComparison.OrdinalIgnoreCase))
                {
                    _account.Status = status;
                    throw new Exception(status);
                }
            }

            if (_settingScript.GetBooleanValue("checkBox7", true) &&
               total >= maxtotal)
            {
                await DelayMessageAsync(_settingScript.GetIntType("numericUpDown23", 30), "Đã làm đủ job của 1 ngày", 2);
                throw new Exception("Đã làm đủ job của 1 ngày");
            }

            if (_typeJob.ContainsKey("like") && _jobWorking.TryGetValue("tym", out int tym) &&
                tym >= _jobWorking["sub_max"])
            {
                _typeJob.Remove("like");
            }

            if (_typeJob.ContainsKey("follow") && _jobWorking.TryGetValue("sub", out int sub) &&
                sub >= _jobWorking["sub_max"])
            {
                _typeJob.Remove("follow");
            }

            if (!_typeJob.Any())
                throw new Exception("Hết loại job cần làm!");

        }

        private async Task DelayMessageAsync(int second, string message, int color)
        {
            for (int i = 1; i <= second; i++)
            {
                SetStatus($"[{i}/{second}]... {message}", color);
                await Task.Delay(1000);
            }
        }
        private void SetStatus(string status, int color)
        {
            if (_account != null)
            {
                _account.Status = status;
                _account.RecentInteraction = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                _account.ColorType = color;
            }

        }
        private string GetProxyType()
        {
            try
            {
                int index = _settingGeneral.GetIntType("cbb_ListTypeProxy", 0);
                return index >= 0 ? ProxyService.ProxyTypes[index] : ProxyService.NoIP;
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                SetStatus(ex.Message, 1);
                return ProxyService.NoIP;
            }
        }
        private async Task<string> GetProxyFromServiceAsync(Func<string, Task<string>> newProxyFunc, Func<string, Task<string>> getProxyFunc)
        {
            string key = ProxyService.GetProxy();
            return await newProxyFunc(key) ?? await getProxyFunc(key);
        }
        private async Task<string> GetCustomProxyAsync()
        {
            string line = ProxyService.GetProxy();
            if (string.IsNullOrEmpty(line)) return string.Empty;
            var parts = line.Split('|');
            string proxy = parts[0].Trim();
            string link = parts.Length > 1 ? parts[1].Trim() : string.Empty;
            if (!string.IsNullOrEmpty(link)) await RequestService.Get(link);
            return proxy;
        }
        private HttpClient CreateClient(string proxy = null)
        {
            var handler = new HttpClientHandler
            {
                CookieContainer = new CookieContainer(),
                UseCookies = true
            };

            if (!string.IsNullOrWhiteSpace(proxy))
            {
                var proxyUri = new Uri(proxy);
                var webProxy = new WebProxy(proxyUri.Host, proxyUri.Port);

                if (!string.IsNullOrWhiteSpace(proxyUri.UserInfo))
                {
                    var userInfo = proxyUri.UserInfo.Split(':');
                    if (userInfo.Length == 2)
                    {
                        webProxy.Credentials = new NetworkCredential(userInfo[0], userInfo[1]);
                    }
                }

                handler.UseProxy = true;
                handler.Proxy = webProxy;
            }

            return new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(60)
            };
        }
        private async Task ChangeProxyAsync()
        {
            SetStatus("Đang làm", 2);
            string proxy = string.Empty;
            var proxyType = GetProxyType();
            SetStatus($"Loại: [{proxyType}] - ", 2);
            switch (proxyType)
            {
                case ProxyService.NoIP:
                    return;
                case ProxyService.Mobile4G:
                    return;
                case ProxyService.KiotProxy:
                    proxy = await GetProxyFromServiceAsync(ProxyKiot.NewProxy, ProxyKiot.GetProxy);
                    break;
                case ProxyService.WWProxy:
                    proxy = await GetProxyFromServiceAsync(ProxyWWW.NewProxy, ProxyWWW.GetProxy);
                    break;
                case ProxyService.ProxyMart:
                    proxy = await GetProxyFromServiceAsync(ProxyMart.NewProxy, ProxyMart.GetProxy);
                    break;
                case ProxyService.CustomProxy:
                    proxy = await GetCustomProxyAsync();
                    break;
                case ProxyService.ProxyAssigned:
                    proxy = _account.Proxy;
                    break;
            }
            SetStatus($"Loại: [{proxyType}] - [{proxy}]", 2);
            if (!string.IsNullOrEmpty(proxy))
            {
                string ip = await CheckProxyAsync(proxy);
                _account.IP = ip;
                _proxy = proxy;
            }
            int timeDelay = _settingGeneral.GetIntType("numericUpDown3", 10);
            await DelayMessageAsync(timeDelay, "Delay kết nối.", 2);
        }
        private async Task<string> CheckProxyAsync(string proxy)
        {
            try
            {
                using (var client = CreateClient(proxy))
                {
                    var response = await client.GetAsync("https://api64.ipify.org/?format=json");
                    if (response.IsSuccessStatusCode)
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        string pattern = @"""ip""\s*:\s*""([\d\.]+)""";
                        var match = Regex.Match(content, pattern);

                        if (match.Success)
                        {
                            string ip = match.Groups[1].Value;
                            return ip;
                        }
                        throw new Exception("Không tìm thấy IP trong response.");
                    }
                    throw new Exception($"Lỗi connect proxy: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi connect proxy: {ex.Message}");
            }
        }








    }
}
