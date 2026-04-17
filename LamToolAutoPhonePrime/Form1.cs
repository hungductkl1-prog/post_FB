using AntdUI;
using AutoAndroid;
using LamToolAutoPhonePrime.Utils;
using ScrcpyNet;
using SharpAdbClient;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Data.Models;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace LamToolAutoPhonePrime
{
    public partial class Form1 : AntdUI.Window
    {
        private readonly ScrcpyManager manager = new ScrcpyManager();


        private List<DeviceModel> devices = new List<DeviceModel>();
        private const int DefaultBatchSize = 50;
        private fShowDevice childFormDevice;
        private readonly ucThongBaoDeviceView ucThongBao;
        private readonly List<Control> selectedControls = new List<Control>();
        public Form1(List<DeviceModel> devices)
        {
            AdbServer.Instance.StartServer(Path.Combine(ProcessHelper.ADBPath, "adb.exe"), false);
            FFmpeg.AutoGen.ffmpeg.RootPath = Path.Combine(AppContext.BaseDirectory, "ScrcpyNet");
            InitializeComponent();
            ucThongBao = new ucThongBaoDeviceView();
            childFormDevice = new fShowDevice(this);
            childFormDevice.SettingsButtonClicked += ucMenuscripDevice_HideForm;
            this.devices = devices;

            this.Load += Form1_Load;
            // enable double buffering to reduce flicker when many controls are present
            TryEnableDoubleBuffering(flowLayoutPanel1);
            TryEnableDoubleBuffering(this);
        }

        [DynamicDependency("DoubleBuffered", typeof(Control))]
        private static void TryEnableDoubleBuffering(Control ctrl)
        {
            if (ctrl == null) return;
            try
            {
                var prop = typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                prop?.SetValue(ctrl, true);
            }
            catch
            {
                // ignore if reflection fails in AOT
            }
        }

        private async Task LoadData()
        {
            if (devices == null || devices.Count == 0) return;
            manager.StartAll(devices);


            int batchSize = DefaultBatchSize;
            var batch = new List<DeviceModel>(batchSize);

            // Add in batches to keep UI responsive
            foreach (var device in devices)
            {

                batch.Add(device);
                if (batch.Count >= batchSize)
                {
                    AddBatchToPanel(batch);
                    batch.Clear();
                    // yield briefly to let the UI thread process messages (keeps UI smooth)
                    await Task.Delay(1).ConfigureAwait(false);
                }
            }

            if (batch.Count > 0)
            {
                AddBatchToPanel(batch);
            }
            SetRenderSize(slider2.Value);
        }

        private void AddBatchToPanel(List<DeviceModel> models)
        {
            if (models == null || models.Count == 0) return;

            // Ensure creation & addition happen on UI thread
            if (flowLayoutPanel1.IsHandleCreated)
            {
                flowLayoutPanel1.Invoke((System.Windows.Forms.MethodInvoker)delegate
                {
                    flowLayoutPanel1.SuspendLayout();
                    try
                    {
                        foreach (var device in models)
                        {

                            var ucDevice = new ucDeviceView(device, true, "", manager.StartForDevice(device.Serial));
                            ucDevice.SettingsButtonClicked += UcDevice_SettingsButtonClicked;
                            flowLayoutPanel1.Controls.Add(ucDevice);
                        }
                    }
                    finally
                    {
                        flowLayoutPanel1.ResumeLayout();
                        // Refresh once after batch added
                        flowLayoutPanel1.Invalidate();
                    }
                });
            }
            else
            {
                // fallback (designer/runtime edge cases)
                foreach (var device in models)
                {
                    var ucDevice = new ucDeviceView(device, true, "", manager.StartForDevice(device.Serial));
                    ucDevice.SettingsButtonClicked += UcDevice_SettingsButtonClicked;
                    flowLayoutPanel1.Controls.Add(ucDevice);
                }
            }

        }
        private void UcDevice_SettingsButtonClicked(object? sender, EventArgs e)
        {
            if (sender is not ucDeviceView deviceView) return;

            if (childFormDevice.isDragging) return;
            try
            {
                // Lưu lại chỉ số vị trí cũ của ucDeviceView trong FlowLayoutPanel
                int oldIndex = flowLayoutPanel1.Controls.GetChildIndex(deviceView);

                // Xóa control cũ (thiết bị)
                flowLayoutPanel1.Controls.Remove(deviceView);
                deviceView.Dispose();
                ucDeviceView ucNew = new ucDeviceView(deviceView.device, false, "", manager.StartForDevice(deviceView.device.Serial));
                // Tạo ucThongBao (hoặc lấy sẵn từ instance)
                ucThongBao.Width = deviceView.Width;
                ucThongBao.Height = deviceView.Height;
                ucThongBao.Margin = deviceView.Margin;

                // Thêm ucThongBao vào đúng vị trí cũ
                flowLayoutPanel1.Controls.Add(ucThongBao);
                flowLayoutPanel1.Controls.SetChildIndex(ucThongBao, oldIndex);
                // Cấu hình form con (chi tiết thiết bị)
                childFormDevice.Owner = this; // Giữ form con luôn nằm trên form cha
                childFormDevice.ShowInTaskbar = false;
                childFormDevice.TopMost = false;

                // Đặt vị trí hiển thị — ví dụ canh giữa theo form chính
                childFormDevice.StartPosition = FormStartPosition.Manual;
                childFormDevice.Location = new System.Drawing.Point(
                    this.Location.X + (this.Width - childFormDevice.Width) / 2,
                    this.Location.Y + (this.Height - childFormDevice.Height) / 2
                );
                childFormDevice.Load(ucNew);
                // Hiển thị form chi tiết
                childFormDevice.Show();
                SetRenderSize(slider2.Value);
                SetRenderSizeBig(slider1.Value);
            }
            finally
            {
                childFormDevice.isDragging = true;
            }

        }
        private async void Form1_Load(object? sender, EventArgs e)
        {
            CreateLoadingOverlay();
            InitSelectionFeature();
            // Fire-and-forget load; keep UI responsive
            await LoadData();
            HideLoading();
        }
        private void SetRenderSize(int size)
        {
            foreach (Control ctrl in flowLayoutPanel1.Controls)
            {
                if (ctrl is ucDeviceView ucDevice)
                {
                    ucDevice.SetRenderSize(size);
                }
                else if (ctrl is ucThongBaoDeviceView ucThongBao)
                {
                    ucThongBao.SetRenderSize(size);
                }
            }

            // Use BeginInvoke to coalesce multiple rapid slider events into fewer repaints
            if (flowLayoutPanel1.IsHandleCreated)
            {
                flowLayoutPanel1.BeginInvoke((System.Windows.Forms.MethodInvoker)delegate
                {
                    flowLayoutPanel1.Refresh();
                });
            }
            else
            {
                flowLayoutPanel1.Refresh();
            }
        }
        private void SetRenderSizeBig(int size)
        {
            childFormDevice.SetRenderSize(size);
        }
        private void slider2_ValueChanged(object sender, AntdUI.IntEventArgs e)
        {
            SetRenderSize(e.Value);
        }

        private void slider1_ValueChanged(object sender, IntEventArgs e)
        {
            if (!childFormDevice.isDragging) return;
            SetRenderSizeBig(e.Value);
        }

        private void ucMenuscripDevice_HideForm(object? sender, EventArgs e)
        {
            if (sender is not fShowDevice fshow) return;
            try
            {

                int oldIndex = flowLayoutPanel1.Controls.GetChildIndex(ucThongBao);
                flowLayoutPanel1.Controls.Remove(ucThongBao);
                ucDeviceView uc = fshow.ucDevice;
                fshow.ucDevice.Dispose();
                ucDeviceView ucNew = new ucDeviceView(uc.device, true, "", manager.StartForDevice(uc.device.Serial));
                ucNew.SettingsButtonClicked += UcDevice_SettingsButtonClicked;
                ucNew.Dock = DockStyle.None;
                ucNew.Width = ucThongBao.Width;
                ucNew.Height = ucThongBao.Height;
                ucNew.Margin = ucThongBao.Margin;
                flowLayoutPanel1.Controls.Add(ucNew);
                flowLayoutPanel1.Controls.SetChildIndex(ucNew, oldIndex);
                fshow.Hide();

            }
            finally
            {
                childFormDevice.isDragging = false;
            }

        }


        private void InitSelectionFeature()
        {
            flowLayoutPanel1.SelectionChanged += SelectionChanged;
            flowLayoutPanel1.SelectionFinished += SelectionFinished;
        }
        private void SelectionChanged(Rectangle rect)
        {
            foreach (Control ctrl in flowLayoutPanel1.Controls)
            {
                if (ctrl is ucDeviceView uc)
                {
                    bool intersect = rect.IntersectsWith(ctrl.Bounds);
                    if (intersect)
                    {
                        if (!selectedControls.Contains(uc))
                        {
                            selectedControls.Add(uc);
                            uc.panel1.BorderColor = Color.Green;
                        }
                    }
                    else
                    {
                        if (selectedControls.Contains(uc))
                        {
                            selectedControls.Remove(uc);
                            uc.panel1.BorderColor = Color.RoyalBlue;
                        }
                    }
                }
            }
        }

        private void SelectionFinished()
        {
            // Khi thả chuột: có thể xử lý gì thêm
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            manager.StopAll();
        }



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
            this.panel1.Enabled = true;
        }

        private CancellationTokenSource _loadingCts;
        private System.Windows.Forms.Panel _loadingOverlay;
    }
}
