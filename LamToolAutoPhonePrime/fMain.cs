using AntdUI;
using LamToolAutoPhonePrime.Utils;
using LamToolAutoPhonePrime.Views.Controls;
using LamToolAutoPhonePrime.Views.Forms;
using Sunny.Subdy.Common.API;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Models;
using Sunny.Subdy.Common.Services;
using Sunny.Subdy.UI.View.Pages;
using System.Diagnostics;
using System.Reflection;


namespace LamToolAutoPhonePrime
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
        private System.Windows.Forms.Panel _loadingOverlay;
        private Control _currentButton;

        public static DateTime? StartTime = null;

        public fMain()
        {
            InitializeComponent();

            // Tạo menu động (thứ tự ngược do DockStyle.Top stacking)
            CreateMenu("Lịch sử", "history", Properties.Resources.icons8_history_30);
            CreateMenu("Tài khoản", "facebook", Properties.Resources.icons8_facebook_30);
            CreateMenu("Thiết bị", "android", Properties.Resources.icons8_android_30_New);

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
            Globals.CoinLable = label8;

            // Tooltip cho các nút title bar và logout
            var toolTip = new ToolTip { AutoPopDelay = 3000, InitialDelay = 300, ReshowDelay = 200 };
            toolTip.SetToolTip(btn_mode, "Thu nhỏ");
            toolTip.SetToolTip(btn_global, "Phóng to / Thu nhỏ");
            toolTip.SetToolTip(btn_setting, "Đóng");
            toolTip.SetToolTip(button9, "Đăng xuất");

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
                Font = new Font(FontUtil._fontSemiBold, 11.25F, FontStyle.Bold),
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
                "Tài khoản" => "Quản lý tài khoản Facebook",
                "Lịch sử" => "Lịch sử hoạt động",
                _ => btn.Text
            };
            label1.Text = labelText;

            switch (btn.Name)
            {
                case "btn_android": _ucDevices.BringToFront(); break;
                case "btn_facebook": _ucFacebook.BringToFront(); break;
                case "btn_history": _ucHistoriesJob.BringToFront(); break;
            }

            // Chỉ hiện button thu gọn/mở rộng panel khi ở tab Thiết bị
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
            "btn_history" => Properties.Resources.icons8_history_30_Acti,
            _ => null
        };

        private Image GetNormalIcon(string name) => name switch
        {
            "btn_android" => Properties.Resources.icons8_android_30_New,
            "btn_facebook" => Properties.Resources.icons8_facebook_30,
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
                Font = new Font(FontUtil._fontSemiBold, 16f),
                Text = "Đang khởi động...",
                ForeColor = Color.FromArgb(70, 70, 70)
            };

            _loadingOverlay.Controls.Add(spin);
            Controls.Add(_loadingOverlay);
            _loadingOverlay.BringToFront();

            var messages = new[] { "Đang tải UI...", "Đang tải dữ liệu...", "Đang đồng bộ...", "Đang khởi động hệ thống...", "Subdy Phone Farm xin chào!" };

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

            // Hiển thị thông tin tài khoản từ Globals.User
            UpdateUserInfo();

            // Bắt đầu tải dữ liệu nền song song với việc tạo UI controls
            var dataTask = LoadData();

            // Tạo controls trên UI thread trong khi dữ liệu đang tải
            _ucDevices = new ucManagerDevices(this);
            _ucFacebook = new ucdgvAccount(this, PlatformModel.Facebook);
            _ucHistoriesJob = new ucHistoriesJob();

            pContent.SuspendLayout();
            foreach (var uc in new Control[] { _ucDevices, _ucFacebook, _ucHistoriesJob })
            {
                uc.Dock = DockStyle.Fill;
                pContent.Controls.Add(uc);
                EnableDoubleBuffer(uc);
            }
            pContent.ResumeLayout(false);
            pContent.PerformLayout();

            // Chờ dữ liệu tải xong
            await dataTask;
           await _ucDevices.LoadDevices();
            pMenu.Enabled = true;
            HideLoading();

            var first = pMenu.Controls.OfType<System.Windows.Forms.Panel>().SelectMany(p => p.Controls.OfType<System.Windows.Forms.Button>())
                .FirstOrDefault(b => b.Name == "btn_android");
            if (first != null)
                MenuButton_Click(first, EventArgs.Empty);
        }

        private void UpdateUserInfo()
        {
            var user = Globals.User;
            if (user == null) return;

            label4.Text = user.FullName ?? user.UserName ?? "Subdy.net";
            label8.Text = $"{user.Balance:N0} xu";
            label9.Text = user.Email ?? "";
        }

        private async Task LoadData()
        {
            try
            {
                // Chạy song song ADB init và Device models
               // var adbTask = Task.Run(() => ADBHelper.InitADB());
                var deviceTask = Task.Run(() => DeviceServices.GetDeviceModels());

                await Task.WhenAll(deviceTask);

                // if (!File.Exists(@"C:\DTAHelper\sdk\platform-tools\adb.exe"))
                // {
                //     CommonMethod.ShowMessageWarning("Chưa cài thư viện DTAHelper, vui lòng cài đặt lại.");
                //     OpenBrowser("https://www.dropbox.com/scl/fi/3bediza9mih9gmekxqi4n/DTAHelper.zip?dl=1");
                //     TempLoginStorage.Clear();
                //     Application.Restart();
                // }

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

                    if ((now - _lastHistoriesUpdate).TotalMilliseconds >= (minimized ? 15000 : 5000))
                    {
                        await Task.Run(() => _ucHistoriesJob.UpdateView());
                        _lastHistoriesUpdate = now;
                    }

                    if (_lastCheckUpdateTime == null || (now - _lastCheckUpdateTime.Value).TotalMinutes >= 30)
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
        private void button9_Click(object sender, EventArgs e)
        {
            if (CommonMethod.ShowConfirmWarning("Bạn có chắc muốn đăng xuất tài khoản ra khỏi phần mềm?"))
            {
                Program.SetStartup(false);
                TempLoginStorage.Clear();
                Application.Restart();
                Environment.Exit(0);
            }
        }
        private void btn_mode_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
            btn_mode.Refresh();
        }
        private void CheckUpdateVersion()
        {
            string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
            var (ok, vs, url) = LamToolClient.GetApiResponseAsync(Globals.DeviceId, Globals.NameApp, version);
            
            if (LamToolClient.IsNewerVersion(version, vs))
            {
                string title = "Thông báo";
                string message = $"Đã có version [{vs}] mới nhất.";
                fShowThongBao f = new fShowThongBao(title, message);
                if (f.ShowDialog() == DialogResult.OK)
                {
                    this.Hide();
                    using (var updateForm = new fUpdateAuto(url, version))
                    {
                        updateForm.ShowDialog(this);
                    }
                    Environment.Exit(0);
                }
            }
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
