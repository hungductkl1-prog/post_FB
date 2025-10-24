using AntdUI;
using StreamAndroid.Helper;
using StreamAndroid.Services;
using StreamAndroid.View;
using System.Reflection;
using System.Threading.Tasks;

namespace StreamAndroid
{
    public partial class ucManagerDevices : UserControl
    {
        private DeviceManagerService deviceManagerService;
        public SelectableFlowLayoutPanel flControlAndroid;
        public ucDataGridViewDevice dataGridViewDevice;
        public ucManagerDevices()
        {
            InitializeComponent();
            CreateLoadingOverlay();


            flControlAndroid = new SelectableFlowLayoutPanel();
            flControlAndroid.AutoScroll = true;
            flControlAndroid.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            flControlAndroid.Dock = DockStyle.Fill;
            flControlAndroid.Location = new System.Drawing.Point(334, 15);
            flControlAndroid.Padding = new Padding(10, 0, 0, 0);
            flControlAndroid.Size = new Size(648, 500);
            flControlAndroid.TabIndex = 1;
            pMain.Controls.Add(flControlAndroid);
            flControlAndroid.BringToFront();

            dataGridViewDevice = new ucDataGridViewDevice();
            dataGridViewDevice.AutoScroll = true;
            dataGridViewDevice.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            dataGridViewDevice.Dock = DockStyle.Fill;
            dataGridViewDevice.Location = new System.Drawing.Point(334, 15);
            dataGridViewDevice.Padding = new Padding(10, 0, 0, 0);
            dataGridViewDevice.Size = new Size(648, 500);
            dataGridViewDevice.TabIndex = 1;
            pMain.Controls.Add(dataGridViewDevice);

            EnableDoubleBufferingRecursive(this);


            deviceManagerService = new DeviceManagerService(this);
            this.Load += ucManagerDevices_Load;
            button6.Click += button6_Click;

        }

        private async void ucManagerDevices_Load(object? sender, EventArgs e)
        {
            try
            {
                await SetupHelper.Setup();
                await deviceManagerService.HookDeviceEvents();
            }
            catch (Exception ex)
            {
                CommonMethod.ShowMessageError("Lỗi: " + ex.Message);
                Environment.Exit(0);
                return;
            }
            finally
            {
                HideLoading();
            }

        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                const float HiddenWidth = 0F;
                const float ShownWidth = 270F;
                bool wantHide = button1.IconSvg == "MenuUnfoldOutlined";
                var rightCol = tableLayoutPanel3.ColumnStyles[1];
                if (wantHide)
                {
                    if (!panel8.Visible && Math.Abs(rightCol.Width - HiddenWidth) < 0.5f)
                    {
                        button1.IconSvg = "MenuFoldOutlined";
                        return;
                    }
                }
                else
                {
                    if (panel8.Visible && Math.Abs(rightCol.Width - ShownWidth) < 0.5f)
                    {
                        button1.IconSvg = "MenuUnfoldOutlined";
                        return;
                    }
                }
                if (wantHide)
                {
                    panel8.Visible = false;
                    rightCol.SizeType = SizeType.Absolute;
                    rightCol.Width = HiddenWidth;
                    button1.IconSvg = "MenuFoldOutlined";
                }
                else
                {
                    rightCol.SizeType = SizeType.Absolute;
                    rightCol.Width = ShownWidth;
                    panel8.Visible = true;
                    button1.IconSvg = "MenuUnfoldOutlined";
                }
            }
            finally
            {

            }
        }
        private void SetDoubleBuffered(Control c)
        {
            typeof(Control)
                .GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(c, true, null);
        }
        private void EnableDoubleBufferingRecursive(Control root)
        {
            try { SetDoubleBuffered(root); } catch { }

            foreach (Control child in root.Controls)
            {
                try { SetDoubleBuffered(child); } catch { }
                if (child.HasChildren)
                    EnableDoubleBufferingRecursive(child);
            }
        }
        private async Task RunBackgroundWork(Func<Task> backgroundWork, Action onUiComplete = null, Action<Exception> onError = null)
        {
            try
            {
                await Task.Run(backgroundWork).ConfigureAwait(false);
                if (onUiComplete != null)
                {
                    if (IsHandleCreated)
                        BeginInvoke(onUiComplete);
                    else
                        onUiComplete();
                }
            }
            catch (Exception ex)
            {
                if (onError != null)
                {
                    if (IsHandleCreated)
                        BeginInvoke(new Action(() => onError(ex)));
                    else
                        onError(ex);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }
        }
        private async void button5_Click(object sender, EventArgs e)
        {
            button5.Enabled = false;
            var oldText = button5.Text;
            button5.Text = "Đang tải...";

            await RunBackgroundWork(async () =>
            {
                await Task.Delay(800).ConfigureAwait(false);
            },
            onUiComplete: () =>
            {
                button5.Enabled = true;
                button5.Text = oldText;
            },
            onError: (ex) =>
            {
                button5.Enabled = true;
                button5.Text = oldText;
                MessageBox.Show($"Lỗi khi tải: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            });
        }
        private async void button6_Click(object sender, EventArgs e)
        {
            button6.Enabled = false;
            var old = button6.Text;
            button6.Text = "Đang dừng ADB...";

            await RunBackgroundWork(async () =>
            {
                await Task.Delay(600).ConfigureAwait(false);
            },
            onUiComplete: () =>
            {
                button6.Enabled = true;
                button6.Text = old;
            },
            onError: (ex) =>
            {
                button6.Enabled = true;
                button6.Text = old;
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            });
        }

        private void segmented5_SelectIndexChanged(object sender, AntdUI.IntEventArgs e)
        {
            if (e.Value == 0)
            {
                flControlAndroid.BringToFront();
            }
            else
            {
                dataGridViewDevice.BringToFront();
            }
        }
        private CancellationTokenSource _loadingCts;
        private System.Windows.Forms.Panel _loadingOverlay;
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
                Font = new Font("Segoe UI", 16f),
                Text = "Đang khởi động...",
                ForeColor = Color.FromArgb(70, 70, 70)
            };

            _loadingOverlay.Controls.Add(spin);
            Controls.Add(_loadingOverlay);
            _loadingOverlay.BringToFront();

            var messages = new[] { "Đang tải UI...", "Đang tải dữ liệu...", "Đang đồng bộ...", "Đang khởi động hệ thống...", "LamTool xin chào!" };

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
        }

        private async void slider3_ValueChanged(object sender, IntEventArgs e)
        {
            var value = e.Value;
            await RunBackgroundWork(async () =>
            {
                flControlAndroid?.Invoke(new Action(() =>
                {
                    SetRenderSize(value);
                }));
               
                await Task.Delay(100).ConfigureAwait(false);
            },
            onUiComplete: () =>
            {
            },
            onError: (ex) =>
            {
            });
        }
        public void SetRenderSize(int size)
        {
            foreach (Control ctrl in flControlAndroid.Controls)
            {
                if (ctrl is ucControlAndroid ucDevice)
                {
                    ucDevice.SetRenderSize(size);
                }
                else
                if (ctrl is ucThongBaoDeviceView uc)
                {
                    uc.SetRenderSize(size);
                }

            }
            if (flControlAndroid.IsHandleCreated)
            {
                flControlAndroid.BeginInvoke((System.Windows.Forms.MethodInvoker)delegate
                {
                    flControlAndroid.Refresh();
                });
            }
            else
            {
                flControlAndroid.Refresh();
            }
        }


    }
}
