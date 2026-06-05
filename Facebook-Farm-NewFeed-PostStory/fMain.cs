using AntdUI;
using Facebook_Farm_NewFeed_PostStory.Utils;
using Facebook_Farm_NewFeed_PostStory.Utils.Design;
using Facebook_Farm_NewFeed_PostStory.Views.Controls;
using Facebook_Farm_NewFeed_PostStory.Views.Forms;
using Sunny.Subdy.Common.API;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Models;
using Sunny.Subdy.Common.Services;
using Sunny.Subdy.UI.View.Pages;
using System.Diagnostics;
using System.Reflection;


using CommonMethod = Sunny.Subdy.Common.ControlMethod.CommonMethod;
namespace Facebook_Farm_NewFeed_PostStory
{
    public partial class fMain : AntdUI.Window
    {
        private static readonly Color ActiveColor = Color.DodgerBlue;
        private static readonly Color HoverColor = Color.FromArgb(236, 240, 241);
        private static readonly Color InactiveText = Color.Black;

        private CancellationTokenSource _uiCts;
        private CancellationTokenSource _loadingCts;
        private DateTime _lastUiUpdate = DateTime.MinValue;
        private DateTime _lastHistoriesUpdate = DateTime.MinValue;
        private DateTime? _lastCheckUpdateTime;
        private bool _updateConfirmOpen;
        private System.Windows.Forms.Panel _loadingOverlay;
        private Control _currentButton;

        public static DateTime? StartTime = null;

        public fMain()
        {
            InitializeComponent();

            // Dọn các kịch bản seed mặc định (FarmXu + "Làm Job QN") và clear NameScript
            // của account đang gán các kịch bản đó. Project mới không dùng các kịch bản này.
            // Defer sang background để không kéo dài constructor.
            _ = Task.Run(() =>
            {
                try
                {
                    var scriptCtx = new Sunny.Subdy.Data.Context.ScriptContext();
                    scriptCtx.PurgeFarmXu();
                    var accCtx = new Sunny.Subdy.Data.Context.AccountContext();
                    foreach (var platform in new[] { Sunny.Subdy.Common.Models.PlatformModel.Facebook, Sunny.Subdy.Common.Models.PlatformModel.Instagram, Sunny.Subdy.Common.Models.PlatformModel.Threads })
                    {
                        var scripts = scriptCtx.GetByPlatform(platform);
                        if (scripts != null)
                        {
                            foreach (var s in scripts.Where(s =>
                                string.Equals(s.Name, Sunny.Subdy.Data.Context.ScriptNames.FarmXuVip, StringComparison.OrdinalIgnoreCase)
                                || string.Equals(s.Name, Sunny.Subdy.Data.Context.ScriptNames.FarmXuVipLegacy, StringComparison.OrdinalIgnoreCase)))
                            {
                                scriptCtx.DeleteById(s.Id);
                            }
                        }

                        var all = accCtx.GetAll(new List<string>(), platform, true);
                        if (all == null) continue;
                        var legacy = all.Where(a =>
                            string.Equals(a.NameScript, Sunny.Subdy.Data.Context.ScriptNames.FarmXu, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(a.NameScript, Sunny.Subdy.Data.Context.ScriptNames.FarmXuVip, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(a.NameScript, Sunny.Subdy.Data.Context.ScriptNames.FarmXuVipLegacy, StringComparison.OrdinalIgnoreCase)
                        ).ToList();
                        if (legacy.Count == 0) continue;
                        foreach (var a in legacy) a.NameScript = "";
                        accCtx.Update(legacy);
                    }
                }
                catch (Exception ex) { Sunny.Subdy.Common.Logs.LogManager.Error(ex); }
            });

            // Tạo menu động (thứ tự ngược do DockStyle.Top stacking).
            // Project mới chỉ giữ Facebook + Thiết bị — bỏ Dashboard/IG/Threads.
            CreateMenu("Facebook", "facebook", Properties.Resources.icons8_facebook_30);
            CreateMenu("Thiết bị", "android", Properties.Resources.icons8_android_30_New);

            // Title cố định — không đổi theo section
            windowBar.Text = "QNAutoPhone";

            // SSA visual redesign — chỉ đụng UI, không đổi business logic
            SsaTheme.ApplyFMain(this);

            this.Load += fMain_Load;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Normal;

            ApplySmoothUI();
            CreateLoadingOverlay();

            pMenu.Enabled = false;
            FontUtil.ApplyFontToAllControls(this);
            new DragHandler(label1, this);

            string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
            windowBar.SubText = $"v{version}";

            // Tooltip cho các nút title bar
            var toolTip = new ToolTip { AutoPopDelay = 3000, InitialDelay = 300, ReshowDelay = 200 };
            toolTip.SetToolTip(btn_mode, "Thu nhỏ");
            toolTip.SetToolTip(btn_global, "Phóng to / Thu nhỏ");
            toolTip.SetToolTip(btn_setting, "Đóng");

            // Overload alert: subscribe event để hiện toast cảnh báo khi CPU/RAM cao kéo dài.
            SystemUsageMonitor.OverloadDetected += OnSystemOverload;
            this.FormClosed += (_, __) => SystemUsageMonitor.OverloadDetected -= OnSystemOverload;
        }

        private void OnSystemOverload(string resource, float value)
        {
            if (this.IsDisposed || !this.IsHandleCreated) return;
            try
            {
                this.BeginInvoke(new Action(() =>
                {
                    AntdHelper.NotifyWarn(
                        this,
                        $"Tài nguyên {resource} cao",
                        $"{resource} đang ở mức {value:0.0}% trong hơn {SystemUsageMonitor.SustainedSeconds}s. " +
                        "Cân nhắc giảm số thiết bị chạy song song.");
                }));
            }
            catch { /* form đang đóng — bỏ qua */ }
        }

        #region ==== Menu ====

        private void CreateMenu(string text, string name, Image icon)
        {
            var leftBar = new System.Windows.Forms.Panel
            {
                BackColor = ActiveColor,
                Dock = DockStyle.Left,
                Width = 10,
                Visible = false,
                TabStop = false
            };

            var btn = new System.Windows.Forms.Button
            {
                Cursor = Cursors.Hand,
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0, MouseDownBackColor = Color.White },
                Font = new Font(FontScale.FamilyName, 11.25F, FontStyle.Bold),
                ForeColor = Color.Black,
                Image = icon,
                ImageAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(15, 0, 0, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Text = text,
                Tag = leftBar,
                Name = $"btn_{name}"
            };

            btn.Click += MenuButton_Click;
            btn.MouseEnter += (s, e) => HoverButton(btn);
            btn.MouseLeave += (s, e) => UnhoverButton(btn);

            // Ẩn focus rectangle
            btn.GotFocus += (s, e) => btn.Parent?.Focus();

            var container = new System.Windows.Forms.Panel { Height = 59, Dock = DockStyle.Top };
            container.Controls.Add(btn);
            container.Controls.Add(leftBar);
            pMenu.Controls.Add(container);
        }

        private void HoverButton(System.Windows.Forms.Button btn)
        {
            if (btn == _currentButton || _loadingOverlay?.Visible == true) return;

            btn.ForeColor = ActiveColor;
            btn.BackColor = HoverColor;
            btn.Image = GetActiveIcon(btn.Name);
        }

        private void UnhoverButton(System.Windows.Forms.Button btn)
        {
            if (btn == _currentButton || _loadingOverlay?.Visible == true) return;
            ResetButtonStyle(btn);
        }

        // Khi user kéo cạnh trên / góc trên để resize, OS sinh WM_SIZE liên tục vì cả
        // origin lẫn height đều thay đổi. WinForms relayout toàn bộ control mỗi tick
        // → khựng rõ rệt so với kéo phải/trái/dưới (chỉ đổi size, không đổi origin).
        // Suspend layout trong khoảng [WM_ENTERSIZEMOVE..WM_EXITSIZEMOVE] để gom layout
        // lại 1 lần duy nhất khi user nhả chuột — kéo lên mượt như các hướng còn lại.
        private const int WM_ENTERSIZEMOVE = 0x0231;
        private const int WM_EXITSIZEMOVE = 0x0232;
        private bool _resizeLayoutSuspended;

        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            if (m.Msg == WM_ENTERSIZEMOVE && !_resizeLayoutSuspended)
            {
                _resizeLayoutSuspended = true;
                SuspendLayout();
            }
            else if (m.Msg == WM_EXITSIZEMOVE && _resizeLayoutSuspended)
            {
                _resizeLayoutSuspended = false;
                ResumeLayout(true);
                PerformLayout();
                Invalidate(true);
            }
            base.WndProc(ref m);
        }

        protected override bool ProcessCmdKey(ref System.Windows.Forms.Message msg, Keys keyData)
        {
            // Ctrl+1..5 → chuyển nhanh giữa các tab menu.
            // Thứ tự hiển thị (top→bottom): Thiết bị, Facebook, Instagram, Thread, Dashboard.
            if ((keyData & Keys.Control) == Keys.Control)
            {
                string? target = (keyData & Keys.KeyCode) switch
                {
                    Keys.D1 => "btn_android",
                    Keys.D2 => "btn_facebook",
                    _ => null
                };
                if (target != null)
                {
                    var btn = pMenu.Controls
                        .OfType<System.Windows.Forms.Panel>()
                        .SelectMany(p => p.Controls.OfType<System.Windows.Forms.Button>())
                        .FirstOrDefault(b => b.Name == target);
                    if (btn != null && pMenu.Enabled && _loadingOverlay?.Visible != true)
                    {
                        btn.PerformClick();
                        return true;
                    }
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void MenuButton_Click(object sender, EventArgs e)
        {
            if (_loadingOverlay?.Visible == true || !pMenu.Enabled) return;

            if (sender is not System.Windows.Forms.Button btn) return;

            // Reset button cũ
            if (_currentButton is System.Windows.Forms.Button old)
            {
                if (old.Tag is System.Windows.Forms.Panel oldPanel) oldPanel.Visible = false;
                ResetButtonStyle(old);
            }

            // Đặt active cho button mới
            _currentButton = btn;
            if (btn.Tag is System.Windows.Forms.Panel p) p.Visible = true;

            SetButtonActive(btn);
        }

        private void SetButtonActive(System.Windows.Forms.Button btn)
        {
            btn.ForeColor = ActiveColor;
            btn.BackColor = HoverColor;
            btn.Image = GetActiveIcon(btn.Name);

            string labelText = btn.Text switch
            {
                "Thiết bị" => "Quản lý thiết bị",
                "Facebook" => "Quản lý tài khoản Facebook",
                "Instagram" => "Quản lý tài khoản Instagram",
                "Thread" => "Quản lý tài khoản Thread",
                "Dashboard" => "Dashboard",
                "Lịch sử" => "Dashboard",
                _ => btn.Text
            };
            label1.Text = labelText;
            // windowBar.Text giữ cố định ("QNAutoPhone") — không đổi theo section

            // UC nặng (Devices/Instagram/Threads/Histories) được lazy-create sau frame đầu.
            // Nếu user click trước khi sẵn sàng → no-op, sẽ vào tab khi UC tạo xong.
            switch (btn.Name)
            {
                case "btn_android": _ucDevices?.BringToFront(); break;
                case "btn_facebook": _ucFacebook?.BringToFront(); break;
            }

            // Chỉ hiện button thu gọn/mở rộng panel khi ở tab Thiết bị
            if (_ucDevices != null)
                _ucDevices.TogglePanelButtonVisible = btn.Name == "btn_android";
        }

        private void ResetButtonStyle(System.Windows.Forms.Button btn)
        {
            btn.ForeColor = InactiveText;
            btn.BackColor = Color.White;
            btn.Image = GetNormalIcon(btn.Name);
        }

        private Image GetActiveIcon(string name) => name switch
        {
            "btn_android" => Properties.Resources.icons8_android_30_Acti,
            "btn_facebook" => Properties.Resources.icons8_facebook_30_Acti,
            "btn_instagram" => Properties.Resources.icons8_instagram_30_Acti,
            "btn_threads" => Properties.Resources.icons8_threads_30_Acti,
            "btn_history" => Properties.Resources.icons8_history_30_Acti,
            _ => null
        };

        private Image GetNormalIcon(string name) => name switch
        {
            "btn_android" => Properties.Resources.icons8_android_30_New,
            "btn_facebook" => Properties.Resources.icons8_facebook_30,
            "btn_instagram" => Properties.Resources.icons8_instagram_30,
            "btn_threads" => Properties.Resources.icons8_threads_30,
            "btn_history" => Properties.Resources.icons8_history_30,
            _ => null
        };

        #endregion

        #region ==== Loading ====

        private void CreateLoadingOverlay()
        {
            _loadingOverlay = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 255, 255, 255),
                Cursor = Cursors.WaitCursor
            };

            var spin = new AntdUI.Spin
            {
                Dock = DockStyle.Fill,
                Font = new Font(FontScale.FamilyName, 16f, FontStyle.Bold),
                Text = "Đang khởi động...",
                ForeColor = Color.FromArgb(70, 70, 70)
            };

            _loadingOverlay.Controls.Add(spin);
            Controls.Add(_loadingOverlay);
            _loadingOverlay.BringToFront();

            var messages = new[] { "Đang tải UI...", "Đang tải dữ liệu...", "Đang đồng bộ...", "Đang khởi động hệ thống...", "QN Phone Farm xin chào!" };

            _loadingCts = new CancellationTokenSource();
            var token = _loadingCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    int i = 0;
                    while (!token.IsCancellationRequested)
                    {
                        if (!spin.IsHandleCreated)
                        {
                            await Task.Delay(100, token);
                            continue;
                        }

                        spin.Invoke(new Action(() =>
                        {
                            if (!spin.IsDisposed)
                                spin.Text = messages[i];
                        }));

                        i = (i + 1) % messages.Length;
                        await Task.Delay(500, token); // tăng delay cho dễ đọc
                    }
                }
                catch (TaskCanceledException)
                {
                    // bỏ qua khi overlay bị hủy
                }
            });
        }

        private void HideLoading()
        {
            if (_loadingOverlay == null) return;
            _loadingOverlay.Visible = false;
            _loadingCts?.Cancel();
            pMenu.Enabled = true;
        }

        #endregion

        #region ==== UI / Form ====

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _uiCts?.Cancel();
            _loadingCts?.Cancel();
            base.OnFormClosing(e);
        }

        private async void fMain_Load(object sender, EventArgs e)
        {
            // Register UI control for ThrottledPropertyNotifier so PropertyChanged events
            // are always marshalled to the UI thread via BeginInvoke (not SynchronizationContext,
            // which may be null at Program.Main time).
            Sunny.Subdy.Data.Models.ThrottledPropertyNotifier.Initialize();

            // Tab Facebook là tab mặc định mở đầu — tạo trước, BringToFront, hide loading
            // để user thấy UI hữu dụng ngay. Các UC khác defer sang sau (lazy create chunks)
            // → cắt được ~60-70% thời gian fMain_Load block trên UI thread.
            _ucFacebook = new ucdgvAccount(this, PlatformModel.Facebook);
            pContent.SuspendLayout();
            _ucFacebook.Dock = DockStyle.Fill;
            pContent.Controls.Add(_ucFacebook);
            EnableDoubleBuffer(_ucFacebook);
            pContent.ResumeLayout(false);
            pContent.PerformLayout();

            // Bắt đầu tải dữ liệu nền — không await ở đây để UI hiện sớm.
            var dataTask = LoadData();

            pMenu.Enabled = true;
            HideLoading();

            var first = pMenu.Controls.OfType<System.Windows.Forms.Panel>().SelectMany(p => p.Controls.OfType<System.Windows.Forms.Button>())
                .FirstOrDefault(b => b.Name == "btn_facebook");
            if (first != null)
                MenuButton_Click(first, EventArgs.Empty);

            // Yield để frame đầu (Facebook + menu) render xong, rồi mới tạo UC còn lại.
            // BeginInvoke đẩy việc về cuối WM queue → user thấy form responsive ngay,
            // các UC nặng tạo bất đồng bộ; nếu user click tab trước khi UC ready, tab đó
            // sẽ chưa xuất hiện (MenuButton_Click null-check qua field).
            await Task.Yield();
            this.BeginInvoke(new Action(CreateRemainingControls));

            // Chờ dữ liệu tải xong (không block UI render — đã hide loading trước đó).
            await dataTask;
        }

        private async void CreateRemainingControls()
        {
            // Project mới chỉ còn 2 UC: Devices + Facebook (Facebook đã tạo trong
            // fMain_Load). Chỉ cần tạo ucDevices.
            try
            {
                pContent.SuspendLayout();
                _ucDevices = new ucManagerDevices(this);
                _ucDevices.Dock = DockStyle.Fill;
                pContent.Controls.Add(_ucDevices);
                EnableDoubleBuffer(_ucDevices);
                pContent.ResumeLayout(false);
                await Task.Yield();

                // Devices xong → load list devices (đợi ADB ready trước).
                try { await Globals.AdbReadyTask; } catch { }
                if (!IsDisposed) await _ucDevices.LoadDevices();
            }
            catch (Exception ex) { Sunny.Subdy.Common.Logs.LogManager.Error(ex); }
        }

        private async Task LoadData()
        {
            try
            {
                // Chạy song song ADB init và Device models
               // var adbTask = Task.Run(() => ADBHelper.InitADB());
                var deviceTask = Task.Run(() => DeviceServices.GetDeviceModels());

                await Task.WhenAll(deviceTask);

                // Đợi device-id hoàn tất (chạy nền từ Program.Main).
                try { await Globals.DeviceIdTask; } catch { }

                _ = UpdateUiLoop();
            }
            catch (Exception ex)
            {
                CommonMethod.ShowMessageError(ex.Message);
            }
        }

        private async Task UpdateUiLoop()
        {
            _uiCts = new CancellationTokenSource();
            var token = _uiCts.Token;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.Now;
                    bool minimized = this.WindowState == FormWindowState.Minimized;

                    if ((now - _lastUiUpdate).TotalMilliseconds >= (minimized ? 10000 : 3000))
                    {
                        var cpuTask = SystemUsageMonitor.GetCpuUsage();
                        var ram = SystemUsageMonitor.GetRamUsage();
                        var cpu = await cpuTask;
                        ControlHelper.SetToolStripLabelTextSafe(uiLabel5, $"{cpu:0.00}%");
                        ControlHelper.SetToolStripLabelTextSafe(uiLabel6, $"{ram:0.00}%");
                        SystemUsageMonitor.CheckOverload(cpu, ram);
                        _lastUiUpdate = now;
                    }

                    if (StartTime.HasValue)
                    {
                        var elapsed = DateTime.Now - StartTime.Value;
                        ControlHelper.SetToolStripLabelTextSafe(toolStripLabel5, elapsed.ToString(@"hh\:mm\:ss"));
                    }
                    else
                    {
                        ControlHelper.SetToolStripLabelTextSafe(toolStripLabel5, "00:00:00");
                    }

                    // Histories UC đã bỏ — không update gì ở vòng lặp này.

                    if (_lastCheckUpdateTime == null || (now - _lastCheckUpdateTime.Value).TotalMinutes >= 5)
                    {
                       this.BeginInvoke(new Action(CheckUpdateVersion));
                        _lastCheckUpdateTime = now;
                    }

                    await Task.Delay(minimized ? 5000 : 1000, token);
                }
                catch (TaskCanceledException) { break; }
                catch { await Task.Delay(2000, token); }
            }
        }
        private void btn_global_SelectedValueChanged(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Maximized)
            {
                this.WindowState = FormWindowState.Normal;
                return;
            }
            Rectangle workingArea = Screen.FromHandle(this.Handle).WorkingArea;
            workingArea.Location = new Point(0, 0);
            this.MaximumSize = workingArea.Size;
            this.WindowState = FormWindowState.Maximized;
            btn_global.Refresh();
        }
        public void btn_setting_Click(object sender, EventArgs e)
        {
            if (CommonMethod.ShowConfirmWarning("Bạn có chắc muốn đóng phần mềm?"))
            {
                _ucFacebook.SaveConfig();
                this.Close();
                Environment.Exit(0);
            }
            btn_setting.Refresh();
        }
        private void btn_mode_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
            btn_mode.Refresh();
        }
        private void CheckUpdateVersion()
        {
            // Nếu modal Confirm cập nhật đang hiển thị thì skip — tránh đè notification
            // và mở nhiều modal chồng nhau khi timer 5 phút tick lại.
            if (_updateConfirmOpen) return;

            string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
            var (ok, vs, url) = LamToolClient.GetApiResponseAsync(Globals.DeviceId, Globals.NameApp, version);

            if (!LamToolClient.IsNewerVersion(version, vs)) return;

            // Toast (non-blocking) thay vì modal countdown — user có thể tiếp tục dùng app.
            // Click vào notification hoặc đợi user mở Settings để cập nhật.
            AntdHelper.NotifyWarn(
                this,
                "Có bản cập nhật mới",
                $"Phiên bản [{vs}] đã sẵn sàng. Đang chuẩn bị cập nhật...");

            // Sau 3 giây mở modal confirm — giữ behaviour cũ (force update) nhưng không
            // block UI ngay lúc khởi động (tránh user nhìn modal 120s ngay khi vừa mở app).
            var t = new System.Windows.Forms.Timer { Interval = 3000 };
            t.Tick += (_, __) =>
            {
                t.Stop();
                t.Dispose();
                if (this.IsDisposed) return;
                if (_updateConfirmOpen) return;

                _updateConfirmOpen = true;
                try
                {
                    if (AntdHelper.Confirm(this, "Cập nhật phiên bản",
                            $"Đã có phiên bản [{vs}] mới nhất. Bạn có muốn cập nhật ngay bây giờ?"))
                    {
                        this.Hide();
                        using (var updateForm = new fUpdateAuto(url, version) { TopMost = true })
                        {
                            updateForm.ShowDialog(this);
                        }
                        Environment.Exit(0);
                    }
                }
                finally
                {
                    _updateConfirmOpen = false;
                }
            };
            t.Start();
        }
        private void OpenBrowser(string url)
        {
            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                url = "https://" + url;
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }

        private void ApplySmoothUI()
        {
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            this.UpdateStyles();
        }

        private void EnableDoubleBuffer(Control ctrl)
        {
            try
            {
                typeof(Control).GetProperty("DoubleBuffered", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.SetValue(ctrl, true, null);
            }
            catch { }
        }

        #endregion
    }
}
