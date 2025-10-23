using AntdUI;
using AutoAndroid;
using ScrcpyNet;
using System.Collections.Concurrent;
using System.Reflection;

namespace LamToolAutoPhonePrime
{
    public partial class Form1 : AntdUI.Window
    {
        private readonly ConcurrentDictionary<string, Scrcpy> instances = new();




        private List<DeviceModel> devices = new List<DeviceModel>();
        private const int DefaultBatchSize = 50;
        private fShowDevice childFormDevice;
        private readonly ucThongBaoDeviceView ucThongBao;
        private readonly List<Control> selectedControls = new List<Control>();
        public Form1(List<DeviceModel> devices)
        {
            InitializeComponent();
            childFormDevice = new fShowDevice(this);
            childFormDevice.SettingsButtonClicked += ucMenuscripDevice_HideForm;
            ucThongBao = new ucThongBaoDeviceView();
            this.devices = devices;
            this.Load += Form1_Load;
            // enable double buffering to reduce flicker when many controls are present
            TryEnableDoubleBuffering(flowLayoutPanel1);
            TryEnableDoubleBuffering(this);
        }

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
                // ignore if reflection fails
            }
        }

        private async Task LoadData()
        {
            if (devices == null || devices.Count == 0) return;

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
                            var ucDevice = new ucDeviceView(device, true, "");
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
                    var ucDevice = new ucDeviceView(device, true, "");
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
                ucDeviceView ucNew = new ucDeviceView(deviceView.device, false, "");
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
        private void Form1_Load(object? sender, EventArgs e)
        {
            InitSelectionFeature();
            // Fire-and-forget load; keep UI responsive
            _ = LoadData();
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
                ucDeviceView ucNew = new ucDeviceView(uc.device, true, "");
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
    }
}
