using AntdUI;
using AutoAndroid;
using Facebook_Farm_NewFeed_PostStory.Utils;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Data.Models;
using System.ComponentModel;
using System.Windows.Forms;
using CommonMethod = Sunny.Subdy.Common.ControlMethod.CommonMethod;
using Facebook_Farm_NewFeed_PostStory.Utils.Design;

namespace Facebook_Farm_NewFeed_PostStory.Views.Forms
{
    public partial class fDevicesWifi : Facebook_Farm_NewFeed_PostStory.Utils.BaseForm
    {
        private readonly List<DeviceModel> _devices;
        private readonly BindingList<WifiRow> _rows = new();
        private readonly BindingSource _bs = new();

        private static readonly Color _greenLive = Color.FromArgb(34, 197, 94);
        private static readonly Color _redDead = Color.FromArgb(220, 53, 69);

        private AntdUI.PageHeader windowBar = null!;
        private System.Windows.Forms.DataGridView grid = null!;
        private AntdUI.Button btnCheck = null!;
        private AntdUI.Button btnConnect = null!;
        private AntdUI.Button btnClose = null!;

        public class WifiRow : INotifyPropertyChanged
        {
            private string _status = "";
            private bool? _online; // null=unknown, true=green, false=red

            public string DeviceId { get; set; } = "";
            public string Name { get; set; } = "";
            public string UserName { get; set; } = "";
            public string Password { get; set; } = "";

            public string Status
            {
                get => _status;
                set { _status = value; OnPropertyChanged(nameof(Status)); }
            }

            [Browsable(false)]
            public bool? Online
            {
                get => _online;
                set { _online = value; OnPropertyChanged(nameof(Online)); }
            }

            public event PropertyChangedEventHandler? PropertyChanged;
            private void OnPropertyChanged(string n) =>
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
        }

        public fDevicesWifi(List<DeviceModel> devices)
        {
            _devices = devices ?? new List<DeviceModel>();
            BuildUi();
            FontUtil.ApplyFontToAllControls(this); Facebook_Farm_NewFeed_PostStory.Utils.Design.VietnameseFont.Enforce(this);
            LoadRows();
        }

        private void BuildUi()
        {
            Text = "Kết nối Wifi";
            ClientSize = new Size(820, 460);
            MinimumSize = new Size(700, 360);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(245, 247, 250);

            windowBar = new AntdUI.PageHeader
            {
                Dock = DockStyle.Top,
                Text = "Kết nối Wifi cho thiết bị",
                MDI = true,
                BackColor = Color.White,
                Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
                Size = new Size(820, 36),
            };
            Controls.Add(windowBar);

            var bottom = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                BackColor = Color.White,
                Padding = new Padding(12, 10, 12, 10),
            };
            Controls.Add(bottom);

            btnCheck = new AntdUI.Button
            {
                Dock = DockStyle.Left,
                Width = 130,
                Text = "Kiểm tra",
                Type = AntdUI.TTypeMini.Primary,
                Shape = AntdUI.TShape.Round,
                IconSvg = "ReloadOutlined",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
            };
            btnCheck.Click += BtnCheck_Click;
            bottom.Controls.Add(btnCheck);

            var spacer = new System.Windows.Forms.Panel { Dock = DockStyle.Left, Width = 8, BackColor = Color.Transparent };
            bottom.Controls.Add(spacer);

            btnConnect = new AntdUI.Button
            {
                Dock = DockStyle.Left,
                Width = 160,
                Text = "Kết nối wifi",
                Type = AntdUI.TTypeMini.Success,
                Shape = AntdUI.TShape.Round,
                IconSvg = "WifiOutlined",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
            };
            btnConnect.Click += BtnConnect_Click;
            bottom.Controls.Add(btnConnect);

            btnClose = new AntdUI.Button
            {
                Dock = DockStyle.Right,
                Width = 110,
                Text = "Đóng",
                Type = AntdUI.TTypeMini.Error,
                Shape = AntdUI.TShape.Round,
                IconSvg = "CloseOutlined",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
            };
            btnClose.Click += (_, __) => Close();
            bottom.Controls.Add(btnClose);

            // re-add in correct visual order: connect -> spacer -> check on left
            bottom.Controls.SetChildIndex(btnConnect, 0);
            bottom.Controls.SetChildIndex(spacer, 1);
            bottom.Controls.SetChildIndex(btnCheck, 2);

            grid = new System.Windows.Forms.DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                EnableHeadersVisualStyles = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true,
                ColumnHeadersHeight = 32,
            };
            grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(248, 249, 252),
                ForeColor = Color.Black,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter,
            };
            grid.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Color.FromArgb(26, 26, 26),
                SelectionBackColor = Color.FromArgb(0, 120, 215),
                SelectionForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
            };

            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "#", Name = "colIndex", Width = 50, ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "DeviceId", DataPropertyName = nameof(WifiRow.DeviceId), Name = "colDeviceId", Width = 180, ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Name", DataPropertyName = nameof(WifiRow.Name), Name = "colName", Width = 160, ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "UserName", DataPropertyName = nameof(WifiRow.UserName), Name = "colUserName", Width = 160 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Password", DataPropertyName = nameof(WifiRow.Password), Name = "colPassword", Width = 140 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Trạng thái", DataPropertyName = nameof(WifiRow.Status), Name = "colStatus", Width = 200, ReadOnly = true });

            _bs.DataSource = _rows;
            grid.DataSource = _bs;

            grid.RowPrePaint += Grid_RowPrePaint;
            grid.CellFormatting += Grid_CellFormatting;
            grid.CellEndEdit += Grid_CellEndEdit;

            Controls.Add(grid);
            Controls.SetChildIndex(grid, 0); // fill above bottom + below windowBar
        }

        private void LoadRows()
        {
            var saved = WifiCredentialsStore.LoadAll();
            _rows.Clear();
            foreach (var d in _devices)
            {
                var row = new WifiRow
                {
                    DeviceId = d.Serial ?? "",
                    Name = string.IsNullOrEmpty(d.NameDevice) ? d.Model ?? "" : d.NameDevice,
                };
                if (!string.IsNullOrEmpty(row.DeviceId) && saved.TryGetValue(row.DeviceId, out var c))
                {
                    row.UserName = c.UserName;
                    row.Password = c.Password;
                }
                _rows.Add(row);
            }
        }

        private void Grid_RowPrePaint(object? sender, DataGridViewRowPrePaintEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _rows.Count) return;
            var row = _rows[e.RowIndex];
            var color = row.Online switch
            {
                true => _greenLive,
                false => _redDead,
                _ => Color.FromArgb(26, 26, 26),
            };
            grid.Rows[e.RowIndex].DefaultCellStyle.ForeColor = color;
        }

        private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (grid.Columns[e.ColumnIndex].Name == "colIndex")
            {
                e.Value = (e.RowIndex + 1).ToString();
                e.FormattingApplied = true;
            }
        }

        private void Grid_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _rows.Count) return;
            var r = _rows[e.RowIndex];
            WifiCredentialsStore.Upsert(r.DeviceId, r.UserName, r.Password);
        }

        private async void BtnCheck_Click(object? sender, EventArgs e)
        {
            if (_rows.Count == 0)
            {
                AntdHelper.NotifyWarn(this, "Cảnh báo", "Không có thiết bị nào.");
                return;
            }

            Enabled = false;
            try
            {
                await AntdUI.Spin.open(this, "Đang kiểm tra internet...", async _ =>
                {
                    var tasks = new List<Task>();
                    foreach (var row in _rows.ToList())
                    {
                        var captured = row;
                        captured.Status = "Đang kiểm tra...";
                        captured.Online = null;
                        tasks.Add(Task.Run(() =>
                        {
                            try
                            {
                                var device = _devices.FirstOrDefault(d => d.Serial == captured.DeviceId);
                                if (device == null)
                                {
                                    SetRowResult(captured, false, "Không tìm thấy thiết bị");
                                    return;
                                }
                                bool ok = QuickInternetCheck(device.Serial);
                                SetRowResult(captured, ok, ok ? "Có internet" : "Không có internet");
                            }
                            catch (Exception ex)
                            {
                                SetRowResult(captured, false, $"Lỗi: {ex.Message}");
                            }
                        }));
                    }
                    await Task.WhenAll(tasks);
                });
            }
            finally { Enabled = true; }
        }

        private void SetRowResult(WifiRow row, bool online, string status)
        {
            if (InvokeRequired) { BeginInvoke(new Action(() => SetRowResult(row, online, status))); return; }
            row.Online = online;
            row.Status = status;
            grid.Invalidate();
        }

        private async void BtnConnect_Click(object? sender, EventArgs e)
        {
            var selectedRows = new List<WifiRow>();
            foreach (DataGridViewRow r in grid.SelectedRows)
                if (r.DataBoundItem is WifiRow w) selectedRows.Add(w);

            if (selectedRows.Count == 0)
            {
                AntdHelper.NotifyWarn(this, "Cảnh báo", "Vui lòng bôi đen ít nhất 1 thiết bị.");
                return;
            }

            using var input = new fInputWifiCredentials();
            if (input.ShowDialog(this) != DialogResult.OK) return;

            var lines = input.Lines;
            if (lines.Count == 0)
            {
                AntdHelper.NotifyWarn(this, "Cảnh báo", "Danh sách username|password trống.");
                return;
            }

            // Pair each device with one credential line (round-robin)
            var pairs = new List<(WifiRow row, string user, string pass)>();
            for (int i = 0; i < selectedRows.Count; i++)
            {
                var line = lines[i % lines.Count];
                var parts = line.Split('|');
                if (parts.Length < 2) continue;
                var u = parts[0].Trim();
                var p = string.Join('|', parts.Skip(1)).Trim();
                pairs.Add((selectedRows[i], u, p));
            }

            if (pairs.Count == 0)
            {
                AntdHelper.NotifyWarn(this, "Cảnh báo", "Không có dòng hợp lệ (định dạng: username|password).");
                return;
            }

            Enabled = false;
            try
            {
                await AntdUI.Spin.open(this, "Đang chuẩn bị APK...", async _ =>
                {
                    // Pre-fetch apk once for all devices to avoid N parallel downloads.
                    bool apkOk = await AdbJoinWifiService.EnsureApkAvailableAsync();
                    if (!apkOk)
                    {
                        foreach (var (row, _, _) in pairs)
                            SetRowResult(row, false, "Thiếu APK adb-join-wifi (tải thất bại)");
                        return;
                    }
                });

                await AntdUI.Spin.open(this, "Đang kết nối Wifi...", async _ =>
                {
                    var tasks = new List<Task>();
                    foreach (var (row, user, pass) in pairs)
                    {
                        var rCap = row;
                        var u = user;
                        var p = pass;
                        tasks.Add(Task.Run(async () =>
                        {
                            try
                            {
                                var device = _devices.FirstOrDefault(d => d.Serial == rCap.DeviceId);
                                if (device == null) { SetRowResult(rCap, false, "Không tìm thấy thiết bị"); return; }

                                SetRowStatus(rCap, "Đang cài apk...");
                                var client = new ADBClient(device);
                                var wifi = new AdbJoinWifiService(client);

                                SetRowStatus(rCap, $"Đang kết nối: {u}");
                                bool sent = await wifi.ConnectToWifiNetwork(u, p);
                                if (!sent)
                                {
                                    SetRowResult(rCap, false, "Cài APK thất bại");
                                    return;
                                }

                                rCap.UserName = u;
                                rCap.Password = p;
                                WifiCredentialsStore.Upsert(rCap.DeviceId, u, p);

                                // Verify: poll up to ~12s for internet to come up.
                                SetRowStatus(rCap, "Đang xác minh kết nối...");
                                bool online = false;
                                for (int attempt = 0; attempt < 6; attempt++)
                                {
                                    await Task.Delay(2000);
                                    if (QuickInternetCheck(device.Serial)) { online = true; break; }
                                }
                                SetRowResult(rCap, online,
                                    online ? "Kết nối thành công" : "Không có internet (kiểm tra SSID/mật khẩu)");
                            }
                            catch (Exception ex)
                            {
                                SetRowResult(rCap, false, $"Lỗi: {ex.Message}");
                            }
                        }));
                    }
                    await Task.WhenAll(tasks);
                });
            }
            finally { Enabled = true; }
        }

        private void SetRowStatus(WifiRow row, string status)
        {
            if (InvokeRequired) { BeginInvoke(new Action(() => SetRowStatus(row, status))); return; }
            row.Status = status;
            grid.Invalidate();
        }

        // Quick internet check via `ping -c 1 -W 2 8.8.8.8` over adb shell. ~2s per device.
        // Why: GetIp() is a heavyweight HTTP/MaxChange call (10-retry, ~10-30s). Ping is enough
        // to answer "có internet hay không" for the green/red indicator.
        private static bool QuickInternetCheck(string serial)
        {
            if (string.IsNullOrEmpty(serial)) return false;
            try
            {
                string r = ProcessHelper.RunAdbWithTimeout(
                    $"-s {serial} shell ping -c 1 -W 2 8.8.8.8", timeoutSeconds: 5);
                if (string.IsNullOrEmpty(r)) return false;
                // success line example: "1 packets transmitted, 1 received, 0% packet loss"
                return r.Contains("1 received", StringComparison.OrdinalIgnoreCase)
                    || r.Contains("bytes from", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }
    }
}
