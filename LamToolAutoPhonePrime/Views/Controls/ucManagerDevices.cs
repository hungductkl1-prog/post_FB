using AntdUI;
using AutoAndroid;
using Emgu.CV.Structure;
using LamToolAutoPhonePrime;
using LamToolAutoPhonePrime.Utils;
using LamToolAutoPhonePrime.Utils.Design;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Logs;
using Sunny.Subdy.Common.Services;
using Sunny.Subdy.Data.Models;
using System;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.ComponentModel;

namespace Sunny.Subdy.UI.View.Pages
{
    public partial class ucManagerDevices : UserControl
    {
        private CancellationTokenSource? cancellationTokenSource;
        AntdUI.IContextMenuStripItem[]? menulist = { };
        private AdbTrackDevicesService? _trackService;
        private string _currentFilter = "Tất cả";

        // Cached fonts to avoid creating new Font objects in CellFormatting (perf)
        private static readonly Font _liveDotFont = new Font("Segoe UI", 14F);
        private static readonly Color _greenLive = Color.FromArgb(34, 197, 94);
        private static readonly Color _redDead = Color.FromArgb(220, 53, 69);
        private static readonly Color _grayOffline = Color.FromArgb(160, 160, 160);
        private System.Windows.Forms.Timer? _saveCheckedTimer;
        private bool _rightPanelCollapsed;
        private int _savedSplitterDistance;

        // Persistent binding objects — never replaced, only refilled
        private SortableBindingList<DeviceModel> _deviceBindingList = new();
        private BindingSource _deviceBindingSource = new();

        // Phone setup checkboxes
        private AntdUI.Checkbox? _chkCaiDatBanDau;
        private AntdUI.Checkbox? _chkTatAmThanh;
        private AntdUI.Checkbox? _chkNgonNguEng;
        private AntdUI.Checkbox? _chkTatGPS;
        private AntdUI.Checkbox? _chkCaiFacebook;
        private AntdUI.Checkbox? _chkCaiTLC;
        private AntdUI.Checkbox? _chkKhoiDong;
        private AntdUI.Checkbox? _chkCapQuyenTLC;
        private NumericUpDown? _nudBright;
        private NumericUpDown? _nudPin;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool TogglePanelButtonVisible
        {
            get => _btnTogglePanel?.Visible ?? false;
            set { if (_btnTogglePanel != null) _btnTogglePanel.Visible = value; }
        }

        public ucManagerDevices(Form form)
        {
            InitializeComponent();

            button2.Click += button2_Click;
            GridStyleHelper.Apply(dataGridView1);
            dataGridView1.AutoGenerateColumns = false;
            // Font cho dòng dữ liệu: semi-bold cho dễ đọc trên list dài
            dataGridView1.DefaultCellStyle.Font = FontScale.Body9Bold;
            dataGridViewCheckBoxColumn1.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCheckBoxColumn1.DataPropertyName = nameof(DeviceModel.Checked);
            // Events
            dataGridView1.SelectionChanged += DataGridView_SelectionChanged;
            dataGridView1.CellFormatting += uiDataGridView1_CellFormatting;
            dataGridView1.RowTemplate.Height = Math.Max(22, dataGridView1.RowTemplate.Height);


            dataGridViewCheckBoxColumn1.Width = 40;
            dataGridViewCheckBoxColumn1.MinimumWidth = 40;
            dataGridViewCheckBoxColumn1.Resizable = DataGridViewTriState.True;
            dataGridViewTextBoxColumn1.Width = 40;
            dataGridViewTextBoxColumn1.MinimumWidth = 40;
            dataGridViewTextBoxColumn1.Resizable = DataGridViewTriState.False;
            dataGridView1.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, dataGridView1.ClientRectangle,
                    Color.White, 1, ButtonBorderStyle.Solid, // Left
                    SystemColors.AppWorkspace, 1, ButtonBorderStyle.Solid, // Top
                    Color.White, 1, ButtonBorderStyle.Solid, // Right
                    Color.White, 1, ButtonBorderStyle.Solid  // Bottom
                );

                // Empty state: hiển thị hướng dẫn khi chưa có thiết bị
                if (dataGridView1.Rows.Count == 0)
                {
                    var rect = dataGridView1.ClientRectangle;
                    using var iconFont = new Font("Segoe UI", 32F);
                    using var titleFont = new Font("Segoe UI", 13F, FontStyle.Bold);
                    using var hintFont = new Font("Segoe UI", 10F);
                    var gray = Color.FromArgb(160, 160, 160);

                    string icon = "📱";
                    string title = "Chưa có thiết bị nào";
                    string hint = "Nhấn \"Tải thiết bị\" để tải danh sách thiết bị";

                    var iconSize = e.Graphics.MeasureString(icon, iconFont);
                    var titleSize = e.Graphics.MeasureString(title, titleFont);
                    var hintSize = e.Graphics.MeasureString(hint, hintFont);

                    float totalH = iconSize.Height + titleSize.Height + hintSize.Height + 16;
                    float startY = (rect.Height - totalH) / 2;

                    using var grayBrush = new SolidBrush(gray);
                    using var darkBrush = new SolidBrush(Color.FromArgb(100, 100, 100));
                    e.Graphics.DrawString(icon, iconFont, grayBrush, (rect.Width - iconSize.Width) / 2, startY);
                    e.Graphics.DrawString(title, titleFont, darkBrush, (rect.Width - titleSize.Width) / 2, startY + iconSize.Height + 8);
                    e.Graphics.DrawString(hint, hintFont, grayBrush, (rect.Width - hintSize.Width) / 2, startY + iconSize.Height + titleSize.Height + 16);
                }
            };

            LoadColumnsDataGridView();
            SetupFilterComboBox();
            SetupRightPanel();
            SetupRightPanelToggle();

            dataGridView1.MouseClick += Control_MouseClick;

            dataGridView1.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (dataGridView1.IsCurrentCellDirty)
                {
                    dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };

            // Debounced save: only write DB 500ms after last checkbox change
            _saveCheckedTimer = new System.Windows.Forms.Timer { Interval = 500 };
            _saveCheckedTimer.Tick += (s, e) =>
            {
                _saveCheckedTimer.Stop();
                Task.Run(() =>
                {
                    try { DeviceServices.SaveCheckedState(); }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[SaveCheckedState] {ex}"); }
                });
            };
            dataGridView1.CellValueChanged += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex == dataGridViewCheckBoxColumn1.Index)
                {
                    _saveCheckedTimer.Stop();
                    _saveCheckedTimer.Start();
                    UpdateStatusBar();
                }
            };

            this.Load += ucManagerDevices_Load;
            this.Disposed += (s, e) =>
            {
                _trackService?.Dispose();
                _saveCheckedTimer?.Stop();
                DeviceServices.SaveCheckedState();
                _deviceBindingSource?.Dispose();
            };
            Common.Helper.FontUtil.ApplyFontToAllControls(this);
           button1.Click += button1_Click;
        }

        private void Button2_Click(object? sender, EventArgs e)
        {
            throw new NotImplementedException();
        }


        #region Filter ComboBox

        private void SetupFilterComboBox()
        {
            cboFilter.Items.Clear();
            cboFilter.Items.Add("Tất cả");
            cboFilter.Items.Add("Online");
            cboFilter.Items.Add("Offline");
            cboFilter.SelectedValueChanged += CboFilter_SelectedValueChanged;
        }

        private void CboFilter_SelectedValueChanged(object sender, AntdUI.ObjectNEventArgs e)
        {
            _currentFilter = e.Value?.ToString() ?? "Tất cả";
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            List<DeviceModel> filtered;
            switch (_currentFilter)
            {
                case "Online":
                    filtered = DeviceServices.DeviceModels.Where(d => d.IsLive).ToList();
                    break;
                case "Offline":
                    filtered = DeviceServices.DeviceModels.Where(d => !d.IsLive).ToList();
                    break;
                default:
                    filtered = DeviceServices.DeviceModels;
                    break;
            }

            // Suspend notifications while refilling to avoid mid-update events into BindingSource
            _deviceBindingSource.RaiseListChangedEvents = false;
            _deviceBindingList.RaiseListChangedEvents = false;

            _deviceBindingList.Clear();       // OnBeforeRemove fires → ThrottledPropertyNotifier.Unregister
            foreach (var d in filtered)
                _deviceBindingList.Add(d);

            _deviceBindingList.RaiseListChangedEvents = true;
            _deviceBindingSource.RaiseListChangedEvents = true;

            _deviceBindingList.ResetBindings();
            dataGridView1.Refresh();
            UpdateStatusBar();
        }

        #endregion

        #region Right Panel (Action Toolbar)

        private AntdUI.Input? _txtDeviceId;
        private AntdUI.Input? _txtDeviceName;

        private void SetupRightPanel()
        {
            panelRight.Controls.Clear();
            var boldFont = FontScale.Body9Bold;
            var normalFont = FontScale.Body9;
            int W = 320; // usable width inside padding

            var main = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(10, 5, 5, 5)
            };

            int y = 5;

            // ── Cài đặt phone ──
            main.Controls.Add(MkGroupLabel("Cài đặt phone", boldFont, ref y, W));

            // 2-column checkboxes matching screenshot
            var chkPairs = new (string left, string right)[]
            {
                ("Cài đặt ban đầu", "Chinh sáng"),
                ("Tắt âm thanh", "Set % pin"),
                ("Cài ngôn ngữ English", "Tắt GPS"),
                ("Cài app Facebook", "Cài GolikeHelper"),
                ("Khởi động lại máy", "Cấp quyền GolikeHelper"),
            };

            // Row 0: Cài đặt ban đầu | Chinh sáng [100] %
            _chkCaiDatBanDau = MkCheckbox("Cài đặt ban đầu", normalFont);
            _chkCaiDatBanDau.Location = new Point(10, y);
            main.Controls.Add(_chkCaiDatBanDau);
            var lblBright = MkLabel("Chinh sáng", normalFont);
            lblBright.Location = new Point(170, y + 3);
            main.Controls.Add(lblBright);
            _nudBright = new NumericUpDown { Minimum = 0, Maximum = 100, Value = 100, Width = 55, Location = new Point(250, y), Font = normalFont };
            main.Controls.Add(_nudBright);
            var lblBrightPct = MkLabel("%", normalFont);
            lblBrightPct.Location = new Point(308, y + 3);
            main.Controls.Add(lblBrightPct);
            y += 28;

            // Row 1: Tắt âm thanh | Set % pin [999] %
            _chkTatAmThanh = MkCheckboxAt("Tắt âm thanh", normalFont, 10, y);
            main.Controls.Add(_chkTatAmThanh);
            var lblPin = MkLabel("Set % pin", normalFont);
            lblPin.Location = new Point(170, y + 3);
            main.Controls.Add(lblPin);
            _nudPin = new NumericUpDown { Minimum = 0, Maximum = 999, Value = 999, Width = 55, Location = new Point(250, y), Font = normalFont };
            main.Controls.Add(_nudPin);
            var lblPinPct = MkLabel("%", normalFont);
            lblPinPct.Location = new Point(308, y + 3);
            main.Controls.Add(lblPinPct);
            y += 28;

            // Row 2: Cài ngôn ngữ English | Tắt GPS
            _chkNgonNguEng = MkCheckboxAt("Cài ngôn ngữ English", normalFont, 10, y);
            main.Controls.Add(_chkNgonNguEng);
            _chkTatGPS = MkCheckboxAt("Tắt GPS", normalFont, 170, y);
            main.Controls.Add(_chkTatGPS);
            y += 28;

            // Row 3: Cài app Facebook | Cài GolikeHelper
            _chkCaiFacebook = MkCheckboxAt("Cài app Facebook", normalFont, 10, y);
            main.Controls.Add(_chkCaiFacebook);
            _chkCaiTLC = MkCheckboxAt("Cài GolikeHelper", normalFont, 170, y);
            main.Controls.Add(_chkCaiTLC);
            y += 28;

            // Row 4: Khởi động lại máy | Cấp quyền GolikeHelper
            _chkKhoiDong = MkCheckboxAt("Khởi động lại máy", normalFont, 10, y);
            main.Controls.Add(_chkKhoiDong);
            _chkCapQuyenTLC = MkCheckboxAt("Cấp quyền GolikeHelper", normalFont, 170, y);
            main.Controls.Add(_chkCapQuyenTLC);
            y += 35;

            // Buttons: Bắt đầu | Kết nối
            var btnStart = new AntdUI.Button
            {
                Text = "Bắt đầu", Type = AntdUI.TTypeMini.Primary, Shape = AntdUI.TShape.Round,
                Size = new Size(145, 36), Location = new Point(10, y), Font = boldFont
            };
            btnStart.Click += async (s, e) =>
            {
                // Chỉ lấy các device đang bị bôi đen (highlighted) trong grid
                var targets = GetSelectedDevices();
                if (targets.Count == 0)
                {
                    AntdHelper.NotifyWarn(this.FindForm(), "Chưa chọn thiết bị", "Vui lòng bôi đen ít nhất 1 thiết bị để bắt đầu.");
                    return;
                }

                bool caiDatBanDau   = _chkCaiDatBanDau?.Checked == true;
                bool tatAmThanh     = _chkTatAmThanh?.Checked == true;
                bool ngonNguEng     = _chkNgonNguEng?.Checked == true;
                bool caiFacebook    = _chkCaiFacebook?.Checked == true;
                bool khoiDong       = _chkKhoiDong?.Checked == true;
                bool tatGPS         = _chkTatGPS?.Checked == true;
                bool caiTLC         = _chkCaiTLC?.Checked == true;
                bool capQuyenTLC    = _chkCapQuyenTLC?.Checked == true;

                if (!caiDatBanDau && !tatAmThanh && !ngonNguEng && !caiFacebook
                    && !khoiDong && !tatGPS && !caiTLC && !capQuyenTLC)
                {
                    AntdHelper.NotifyWarn(this.FindForm(), "Chưa chọn thao tác", "Vui lòng tick ít nhất 1 mục trong 'Cài đặt phone'.");
                    return;
                }

                SetRightPanelEnabled(false);
                // Auto re-enable panel sau 60s — tránh kẹt UI nếu 1 device treo lâu
                _ = Task.Delay(TimeSpan.FromSeconds(60)).ContinueWith(_ =>
                {
                    if (IsDisposed || Disposing) return;
                    try { BeginInvoke(new Action(() => SetRightPanelEnabled(true))); } catch { }
                }, TaskScheduler.Default);
                try
                {
                    var tasks = new List<Task>();
                    foreach (var device in targets)
                    {
                        var dev = device;
                        tasks.Add(Task.Run(async () =>
                        {
                            var client = new ADBClient(dev);
                            try
                            {
                                // 1. Cài đặt ban đầu: setup ATX + cài GolikeHelper + cấp quyền
                                //    + reset wallpaper + cài DeviceInfoHW + cài Facebook
                                if (caiDatBanDau)
                                {
                                    dev.Status = "Cài đặt ban đầu...";
                                    dev.TypeColor = 0;

                                    // auto setup và connect atx
                                    client.Connect();

                                    // cài đặt apk com.golike.helper (+ DeviceInfoHW) và cấp quyền cơ bản
                                    await client.maxChange.Install();

                                    // cài đặt hình nền điện thoại
                                    client.maxChange.ResetWallpaperDefault();

                                    // cài đặt apk facebook
                                    await InstallFacebookApk(client);
                                }

                                // 2. Tắt âm thanh: dùng lệnh adb
                                if (tatAmThanh)
                                {
                                    dev.Status = "Tắt âm thanh...";
                                    client.Shell("media volume --stream 3 --set 0");
                                    client.Shell("media volume --stream 1 --set 0");
                                    client.Shell("media volume --stream 5 --set 0");
                                    client.Shell("media volume --stream 2 --set 0");
                                    client.Shell("media volume --stream 4 --set 0");
                                    client.LogHelper.SUCCESS("Đã tắt âm thanh");
                                }

                                // 3. Cài đặt ngôn ngữ English
                                if (ngonNguEng)
                                {
                                    dev.Status = "Cài ngôn ngữ English...";
                                    var lang = new ChangeLanguageService(client);
                                    await lang.Change("en", "US");
                                    client.LogHelper.SUCCESS("Đã chuyển ngôn ngữ English");
                                }

                                // 4. Cài đặt app Facebook
                                if (caiFacebook)
                                {
                                    dev.Status = "Cài Facebook...";
                                    await InstallFacebookApk(client);
                                }

                                // 5. Khởi động lại máy
                                if (khoiDong)
                                {
                                    dev.Status = "Khởi động lại...";
                                    client.RebootAndWaitForDeviceReady();
                                }

                                // 6. Tắt GPS
                                if (tatGPS)
                                {
                                    dev.Status = "Tắt GPS...";
                                    client.Shell("settings put secure location_providers_allowed -gps,-network");
                                    client.LogHelper.SUCCESS("Đã tắt GPS");
                                }

                                // 7. Cài GolikeHelper
                                if (caiTLC)
                                {
                                    dev.Status = "Cài GolikeHelper...";
                                    await client.maxChange.Install();
                                }

                                // 8. Cấp quyền GolikeHelper
                                if (capQuyenTLC)
                                {
                                    dev.Status = "Cấp quyền GolikeHelper...";
                                    client.SetEnableModuleMaxChange();
                                    client.LogHelper.SUCCESS("Đã cấp quyền GolikeHelper");
                                }

                                dev.Status = "Hoàn thành";
                                dev.TypeColor = 2;
                            }
                            catch (Exception ex)
                            {
                                dev.Status = $"Lỗi: {ex.Message}";
                                dev.TypeColor = 1;
                                LogManager.Error(ex);
                            }
                        }));
                    }
                    await Task.WhenAll(tasks);
                    ApplyFilter();
                }
                finally
                {
                    SetRightPanelEnabled(true);
                }
            };
            main.Controls.Add(btnStart);

            var btnConnect = new AntdUI.Button
            {
                Text = "Kết nối", Type = AntdUI.TTypeMini.Success, Shape = AntdUI.TShape.Round,
                Size = new Size(145, 36), Location = new Point(170, y), Font = boldFont
            };
            btnConnect.Click += async (s, e) =>
            {
                // Get devices: selected (bôi đen) OR checked (tick)
                var targets = GetSelectedOrCheckedDevices();
                if (targets.Count == 0)
                {
                    AntdHelper.NotifyWarn(this.FindForm(), "Chưa chọn thiết bị", "Vui lòng chọn hoặc bôi đen ít nhất 1 thiết bị.");
                    return;
                }
                // Temporarily check them so Connect() picks them up
                foreach (var d in targets) d.Checked = true;
                await DeviceServices.Connect();
                ApplyFilter();
            };
            main.Controls.Add(btnConnect);
            y += 50;

            // ── Cập nhật tên ──
            main.Controls.Add(MkGroupLabel("Cập nhật tên", boldFont, ref y, W));

            // Id row
            var lblId = MkLabel("Id", boldFont);
            lblId.Location = new Point(15, y + 5);
            main.Controls.Add(lblId);
            _txtDeviceId = new AntdUI.Input { Location = new Point(70, y), Size = new Size(245, 30), Font = normalFont };
            main.Controls.Add(_txtDeviceId);
            y += 38;

            // Name row
            var lblName = MkLabel("Name", boldFont);
            lblName.Location = new Point(15, y + 5);
            main.Controls.Add(lblName);
            _txtDeviceName = new AntdUI.Input { Location = new Point(70, y), Size = new Size(245, 30), Font = normalFont };
            main.Controls.Add(_txtDeviceName);
            y += 38;

            // Auto Index | Update
            var btnAutoIndex = new AntdUI.Button
            {
                Text = "Auto Index", Shape = AntdUI.TShape.Round, Size = new Size(145, 34),
                Location = new Point(10, y), Font = normalFont
            };
            btnAutoIndex.Click += (s, e) =>
            {
                var selected = GetSelectedOrCheckedDevices();
                if (selected.Count == 0)
                {
                    AntdHelper.NotifyWarn(this.FindForm(), "Chưa chọn thiết bị", "Vui lòng chọn hoặc bôi đen ít nhất 1 thiết bị.");
                    return;
                }
                string baseName = (_txtDeviceName?.Text ?? "").Trim();
                for (int i = 0; i < selected.Count; i++)
                    selected[i].NameDevice = string.IsNullOrEmpty(baseName) ? $"{i + 1}" : $"{baseName} {i + 1}";
                dataGridView1.Refresh();
            };
            main.Controls.Add(btnAutoIndex);
            var btnUpdate = new AntdUI.Button
            {
                Text = "Update", Shape = AntdUI.TShape.Round, Size = new Size(145, 34),
                Location = new Point(170, y), Font = normalFont, Type = AntdUI.TTypeMini.Primary
            };
            btnUpdate.Click += async (s, e) =>
            {
                var selected = GetSelectedOrCheckedDevices();
                if (selected.Count == 0)
                {
                    AntdHelper.NotifyWarn(this.FindForm(), "Chưa chọn thiết bị", "Vui lòng chọn hoặc bôi đen ít nhất 1 thiết bị.");
                    return;
                }
                string newName = (_txtDeviceName?.Text ?? "").Trim();
                if (string.IsNullOrEmpty(newName))
                {
                    AntdHelper.NotifyWarn(this.FindForm(), "Thiếu tên", "Vui lòng nhập Name trước khi Update.");
                    return;
                }
                foreach (var dev in selected) dev.NameDevice = newName;
                dataGridView1.Refresh();

                var tasks = new List<Task>();
                foreach (var device in selected)
                {
                    var dev = device;
                    tasks.Add(Task.Run(() =>
                    {
                        try
                        {
                            var client = new ADBClient(dev);
                            client.Shell($"settings put global device_name \"{dev.NameDevice}\"");
                            dev.Status = "Đổi tên thành công";
                            dev.TypeColor = 2;
                        }
                        catch (Exception ex)
                        {
                            dev.Status = $"Lỗi: {ex.Message}";
                            dev.TypeColor = 1;
                        }
                    }));
                }
                await Task.WhenAll(tasks);
                dataGridView1.Refresh();
            };
            main.Controls.Add(btnUpdate);
            y += 48;

            // ── Test change ──
            main.Controls.Add(MkGroupLabel("Test change", boldFont, ref y, W));
            var btnInfoDevice = new AntdUI.Button
            {
                Text = "Info Device", Shape = AntdUI.TShape.Round, Size = new Size(145, 34),
                Location = new Point(10, y), Font = normalFont
            };
            main.Controls.Add(btnInfoDevice);
            var btnChange4G = new AntdUI.Button
            {
                Text = "Change 4G", Shape = AntdUI.TShape.Round, Size = new Size(145, 34),
                Location = new Point(170, y), Font = normalFont, Type = AntdUI.TTypeMini.Primary
            };
            main.Controls.Add(btnChange4G);
            y += 48;

            // ── Chức năng khác ──
            main.Controls.Add(MkGroupLabel("Chức năng khác", boldFont, ref y, W));
            var btnTelegram = new AntdUI.Button
            {
                Text = "Thông báo Telegram", Shape = AntdUI.TShape.Round, Size = new Size(145, 34),
                Location = new Point(10, y), Font = normalFont
            };
            main.Controls.Add(btnTelegram);
            var btnSort = new AntdUI.Button
            {
                Text = "Sắp xếp", Shape = AntdUI.TShape.Round, Size = new Size(90, 34),
                Location = new Point(170, y), Font = normalFont
            };
            main.Controls.Add(btnSort);
            var nudSortNum = new NumericUpDown { Minimum = 1, Maximum = 100, Value = 10, Width = 50, Location = new Point(268, y + 2), Font = normalFont };
            main.Controls.Add(nudSortNum);
            y += 48;

            _mainRightPanel = main;
            panelRight.Controls.Add(main);

            // Fill Id/Name when selecting a row
            dataGridView1.SelectionChanged += (s, e) =>
            {
                if (dataGridView1.SelectedRows.Count == 1 &&
                    dataGridView1.SelectedRows[0].DataBoundItem is DeviceModel dev)
                {
                    _txtDeviceId.Text = dev.Serial;
                    _txtDeviceName.Text = dev.NameDevice ?? "";
                }
            };
        }

        private AntdUI.Button? _btnTogglePanel;
        private System.Windows.Forms.Panel? _mainRightPanel;

        private void SetupRightPanelToggle()
        {
            _savedSplitterDistance = splitContainer1.SplitterDistance;

            _btnTogglePanel = new AntdUI.Button
            {
                Text = "«",
                Size = new Size(22, 80),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Shape = AntdUI.TShape.Round,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                BackColor = Color.FromArgb(230, 230, 230),
            };

            // Đặt button lên Form chính (fMain), sát mép phải
            this.ParentChanged += (s, e) =>
            {
                var form = this.FindForm();
                if (form == null || _btnTogglePanel.Parent == form) return;
                form.Controls.Add(_btnTogglePanel);
                _btnTogglePanel.BringToFront();
                PositionToggleButton(form);
                form.Resize += (_, __) => PositionToggleButton(form);
            };


            _btnTogglePanel.Click += (s, e) =>
            {
                _rightPanelCollapsed = !_rightPanelCollapsed;
                if (_rightPanelCollapsed)
                {
                    _savedSplitterDistance = splitContainer1.SplitterDistance;
                    splitContainer1.Panel2Collapsed = true;
                    _btnTogglePanel.Text = "»";
                }
                else
                {
                    splitContainer1.Panel2Collapsed = false;
                    splitContainer1.SplitterDistance = _savedSplitterDistance;
                    _btnTogglePanel.Text = "«";
                }
                var form = this.FindForm();
                if (form != null) PositionToggleButton(form);
            };
        }

        private void PositionToggleButton(Form form)
        {
            if (_btnTogglePanel == null) return;
            // Sát mép phải form, căn giữa theo chiều dọc vùng content
            _btnTogglePanel.Location = new Point(
                form.ClientSize.Width - _btnTogglePanel.Width - 2,
                (form.ClientSize.Height - _btnTogglePanel.Height) / 2);
        }

        // Helper: section title with GroupBox-style line
        private System.Windows.Forms.Label MkGroupLabel(string text, Font font, ref int y, int w)
        {
            var lbl = new System.Windows.Forms.Label
            {
                Text = text, Font = font, AutoSize = false,
                Size = new Size(w, 22), Location = new Point(5, y),
                ForeColor = ColorTranslator.FromHtml("#333333"),
                BorderStyle = BorderStyle.None
            };
            y += 25;
            return lbl;
        }

        private System.Windows.Forms.Label MkLabel(string text, Font font)
        {
            return new System.Windows.Forms.Label { Text = text, Font = font, AutoSize = true };
        }

        private void SetRightPanelEnabled(bool enabled)
        {
            if (_mainRightPanel == null) return;
            void SetEnabled(Control.ControlCollection controls)
            {
                foreach (Control c in controls)
                {
                    if (c is DataGridView) continue;
                    c.Enabled = enabled;
                    if (c.Controls.Count > 0) SetEnabled(c.Controls);
                }
            }
            SetEnabled(_mainRightPanel.Controls);
        }

        private AntdUI.Checkbox MkCheckbox(string text, Font font)
        {
            return new AntdUI.Checkbox { Text = text, AutoSize = true, Font = font };
        }

        private AntdUI.Checkbox MkCheckboxAt(string text, Font font, int x, int y)
        {
            return new AntdUI.Checkbox { Text = text, AutoSize = true, Font = font, Location = new Point(x, y) };
        }

        /// <summary>
        /// Get devices that are either selected (bôi đen) or checked (tick).
        /// </summary>
        private List<DeviceModel> GetSelectedOrCheckedDevices()
        {
            var result = new HashSet<DeviceModel>();
            // Add checked devices
            foreach (var d in DeviceServices.DeviceModels)
                if (d.Checked) result.Add(d);
            // Add selected (highlighted) rows
            foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                if (row.DataBoundItem is DeviceModel dev)
                    result.Add(dev);
            return result.ToList();
        }

        private static void LaunchViewControl(string args)
        {
            try
            {
                string exePath = Path.Combine(AppContext.BaseDirectory, "Golike-Android-View.exe");
                if (!File.Exists(exePath))
                {
                    MessageBox.Show($"Không tìm thấy Golike-Android-View.exe tại:\n{exePath}", "Golike Android View", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = args,
                    UseShellExecute = false
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi khởi chạy Golike Android View:\n{ex.Message}", "Golike Android View", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Live Column

        // Live indicator is handled in CellFormatting via text "●" with color

        #endregion

        #region ADB Track Devices

        private void StartTrackDevices()
        {
            _trackService?.Dispose();
            _trackService = new AdbTrackDevicesService();
            _trackService.DevicesChanged += OnDevicesChanged;
            _trackService.Start();
        }

        private void OnDevicesChanged(List<string> onlineSerials)
        {
            // Heavy ADB work runs HERE on background thread
            bool changed = DeviceServices.UpdateDeviceOnlineStatus(onlineSerials);

            // Only touch UI if something changed
            if (!changed) return;

            if (this.IsDisposed) return;
            if (this.InvokeRequired)
            {
                this.BeginInvoke(() => OnDeviceStatusUpdated());
            }
            else
            {
                OnDeviceStatusUpdated();
            }
        }

        private void OnDeviceStatusUpdated()
        {
            // Lightweight UI-only work
            ApplyFilter();
            UpdateStatusBar();
        }

        #endregion

        private void LoadColumnsDataGridView()
        {
            var centerStyle = new DataGridViewCellStyle
            {
                Font = FontScale.Body9Bold,
                ForeColor = Color.FromArgb(0, 120, 215),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            var leftStyle = new DataGridViewCellStyle
            {
                Font = FontScale.Body9Bold,
                ForeColor = Color.FromArgb(0, 120, 215),
                Alignment = DataGridViewContentAlignment.MiddleLeft
            };
            dataGridViewCheckBoxColumn1.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = FontScale.Body9Bold,
                ForeColor = Color.FromArgb(0, 120, 215),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            dataGridViewCheckBoxColumn1.DataPropertyName = nameof(DeviceModel.Checked);
            dataGridViewTextBoxColumn1.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = FontScale.Body9Bold,
                ForeColor = Color.FromArgb(0, 120, 215),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            dataGridViewTextBoxColumn1.ToolTipText = "Số thứ tự trong bảng";
            dataGridViewTextBoxColumn1.DataPropertyName = nameof(DeviceModel.Index);

            var columns = new List<(string Name, string Header, string Tooltip)>
                    {
                        ("Live", "Live", "Trạng thái kết nối"),
                        (nameof(DeviceModel.Serial), nameof(DeviceModel.Serial), "DeviceId thiết bị"),
                        (nameof(DeviceModel.NameDevice), "Tên", "Tên thiết bị"),
                        (nameof(DeviceModel.OS), nameof(DeviceModel.OS), "Android version"),
                        (nameof(DeviceModel.Status), "Trạng thái", "Trạng thái thiết bị"),
                        (nameof(DeviceModel.TypeColor), "Trạng thái", "Trạng thái thiết bị"),
                    };

            var colDefs = new List<DataGridViewColumn>();

            foreach (var col in columns)
            {
                string header = col.Header;
                string tooltip = col.Tooltip;
                bool visible = !(col.Name == nameof(DeviceModel.TypeColor));

                // Live column = center, all others = left
                bool isCenter = col.Name == "Live";

                int minWidth = col.Name switch
                {
                    "Live" => 50,
                    nameof(DeviceModel.OS) => 50,
                    nameof(DeviceModel.Status) => 300,
                    _ => 100
                };
                var sizeMode = col.Name == nameof(DeviceModel.Status)
                    ? DataGridViewAutoSizeColumnMode.Fill
                    : DataGridViewAutoSizeColumnMode.None;

                colDefs.Add(CreateColumnsDataGridView(
                    col.Name == "Live" ? "IsLive" : col.Name,
                    header,
                    tooltip,
                    visible,
                    minWidth,
                    sizeMode,
                    isCenter ? centerStyle : leftStyle
                ));
            }

            // Override Live column name for CellPainting
            if (colDefs.Count > 0)
            {
                colDefs[0].Name = "col_Live";
            }

            dataGridView1.Columns.AddRange(colDefs.ToArray());
        }

        private DataGridViewColumn CreateColumnsDataGridView(string dataPropertyName, string header, string toolTip, bool visible, int miniWith, DataGridViewAutoSizeColumnMode size, DataGridViewCellStyle style)
        {
            DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn();
            column.DefaultCellStyle = style;
            column.DataPropertyName = dataPropertyName;
            column.Name = "col_" + dataPropertyName;
            column.HeaderText = header;
            column.ToolTipText = toolTip;
            column.ReadOnly = true;
            column.MinimumWidth = miniWith;
            column.Width = miniWith;
            column.Visible = visible;
            column.AutoSizeMode = size;
            return column;
        }

        private bool _suppressSelectionChanged;
        private void DataGridView_SelectionChanged(object sender, EventArgs e)
        {
            if (_suppressSelectionChanged) return;
            int selectedRowCount = dataGridView1.SelectedRows.Count;
            toolStripLabel12.Text = selectedRowCount.ToMoneyString();
        }

        private void uiDataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var dgv = sender as DataGridView;
            var row = dgv.Rows[e.RowIndex];
            if (row.DataBoundItem is not DeviceModel device) return;

            string colName = dgv.Columns[e.ColumnIndex].Name;

            // Live column: show "●" with cached font (no allocation)
            if (colName == "col_Live")
            {
                e.Value = "●";
                e.FormattingApplied = true;
                var color = device.IsLive ? _greenLive : _redDead;
                e.CellStyle.ForeColor = color;
                e.CellStyle.SelectionForeColor = color;
                e.CellStyle.Font = _liveDotFont;
                e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                return;
            }

            // Row text color via e.CellStyle (avoids paint loop from row.DefaultCellStyle)
            if (!device.IsAdbOnline)
            {
                e.CellStyle.ForeColor = _grayOffline;
            }
            else
            {
                e.CellStyle.ForeColor = device.TypeColor switch
                {
                    1 => Color.Red,
                    2 => Color.FromArgb(34, 139, 34),
                    _ => Color.FromArgb(0, 120, 215)
                };
            }
            // Giữ cùng ForeColor khi row được "bôi đen" để đọc được trên nền pale blue
            e.CellStyle.SelectionForeColor = e.CellStyle.ForeColor;
        }

        private void UpdateStatusBar()
        {
            toolStripLabel8.Text = $"{DeviceServices.DeviceModels.Count.ToMoneyString()}";
            int onlineCount = DeviceServices.DeviceModels.Count(d => d.IsLive);
            toolStripLabelOnlineCount.Text = $"{onlineCount}";
            int checkedCount = DeviceServices.DeviceModels.Count(d => d.Checked);
            toolStripLabel10.Text = $"{checkedCount}";
        }

        public async Task LoadDevices()
        {
            try
            {
                ApplyFilter();
                _ = Configs();
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                throw;
            }
            finally
            {
                menulist = null;
                if (dataGridView1.Rows.Count > 0)
                {
                    CreateMenuStrip();
                }
            }

        }

        private async Task Configs()
        {
            List<DeviceModel> devices = new List<DeviceModel>();
            var adbTasks = new List<Task>();

            for (int i = 0; i < DeviceServices.DeviceModels.Count; i++)
            {
                DeviceModel device = DeviceServices.DeviceModels[i];
                if (device == null) continue;
                if (device.Serial.Contains("emulator"))
                {
                    devices.Add(device);
                }
                else
                {
                    var serial = device.Serial;
                    adbTasks.Add(Task.Run(() =>
                    {
                        ProcessHelper.RunAdbCommand($"-s {serial} shell wm size 1440x2560");
                        ProcessHelper.RunAdbCommand($"-s {serial} shell wm density 560");
                    }));
                }
            }
            await Task.WhenAll(adbTasks);
            string folderPath = LdPlayerHelper.GetPathFolder().Replace("dnplayer.exe", "");
            if (!Directory.Exists(folderPath)) return;
            var indexs = LdPlayerHelper.GetIndex(Path.Combine(folderPath, "ldDebug.exe"));
            if (!indexs.Any()) return;
            await Config(folderPath, indexs);
        }

        private async Task Config(string folderPath, List<int> indexs)
        {
            List<Task> tasks = new List<Task>();

            foreach (int index in indexs)
            {
                tasks.Add(Task.Run(() =>
                {
                    string fileConfig = Path.Combine(folderPath, "vms", "config", $"leidian{index}.config");
                    if (LdPlayerHelper.Config(fileConfig))
                    {
                        LdPlayerHelper.Close(Path.Combine(folderPath, "ldDebug.exe"), index.ToString());
                        LdPlayerHelper.Config(fileConfig);
                        LdPlayerHelper.Open(Path.Combine(folderPath, "ldDebug.exe"), index.ToString());
                    }
                }));
            }

            await Task.WhenAll(tasks);
            LdPlayerHelper.SortWnd(folderPath);

            ApplyFilter();
        }

        private async void ucManagerDevices_Load(object sender, EventArgs e)
        {
            try
            {
                // Set Panel2 = 350px after layout is ready
                this.BeginInvoke((Action)(() =>
                {
                    try
                    {
                        int targetPanel2 = 350;
                        int available = splitContainer1.Width - splitContainer1.SplitterWidth;
                        if (available > targetPanel2 + 100)
                        {
                            splitContainer1.Panel2MinSize = targetPanel2;
                            splitContainer1.SplitterDistance = available - targetPanel2;
                        }
                    }
                    catch { }
                }));

                // Wire persistent BindingSource — done here (not constructor) so
                // the control handle and SynchronizationContext are already ready.
                _deviceBindingSource.DataSource = _deviceBindingList;
                dataGridView1.DataSource = _deviceBindingSource;

                Enable(false);
                // Lần đầu mở form: phải query ADB lấy danh sách device rồi mới bind UI.
                // Nếu chỉ gọi LoadDevices() thì DeviceServices.DeviceModels còn rỗng → bảng trống.
                // GetDeviceModels đã gọi ADBHelper.GetDevices (đã EnsureServerStarted + retry).
                await DeviceServices.GetDeviceModels();
                await LoadDevices();
                Enable(true);

                // Start ADB track-devices auto-update
                StartTrackDevices();
            }
            catch (Exception ex)
            {
                Enable(true);
                System.Diagnostics.Debug.WriteLine($"[ucManagerDevices_Load] {ex}");
            }
        }
        private void Enable(bool enable)
        {
            button53.Enabled = enable;
            button1.Enabled = enable;
            button2.Enabled = enable;
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            try
            {
                Enable(false);
                await DeviceServices.ADBKill();
                await LoadDevices();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[button1_Click] {ex}");
            }
            finally { Enable(true); }
        }

        private async void button53_Click(object sender, EventArgs e)
        {
            try
            {
                Enable(false);
                await DeviceServices.GetDeviceModels();
                await LoadDevices();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[button53_Click] {ex}");
            }
            finally { Enable(true); }
        }
        private void CreateMenuStrip()
        {
            string tick1svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"m424-312 282-282-56-56-226 226-114-114-56 56 170 170ZM200-120q-33 0-56.5-23.5T120-200v-560q0-33 23.5-56.5T200-840h560q33 0 56.5 23.5T840-760v560q0 33-23.5 56.5T760-120H200Zm0-80h560v-560H200v560Zm0-560v560-560Z\"/></svg>";

            menulist = new AntdUI.IContextMenuStripItem[]
            {
                new AntdUI.ContextMenuStripItem("Chọn").SetIcon(tick1svg).SetSub(
                    new AntdUI.ContextMenuStripItem("Tất cả").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M268-240 42-466l57-56 170 170 56 56-57 56Zm226 0L268-466l56-57 170 170 368-368 56 57-424 424Zm0-226-57-56 198-198 57 56-198 198Z\"/></svg>"),
                    new AntdUI.ContextMenuStripItem("Bôi đen").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M655-200 513-342l56-56 85 85 170-170 56 57-225 226Zm0-320L513-662l56-56 85 85 170-170 56 57-225 226ZM80-280v-80h360v80H80Zm0-320v-80h360v80H80Z\"/></svg>"),
                    new AntdUI.ContextMenuStripItem("Bỏ chọn bôi đen").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M200-120q-33 0-56.5-23.5T120-200v-560q0-33 23.5-56.5T200-840h560q33 0 56.5 23.5T840-760v560q0 33-23.5 56.5T760-120H200Zm0-80h560v-560H200v560Z\"/></svg>")
                ),
                new AntdUI.ContextMenuStripItem("Bỏ chọn tất cả").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M200-120q-33 0-56.5-23.5T120-200v-560q0-33 23.5-56.5T200-840h560q33 0 56.5 23.5T840-760v560q0 33-23.5 56.5T760-120H200Zm0-80h560v-560H200v560Z\"/></svg>"),
                new AntdUI.ContextMenuStripItem("Connect").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#00aa00\"><path d=\"M480-80q-83 0-156-31.5T197-197q-54-54-85.5-127T80-480q0-83 31.5-156T197-763q54-54 127-85.5T480-880q83 0 156 31.5T763-763q54 54 85.5 127T880-480q0 83-31.5 156T763-197q-54 54-127 85.5T480-80Zm0-80q134 0 227-93t93-227q0-134-93-227t-227-93q-134 0-227 93t-93 227q0 134 93 227t227 93Zm0-320Z\"/></svg>"),
                new AntdUI.ContextMenuStripItem("Disconnect").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#cc0000\"><path d=\"M480-80q-83 0-156-31.5T197-197q-54-54-85.5-127T80-480q0-83 31.5-156T197-763q54-54 127-85.5T480-880q83 0 156 31.5T763-763q54 54 85.5 127T880-480q0 83-31.5 156T763-197q-54 54-127 85.5T480-80Zm0-80q134 0 227-93t93-227q0-134-93-227t-227-93q-134 0-227 93t-93 227q0 134 93 227t227 93Zm0-320Z\"/></svg>"),
                new AntdUI.ContextMenuStripItem("Màn hình").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M80-160v-120h80v-440q0-33 23.5-56.5T240-800h600v80H240v440h240v120H80Zm520 0q-17 0-28.5-11.5T560-200v-360q0-17 11.5-28.5T600-600h240q17 0 28.5 11.5T880-560v360q0 17-11.5 28.5T840-160H600Zm40-80h160v-280H640v280Zm0 0h160-160Z\"/></svg>").SetSub(
                    new AntdUI.ContextMenuStripItem("View nhiều màn hình").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#2196F3\"><path d=\"M80-160v-120h80v-440q0-33 23.5-56.5T240-800h600v80H240v440h240v120H80Zm520 0q-17 0-28.5-11.5T560-200v-360q0-17 11.5-28.5T600-600h240q17 0 28.5 11.5T880-560v360q0 17-11.5 28.5T840-160H600Zm40-80h160v-280H640v280Zm0 0h160-160Z\"/></svg>"),
                    new AntdUI.ContextMenuStripItem("View 1 màn hình").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#4CAF50\"><path d=\"M280-120q-33 0-56.5-23.5T200-200v-560q0-33 23.5-56.5T280-840h400q33 0 56.5 23.5T760-760v560q0 33-23.5 56.5T680-120H280Zm0-80h400v-560H280v560Zm200-280Z\"/></svg>"),
                    new AntdUI.ContextMenuStripItem("Đóng màn hình").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#cc0000\"><path d=\"m256-200-56-56 224-224-224-224 56-56 224 224 224-224 56 56-224 224 224 224-56 56-224-224-224 224Z\"/></svg>")
                ),
                new AntdUI.ContextMenuStripItem("Chức năng").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M520-600v-240h320v240H520ZM120-440v-400h320v400H120Zm400 320v-400h320v400H520Zm-400 0v-240h320v240H120Zm80-400h160v-240H200v240Zm400 320h160v-240H600v240Zm0-480h160v-80H600v80ZM200-200h160v-80H200v80Zm160-320Zm240-160Zm0 240ZM360-280Z\"/></svg>").SetSub(
                    new AntdUI.ContextMenuStripItem("Nâng cao").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"m370-80-16-128q-13-5-24.5-12T307-235l-119 50L78-375l103-78q-1-7-1-13.5v-27q0-6.5 1-13.5L78-585l110-190 119 50q11-8 23-15t24-12l16-128h220l16 128q13 5 24.5 12T590-725l119-50 110 190-103 78q1 7 1 13.5v27q0 6.5-1 13.5l103 78-110 190-119-50q-11 8-23 15t-24 12L590-80H370Zm70-80h79l14-106q31-8 57.5-23.5T639-327l99 41 39-68-86-65q5-14 7-29.5t2-31.5q0-16-2-31.5t-7-29.5l86-65-39-68-99 42q-22-23-48.5-38.5T533-694l-13-106h-79l-14 106q-31 8-57.5 23.5T321-633l-99-41-39 68 86 64q-5 15-7 30t-2 32q0 16 2 31t7 30l-86 65 39 68 99-42q22 23 48.5 38.5T427-266l13 106Zm42-180q58 0 99-41t41-99q0-58-41-99t-99-41q-59 0-99.5 41T342-480q0 58 40.5 99t99.5 41Zm-2-140Z\"/></svg>").SetSub(
                        new AntdUI.ContextMenuStripItem("Cài đặt apk").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M480-320 280-520l56-58 104 104v-326h80v326l104-104 56 58-200 200ZM240-160q-33 0-56.5-23.5T160-240v-120h80v120h480v-120h80v120q0 33-23.5 56.5T720-160H240Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Tắt âm thanh").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M792-56 671-177q-25 16-53 27.5T560-131v-82q14-5 27.5-10t25.5-12L480-368v208L280-360H120v-240h128L56-792l56-56 736 736-56 56Zm-8-232-58-58q17-31 25.5-65t8.5-70q0-94-55-168T560-749v-82q124 28 202 125.5T840-480q0 53-14.5 102T784-288ZM640-514l-56-56-23-22v-271l200 200ZM480-800 376-696l-96-96 200-200v192Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Tắt blutooth").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M440-80v-294L201-135l-56-57 256-256v-44L145-748l56-57 239 239v-314h40l238 238-200 200 200 200L480-80h-40Zm80-496 76-76-76-74v150Zm0 342 76-74-76-76v150Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Push file to sdcard").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M440-320v-326L336-542l-56-58 200-200 200 200-56 58-104-104v326h-80ZM240-160q-33 0-56.5-23.5T160-240v-120h80v120h480v-120h80v120q0 33-23.5 56.5T720-160H240Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Cài package cần thiết").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M440-183v-274L200-596v274l240 139Zm80 0 240-139v-274L520-457v274Zm-40-343 237-137-237-137-237 137 237 137ZM160-252q-19-11-29.5-29T120-322v-316q0-22 10.5-40t29.5-29l280-161q19-11 40-11t40 11l280 161q19 11 29.5 29t10.5 40v316q0 22-10.5 40T800-252L520-91q-19 11-40 11t-40-11L160-252Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Cài ngôn ngữ tiếng việt").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" width=\"24px\" height=\"24px\" viewBox=\"0 0 64 64\"><circle cx=\"32\" cy=\"32\" r=\"30\" fill=\"#f42f4c\"/><path fill=\"#ffe62e\" d=\"M32 39l9.9 7l-3.7-11.4l9.8-7.4H35.8L32 16l-3.7 11.2H16l9.8 7.4L22.1 46z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Check IP bằng browser").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M480-80q-83 0-156-31.5T197-197q-54-54-85.5-127T80-480q0-83 31.5-156T197-763q54-54 127-85.5T480-880q83 0 156 31.5T763-763q54 54 85.5 127T880-480q0 83-31.5 156T763-197q-54 54-127 85.5T480-80Zm-40-82v-78q-33 0-56.5-23.5T360-320v-40L168-552q-3 18-5.5 36t-2.5 36q0 121 79.5 212T440-162Zm276-102q20-22 35.5-47.5t26-53q10.5-27.5 16-56.5t5.5-59q0-98-54-177.5T600-776v16q0 33-23.5 56.5T520-680h-80v80q0 17-11.5 28.5T400-560h-80v80h240q17 0 28.5 11.5T600-440v120h40q26 0 47 15.5t29 40.5Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Chuyển về bàn phím adb").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M160-200q-33 0-56.5-23.5T80-280v-400q0-33 23.5-56.5T160-760h640q33 0 56.5 23.5T880-680v400q0 33-23.5 56.5T800-200H160Zm0-80h640v-400H160v400Zm160-40h320v-80H320v80ZM200-440h80v-80h-80v80Zm120 0h80v-80h-80v80Zm120 0h80v-80h-80v80Zm120 0h80v-80h-80v80Zm120 0h80v-80h-80v80Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Chuyển về bàn phím thường").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M160-200q-33 0-56.5-23.5T80-280v-400q0-33 23.5-56.5T160-760h640q33 0 56.5 23.5T880-680v400q0 33-23.5 56.5T800-200H160Zm0-80h640v-400H160v400Zm160-40h320v-80H320v80ZM200-440h80v-80h-80v80Zm120 0h80v-80h-80v80Zm120 0h80v-80h-80v80Zm120 0h80v-80h-80v80Zm120 0h80v-80h-80v80Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Bật chế độ không làm phiền").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M480-80q-83 0-156-31.5T197-197q-54-54-85.5-127T80-480q0-83 31.5-156T197-763q54-54 127-85.5T480-880q83 0 156 31.5T763-763q54 54 85.5 127T880-480q0 83-31.5 156T763-197q-54 54-127 85.5T480-80Zm0-80q54 0 104-17.5t92-50.5L228-676q-33 42-50.5 92T160-480q0 134 93 227t227 93Zm252-124q33-42 50.5-92T800-480q0-134-93-227t-227-93q-54 0-104 17.5T284-732l448 448Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Tắt nguồn thiết bị").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M440-80q-133-19-221.5-122.5T130-450v-310l310-120 310 120v310q0 137-88.5 240.5T440-80Zm0-92q88-26 144-114.5T640-483v-249l-200-77-200 77v249q0 105 56 193.5T440-172Zm0-308Z\"/></svg>")
                    ),
                    new AntdUI.ContextMenuStripItem("Màn hình").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M80-160v-120h80v-440q0-33 23.5-56.5T240-800h600v80H240v440h240v120H80Zm520 0q-17 0-28.5-11.5T560-200v-360q0-17 11.5-28.5T600-600h240q17 0 28.5 11.5T880-560v360q0 17-11.5 28.5T840-160H600Zm40-80h160v-280H640v280Zm0 0h160-160Z\"/></svg>").SetSub(
                        new AntdUI.ContextMenuStripItem("Vẽ màn hình chính").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M200-120q-33 0-56.5-23.5T120-200v-560q0-33 23.5-56.5T200-840h560q33 0 56.5 23.5T840-760v560q0 33-23.5 56.5T760-120H200Zm0-80h560v-560H200v560Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Mở khóa màn hình").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M240-640h360v-80q0-50-35-85t-85-35q-50 0-85 35t-35 85h-80q0-83 58.5-141.5T480-920q83 0 141.5 58.5T680-720v80h40q33 0 56.5 23.5T800-560v360q0 33-23.5 56.5T720-120H240q-33 0-56.5-23.5T160-200v-360q0-33 23.5-56.5T240-640Zm240 360q33 0 56.5-23.5T560-360q0-33-23.5-56.5T480-440q-33 0-56.5 23.5T400-360q0 33 23.5 56.5T480-280Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Khóa màn hình").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M240-640h360v-80q0-50-35-85t-85-35q-50 0-85 35t-35 85h-80q0-83 58.5-141.5T480-920q83 0 141.5 58.5T680-720v80h40q33 0 56.5 23.5T800-560v360q0 33-23.5 56.5T720-120H240q-33 0-56.5-23.5T160-200v-360q0-33 23.5-56.5T240-640Zm240 360q33 0 56.5-23.5T560-360q0-33-23.5-56.5T480-440q-33 0-56.5 23.5T400-360q0 33 23.5 56.5T480-280Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Tắt màn hình").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"m256-200-56-56 224-224-224-224 56-56 224 224 224-224 56 56-224 224 224 224-56 56-224-224-224 224Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Tắt xoay màn hình").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M480-80q-83 0-156-31.5T197-197q-54-54-85.5-127T80-480q0-83 31.5-156T197-763q54-54 127-85.5T480-880q83 0 156 31.5T763-763q54 54 85.5 127T880-480q0 83-31.5 156T763-197q-54 54-127 85.5T480-80Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Tắt tự tắt màn hình").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M480-80q-83 0-156-31.5T197-197q-54-54-85.5-127T80-480q0-83 31.5-156T197-763q54-54 127-85.5T480-880q83 0 156 31.5T763-763q54 54 85.5 127T880-480q0 83-31.5 156T763-197q-54 54-127 85.5T480-80Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Chụp ảnh màn hình").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M480-260q75 0 127.5-52.5T660-440q0-75-52.5-127.5T480-620q-75 0-127.5 52.5T300-440q0 75 52.5 127.5T480-260Zm0-80q-42 0-71-29t-29-71q0-42 29-71t71-29q42 0 71 29t29 71q0 42-29 71t-71 29ZM160-120q-33 0-56.5-23.5T80-200v-480q0-33 23.5-56.5T160-760h126l74-80h240l74 80h126q33 0 56.5 23.5T880-680v480q0 33-23.5 56.5T800-120H160Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Cài hình nền").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M200-120q-33 0-56.5-23.5T120-200v-560q0-33 23.5-56.5T200-840h560q33 0 56.5 23.5T840-760v560q0 33-23.5 56.5T760-120H200Zm0-80h560v-560H200v560Zm0 0v-560 560Zm80-80h400L560-440l-120 160-80-100-80 100Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Cài hình nền phone farm").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M200-120q-33 0-56.5-23.5T120-200v-560q0-33 23.5-56.5T200-840h560q33 0 56.5 23.5T840-760v560q0 33-23.5 56.5T760-120H200Zm0-80h560v-560H200v560Zm0 0v-560 560Zm80-80h400L560-440l-120 160-80-100-80 100Z\"/></svg>")
                    ),
                    new AntdUI.ContextMenuStripItem("Chỉnh sáng").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#FFA500\"><path d=\"M480-280q-83 0-141.5-58.5T280-480q0-83 58.5-141.5T480-680q83 0 141.5 58.5T680-480q0 83-58.5 141.5T480-280ZM200-440H40v-80h160v80Zm720 0H760v-80h160v80ZM440-760v-160h80v160h-80Zm0 720v-160h80v160h-80ZM256-650l-101-102 56-57 102 103-57 56Zm492 494-103-104 56-56 104 102-57 58ZM154-254l56-57 103 103-57 57-102-103Zm494-494 56-56 102 101-57 57-101-102Z\"/></svg>").SetSub(
                        new AntdUI.ContextMenuStripItem("0%").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M440-760v-160h80v160h-80Zm0 720v-160h80v160h-80ZM154-254l56-57 103 103-57 57-102-103Zm492-494-57-56 102-101 57 57-102 100Zm98 248h160v80H760v-80ZM40-440h160v80H40v-80Zm397 23Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("25%").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#aaaaaa\"><path d=\"M480-280q-83 0-141.5-58.5T280-480q0-83 58.5-141.5T480-680q83 0 141.5 58.5T680-480q0 83-58.5 141.5T480-280ZM200-440H40v-80h160v80Zm720 0H760v-80h160v80ZM440-760v-160h80v160h-80Zm0 720v-160h80v160h-80Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("50%").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#888888\"><path d=\"M480-280q-83 0-141.5-58.5T280-480q0-83 58.5-141.5T480-680q83 0 141.5 58.5T680-480q0 83-58.5 141.5T480-280ZM200-440H40v-80h160v80Zm720 0H760v-80h160v80ZM440-760v-160h80v160h-80Zm0 720v-160h80v160h-80ZM256-650l-101-102 56-57 102 103-57 56Zm492 494-103-104 56-56 104 102-57 58ZM154-254l56-57 103 103-57 57-102-103Zm494-494 56-56 102 101-57 57-101-102Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("100%").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#FFA500\"><path d=\"M480-280q-83 0-141.5-58.5T280-480q0-83 58.5-141.5T480-680q83 0 141.5 58.5T680-480q0 83-58.5 141.5T480-280ZM200-440H40v-80h160v80Zm720 0H760v-80h160v80ZM440-760v-160h80v160h-80Zm0 720v-160h80v160h-80ZM256-650l-101-102 56-57 102 103-57 56Zm492 494-103-104 56-56 104 102-57 58ZM154-254l56-57 103 103-57 57-102-103Zm494-494 56-56 102 101-57 57-101-102Z\"/></svg>")
                    ),
                    new AntdUI.ContextMenuStripItem("GPS").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#4CAF50\"><path d=\"M480-80q-106 0-181-75t-75-181q0-62 27.5-117t72.5-91l156-137 156 137q45 36 72.5 91T736-336q0 106-75 181T480-80Zm0-80q72 0 124-50.5T656-336q0-44-19-83t-53-65l-104-91-104 91q-34 26-53 65t-19 83q0 75 52 125.5T480-160Zm440-320h-80q0-134-93-227t-227-93v-80q167 0 283.5 116.5T920-480Zm-160 0h-80q0-100-70-170t-170-70v-80q134 0 227 93t93 227ZM480-440Z\"/></svg>").SetSub(
                        new AntdUI.ContextMenuStripItem("Bật GPS").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#4CAF50\"><path d=\"M480-80q-106 0-181-75t-75-181q0-62 27.5-117t72.5-91l156-137 156 137q45 36 72.5 91T736-336q0 106-75 181T480-80Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Tắt GPS").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#cc0000\"><path d=\"M480-80q-106 0-181-75t-75-181q0-62 27.5-117t72.5-91l156-137 156 137q45 36 72.5 91T736-336q0 106-75 181T480-80Z\"/></svg>")
                    ),
                    new AntdUI.ContextMenuStripItem("Wifi").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#2196F3\"><path d=\"M480-120 0-600q84-84 196.5-132T480-780q123 0 235.5 48T912-600L480-120ZM480-247l337-337q-64-64-149.5-100T480-720q-95 0-180.5 36T150-584l330 337Z\"/></svg>").SetSub(
                        new AntdUI.ContextMenuStripItem("Kết nối wifi").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#2196F3\"><path d=\"M480-120 0-600q84-84 196.5-132T480-780q123 0 235.5 48T912-600L480-120Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Bật wifi").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#00aa00\"><path d=\"M480-120 0-600q84-84 196.5-132T480-780q123 0 235.5 48T912-600L480-120Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Tắt wifi").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#cc0000\"><path d=\"M480-120 0-600q84-84 196.5-132T480-780q123 0 235.5 48T912-600L480-120Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Check IP").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M480-80q-83 0-156-31.5T197-197q-54-54-85.5-127T80-480q0-83 31.5-156T197-763q54-54 127-85.5T480-880q83 0 156 31.5T763-763q54 54 85.5 127T880-480q0 83-31.5 156T763-197q-54 54-127 85.5T480-80Z\"/></svg>")
                    ),
                    new AntdUI.ContextMenuStripItem("Proxy").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#9C27B0\"><path d=\"M360-120H200q-33 0-56.5-23.5T120-200v-160q0-33 23.5-56.5T200-440h160q33 0 56.5 23.5T440-360v160q0 33-23.5 56.5T360-120Zm400 0H600q-33 0-56.5-23.5T520-200v-160q0-33 23.5-56.5T600-440h160q33 0 56.5 23.5T840-360v160q0 33-23.5 56.5T760-120Zm-200-400H400q-33 0-56.5-23.5T320-560v-200q0-33 23.5-56.5T400-840h160q33 0 56.5 23.5T640-760v200q0 33-23.5 56.5T560-520Z\"/></svg>").SetSub(
                        new AntdUI.ContextMenuStripItem("Connect Proxy").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#00aa00\"><path d=\"M360-120H200q-33 0-56.5-23.5T120-200v-160q0-33 23.5-56.5T200-440h160q33 0 56.5 23.5T440-360v160q0 33-23.5 56.5T360-120Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Disconnect Proxy").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#cc0000\"><path d=\"M360-120H200q-33 0-56.5-23.5T120-200v-160q0-33 23.5-56.5T200-440h160q33 0 56.5 23.5T440-360v160q0 33-23.5 56.5T360-120Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Check IP (Proxy)").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M480-80q-83 0-156-31.5T197-197q-54-54-85.5-127T80-480q0-83 31.5-156T197-763q54-54 127-85.5T480-880q83 0 156 31.5T763-763q54 54 85.5 127T880-480q0 83-31.5 156T763-197q-54 54-127 85.5T480-80Z\"/></svg>")
                    ),
                    new AntdUI.ContextMenuStripItem("Sim 4G").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#FF9800\"><path d=\"M280-120q-33 0-56.5-23.5T200-200v-560q0-33 23.5-56.5T280-840h280l200 200v440q0 33-23.5 56.5T680-120H280Zm0-80h400v-400H520v-160H280v560Zm200-320Z\"/></svg>").SetSub(
                        new AntdUI.ContextMenuStripItem("Bật 4G").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#00aa00\"><path d=\"M280-120q-33 0-56.5-23.5T200-200v-560q0-33 23.5-56.5T280-840h280l200 200v440q0 33-23.5 56.5T680-120H280Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Tắt 4G").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#cc0000\"><path d=\"M280-120q-33 0-56.5-23.5T200-200v-560q0-33 23.5-56.5T280-840h280l200 200v440q0 33-23.5 56.5T680-120H280Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Check IP (4G)").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M480-80q-83 0-156-31.5T197-197q-54-54-85.5-127T80-480q0-83 31.5-156T197-763q54-54 127-85.5T480-880q83 0 156 31.5T763-763q54 54 85.5 127T880-480q0 83-31.5 156T763-197q-54 54-127 85.5T480-80Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("IPv4").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M480-80q-83 0-156-31.5T197-197q-54-54-85.5-127T80-480q0-83 31.5-156T197-763q54-54 127-85.5T480-880q83 0 156 31.5T763-763q54 54 85.5 127T880-480q0 83-31.5 156T763-197q-54 54-127 85.5T480-80Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("IPv6").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M480-80q-83 0-156-31.5T197-197q-54-54-85.5-127T80-480q0-83 31.5-156T197-763q54-54 127-85.5T480-880q83 0 156 31.5T763-763q54 54 85.5 127T880-480q0 83-31.5 156T763-197q-54 54-127 85.5T480-80Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("IPv4/IPv6").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M480-80q-83 0-156-31.5T197-197q-54-54-85.5-127T80-480q0-83 31.5-156T197-763q54-54 127-85.5T480-880q83 0 156 31.5T763-763q54 54 85.5 127T880-480q0 83-31.5 156T763-197q-54 54-127 85.5T480-80Z\"/></svg>")
                    ),
                    new AntdUI.ContextMenuStripItem("Facebook").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 0 24 24\" width=\"24px\"><path fill=\"#1877F2\" d=\"M24 12.073c0-6.627-5.373-12-12-12s-12 5.373-12 12c0 5.99 4.388 10.954 10.125 11.854v-8.385H7.078v-3.47h3.047V9.43c0-3.007 1.792-4.669 4.533-4.669 1.312 0 2.686.235 2.686.235v2.953H15.83c-1.491 0-1.956.925-1.956 1.874v2.25h3.328l-.532 3.47h-2.796v8.385C19.612 23.027 24 18.062 24 12.073z\"/></svg>").SetSub(
                        new AntdUI.ContextMenuStripItem("Mở app facebook").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 0 24 24\" width=\"24px\"><path fill=\"#1877F2\" d=\"M24 12.073c0-6.627-5.373-12-12-12s-12 5.373-12 12c0 5.99 4.388 10.954 10.125 11.854v-8.385H7.078v-3.47h3.047V9.43c0-3.007 1.792-4.669 4.533-4.669 1.312 0 2.686.235 2.686.235v2.953H15.83c-1.491 0-1.956.925-1.956 1.874v2.25h3.328l-.532 3.47h-2.796v8.385C19.612 23.027 24 18.062 24 12.073z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Backup data").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M480-320 280-520l56-58 104 104v-326h80v326l104-104 56 58-200 200ZM240-160q-33 0-56.5-23.5T160-240v-120h80v120h480v-120h80v120q0 33-23.5 56.5T720-160H240Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Load backup data").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M440-320v-326L336-542l-56-58 200-200 200 200-56 58-104-104v326h-80ZM240-160q-33 0-56.5-23.5T160-240v-120h80v120h480v-120h80v120q0 33-23.5 56.5T720-160H240Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Check live").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#00aa00\"><path d=\"m424-312 282-282-56-56-226 226-114-114-56 56 170 170Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Get uid").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M480-480q-66 0-113-47t-47-113q0-66 47-113t113-47q66 0 113 47t47 113q0 66-47 113t-113 47ZM160-160v-112q0-34 17.5-62.5T224-378q62-31 126-46.5T480-440q66 0 130 15.5T736-378q29 15 46.5 43.5T800-272v112H160Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Get cookie").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M480-80q-83 0-156-31.5T197-197q-54-54-85.5-127T80-480q0-83 31.5-156T197-763q54-54 127-85.5T480-880q83 0 156 31.5T763-763q54 54 85.5 127T880-480q0 83-31.5 156T763-197q-54 54-127 85.5T480-80Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Get token").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M240-80q-33 0-56.5-23.5T160-160v-640q0-33 23.5-56.5T240-880h320l240 240v480q0 33-23.5 56.5T720-80H240Zm280-520v-200H240v640h480v-440H520ZM240-800v200-200 640-640Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Get device info").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M440-280h80v-240h-80v240Zm40-320q17 0 28.5-11.5T520-640q0-17-11.5-28.5T480-680q-17 0-28.5 11.5T440-640q0 17 11.5 28.5T480-600Zm0 520q-83 0-156-31.5T197-197q-54-54-85.5-127T80-480q0-83 31.5-156T197-763q54-54 127-85.5T480-880q83 0 156 31.5T763-763q54 54 85.5 127T880-480q0 83-31.5 156T763-197q-54 54-127 85.5T480-80Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Get info account").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M480-480q-66 0-113-47t-47-113q0-66 47-113t113-47q66 0 113 47t47 113q0 66-47 113t-113 47ZM160-160v-112q0-34 17.5-62.5T224-378q62-31 126-46.5T480-440q66 0 130 15.5T736-378q29 15 46.5 43.5T800-272v112H160Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Gỡ app facebook").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#cc0000\"><path d=\"m256-200-56-56 224-224-224-224 56-56 224 224 224-224 56 56-224 224 224 224-56 56-224-224-224 224Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Xóa dữ liệu app").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#cc0000\"><path d=\"M280-120q-33 0-56.5-23.5T200-200v-520h-40v-80h200v-40h240v40h200v80h-40v520q0 33-23.5 56.5T680-120H280Zm400-600H280v520h400v-520ZM360-280h80v-360h-80v360Zm160 0h80v-360h-80v360Z\"/></svg>")
                    ),
                    new AntdUI.ContextMenuStripItem("Tiktok").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 0 24 24\" width=\"24px\"><path fill=\"#000000\" d=\"M19.59 6.69a4.83 4.83 0 01-3.77-4.25V2h-3.45v13.67a2.89 2.89 0 01-2.88 2.5 2.89 2.89 0 01-2.89-2.89 2.89 2.89 0 012.89-2.89c.28 0 .54.04.79.1V9.01a6.27 6.27 0 00-.79-.05 6.34 6.34 0 00-6.34 6.34 6.34 6.34 0 006.34 6.34 6.34 6.34 0 006.33-6.34V8.69a8.18 8.18 0 004.79 1.52V6.75a4.85 4.85 0 01-1.02-.06z\"/></svg>").SetSub(
                        new AntdUI.ContextMenuStripItem("Mở app tiktok").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 0 24 24\" width=\"24px\"><path fill=\"#000000\" d=\"M19.59 6.69a4.83 4.83 0 01-3.77-4.25V2h-3.45v13.67a2.89 2.89 0 01-2.88 2.5 2.89 2.89 0 01-2.89-2.89 2.89 2.89 0 012.89-2.89c.28 0 .54.04.79.1V9.01a6.27 6.27 0 00-.79-.05 6.34 6.34 0 00-6.34 6.34 6.34 6.34 0 006.34 6.34 6.34 6.34 0 006.33-6.34V8.69a8.18 8.18 0 004.79 1.52V6.75a4.85 4.85 0 01-1.02-.06z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Backup data tiktok").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M480-320 280-520l56-58 104 104v-326h80v326l104-104 56 58-200 200ZM240-160q-33 0-56.5-23.5T160-240v-120h80v120h480v-120h80v120q0 33-23.5 56.5T720-160H240Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Load backup data tiktok").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M440-320v-326L336-542l-56-58 200-200 200 200-56 58-104-104v326h-80ZM240-160q-33 0-56.5-23.5T160-240v-120h80v120h480v-120h80v120q0 33-23.5 56.5T720-160H240Z\"/></svg>")
                    ),
                    new AntdUI.ContextMenuStripItem("LSPosed").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#E91E63\"><path d=\"m370-80-16-128q-13-5-24.5-12T307-235l-119 50L78-375l103-78q-1-7-1-13.5v-27q0-6.5 1-13.5L78-585l110-190 119 50q11-8 23-15t24-12l16-128h220l16 128q13 5 24.5 12T590-725l119-50 110 190-103 78q1 7 1 13.5v27q0 6.5-1 13.5l103 78-110 190-119-50q-11 8-23 15t-24 12L590-80H370Zm70-80h79l14-106q31-8 57.5-23.5T639-327l99 41 39-68-86-65q5-14 7-29.5t2-31.5q0-16-2-31.5t-7-29.5l86-65-39-68-99 42q-22-23-48.5-38.5T533-694l-13-106h-79l-14 106q-31 8-57.5 23.5T321-633l-99-41-39 68 86 64q-5 15-7 30t-2 32q0 16 2 31t7 30l-86 65 39 68 99-42q22 23 48.5 38.5T427-266l13 106Zm42-180q58 0 99-41t41-99q0-58-41-99t-99-41q-59 0-99.5 41T342-480q0 58 40.5 99t99.5 41Zm-2-140Z\"/></svg>").SetSub(
                        new AntdUI.ContextMenuStripItem("Cài app lsposed").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M480-320 280-520l56-58 104 104v-326h80v326l104-104 56 58-200 200ZM240-160q-33 0-56.5-23.5T160-240v-120h80v120h480v-120h80v120q0 33-23.5 56.5T720-160H240Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Cài module lsposed").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M480-320 280-520l56-58 104 104v-326h80v326l104-104 56 58-200 200ZM240-160q-33 0-56.5-23.5T160-240v-120h80v120h480v-120h80v120q0 33-23.5 56.5T720-160H240Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Mở module lsposed").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M200-120q-33 0-56.5-23.5T120-200v-560q0-33 23.5-56.5T200-840h560q33 0 56.5 23.5T840-760v560q0 33-23.5 56.5T760-120H200Zm0-80h560v-560H200v560Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Cấp quyền tlc helper").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#00aa00\"><path d=\"m424-312 282-282-56-56-226 226-114-114-56 56 170 170Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Tắt quyền tlc helper").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#cc0000\"><path d=\"m256-200-56-56 224-224-224-224 56-56 224 224 224-224 56 56-224 224 224 224-56 56-224-224-224 224Z\"/></svg>")
                    ),
                    new AntdUI.ContextMenuStripItem("Cấu hình TCP/IP").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#607D8B\"><path d=\"M80-160v-80h800v80H80Zm40-160v-320h80v320H120Zm160 0v-320h80v320H280Zm160 0v-320h80v320H440Zm160 0v-320h80v320H600Zm160 0v-320h80v320H760ZM80-680v-80l400-160 400 160v80H80Z\"/></svg>").SetSub(
                        new AntdUI.ContextMenuStripItem("Connect TCP/IP").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#00aa00\"><path d=\"M480-80q-83 0-156-31.5T197-197q-54-54-85.5-127T80-480q0-83 31.5-156T197-763q54-54 127-85.5T480-880q83 0 156 31.5T763-763q54 54 85.5 127T880-480q0 83-31.5 156T763-197q-54 54-127 85.5T480-80Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Disconnect TCP/IP").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#cc0000\"><path d=\"M480-80q-83 0-156-31.5T197-197q-54-54-85.5-127T80-480q0-83 31.5-156T197-763q54-54 127-85.5T480-880q83 0 156 31.5T763-763q54 54 85.5 127T880-480q0 83-31.5 156T763-197q-54 54-127 85.5T480-80Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Scan Wifi Adb TCP/IP").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#2196F3\"><path d=\"M480-120 0-600q84-84 196.5-132T480-780q123 0 235.5 48T912-600L480-120Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Remove Save Adb TCP/IP").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#cc0000\"><path d=\"M280-120q-33 0-56.5-23.5T200-200v-520h-40v-80h200v-40h240v40h200v80h-40v520q0 33-23.5 56.5T680-120H280Z\"/></svg>")
                    ),
                    new AntdUI.ContextMenuStripItem("Chức năng khác").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M480-160q-33 0-56.5-23.5T400-240q0-33 23.5-56.5T480-320q33 0 56.5 23.5T560-240q0 33-23.5 56.5T480-160Zm0-240q-33 0-56.5-23.5T400-480q0-33 23.5-56.5T480-560q33 0 56.5 23.5T560-480q0 33-23.5 56.5T480-400Zm0-240q-33 0-56.5-23.5T400-720q0-33 23.5-56.5T480-800q33 0 56.5 23.5T560-720q0 33-23.5 56.5T480-640Z\"/></svg>").SetSub(
                        new AntdUI.ContextMenuStripItem("Copy xml").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M360-240q-33 0-56.5-23.5T280-320v-480q0-33 23.5-56.5T360-880h360q33 0 56.5 23.5T800-800v480q0 33-23.5 56.5T720-240H360Zm0-80h360v-480H360v480ZM200-80q-33 0-56.5-23.5T120-160v-560h80v560h440v80H200Zm160-240v-480 480Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Test goto link").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M440-280H280q-83 0-141.5-58.5T80-480q0-83 58.5-141.5T280-680h160v80H280q-50 0-85 35t-35 85q0 50 35 85t85 35h160v80ZM320-440v-80h320v80H320Zm200 160v-80h160q50 0 85-35t35-85q0-50-35-85t-85-35H520v-80h160q83 0 141.5 58.5T880-480q0 83-58.5 141.5T680-280H520Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Test click xpath").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M480-80q-83 0-156-31.5T197-197q-54-54-85.5-127T80-480q0-83 31.5-156T197-763q54-54 127-85.5T480-880q83 0 156 31.5T763-763q54 54 85.5 127T880-480q0 83-31.5 156T763-197q-54 54-127 85.5T480-80Z\"/></svg>"),
                        new AntdUI.ContextMenuStripItem("Test change info").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M200-200h57l391-391-57-57-391 391v57Zm-80 80v-170l528-527q12-11 26.5-17t30.5-6q16 0 31 6t26 18l55 56q12 11 17.5 26t5.5 30q0 16-5.5 30.5T817-647L290-120H120Zm640-584-56-56 56 56Zm-141 85-28-29 57 57-29-28Z\"/></svg>")
                    ),
                    new AntdUI.ContextMenuStripItem("Copy debug lỗi").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M480-200q66 0 113-47t47-113v-160q0-66-47-113t-113-47q-66 0-113 47t-47 113v160q0 66 47 113t113 47Zm-80-120h160v-80H400v80Zm0-160h160v-80H400v80Zm80 40Zm0 320q-65 0-120.5-32T272-240H160v-80h84q-3-20-3.5-40t-.5-40h-80v-80h80q0-20 .5-40t3.5-40h-84v-80h112q14-23 31.5-43t40.5-35l-64-66 56-56 86 86q28-9 57-9t57 9l88-86 56 56-66 66q23 15 41.5 34.5T688-640h112v80h-84q3 20 3.5 40t.5 40h80v80h-80q0 20-.5 40t-3.5 40h84v80H688q-32 56-87.5 88T480-120Z\"/></svg>"),
                    new AntdUI.ContextMenuStripItem("Khởi động lại máy").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M480-160q-134 0-227-93t-93-227q0-134 93-227t227-93q69 0 132 28.5T720-690v-110h80v280H520v-80h168q-32-56-87.5-88T480-720q-100 0-170 70t-70 170q0 100 70 170t170 70q77 0 139-44t87-116h84q-28 106-114 173t-196 67Z\"/></svg>"),
                    new AntdUI.ContextMenuStripItem("Tắt các app đang chạy").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#cc0000\"><path d=\"m256-200-56-56 224-224-224-224 56-56 224 224 224-224 56 56-224 224 224 224-56 56-224-224-224 224Z\"/></svg>"),
                    new AntdUI.ContextMenuStripItem("Hiển thị thiết bị").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M80-160v-120h80v-440q0-33 23.5-56.5T240-800h600v80H240v440h240v120H80Zm520 0q-17 0-28.5-11.5T560-200v-360q0-17 11.5-28.5T600-600h240q17 0 28.5 11.5T880-560v360q0 17-11.5 28.5T840-160H600Zm40-80h160v-280H640v280Zm0 0h160-160Z\"/></svg>"),
                    new AntdUI.ContextMenuStripItem("Chuyển ngôn ngữ máy về tiếng việt").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" width=\"24px\" height=\"24px\" viewBox=\"0 0 64 64\"><circle cx=\"32\" cy=\"32\" r=\"30\" fill=\"#f42f4c\"/><path fill=\"#ffe62e\" d=\"M32 39l9.9 7l-3.7-11.4l9.8-7.4H35.8L32 16l-3.7 11.2H16l9.8 7.4L22.1 46z\"/></svg>"),
                    new AntdUI.ContextMenuStripItem("Chuyển ngôn ngữ máy về tiếng anh").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"24px\" height=\"24px\" viewBox=\"0 -4 28 28\" fill=\"none\"><g clip-path=\"url(#clip0_503_3486)\"><rect width=\"28\" height=\"20\" rx=\"2\" fill=\"white\"/><mask id=\"mask0_503_3486\" style=\"mask-type:alpha\" maskUnits=\"userSpaceOnUse\" x=\"0\" y=\"0\" width=\"28\" height=\"20\"><rect width=\"28\" height=\"20\" rx=\"2\" fill=\"white\"/></mask><g mask=\"url(#mask0_503_3486)\"><path fill-rule=\"evenodd\" clip-rule=\"evenodd\" d=\"M28 0H0V1.33333H28V0ZM28 2.66667H0V4H28V2.66667ZM0 5.33333H28V6.66667H0V5.33333ZM28 8H0V9.33333H28V8ZM0 10.6667H28V12H0V10.6667ZM28 13.3333H0V14.6667H28V13.3333ZM0 16H28V17.3333H0V16ZM28 18.6667H0V20H28V18.6667Z\" fill=\"#D02F44\"/><rect width=\"12\" height=\"9.33333\" fill=\"#46467F\"/></g></g></svg>"),
                    new AntdUI.ContextMenuStripItem("Sao lưu debug").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M480-200q66 0 113-47t47-113v-160q0-66-47-113t-113-47q-66 0-113 47t-47 113v160q0 66 47 113t113 47Zm-80-120h160v-80H400v80Zm0-160h160v-80H400v80Zm80 40Zm0 320q-65 0-120.5-32T272-240H160v-80h84q-3-20-3.5-40t-.5-40h-80v-80h80q0-20 .5-40t3.5-40h-84v-80h112q14-23 31.5-43t40.5-35l-64-66 56-56 86 86q28-9 57-9t57 9l88-86 56 56-66 66q23 15 41.5 34.5T688-640h112v80h-84q3 20 3.5 40t.5 40h80v80h-80q0 20-.5 40t-3.5 40h84v80H688q-32 56-87.5 88T480-120Z\"/></svg>")
                ),
                new AntdUI.ContextMenuStripItem("Xóa khỏi danh sách").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#cc0000\"><path d=\"M280-120q-33 0-56.5-23.5T200-200v-520h-40v-80h200v-40h240v40h200v80h-40v520q0 33-23.5 56.5T680-120H280Zm400-600H280v520h400v-520ZM360-280h80v-360h-80v360Zm160 0h80v-360h-80v360ZM280-720v520-520Z\"/></svg>"),
            };
        }
        private void Control_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && menulist != null)
            {

                AntdUI.ContextMenuStrip.Config config = new AntdUI.ContextMenuStrip.Config(this, RightKey, menulist);
                config.Font = FontScale.Body9Bold;

                AntdUI.ContextMenuStrip.open(config);
            }
        }
        private List<DeviceModel> GetSelectedDevices()
        {
            var list = new List<DeviceModel>();
            foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                if (row.DataBoundItem is DeviceModel d) list.Add(d);
            return list;
        }

        private static Task InstallFacebookApk(ADBClient client)
            => InstallAppApk(client, Sunny.Subdy.Common.Models.PlatformModel.Facebook);

        /// <summary>
        /// Tải + cài APK theo platform. URL/package được lấy từ FacebookHander.
        /// Lưu ý: link Instagram hiện đang dùng softonic post-download (HTML),
        /// có thể không trả APK trực tiếp; nếu fail nên thay bằng direct CDN link.
        /// </summary>
        private static async Task InstallAppApk(ADBClient client, string platform)
        {
            string package = Sunny.Subd.Core.Facebook.FacebookHander.Package(platform);
            string url = Sunny.Subd.Core.Facebook.FacebookHander.DownloadUrl(platform);
            string apkPath = Path.Combine(AppContext.BaseDirectory, "App", $"{platform}.apk");

            for (int i = 1; i <= 5; i++)
            {
                if (!File.Exists(apkPath))
                {
                    string dir = Path.GetDirectoryName(apkPath) ?? AppContext.BaseDirectory;
                    Directory.CreateDirectory(dir);
                    client.LogHelper.SUCCESS($"[{i}/5] Đang tải APK {platform}...");
                    InitHelper.GithubDown(url, apkPath);
                }
                if (!File.Exists(apkPath))
                {
                    client.LogHelper.ERROR($"Không tải được APK {platform}");
                    continue;
                }
                client.LogHelper.SUCCESS($"[{i}/5] Cài {platform}...");
                client.InstallApp(apkPath);
                if (client.AppList().Contains(package))
                {
                    client.LogHelper.SUCCESS($"Đã cài {platform}");
                    return;
                }
            }
            client.LogHelper.ERROR($"Cài {platform} thất bại sau 5 lần thử");
            await Task.CompletedTask;
        }

        private async Task RunOnSelected(Func<ADBClient, Task> action)
        {
            var tasks = new List<Task>();
            foreach (var device in GetSelectedDevices())
            {
                var d = device;
                tasks.Add(Task.Run(async () =>
                {
                    var client = new ADBClient(d);
                    try { await action(client); }
                    catch (Exception ex) { LogManager.Error(ex); client.LogHelper.ERROR(ex.Message); }
                }));
            }
            await Task.WhenAll(tasks);
        }

        private bool _rightKeyBusy;
        private async void RightKey(AntdUI.ContextMenuStripItem it)
        {
            if (_rightKeyBusy) return;
            _rightKeyBusy = true;
            try
            {
                var text = it.Text;

                // ── Chọn ──────────────────────────────────────────────────────
                if (text == "Tất cả")
                    DeviceServices.DeviceModels.ForEach(x => x.Checked = true);
                else if (text == "Bôi đen")
                {
                    DeviceServices.DeviceModels.ForEach(x => x.Checked = false);
                    foreach (var d in GetSelectedDevices()) d.Checked = true;
                }
                else if (text == "Bỏ chọn bôi đen")
                    foreach (var d in GetSelectedDevices()) d.Checked = false;
                else if (text == "Bỏ chọn tất cả")
                    DeviceServices.DeviceModels.ForEach(x => x.Checked = false);

                // ── Connect / Disconnect ───────────────────────────────────────
                else if (text == "Connect")
                {
                    var targets = GetSelectedOrCheckedDevices();
                    if (targets.Count == 0)
                    {
                        AntdHelper.NotifyWarn(this.FindForm(), "Chưa chọn thiết bị", "Vui lòng chọn hoặc bôi đen ít nhất 1 thiết bị.");
                        return;
                    }
                    foreach (var d in targets) d.Checked = true;
                    await DeviceServices.Connect();
                    ApplyFilter();
                }
                else if (text == "Disconnect")
                {
                    SetRightPanelEnabled(false);
                    try
                    {
                        await RunOnSelected(async c =>
                        {
                            try
                            {
                                int port = c.Device.Port;
                                if (port > 0)
                                {
                                    ProcessHelper.RunAdbWithTimeout(
                                        $"-s {c.Device.Serial} forward --remove tcp:{port}", 5);
                                }
                            }
                            catch (Exception ex) { c.LogHelper.ERROR($"Kill forward lỗi: {ex.Message}"); }

                            c.Device.IsLive = false;
                            c.Device.TypeColor = 1;
                            c.LogHelper.SUCCESS("Đã ngắt kết nối ATX.");
                            await Task.CompletedTask;
                        });
                        ApplyFilter();
                    }
                    finally { SetRightPanelEnabled(true); }
                }

                // ── Màn hình (top-level shortcut) ──────────────────────────────
                else if (text == "Vẽ màn hình chính")
                    await RunOnSelected(async c => { c.ATX.Press(AutoAndroid.PressKey.Home); await Task.CompletedTask; c.LogHelper.SUCCESS("Về màn hình chính."); });
                else if (text == "Mở khóa màn hình")
                    await RunOnSelected(async c => { c.ATX.ScreenOn(); await Task.CompletedTask; c.LogHelper.SUCCESS("Đã mở khóa màn hình."); });
                else if (text == "Khóa màn hình")
                    await RunOnSelected(async c => { c.Shell("input keyevent 26"); await Task.CompletedTask; c.LogHelper.SUCCESS("Đã khóa màn hình."); });
                else if (text == "Tắt màn hình")
                    await RunOnSelected(async c => { c.ATX.ScreenOff(); await Task.CompletedTask; c.LogHelper.SUCCESS("Đã tắt màn hình."); });
                else if (text == "Tắt xoay màn hình")
                    await RunOnSelected(async c => { c.Shell("content insert --uri content://settings/system --bind name:s:accelerometer_rotation --bind value:i:0"); await Task.CompletedTask; c.LogHelper.SUCCESS("Đã tắt xoay màn hình."); });
                else if (text == "Tắt tự tắt màn hình")
                    await RunOnSelected(async c => { c.Shell("settings put system screen_off_timeout 2147483647"); await Task.CompletedTask; c.LogHelper.SUCCESS("Đã tắt tự tắt màn hình."); });
                else if (text == "Chụp ảnh màn hình")
                    await RunOnSelected(async c =>
                    {
                        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                        string path = Path.Combine(desktop, $"screenshot_{c.Device.Serial}_{DateTime.Now:HHmmss}.png");
                        var bmp = c.ATX.Screenshot();
                        bmp?.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                        await Task.CompletedTask;
                        c.LogHelper.SUCCESS($"Đã lưu ảnh: {path}");
                    });
                else if (text == "Cài hình nền")
                    await RunOnSelected(async c => { c.Shell("am start -a android.intent.action.SET_WALLPAPER"); await Task.CompletedTask; c.LogHelper.SUCCESS("Đã mở cài hình nền."); });
                else if (text == "Cài hình nền phone farm")
                    await RunOnSelected(async c =>
                    {
                        var bmp = new Bitmap(1080, 1920);
                        using (var g = Graphics.FromImage(bmp))
                            g.Clear(Color.Black);
                        string tmpPath = Path.Combine(Path.GetTempPath(), $"wallpaper_{c.Device.Serial}.png");
                        bmp.Save(tmpPath, System.Drawing.Imaging.ImageFormat.Png);
                        c.Push(tmpPath, "/sdcard/wallpaper_farm.png");
                        c.Shell("am start -n com.android.launcher/com.android.launcher2.WallpaperChooser");
                        await Task.CompletedTask;
                        c.LogHelper.SUCCESS("Đã cài hình nền phone farm.");
                    });

                // ── View màn hình Android ─────────────────────────────────────
                else if (text == "View nhiều màn hình")
                {
                    var targets = GetSelectedOrCheckedDevices();
                    if (targets.Count == 0)
                    {
                        AntdUI.Message.warn(this.FindForm()!, "Chưa bôi đen thiết bị nào.", autoClose: 2);
                    }
                    else
                    {
                        string serials = string.Join(",", targets.Select(d => d.Serial));
                        LaunchViewControl(serials);
                    }
                }
                else if (text == "View 1 màn hình")
                {
                    var targets = GetSelectedOrCheckedDevices();
                    if (targets.Count == 0)
                    {
                        AntdUI.Message.warn(this.FindForm()!, "Chưa bôi đen thiết bị nào.", autoClose: 2);
                    }
                    else
                    {
                        LaunchViewControl(targets[0].Serial);
                    }
                }
                else if (text == "Đóng màn hình")
                {
                    foreach (var proc in System.Diagnostics.Process.GetProcessesByName("ViewControl"))
                    {
                        try { proc.Kill(); } catch { }
                    }
                }

                // ── Chỉnh sáng ────────────────────────────────────────────────
                else if (text == "0%")
                    await RunOnSelected(async c => { c.Shell("settings put system screen_brightness 0"); await Task.CompletedTask; c.LogHelper.SUCCESS("Độ sáng 0%."); });
                else if (text == "25%")
                    await RunOnSelected(async c => { c.Shell("settings put system screen_brightness 64"); await Task.CompletedTask; c.LogHelper.SUCCESS("Độ sáng 25%."); });
                else if (text == "50%")
                    await RunOnSelected(async c => { c.Shell("settings put system screen_brightness 128"); await Task.CompletedTask; c.LogHelper.SUCCESS("Độ sáng 50%."); });
                else if (text == "100%")
                    await RunOnSelected(async c => { c.Shell("settings put system screen_brightness 255"); await Task.CompletedTask; c.LogHelper.SUCCESS("Độ sáng 100%."); });

                // ── GPS ───────────────────────────────────────────────────────
                else if (text == "Bật GPS")
                    await RunOnSelected(async c => { c.Shell("settings put secure location_providers_allowed +gps,+network"); await Task.CompletedTask; c.LogHelper.SUCCESS("Đã bật GPS."); });
                else if (text == "Tắt GPS")
                    await RunOnSelected(async c => { c.Shell("settings put secure location_providers_allowed -gps,-network"); await Task.CompletedTask; c.LogHelper.SUCCESS("Đã tắt GPS."); });

                // ── Wifi ──────────────────────────────────────────────────────
                else if (text == "Kết nối wifi")
                {
                    var devices = GetSelectedDevices();
                    if (devices.Count == 0) devices = DeviceServices.DeviceModels?.ToList() ?? new List<DeviceModel>();
                    if (devices.Count == 0)
                    {
                        AntdUI.Message.warn(this.FindForm()!, "Không có thiết bị nào.", autoClose: 2);
                    }
                    else
                    {
                        var form = new LamToolAutoPhonePrime.Views.Forms.fDevicesWifi(devices);
                        form.ShowDialog(this.FindForm());
                    }
                }
                else if (text == "Bật wifi")
                    await RunOnSelected(async c => { await c.EnableWifi(); c.LogHelper.SUCCESS("Đã bật Wifi."); });
                else if (text == "Tắt wifi")
                    await RunOnSelected(async c => { await c.DisableWifi(); c.LogHelper.SUCCESS("Đã tắt Wifi."); });
                else if (text == "Check IP")
                    await RunOnSelected(async c => { string ip = await c.GetIp(); c.LogHelper.SUCCESS($"IP: {ip}"); });

                // ── Proxy ─────────────────────────────────────────────────────
                else if (text == "Connect Proxy")
                    await RunOnSelected(async c => { c.ConnectProxy(""); await Task.CompletedTask; c.LogHelper.SUCCESS("Đã kết nối Proxy."); });
                else if (text == "Disconnect Proxy")
                    await RunOnSelected(async c => { c.Shell("settings put global http_proxy :0"); await Task.CompletedTask; c.LogHelper.SUCCESS("Đã ngắt Proxy."); });
                else if (text == "Check IP (Proxy)")
                    await RunOnSelected(async c => { string ip = await c.GetIp(); c.LogHelper.SUCCESS($"IP: {ip}"); });

                // ── Sim 4G ────────────────────────────────────────────────────
                else if (text == "Bật 4G")
                    await RunOnSelected(async c => { c.Enabel4G(); await Task.CompletedTask; c.LogHelper.SUCCESS("Đã bật 4G."); });
                else if (text == "Tắt 4G")
                    await RunOnSelected(async c => { c.Disable4G(); await Task.CompletedTask; c.LogHelper.SUCCESS("Đã tắt 4G."); });
                else if (text == "Check IP (4G)")
                    await RunOnSelected(async c => { string ip = await c.GetIp(); c.LogHelper.SUCCESS($"IP: {ip}"); });
                else if (text == "IPv4")
                    await RunOnSelected(async c => { c.Shell("settings put global preferred_network_mode 0"); await Task.CompletedTask; c.LogHelper.SUCCESS("Đã set IPv4."); });
                else if (text == "IPv6")
                    await RunOnSelected(async c => { c.Shell("settings put global preferred_network_mode 11"); await Task.CompletedTask; c.LogHelper.SUCCESS("Đã set IPv6."); });
                else if (text == "IPv4/IPv6")
                    await RunOnSelected(async c => { c.Shell("settings put global preferred_network_mode 22"); await Task.CompletedTask; c.LogHelper.SUCCESS("Đã set IPv4/IPv6."); });

                // ── Facebook ──────────────────────────────────────────────────
                else if (text == "Mở app facebook")
                    await RunOnSelected(async c => { c.AppStart("com.facebook.katana"); await Task.CompletedTask; c.LogHelper.SUCCESS("Đã mở Facebook."); });
                else if (text == "Backup data")
                    await RunOnSelected(async c =>
                    {
                        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                        string path = Path.Combine(desktop, $"fb_backup_{c.Device.Serial}_{DateTime.Now:yyyyMMdd_HHmmss}.tar");
                        var helper = new Sunny.Subdy.Common.BackupRestoreHelper(c.Device);
                        helper.BackupFacebook(path);
                        await Task.CompletedTask;
                        c.LogHelper.SUCCESS($"Backup xong: {path}");
                    });
                else if (text == "Load backup data")
                    await RunOnSelected(async c =>
                    {
                        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                        var files = Directory.GetFiles(desktop, $"fb_backup_{c.Device.Serial}_*.tar");
                        if (files.Length == 0) { c.LogHelper.ERROR("Không tìm thấy file backup."); return; }
                        var helper = new Sunny.Subdy.Common.BackupRestoreHelper(c.Device);
                        helper.RestoreFacebook(files[^1]);
                        await Task.CompletedTask;
                        c.LogHelper.SUCCESS("Restore xong.");
                    });
                else if (text == "Check live")
                    await RunOnSelected(async c => { string ip = await c.GetIp(); c.LogHelper.SUCCESS($"Live - IP: {ip}"); });
                else if (text == "Get uid")
                    await RunOnSelected(async c => { string r = c.Shell("pm list packages -U com.facebook.katana"); await Task.CompletedTask; c.LogHelper.SUCCESS(r); });
                else if (text == "Get cookie")
                    await RunOnSelected(async c =>
                    {
                        const string pkg = "com.facebook.katana";
                        string remote = $"/data/data/{pkg}/app_webview/Default/Cookies";
                        string copy = c.Shell($"su -c 'cat {remote} 2>/dev/null | base64'");
                        if (string.IsNullOrWhiteSpace(copy) || copy.Contains("No such"))
                        {
                            c.LogHelper.ERROR("Không tìm thấy file cookies (cần root).");
                            return;
                        }
                        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                        string outPath = Path.Combine(desktop, $"fb_cookies_{c.Device.Serial}_{DateTime.Now:HHmmss}.bin");
                        try
                        {
                            File.WriteAllBytes(outPath, Convert.FromBase64String(copy.Trim()));
                            c.LogHelper.SUCCESS($"Đã lưu cookie: {outPath}");
                        }
                        catch (Exception ex) { c.LogHelper.ERROR($"Decode cookie lỗi: {ex.Message}"); }
                        await Task.CompletedTask;
                    });
                else if (text == "Get token")
                    await RunOnSelected(async c =>
                    {
                        const string pkg = "com.facebook.katana";
                        string xml = c.Shell($"su -c 'cat /data/data/{pkg}/shared_prefs/com.facebook.katana_preferences.xml 2>/dev/null'");
                        if (string.IsNullOrWhiteSpace(xml))
                        {
                            c.LogHelper.ERROR("Không đọc được prefs Facebook (cần root).");
                            return;
                        }
                        var match = System.Text.RegularExpressions.Regex.Match(xml, @"access_token[^>]*>([^<]+)<");
                        if (match.Success)
                            c.LogHelper.SUCCESS($"Token: {match.Groups[1].Value}");
                        else
                            c.LogHelper.ERROR("Không tìm thấy access_token trong prefs.");
                        await Task.CompletedTask;
                    });
                else if (text == "Get device info")
                    await RunOnSelected(async c => { string r = c.Shell("getprop"); await Task.CompletedTask; c.LogHelper.SUCCESS(r.Length > 500 ? r[..500] : r); });
                else if (text == "Get info account")
                    await RunOnSelected(async c =>
                    {
                        const string pkg = "com.facebook.katana";
                        string xml = c.Shell($"su -c 'cat /data/data/{pkg}/shared_prefs/com.facebook.katana_preferences.xml 2>/dev/null'");
                        var token = System.Text.RegularExpressions.Regex.Match(xml ?? "", @"access_token[^>]*>([^<]+)<");
                        if (!token.Success)
                        {
                            c.LogHelper.ERROR("Chưa có token để lấy info (cần root).");
                            return;
                        }
                        try
                        {
                            using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(15) };
                            string url = $"https://graph.facebook.com/me?fields=id,name,email&access_token={token.Groups[1].Value}";
                            string body = await http.GetStringAsync(url);
                            c.LogHelper.SUCCESS($"Account info: {body}");
                        }
                        catch (Exception ex) { c.LogHelper.ERROR($"Graph API lỗi: {ex.Message}"); }
                    });
                else if (text == "Gỡ app facebook")
                    await RunOnSelected(async c => { c.UninstallApp("com.facebook.katana"); await Task.CompletedTask; c.LogHelper.SUCCESS("Đã gỡ Facebook."); });
                else if (text == "Xóa dữ liệu app")
                    await RunOnSelected(async c => { c.Shell("pm clear com.facebook.katana"); await Task.CompletedTask; c.LogHelper.SUCCESS("Đã xóa dữ liệu Facebook."); });

                // ── Tiktok ────────────────────────────────────────────────────
                else if (text == "Mở app tiktok")
                    await RunOnSelected(async c => { c.AppStart("com.zhiliaoapp.musically"); await Task.CompletedTask; c.LogHelper.SUCCESS("Đã mở TikTok."); });
                else if (text == "Backup data tiktok")
                    await RunOnSelected(async c =>
                    {
                        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                        string path = Path.Combine(desktop, $"tiktok_backup_{c.Device.Serial}_{DateTime.Now:yyyyMMdd_HHmmss}.tar");
                        var helper = new Sunny.Subdy.Common.BackupRestoreHelper(c.Device);
                        await helper.BackupTikTok(path);
                        c.LogHelper.SUCCESS($"Backup TikTok xong: {path}");
                    });
                else if (text == "Load backup data tiktok")
                    await RunOnSelected(async c =>
                    {
                        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                        var files = Directory.GetFiles(desktop, $"tiktok_backup_{c.Device.Serial}_*.tar");
                        if (files.Length == 0) { c.LogHelper.ERROR("Không tìm thấy file backup TikTok."); return; }
                        var helper = new Sunny.Subdy.Common.BackupRestoreHelper(c.Device);
                        await helper.RestoreTikTok(files[^1]);
                        c.LogHelper.SUCCESS("Restore TikTok xong.");
                    });

                // ── LSPosed ───────────────────────────────────────────────────
                else if (text == "Cài app lsposed")
                {
                    var apkPaths = PromptApkFiles("Chọn APK LSPosed", multi: false);
                    if (apkPaths.Length == 0) return;
                    string apk = apkPaths[0];
                    await RunOnSelected(async c =>
                    {
                        c.LogHelper.SUCCESS($"Đang cài LSPosed: {Path.GetFileName(apk)}...");
                        bool ok = c.InstallApp(apk);
                        if (ok) c.LogHelper.SUCCESS("Đã cài LSPosed.");
                        else c.LogHelper.ERROR("Cài LSPosed thất bại.");
                        await Task.CompletedTask;
                    });
                }
                else if (text == "Cài module lsposed")
                {
                    var apkPaths = PromptApkFiles("Chọn APK module LSPosed", multi: true);
                    if (apkPaths.Length == 0) return;
                    await RunOnSelected(async c =>
                    {
                        foreach (var apk in apkPaths)
                        {
                            c.LogHelper.SUCCESS($"Đang cài module: {Path.GetFileName(apk)}...");
                            bool ok = c.InstallApp(apk);
                            if (ok) c.LogHelper.SUCCESS($"Đã cài module: {Path.GetFileName(apk)}.");
                            else c.LogHelper.ERROR($"Cài thất bại: {Path.GetFileName(apk)}");
                        }
                        await Task.CompletedTask;
                    });
                }
                else if (text == "Mở module lsposed")
                    await RunOnSelected(async c => { c.AppStart("org.lsposed.manager"); await Task.CompletedTask; c.LogHelper.SUCCESS("Đã mở LSPosed Manager."); });
                else if (text == "Cấp quyền tlc helper")
                    await RunOnSelected(async c => { c.SetEnableModuleMaxChange(); await Task.CompletedTask; c.LogHelper.SUCCESS("Đã cấp quyền TLC Helper."); });
                else if (text == "Tắt quyền tlc helper")
                    await RunOnSelected(async c =>
                    {
                        string pkg = MaxChangeService.package_MaxChange;
                        string disable = $"su -c \"sqlite3 /data/adb/lspd/config/modules_config.db 'UPDATE modules SET enabled = 0 WHERE module_pkg_name = \\\"{pkg}\\\";'\"";
                        c.Shell(disable);
                        string clearScope = $"su -c \"sqlite3 /data/adb/lspd/config/modules_config.db 'DELETE FROM scope WHERE app_pkg_name IN (\\\"{pkg}\\\", \\\"com.facebook.katana\\\", \\\"com.instagram.android\\\", \\\"ru.andr7e.deviceinfohw\\\");'\"";
                        c.Shell(clearScope);
                        c.LogHelper.SUCCESS("Đã tắt quyền TLC Helper.");
                        await Task.CompletedTask;
                    });

                // ── TCP/IP ────────────────────────────────────────────────────
                else if (text == "Connect TCP/IP")
                    await RunOnSelected(async c => { c.Connect("tcpip"); await Task.CompletedTask; c.LogHelper.SUCCESS("Đã connect TCP/IP."); });
                else if (text == "Disconnect TCP/IP")
                    await RunOnSelected(async c => { c.Shell($"adb disconnect {c.Device.Serial}"); await Task.CompletedTask; c.LogHelper.SUCCESS("Đã disconnect TCP/IP."); });
                else if (text == "Scan Wifi Adb TCP/IP")
                    await RunOnSelected(async c =>
                    {
                        const int port = 5555;
                        c.LogHelper.SUCCESS("Đang bật chế độ TCP/IP...");
                        ProcessHelper.RunAdbWithTimeout($"-s {c.Device.Serial} tcpip {port}", 10);
                        await Task.Delay(1500);

                        string ip = await c.GetIp();
                        if (string.IsNullOrWhiteSpace(ip))
                        {
                            c.LogHelper.ERROR("Không lấy được IP wifi của thiết bị.");
                            return;
                        }
                        string target = $"{ip}:{port}";
                        var result = ProcessHelper.RunAdbWithTimeout($"connect {target}", 10);
                        if (!string.IsNullOrEmpty(result) && (result.Contains("connected") || result.Contains("already")))
                            c.LogHelper.SUCCESS($"Đã connect ADB qua wifi: {target}");
                        else
                            c.LogHelper.ERROR($"Connect TCP/IP thất bại: {result}");
                    });
                else if (text == "Remove Save Adb TCP/IP")
                    await RunOnSelected(async c =>
                    {
                        string serial = c.Device.Serial;
                        if (serial.Contains(":"))
                        {
                            ProcessHelper.RunAdbWithTimeout($"disconnect {serial}", 5);
                            c.LogHelper.SUCCESS($"Đã disconnect {serial}.");
                        }
                        else
                        {
                            ProcessHelper.RunAdbWithTimeout("disconnect", 5);
                            c.LogHelper.SUCCESS("Đã disconnect tất cả TCP/IP đã lưu.");
                        }
                        await Task.CompletedTask;
                    });

                // ── Chức năng khác ────────────────────────────────────────────
                else if (text == "Copy xml")
                    await RunOnSelected(async c =>
                    {
                        string xml = c.GetXMLSource();
                        this.BeginInvoke(() => Clipboard.SetText(xml));
                        await Task.CompletedTask;
                        c.LogHelper.SUCCESS("Đã copy XML.");
                    });
                else if (text == "Test goto link")
                {
                    string? url = PromptText("Test goto link", "Nhập URL", "https://www.facebook.com");
                    if (string.IsNullOrWhiteSpace(url)) return;
                    await RunOnSelected(async c =>
                    {
                        c.Shell($"am start -a android.intent.action.VIEW -d \"{url}\"");
                        c.LogHelper.SUCCESS($"Đã mở link: {url}");
                        await Task.CompletedTask;
                    });
                }
                else if (text == "Test click xpath")
                {
                    string? xpath = PromptText("Test click xpath", "Nhập XPath", "//*[@text='OK']");
                    if (string.IsNullOrWhiteSpace(xpath)) return;
                    await RunOnSelected(async c =>
                    {
                        bool ok = c.ElementWithAttributes(xpath, timeoutInSeconds: 5, click: true);
                        if (ok) c.LogHelper.SUCCESS($"Đã click XPath: {xpath}");
                        else c.LogHelper.ERROR($"Không tìm thấy XPath: {xpath}");
                        await Task.CompletedTask;
                    });
                }
                else if (text == "Test change info")
                {
                    string? brand = PromptText("Test change info", "Nhập brand (vd: samsung)", "samsung");
                    if (string.IsNullOrWhiteSpace(brand)) return;
                    string? country = PromptText("Test change info", "Nhập country code (vd: VN)", "VN");
                    if (string.IsNullOrWhiteSpace(country)) return;
                    await RunOnSelected(async c =>
                    {
                        bool ok = c.maxChange.ChangeDeviceName(brand!, country!);
                        if (ok) c.LogHelper.SUCCESS($"Đã đổi info: {brand}/{country}");
                        else c.LogHelper.ERROR("Đổi info thất bại.");
                        await Task.CompletedTask;
                    });
                }

                // ── Nâng cao ──────────────────────────────────────────────────
                else if (text == "Cài đặt apk")
                {
                    var apkPaths = PromptApkFiles("Chọn APK để cài", multi: true);
                    if (apkPaths.Length == 0) return;
                    await RunOnSelected(async c =>
                    {
                        foreach (var apk in apkPaths)
                        {
                            c.LogHelper.SUCCESS($"Đang cài {Path.GetFileName(apk)}...");
                            bool ok = c.InstallApp(apk);
                            if (ok) c.LogHelper.SUCCESS($"Đã cài {Path.GetFileName(apk)}.");
                            else c.LogHelper.ERROR($"Cài thất bại: {Path.GetFileName(apk)}");
                        }
                        await Task.CompletedTask;
                    });
                }
                else if (text == "Tắt âm thanh")
                    await RunOnSelected(async c => { c.Shell("media volume --stream 3 --set 0"); await Task.CompletedTask; c.LogHelper.SUCCESS("Đã tắt âm thanh."); });
                else if (text == "Tắt blutooth")
                    await RunOnSelected(async c => { c.Shell("service call bluetooth_manager 8"); await Task.CompletedTask; c.LogHelper.SUCCESS("Đã tắt Bluetooth."); });
                else if (text == "Push file to sdcard")
                {
                    var files = PromptFiles("Chọn file đẩy vào /sdcard/Download/", multi: true);
                    if (files.Length == 0) return;
                    await RunOnSelected(async c =>
                    {
                        foreach (var f in files)
                        {
                            string remote = $"/sdcard/Download/{Path.GetFileName(f)}";
                            bool ok = c.Push(f, remote);
                            if (ok) c.LogHelper.SUCCESS($"Đã push: {remote}");
                            else c.LogHelper.ERROR($"Push thất bại: {f}");
                        }
                        await Task.CompletedTask;
                    });
                }
                else if (text == "Cài package cần thiết")
                {
                    string appDir = Path.Combine(AppContext.BaseDirectory, "App");
                    if (!Directory.Exists(appDir))
                    {
                        AntdHelper.NotifyWarn(this.FindForm(), "Không tìm thấy thư mục", $"Thiếu thư mục: {appDir}");
                        return;
                    }
                    var apks = Directory.GetFiles(appDir, "*.apk");
                    if (apks.Length == 0)
                    {
                        AntdHelper.NotifyWarn(this.FindForm(), "Không có APK", $"Thư mục {appDir} không có file .apk nào.");
                        return;
                    }
                    await RunOnSelected(async c =>
                    {
                        var installed = c.AppList();
                        foreach (var apk in apks)
                        {
                            c.LogHelper.SUCCESS($"Đang cài {Path.GetFileName(apk)}...");
                            bool ok = c.InstallApp(apk);
                            if (ok) c.LogHelper.SUCCESS($"Đã cài {Path.GetFileName(apk)}.");
                            else c.LogHelper.ERROR($"Cài thất bại: {Path.GetFileName(apk)}");
                        }
                        await Task.CompletedTask;
                    });
                }
                else if (text == "Cài ngôn ngữ tiếng việt")
                {
                    await RunOnSelected(async c =>
                    {
                        var svc = new ChangeLanguageService(c);
                        await svc.Change("vi", "VN");
                        await c.TurnOnADBKeyboard();
                        c.LogHelper.SUCCESS("Đã chuyển tiếng Việt.");
                    });
                }
                else if (text == "Check IP bằng browser")
                    await RunOnSelected(async c => { c.Shell("am start -a android.intent.action.VIEW -d https://api.ipify.org"); await Task.CompletedTask; c.LogHelper.SUCCESS("Đã mở browser check IP."); });
                else if (text == "Chuyển về bàn phím adb")
                    await RunOnSelected(async c => { await c.TurnOnADBKeyboard(); c.LogHelper.SUCCESS("Đã chuyển về bàn phím ADB."); });
                else if (text == "Chuyển về bàn phím thường")
                    await RunOnSelected(async c => { c.Shell("ime set com.android.inputmethod.latin/.LatinIME"); await Task.CompletedTask; c.LogHelper.SUCCESS("Đã chuyển bàn phím thường."); });
                else if (text == "Bật chế độ không làm phiền")
                    await RunOnSelected(async c => { c.Shell("cmd notification set_dnd priority"); await Task.CompletedTask; c.LogHelper.SUCCESS("Đã bật không làm phiền."); });
                else if (text == "Tắt nguồn thiết bị")
                    await RunOnSelected(async c => { c.Shell("reboot -p"); await Task.CompletedTask; c.LogHelper.SUCCESS("Đã tắt nguồn."); });

                // ── Chức năng (top-level) ──────────────────────────────────────
                else if (text == "Hiển thị thiết bị")
                {
                    var selectedDevices = GetSelectedDevices();
                    Form1 form = new Form1(selectedDevices);
                    form.Show(this);
                }
                else if (text == "Chuyển ngôn ngữ máy về tiếng việt")
                {
                    await RunOnSelected(async c =>
                    {
                        var svc = new ChangeLanguageService(c);
                        await svc.Change("vi", "VN");
                        await c.TurnOnADBKeyboard();
                        c.LogHelper.SUCCESS("Đã chuyển tiếng Việt.");
                    });
                }
                else if (text == "Chuyển ngôn ngữ máy về tiếng anh")
                {
                    await RunOnSelected(async c =>
                    {
                        var svc = new ChangeLanguageService(c);
                        await svc.Change("en", "US");
                        await c.TurnOnADBKeyboard();
                        c.LogHelper.SUCCESS("Đã chuyển tiếng Anh.");
                    });
                }

                // ── Copy debug / Reboot / Kill apps ──────────────────────────
                else if (text == "Copy debug lỗi")
                {
                    await RunOnSelected(async c =>
                    {
                        var targetColor = Color.FromArgb(2, 5, 10);
                        Point point = Point.Empty;
                        for (int i = 0; i < 15; i++)
                        {
                            var filtereds = c.FindColorCoordinates(targetColor, tolerance: 0);
                            if (!filtereds.Any()) { await Task.Delay(1000); continue; }
                            var filtered = filtereds.FindAll(r => r.Rx == 3 && (r.Ry == 2 || r.Ry == 3));
                            if (!filtered.Any()) { await Task.Delay(1000); continue; }
                            point = filtered.First().Center;
                            c.Click(point.X, point.Y);
                            break;
                        }
                        string xml = c.GetXMLSource();
                        string folderName = $"{DateTime.Now:HH-mm-ss dd.MM.yyyy} {c.Device.Serial}";
                        string tempPath = Path.Combine(Path.GetTempPath(), folderName);
                        Directory.CreateDirectory(tempPath);
                        File.WriteAllText(Path.Combine(tempPath, "xml.xml"), xml, Encoding.UTF8);
                        string imagePath = Path.Combine(tempPath, "screenshot.png");
                        AutoAndroid.FileHelper.DownImage($"{c.ATX._url}/screenshot/0", imagePath);
                        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                        string zipPath = Path.Combine(desktop, $"{folderName}.zip");
                        if (File.Exists(zipPath)) File.Delete(zipPath);
                        ZipFile.CreateFromDirectory(tempPath, zipPath);
                        Directory.Delete(tempPath, true);
                        c.LogHelper.SUCCESS("Đã lưu debug ở màn hình.");
                    });
                }
                else if (text == "Sao lưu debug")
                {
                    await RunOnSelected(async c =>
                    {
                        var targetColor = Color.FromArgb(2, 5, 10);
                        Point point = Point.Empty;
                        for (int i = 0; i < 15; i++)
                        {
                            var filtereds = c.FindColorCoordinates(targetColor, tolerance: 0);
                            if (!filtereds.Any()) { await Task.Delay(1000); continue; }
                            var filtered = filtereds.FindAll(r => r.Rx == 3 && (r.Ry == 2 || r.Ry == 3));
                            if (!filtered.Any()) { await Task.Delay(1000); continue; }
                            point = filtered.First().Center;
                            c.Click(point.X, point.Y);
                            break;
                        }
                        string xml = c.GetXMLSource();
                        string folderName = $"{DateTime.Now:HH-mm-ss dd.MM.yyyy} {c.Device.Serial}";
                        string tempPath = Path.Combine(Path.GetTempPath(), folderName);
                        Directory.CreateDirectory(tempPath);
                        File.WriteAllText(Path.Combine(tempPath, "xml.xml"), xml, Encoding.UTF8);
                        string imagePath = Path.Combine(tempPath, "screenshot.png");
                        AutoAndroid.FileHelper.DownImage($"{c.ATX._url}/screenshot/0", imagePath);
                        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                        string zipPath = Path.Combine(desktop, $"{folderName}.zip");
                        if (File.Exists(zipPath)) File.Delete(zipPath);
                        ZipFile.CreateFromDirectory(tempPath, zipPath);
                        Directory.Delete(tempPath, true);
                        c.LogHelper.SUCCESS("Đã lưu ở màn hình.");
                    });
                }
                else if (text == "Khởi động lại máy")
                    await RunOnSelected(async c => { c.RebootAndWaitForDeviceReady(); await Task.CompletedTask; c.LogHelper.SUCCESS("Đã khởi động lại."); });
                else if (text == "Tắt các app đang chạy")
                    await RunOnSelected(async c => { c.Shell("am kill-all"); await Task.CompletedTask; c.LogHelper.SUCCESS("Đã tắt các app."); });

                // ── Xóa khỏi danh sách ────────────────────────────────────────
                else if (text == "Xóa khỏi danh sách")
                {
                    foreach (var d in GetSelectedDevices())
                        DeviceServices.DeviceModels.Remove(d);
                    DeviceServices.SaveCheckedState();
                }

                DeviceServices.SaveCheckedState();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RightKey] {ex}");
            }
            finally
            {
                _rightKeyBusy = false;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (!DeviceServices.DeviceModels.Any(x => x.Checked))
            {
                AntdHelper.NotifyWarn(this.FindForm(), "Chưa chọn thiết bị", "Vui lòng chọn ít nhất 1 thiết bị để bắt đầu.");
                return;
            }
            Form parentForm = this.FindForm();
            if (parentForm != null)
            {
                parentForm.DialogResult = DialogResult.OK;
                parentForm.Close();
            }
            button2.Visible = false;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Form parentForm = this.FindForm();
            if (parentForm != null)
            {
                button2.Visible = false;
                parentForm.DialogResult = DialogResult.Cancel;
                parentForm.Close();
            }
        }

        #region ==== Tour targets ====
        public Control TourBtnScan => button1;
        public Control TourCboFilter => cboFilter;
        public Control TourBtnStart => button2;
        #endregion

        #region ==== RightKey input helpers ====
        private string[] PromptApkFiles(string title, bool multi)
        {
            using var dlg = new OpenFileDialog
            {
                Title = title,
                Filter = "Android Package (*.apk)|*.apk|Tất cả file (*.*)|*.*",
                Multiselect = multi,
                CheckFileExists = true
            };
            return dlg.ShowDialog(this.FindForm()) == DialogResult.OK ? dlg.FileNames : Array.Empty<string>();
        }

        private string[] PromptFiles(string title, bool multi)
        {
            using var dlg = new OpenFileDialog
            {
                Title = title,
                Filter = "Tất cả file (*.*)|*.*",
                Multiselect = multi,
                CheckFileExists = true
            };
            return dlg.ShowDialog(this.FindForm()) == DialogResult.OK ? dlg.FileNames : Array.Empty<string>();
        }

        private string? PromptText(string title, string label, string defaultValue = "")
        {
            string? result = null;
            var form = this.FindForm();
            if (form == null) return null;

            using var dlg = new Form
            {
                Text = title,
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MinimizeBox = false,
                MaximizeBox = false,
                ClientSize = new Size(420, 130)
            };
            var lbl = new System.Windows.Forms.Label { Text = label, Left = 12, Top = 12, AutoSize = true };
            var txt = new System.Windows.Forms.TextBox { Left = 12, Top = 38, Width = 396, Text = defaultValue };
            var ok = new System.Windows.Forms.Button { Text = "Xác nhận", Left = 232, Top = 80, Width = 80, DialogResult = DialogResult.OK };
            var cancel = new System.Windows.Forms.Button { Text = "Hủy", Left = 322, Top = 80, Width = 80, DialogResult = DialogResult.Cancel };
            dlg.Controls.AddRange(new Control[] { lbl, txt, ok, cancel });
            dlg.AcceptButton = ok;
            dlg.CancelButton = cancel;
            if (dlg.ShowDialog(form) == DialogResult.OK)
                result = txt.Text;
            return result;
        }
        #endregion
    }
}
