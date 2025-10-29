using AntdUI;
using Newtonsoft.Json.Linq;
using StreamAndroid.Helper;
using StreamAndroid.Services;
using StreamAndroid.View;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Data.Models;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace StreamAndroid
{
    public partial class ucManagerDevices : UserControl
    {
        private readonly DeviceManagerService deviceManagerService;
        public SelectableFlowLayoutPanel flControlAndroid;
        public ucDataGridViewDevice dataGridViewDevice;
        public SortableBindingList<Account> bindingList;
        public ucManagerDevices()
        {
            InitializeComponent();



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
            flControlAndroid.SelectionChanged += SelectionChanged;


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

            select1.Items.Add("Tất cả");
            select1.SelectedIndex = 0;
            deviceManagerService = new DeviceManagerService(this);
            this.Load += ucManagerDevices_Load;
            button6.Click += button6_Click;

        }

        private async void ucManagerDevices_Load(object? sender, EventArgs e)
        {
            try
            {
                CreateLoadingOverlay();
                await Task.Delay(500);
                await SetupHelper.Setup();
                await deviceManagerService.HookDeviceEvents();
                SetRenderSize(slider3.Value, rotationAngle);
                SetOverlayTextOpacity(slider1.Value);
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
                SetRenderSize(slider3.Value, rotationAngle);
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
            // Chỉ resize, KHÔNG thay đổi rotation
            SetRenderSize(value, rotationAngle); // Truyền rotation hiện tại
            await Task.Delay(200).ConfigureAwait(false);
        }
        public void SetRenderSize(int size, int? rotation = null)
        {
            try
            {
                // Nếu có rotation mới thì update
                if (rotation.HasValue)
                {
                    rotationAngle = rotation.Value;
                }

                foreach (Control ctrl in flControlAndroid.Controls)
                {
                    if (ctrl is ucControlAndroid uc)
                    {
                        if (uc.InvokeRequired)
                        {
                            uc.Invoke(new Action(() =>
                            {
                                uc.SetRenderSize(size, rotationAngle);
                            }));
                        }
                        else
                        {
                            uc.SetRenderSize(size, rotationAngle);
                        }
                    }
                    else if (ctrl is ucThongBaoDeviceView uct)
                    {
                        if (uct.InvokeRequired)
                        {
                            uct.Invoke(new Action(() =>
                            {
                                uct.SetRenderSize(size, rotationAngle);
                            }));
                        }
                        else
                        {
                            uct.SetRenderSize(size, rotationAngle);
                        }
                    }
                }

                if (flControlAndroid.InvokeRequired)
                {
                    flControlAndroid.Invoke(new Action(() => flControlAndroid.Refresh()));
                }
                else
                {
                    flControlAndroid.Refresh();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ Error in SetRenderSize: {ex.Message}");
            }
        }

        private void SelectionChanged(Rectangle rect)
        {
            List<DeviceModel> selectedDevices = new List<DeviceModel>();
            foreach (Control ctrl in flControlAndroid.Controls)
            {
                if (ctrl is ucControlAndroid uc)
                {
                    uc.device.IsSelectControl = false;
                    bool intersect = rect.IntersectsWith(ctrl.Bounds);
                    if (intersect)
                    {
                        uc.device.IsSelectControl = true;

                    }
                    uc.panel1.BorderColor = uc.device.IsSelectControl
                              ? Color.Green
                              : Color.RoyalBlue;
                }
            }
            foreach (DataGridViewRow row in dataGridViewDevice.dataGridView1.Rows)
            {
                row.Selected = false;
                if (row.DataBoundItem is DeviceModel device)
                {
                    if (device.IsSelectControl)
                    {
                        row.Selected = true;
                    }
                }
            }
            label4.Text = $"Bôi đen\r\n{dataGridViewDevice.dataGridView1.SelectedRows.Count}";
        }

        private async void select4_SelectedIndexChanged(object sender, IntEventArgs e)
        {
            if (e == null || deviceManagerService == null) return;
            // input6.Text = "";
            await deviceManagerService.FilterDevices(select1.Text.Trim(), input6.Text);
            //  input6_TextChanged(null, null);
        }
        private const int WM_SETREDRAW = 0x000B;
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        private static void SetRedraw(Control c, bool enable)
        {
            if (!c.IsHandleCreated) return;
            SendMessage(c.Handle, WM_SETREDRAW, enable ? (IntPtr)1 : IntPtr.Zero, IntPtr.Zero);
        }
        private void ApplySelection(IReadOnlyList<int> indexes, bool hasSearch)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => ApplySelection(indexes, hasSearch)));
                return;
            }

            dataGridViewDevice._suppressSelectionChanged = true;

            try
            {
                SetRedraw(dataGridViewDevice, false);
                dataGridViewDevice.SuspendLayout();

                var dgv = dataGridViewDevice.dataGridView1;
                dgv.ClearSelection();
                dgv.CurrentCell = null; // tránh lỗi khi ẩn dòng

                int total = DeviceManagerService.DeviceModels.Count;
                bool showAll = !hasSearch; // nếu không search => hiện tất cả
                var visibleSet = new HashSet<int>(indexes);

                for (int i = 0; i < total; i++)
                {
                    bool visible = showAll || visibleSet.Contains(i);

                    if (i < flControlAndroid.Controls.Count)
                        flControlAndroid.Controls[i].Visible = visible;

                    if (i < dgv.Rows.Count)
                    {
                        var row = dgv.Rows[i];
                        if (row.Visible != visible)
                            row.Visible = visible;
                    }
                }
            }
            finally
            {
                dataGridViewDevice.ResumeLayout();
                SetRedraw(dataGridViewDevice, true);
                dataGridViewDevice.Invalidate();
                dataGridViewDevice._suppressSelectionChanged = false;
            }
        }

        private async void input6_TextChanged(object sender, EventArgs e)
        {

            try
            {
                await deviceManagerService.FilterDevices(select1.Text.Trim(), input6.Text);
                await Task.Delay(250);
            }
            catch (TaskCanceledException)
            {
                return;
            }


        }
        private int rotationAngle = 0;
        private async void button2_Click(object sender, EventArgs e)
        {
            button2.Enabled = false;

            try
            {
                // Tăng rotation
                rotationAngle = (rotationAngle + 90) % 360;

                // Gọi SetRenderSize với rotation mới
                SetRenderSize(slider3.Value, rotationAngle);

                await Task.Delay(100); // Đợi render ổn định
            }
            finally
            {
                button2.Enabled = true;
            }
        }

        private void switch1_CheckedChanged(object sender, BoolEventArgs e)
        {
            if (e.Value)
            {
                foreach (Control ctrl in flControlAndroid.Controls)
                {
                    ctrl.Enabled = true;
                }
            }
            else
            {
                foreach (Control ctrl in flControlAndroid.Controls)
                {
                    ctrl.Enabled = false;
                }
            }
        }
        private void SetOverlayTextOpacity(int opacity)
        {
            foreach (Control ctrl in flControlAndroid.Controls)
            {
                if (ctrl is ucControlAndroid uc)
                {
                    uc.SetOverlayTextOpacity(opacity);
                }
                if (ctrl is ucThongBaoDeviceView uct)
                {
                    uct.SetTextOpacity(opacity);
                }
            }
        }
        private void slider1_ValueChanged(object sender, IntEventArgs e)
        {
            SetOverlayTextOpacity(slider1.Value);
            SetRenderSize(slider3.Value, rotationAngle);
        }

        private void button9_Click(object sender, EventArgs e)
        {

        }

        private void slider4_ValueChanged(object sender, IntEventArgs e)
        {
            deviceManagerService.SetValueToSlider(slider4.Value);
        }
    }
}
