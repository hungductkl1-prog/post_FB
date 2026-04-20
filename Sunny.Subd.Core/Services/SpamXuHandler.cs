using AntdUI;
using AutoAndroid;
using Org.BouncyCastle.Asn1.Utilities;
using Sunny.Subd.Core.Models;
using Sunny.Subd.Core.Services;
using Sunny.Subd.Core.Utils;
using Sunny.Subdy.Common.API;
using Sunny.Subdy.Common.API.Captchas;
using Sunny.Subdy.Common.API.Jobs;
using Sunny.Subdy.Common.API.Jobs.GoLike;
using Sunny.Subdy.Common.API.Jobs.TuongTacCheo;
using Sunny.Subdy.Common.API.Jobs.VipIG;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Json;
using Sunny.Subdy.Common.Logs;
using Sunny.Subdy.Common.Models;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;

using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using static System.ComponentModel.Design.ObjectSelectorEditor;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

namespace Sunny.Subd.Core.Facebook.ScriptActions
{
    public class SpamXuHandler : MainService
    {
        public SpamXuHandler(string platform, ADBClient device, ConfigModel config, CancellationToken ct, JsonHelper settingScriptAction, Account account)
            : base(platform, device, config, ct)
        {
            _settingScriptAction = settingScriptAction;
            _account = account;

        }
        private Dictionary<string, int> _doJobInfo = new Dictionary<string, int>();
        private string _jobService;
        private HashSet<string> _job_types = new();
        private Stopwatch _stopwatchScriptAction = new();
        private ManualResetEventSlim waitForDoJobStart = new(false);
        private int _timeoutScriptAction, _stopJobTatolAccount, _stopJobBlockTuongTac, _stopJobFail, _removeJob, _stopJobToday;
        private int _blockJobTuongTac, _jobFail_LienTiep, _countJob;
        private string _typeJob = string.Empty;
        private string _tokenJobService = string.Empty;
        private bool _isStop = false;
        private JobHistoryContext _jobHistoryContext = new JobHistoryContext();
        private Dictionary<string, string> _infoAccountService = new Dictionary<string, string>();
        private string _cookieService = string.Empty;
        private string _jobPrefix = string.Empty;
        private async Task<List<JobModel>> GetJob()
        {

            List<JobModel> jobs = new List<JobModel>();
            string error = string.Empty;
            int index = _settingScriptAction.GetIntType("numericUpDown48", 100);
            int delay = _settingScriptAction.GetIntType("numericUpDown46", 3);
            if (_platform == "Instagram" && _jobService == "https://vipig.net/")
            {
                var vipigClient = new VipIGClient(_cookieService);
                for (int i = 0; i < 10; i++)
                {
                    _sate = $"Đăng nhập VipIG lần {i + 1}/10";
                    var coin = await vipigClient.GetBalance();
                    if (!string.IsNullOrEmpty(coin))
                    {
                        _account.Result = coin;
                        break;
                    }
                    var accVipIG = await vipigClient.LoginByToken(_account.TokenJob);
                    if (string.IsNullOrEmpty(accVipIG))
                        throw new Exception("Đăng nhập vipig.net lỗi.");
                    if (string.IsNullOrEmpty(accVipIG.Split('|')[0]))
                    {
                        await DelayMessageAsync(150, accVipIG.Split('|')[1] + " đợi {time} giây", 2);
                        continue;
                    }
                    _cookieService = accVipIG.Split('|')[2];
                    _account.Result = accVipIG.Split('|')[1];
                    break;
                }


            }
            for (int i = 1; i <= index; i++)
            {
                _sate = $"Lấy danh sách job {i}/{index}";
                try
                {
                    _typeJob = SubdyHelper.GetStringRandom(_job_types.ToList());

                    if (_platform == PlatformModel.Facebook)
                    {
                        if (_jobService == "https://tuongtaccheo.com/")
                        {
                            _jobPrefix = TuongTacCheoClient.GetJobPrefix(_typeJob);

                        }
                        else if (_jobService == "Subdy")
                        {
                            jobs = SubdyClient.GetJobs(_tokenJobService, _account.Uid);
                        }
                        else
                        {
                            jobs = await JobClient.GetFacebookJob(_jobService, _account.Uid, _tokenJobService, _typeJob, _jobPrefix);
                        }

                    }
                    else if (_platform == "Instagram" && _jobService == "https://app.golike.net/")
                    {
                        jobs = await new GoLikeClient().GetInstagramJob(_infoAccountService["id"], _account.TokenJob);
                    }
                    else if (_platform == "Instagram" && _jobService == "https://vipig.net/")
                    {
                        var vipigClient = new VipIGClient(_cookieService);
                        jobs = await vipigClient.GetJobInstagram(_typeJob);
                    }
                    var jobsResult = jobs.FindAll(x => _job_types.Contains(x.Type));
                    if (!jobsResult.Any())
                    {
                        throw new Exception("Không có job phù hợp theo yêu cầu.");
                    }
                    return jobsResult;
                }
                catch (Exception ex)
                {
                    LogManager.Error(ex);
                    error = ex.Message;
                }
                await DelayMessageAsync(delay, error + " đợi {time} giây", 2);
            }

            throw new Exception($"Kết thúc hành động khi get job thất bại liên tiếp {index} lần");
        }

        private async Task StopScriptAction()
        {
            await Stop();
            await _facebookService.HanderAccount(_client, _account, 5, this._ct, this);
            if (_timeoutScriptAction > 0 && _stopwatchScriptAction.IsRunning &&
                _stopwatchScriptAction.ElapsedMilliseconds > _timeoutScriptAction)
            {
                _stopwatch.Restart();
                SetStatus("Đã quá thời gian thực hiện hành động, dừng tài khoản.", 1);
                throw new TimeoutException();
            }

            var jobTodayParts = _account.JobToday.Split('/');
            int jobTodayTotal = (int.TryParse(jobTodayParts.ElementAtOrDefault(0), out var s) ? s : 0)
                              + (int.TryParse(jobTodayParts.ElementAtOrDefault(1), out var f) ? f : 0);
            if (ShouldStopByJobLimit(jobTodayTotal, _stopJobTatolAccount, "tổng số job hôm nay")) return;
            if (ShouldStopByJobLimit(_blockJobTuongTac, _stopJobBlockTuongTac, "bị chặn tương tác")) return;
            if (ShouldStopByJobLimit(_jobFail_LienTiep, _stopJobFail, "job thất bại liên tiếp")) return;

            if (_doJobInfo.ContainsKey($"{_typeJob}_faillientiep") && _removeJob > 0 && _doJobInfo[$"{_typeJob}_faillientiep"] >= _removeJob)
            {
                await DelayMessageAsync(_settingScriptAction.GetIntType("numericUpDown20", 30),
                    $"Xóa loại job [{_typeJob}] thất bại liên tiếp {_removeJob}." + " Đợi {time} giây", 2);
                _job_types.Remove(_typeJob);
                _jobFail_LienTiep = 0;
            }

            if (ShouldStopByJobLimit(Convert.ToInt32(_account.JobToday.Split("/")?.First()), _stopJobToday, "job/ngày")) return;

            await HandleBreakTime();

            RemoveOverLimitReactions();
            if (!_job_types.Any() && _config.JobService != "Subdy")
            {
                await DelayMessageAsync(10,
                  $"Không có loại job nào cần làm!" + " đợi {time} giây", 2);
                throw new Exception($"Không có loại job nào cần làm!");
            }
        }

        private bool ShouldStopByJobLimit(int current, int limit, string reason)
        {
            if (limit > 0 && current >= limit)
            {
                DelayMessageAsync(_settingScriptAction.GetIntType("numericUpDown17", 30),
                    $"Kết thúc hành động khi {reason} đạt {limit}" + ". Đợi {time} giây", 2).Wait();
                throw new Exception($"Dừng khi {reason} đạt {limit}");
            }
            return false;
        }

        private async Task HandleBreakTime()
        {
            int delayCount = SubdyHelper.RandomValue(
                _settingScriptAction.GetIntType("numericUpDown10", 5),
                _settingScriptAction.GetIntType("numericUpDown9", 10));

            if (_settingScriptAction.GetBooleanValue("checkBox2", true) && _countJob > 0 && _countJob % delayCount == 0)
            {
                _sate = "Nghỉ giải lao";
                waitForDoJobStart.Reset();
                int restTime = SubdyHelper.RandomValue(
                    _settingScriptAction.GetIntType("numericUpDown8", 60),
                    _settingScriptAction.GetIntType("numericUpDown7", 120));

                if (_settingScriptAction.GetBooleanValue("radioButton1", true))
                {
                    await DelayMessageAsync(restTime, $"Làm {delayCount} job liên tiếp, nghỉ giải lao" + ". Đợi {time} giây", 2);
                }
                else if (_settingScriptAction.GetBooleanValue("radioButton3", false))
                {
                    waitForDoJobStart.Set();
                    await DelayMessageAsync(restTime, $"Làm {delayCount} job liên tiếp, lướt newfeed" + ". Đợi {time} giây", 2);
                }
                else if (_settingScriptAction.GetBooleanValue("radioButton2", false))
                {
                    if (_platform == PlatformModel.Facebook)
                    {
                        _client.ADB.Shell("am start -n com.facebook.katana/.IntentUriHandler \"fb://watch\"");
                    }
                    else if (_platform == "Instagram")
                    {
                        _client.ADB.Shell($"am start -a android.intent.action.VIEW -d \"https://www.instagram.com/reels/{SubdyHelper.RandomString(length: SubdyHelper.RandomValue(6, 18))}\" -p com.instagram.android");
                        _client.SwipeByPercent(56, 82, 56, 16, 1000, 3, SubdyHelper.RandomValue(100, 2000));
                    }

                    await DelayMessageAsync(restTime, $"Làm {delayCount} job liên tiếp, xem video" + ". Đợi {time} giây", 2);
                    _client.Shell("input keyevent 4");

                }

                waitForDoJobStart.Set();
            }
        }

        private static readonly (string Type, string Low, string High)[] _jobLimitChecks = new (string Type, string Low, string High)[]
        {
            (JobTypes.Like, "numericUpDown3", "numericUpDown4"),
            (JobTypes.Love, "numericUpDown6", "numericUpDown5"),
            (JobTypes.Care, "numericUpDown27", "numericUpDown26"),
            (JobTypes.Haha, "numericUpDown31", "numericUpDown30"),
            (JobTypes.Sad, "numericUpDown29", "numericUpDown28"),
            (JobTypes.Wow, "numericUpDown33", "numericUpDown32"),
            (JobTypes.Angry, "numericUpDown43", "numericUpDown42"),
            (JobTypes.LikePage, "numericUpDown45", "numericUpDown44"),
            (JobTypes.JoinGroup, "numericUpDown39", "numericUpDown38"),
            (JobTypes.Share, "numericUpDown37", "numericUpDown38"),
            (JobTypes.Follow, "numericUpDown35", "numericUpDown34"),
            (JobTypes.LikeComment, "numericUpDown41", "numericUpDown40")
        };

        private List<JobModel> LimitJobsByQuota(List<JobModel> jobs)
        {
            var limited = new List<JobModel>();
            foreach (var job in jobs)
            {
                string type = job.Type.ToLower();
                if (!_job_types.Contains(type))
                    continue;

                int done = _doJobInfo.ContainsKey(type) ? _doJobInfo[type] : 0;
                var check = _jobLimitChecks.FirstOrDefault(c => c.Type == type);
                if (check.Type != null)
                {
                    int limit = SubdyHelper.RandomValue(
                        _settingScriptAction.GetIntType(check.Low, 100),
                        _settingScriptAction.GetIntType(check.High, 500));
                    int remaining = limit - done;
                    int countInList = limited.Count(j => j.Type.ToLower() == type);
                    if (countInList >= remaining)
                        continue;
                }
                limited.Add(job);
            }
            return limited;
        }

        private void RemoveOverLimitReactions()
        {
            var checks = _jobLimitChecks;

            foreach (var (type, minKey, maxKey) in checks)
            {
                if (_job_types.Contains(type) && _doJobInfo.ContainsKey(type) && _doJobInfo[type] >=
                    SubdyHelper.RandomValue(_settingScriptAction.GetIntType(minKey, 100),
                                            _settingScriptAction.GetIntType(maxKey, 500)))
                {
                    _job_types.Remove(type);
                }
            }
        }

        public async Task<SubdyExtension> ExecuteAsync()
        {
            try
            {
                _sate = "Khởi tạo dịch vụ job";
                await InitSettings();
                if (string.IsNullOrEmpty(Globals.User.ApiKey))
                {
                    var reponesapiKey = SubdyClient.GetApiKey(Globals.User.Token);
                    if (reponesapiKey == null || string.IsNullOrEmpty(reponesapiKey.ApiKey))
                    {
                        var reponesapi = SubdyClient.CreateApiKey(Globals.User.Token);
                        Globals.User.ApiKey = reponesapi.ApiKey;
                    }
                    else
                    {
                        Globals.User.ApiKey = reponesapiKey.ApiKey;
                    }
                }
                _tokenJobService = Globals.User.ApiKey;
                //SubdyClient.AddPlatformAccount(Globals.User.Token, 1, _account.Uid, _account.FullName);

                _client.AppStart(FacebookHander.Package(_platform), true, true, true);
                _ = Task.Run(ScrollNewFeed); // background task

                while (true)
                {
                    try
                    {
                        await StopScriptAction();

                        var jobs = await GetJob();

                        if (jobs == null || !jobs.Any())
                        {
                            //if (_config.JobService == "Subdy")
                            //{
                            //    throw new SubdyExtension(SubdyEnum.JobFail, "Hết job để làm.");
                            //}
                            continue;
                        }

                        // Giới hạn số job theo cài đặt min/max cho từng loại
                        jobs = LimitJobsByQuota(jobs);
                        if (!jobs.Any())
                        {
                            continue;
                        }

                        List<string> listJob = new List<string>();
                        bool isClaim = true;
                        for (int index = 0; index < jobs.Count; index++)
                        {
                            var job = jobs[index];
                            if (job == null)
                            {
                                continue;
                            }
                            _typeJob = job.Type.ToLower();

                            // Kiểm tra nếu loại job đã bị xóa (đã đạt giới hạn) thì bỏ qua
                            if (!_job_types.Contains(_typeJob))
                            {
                                continue;
                            }

                            _sate = $"Thực hiện {_typeJob.ToUpper()} job {index + 1}/{jobs.Count}";
                            if (_typeJob == JobTypes.Follow && jobs.Count < 5 && _jobService == "https://vipig.net/" && _platform == "Instagram")
                            {
                                int second = SubdyHelper.RandomValue(
                                    _settingScriptAction.GetIntType("nudJobDelayFrom", 5),
                                    _settingScriptAction.GetIntType("nudJobDelayTo", 10)
                                );

                                await DelayMessageAsync(second, $"Không đủ trên {jobs.Count}/5 job follow." + " Đợi {time} giây", 2);
                                break;
                            }


                            SubdyExtension subdy = null;
                            try
                            {
                                await StopScriptAction();

                                waitForDoJobStart.Reset();

                                string jobIdShort = job.ObjectId?.Length >= 3 ? job.ObjectId.Substring(0, 3) : job.ObjectId ?? "null";
                                string jobType = job.Type ?? "unknown";

                                await DelayMessageAsync(5, $"Chuẩn bị làm job {jobType} ObjectId: {jobIdShort}... " + " Đợi {time} giây", 2);

                                subdy = await DoJob(job);

                                subdy = await HanderDoJob(job, subdy);

                                waitForDoJobStart.Set();

                                if (_jobService == "https://vipig.net/" && job.Type == JobTypes.Follow)
                                {
                                    listJob.Add(job.JobId);
                                    isClaim = false;
                                    if (listJob.Count == jobs.Count)
                                    {
                                        isClaim = true;
                                        subdy.SubdyEnum = SubdyEnum.Success;
                                    }
                                }
                                if (isClaim)
                                {
                                    subdy = await ReportJob(job, subdy, listJob);
                                }
                                else
                                {
                                    string messsage = subdy.Message;
                                    messsage = messsage + " - " + SubdyClient.SkipJob(_tokenJobService, Convert.ToInt32(job.JobId), _account.Uid, subdy.Message);
                                }
                                HanderJob(subdy, job);
                            }
                            finally
                            {
                                if (subdy != null)
                                {
                                    await UpdateJob(subdy, job);
                                    int second = SubdyHelper.RandomValue(
                                  _settingScriptAction.GetIntType("nudJobDelayFrom", 5),
                                  _settingScriptAction.GetIntType("nudJobDelayTo", 10)
                              );
                                    await DelayMessageAsync(second, $"{subdy.Message} - [Delay tương tác tiếp theo]" + " đợi {time} giây", 2);
                                }


                            }

                        }

                    }
                    catch (Exception ex)
                    {
                        LogManager.Error(ex);
                        throw ex;
                    }

                }
            }
            finally
            {
                _isStop = true;

            }
            // Không bao giờ đến đây
            return new SubdyExtension(SubdyEnum.None, "Done");
        }
        private async Task UpdateJob(SubdyExtension subdy, JobModel job)
        {
            var model = new JobHistory
            {
                Id = Guid.NewGuid(),
                IdJob = job.JobId,
                IdObject = job.ObjectId,
                Coin = job.Coin.ToString(),
                Uid = _account.Uid,
                Service = _jobService,
                Method = job.Type,
                Description = subdy.Message,
                Platform = _platform,
                Status = subdy?.SubdyEnum == SubdyEnum.Success
                      ? SubdyEnum.Success.ToString()
                      : SubdyEnum.JobFail.ToString()
            };

            _jobHistoryContext.Add(model);
            var rows = _jobHistoryContext.GetHistorySummaryToDayByUid(_account.Uid, _platform);
            var grouped = rows
.GroupBy(kv => kv.Key.EndsWith("_skip")
               ? kv.Key.Replace("_skip", "")
               : kv.Key)
.ToDictionary(
   g => g.Key,
   g => new
   {
       Success = g.Where(x => !x.Key.EndsWith("_skip")).Sum(x => x.Value),
       Skip = g.Where(x => x.Key.EndsWith("_skip")).Sum(x => x.Value)
   });
            int countTodaySuccess = 0, countTodayFail = 0;
            _account.Summary = "";
            _account.Summary_Skip = "";
            foreach (var kv in grouped)
            {
                string key = kv.Key;
                string displayKey = char.ToUpper(key[0]) + key.Substring(1).ToLower();
                countTodaySuccess += kv.Value.Success;
                countTodayFail += kv.Value.Skip;
                _account.Summary += $"{displayKey}: {kv.Value.Success} ";
                _account.Summary_Skip += $"{displayKey}: {kv.Value.Skip} ";
            }
            _account.JobToday = $"{countTodaySuccess}/{countTodayFail}";

            // Cộng dồn xu hôm nay (chỉ khi job success và có coin)
            if (subdy?.SubdyEnum == SubdyEnum.Success && job.Coin > 0)
            {
                double.TryParse(_account.XuToday, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double currentXu);
                currentXu += job.Coin;
                _account.XuToday = currentXu.ToString("0.##");
            }

            _accountContext.Update(_account);
            loadJobTotals();
            if (subdy.SubdyEnum == SubdyEnum.Success)
            {
                var balance = SubdyClient.GetProfile(Globals.User.Token);
                Globals.User.Balance = balance;
                ControlHelper.SetLabelTextSafe(Globals.CoinLable, ((int)balance).ToMoneyString() + " xu");
            }
            if (_platform == PlatformModel.Facebook)
            {
                switch (_jobService)
                {
                    case "https://tuongtaccheo.com/":
                        {
                            var client = new TuongTacCheoClient();
                            var coin = await client.GetCoin(_account.TokenJob);
                            if (!string.IsNullOrEmpty(coin))
                            {
                                _account.Result = coin;
                            }
                            break;
                        }
                    case "https://app.golike.net/":
                        {
                            var client = new GoLikeClient();
                            var coin = client.GetCoin(_account.TokenJob);
                            if (!string.IsNullOrEmpty(coin))
                            {
                                _account.Result = coin;
                            }
                            break;
                        }
                }
            }
        }
        private void loadJobTotals()
        {
            var rows = _jobHistoryContext.GetJobTotals(_platform, _jobService, DateTime.Now.ToString("dd/MM/yyyy"));
            if (!rows.Any())
            {
                return;
            }
            int today = 0;
            int success = 0;
            int fail = 0;
            foreach (var row in rows)
            {
                today += row.Value;
                if (row.Key.Contains("_skip"))
                {
                    fail += row.Value;

                }
                else
                {
                    success += row.Value;
                }
            }
            var grouped = rows
    .GroupBy(kv => kv.Key.EndsWith("_skip")
                    ? kv.Key.Replace("_skip", "")
                    : kv.Key)
    .ToDictionary(
        g => g.Key,
        g => new
        {
            Success = g.Where(x => !x.Key.EndsWith("_skip")).Sum(x => x.Value),
            Skip = g.Where(x => x.Key.EndsWith("_skip")).Sum(x => x.Value)
        });
            foreach (var kv in grouped)
            {
                string key = kv.Key;
                string displayKey = char.ToUpper(key[0]) + key.Substring(1).ToLower();
                string text = $"{displayKey}: {kv.Value.Success}/{kv.Value.Skip}";
                var ts = Globals.ToolStripDropDownButton1;
                if (ts.GetCurrentParent().InvokeRequired)
                {
                    ts.GetCurrentParent().BeginInvoke(new Action(() =>
                    {
                        foreach (ToolStripMenuItem item in ts.DropDownItems)
                        {
                            string name = item.Name.Split('_').First().ToLower();
                            if (name != kv.Key) continue;
                            ControlHelper.SetToolStripMenuItemTextSafe(item, text);
                            break;
                        }
                    }));
                }
                else
                {
                    foreach (ToolStripMenuItem item in ts.DropDownItems)
                    {
                        string name = item.Name.Split('_').First().ToLower();
                        if (name != kv.Key) continue;
                        ControlHelper.SetToolStripMenuItemTextSafe(item, text);
                        break;
                    }
                }
                //foreach (ToolStripMenuItem item in Globals.ToolStripDropDownButton1.DropDownItems)
                //{
                //    string name = item.Name.Split("_").First().ToLower();
                //    if (name != kv.Key) continue;
                //    ControlHelper.SetToolStripMenuItemTextSafe(item, text);
                //    break;
                //}
            }
            ControlHelper.SetToolStripLabelTextSafe(Globals.ToolStripLabel16, today.ToString());
            ControlHelper.SetToolStripMenuItemTextSafe(Globals.JobTotal_toolStripMenuItem, $"Job Total: {success}/{fail}");
        }
        private async Task InitSettings()
        {
            _sate = "Cấu hình dịch vụ";
            SetStatus("Đang khởi tạo cài đặt...", 2);
            if (_settingScriptAction.GetBooleanValue("ckbTimeoutScript"))
            {
                _stopwatchScriptAction.Restart();
                _timeoutScriptAction = SubdyHelper.RandomValue(
                    _settingScript.GetIntType("numericUpDown5", 40),
                    _settingScript.GetIntType("numericUpDown4", 60)) * 60 * 1000;
            }

            if (_settingScriptAction.GetBooleanValue("checkBox3", true))
                _stopJobTatolAccount = SubdyHelper.RandomValue(_settingScriptAction.GetIntType("numericUpDown14", 100),
                                                               _settingScriptAction.GetIntType("numericUpDown13", 200));

            if (_settingScriptAction.GetBooleanValue("checkBox4", true))
                _stopJobBlockTuongTac = SubdyHelper.RandomValue(_settingScriptAction.GetIntType("numericUpDown12", 10),
                                                                 _settingScriptAction.GetIntType("numericUpDown11", 20));

            if (_settingScriptAction.GetBooleanValue("checkBox5", true))
                _stopJobFail = SubdyHelper.RandomValue(_settingScriptAction.GetIntType("numericUpDown16", 100),
                                                        _settingScriptAction.GetIntType("numericUpDown15", 200));

            if (_settingScriptAction.GetBooleanValue("checkBox6", true))
                _removeJob = SubdyHelper.RandomValue(_settingScriptAction.GetIntType("numericUpDown22", 100),
                                                     _settingScriptAction.GetIntType("numericUpDown21", 200));

            if (_settingScriptAction.GetBooleanValue("checkBox7", true))
                _stopJobToday = SubdyHelper.RandomValue(_settingScriptAction.GetIntType("numericUpDown25", 100),
                                                        _settingScriptAction.GetIntType("numericUpDown24", 200));

            if (_settingScriptAction.GetBooleanValue("checkBox19"))
            {
                await Globals.Semaphore.WaitAsync();
                try
                {
                    var list = _settingScriptAction.GetValuesList("textBox1");
                    if (!list.Any())
                    {
                        throw new Exception("Không có token job service.");
                    }
                    _account.TokenJob = list[0];
                    list.RemoveAt(0);
                    _settingScriptAction.AddValueList("textBox1", list);
                }
                finally
                {
                    Globals.Semaphore.Release();
                }
            }

            if (_platform == PlatformModel.Facebook)
            {
                _jobService = _config.JobService;

                switch (_jobService)
                {
                    case "https://tuongtaccheo.com/":
                        {
                            if (string.IsNullOrEmpty(_account.TokenJob))
                            {
                                throw new Exception("Vui lòng thêm token tuongtaccheo.");
                            }
                            var tuongtaccheoclient = new TuongTacCheoClient();
                            _tokenJobService = await tuongtaccheoclient.GetCookie(_account.TokenJob);
                            if (!await tuongtaccheoclient.DatNick(_tokenJobService, _account.Uid))
                            {
                                string keyCaptcha = _settingScriptAction.GetValuesFromInputString("textBox2");
                                if (string.IsNullOrEmpty(keyCaptcha))
                                {
                                    throw new Exception("Không có key Captcha");
                                }
                                int refail = _settingScriptAction.GetIntType("numericUpDown40", 3);
                                string token = string.Empty;
                                for (int i = 0; i < refail; i++)
                                {
                                    string message = string.Empty;
                                    try
                                    {
                                        _sate = $"Cấu hình tài khoản TuongTacCheo lần {i + 1}/{refail}";
                                        string siteKey = await tuongtaccheoclient.GetSiteKey(_tokenJobService);
                                        if (string.IsNullOrEmpty(siteKey))
                                        {
                                            message = "Get SiteKey TuongTacCheo thất bại.";
                                            continue;
                                        }
                                        token = await CaptchaV2(keyCaptcha, siteKey, tuongtaccheoclient.SiteUrl);
                                        if (!string.IsNullOrEmpty(token))
                                        {
                                            message = "Giải captcha thành công!";
                                            break;
                                        }
                                        message = "Giải captcha thất bại!";
                                    }
                                    finally
                                    {
                                        await DelayMessageAsync(5, message + ". Đợi {time} giây", 2);
                                    }

                                }
                                if (string.IsNullOrEmpty(token))
                                {
                                    throw new Exception("Giải captcha thất bại!");
                                }
                                string messageAdd = await tuongtaccheoclient.AddAccount(_tokenJobService, _account.Uid, token);
                                if (messageAdd.Contains("error"))
                                {
                                    throw new Exception(messageAdd);
                                }
                                if (!await tuongtaccheoclient.DatNick(_tokenJobService, _account.Uid))
                                {
                                    throw new Exception("Cấu hình tài khoản tuongtaccheo thất bại.");
                                }
                            }
                            break;
                        }
                    case "https://app.golike.net/":
                        {
                            _tokenJobService = _account.TokenJob;
                            break;
                        }
                }
            }
            else if (_platform == "Instagram")
            {
                _jobService = _config.JobService;

                if (string.IsNullOrEmpty(_account.TokenJob))
                {
                    throw new Exception("Không có token job service.");
                }
                switch (_jobService)
                {
                    case "https://app.golike.net/":
                        {
                            var client = new GoLikeClient();
                            if (string.IsNullOrEmpty(_account.FullName) || string.IsNullOrEmpty(_account.Bio))
                            {
                                var accountIg = await client.GetAccount(_account.TokenJob);
                                if (accountIg.ContainsKey("error"))
                                {
                                    throw new Exception($"Get info account golike lỗi: {accountIg["error"]}");
                                }
                                string fullname = string.Empty;
                                if (string.IsNullOrEmpty(_account.FullName))
                                {
                                    fullname = $"{SubdyHelper.GetStringRandom(SubdyHelper.FirstnameVN)} {SubdyHelper.GetStringRandom(SubdyHelper.LastnameVN)}";
                                }
                                string code = accountIg["code"];


                                var value = await _facebookService.UpateInfo(_client, fullname, code, "");
                                if (value.ContainsKey("fullname"))
                                {
                                    _account.FullName = value["fullname"];
                                }
                                if (value.ContainsKey("bio"))
                                {
                                    _account.Bio = value["bio"];
                                }
                                _accountContext.Update(_account);
                            }
                            bool isvery = false;
                            for (int i = 0; i < 2; i++)
                            {
                                var accountIg = await client.GetInstagramAccount(_account.TokenJob);
                                if (accountIg.ContainsKey("error"))
                                {
                                    throw new Exception($"Get list id account golike lỗi: {accountIg["error"]}");
                                }
                                if (!accountIg.ContainsKey(_account.Uid))
                                {
                                    accountIg = await client.VerifyAccountInstagram(_account.TokenJob, _account.UserName);
                                    if (accountIg.ContainsKey("error"))
                                    {
                                        throw new Exception($"Verify account instagram golike lỗi: {accountIg["error"]}");
                                    }
                                    SetStatus(accountIg["success"], 2);
                                    isvery = false;
                                    continue;
                                }
                                else
                                {
                                    isvery = true;
                                    break;
                                }

                            }
                            if (!isvery)
                            {
                                throw new Exception("Đã xảy ra khi thêm tài khoản golike...");
                            }
                            break;
                        }
                    case "https://vipig.net/":
                        {
                            VipIGClient vipigClient = new VipIGClient();
                            var accVipIG = await vipigClient.LoginByToken(_account.TokenJob);
                            if (string.IsNullOrEmpty(accVipIG))
                                throw new Exception("Đăng nhập vipig.net lỗi.");
                            _cookieService = accVipIG.Split('|')[2];
                            _account.Result = accVipIG.Split('|')[1];

                            _sate = "Cấu hình tài khoản VipIG";
                            SetStatus("Đang cấu hình tài khoản VipIG...", 2);
                            bool configured;
                            if (!_settingScriptAction.GetBooleanValue("check_AddAccount", false))
                            {
                                configured = await vipigClient.CauHinh(_account.Uid);
                                if (!configured) throw new Exception("Cấu hình không hợp lệ");

                                bool check = false;
                                string id = await vipigClient.GetIdByUsername(_account.Uid);
                                if (string.IsNullOrEmpty(id))
                                {
                                    check = await vipigClient.DatNick(_account.Uid) != 1;
                                }
                                if (!check)
                                {
                                    if (!await vipigClient.CauHinhNhanh(_account.Uid))
                                        throw new Exception($"Cần thêm nick: {_account.Uid} vào trước khi chạy");
                                }
                            }
                            else
                            {
                                _sate = "Cấu hình tài khoản VipIG nhanh";
                                SetStatus("Đang cấu hình tài khoản VipIG nhanh...", 2);
                                bool check = false;
                                string id = await vipigClient.GetIdByUsername(_account.Uid);
                                if (!string.IsNullOrEmpty(id))
                                {
                                    check = await vipigClient.DatNick(id) == 1;
                                }
                                if (!check)
                                {
                                    if (!await vipigClient.CauHinhNhanh(_account.Uid))
                                        throw new Exception($"Cần thêm nick: {_account.Uid} vào trước khi chạy");
                                }

                            }



                            break;
                        }
                }
            }
            var jobMappings = new Dictionary<string, string>
{
    { "checkBox1",  JobTypes.Like },
    { "checkBox8",  JobTypes.Love },
    { "checkBox2",  JobTypes.Care },
    { "checkBox3",  JobTypes.Haha },
    { "checkBox4",  JobTypes.Sad },
    { "checkBox5",  JobTypes.Wow },
    { "checkBox15", JobTypes.Angry },
    { "checkBox18", JobTypes.LikePage },
    { "checkBox17", JobTypes.JoinGroup },
    { "checkBox7",  JobTypes.Share },
    { "checkBox6", JobTypes.Follow },
};

            foreach (var kv in jobMappings)
            {
                if (_settingScriptAction.GetBooleanValue(kv.Key, true) && JobServices.GetTypeJobByPlatformt(_platform).Contains(kv.Value))
                {
                    _job_types.Add(kv.Value);
                }
            }
            if (!_job_types.Any())
            {
                await DelayMessageAsync(10,
                  $"Không có loại job nào cần làm! Mở cài đặt job chọn loại job cần làm" + ". Đợi {time} giây", 2);
                throw new Exception($"Không có loại job nào cần làm! Mở cài đặt job chọn loại job cần làm");
            }
        }
        private async Task<string> CaptchaV2(string key, string sitekey, string siteurl)
        {
            string site = CaptchaService.SitesV2[_settingScriptAction.GetIntType("cbb_ListTypeProxy")];

            int timeout = _settingScriptAction.GetIntType("numericUpDown41", 180);
            _stopwatch.Restart();
            string id = string.Empty;
            string message = string.Empty;
            string token = string.Empty;
            while (_stopwatch.ElapsedMilliseconds < timeout * 1000)
            {
                try
                {
                    if (string.IsNullOrEmpty(id))
                    {
                        SetStatus("Đang lấy id captcha", 2);
                        id = await CaptchaService.GetIdCaptchaV2(site, key, sitekey, siteurl);
                        if (string.IsNullOrEmpty(id))
                        {
                            message = $"({_stopwatch.Elapsed.TotalSeconds}/{timeout})Server không phản hồi khi tạo id captcha";
                            id = string.Empty;
                            continue;
                        }
                        if (id.Contains("error"))
                        {
                            message = $"({_stopwatch.Elapsed.TotalSeconds}/{timeout})Tạo id captcha thât bại: " + id;
                            id = string.Empty;
                            continue;
                        }
                    }
                    message = await CaptchaService.GetTokenCaptchaV2(site, key, id);
                    if (string.IsNullOrEmpty(message))
                    {
                        message = $"({_stopwatch.Elapsed.TotalSeconds}/{timeout})Server không phản hồi khi get token captcha";
                        continue;
                    }
                    if (message.Contains("error"))
                    {
                        message = $"({_stopwatch.Elapsed.TotalSeconds}/{timeout})Get token captcha thât bại: " + message;
                        continue;
                    }
                    token = message;
                    return token;
                }
                finally
                {
                    await DelayMessageAsync(2, message + ". Đợi {time} giây", 1);
                }

            }
            return token;
        }
        private void SwipeUp(int repeat = 1, int duration = 800)
        {
            var screen = _client.GetScreenResolution();
            var rnd = new Random();
            for (int i = 0; i < repeat; i++)
            {
                int x = (int)(screen.X * (0.30 + rnd.NextDouble() * 0.40));          // 30–70% ngang
                int startY = (int)(screen.Y * (0.60 + rnd.NextDouble() * 0.25));     // 60–85% dọc
                int endY = (int)(screen.Y * (0.15 + rnd.NextDouble() * 0.20));       // 15–35% dọc
                _client.Swipe(x, startY, x, endY, duration);
                if (i < repeat - 1) Thread.Sleep(SubdyHelper.RandomValue(300, 800));
            }
        }

        private void SwipeDown(int repeat = 1, int duration = 800)
        {
            var screen = _client.GetScreenResolution();
            var rnd = new Random();
            for (int i = 0; i < repeat; i++)
            {
                int x = (int)(screen.X * (0.30 + rnd.NextDouble() * 0.40));          // 30–70% ngang
                int startY = (int)(screen.Y * (0.15 + rnd.NextDouble() * 0.20));     // 15–35% dọc
                int endY = (int)(screen.Y * (0.60 + rnd.NextDouble() * 0.25));       // 60–85% dọc
                _client.Swipe(x, startY, x, endY, duration);
                if (i < repeat - 1) Thread.Sleep(SubdyHelper.RandomValue(300, 800));
            }
        }

        private async Task ScrollNewFeed()
        {
            while (!_isStop)
            {
                waitForDoJobStart.Wait();
                if (!_settingGeneral.GetBooleanValue("checkBox14", true)) continue;
                if (!_client.Package(FacebookHander.Package(_platform), 1))
                {
                    if (_platform == PlatformModel.Facebook)
                    {
                        _client.Shell($"am start -n com.facebook.katana/.IntentUriHandler \"fb://feed\"");
                    }
                    else if (_platform == "Instagram")
                    {
                        _client.AppStart(FacebookHander.Package(_platform));
                    }
                    continue;
                }
                _client.ElementWithAttributes("//*[@content-desc=\"Close\"]", timeoutInSeconds: 1);
                _client.SwipeUp(SubdyHelper.RandomValue(1, 5), SubdyHelper.RandomValue(500, 1000), SubdyHelper.RandomValue(200, 1000));
                // _client.SwipeByPercent(52, 92, 52, 45, 1000, SubdyHelper.RandomValue(1, 5));
                int second = SubdyHelper.RandomValue(1, 15);
                for (int i = 0; i < second; i++)
                {
                    waitForDoJobStart.Wait();
                    await Task.Delay(1000);
                }

            }
        }

        private async Task<SubdyExtension> DoJob(JobModel job)
        {
            SubdyExtension extension = new SubdyExtension(SubdyEnum.JobFail, $"Chưa hỗ trợ loại {job.Type} này.");
            switch (_typeJob)
            {
                case JobTypes.Like:
                case JobTypes.Love:
                case JobTypes.Care:
                case JobTypes.Haha:
                case JobTypes.Wow:
                case JobTypes.Sad:
                case JobTypes.Angry:
                case JobTypes.LikeComment:
                    {
                        return await JobReaction(job);
                    }
                case JobTypes.Follow:
                    {
                        return await JobFollow(job);
                    }
                case JobTypes.JoinGroup:
                    {
                        return await JobGroup(job);
                    }
                case JobTypes.LikePage:
                    {
                        return await JobLikePage(job);
                    }
            }



            return extension;
        }
        private async Task<SubdyExtension> HanderDoJob(JobModel job, SubdyExtension subdy)
        {
            if (subdy.SubdyEnum == SubdyEnum.Success && !string.IsNullOrEmpty(job.Link))
            {
                _client.ADB.Shell("input keyevent 4");
                if (await GotoUrl(job.Link))
                {
                    string type = job.Type.ToLower();
                    if (type == JobTypes.Like || type == JobTypes.Love ||
                       type == JobTypes.Sad || type == JobTypes.Haha ||
                       type == JobTypes.Wow || type == JobTypes.Angry ||
                       type == JobTypes.Care || type == JobTypes.LikeComment)
                    {
                        if (_platform == PlatformModel.Facebook)
                        {
                            if (!_client.ElementWithAttributes("//*[@content-desc=\"Navigate to your Reels profile\"]", 5, click: false))
                            {
                                for (int i = 0; i < 10; i++)
                                {
                                    string dump = _client.GetXMLSource();
                                    if (string.IsNullOrEmpty(dump))
                                    {
                                        continue;
                                    }
                                    dump = dump.ToLower();
                                    if (!_client.ElementWithAttributes(new List<string> { "//*[contains(@text,\"Share\")]", "//*[contains(@content-desc,\"Share\")]" }, 5, dump, false) || !dump.Contains("share", StringComparison.OrdinalIgnoreCase) && !dump.Contains("like", StringComparison.OrdinalIgnoreCase))
                                    {
                                        _client.SwipeUp(1, SubdyHelper.RandomValue(500, 2000), SubdyHelper.RandomValue(500, 2000));
                                        continue;
                                    }
                                    break;
                                }
                            }
                           
                        }

                    }

                    string xml = _client.GetXMLSource().ToLower();
                    if (xml.Contains("log into another account"))
                    {
                        subdy.SubdyEnum = SubdyEnum.LogOut;
                        subdy.Message = $"Tài khoản logout";
                    }
                    else if (xml.Contains("you can't use this feature right now"))
                    {
                        subdy.SubdyEnum = SubdyEnum.Block;
                        subdy.Message = $"Tài khoản bị chặn chức năng [You Can't Use This Feature Right Now]";
                    }
                    else if (xml.Contains("sorry, something went wrong"))
                    {
                        subdy.SubdyEnum = SubdyEnum.Block;
                        subdy.Message = "Có thể tài khoản đã bị chặn tương tác [sorry, something went wrong]";
                    }
                    else if (xml.Contains("text=\"cancel\""))
                    {
                        subdy.SubdyEnum = SubdyEnum.LogOut;
                        subdy.Message = "không load được trang facebook";
                    }
                    else if (xml.Contains("use facebook without messaging"))
                    {
                        subdy.SubdyEnum = SubdyEnum.JobFail;
                        subdy.Message = $"Không tìm thấy nút : {job.Type.ToUpper()} [Use Facebook without messaging]";
                    }
                    else if (xml.Contains("go to news feed") || xml.Contains("see more on facebook"))
                    {
                        subdy.SubdyEnum = SubdyEnum.JobFail;
                        subdy.Message = $"Link lỗi hoặc không tồn tại! LINK: [{job.Link}]";
                    }
                    if (type == JobTypes.Like || type == JobTypes.Love ||
                      type == JobTypes.Sad || type == JobTypes.Haha ||
                      type == JobTypes.Wow || type == JobTypes.Angry ||
                      type == JobTypes.Care || type == JobTypes.LikeComment)
                    {
                        if (!_client.ElementWithAttributes("//*[@content-desc=\"Navigate to your Reels profile\"]", 5, click: false))
                        {
                            if (_platform == PlatformModel.Facebook && !xml.Contains(", pressed. double tap and hold"))
                            {
                                subdy.SubdyEnum = SubdyEnum.Block;
                                subdy.Message = $"Chặn tương tác {job.Type}";
                            }
                            else if (_platform == "Instagram" && !xml.Contains("liked"))
                            {
                                subdy.SubdyEnum = SubdyEnum.Block;
                                subdy.Message = $"Chặn tương tác {job.Type}";
                            }
                        }
                       
                    }
                    if (type == JobTypes.Follow)
                    {
                        if (_platform == "Instagram")
                        {
                            bool check = xml.Contains("requested");
                            if (!check)
                            {
                                check = (_client.FindElements(1, "", "//*[@resource-id=\"com.instagram.android:id/profile_header_user_action_follow_button\"]").Any() && _client.FindElements(1, "", "//*[@resource-id=\"com.instagram.android:id/profile_header_user_action_follow_button\"]")[0].OuterXml.ToLower().Contains("following"));
                            }
                            if (!check)
                            {
                                subdy.SubdyEnum = SubdyEnum.Block;
                                subdy.Message = $"Chặn tương tác {job.Type}";
                            }
                        }

                    }
                    if (type == JobTypes.JoinGroup)
                    {
                        if (_platform == PlatformModel.Facebook && !xml.Contains("joined"))
                        {
                            subdy.SubdyEnum = SubdyEnum.Block;
                            subdy.Message = $"Chặn tương tác {job.Type}";
                        }
                    }
                }
            }
            _client.ADB.Shell("input keyevent 4");
            return subdy;
        }
        private async Task<SubdyExtension> ReportJob(JobModel job, SubdyExtension subdy, List<string> idJobs = null)
        {
            if (subdy.SubdyEnum == SubdyEnum.Success)
            {
                try
                {
                    if (_platform == PlatformModel.Facebook)
                    {
                        if (_config.JobService == "Subdy")
                        {
                            var message = SubdyClient.ClaimJob(_tokenJobService, Convert.ToInt32(job.JobId), _account.Uid);
                            return subdy;
                        }

                        subdy.Message = await JobClient.ReportFacebookJob(_jobService, _account.Uid, _account.FullName, _tokenJobService, job, _jobPrefix);
                    }
                    else if (_platform == "Instagram")
                    {
                        if (_jobService == "https://app.golike.net/")
                        {
                            var message = await new GoLikeClient().ReportInstagramJob(job.JobId, _infoAccountService["id"], _account.TokenJob);
                            if (message.ContainsKey("error"))
                            {
                                throw new Exception(message["error"]);
                            }
                            subdy.Message = message["success"];
                        }
                        else if (_jobService == "https://vipig.net/" && job.Type == JobTypes.Like)
                        {
                            var reward = await new VipIGClient(_cookieService).ClaimLikeReward(job.ObjectId);
                            string message = (reward["mess"] ?? reward["error"])?.ToString();
                            subdy.Message = message;
                        }
                        else if (_jobService == "https://vipig.net/" && job.Type == JobTypes.Follow)
                        {
                            string idList = string.Join(",", idJobs);
                            idJobs.Clear();
                            var reward = await new VipIGClient(_cookieService).ClaimFollowReward(idList);
                            string message = (reward["mess"] ?? reward["error"])?.ToString();
                            subdy.Message = message;
                        }
                    }
                }
                catch (Exception ex)
                {
                    subdy.SubdyEnum = SubdyEnum.JobFail;
                    subdy.Message = ex.Message;
                }
            }

            return subdy;
        }
        private void EnsureKey(string key)
        {
            if (!_doJobInfo.ContainsKey(key))
                _doJobInfo[key] = 0;
        }
        private void HanderJob(SubdyExtension subdy, JobModel job)
        {

            string type = job.Type;
            EnsureKey(type);
            EnsureKey($"{type}_fail");
            EnsureKey($"{type}_faillientiep");

            switch (subdy.SubdyEnum)
            {
                case SubdyEnum.LogOut:
                    throw subdy;

                case SubdyEnum.Block:
                    _blockJobTuongTac++;
                    _doJobInfo[$"{type}_faillientiep"]++;
                    _doJobInfo[$"{type}_fail"]++;
                    break;

                case SubdyEnum.JobFail:
                    _jobFail_LienTiep++;
                    _doJobInfo[$"{type}_faillientiep"]++;
                    _doJobInfo[$"{type}_fail"]++;
                    break;

                case SubdyEnum.Success:
                    _jobFail_LienTiep = 0;
                    _doJobInfo[type]++;
                    _doJobInfo[$"{type}_faillientiep"] = 0;
                    _account.JobTotal++;

                    // Kiểm tra ngay sau khi tăng counter: nếu đã đủ lượt thì remove loại job này luôn
                    var matchedCheck = _jobLimitChecks.FirstOrDefault(c => c.Type == type);
                    if (matchedCheck.Type != null && _job_types.Contains(type))
                    {
                        int limit = SubdyHelper.RandomValue(
                            _settingScriptAction.GetIntType(matchedCheck.Low, 100),
                            _settingScriptAction.GetIntType(matchedCheck.High, 500));
                        if (_doJobInfo[type] >= limit)
                        {
                            _job_types.Remove(type);
                        }
                    }
                    break;
            }

            _countJob++;
        }
        private async Task<SubdyExtension> JobReaction(JobModel job)
        {
            if (_platform == PlatformModel.Facebook)
            {
                string url = "";
                bool isLink = false;
                List<string> urls = new List<string>();
                //if (_config.JobService == "Subdy")
                //{
                //    urls.Add(job.Link);
                //}
                //else
                //{
                //    url = await FacebookHander.GetUrlByObjectId(job.ObjectId);
                //    if (string.IsNullOrEmpty(url))
                //    {
                //        //string charString = SubdyHelper.RandomString("abcdefghijklmnopqrstuvwxyz", SubdyHelper.RandomValue(3, 10));
                //        urls = new List<string>
                //{
                //    $"https://www.facebook.com/abc/posts/{job.ObjectId}",
                //    $"https://www.facebook.com/photo/?fbid={job.ObjectId}",
                //    $"https://www.facebook.com/permalink.php?story_fbid={job.ObjectId}"
                //};

                //    }
                //    else
                //    {
                //        urls.Add(url);
                //    }
                //}
                urls.Add(job.ObjectId);
                //   urls.Add($"fb://faceweb/f?href=https://www.facebook.com/{SubdyHelper.RandomString("0123456789", SubdyHelper.RandomValue(6, 20))}/posts/{job.ObjectId}");
                foreach (string link in urls)
                {
                    isLink = await GotoUrl(link);
                    if (isLink)
                    {
                        url = link;
                        break;
                    }
                    _client.Shell("input keyevent 4");
                }
                if (!isLink)
                {
                    return new SubdyExtension(SubdyEnum.JobFail, $"Job: {job.ObjectId?.Substring(0, 3)}... fail. Không tồn tại bài viết.");
                }
            ReFail:
                isLink = false;
                for (int i = 0; i < 10; i++)
                {
                    string dump = _client.GetXMLSource();
                    if (string.IsNullOrEmpty(dump))
                    {
                        continue;
                    }
                    if (!_client.ElementWithAttributes(new List<string> { "//*[contains(@text,\"Share\")]", "//*[contains(@content-desc,\"Share\")]" }, 5, dump, false) || !dump.Contains("share", StringComparison.OrdinalIgnoreCase) && !dump.Contains("like", StringComparison.OrdinalIgnoreCase))
                    {
                        _client.SwipeUp(1, SubdyHelper.RandomValue(500, 2000), SubdyHelper.RandomValue(500, 2000));
                        //  _client.SwipeByPercent(56, 82, 56, 16, 1000);
                        continue;
                    }
                    if (dump.Contains(", pressed. double tap and hold"))
                    {
                        return new SubdyExtension(SubdyEnum.JobFail, $"Job: {job.ObjectId?.Substring(0, 3)}... fail. Đã làm job đó trước.");
                    }
                    isLink = true;
                    break;
                }
                if (!isLink)
                {
                    return new SubdyExtension(SubdyEnum.JobFail, $"Job: {job.ObjectId?.Substring(0, 3)}... fail. Không tìm được nút {job.Type}.");
                }
                job.Link = url;
                int second = SubdyHelper.RandomValue(_settingScriptAction.GetIntType("numericUpDown26", 5), _settingScriptAction.GetIntType("numericUpDown4", 10));
                await DelayMessageAsync(second, "Delay trước khi click tương tác" + ". Đợi {time} giây", 2);
                if (_client.ElementWithAttributes("//*[@content-desc=\"Navigate to your Reels profile\"]", 5, click: false))
                {
                    var elementLike = _client.FindPoint("//*[contains(@text, \"reactions\")]", 15);
                    string type = job.Type.ToLower();
                    if (elementLike != null && elementLike != System.Drawing.Point.Empty)
                    {
                        string num = job.Type.ToLower();

                        _client.LongClick(elementLike.X, elementLike.Y, 1000);
                        if (!type.Contains("like"))
                        {
                            num = char.ToUpper(type[0]) + type.Substring(1);
                        }
                        if (_client.ElementWithAttributes($"//*[@content-desc='{num}']", 3))
                        {
                            return new SubdyExtension(SubdyEnum.Success, $"Job: {job.ObjectId?.Substring(0, 3)}... success.");
                        }
                    }
                }
                else
                {
                    var element = _client.FindElement("", new List<string> { "//*[@content-desc=\"Tap to open more options\"]", "//*[contains(@content-desc, 'Like button')]", "//*[contains(@content-desc, 'Like. Double')]" }, 5);
                    if (element == "//*[@content-desc=\"Tap to open more options\"]")
                    {
                        _client.ElementWithAttributes(element, 5);
                        _client.ElementWithAttributes("//*[@content-desc=\"Hide\"]", 5);
                        goto ReFail;
                    }
                    var elementLike = _client.FindPoint(element, 15);
                    string type = job.Type.ToLower();
                    if (elementLike != null && elementLike != System.Drawing.Point.Empty)
                    {
                        string num = job.Type.ToLower();

                        _client.LongClick(elementLike.X, elementLike.Y, 1000);
                        if (!type.Contains("like"))
                        {
                            num = char.ToUpper(type[0]) + type.Substring(1);
                        }
                        if (_client.ElementWithAttributes($"//*[@content-desc='{num}']", 3))
                        {
                            return new SubdyExtension(SubdyEnum.Success, $"Job: {job.ObjectId?.Substring(0, 3)}... success.");
                        }
                    }
                }
            }
            else if (_platform == "Instagram")
            {
                string link = $"instagram://media?id={job.ObjectId}";
                if (!await GotoUrl(link))
                {
                    return new SubdyExtension(SubdyEnum.JobFail, $"Job: {job.ObjectId?.Substring(0, 3)}... fail. Không tồn tại bài viết.");
                }
                bool isLink = false;
                for (int i = 0; i < 10; i++)
                {
                    string dump = _client.GetXMLSource();
                    if (string.IsNullOrEmpty(dump))
                    {
                        continue;
                    }
                    dump = dump.ToLower();
                    if (_client.ElementWithAttributes("//*[@content-desc=\"Liked\"]", 1, dump, false))
                    {
                        return new SubdyExtension(SubdyEnum.JobFail, $"Job: {job.ObjectId?.Substring(0, 3)}... fail. Đã làm job đó trước.");
                    }
                    if (_client.ElementWithAttributes("//*[@content-desc=\"Like\"]", 1, dump, false))
                    {
                        isLink = true;
                        break;
                    }
                    _client.SwipeByPercent(56, 82, 56, 16, 1000);
                }
                if (!isLink)
                {
                    return new SubdyExtension(SubdyEnum.JobFail, $"Job: {job.ObjectId?.Substring(0, 3)}... fail. Không tìm được nút {job.Type}.");
                }
                job.Link = link;
                int second = SubdyHelper.RandomValue(_settingScriptAction.GetIntType("numericUpDown26", 5), _settingScriptAction.GetIntType("numericUpDown4", 10));
                await DelayMessageAsync(second, "Delay trước khi click tương tác" + ". Đợi {time} giây", 2);
                if (_client.ElementWithAttributes("//*[@content-desc=\"Like\"]", 5, "", true))
                {
                    return new SubdyExtension(SubdyEnum.Success, $"Job: {job.ObjectId?.Substring(0, 3)}... success.");
                }
            }
            return new SubdyExtension(SubdyEnum.JobFail, $"Job: {job.ObjectId?.Substring(0, 3)}... fail. Không tìm được nút {job.Type}.");
        }
        private async Task<bool> GotoUrl(string url)
        {
            List<string> xpaths = new List<string>
            {
                "//*[contains(@text, \"go to news feed\")]",
                "//*[contains(@text, \"content not found\")]",
                "//*[@text=\"Page Not Found\"]",
                "//*[@text=\"Connection lost\"]",
                "//*[@text=\"The page you requested was not found.\"]",
                "//*[@content-desc=\"Go to profile\"]",
                "//*[@text=\"Sorry, this page isn't available.\"]",
                "//*[@text=\"The link you followed may be broken, or the page may have been removed. \"]",
                "//*[@content-desc=\"Navigate to your Reels profile\"]"
            };
            if (url.Contains("posts"))
            {
                xpaths.AddRange(new string[]{
                "//*[@text=\"From your messages.\"]",
                "//*[@content-desc=\"Close\"]",
                "//*[@content-desc=\"Search\"]",
                });
            }
            else if (url.Contains("photo"))
            {
                xpaths.AddRange(new string[]{
                 "//*[@text=\"From your messages.\"]",
                "//*[@content-desc=\"Back\"]",
                "//*[@content-desc=\"More\"]"});
            }
            else if (url.Contains("instagram://media") || url.Contains("instagram://user"))
            {
                xpaths.Add("//*[@resource-id=\"com.instagram.android:id/action_bar_button_back\"]");
                xpaths.Add("//*[@resource-id=\"com.instagram.android:id/profile_header_actions_top_row\"]");
                xpaths.Add("//*[@resource-id=\"com.instagram.android:id/action_bar_new_title_container\"]");
                xpaths.Add("//*[@resource-id=\"com.instagram.android:id/media_option_button\"]");
                xpaths.Add("//*[@text=\"Follow\"]");
                xpaths.Add("//*[contains(@text, 'Follow')]");
                xpaths.Add("//*[contains(@content-desc, 'Follow')]");
            }
            else if (url.Contains("groups"))
            {
                xpaths.Add("//*[contains(@content-desc, 'Join')]");
                xpaths.Add("//*[contains(@content-desc, 'joined')]");
            }
            else if (url.Contains("page"))
            {
                xpaths.Add("//*[@text=\"Liked\"]");
                xpaths.Add("//*[@text=\"Like\"]");
            }
            else if (url.Contains("profile"))
            {
                xpaths.Add("//*[@text=\"Following\"]");
                xpaths.Add("//*[@text=\"Follow\"]");
            }

            if (_platform == PlatformModel.Facebook)
            {
                _client.ADB.Shell($"am start -n com.facebook.katana/com.facebook.katana.IntentUriHandler -d \"{url}\"");
            }
            else if (_platform == "Instagram")
            {
                _client.ADB.Shell($"am start -a android.intent.action.VIEW -d \"{url}\" -p com.instagram.android");
            }

            await Task.Delay(5000);
            var xpath = _client.FindElement("", xpaths, 20);
            switch (xpath)
            {
                case "//*[@text=\"Sorry, this page isn't available.\"]":
                case "//*[@text=\"The link you followed may be broken, or the page may have been removed. \"]":
                case "//*[contains(@text, \"go to news feed\")]":
                case "//*[contains(@text, \"content not found\")]":
                case "//*[@text=\"Page Not Found\"]":
                case "//*[@text=\"The page you requested was not found.\"]":
                case "//*[@content-desc=\"Go to profile\"]":
                    return false;
                case "//*[@content-desc=\"Close\"]":
                case "//*[@content-desc=\"Search\"]":
                    {
                        if (_client.ElementWithAttributes("//*[@content-desc=\"Close\"]", 1, click: false) && _client.ElementWithAttributes("//*[@content-desc=\"Search\"]", 1, click: false))
                        {
                            return true;
                        }
                        return false;
                    }
                case "//*[@content-desc=\"Navigate to your Reels profile\"]":
                case "//*[@text=\"From your messages.\"]":
                    {
                        return true;
                    }
                case "//*[contains(@content-desc, 'joined')]":
                case "//*[contains(@content-desc, 'Join')]":
                    {
                        return true;
                    }
                case "//*[@text=\"Liked\"]":
                case "//*[@text=\"Like\"]":
                    {
                        return true;
                    }
                case "//*[@text=\"Following\"]":
                    {
                        return true;
                    }
                case "//*[@content-desc=\"Back\"]":
                case "//*[@content-desc=\"More\"]":
                    {
                        if (_client.ElementWithAttributes("//*[@content-desc=\"Back\"]", 1, click: false) && _client.ElementWithAttributes("//*[@content-desc=\"More\"]", 1, click: false))
                        {
                            return true;
                        }
                        return false;
                    }
                case "//*[@text=\"Follow\"]":
                case "//*[contains(@text, 'Follow')]":
                case "//*[contains(@content-desc, 'Follow')]":
                case "//*[@resource-id=\"com.instagram.android:id/media_option_button\"]":
                case "//*[@resource-id=\"com.instagram.android:id/action_bar_new_title_container\"]":
                case "//*[@resource-id=\"com.instagram.android:id/profile_header_actions_top_row\"]":
                case "//*[@resource-id=\"com.instagram.android:id/action_bar_button_back\"]":
                    {
                        return true;
                    }
                case "//*[@text=\"Connection lost\"]":
                    {
                        throw new SubdyExtension(SubdyEnum.Error, "Mất kết nối internet.");
                    }
            }
            return false;
        }
        private async Task<SubdyExtension> JobFollow(JobModel job)
        {
            if (_platform == PlatformModel.Facebook)
            {
                job.ObjectId = job.JobId;
                string link = $"fb://profile/{job.JobId}";
                if (!await GotoUrl(link))
                {
                    return new SubdyExtension(SubdyEnum.JobFail, $"Job: {job.ObjectId?.Substring(0, 3)}... fail. Không tồn tại profile.");
                }
                if (_client.ElementWithAttributes("//*[@text=\"Following\"]", 5, "", false))
                {
                    return new SubdyExtension(SubdyEnum.JobFail, $"Job: {job.ObjectId?.Substring(0, 3)}... fail. Đã làm job đó trước.");
                }
                job.Link = link;
                int second = SubdyHelper.RandomValue(_settingScriptAction.GetIntType("numericUpDown26", 5), _settingScriptAction.GetIntType("numericUpDown4", 10));
                await DelayMessageAsync(second, "Delay trước khi click tương tác" + ". Đợi {time} giây", 2);
                if (_client.ElementWithAttributes("//*[@text=\"Follow\"]", 5))
                {
                    return new SubdyExtension(SubdyEnum.Success, $"Job: {job.ObjectId?.Substring(0, 3)}... success.");
                }
            }
            else if (_platform == "Instagram")
            {
                string link = $"instagram://user?username={job.ObjectId}";
                if (!await GotoUrl(link))
                {
                    return new SubdyExtension(SubdyEnum.JobFail, $"Job: {job.ObjectId?.Substring(0, 3)}... fail. Không tồn tại bài viết.");
                }
                bool isLink = false;
                for (int i = 0; i < 10; i++)
                {
                    string dump = _client.GetXMLSource();
                    if (string.IsNullOrEmpty(dump))
                    {
                        continue;
                    }
                    dump = dump.ToLower();
                    if (_client.ElementWithAttributes("//*[@text=\"Requested\"]", 1, dump, false) || (_client.FindElements(1, "", "//*[@resource-id=\"com.instagram.android:id/profile_header_user_action_follow_button\"]").Any() && _client.FindElements(1, "", "//*[@resource-id=\"com.instagram.android:id/profile_header_user_action_follow_button\"]")[0].OuterXml.ToLower().Contains("following")))
                    {
                        return new SubdyExtension(SubdyEnum.JobFail, $"Job: {job.ObjectId?.Substring(0, 3)}... fail. Đã làm job đó trước.");
                    }
                    if (_client.ElementWithAttributes(new List<string> { "//*[@text=\"Follow\"]", "//*[contains(@text, 'Follow')]", "//*[contains(@content-desc, 'Follow')]" }, 1, dump, false))
                    {
                        isLink = true;
                        break;
                    }
                }
                if (!isLink)
                {
                    return new SubdyExtension(SubdyEnum.JobFail, $"Job: {job.ObjectId?.Substring(0, 3)}... fail. Không tìm được nút {job.Type}.");
                }
                job.Link = link;
                int second = SubdyHelper.RandomValue(_settingScriptAction.GetIntType("numericUpDown26", 5), _settingScriptAction.GetIntType("numericUpDown4", 10));
                await DelayMessageAsync(second, "Delay trước khi click tương tác" + ". Đợi {time} giây", 2);
                if (_client.ElementWithAttributes(new List<string> { "//*[@text=\"Follow\"]", "//*[contains(@text, 'Follow')]", "//*[contains(@content-desc, 'Follow')]" }, 5, "", true))
                {
                    return new SubdyExtension(SubdyEnum.Success, $"Job: {job.ObjectId?.Substring(0, 3)}... success.");
                }
            }
            return new SubdyExtension(SubdyEnum.JobFail, $"Job: {job.ObjectId?.Substring(0, 3)}... fail. Không tìm được nút {job.Type}.");
        }
        private async Task<SubdyExtension> JobLikePage(JobModel job)
        {
            if (_platform == PlatformModel.Facebook)
            {
                job.ObjectId = job.JobId;
                string link = $"fb://page/{job.JobId}";
                if (!await GotoUrl(link))
                {
                    return new SubdyExtension(SubdyEnum.JobFail, $"Job: {job.ObjectId?.Substring(0, 3)}... fail. Không tồn tại page.");
                }
                if (_client.ElementWithAttributes("//*[@text=\"Liked\"]", 5, "", false))
                {
                    return new SubdyExtension(SubdyEnum.JobFail, $"Job: {job.ObjectId?.Substring(0, 3)}... fail. Đã làm job đó trước.");
                }
                job.Link = link;
                int second = SubdyHelper.RandomValue(_settingScriptAction.GetIntType("numericUpDown26", 5), _settingScriptAction.GetIntType("numericUpDown4", 10));
                await DelayMessageAsync(second, "Delay trước khi click tương tác" + ". Đợi {time} giây", 2);
                if (_client.ElementWithAttributes("//*[@text=\"Like\"]", 5))
                {
                    return new SubdyExtension(SubdyEnum.Success, $"Job: {job.ObjectId?.Substring(0, 3)}... success.");
                }
            }
            return new SubdyExtension(SubdyEnum.JobFail, $"Job: {job.ObjectId?.Substring(0, 3)}... fail. Không tìm được nút {job.Type}.");
        }
        private async Task<SubdyExtension> JobGroup(JobModel job)
        {
            if (_platform == PlatformModel.Facebook)
            {
                job.ObjectId = job.JobId;
                string link = $"https://www.facebook.com/groups/{job.JobId}";
                if (!await GotoUrl(link))
                {
                    return new SubdyExtension(SubdyEnum.JobFail, $"Job: {job.ObjectId?.Substring(0, 3)}... fail. Không tồn tại group.");
                }
                if (_client.ElementWithAttributes("//*[contains(@content-desc, 'joined')]", 5, "", false))
                {
                    return new SubdyExtension(SubdyEnum.JobFail, $"Job: {job.ObjectId?.Substring(0, 3)}... fail. Đã làm job đó trước.");
                }
                job.Link = link;
                int second = SubdyHelper.RandomValue(_settingScriptAction.GetIntType("numericUpDown26", 5), _settingScriptAction.GetIntType("numericUpDown4", 10));
                await DelayMessageAsync(second, "Delay trước khi click tương tác" + ". Đợi {time} giây", 2);
                if (_client.ElementWithAttributes("//*[contains(@content-desc, 'Join')]", 5))
                {
                    return new SubdyExtension(SubdyEnum.Success, $"Job: {job.ObjectId?.Substring(0, 3)}... success.");
                }
            }
            return new SubdyExtension(SubdyEnum.JobFail, $"Job: {job.ObjectId?.Substring(0, 3)}... fail. Không tìm được nút {job.Type}.");
        }
        private async Task<SubdyExtension> JobShare(JobModel job)
        {
            return null;
        }
    }

}
