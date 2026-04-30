using AntdUI;
using AutoAndroid;
using LamToolAutoPhonePrime.Utils;
using LamToolAutoPhonePrime.Utils.Design;
using LamToolAutoPhonePrime.Views.Forms;
using Sunny.Subd.Core.Facebook;
using Sunny.Subd.Core.Utils;
using Sunny.Subd.Core.Models;
using Sunny.Subd.Core.Proxies;
using Sunny.Subd.Core.Services;
using Sunny.Subd.Core.Telegram;
using Sunny.Subdy.Common.API;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Json;
using Sunny.Subdy.Common.Models;
using Sunny.Subdy.Common.Services;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CommonMethod = Sunny.Subdy.Common.ControlMethod.CommonMethod;
using File = System.IO.File;

namespace LamToolAutoPhonePrime.Views.Controls
{
    public partial class ucdgvAccount : UserControl
    {
        private readonly fMain _form;
        private readonly string _platform;
        AntdUI.IContextMenuStripItem[] menulist;
        private readonly FolderContext _folderContext;
        private readonly AccountContext _accountContext;
        private readonly ConfigHelper _configHelper;
        private readonly JobHistoryContext _jobHistoryContext;
        public List<Account> _accounts;
        private SortableBindingList<Account> bindingList;
        private List<string> _folderNames = new List<string>();
        private readonly ScriptContext _scriptContext;
        private System.Windows.Forms.Timer _countsTimer;

        // Cached column indices for performance
        private int _colIndexColorType = -1;
        private int _colIndexRunning = -1;
        private int _colIndexChecked = -1;

        public ucdgvAccount(fMain form, string platform)
        {
            InitializeComponent();

            _accounts = new List<Account>();
            _scriptContext = new ScriptContext();
            bindingList = new SortableBindingList<Account>(_accounts);
            _jobHistoryContext = new JobHistoryContext();

            // Visual setup (style chung do GridStyleHelper + SsaTheme quản lý)
            dataGridView1.AutoGenerateColumns = false;
            dataGridViewCheckBoxColumn1.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCheckBoxColumn1.DataPropertyName = nameof(Account.Checked);

            _form = form;
            _platform = platform;

            LoadColumnsDataGridView();

            // Events
            dataGridView1.SelectionChanged += DataGridView_SelectionChanged;
            dataGridView1.CellFormatting += uiDataGridView1_CellFormatting;
            dataGridView1.RowPrePaint += DataGridView1_RowPrePaint;
            dataGridView1.RowTemplate.Height = Math.Max(22, dataGridView1.RowTemplate.Height);

            _folderContext = new FolderContext();
            _accountContext = new AccountContext();

            _configHelper = new Sunny.Subdy.Common.Json.ConfigHelper(
                this,
                this.Name + "_" + _platform,
                onLoad: () =>
                {
                    input6.Text = string.Empty;
                }
            );

            menulist = null;
            CreateMenuStrip();

            dataGridViewCheckBoxColumn1.Width = 40;
            dataGridViewCheckBoxColumn1.MinimumWidth = 40;
            dataGridViewCheckBoxColumn1.Resizable = DataGridViewTriState.True;

            dataGridViewTextBoxColumn1.Width = 40;
            dataGridViewTextBoxColumn1.MinimumWidth = 40;
            dataGridViewTextBoxColumn1.Resizable = DataGridViewTriState.False;
            tableLayoutPanel1.Resize += tableLayoutPanel1_Resize;

            // Empty state: delegated to SsaTheme.ApplyUcAccount (SSA styled)

            // Update checked/running counts via timer instead of CellFormatting
            _countsTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _countsTimer.Tick += (s, e) =>
            {
                var snapshot = _accounts;
                if (snapshot == null) return;
                ControlHelper.SetToolStripLabelTextSafe(toolStripLabel6, $"{snapshot.Count(x => x.Checked)}");
                ControlHelper.SetToolStripLabelTextSafe(toolStripLabel2, $"{snapshot.Count(x => x.Running)}");
            };
            _countsTimer.Start();

            // Nếu config cột chưa tồn tại (first run) → apply "Hiển thị tối ưu" làm mặc định:
            // chỉ show core (UID, Họ tên, Nhóm, Kịch bản, Trạng thái) + 5 cột tối ưu
            // (TOTAL, HÔM NAY, LẦN TƯƠNG TÁC CUỐI, XU, TÌNH TRẠNG). Sau khi user chọn qua
            // dialog "Hiển thị", config file sẽ override list này.
            // Include cả original-case lẫn UPPERCASE vì LoadConfigColums được gọi 2 lần:
            // trước SsaTheme.ApplyUcAccount (header còn original) và sau (header đã uppercase).
            var colConfigFile = $"configs\\{dataGridView1.Name}.txt";
            var hideList = new List<string> { nameof(Account.Id), nameof(Account.ColorType), nameof(Account.Running) };
            if (!System.IO.File.Exists(colConfigFile))
            {
                var hideHeaders = new[]
                {
                    "Mật khẩu", "2FA", "Cookie", "Token", "Giới tính", "Bio",
                    "Bạn bè", "Following", "Bạn bè/Following", "Page profile", "Số nhóm", "Follow",
                    "Ngày sinh", "Ngày tạo", "Avatar", "Số điện thoại", "Email", "Mật khẩu email",
                    "Mail client id", "Mail refresh token", "Email khôi phục", "User Agent",
                    "Mail Recover Pass", "Thông tin thiết bị", "Proxy", "Ghi chú",
                    "Job success", "Job fail",
                    "Thiết bị", "Token Job"
                };
                hideList.AddRange(hideHeaders);
                hideList.AddRange(hideHeaders.Select(h => h.ToUpperInvariant()));
            }
            ControlHelper.LoadConfigColums(dataGridView1, hideList);
            ForceHideInternalColumns(dataGridView1);

            // Tooltip cho các icon button quản lý nhóm
            var toolTip = new ToolTip { AutoPopDelay = 3000, InitialDelay = 300, ReshowDelay = 200 };
            toolTip.SetToolTip(button3, "Thêm nhóm");
            toolTip.SetToolTip(button2, "Đổi tên nhóm");
            toolTip.SetToolTip(button1, "Xóa nhóm");
            toolTip.SetToolTip(button17, "Hiển thị / Ẩn tài khoản");

            // Setup combobox lọc tài khoản (multi-select)
            cboFilterAccount.Items.Clear();
            cboFilterAccount.Items.AddRange(new object[]
            {
                "LIVE", "DIE", "Chưa xác định",
                "Đang chạy", "Lỗi", "Đăng xuất", "Captcha", "Bị chặn", "Đã dừng",
                "Có trạng thái", "Chưa có trạng thái",
                "Tên tiếng Việt", "Tên tiếng Anh",
                "UID đầu 6", "UID đầu 1",
                "Tương tác hôm nay", "Tương tác hôm qua", "Chưa tương tác",
            });
            cboFilterAccount.SelectedValueChanged += CboFilterAccount_SelectedValueChanged;

            GridStyleHelper.Apply(dataGridView1);

            FontUtil.ApplyFontToAllControls(this);

            // SSA visual redesign — chỉ đụng UI, không đổi business logic
            SsaTheme.ApplyUcAccount(this);

            // SsaTheme đã uppercase headers → LoadConfigColums gọi ở dòng ~127 match sai
            // (config file chứa "UID" nhưng lúc đó header còn "Uid"). Re-apply config sau
            // khi headers đã final để visibility đúng theo lựa chọn của user.
            ControlHelper.LoadConfigColums(dataGridView1, hideList);
            ForceHideInternalColumns(dataGridView1);

            // Re-apply column visibility sau khi ConfigHelper restore (Load fire sau ctor)
            // → đảm bảo grid paint lần đầu đã có đúng cấu hình cột.
            this.Load += (_, __) =>
            {
                ControlHelper.LoadConfigColums(dataGridView1, hideList);
                ForceHideInternalColumns(dataGridView1);
            };
        }

        private void tableLayoutPanel1_Resize(object sender, EventArgs e)
        {
            AdjustTableLayoutColumns();
        }

        private void LoadColumnsDataGridView()
        {
            var style = new DataGridViewCellStyle
            {
                Font = FontScale.Body9Bold,
                ForeColor = ColorPalette.Primary
            };

            dataGridViewCheckBoxColumn1.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = FontScale.Body9Bold,
                ForeColor = ColorPalette.Primary,
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            dataGridViewTextBoxColumn1.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = FontScale.Body9Bold,
                ForeColor = ColorPalette.Primary,
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            dataGridViewTextBoxColumn1.ToolTipText = "Số thứ tự trong bảng";
            dataGridViewTextBoxColumn1.DataPropertyName = nameof(Account.STT);

            var columns = new List<(string Name, string Header, string Tooltip, bool visible)>
                                                                                                                                                                                                                                                                                    {
                                                                                                                                                                                                                                                                                        (nameof(Account.Uid), nameof(Account.Uid), "Uid hoặc username tài khoản", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.FullName), "Họ và tên", "Họ và tên tài khoản", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.Password), "Mật khẩu", "Mật khẩu tài khoản", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.TowFA), "2FA", "Mã xác thực tài khoản", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.Cookie), "Cookie", "Cookie tài khoản", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.Token), "Token", "Token tài khoản", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.Gender), "Giới tính", "Giới tính tài khoản", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.Bio), "Bio", "Bio tài khoản", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.Friends), "Bạn bè/Following", "Số lượng bạn bè hoặc following", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.PagePro5), "Page profile", "Số lượng page profile", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.Groups), "Nhóm", "Số lượng nhóm", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.Follow), "Follow", "Số lượng follow", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.Birthday), "Ngày sinh", "Ngày sinh tài khoản", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.DateCreate), "Ngày tạo", "Ngày tạo tài khoản", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.Avatar), "Avatar", "Tài khoản có avatar không?", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.Phone), "Số điện thoại", "Số điện thoại tài khoản", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.Email), "Email", "Email của tài khoản", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.PassMail), "Mật khẩu email", "Mật khẩu email", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.MailClientId), "Mail client id", "Mail client id của hotmail", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.MailRefreshToken), "Mail refresh token", "Mail refresh token của hotmail", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.EmailAddress), "Email khôi phục", "Email khôi phục", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.UserAgent), "User Agent", "User Agent tài khoản", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.PassPrivateEmailAddress), "Mail Recover Pass", "Mật khẩu email khôi phục", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.DeviceInfo), "Thông tin thiết bị", "Thông tin thiết bị", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.NameFolder), "Nhóm", "Tên nhóm chứa tài khoản", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.NameScript), "Kịch bản", "Tên kịch bản", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.Proxy), "Proxy", "Proxy tài khoản", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.Note), "Ghi chú", "Ghi chú tài khoản", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.JobToday), "Hôm nay", "Số job hôm nay", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.JobTotal), "Total", "Tổng số job", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.Summary), "Job success", "Thống kê job thành công", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.Summary_Skip), "Job fail", "Thống kê job thất bại", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.RecentInteraction), "Lần tương tác cuối", "Lần tương tác cuối", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.Serial), "Thiết bị", "Thiết bị đang đăng nhập", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.TokenJob), "Token Job", "Token của server cần chạy job", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.XuToday), "Xu", "Xu kiếm được hôm nay", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.State), "Tình trạng", "Tình trạng tài khoản", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.Status), "Trạng thái", "Trạng thái tài khoản", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.Id), nameof(Account.Id), "", false),
                                                                                                                                                                                                                                                                                        (nameof(Account.ColorType), nameof(Account.ColorType), "", false),
                                                                                                                                                                                                                                                                                        (nameof(Account.Running), nameof(Account.Running), "", false)
                                                                                                                                                                                                                                                                                    };

            var colDefs = new List<DataGridViewColumn>();

            foreach (var col in columns)
            {
                // Skip certain columns for non-Facebook platforms
                if (_platform != PlatformModel.Facebook &&
                    (col.Name == nameof(Account.Token) ||
                     col.Name == nameof(Account.PagePro5) ||
                     col.Name == nameof(Account.Groups)))
                    continue;

                string header = col.Header;
                string tooltip = col.Tooltip;

                // Change header of Friends column based on platform
                if (col.Name == nameof(Account.Friends))
                {
                    header = _platform == PlatformModel.Facebook ? "Bạn bè" : "Following";
                }

                colDefs.Add(CreateColumnsDataGridView(
                    col.Name,
                    header,
                    tooltip,
                    col.visible,
                    col.Name == nameof(Account.Status) ? 300 : 100,
                    col.Name == nameof(Account.Status) ? DataGridViewAutoSizeColumnMode.Fill : DataGridViewAutoSizeColumnMode.None,
                    style
                ));
            }

            dataGridView1.Columns.AddRange(colDefs.ToArray());

            // Cache commonly used column indices to avoid repeated string lookups in hot paths
            _colIndexColorType = GetColumnIndexSafe(nameof(Account.ColorType));
            _colIndexRunning = GetColumnIndexSafe(nameof(Account.Running));
            _colIndexChecked = GetColumnIndexSafe(dataGridViewCheckBoxColumn1.Name) != -1
                ? GetColumnIndexSafe(dataGridViewCheckBoxColumn1.Name)
                : GetColumnIndexSafe(nameof(Account.Checked)); // fallback
        }

        private int GetColumnIndexSafe(string dataPropOrName)
        {
            // columns stored as "col_<dataPropertyName>"
            string colName = dataPropOrName.StartsWith("col_") ? dataPropOrName : "col_" + dataPropOrName;
            if (dataGridView1.Columns.Contains(colName))
                return dataGridView1.Columns[colName].Index;
            // fallback: try raw name
            if (dataGridView1.Columns.Contains(dataPropOrName))
                return dataGridView1.Columns[dataPropOrName].Index;
            return -1;
        }

        /// <summary>
        /// Force ẩn các cột nội bộ (Id / ColorType / Running) — chạy SAU LoadConfigColums
        /// vì helper đó dùng HeaderText để match, không bền khi header bị uppercase.
        /// </summary>
        private static void ForceHideInternalColumns(DataGridView dgv)
        {
            string[] hiddenNames = { "col_Id", "col_ColorType", "col_Running" };
            foreach (var name in hiddenNames)
            {
                var col = dgv.Columns[name];
                if (col != null) col.Visible = false;
            }
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
            column.Visible = visible;
            column.AutoSizeMode = size;
            return column;
        }

        private void AdjustTableLayoutColumns()
        {
            int columnCount = 2;
            int fixedWidth = 380;
            int totalFixedWidth = columnCount * fixedWidth;

            int extraSpace = tableLayoutPanel1.Width - totalFixedWidth;
            int spacing = Math.Max(0, extraSpace / (columnCount > 1 ? columnCount - 1 : 1));

            tableLayoutPanel1.ColumnStyles.Clear();
            tableLayoutPanel1.ColumnCount = columnCount * 2 - 1; // columns + spacers

            for (int i = 0; i < tableLayoutPanel1.ColumnCount; i++)
            {
                if (i % 2 == 0)
                {
                    tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, fixedWidth));
                }
                else
                {
                    tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, spacing));
                }
            }

            // move panels into main columns (0,2)
            tableLayoutPanel1.Controls.Clear();
            tableLayoutPanel1.Controls.Add(panel2, 0, 0);
            tableLayoutPanel1.Controls.Add(panel3, 2, 0);
        }

        public void SaveConfig()
        {
            _configHelper?.ControlClosing(null, null);
            SaveCheckedAccounts();
        }

        private void SaveCheckedAccounts()
        {
            try
            {
                if (_accounts == null || _accounts.Count == 0) return;
                _accountContext.Update(_accounts);
            }
            catch { }
        }

        private void DataGridView_SelectionChanged(object sender, EventArgs e)
        {
            if (_suppressSelectionChanged) return;
            int selectedRowCount = dataGridView1.SelectedRows.Count;
            ControlHelper.SetToolStripLabelTextSafe(toolStripLabel4, selectedRowCount.ToMoneyString());
        }

        private static readonly Color _defaultForeColor = ColorTranslator.FromHtml("#1A1A1A");
        private static readonly Color _selectionBackColor = Color.FromArgb(0, 120, 215);
        private static readonly Color _logoutForeColor = Color.FromArgb(139, 92, 246); // violet-500

        private static bool IsLogoutStatus(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            string u = s.ToUpperInvariant();
            return u.Contains("LOGOUT") || u.Contains("ĐĂNG XUẤT");
        }

        // Use RowPrePaint to style a whole row once per draw pass (much faster than per-cell)
        private void DataGridView1_RowPrePaint(object? sender, DataGridViewRowPrePaintEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0 || e.RowIndex >= dataGridView1.Rows.Count) return;
                var row = dataGridView1.Rows[e.RowIndex];
                if (row.DataBoundItem is Account acc)
                {
                    var style = row.DefaultCellStyle;

                    // Only set when value actually differs (avoids invalidate → repaint loop)
                    var expectedBack = acc.Running ? Color.Khaki : Color.White;
                    if (style.BackColor != expectedBack)
                        style.BackColor = expectedBack;

                    // Logout → tím; ColorType ưu tiên sau (Logout hiếm khi trùng ColorType)
                    bool isLogout = IsLogoutStatus(acc.State) || IsLogoutStatus(acc.Status);
                    var expectedFore = isLogout
                        ? _logoutForeColor
                        : acc.ColorType switch
                        {
                            1 => Color.Red,
                            2 => Color.Green,
                            _ => _defaultForeColor
                        };
                    if (style.ForeColor != expectedFore)
                        style.ForeColor = expectedFore;

                    if (style.SelectionBackColor != _selectionBackColor)
                        style.SelectionBackColor = _selectionBackColor;
                    if (style.SelectionForeColor != Color.White)
                        style.SelectionForeColor = Color.White;
                }
            }
            catch
            {
            }
        }

        private void uiDataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var col = dataGridView1.Columns[e.ColumnIndex];
            if (col == null) return;

            if (col.DataPropertyName == nameof(Account.STT))
            {
                e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            else if (col.DataPropertyName == nameof(Account.Password) || col.DataPropertyName == nameof(Account.TowFA))
            {
                if (e.Value != null && !string.IsNullOrEmpty(e.Value.ToString()))
                {
                    e.Value = "***";
                    e.FormattingApplied = true;
                }
            }
        }

        private async void ucdgvAccount_Load(object sender, EventArgs e)
        {
            Enable(false);
            await LoadJobService();
            await LoadFolders();
            select4_SelectedIndexChanged(null, null);
            Enable(true);
        }

        private async Task LoadFolders()
        {
            // Ghi nhớ nhóm đang chọn trước khi reload
            string previousSelected = select1.Text;

            var folders = await Task.Run(() => _folderContext.GetByType(_platform)) ?? new List<Sunny.Subdy.Data.Models.Folder>();
            _folderNames = folders.Select(f => f.Name ?? "").Where(n => n != "").ToList();
            select1.Items.Clear();
            select1.Items.Add("[ Tất cả các nhóm ]");
            if (folders.Any())
            {
                select1.Items.Add("[ Chọn theo uid ]");
                foreach (var folder in folders)
                    select1.Items.Add(folder.Name);
                select1.Items.Add("[ Chọn nhiều nhóm ]");
                select1.Items.Add("[ Tài khoản đã xóa ]");
            }

            // Cập nhật combobox lọc: thêm nhóm tài khoản
            UpdateFilterFolders(folders.Select(f => f.Name).ToList());

            // Khôi phục nhóm đã chọn trước đó (hoặc mặc định index 0)
            int restoredIndex = -1;
            if (!string.IsNullOrEmpty(previousSelected))
            {
                for (int i = 0; i < select1.Items.Count; i++)
                {
                    if (select1.Items[i]?.ToString() == previousSelected)
                    {
                        restoredIndex = i;
                        break;
                    }
                }
            }
            select1.SelectedIndex = restoredIndex >= 0 ? restoredIndex : 0;

            await LoadAccounts();
        }

        private async Task LoadJobService()
        {
            await Task.Run(() =>
            {
                var items = new List<string>();

                var typejobs = JobServices.GetTypeJobByPlatformt(_platform);
                var typejobsLower = typejobs.Select(j => j.ToLower()).ToHashSet();

                foreach (ToolStripItem item in toolStripDropDownButton1.DropDownItems)
                {
                    if (item.Name.Contains("total", StringComparison.OrdinalIgnoreCase))
                        continue;
                    var jobName = item.Name.Split('_')[0].ToLower();
                    item.Visible = typejobsLower.Contains(jobName);
                }

                // Thêm các job type mặc định vào danh sách
                items.AddRange(typejobs);

                var scripts = _scriptContext.GetByPlatform(_platform);
                if (scripts.Count > 0)
                {
                    var scriptNames = scripts
                        .Where(x => !string.IsNullOrEmpty(x.Name))
                        .Select(x => x.Name)
                        .ToList();

                    items.AddRange(scriptNames);
                }
               
            });
        }

        private void select1_SelectedIndexChanged(object sender, AntdUI.IntEventArgs e)
        {
            if (select1.SelectedIndex == -1) return;
            switch (select1.Text)
            {
                case "[ Chọn theo uid ]":
                    button1.Enabled = button2.Enabled = false;
                    var fUid = new LamToolAutoPhonePrime.Views.Forms.fSelectByUid(_platform);
                    fUid.ShowDialog();
                    if (fUid.Saved)
                        _ = LoadAccounts();
                    break;
                case "[ Chọn nhiều nhóm ]":
                    button1.Enabled = button2.Enabled = false;
                    var fMulti = new LamToolAutoPhonePrime.Views.Forms.fSelectMultiFolder(_platform, _folderNames);
                    fMulti.ShowDialog();
                    if (fMulti.Saved)
                        _ = LoadAccounts();
                    break;
                case "[ Tài khoản đã xóa ]":
                case "[ Tất cả các nhóm ]":
                    button1.Enabled = button2.Enabled = false;
                    _ = LoadAccounts();
                    break;
                default:
                    button1.Enabled = button2.Enabled = true;
                    _ = LoadAccounts();
                    break;
            }
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            await LoadFolders();
        }

        private async void button3_Click_1(object sender, EventArgs e)
        {
            fFolder f = new fFolder("AddFolder", _platform);
            f.ShowDialog();
            await LoadFolders();
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            fFolder f = new fFolder("EditFolder", select1.Text.Trim(), _platform);
            f.ShowDialog();
            await LoadFolders();
        }

        private async void button1_Click_1(object sender, EventArgs e)
        {
            if (AntdHelper.Confirm(_form, "Cảnh báo", $"Bạn có chắc chắn muốn xóa nhóm tài khoản [{select1.Text}] ?"))
            {
                var folder = _folderContext.GetByName(select1.Text, _platform);
                await Task.Run(() => _folderContext.DeleteById(folder.Id));
                await LoadFolders();
            }
        }

        private async Task LoadAccounts()
        {
            tableLayoutPanel1.Enabled = panel4.Enabled = false;
            try
            {
                // Load data in background thread
                var result = await Task.Run(() =>
                {
                    var list = new List<Account>();
                    try
                    {
                        string query = $"SELECT * FROM {nameof(Account)} WHERE {nameof(Account.Platformt)} = @platformt AND {nameof(Account.IsView)} = @isView";
                        var parameters = new Dictionary<string, object>
                        {
                            ["@platformt"] = _platform,
                            ["@isView"] = 1,
                        };

                        string namefolder = select1.Text.Trim();
                        List<string> uids = new List<string>();

                        if (namefolder == "[ Chọn theo uid ]")
                        {
                            string uidPath = Path.Combine(Path.GetTempPath(), $"uids_{_platform}.txt");
                            if (File.Exists(uidPath))
                                uids.AddRange(File.ReadLines(uidPath).Where(x => !string.IsNullOrWhiteSpace(x)));

                            if (!uids.Any()) return list;

                            query += $" AND Uid IN ({string.Join(",", uids.Select((_, i) => $"@uid{i}"))})";
                            for (int i = 0; i < uids.Count; i++)
                                parameters[$"@uid{i}"] = uids[i];
                        }
                        else if (namefolder == "[ Chọn nhiều nhóm ]")
                        {
                            string folderPath = Path.Combine(Path.GetTempPath(), $"folders_{_platform}.txt");
                            if (File.Exists(folderPath))
                            {
                                var folders = File.ReadLines(folderPath).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                                if (folders.Any())
                                {
                                    query += $" AND {nameof(Account.NameFolder)} IN ({string.Join(",", folders.Select((_, i) => $"@folder{i}"))})";
                                    for (int i = 0; i < folders.Count; i++)
                                        parameters[$"@folder{i}"] = folders[i];
                                }
                            }
                        }
                        else if (namefolder == "[ Tài khoản đã xóa ]")
                        {
                            parameters["@isView"] = 0;
                        }
                        else if (namefolder != "[ Tất cả các nhóm ]")
                        {
                            query += $" AND {nameof(Account.NameFolder)} = @folder";
                            parameters["@folder"] = namefolder;
                        }

                        var accounts = _accountContext.GetAll(query, parameters);
                        if (accounts != null)
                        {
                            // Bulk load JobToday counts from JobHistory
                            var accountUids = accounts.Select(a => a.Uid).Where(u => !string.IsNullOrEmpty(u)).Distinct();
                            var todayCounts = _jobHistoryContext.GetTodayCountsByUids(accountUids, _platform);

                            int index = 1;
                            foreach (var acc in accounts)
                            {
                                if (acc.State == "LIVE") acc.ColorType = 2;
                                else if (acc.State == "DIE" || acc.State == "CP_282" || acc.State == "CP_956") acc.ColorType = 1;
                                acc.STT = index++;

                                if (string.IsNullOrEmpty(acc.NameScript)) acc.NameScript = ScriptNames.FarmXuVip;

                                if (!string.IsNullOrEmpty(acc.Uid) && todayCounts.TryGetValue(acc.Uid, out var counts))
                                {
                                    acc.JobToday = $"{counts.success}/{counts.fail}";
                                    acc.XuToday = counts.coin > 0 ? counts.coin.ToString("0.##") : "";
                                }
                                else
                                {
                                    acc.JobToday = "0/0";
                                    acc.XuToday = "";
                                }

                                list.Add(acc);
                            }
                        }
                    }
                    catch { }
                    return list;
                });

                // Update UI
                _accounts = result;
                toolStripLabel8.Text = _accounts.Count.ToMoneyString();

                bindingList = new SortableBindingList<Account>(_accounts);
                dataGridView1.SuspendLayout();
                dataGridView1.DataSource = bindingList;
                dataGridView1.ResumeLayout();

                // Update running/checked counts once
                ControlHelper.SetToolStripLabelTextSafe(toolStripLabel2, $"{_accounts.Count(x => x.Running)}");
                ControlHelper.SetToolStripLabelTextSafe(toolStripLabel6, $"{_accounts.Count(x => x.Checked)}");

                // State counts
                var stateCounts = _accounts.GroupBy(x => x.State)
                                           .Select(g => (g.Key ?? "UNKNOWN", g.Count()))
                                           .ToList();
                toolStripLabel10.Text = toolStripLabel12.Text = toolStripLabel14.Text = "0";
                int otherCount = 0;
                foreach (var stateCount in stateCounts)
                {
                    switch (stateCount.Item1)
                    {
                        case "LIVE":
                            toolStripLabel10.Text = stateCount.Item2.ToMoneyString();
                            break;
                        case "DIE":
                            toolStripLabel12.Text = stateCount.Item2.ToMoneyString();
                            break;
                        default:
                            otherCount += stateCount.Item2;
                            break;
                    }
                }
                toolStripLabel14.Text = otherCount.ToMoneyString();

                // Tổng job hôm nay từ JobToday đã load
                int totalTodaySuccess = 0;
                foreach (var acc in _accounts)
                {
                    var parts = acc.JobToday?.Split('/');
                    if (parts != null && parts.Length >= 1 && int.TryParse(parts[0], out int s))
                        totalTodaySuccess += s;
                }
                ControlHelper.SetToolStripLabelTextSafe(toolStripLabel16, totalTodaySuccess.ToMoneyString());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi load accounts: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                tableLayoutPanel1.Enabled = panel4.Enabled = true;
            }
        }

        private async void button16_Click(object sender, EventArgs e)
        {
            fAddAccount fAdd = new fAddAccount(_platform, true);
            fAdd.ShowDialog();
            await LoadFolders();
        }

        private void button17_Click(object sender, EventArgs e)
        {
            // Exclude list dựa trên column Name ("col_<Property>") — bền với việc HeaderText bị
            // transform (uppercase / i18n). CheckBox + STT column không có pattern col_ nên check riêng.
            var excludedNames = new HashSet<string>
            {
                "col_" + nameof(Account.Uid),
                "col_" + nameof(Account.Status),
                "col_" + nameof(Account.Id),
                "col_" + nameof(Account.ColorType),
                "col_" + nameof(Account.Running)
            };

            var remainingHeaders = dataGridView1.Columns
                .Cast<DataGridViewColumn>()
                .Where(c => c != dataGridViewCheckBoxColumn1 && c != dataGridViewTextBoxColumn1)
                .Where(c => !excludedNames.Contains(c.Name))
                .Select(c => c.HeaderText)
                .ToList();
            fViewDataGridView f = new fViewDataGridView(remainingHeaders, dataGridView1.Name);
            f.ShowDialog();
            ControlHelper.LoadConfigColums(dataGridView1, new List<string> { nameof(Account.Id), nameof(Account.ColorType), nameof(Account.Running) });
            ForceHideInternalColumns(dataGridView1);
        }

        private CancellationTokenSource _searchCts;
        private volatile int _searchVersion;
        private bool _suppressSelectionChanged;
        private const int WM_SETREDRAW = 0x000B;
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        private static void SetRedraw(Control c, bool enable)
        {
            if (!c.IsHandleCreated) return;
            SendMessage(c.Handle, WM_SETREDRAW, enable ? (IntPtr)1 : IntPtr.Zero, IntPtr.Zero);
        }

        private async void input6_TextChanged(object sender, EventArgs e)
        {
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            string searchValue = input6.Text.Trim();

            if (string.IsNullOrEmpty(searchValue))
            {
                ApplySelection(Array.Empty<int>()); // clear quickly
                return;
            }

            // Increase version to discard older results
            int version = Interlocked.Increment(ref _searchVersion);

            try
            {
                // Debounce
                await Task.Delay(250, token);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            // Search in background
            List<int> matched = await Task.Run(() =>
            {
                var list = new List<int>(128);
                for (int i = 0; i < _accounts.Count; i++)
                {
                    if (token.IsCancellationRequested) return list;

                    var acc = _accounts[i];
                    if (acc.Uid?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) == true ||
                        acc.FullName?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) == true ||
                        acc.Email?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) == true ||
                        acc.PassMail?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) == true ||
                        acc.Token?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) == true ||
                        acc.Cookie?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) == true ||
                        acc.Password?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) == true ||
                        acc.EmailAddress?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) == true ||
                        acc.MailClientId?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) == true ||
                        acc.MailRefreshToken?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) == true ||
                        acc.Proxy?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) == true ||
                        acc.TokenJob?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        list.Add(i);
                    }
                }
                return list;
            }, token);

            if (token.IsCancellationRequested) return;
            if (version != _searchVersion) return; // newer results exist

            // Apply selection on UI thread with minimal redraw
            if (dataGridView1.IsHandleCreated)
            {
                dataGridView1.BeginInvoke(new Action(() =>
                {
                    if (version != _searchVersion) return;
                    ApplySelection(matched);
                }));
            }
        }

        private void ApplySelection(IReadOnlyList<int> indexes)
        {
            _suppressSelectionChanged = true;
            try
            {
                SetRedraw(dataGridView1, false);
                dataGridView1.SuspendLayout();
                dataGridView1.ClearSelection();
                const int MAX_SELECT = 1000;
                int count = 0;

                foreach (var idx in indexes)
                {
                    if (idx >= 0 && idx < dataGridView1.Rows.Count)
                    {
                        dataGridView1.Rows[idx].Selected = true;
                        if (++count >= MAX_SELECT) break;
                    }
                }
            }
            finally
            {
                toolStripLabel4.Text = dataGridView1.SelectedRows.Count.ToMoneyString();
                dataGridView1.ResumeLayout();
                SetRedraw(dataGridView1, true);
                dataGridView1.Invalidate();
                _suppressSelectionChanged = false;
            }
        }

        private async void button9_Click(object sender, EventArgs e)
        {
            await LoadAccounts();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            fSettingDefault fSetting = new fSettingDefault(_platform);
            fSetting.ShowDialog();
        }

        private async void button4_Click(object sender, EventArgs e)
        {
            //if (SubdyHelper.JobServiceByPlatform(_platform).Contains(select4.Text.Trim()))
            //{
            this.Cursor = Cursors.WaitCursor;
            fSettingJob f = new fSettingJob(_platform, "Subdy");
            this.Cursor = Cursors.Default;
            f.ShowDialog();
            //}
            //else
            //{
            //    var script = _scriptContext.GetByName(select4.Text.Trim(), _platform);
            //    if (script != null)
            //    {
            //        this.Cursor = Cursors.WaitCursor;
            //        fChiTietKichBan f = new fChiTietKichBan(script.Id);
            //        this.Cursor = Cursors.Default;
            //        f.ShowDialog();
            //        await LoadJobService();
            //    }
            //    else
            //    {
            //        CommonMethod.ShowMessageWarning("Vui lòng chọn dịch vụ hoặc kịch bản để thiết lập");
            //    }
            //}
        }

        private void Enable(bool enable)
        {
            button7.Visible = enable;
            button8.Visible = !enable;
            button8.Enabled = !enable;
            button8.Text    = "Dừng"; // reset lại sau khi stop completed
            button9.Enabled = enable;
            panel2.Enabled = enable;
            panel3.Enabled = enable;
            button16.Enabled = enable;

            // Disable thêm các control SSA khi đang chạy:
            // cboScript / select1 (Nhóm) / ssaBtnFolderMgr / cboFilterAccount / Cài đặt jobs / chung / Tương tác
            var panel4 = this.Controls.Find("panel4", true).FirstOrDefault() as AntdUI.Panel;
            if (panel4 != null)
            {
                foreach (var name in new[] { "ssaCboScript", "select1", "ssaBtnFolderMgr", "button4", "button5", "button6" })
                {
                    var c = panel4.Controls.Find(name, true).FirstOrDefault();
                    if (c != null) c.Enabled = enable;
                }
            }
            var cboFilter = this.Controls.Find("cboFilterAccount", true).FirstOrDefault();
            if (cboFilter != null) cboFilter.Enabled = enable;
        }

        private async void button7_Click(object sender, EventArgs e)
        {
            try
            {
                Enable(false);
                if (!_accounts.Any(x => x.Checked))
                {
                    AntdHelper.MsgWarn(_form, "Vui lòng chọn ít nhất 1 tài khoản để bắt đầu");
                    return;
                }
                if (!SeleceterDevice())
                {
                    return;
                }
                var model = GetConfigModel();
                if (model == null)
                {
                    return;
                }

               

                // Clear registry clients cũ trước khi start batch mới
                while (_activeClients.TryTake(out _)) { }

                FacebookFarming._data.Clear();
                Globals.ToolStripDropDownButton1 = toolStripDropDownButton1;
                Globals.JobTotal_toolStripMenuItem = JobTotal_toolStripMenuItem;
                Globals.ToolStripLabel16 = toolStripLabel16;
                fMain.StartTime = DateTime.Now;
                Globals.CancellationTokenSource = new CancellationTokenSource();
                CancellationToken ct = Globals.CancellationTokenSource.Token;
                List<Task> tasks = new List<Task>();
                AccountServices.Accounts.Clear();
                AccountServices.Accounts = _accounts.Where(x => x.Checked).ToList();
                int indexRunning = 0;
                {
                    while (_accounts.Any(x => x.Checked))
                    {
                        if (ct.IsCancellationRequested) break;
                        await XpathManagerFacebook.LoadFromApiAsync();
                        foreach (var device in DeviceServices.DeviceModels.Where(x => x.Checked))
                        {
                            if (ct.IsCancellationRequested) break;
                            tasks.Add(Task.Run(async () =>
                            {
                                await RunningThread(ct, device, model);
                            }));
                        }

                        // Chờ tất cả task — nhưng khi cancel, chỉ chờ tối đa 10s rồi abandon
                        // (các task con không respect ct sẽ tiếp tục chạy ngầm, nhưng UI không bị block)
                        try
                        {
                            var whenAll = Task.WhenAll(tasks);
                            var tcs = new TaskCompletionSource<bool>();
                            using var reg = ct.Register(() =>
                            {
                                Task.Delay(10000).ContinueWith(_ => tcs.TrySetResult(true));
                            });
                            await Task.WhenAny(whenAll, tcs.Task);
                        }
                        catch { }

                        if (ct.IsCancellationRequested) break;
                        if (model.SettingGeneral.GetBooleanValue("radioButton1", true))
                        {
                            break;
                        }
                        if (indexRunning >= model.SettingGeneral.GetIntType("numericUpDown9", 1))
                        {
                            break;
                        }
                        indexRunning++;
                        if (!await DelayAndRestartAccounts(SubdyHelper.RandomValue(model.SettingGeneral.GetIntType("numericUpDown8", 30), model.SettingGeneral.GetIntType("numericUpDown7", 30)) * 1000 * 60))
                        {
                            break;
                        }
                    }
                }
            }
            finally
            {
                Enable(true);
                fMain.StartTime = null;
            }
        }

        async Task<bool> DelayAndRestartAccounts(int delayInSeconds)
        {
            bool isCheck = true;
            var spinnerDelay = new SpinnerHelper("\t\tVui Lòng Chờ...\r\n" +
                                                "Còn {0} giây sẽ chạy lại số tài khoản đã chọn từ đầu.\r\n   Bạn có thể dừng nếu không muốn chạy lại từ đầu!", delayInSeconds);
            spinnerDelay.Dock = DockStyle.Fill;
            Controls.Add(spinnerDelay);
            spinnerDelay.BringToFront();

            await Task.Run(async () =>
            {
                while (delayInSeconds > 0)
                {
                    delayInSeconds--;
                    if (Globals.CancellationTokenSource.Token.IsCancellationRequested)
                    {
                        isCheck = false;
                        break;
                    }
                    await Task.Delay(1000);
                }
            });

            Controls.Remove(spinnerDelay);

            return isCheck;
        }

        private ConfigModel GetConfigModel(bool isreg = false)
        {
            ConfigModel model = new ConfigModel();
            if (!isreg)
            {
                model.SettingGeneral = SettingsTool.GetSettings($"{nameof(fSettingDefault)}_{_platform}", true);
               model.SettingJob = SettingsTool.GetSettings($"{nameof(fSettingJob)}_{_platform}_Subdy", true);
                model.JobService = "Subdy";
            }
            else
            {
                model.SettingGeneral = SettingsTool.GetSettings($"{nameof(fSettingRegsiner)}_{_platform}", true);
                string text = select1.Text.Trim();
                model.Accounts = bindingList;
                if (text == "[ Tất cả các nhóm ]" || text == "[ Chọn theo uid ]" || text == "[ Chọn nhiều nhóm ]" || text == "[ Tài khoản đã xóa ]")
                {
                    model.JobService = "";
                }
                else
                {
                    model.JobService = select1.Text.Trim();
                }
                int index = model.SettingGeneral.GetIntType("comboBox1", 0);
                string type = RegistrationType.RegFacebook_AllTypes[index];
                if (type == RegistrationType.Gmail || type == RegistrationType.Gmail_BaitPhoneNumber)
                {
                    string fileGmail = model.SettingGeneral.GetValuesFromInputString("txtGmail", "");
                    if (!File.Exists(fileGmail))
                    {
                        AntdHelper.MsgError(_form, "File Gmail không tồn tại");
                        return null;
                    }
                    var lines = File.ReadAllLines(fileGmail);
                    if (!lines.Any())
                    {
                        AntdHelper.MsgError(_form, "File Gmail hết gmail");
                        return null;
                    }
                    Globals.Gmails.Clear();
                    Globals.Gmails.AddRange(lines);
                }
                TelegramBotServices.BotTelegram = null;
                if (model.SettingGeneral.GetBooleanValue("checkBox6") && !string.IsNullOrEmpty(model.SettingGeneral.GetValuesFromInputString("textBox7")) && !string.IsNullOrEmpty(model.SettingGeneral.GetValuesFromInputString("textBox8")))
                {
                    TelegramBotServices.BotTelegram = new TelegramBotServices(model.SettingGeneral.GetValuesFromInputString("textBox7"));
                }
            }
            ProxyService.Proxies.Clear();
            ProxyService.Proxies.AddRange(model.SettingGeneral.GetValuesList("txtLines"));
            return model;
        }

        // Registry các ADBClient đang chạy để có thể force-stop từ button Dừng
        private readonly System.Collections.Concurrent.ConcurrentBag<ADBClient> _activeClients = new();

        private async Task RunningThread(CancellationToken ct, DeviceModel device, ConfigModel model)
        {
            ADBClient client = new ADBClient(device);
            _activeClients.Add(client);
            try
            {
                ChangeLanguageService changeLanguage = new ChangeLanguageService(client);
                await changeLanguage.Change("en", "US");
                await client.TurnOnADBKeyboard();
                MainService service = new MainService(_platform, client, model, ct);
                await service.RunAsync();
            }
            finally
            {
                try { client.AppClear(FacebookHander.Package(_platform)); } catch { }
                client.Running = false;
            }
        }

        private bool SeleceterDevice()
        {
            var uc = _form._ucDevices;

            // Save original state
            bool panelRightCollapsed = uc.splitContainer1.Panel2Collapsed;

            // Hide panelRight (thanh công cụ bên phải), chỉ hiện nút Bắt đầu sát lề phải
            uc.splitContainer1.Panel2Collapsed = true;
            uc.button2.Visible = true;
            uc.button2.Enabled = true;
            uc.button2.BringToFront();

            // Refresh DataGridView để load đúng trạng thái checkbox
            uc.dataGridView1.Refresh();

            fAddUsercontrol f = new fAddUsercontrol("SelectDevices", _platform, uc);
            f.ShowDialog();

            // Restore original state (không restore button2.Location: fAddUsercontrol
            // sẽ tự re-anchor về mép phải panel6 mỗi lần Shown, tránh lần mở thứ 2
            // button bị đẩy ngoài bounds do container có width khác).
            uc.splitContainer1.Panel2Collapsed = panelRightCollapsed;
            uc.button2.Visible = false;

            uc.Dock = DockStyle.Fill;
            _form.pContent.Controls.Add(uc);
            // Đưa tab Facebook (this) lên front để user không thấy tab Thiết bị đè lên
            this.BringToFront();
            if (f.DialogResult != DialogResult.OK)
            {
                return false;
            }
            return true;
        }

        private void CreateMenuStrip()
        {
            // SVG icons — mỗi nhóm chức năng có màu riêng
            string svgTick      = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#1890ff\"><path d=\"m424-312 282-282-56-56-226 226-114-114-56 56 170 170ZM200-120q-33 0-56.5-23.5T120-200v-560q0-33 23.5-56.5T200-840h560q33 0 56.5 23.5T840-760v560q0 33-23.5 56.5T760-120H200Zm0-80h560v-560H200v560Zm0-560v560-560Z\"/></svg>";
            string svgSelectAll = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#1890ff\"><path d=\"M268-240 42-466l57-56 170 170 56 56-57 56Zm226 0L268-466l56-57 170 170 368-368 56 57-424 424Zm0-226-57-56 198-198 57 56-198 198Z\"/></svg>";
            string svgHighlight = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#1890ff\"><path d=\"M655-200 513-342l56-56 85 85 170-170 56 57-225 226Zm0-320L513-662l56-56 85 85 170-170 56 57-225 226ZM80-280v-80h360v80H80Zm0-320v-80h360v80H80Z\"/></svg>";
            string svgUncheck   = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#8c8c8c\"><path d=\"M200-120q-33 0-56.5-23.5T120-200v-560q0-33 23.5-56.5T200-840h560q33 0 56.5 23.5T840-760v560q0 33-23.5 56.5T760-120H200Zm0-80h560v-560H200v560Z\"/></svg>";
            string svgCopy      = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#13c2c2\"><path d=\"M760-200H320q-33 0-56.5-23.5T240-280v-560q0-33 23.5-56.5T320-920h280l240 240v400q0 33-23.5 56.5T760-200ZM560-640v-200H320v560h440v-360H560ZM160-40q-33 0-56.5-23.5T80-120v-560h80v560h440v80H160Zm160-800v200-200 560-560Z\"/></svg>";
            string svgUser      = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#1890ff\"><path d=\"M480-480q-66 0-113-47t-47-113q0-66 47-113t113-47q66 0 113 47t47 113q0 66-47 113t-113 47ZM160-160v-112q0-34 17.5-62.5T224-378q62-31 126-46.5T480-440q66 0 130 15.5T736-378q29 15 46.5 43.5T800-272v112H160Zm80-80h480v-32q0-11-5.5-20T700-306q-54-27-109-40.5T480-360q-56 0-111 13.5T260-306q-9 5-14.5 14t-5.5 20v32Zm240-320q33 0 56.5-23.5T560-640q0-33-23.5-56.5T480-720q-33 0-56.5 23.5T400-640q0 33 23.5 56.5T480-560Zm0-80Zm0 400Z\"/></svg>";
            string svgPass      = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#fa8c16\"><path d=\"M80-200v-80h800v80H80Zm46-242-52-30 34-60H40v-60h68l-34-58 52-30 34 58 34-58 52 30-34 58h68v60h-68l34 60-52 30-34-60-34 60Zm320 0-52-30 34-60h-68v-60h68l-34-58 52-30 34 58 34-58 52 30-34 58h68v60h-68l34 60-52 30-34-60-34 60Zm320 0-52-30 34-60h-68v-60h68l-34-58 52-30 34 58 34-58 52 30-34 58h68v60h-68l34 60-52 30-34-60-34 60Z\"/></svg>";
            string svgLock      = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#722ed1\"><path d=\"M240-160h480v-400H240v400Zm240-120q33 0 56.5-23.5T560-360q0-33-23.5-56.5T480-440q-33 0-56.5 23.5T400-360q0 33 23.5 56.5T480-280ZM240-160v-400 400Zm0 80q-33 0-56.5-23.5T160-160v-400q0-33 23.5-56.5T240-640h280v-80q0-83 58.5-141.5T720-920q83 0 141.5 58.5T920-720h-80q0-50-35-85t-85-35q-50 0-85 35t-35 85v80h120q33 0 56.5 23.5T800-560v400q0 33-23.5 56.5T720-80H240Z\"/></svg>";
            string svgMail      = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#52c41a\"><path d=\"M160-160q-33 0-56.5-23.5T80-240v-480q0-33 23.5-56.5T160-800h640q33 0 56.5 23.5T880-720v480q0 33-23.5 56.5T800-160H160Zm320-280L160-640v400h640v-400L480-440Zm0-80 320-200H160l320 200ZM160-640v-80 480-400Z\"/></svg>";
            string svgMailPass  = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#52c41a\"><path d=\"m658-127-64-47 61-85-99-32 24-77 100 33v-105h80v105l100-33 24 77-99 32 61 85-64 47-62-85-62 85Zm-458 7q-33 0-56.5-23.5T120-200v-560q0-33 23.5-56.5T200-840h560q33 0 56.5 23.5T840-760v226q-19-9-39-14.5t-41-8.5v-203H200v360h168q9 27 30 47t47 28q-3 20-4 40.5t2 40.5q-36-7-67.5-26.5T320-320H200v120h253q7 22 16 42t22 38H200Zm0-80h253-253Zm80-410h400v-80H280v80Zm0 140h237q27-29 60.5-49t72.5-31H280v80Z\"/></svg>";
            string svgCookie    = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#d48806\"><path d=\"M480-80q-83 0-156-31.5T197-197q-54-54-85.5-127T80-480q0-75 29-147t81-128.5q52-56.5 125-91T475-881q21 0 43 2t45 7q-9 45 6 85t45 66.5q30 26.5 71.5 36.5t85.5-5q-26 59 7.5 113t99.5 56q1 11 1.5 20.5t.5 20.5q0 82-31.5 154.5t-85.5 127q-54 54.5-127 86T480-80Zm-60-480q25 0 42.5-17.5T480-620q0-25-17.5-42.5T420-680q-25 0-42.5 17.5T360-620q0 25 17.5 42.5T420-560Zm-80 200q25 0 42.5-17.5T400-420q0-25-17.5-42.5T340-480q-25 0-42.5 17.5T280-420q0 25 17.5 42.5T340-360Zm260 40q17 0 28.5-11.5T640-360q0-17-11.5-28.5T600-400q-17 0-28.5 11.5T560-360q0 17 11.5 28.5T600-320ZM480-160q122 0 216.5-84T800-458q-50-22-78.5-60T683-603q-77-11-132-66t-68-132q-80-2-140.5 29t-101 79.5Q201-644 180.5-587T160-480q0 133 93.5 226.5T480-160Zm0-324Z\"/></svg>";
            string svgToken     = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#eb2f96\"><path d=\"M480-80 120-280v-400l360-200 360 200v400L480-80ZM364-590q23-24 53-37t63-13q33 0 63 13t53 37l120-67-236-131-236 131 120 67Zm76 396v-131q-54-14-87-57t-33-98q0-11 1-20.5t4-19.5l-125-70v263l240 133Zm40-206q33 0 56.5-23.5T560-480q0-33-23.5-56.5T480-560q-33 0-56.5 23.5T400-480q0 33 23.5 56.5T480-400Zm40 206 240-133v-263l-125 70q3 10 4 19.5t1 20.5q0 55-33 98t-87 57v131Z\"/></svg>";
            string svgProxy     = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#096dd9\"><path d=\"M480-80q-139-35-229.5-159.5T160-516v-244l320-120 320 120v244q0 152-90.5 276.5T480-80Zm0-84q97-30 162-118.5T718-480H480v-315l-240 90v207q0 7 2 18h238v316Z\"/></svg>";
            string svgPhone     = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#08979c\"><path d=\"M798-120q-125 0-247-54.5T329-329Q225-433 170.5-555T116-802q0-18 12-28t28-10h150q14 0 23 10t13 25l26 128q2 13-0.5 24T359-634L259-533q26 44 55 82t64 72q37 38 78 69.5t86 55.5l95-98q10-11 23-15t25-2l119 26q15 4 24 14t9 25v145q0 16-10 28t-28 12Z\"/></svg>";
            string svgStatus    = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#595959\"><path d=\"M280-320q17 0 28.5-11.5T320-360q0-17-11.5-28.5T280-400q-17 0-28.5 11.5T240-360q0 17 11.5 28.5T280-320Zm-40-120h80v-200h-80v200Zm160 80h320v-80H400v80Zm0-160h320v-80H400v80ZM160-160q-33 0-56.5-23.5T80-240v-480q0-33 23.5-56.5T160-800h640q33 0 56.5 23.5T880-720v480q0 33-23.5 56.5T800-160H160Zm0-80h640v-480H160v480Zm0 0v-480 480Z\"/></svg>";
            string svgFilter    = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#531dab\"><path d=\"M440-160q-17 0-28.5-11.5T400-200v-240L168-736q-15-20-4.5-42t36.5-22h560q26 0 36.5 22t-4.5 42L560-440v240q0 17-11.5 28.5T520-160h-80Zm40-308 198-252H282l198 252Zm0 0Z\"/></svg>";
            string svgUpdate    = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#0050b3\"><path d=\"M467-120q-73-1-136-14t-110-34.5q-47-21.5-74-50T120-280q0 33 27 61.5t74 50Q268-147 331-134t136 14Zm-15-200q-38-2-73.5-6.5t-67.5-12q-32-7.5-60-17.5t-51-23q23 13 51 23t60 17.5q32 7.5 67.5 12T452-320Zm28-279q89 0 179-26.5T760-679q-11-29-100.5-55T480-760q-91 0-178.5 25.5T200-679q14 27 101.5 53.5T480-599Zm220 479h40v-164l72 72 28-28-120-120-120 120 28 28 72-72v164Zm20 80q-83 0-141.5-58.5T520-240q0-83 58.5-141.5T720-440q83 0 141.5 58.5T920-240q0 83-58.5 141.5T720-40ZM443-201q3 22 9 42t15 39q-73-1-136-14t-110-34.5q-47-21.5-74-50T120-280v-400q0-66 105.5-113T480-840q149 0 254.5 47T840-680v187q-19-9-39-15t-41-9v-62q-52 29-124 44t-156 15q-85 0-157-15t-123-44v101q51 47 130.5 62.5T480-400h11q-13 18-22.5 38T452-320q-76-4-141-18.5T200-379v99q7 13 30 26.5t56 24q33 10.5 73.5 18T443-201Z\"/></svg>";
            string svgTrash     = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#fa8c16\"><path d=\"M280-720v520-520Zm170 600H280q-33 0-56.5-23.5T200-200v-520h-40v-80h200v-40h240v40h200v80h-40v172q-17-5-39.5-8.5T680-560v-160H280v520h132q6 21 16 41.5t22 38.5Zm-90-160h40q0-63 20-103.5l20-40.5v-216h-80v360Zm160-230q17-11 38.5-22t41.5-16v-92h-80v130ZM680-80q-83 0-141.5-58.5T480-280q0-83 58.5-141.5T680-480q83 0 141.5 58.5T880-280q0 83-58.5 141.5T680-80Zm66-106 28-28-74-74v-112h-40v128l86 86Z\"/></svg>";
            string svgDelete    = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#cf1322\"><path d=\"m376-300 104-104 104 104 56-56-104-104 104-104-56-56-104 104-104-104-56 56 104 104-104 104 56 56Zm-96 180q-33 0-56.5-23.5T200-200v-520h-40v-80h200v-40h240v40h200v80h-40v520q0 33-23.5 56.5T680-120H280Zm400-600H280v520h400v-520Zm-400 0v520-520Z\"/></svg>";
            string svgRestore   = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#389e0d\"><path d=\"M440-320h80v-166l64 62 56-56-160-160-160 160 56 56 64-62v166ZM280-120q-33 0-56.5-23.5T200-200v-520h-40v-80h200v-40h240v40h200v80h-40v520q0 33-23.5 56.5T680-120H280Zm400-600H280v520h400v-520Zm-400 0v520-520Z\"/></svg>";
            string svgScript    = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#d4380d\"><path d=\"M320-240 80-480l240-240 57 57-184 183 184 183-57 57Zm320 0-57-57 184-183-184-183 57-57 240 240-240 240Z\"/></svg>";
            string svgGroup     = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#08979c\"><path d=\"M0-240v-63q0-43 44-70t116-27q13 0 25 .5t23 2.5q-14 21-21 44t-7 48v65H0Zm240 0v-65q0-32 17.5-58.5T307-410q32-20 76.5-30t96.5-10q53 0 97.5 10t76.5 30q32 20 49 46.5t17 58.5v65H240Zm540 0v-65q0-26-6.5-49T754-398q11-2 22.5-2.5t23.5-.5q72 0 116 26.5t44 70.5v63H780Zm-480-80h360v-6q0-37-74.5-60.5T480-410q-70 0-145 23.5T260-326v6ZM160-440q-33 0-56.5-23.5T80-520q0-34 23.5-57t56.5-23q34 0 57 23t23 57q0 33-23 56.5T160-440Zm640 0q-33 0-56.5-23.5T720-520q0-34 23.5-57t56.5-23q34 0 57 23t23 57q0 33-23 56.5T800-440Zm-320-40q-50 0-85-35t-35-85q0-51 35-85.5t85-34.5q51 0 85.5 34.5T600-600q0 50-34.5 85T480-480Zm0-80q17 0 28.5-11.5T520-600q0-17-11.5-28.5T480-640q-17 0-28.5 11.5T440-600q0 17 11.5 28.5T480-560Zm1 240Zm-1-280Z\"/></svg>";
            string svgDevice    = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#434343\"><path d=\"M300-120q-58 0-99-41t-41-99v-520q0-58 41-99t99-41h360q58 0 99 41t41 99v520q0 58-41 99t-99 41H300Zm0-80h360q25 0 42.5-17.5T720-260v-520q0-25-17.5-42.5T660-840H300q-25 0-42.5 17.5T240-780v520q0 25 17.5 42.5T300-200Zm180-100q17 0 28.5-11.5T520-340q0-17-11.5-28.5T480-380q-17 0-28.5 11.5T440-340q0 17 11.5 28.5T480-300ZM240-780v520-520Z\"/></svg>";
            string svgBackup    = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#006d75\"><path d=\"M480-80q-75 0-140.5-28.5t-114-77q-48.5-48.5-77-114T120-440h80q0 117 81.5 198.5T480-160q117 0 198.5-81.5T760-440q0-117-81.5-198.5T480-720h-6l62 62-56 58-160-160 160-160 56 58-62 62h6q75 0 140.5 28.5t114 77q48.5 48.5 77 114T840-440q0 75-28.5 140.5t-77 114q-48.5 48.5-114 77T480-80Z\"/></svg>";
            string svgHistory   = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#d46b08\"><path d=\"M480-120q-138 0-240.5-91.5T122-440h82q14 104 92.5 172T480-200q117 0 198.5-81.5T760-480q0-117-81.5-198.5T480-760q-69 0-129 32t-101 88h110v80H120v-240h80v94q51-64 124.5-99T480-840q75 0 140.5 28.5t114 77q48.5 48.5 77 114T840-480q0 75-28.5 140.5t-77 114q-48.5 48.5-114 77T480-120Zm112-192L440-464v-216h80v184l128 128-56 56Z\"/></svg>";
            string svgCheckpoint = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#ad4e00\"><path d=\"M480-80q-83 0-141.5-58.5T280-280q0-48 18.5-91t52.5-75l149-148 149 148q34 32 52.5 75t18.5 91q0 83-58.5 141.5T480-80Zm0-80q50 0 85-35t35-85q0-29-11-57t-33-49l-76-75-76 75q-22 21-33 49t-11 57q0 50 35 85t85 35ZM200-440l-56-56 240-240 160 160 164-164H600v-80h240v240h-80v-112L596-440 436-600 200-440Z\"/></svg>";
            string svgBlock     = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#a8071a\"><path d=\"M480-80q-83 0-156-31.5T197-197q-54-54-85.5-127T80-480q0-83 31.5-156T197-763q54-54 127-85.5T480-880q83 0 156 31.5T763-763q54 54 85.5 127T880-480q0 83-31.5 156T763-197q-54 54-127 85.5T480-80Zm0-80q54 0 104-17.5t92-50.5L228-660q-33 42-50.5 92T160-480q0 134 93 227t227 93Zm252-168L252-800q-42 33-59.5 83T175-616l429 429q51-18 88.5-55t55-88.5L732-328Z\"/></svg>";
            string svgLoginPhone = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#1890ff\"><path d=\"M480-120v-80h280v-560H480v-80h280q33 0 56.5 23.5T840-760v560q0 33-23.5 56.5T760-120H480Zm-80-160-57-56 103-104H120v-80h326L343-624l57-56 200 200-200 200Z\"/></svg>";
            string svgDuplicate = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#531dab\"><path d=\"M200-120q-33 0-56.5-23.5T120-200v-560q0-33 23.5-56.5T200-840h168q13-36 43.5-58t68.5-22q38 0 68.5 22t43.5 58h168q33 0 56.5 23.5T840-760v560q0 33-23.5 56.5T760-120H200Zm0-80h560v-560H200v560Zm280-560q17 0 28.5-11.5T520-800q0-17-11.5-28.5T480-840q-17 0-28.5 11.5T440-800q0 17 11.5 28.5T480-760ZM200-200v-560 560Z\"/></svg>";
            string svgName      = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#1890ff\"><path d=\"M160-160v-100l80-80v180h-80Zm160 0v-260l80-80v340h-80Zm160 0v-340l80 81v259h-80Zm160 0v-259l80-80v339h-80Zm160 0v-419l80-80v499h-80ZM160-440l280-280 160 160 200-200 80 80-280 280-160-160-280 280v-160Z\"/></svg>";
            string svgSync      = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#13c2c2\"><path d=\"M160-160v-80h110l-16-14q-52-46-73-105t-21-119q0-111 66.5-197.5T400-790v84q-72 26-116 88.5T240-478q0 45 17 87.5t53 78.5l10 10v-98h80v240H160Zm400-10v-84q72-26 116-88.5T720-482q0-45-17-87.5T650-648l-10-10v98h-80v-240h240v80H690l16 14q49 49 71.5 106.5T800-482q0 111-66.5 197.5T560-170Z\"/></svg>";

            // Load dynamic items
            var scripts = _scriptContext.GetByPlatform(_platform);
            var folders = _folderContext.GetByType(_platform);

            // Build Kịch bản submenu
            var scriptSubItems = new List<AntdUI.IContextMenuStripItem>
            {
                new AntdUI.ContextMenuStripItem("[Không cần kịch bản]").SetIcon(svgScript)
            };
            foreach (var s in (scripts ?? new List<Script>()).Where(x => !string.IsNullOrEmpty(x.Name)))
                scriptSubItems.Add(new AntdUI.ContextMenuStripItem(s.Name ?? "").SetIcon(svgScript));

            // Build Chuyển nhóm submenu
            var folderSubItems = new List<AntdUI.IContextMenuStripItem>();
            foreach (var f in (folders ?? new List<Folder>()).Where(x => !string.IsNullOrEmpty(x.Name)))
                folderSubItems.Add(new AntdUI.ContextMenuStripItem(f.Name ?? "").SetIcon(svgGroup));

            // Build Định danh submenu
            var deviceSubItems = new List<AntdUI.IContextMenuStripItem>
            {
                new AntdUI.ContextMenuStripItem("Định danh nhanh").SetIcon(svgDevice),
                new AntdUI.ContextMenuStripItem("Xóa định danh thiết bị").SetIcon(svgDelete),
            };
            foreach (var d in DeviceServices.DeviceModels.Where(x => !string.IsNullOrEmpty(x.Serial)))
                deviceSubItems.Add(new AntdUI.ContextMenuStripItem($"[ {d.Serial} ][ {d.Model} ]").SetIcon(svgDevice));

            var items = new List<AntdUI.IContextMenuStripItem>
            {
                // 1. Chọn
                new AntdUI.ContextMenuStripItem("Chọn").SetIcon(svgTick).SetSub(
                    new AntdUI.ContextMenuStripItem("Tất cả").SetIcon(svgSelectAll),
                    new AntdUI.ContextMenuStripItem("Bôi đen").SetIcon(svgHighlight),
                    new AntdUI.ContextMenuStripItem("Bỏ chọn bôi đen").SetIcon(svgUncheck),
                    new AntdUI.ContextMenuStripItem("Chọn theo tình trạng").SetIcon(svgFilter).SetSub(
                        new AntdUI.ContextMenuStripItem("LIVE").SetIcon(svgSelectAll),
                        new AntdUI.ContextMenuStripItem("DIE").SetIcon(svgSelectAll),
                        new AntdUI.ContextMenuStripItem("Chưa xác định").SetIcon(svgSelectAll),
                        new AntdUI.ContextMenuStripItem("Đang chạy").SetIcon(svgSelectAll)
                    )
                ),
                // 2. Bỏ chọn tất cả
                new AntdUI.ContextMenuStripItem("Bỏ chọn tất cả").SetIcon(svgUncheck),
                // 3. Copy
                new AntdUI.ContextMenuStripItem("Copy").SetIcon(svgCopy).SetSub(
                    new AntdUI.ContextMenuStripItem("Copy email").SetIcon(svgMail),
                    new AntdUI.ContextMenuStripItem("Copy pass").SetIcon(svgPass),
                    new AntdUI.ContextMenuStripItem("Copy code 2fa").SetIcon(svgLock),
                    new AntdUI.ContextMenuStripItem("Copy uid").SetIcon(svgUser),
                    new AntdUI.ContextMenuStripItem("Copy name").SetIcon(svgName),
                    new AntdUI.ContextMenuStripItem("Copy mail").SetIcon(svgMail),
                    new AntdUI.ContextMenuStripItem("Copy pass mail").SetIcon(svgMailPass),
                    new AntdUI.ContextMenuStripItem("Copy mail recover").SetIcon(svgMail),
                    new AntdUI.ContextMenuStripItem("Copy pass mail recover").SetIcon(svgMailPass),
                    new AntdUI.ContextMenuStripItem("Copy phone").SetIcon(svgPhone),
                    new AntdUI.ContextMenuStripItem("Copy cookie").SetIcon(svgCookie),
                    new AntdUI.ContextMenuStripItem("Copy token").SetIcon(svgToken),
                    new AntdUI.ContextMenuStripItem("Copy proxy").SetIcon(svgProxy),
                    new AntdUI.ContextMenuStripItem("Copy uid|pass").SetIcon(svgCopy),
                    new AntdUI.ContextMenuStripItem("Copy uid|pass|2fa").SetIcon(svgCopy),
                    new AntdUI.ContextMenuStripItem("Copy uid|pass|2fa|cookie").SetIcon(svgCopy),
                    new AntdUI.ContextMenuStripItem("Copy uid|pass|cookie").SetIcon(svgCopy),
                    new AntdUI.ContextMenuStripItem("Copy uid|pass|cookie|2fa").SetIcon(svgCopy),
                    new AntdUI.ContextMenuStripItem("Copy tk bị checkpoint").SetIcon(svgCheckpoint),
                    new AntdUI.ContextMenuStripItem("Copy tk bị chặn tương tác").SetIcon(svgBlock),
                    new AntdUI.ContextMenuStripItem("Copy định dạng tùy chọn").SetIcon(svgFilter)
                ),
                // 4. Chức năng
                new AntdUI.ContextMenuStripItem("Chức năng").SetIcon(svgScript).SetSub(
                    new AntdUI.ContextMenuStripItem("Check live").SetIcon(svgSelectAll),
                    new AntdUI.ContextMenuStripItem("Show password").SetIcon(svgPass),
                    new AntdUI.ContextMenuStripItem("Reset trạng thái").SetIcon(svgRestore),
                    new AntdUI.ContextMenuStripItem("Kiểm tra avatar").SetIcon(svgUser),
                    new AntdUI.ContextMenuStripItem("Kiểm tra cookie").SetIcon(svgCookie),
                    new AntdUI.ContextMenuStripItem("Tải xuống avatar").SetIcon(svgUpdate),
                    new AntdUI.ContextMenuStripItem("Copy debug lỗi").SetIcon(svgCopy),
                    new AntdUI.ContextMenuStripItem("Check name VN").SetIcon(svgName),
                    new AntdUI.ContextMenuStripItem("Kiểm tra live proxy").SetIcon(svgProxy)
                ),
                // 5. Kịch bản
                new AntdUI.ContextMenuStripItem("Kịch bản").SetIcon(svgScript).SetSub(scriptSubItems.ToArray()),
                // 6. Chuyển nhóm
                new AntdUI.ContextMenuStripItem("Chuyển nhóm").SetIcon(svgGroup).SetSub(folderSubItems.Count > 0 ? folderSubItems.ToArray() : new[] { new AntdUI.ContextMenuStripItem("(Chưa có nhóm)").SetIcon(svgGroup) }),
                // 7. Login phone
                new AntdUI.ContextMenuStripItem("Login phone").SetIcon(svgLoginPhone),
                // 8. Cập nhật dữ liệu
                new AntdUI.ContextMenuStripItem("Cập nhật dữ liệu").SetIcon(svgUpdate).SetSub(
                    new AntdUI.ContextMenuStripItem("Theo uid hoặc email").SetIcon(svgUser),
                    new AntdUI.ContextMenuStripItem("Pass").SetIcon(svgPass),
                    new AntdUI.ContextMenuStripItem("2FA").SetIcon(svgLock),
                    new AntdUI.ContextMenuStripItem("Cookie").SetIcon(svgCookie),
                    new AntdUI.ContextMenuStripItem("Proxy").SetIcon(svgProxy),
                    new AntdUI.ContextMenuStripItem("Mail").SetIcon(svgMail),
                    new AntdUI.ContextMenuStripItem("Pass mail").SetIcon(svgMailPass),
                    new AntdUI.ContextMenuStripItem("Mail recover").SetIcon(svgMail),
                    new AntdUI.ContextMenuStripItem("Pass mail recover").SetIcon(svgMailPass),
                    new AntdUI.ContextMenuStripItem("Phone").SetIcon(svgPhone),
                    new AntdUI.ContextMenuStripItem("Ngày sinh").SetIcon(svgHistory),
                    new AntdUI.ContextMenuStripItem("User agent").SetIcon(svgDevice),
                    new AntdUI.ContextMenuStripItem("Ghi chú").SetIcon(svgStatus),
                    new AntdUI.ContextMenuStripItem("Xóa Name").SetIcon(svgDelete)
                ),
                // 9. Định danh thiết bị
                new AntdUI.ContextMenuStripItem("Định danh thiết bị").SetIcon(svgDevice).SetSub(deviceSubItems.ToArray()),
                // 10. Quản lý backup profile
                new AntdUI.ContextMenuStripItem("Quản lý backup profile").SetIcon(svgBackup).SetSub(
                    new AntdUI.ContextMenuStripItem("Check backup profile").SetIcon(svgSelectAll),
                    new AntdUI.ContextMenuStripItem("Xóa backup profile").SetIcon(svgDelete),
                    new AntdUI.ContextMenuStripItem("Copy backup profile").SetIcon(svgCopy),
                    new AntdUI.ContextMenuStripItem("Check backup device").SetIcon(svgSelectAll),
                    new AntdUI.ContextMenuStripItem("Xóa backup device").SetIcon(svgDelete),
                    new AntdUI.ContextMenuStripItem("Copy backup device").SetIcon(svgCopy),
                    new AntdUI.ContextMenuStripItem("Copy backup profile và device").SetIcon(svgCopy),
                    new AntdUI.ContextMenuStripItem("Xóa backup profile và device").SetIcon(svgDelete),
                    new AntdUI.ContextMenuStripItem("Don dẹp tất cả backup dư thừa").SetIcon(svgTrash)
                ),
                // 11. Lọc trùng
                new AntdUI.ContextMenuStripItem("Lọc trùng tài khoản").SetIcon(svgDuplicate),
                // 11.5 Đồng bộ từ tool khác
                new AntdUI.ContextMenuStripItem("Đồng bộ từ tool khác").SetIcon(svgSync).SetSub(
                    new AntdUI.ContextMenuStripItem("MaxCare").SetIcon(svgSync),
                    new AntdUI.ContextMenuStripItem("FPlus").SetIcon(svgSync),
                    new AntdUI.ContextMenuStripItem("MetaMax").SetIcon(svgSync)
                ),
                // 12. Lịch sử
                new AntdUI.ContextMenuStripItem("Lịch sử hoạt động [ HOT ]").SetIcon(svgHistory),
                // 13. Xóa checkpoint
                new AntdUI.ContextMenuStripItem("Xóa tk bị checkpoint").SetIcon(svgCheckpoint),
                // 14. Xóa bị chặn
                new AntdUI.ContextMenuStripItem("Xóa tk bị chặn tương tác").SetIcon(svgBlock),
                // 15. Xóa vào thùng rác
                new AntdUI.ContextMenuStripItem("Xóa tk vào thùng rác").SetIcon(svgTrash),
                // 16. Xóa vĩnh viễn
                new AntdUI.ContextMenuStripItem("Xóa tài khoản vĩnh viễn").SetIcon(svgDelete),
            };

            menulist = items.ToArray();
        }

        private void Control_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                CreateMenuStrip();
                if (menulist == null) return;
                AntdUI.ContextMenuStrip.Config config = new AntdUI.ContextMenuStrip.Config(this, RightKey, menulist);
                config.Font = FontScale.Body9Bold;
                AntdUI.ContextMenuStrip.open(config);
            }
        }

        private void RightKey(AntdUI.ContextMenuStripItem it)
        {
            // ── Chọn ──────────────────────────────────────────────────────────
            if (it.Text.Equals("Tất cả"))
            {
                _accounts.ForEach(x => x.Checked = true);
            }
            else if (it.Text.Equals("Bôi đen"))
            {
                _accounts.ForEach(x => x.Checked = false);
                foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                    if (row.DataBoundItem is Account a) a.Checked = true;
            }
            else if (it.Text.Equals("Bỏ chọn tất cả") || it.Text.Equals("Bỏ chọn bôi đen"))
            {
                if (it.Text.Equals("Bỏ chọn tất cả"))
                    _accounts.ForEach(x => x.Checked = false);
                else
                    foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                        if (row.DataBoundItem is Account a) a.Checked = false;
            }
            // ── Chọn theo tình trạng ─────────────────────────────────────────
            else if (it.Text.Equals("LIVE"))
            {
                _accounts.ForEach(x => x.Checked = x.State?.Equals("live", StringComparison.OrdinalIgnoreCase) == true);
            }
            else if (it.Text.Equals("DIE"))
            {
                _accounts.ForEach(x => x.Checked = x.State?.Equals("die", StringComparison.OrdinalIgnoreCase) == true);
            }
            else if (it.Text.Equals("Chưa xác định"))
            {
                _accounts.ForEach(x => x.Checked = string.IsNullOrEmpty(x.State));
            }
            else if (it.Text.Equals("Đang chạy"))
            {
                _accounts.ForEach(x => x.Checked = x.Running);
            }
            // ── Copy ──────────────────────────────────────────────────────────
            else if (it.Text.Equals("Copy email"))
            {
                ConvertHelper.CopyFormat(nameof(Account.Email), dataGridView1);
            }
            else if (it.Text.Equals("Copy pass"))
            {
                if (!VerifySubdyPassword()) return;
                ConvertHelper.CopyFormat(nameof(Account.Password), dataGridView1);
            }
            else if (it.Text.Equals("Copy code 2fa"))
            {
                if (!VerifySubdyPassword()) return;
                ConvertHelper.CopyFormat(nameof(Account.TowFA), dataGridView1);
            }
            else if (it.Text.Equals("Copy uid"))
            {
                ConvertHelper.CopyFormat(nameof(Account.Uid), dataGridView1);
            }
            else if (it.Text.Equals("Copy name"))
            {
                ConvertHelper.CopyFormat(nameof(Account.FullName), dataGridView1);
            }
            else if (it.Text.Equals("Copy mail"))
            {
                ConvertHelper.CopyFormat(nameof(Account.EmailAddress), dataGridView1);
            }
            else if (it.Text.Equals("Copy pass mail"))
            {
                ConvertHelper.CopyFormat(nameof(Account.PassMail), dataGridView1);
            }
            else if (it.Text.Equals("Copy mail recover"))
            {
                ConvertHelper.CopyFormat(nameof(Account.EmailAddress), dataGridView1);
            }
            else if (it.Text.Equals("Copy pass mail recover"))
            {
                ConvertHelper.CopyFormat(nameof(Account.PassPrivateEmailAddress), dataGridView1);
            }
            else if (it.Text.Equals("Copy phone"))
            {
                ConvertHelper.CopyFormat(nameof(Account.Phone), dataGridView1);
            }
            else if (it.Text.Equals("Copy cookie"))
            {
                ConvertHelper.CopyFormat(nameof(Account.Cookie), dataGridView1);
            }
            else if (it.Text.Equals("Copy token"))
            {
                ConvertHelper.CopyFormat(nameof(Account.Token), dataGridView1);
            }
            else if (it.Text.Equals("Copy proxy"))
            {
                ConvertHelper.CopyFormat(nameof(Account.Proxy), dataGridView1);
            }
            else if (it.Text.Equals("Copy uid|pass"))
            {
                if (!VerifySubdyPassword()) return;
                ConvertHelper.CopyFormat($"{nameof(Account.Uid)}|{nameof(Account.Password)}", dataGridView1);
            }
            else if (it.Text.Equals("Copy uid|pass|2fa"))
            {
                if (!VerifySubdyPassword()) return;
                ConvertHelper.CopyFormat($"{nameof(Account.Uid)}|{nameof(Account.Password)}|{nameof(Account.TowFA)}", dataGridView1);
            }
            else if (it.Text.Equals("Copy uid|pass|2fa|cookie"))
            {
                if (!VerifySubdyPassword()) return;
                ConvertHelper.CopyFormat($"{nameof(Account.Uid)}|{nameof(Account.Password)}|{nameof(Account.TowFA)}|{nameof(Account.Cookie)}", dataGridView1);
            }
            else if (it.Text.Equals("Copy uid|pass|cookie"))
            {
                if (!VerifySubdyPassword()) return;
                ConvertHelper.CopyFormat($"{nameof(Account.Uid)}|{nameof(Account.Password)}|{nameof(Account.Cookie)}", dataGridView1);
            }
            else if (it.Text.Equals("Copy uid|pass|cookie|2fa"))
            {
                if (!VerifySubdyPassword()) return;
                ConvertHelper.CopyFormat($"{nameof(Account.Uid)}|{nameof(Account.Password)}|{nameof(Account.Cookie)}|{nameof(Account.TowFA)}", dataGridView1);
            }
            else if (it.Text.Equals("Copy tk bị checkpoint"))
            {
                var uids = _accounts.Where(x => x.State != null && x.State.StartsWith("CP", StringComparison.OrdinalIgnoreCase))
                                    .Select(x => x.Uid ?? "").Where(u => !string.IsNullOrEmpty(u)).ToList();
                if (!uids.Any()) { AntdHelper.MsgWarn(_form, "Không có tài khoản checkpoint."); return; }
                Clipboard.SetText(string.Join("\n", uids));
                AntdHelper.MsgSuccess(_form, $"Copy thành công {uids.Count} tài khoản checkpoint.");
            }
            else if (it.Text.Equals("Copy tk bị chặn tương tác"))
            {
                var uids = _accounts.Where(x => x.State?.Equals("die", StringComparison.OrdinalIgnoreCase) == true)
                                    .Select(x => x.Uid ?? "").Where(u => !string.IsNullOrEmpty(u)).ToList();
                if (!uids.Any()) { AntdHelper.MsgWarn(_form, "Không có tài khoản bị chặn tương tác."); return; }
                Clipboard.SetText(string.Join("\n", uids));
                AntdHelper.MsgSuccess(_form, $"Copy thành công {uids.Count} tài khoản bị chặn.");
            }
            else if (it.Text.Equals("Copy định dạng tùy chọn"))
            {
                ShowCopyCustomFormatDialog();
            }
            // ── Chức năng ─────────────────────────────────────────────────────
            else if (it.Text.Equals("Show password"))
            {
                if (!VerifySubdyPassword()) return;
                var selected = dataGridView1.SelectedRows.Cast<DataGridViewRow>()
                    .Select(r => r.DataBoundItem as Account).OfType<Account>().ToList();
                if (!selected.Any()) { AntdHelper.MsgWarn(_form, "Vui lòng bôi đen tài khoản cần xem."); return; }
                using var dlg = new Form
                {
                    Text = "Thông tin mật khẩu",
                    Width = 540,
                    Height = Math.Min(60 + selected.Count * 28 + 60, 600),
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    StartPosition = FormStartPosition.CenterParent,
                    MaximizeBox = false, MinimizeBox = false
                };
                var dgv = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    RowHeadersVisible = false,
                    AllowUserToAddRows = false
                };
                dgv.Columns.Add("uid", "UID/Email");
                dgv.Columns.Add("pass", "Password");
                dgv.Columns.Add("twofa", "2FA");
                foreach (var a in selected)
                    dgv.Rows.Add(a.Uid, a.Password, a.TowFA);
                dlg.Controls.Add(dgv);
                dlg.ShowDialog();
            }
            else if (it.Text.Equals("Reset trạng thái"))
            {
                var toUpdate = new List<Account>();
                foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                    if (row.DataBoundItem is Account a) { a.State = ""; toUpdate.Add(a); }
                if (!toUpdate.Any()) { AntdHelper.MsgWarn(_form, "Vui lòng bôi đen tài khoản cần reset."); return; }
                _accountContext.Update(toUpdate);
                _ = LoadAccounts();
                AntdHelper.MsgSuccess(_form, $"Đã reset trạng thái {toUpdate.Count} tài khoản.");
            }
            else if (it.Text.Equals("Copy debug lỗi"))
            {
                var targets = dataGridView1.SelectedRows.Cast<DataGridViewRow>()
                    .Select(r => r.DataBoundItem as Account).OfType<Account>()
                    .Where(a => a.Running).ToList();
                if (!targets.Any()) { AntdHelper.MsgWarn(_form, "Không có tài khoản nào vừa bôi đen vừa đang chạy."); return; }
                var sb = new System.Text.StringBuilder();
                foreach (var a in targets)
                    sb.AppendLine($"Uid:{a.Uid} | State:{a.State} | Status:{a.Status} | Result:{a.Result}");
                Clipboard.SetText(sb.ToString());
                AntdHelper.MsgSuccess(_form, $"Đã copy debug {targets.Count} tài khoản đang chạy.");
            }
            else if (it.Text.Equals("Xóa định danh thiết bị"))
            {
                var toUpdate = new List<Account>();
                foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                    if (row.DataBoundItem is Account a) { a.DeviceInfo = ""; toUpdate.Add(a); }
                if (toUpdate.Any()) { _accountContext.Update(toUpdate); _ = LoadAccounts(); }
            }
            else if (it.Text.Equals("Check live"))
            {
                _ = RunCheckLiveAsync(false);
            }
            else if (it.Text.Equals("Kiểm tra avatar"))
            {
                _ = RunCheckLiveAsync(true);
            }
            else if (it.Text.Equals("Kiểm tra cookie"))
            {
                _ = RunCheckCookieAsync();
            }
            else if (it.Text.Equals("Check name VN"))
            {
                RunCheckNameVN();
            }
            else if (it.Text.Equals("Kiểm tra live proxy"))
            {
                _ = RunCheckProxyAsync();
            }
            else if (it.Text.Equals("Tải xuống avatar") || it.Text.Equals("Định danh nhanh"))
            {
                AntdHelper.MsgWarn(_form, "Tính năng đang phát triển.");
            }
            else if (it.Text.Equals("Login phone"))
            {
                _ = LoginPhoneAsync();
            }
            // ── Quản lý backup profile ─────────────────────────────────────────
            else if (it.Text.Equals("Check backup profile"))
            {
                BackupAction(BackupType.Profile, BackupOp.Check);
            }
            else if (it.Text.Equals("Xóa backup profile"))
            {
                BackupAction(BackupType.Profile, BackupOp.Delete);
            }
            else if (it.Text.Equals("Copy backup profile"))
            {
                BackupAction(BackupType.Profile, BackupOp.Copy);
            }
            else if (it.Text.Equals("Check backup device"))
            {
                BackupAction(BackupType.Device, BackupOp.Check);
            }
            else if (it.Text.Equals("Xóa backup device"))
            {
                BackupAction(BackupType.Device, BackupOp.Delete);
            }
            else if (it.Text.Equals("Copy backup device"))
            {
                BackupAction(BackupType.Device, BackupOp.Copy);
            }
            else if (it.Text.Equals("Copy backup profile và device"))
            {
                BackupAction(BackupType.Both, BackupOp.Copy);
            }
            else if (it.Text.Equals("Xóa backup profile và device"))
            {
                BackupAction(BackupType.Both, BackupOp.Delete);
            }
            else if (it.Text.Equals("Don dẹp tất cả backup dư thừa"))
            {
                CleanupRedundantBackups();
            }
            // ── Kịch bản ──────────────────────────────────────────────────────
            else if (it.Text.Equals("[Không cần kịch bản]"))
            {
                var toUpdate = new List<Account>();
                foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                    if (row.DataBoundItem is Account a) { a.NameScript = ""; toUpdate.Add(a); }
                if (toUpdate.Any()) _accountContext.Update(toUpdate);
            }
            // ── Cập nhật dữ liệu ──────────────────────────────────────────────
            else if (it.Text.Equals("Theo uid hoặc email"))
            {
                fAddAccount fAdd = new fAddAccount(_platform, false);
                fAdd.ShowDialog();
                _ = LoadAccounts();
            }
            else if (it.Text.Equals("Proxy"))
            {
                var ids = new List<Guid>();
                foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                    if (row.DataBoundItem is Account a) ids.Add(a.Id);
                if (!ids.Any()) { AntdHelper.MsgWarn(_form, "Vui lòng select dòng cần cập nhật."); return; }
                new fImportProxy(ids).ShowDialog();
                _ = LoadAccounts();
            }
            else if (it.Text.Equals("Xóa Name"))
            {
                var toUpdate = new List<Account>();
                foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                    if (row.DataBoundItem is Account a) { a.FullName = ""; toUpdate.Add(a); }
                if (!toUpdate.Any()) { AntdHelper.MsgWarn(_form, "Vui lòng select dòng cần xóa."); return; }
                if (!AntdHelper.Confirm(_form, "Xác nhận", $"Xóa Name của {toUpdate.Count} tài khoản?")) return;
                _accountContext.Update(toUpdate);
                _ = LoadAccounts();
            }
            else if (it.Text.Equals("Pass") || it.Text.Equals("2FA") ||
                     it.Text.Equals("Cookie") || it.Text.Equals("Mail") || it.Text.Equals("Pass mail") ||
                     it.Text.Equals("Mail recover") || it.Text.Equals("Pass mail recover") ||
                     it.Text.Equals("Phone") || it.Text.Equals("Ngày sinh") || it.Text.Equals("User agent") ||
                     it.Text.Equals("Ghi chú"))
            {
                ShowUpdateFieldPopup(it.Text);
            }
            // ── Lọc trùng ─────────────────────────────────────────────────────
            else if (it.Text.Equals("Lọc trùng tài khoản"))
            {
                FilterDuplicateAccounts();
            }
            // ── Đồng bộ từ tool khác ──────────────────────────────────────────
            else if (it.Text.Equals("MaxCare") || it.Text.Equals("FPlus") || it.Text.Equals("MetaMax"))
            {
                OpenSyncFromOtherTool(it.Text);
            }
            // ── Lịch sử ───────────────────────────────────────────────────────
            else if (it.Text.Equals("Lịch sử hoạt động [ HOT ]"))
            {
                AntdHelper.MsgWarn(_form, "Tính năng đang phát triển.");
            }
            // ── Xóa theo tình trạng ───────────────────────────────────────────
            else if (it.Text.Equals("Xóa tk bị checkpoint"))
            {
                TrashCheckpointAccounts();
            }
            else if (it.Text.Equals("Xóa tk bị chặn tương tác"))
            {
                TrashBlockedAccounts();
            }
            // ── Xóa / Khôi phục ───────────────────────────────────────────────
            else if (it.Text.Equals("Xóa tk vào thùng rác") || it.Text.Equals("Xóa tài khoản vào thùng rác"))
            {
                TrashSelectedAccounts();
            }
            else if (it.Text.Equals("Xóa tài khoản vĩnh viễn"))
            {
                DeleteAccountsPermanently();
            }
            else if (it.Text.Equals("Khôi phục về nhóm cũ"))
            {
                var ids = new List<Guid>();
                foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                    if (row.DataBoundItem is Account a && !a.IsView) ids.Add(a.Id);
                if (!ids.Any()) { AntdHelper.MsgWarn(_form, "Vui lòng select dòng cần khôi phục tài khoản."); return; }
                if (!AntdHelper.Confirm(_form, "Xác nhận", $"Bạn có chắc chắn muốn khôi phục {ids.Count} tài khoản?")) return;
                if (_accountContext.UpdateIsViewTrue(ids)) AntdHelper.MsgSuccess(_form, "Đã khôi phục thành công.");
                else AntdHelper.MsgError(_form, "Đã xảy ra lỗi.");
                _ = LoadAccounts();
            }
            else if (it.Text.Equals("Cập nhật token job"))
            {
                var ids = new List<string>();
                foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                    if (row.DataBoundItem is Account a) ids.Add(a.Id.ToString());
                if (!ids.Any()) { AntdHelper.MsgWarn(_form, "Vui lòng select dòng cần cập nhật."); return; }
                new fUpdateData(ids, fUpdateData.TokenJob, _platform).ShowDialog();
                _ = LoadAccounts();
            }
            else
            {
                // Dynamic: Chuyển nhóm
                var folders = _folderContext.GetByType(_platform) ?? new List<Folder>();
                var matchFolder = folders.FirstOrDefault(f => f.Name == it.Text);
                if (matchFolder != null)
                {
                    string folderName = matchFolder.Name ?? "";
                    var toUpdate = new List<Account>();
                    foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                        if (row.DataBoundItem is Account a) toUpdate.Add(a);
                    if (!toUpdate.Any())
                    {
                        AntdHelper.MsgWarn(_form, "Vui lòng bôi đen tài khoản cần chuyển nhóm.");
                        return;
                    }
                    if (!AntdHelper.Confirm(_form, "Xác nhận chuyển nhóm",
                        $"Bạn có chắc chắn muốn chuyển {toUpdate.Count} tài khoản bôi đen sang nhóm [{folderName}]?"))
                        return;
                    foreach (var a in toUpdate) a.NameFolder = folderName;
                    _accountContext.Update(toUpdate);
                    AntdHelper.MsgSuccess(_form, $"Đã chuyển {toUpdate.Count} tài khoản sang nhóm [{folderName}].");
                    _ = LoadAccounts();
                    return;
                }

                // Dynamic: Kịch bản
                var scripts = _scriptContext.GetByPlatform(_platform) ?? new List<Script>();
                var matchScript = scripts.FirstOrDefault(s => s.Name == it.Text);
                if (matchScript != null)
                {
                    string scriptName = matchScript.Name ?? "";
                    // "Làm Job Golike" giờ dùng token login Golike — bỏ popup nhập token.
                    var toUpdate = new List<Account>();
                    foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                        if (row.DataBoundItem is Account a) { a.NameScript = scriptName; toUpdate.Add(a); }
                    if (toUpdate.Any()) _accountContext.Update(toUpdate);
                    return;
                }
            }
        }

        private async Task Reg()
        {
            fSettingRegsiner fSettingRegsiner = new fSettingRegsiner(_platform);
            if (fSettingRegsiner.ShowDialog() != DialogResult.OK) return;
            if (!SeleceterDevice())
            {
                return;
            }
            var model = GetConfigModel(true);
            if (model == null)
            {
                return;
            }
            try
            {
                Enable(false);
                fMain.StartTime = DateTime.Now;
                Globals.CancellationTokenSource = new CancellationTokenSource();
                CancellationToken ct = Globals.CancellationTokenSource.Token;
                List<Task> tasks = new List<Task>();
                foreach (var device in DeviceServices.DeviceModels.Where(x => x.Checked))
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        ADBClient client = new ADBClient(device);
                        ChangeLanguageService changeLanguage = new ChangeLanguageService(client);
                        await changeLanguage.Change("en", "US");
                        await client.TurnOnADBKeyboard();
                        FacebookRegsiner service = new FacebookRegsiner(_platform, client, model, ct);
                        service.AccountAdded += (s, acc) =>
                        {
                            AddAccountThreadSafe(acc);
                        };
                        await service.RunAsync();
                    }));
                }
                await Task.WhenAll(tasks);
            }
            finally
            {
                Enable(true);
                fMain.StartTime = null;
            }
        }

        public void AddAccountThreadSafe(Account acc)
        {
            acc.STT = dataGridView1.RowCount + 1;
            if (InvokeRequired)
            {
                Invoke(new Action<Account>(AddAccountThreadSafe), acc);
            }
            else
            {
                bindingList.Add(acc);
            }
        }

        private void select4_SelectedIndexChanged(object sender, IntEventArgs e)
        {
    //        var rows = _jobHistoryContext.GetJobTotals(_platform, select4.Text.Trim(), DateTime.Now.ToString("dd/MM/yyyy"));

    //        if (!rows.Any())
    //        {
    //            return;
    //        }
    //        int today = 0;
    //        int success = 0;
    //        int fail = 0;
    //        foreach (var row in rows)
    //        {
    //            today += row.Value;
    //            if (row.Key.Contains("_skip"))
    //            {
    //                fail += row.Value;
    //            }
    //            else
    //            {
    //                success += row.Value;
    //            }
    //        }
    //        var grouped = rows
    //.GroupBy(kv => kv.Key.EndsWith("_skip")
    //                ? kv.Key.Replace("_skip", "")
    //                : kv.Key)
    //.ToDictionary(
    //    g => g.Key,
    //    g => new
    //    {
    //        Success = g.Where(x => !x.Key.EndsWith("_skip")).Sum(x => x.Value),
    //        Skip = g.Where(x => x.Key.EndsWith("_skip")).Sum(x => x.Value)
    //    });
    //        foreach (var kv in grouped)
    //        {
    //            string key = kv.Key;
    //            string displayKey = char.ToUpper(key[0]) + key.Substring(1).ToLower();
    //            string text = $"{displayKey}: {kv.Value.Success}/{kv.Value.Skip}";
    //            foreach (ToolStripMenuItem item in toolStripDropDownButton1.DropDownItems)
    //            {
    //                string name = item.Name.Split("_").First().ToLower();
    //                if (name != kv.Key) continue;
    //                item.Text = text;
    //                ControlHelper.SetToolStripMenuItemTextSafe(item, text);
    //                break;
    //            }
    //        }
    //        ControlHelper.SetToolStripLabelTextSafe(toolStripLabel16, today.ToMoneyString());
    //        ControlHelper.SetToolStripMenuItemTextSafe(JobTotal_toolStripMenuItem, $"Job Total: {success}/{fail}");
        }

        private void button8_Click(object sender, EventArgs e)
        {
            try { Globals.CancellationTokenSource?.Cancel(); } catch { }

            // Force stop tất cả ADBClient đang chạy: set Running=false để ThrowIfStopped()
            // throw ở vòng lặp tiếp theo trong ADB sync calls → script bubble up và thoát ngay.
            foreach (var c in _activeClients)
            {
                try { c.Running = false; } catch { }
            }

            button8.Enabled = false;
            button8.Text    = "Đang dừng…";

            AntdHelper.MsgInfo(_form, "Đang dừng các tác vụ đang chạy…");
        }

        private void select2_SelectedIndexChanged(object sender, IntEventArgs e)
        {
            List<Account> accounts = new List<Account>();
            string state = select2.Text;
            if (state.Contains("Tất cả"))
            {
                accounts = _accounts;
            }
            else if (state == "unknown")
            {
                accounts = _accounts.Where(x => x.State == "").ToList();
            }
            else
            {
                accounts = _accounts.Where(x => x.State.ToLower() == state.ToLower()).ToList();
            }
            // Đây là UI event handler — gọi trực tiếp, không cần Invoke
            bindingList = new SortableBindingList<Account>(accounts);
            dataGridView1.SuspendLayout();
            dataGridView1.DataSource = bindingList;
            dataGridView1.ResumeLayout();
        }

        private void CboFilterAccount_SelectedValueChanged(object sender, AntdUI.ObjectsEventArgs e)
        {
            var selected = e.Value?
                .Select(v => v?.ToString() ?? "")
                .Where(v => !string.IsNullOrEmpty(v))
                .ToHashSet() ?? new HashSet<string>();

            // Không chọn gì = hiển thị tất cả
            if (selected.Count == 0)
            {
                ApplyFilterResult(_accounts);
                return;
            }

            // Áp dụng tất cả filter cùng lúc (AND)
            IEnumerable<Account> result = _accounts;

            foreach (var filter in selected)
            {
                result = filter switch
                {
                    // State
                    "LIVE" => result.Where(x => x.State.Equals("live", StringComparison.OrdinalIgnoreCase)),
                    "DIE" => result.Where(x => x.State.Equals("die", StringComparison.OrdinalIgnoreCase)),
                    "Chưa xác định" => result.Where(x => string.IsNullOrEmpty(x.State)),
                    // Status
                    "Đang chạy" => result.Where(x => x.Running),
                    "Lỗi" => result.Where(x => x.Status.Contains("Lỗi", StringComparison.OrdinalIgnoreCase)),
                    "Đăng xuất" => result.Where(x => x.Status.Contains("Đăng xuất", StringComparison.OrdinalIgnoreCase)),
                    "Captcha" => result.Where(x => x.Status.Contains("Captcha", StringComparison.OrdinalIgnoreCase)),
                    "Bị chặn" => result.Where(x => x.Status.Contains("chặn", StringComparison.OrdinalIgnoreCase)),
                    "Đã dừng" => result.Where(x => x.Status.Contains("Đã dừng", StringComparison.OrdinalIgnoreCase)),
                    "Có trạng thái" => result.Where(x => !string.IsNullOrEmpty(x.Status)),
                    "Chưa có trạng thái" => result.Where(x => string.IsNullOrEmpty(x.Status)),
                    // Tên
                    "Tên tiếng Việt" => result.Where(x => IsVietnameseName(x.FullName)),
                    "Tên tiếng Anh" => result.Where(x => !string.IsNullOrEmpty(x.FullName) && !IsVietnameseName(x.FullName)),
                    // UID
                    "UID đầu 6" => result.Where(x => x.Uid.StartsWith("6")),
                    "UID đầu 1" => result.Where(x => x.Uid.StartsWith("1")),
                    // Tương tác
                    "Tương tác hôm nay" => result.Where(x => GetInteractionDate(x.RecentInteraction) == DateTime.Today),
                    "Tương tác hôm qua" => result.Where(x => GetInteractionDate(x.RecentInteraction) == DateTime.Today.AddDays(-1)),
                    "Chưa tương tác" => result.Where(x => string.IsNullOrEmpty(x.RecentInteraction)),
                    // Nhóm tài khoản
                    _ when filter.StartsWith("Nhóm: ") && filter.Length >= 6 => result.Where(x => x.NameFolder == filter.Substring(6)),
                    _ => result
                };
            }

            ApplyFilterResult(result.ToList());
        }

        private void ApplyFilterResult(List<Account> accounts)
        {
            bindingList = new SortableBindingList<Account>(accounts);
            dataGridView1.SuspendLayout();
            dataGridView1.DataSource = bindingList;
            dataGridView1.ResumeLayout();
        }

        private void ShowUpdateFieldPopup(string fieldLabel)
        {
            var selectedAccounts = dataGridView1.SelectedRows
                .Cast<DataGridViewRow>()
                .Select(r => r.DataBoundItem as Account)
                .Where(a => a != null)
                .Cast<Account>()
                .ToList();

            if (!selectedAccounts.Any())
            {
                AntdHelper.MsgWarn(_form, "Vui lòng chọn (bôi đen) dòng cần cập nhật.");
                return;
            }

            var inputTxt = new AntdUI.Input
            {
                PlaceholderText = $"Nhập {fieldLabel} mới...",
                Height = 36,
                Dock = DockStyle.Bottom,
            };

            var contentPanel = new System.Windows.Forms.Panel { Width = 380, Height = 72, Padding = new Padding(0) };
            var lblInfo = new AntdUI.Label
            {
                Text = $"Cập nhật {fieldLabel} cho {selectedAccounts.Count} tài khoản",
                Dock = DockStyle.Top,
                Height = 28,
                Font = new System.Drawing.Font("Segoe UI", 9.5f)
            };
            contentPanel.Controls.Add(inputTxt);
            contentPanel.Controls.Add(lblInfo);

            var result = AntdUI.Modal.open(new AntdUI.Modal.Config(_form, $"Cập nhật {fieldLabel}", (Control)contentPanel)
            {
                OkText = "Cập nhật",
                CancelText = "Hủy",
                Width = 440,
            });

            if (result != DialogResult.OK) return;

            string value = inputTxt.Text;
            foreach (var acc in selectedAccounts)
            {
                if (acc == null) continue;
                switch (fieldLabel)
                {
                    case "Pass":              acc.Password = value; break;
                    case "2FA":               acc.TowFA = value; break;
                    case "Cookie":            acc.Cookie = value; break;
                    case "Mail":              acc.Email = value; break;
                    case "Pass mail":         acc.PassMail = value; break;
                    case "Mail recover":      acc.EmailAddress = value; break;
                    case "Pass mail recover": acc.PassPrivateEmailAddress = value; break;
                    case "Phone":             acc.Phone = value; break;
                    case "Ngày sinh":         acc.Birthday = value; break;
                    case "User agent":        acc.UserAgent = value; break;
                    case "Ghi chú":           acc.Note = value; break;
                }
            }
            _accountContext.Update(selectedAccounts);
            _ = LoadAccounts();
        }

        // Map: tên hiển thị trong combobox → DataPropertyName để truyền vào CopyFormat
        private static readonly (string Label, string PropName)[] _copyFields = new[]
        {
            ("Uid",               nameof(Account.Uid)),
            ("Pass",              nameof(Account.Password)),
            ("Token",             nameof(Account.Token)),
            ("2FA",               nameof(Account.TowFA)),
            ("Cookie",            nameof(Account.Cookie)),
            ("Proxy",             nameof(Account.Proxy)),
            ("Name",              nameof(Account.FullName)),
            ("Phone",             nameof(Account.Phone)),
            ("Mail",              nameof(Account.Email)),
            ("Pass Mail",         nameof(Account.PassMail)),
            ("Mail Client Id",    nameof(Account.MailClientId)),
            ("Mail Refresh Token",nameof(Account.MailRefreshToken)),
            ("Mail Recover",      nameof(Account.EmailAddress)),
            ("Pass Mail Recover", nameof(Account.PassPrivateEmailAddress)),
            ("UserAgent",         nameof(Account.UserAgent)),
        };

        private void ShowCopyCustomFormatDialog()
        {
            const int SLOTS       = 10;
            const int COMBO_W     = 120;
            const int COMBO_H     = 24;
            const int SEP_W       = 16;
            const int PAD         = 24;

            // Form vừa đủ chứa 10 combo + 9 separator + padding 2 bên
            int formW = SLOTS * COMBO_W + (SLOTS - 1) * SEP_W + PAD * 2 + 16; // +16 border
            int formH = 185;

            using var dlg = new Form
            {
                Text            = "Cấu hình copy tài khoản",
                Width           = formW,
                Height          = formH,
                FormBorderStyle = FormBorderStyle.FixedSingle,
                StartPosition   = FormStartPosition.CenterParent,
                MaximizeBox     = false,
                MinimizeBox     = false,
                BackColor       = Color.White
            };

            // ── Header ──────────────────────────────────────────────
            var lblTitle = new System.Windows.Forms.Label
            {
                Text      = "Chọn định dạng cần copy",
                Left      = 0, Top = 12, Width = formW - 16, Height = 22,
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };
            var lblHint = new System.Windows.Forms.Label
            {
                Text      = "Auto sẽ tự động lưu lại định dạng copy sau cùng để sử dụng nhanh cho lần copy tiếp theo",
                Left      = 0, Top = 36, Width = formW - 16, Height = 18,
                Font      = new Font("Segoe UI", 8f),
                ForeColor = Color.FromArgb(120, 120, 120),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // ── Divider ──────────────────────────────────────────────
            var div = new System.Windows.Forms.Panel
            {
                Left = 0, Top = 58, Width = formW - 16, Height = 1,
                BackColor = Color.FromArgb(220, 220, 220)
            };

            // ── ComboBox row ─────────────────────────────────────────
            var combos   = new List<ComboBox>();
            var labels   = _copyFields.Select(f => f.Label).Prepend("").ToArray(); // item rỗng ở đầu
            int comboTop = 70;
            int x        = PAD;

            for (int i = 0; i < SLOTS; i++)
            {
                if (i > 0)
                {
                    var sep = new System.Windows.Forms.Label
                    {
                        Text      = "|",
                        Left      = x, Top = comboTop,
                        Width     = SEP_W, Height = COMBO_H,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Font      = new Font("Segoe UI", 10f),
                        ForeColor = Color.FromArgb(180, 180, 180)
                    };
                    dlg.Controls.Add(sep);
                    x += SEP_W;
                }

                var cbo = new ComboBox
                {
                    Left          = x, Top = comboTop,
                    Width         = COMBO_W, Height = COMBO_H,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Font          = new Font("Segoe UI", 8.5f)
                };
                cbo.Items.AddRange(labels.Cast<object>().ToArray());
                cbo.SelectedIndex = 0; // mặc định item rỗng
                combos.Add(cbo);
                dlg.Controls.Add(cbo);
                x += COMBO_W;
            }

            // Chống trùng: khi combo A chọn giá trị đã có ở combo B → reset combo B
            foreach (var cbo in combos)
            {
                cbo.SelectedIndexChanged += (s, _) =>
                {
                    if (s is not ComboBox changed) return;
                    string picked = changed.SelectedItem?.ToString() ?? "";
                    if (string.IsNullOrEmpty(picked)) return; // item rỗng — không reset combo khác
                    foreach (var other in combos)
                    {
                        if (other != changed && other.SelectedItem?.ToString() == picked)
                            other.SelectedIndex = -1;
                    }
                };
            }

            // ── Buttons ──────────────────────────────────────────────
            int btnTop = comboTop + COMBO_H + 16;
            int btnW   = 100, btnH = 30;
            int totalBtnW = btnW * 2 + 12;
            int btnLeft   = (formW - 16 - totalBtnW) / 2;

            var btnCopy = new System.Windows.Forms.Button
            {
                Text      = "Copy",
                Left      = btnLeft, Top = btnTop,
                Width     = btnW, Height = btnH,
                BackColor = Color.FromArgb(24, 144, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                DialogResult = DialogResult.OK
            };
            btnCopy.FlatAppearance.BorderSize = 0;

            var btnClose = new System.Windows.Forms.Button
            {
                Text      = "Đóng",
                Left      = btnLeft + btnW + 12, Top = btnTop,
                Width     = btnW, Height = btnH,
                BackColor = Color.FromArgb(240, 240, 240),
                ForeColor = Color.FromArgb(60, 60, 60),
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9f),
                DialogResult = DialogResult.Cancel
            };
            btnClose.FlatAppearance.BorderSize = 0;

            dlg.Controls.AddRange(new Control[] { lblTitle, lblHint, div, btnCopy, btnClose });
            dlg.AcceptButton = btnCopy;
            dlg.CancelButton = btnClose;

            if (dlg.ShowDialog() != DialogResult.OK) return;

            // Lấy DataPropertyName từ label đã chọn
            var selectedProps = combos
                .Select(c => c.SelectedItem?.ToString() ?? "")
                .Where(s => !string.IsNullOrEmpty(s))
                .Select(label => _copyFields.FirstOrDefault(f => f.Label == label).PropName)
                .Where(p => !string.IsNullOrEmpty(p))
                .ToList();

            if (!selectedProps.Any()) { AntdHelper.MsgWarn(_form, "Vui lòng chọn ít nhất một trường."); return; }

            bool needsAuth = selectedProps.Any(p => p == nameof(Account.Password) || p == nameof(Account.TowFA));
            if (needsAuth && !VerifySubdyPassword()) return;

            ConvertHelper.CopyFormat(string.Join("|", selectedProps), dataGridView1);
        }

        /// <summary>
        /// Hiển thị popup nhập mật khẩu Subdy để xác thực trước khi copy dữ liệu nhạy cảm.
        /// Trả về true nếu xác thực thành công.
        /// </summary>
        private bool VerifySubdyPassword()
        {
            var user = Globals.User;
            if (user == null) { AntdHelper.MsgWarn(_form, "Chưa đăng nhập tài khoản Golike."); return false; }

            const int W = 370, H = 200;
            const int PAD = 20;

            using var dlg = new Form
            {
                Text            = "Xác thực",
                Width           = W,
                Height          = H,
                FormBorderStyle = FormBorderStyle.FixedSingle,
                StartPosition   = FormStartPosition.CenterParent,
                MaximizeBox     = false,
                MinimizeBox     = false,
                BackColor       = Color.White
            };

            // Header xanh
            var pnlTop = new System.Windows.Forms.Panel
            {
                Left = 0, Top = 0, Width = W, Height = 60,
                BackColor = Color.FromArgb(24, 144, 255)
            };
            var lblTitle = new System.Windows.Forms.Label
            {
                Text      = "🔐  Xác thực bảo mật",
                Left      = 0, Top = 0, Width = W, Height = 60,
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlTop.Controls.Add(lblTitle);

            // Label
            var lblUser = new System.Windows.Forms.Label
            {
                Text      = $"Nhập mật khẩu tài khoản  [{user.UserName}]:",
                Left      = PAD, Top = 72, Width = W - PAD * 2, Height = 20,
                Font      = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(60, 60, 60)
            };

            // TextBox mật khẩu
            var txtPass = new TextBox
            {
                Left                  = PAD, Top = 96, Width = W - PAD * 2 - 16, Height = 24,
                UseSystemPasswordChar = true,
                Font                  = new Font("Segoe UI", 10f),
                BorderStyle           = BorderStyle.FixedSingle
            };

            // Buttons — tọa độ tuyệt đối, căn giữa
            int btnW = 110, btnH = 30, btnTop = 135, gap = 10;
            int totalW = btnW * 2 + gap;
            int btnLeft = (W - 16 - totalW) / 2;

            var btnOk = new System.Windows.Forms.Button
            {
                Text      = "Xác nhận",
                Left      = btnLeft, Top = btnTop, Width = btnW, Height = btnH,
                BackColor = Color.FromArgb(24, 144, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                DialogResult = DialogResult.OK
            };
            btnOk.FlatAppearance.BorderSize = 0;

            var btnCancel = new System.Windows.Forms.Button
            {
                Text      = "Hủy",
                Left      = btnLeft + btnW + gap, Top = btnTop, Width = btnW, Height = btnH,
                BackColor = Color.FromArgb(240, 240, 240),
                ForeColor = Color.FromArgb(60, 60, 60),
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9f),
                DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            dlg.Controls.AddRange(new Control[] { pnlTop, lblUser, txtPass, btnOk, btnCancel });
            dlg.AcceptButton = btnOk;
            dlg.CancelButton = btnCancel;
            dlg.Shown += (_, __) => txtPass.Focus();

            if (dlg.ShowDialog() != DialogResult.OK) return false;

            if (txtPass.Text.Trim() == user.Password) return true;

            AntdHelper.MsgError(_form, "Mật khẩu không đúng.");
            return false;
        }

        private static bool IsVietnameseName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            // Ký tự đặc trưng tiếng Việt (có dấu)
            const string vietnameseChars = "àáảãạăắằẳẵặâấầẩẫậèéẻẽẹêếềểễệìíỉĩịòóỏõọôốồổỗộơớờởỡợùúủũụưứừửữựỳýỷỹỵđ";
            return name.Any(c => vietnameseChars.Contains(char.ToLower(c)));
        }

        private static DateTime? GetInteractionDate(string recentInteraction)
        {
            if (string.IsNullOrEmpty(recentInteraction)) return null;
            // Chỉ lấy phần ngày, bỏ giờ phút giây
            string dateOnly = recentInteraction.Split(' ')[0];
            if (DateTime.TryParseExact(dateOnly, new[] { "dd/MM/yyyy", "yyyy-MM-dd", "d/M/yyyy", "MM/dd/yyyy" },
                System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var date))
                return date.Date;
            if (DateTime.TryParse(recentInteraction, out var fallback))
                return fallback.Date;
            return null;
        }

        private void UpdateFilterFolders(List<string?> folderNames)
        {
            // Xóa nhóm cũ (items sau "Chưa tương tác")
            int cutIndex = -1;
            for (int i = 0; i < cboFilterAccount.Items.Count; i++)
            {
                if (cboFilterAccount.Items[i]?.ToString() == "Chưa tương tác")
                {
                    cutIndex = i + 1;
                    break;
                }
            }
            if (cutIndex > 0 && cutIndex < cboFilterAccount.Items.Count)
            {
                while (cboFilterAccount.Items.Count > cutIndex)
                    cboFilterAccount.Items.RemoveAt(cboFilterAccount.Items.Count - 1);
            }

            // Thêm nhóm tài khoản
            if (folderNames.Count > 0)
            {
                foreach (var name in folderNames)
                {
                    if (!string.IsNullOrEmpty(name))
                        cboFilterAccount.Items.Add("Nhóm: " + name);
                }
            }
        }

        private async void button6_Click(object sender, EventArgs e)
        {
            new fQuanLyKichBan(_platform).ShowDialog();
            await LoadJobService();
        }
        public async void fMain_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                await LoadAccounts();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Space)
            { 
                foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                {
                    if (row.DataBoundItem is Account account)
                    {
                        account.Checked = !account.Checked;
                    }
                }
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                _form.btn_setting_Click(null, null);
            }
        }

        // ── Chức năng: Check live / Kiểm tra avatar ────────────────────────────
        private async Task RunCheckLiveAsync(bool checkAvatarMode)
        {
            var targets = dataGridView1.SelectedRows.Cast<DataGridViewRow>()
                .Select(r => r.DataBoundItem as Account).OfType<Account>().ToList();
            if (!targets.Any()) { AntdHelper.MsgWarn(_form, "Vui lòng bôi đen tài khoản cần kiểm tra."); return; }

            int live = 0, total = targets.Count, done = 0;
            string spinLabel = checkAvatarMode ? "Kiểm tra avatar..." : "Check live...";

            await AntdUI.Spin.open(this, spinLabel, async cfg =>
            {
                using var sem = new SemaphoreSlim(10, 10);
                var tasks = targets.Select(async acc =>
                {
                    await sem.WaitAsync();
                    try
                    {
                        bool result = await FacebookRequest.CheckLive(acc.Uid);
                        if (result)
                        {
                            if (checkAvatarMode) { acc.State = "Có avatar"; Interlocked.Increment(ref live); }
                            else { acc.State = "LIVE"; Interlocked.Increment(ref live); }
                        }
                        else
                        {
                            acc.State = checkAvatarMode ? "Không có avatar" : "DIE";
                        }
                        int d = Interlocked.Increment(ref done);
                        cfg.Text = $"{spinLabel} {d} / {total}";
                    }
                    finally { sem.Release(); }
                }).ToList();
                await Task.WhenAll(tasks);
            });

            _accountContext.Update(targets);

            if (checkAvatarMode)
                AntdHelper.MsgSuccess(_form, $"Có avatar: {live} / {total}");
            else
                AntdHelper.MsgSuccess(_form, $"LIVE: {live} / {total}  |  DIE: {total - live}");
        }

        // ── Chức năng: Kiểm tra cookie ─────────────────────────────────────────
        private async Task RunCheckCookieAsync()
        {
            var targets = dataGridView1.SelectedRows.Cast<DataGridViewRow>()
                .Select(r => r.DataBoundItem as Account).OfType<Account>()
                .Where(a => !string.IsNullOrEmpty(a.Cookie)).ToList();
            if (!targets.Any()) { AntdHelper.MsgWarn(_form, "Không có tài khoản bôi đen nào có cookie."); return; }

            int live = 0, total = targets.Count, done = 0;

            await AntdUI.Spin.open(this, "Kiểm tra cookie...", async cfg =>
            {
                using var sem = new SemaphoreSlim(10, 10);
                var tasks = targets.Select(async acc =>
                {
                    await sem.WaitAsync();
                    try
                    {
                        bool result = await FacebookRequest.CheckLive(acc.Uid);
                        acc.State = result ? "LIVE" : "DIE";
                        if (result) Interlocked.Increment(ref live);
                        int d = Interlocked.Increment(ref done);
                        cfg.Text = $"Kiểm tra cookie... {d} / {total}";
                    }
                    finally { sem.Release(); }
                }).ToList();
                await Task.WhenAll(tasks);
            });

            _accountContext.Update(targets);
            AntdHelper.MsgSuccess(_form, $"Cookie LIVE: {live} / {total}  |  DIE: {total - live}");
        }

        // ── Chức năng: Check name VN ───────────────────────────────────────────
        private void RunCheckNameVN()
        {
            var targets = dataGridView1.SelectedRows.Cast<DataGridViewRow>()
                .Select(r => r.DataBoundItem as Account).OfType<Account>().ToList();
            if (!targets.Any()) { AntdHelper.MsgWarn(_form, "Vui lòng bôi đen tài khoản cần kiểm tra."); return; }

            int vnCount = targets.Count(a => IsVietnameseName(a.FullName));
            AntdHelper.MsgSuccess(_form, $"Tên tiếng Việt: {vnCount} / {targets.Count}");
        }

        // ── Chức năng: Kiểm tra live proxy ────────────────────────────────────
        private async Task RunCheckProxyAsync()
        {
            var targets = dataGridView1.SelectedRows.Cast<DataGridViewRow>()
                .Select(r => r.DataBoundItem as Account).OfType<Account>().ToList();
            if (!targets.Any()) { AntdHelper.MsgWarn(_form, "Vui lòng bôi đen tài khoản cần kiểm tra."); return; }

            var noProxy = targets.Where(a => string.IsNullOrEmpty(a.Proxy)).ToList();
            var withProxy = targets.Where(a => !string.IsNullOrEmpty(a.Proxy)).ToList();
            foreach (var a in noProxy) a.State = "Không có proxy";

            int live = 0, die = 0, total = withProxy.Count, done = 0;
            if (total > 0)
            {
                await AntdUI.Spin.open(this, "Kiểm tra proxy...", async cfg =>
                {
                    using var sem = new SemaphoreSlim(10, 10);
                    var tasks = withProxy.Select(async acc =>
                    {
                        await sem.WaitAsync();
                        try
                        {
                            bool isLive = await CheckProxyLiveAsync(acc.Proxy);
                            acc.State = isLive ? "Proxy LIVE" : "Proxy DIE";
                            if (isLive) Interlocked.Increment(ref live);
                            else Interlocked.Increment(ref die);
                            int d = Interlocked.Increment(ref done);
                            cfg.Text = $"Kiểm tra proxy... {d} / {total}";
                        }
                        finally { sem.Release(); }
                    }).ToList();
                    await Task.WhenAll(tasks);
                });
            }

            _accountContext.Update(targets);
            string msg = $"Proxy LIVE: {live} / {total}  |  DIE: {die}";
            if (noProxy.Any()) msg += $"\nKhông có proxy: {noProxy.Count}";
            AntdHelper.MsgSuccess(_form, msg);
        }

        private static async Task<bool> CheckProxyLiveAsync(string proxy)
        {
            try
            {
                var handler = new System.Net.Http.HttpClientHandler();
                if (!string.IsNullOrWhiteSpace(proxy))
                {
                    var uri = new Uri(proxy.StartsWith("http") ? proxy : "http://" + proxy);
                    handler.Proxy = new System.Net.WebProxy(uri);
                    handler.UseProxy = true;
                }
                using var client = new System.Net.Http.HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
                var resp = await client.GetAsync("https://api64.ipify.org/?format=json");
                return resp.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        // ══════════════════════════════════════════════════════════════════════
        // 1. LOGIN PHONE
        // ══════════════════════════════════════════════════════════════════════
        private async Task LoginPhoneAsync()
        {
            var selected = _accounts.Where(x => x.Checked).ToList();
            if (!selected.Any())
            {
                AntdHelper.MsgWarn(_form, "Vui lòng chọn ít nhất 1 tài khoản cần đăng nhập.");
                return;
            }
            if (!SeleceterDevice()) return;

            var devices = DeviceServices.DeviceModels.Where(x => x.Checked).ToList();
            if (!devices.Any()) { AntdHelper.MsgWarn(_form, "Chưa chọn thiết bị."); return; }

            try
            {
                Enable(false);
                fMain.StartTime = DateTime.Now;
                Globals.CancellationTokenSource = new CancellationTokenSource();
                var ct = Globals.CancellationTokenSource.Token;

                int accIndex = 0;
                var tasks = new List<Task>();

                foreach (var device in devices)
                {
                    if (accIndex >= selected.Count) break;
                    var acc = selected[accIndex++];
                    var dev = device;
                    tasks.Add(Task.Run(async () =>
                    {
                        var client = new ADBClient(dev);
                        try
                        {
                            var svc = new Sunny.Subd.Core.Facebook.FacebookService();
                            var ext = await svc.Login(client, acc, ct, 180, null!);
                            string stateText = ext.SubdyEnum == Sunny.Subd.Core.Models.SubdyEnum.Success
                                ? "Login thành công"
                                : $"Login thất bại: {ext.Message}";
                            acc.State = ext.SubdyEnum == Sunny.Subd.Core.Models.SubdyEnum.Success ? "LIVE" : ext.SubdyEnum.ToString();
                            acc.Status = stateText;
                        }
                        catch (Exception ex)
                        {
                            acc.Status = $"Lỗi: {ex.Message}";
                        }
                        _accountContext.Update(acc);
                    }));
                }
                await Task.WhenAll(tasks);
                _ = LoadAccounts();
                AntdHelper.MsgSuccess(_form, $"Đã đăng nhập {tasks.Count} tài khoản.");
            }
            finally
            {
                Enable(true);
                fMain.StartTime = null;
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // 2. QUẢN LÝ BACKUP PROFILE / DEVICE
        // ══════════════════════════════════════════════════════════════════════
        private enum BackupType { Profile, Device, Both }
        private enum BackupOp   { Check, Delete, Copy }

        private string GetBackupDir(BackupType type)
        {
            var s = SettingsTool.GetSettings($"{nameof(fSettingDefault)}_{_platform}", true);
            if (type == BackupType.Profile)
                return s.GetValuesFromInputString("textBox3", System.IO.Path.Combine(AppContext.BaseDirectory, "Backup", "Profile", _platform));
            return s.GetValuesFromInputString("textBox2", System.IO.Path.Combine(AppContext.BaseDirectory, "Backup", "Device", _platform));
        }

        private string GetBackupFile(BackupType type, string uid)
            => System.IO.Path.Combine(GetBackupDir(type), $"{uid}.tar.gz");

        private void BackupAction(BackupType type, BackupOp op)
        {
            var selected = dataGridView1.SelectedRows
                .Cast<DataGridViewRow>()
                .Select(r => r.DataBoundItem as Account)
                .OfType<Account>()
                .Where(a => !string.IsNullOrEmpty(a.Uid))
                .ToList();

            if (!selected.Any()) { AntdHelper.MsgWarn(_form, "Vui lòng bôi đen tài khoản cần thực hiện."); return; }

            // Xác định danh sách (type, uid, path) cần xử lý
            var entries = new List<(BackupType t, string uid, string path)>();
            foreach (var acc in selected)
            {
                if (type == BackupType.Both)
                {
                    entries.Add((BackupType.Profile, acc.Uid, GetBackupFile(BackupType.Profile, acc.Uid)));
                    entries.Add((BackupType.Device,  acc.Uid, GetBackupFile(BackupType.Device,  acc.Uid)));
                }
                else
                {
                    entries.Add((type, acc.Uid, GetBackupFile(type, acc.Uid)));
                }
            }

            int total  = type == BackupType.Both ? selected.Count * 2 : selected.Count;
            int found  = entries.Count(e => System.IO.File.Exists(e.path));
            int notFound = total - found;

            switch (op)
            {
                case BackupOp.Check:
                {
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine($"Tổng: {total} | Có: {found} | Không có: {notFound}");
                    sb.AppendLine(new string('─', 60));
                    foreach (var (t, uid, path) in entries)
                    {
                        bool exists = System.IO.File.Exists(path);
                        string label = type == BackupType.Both ? $"[{(t == BackupType.Profile ? "Profile" : "Device")}] " : "";
                        sb.AppendLine($"{(exists ? "✔" : "✘")} {label}{uid}");
                        if (exists) sb.AppendLine($"   {path}");
                    }
                    ShowBackupResultDialog($"Check backup {BackupTypeLabel(type)}", sb.ToString());
                    break;
                }

                case BackupOp.Delete:
                {
                    if (found == 0) { AntdHelper.MsgWarn(_form, $"Không có file backup nào để xóa ({notFound}/{total} không tồn tại)."); return; }
                    if (!AntdHelper.Confirm(_form, "Xác nhận xóa", $"Xóa {found}/{total} file backup?")) return;

                    int deleted = 0;
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine($"Đã xóa: {0} | Không tồn tại: {notFound}");
                    foreach (var (t, uid, path) in entries)
                    {
                        string label = type == BackupType.Both ? $"[{(t == BackupType.Profile ? "Profile" : "Device")}] " : "";
                        if (System.IO.File.Exists(path))
                        {
                            System.IO.File.Delete(path);
                            deleted++;
                            sb.AppendLine($"✔ Đã xóa: {label}{uid}");
                        }
                        else
                        {
                            sb.AppendLine($"✘ Không tồn tại: {label}{uid}");
                        }
                    }
                    // Cập nhật dòng đầu
                    string result = sb.ToString();
                    result = result.Replace("Đã xóa: 0", $"Đã xóa: {deleted}");
                    ShowBackupResultDialog($"Xóa backup {BackupTypeLabel(type)}", result);
                    break;
                }

                case BackupOp.Copy:
                {
                    if (found == 0) { AntdHelper.MsgWarn(_form, $"Không có file backup nào để copy ({notFound}/{total} không tồn tại)."); return; }

                    using var fbd = new System.Windows.Forms.FolderBrowserDialog { Description = "Chọn thư mục đích để copy backup" };
                    if (fbd.ShowDialog() != DialogResult.OK) return;
                    string destDir = fbd.SelectedPath;

                    int copied = 0;
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine($"Đã copy: 0 | Không tồn tại: {notFound}");
                    foreach (var (t, uid, path) in entries)
                    {
                        string label = type == BackupType.Both ? $"[{(t == BackupType.Profile ? "Profile" : "Device")}] " : "";
                        if (System.IO.File.Exists(path))
                        {
                            string typeFolder = t == BackupType.Profile ? "Profile" : "Device";
                            string dest;
                            if (type == BackupType.Both)
                                dest = System.IO.Path.Combine(destDir, typeFolder, System.IO.Path.GetFileName(path));
                            else
                                dest = System.IO.Path.Combine(destDir, System.IO.Path.GetFileName(path));
                            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dest)!);
                            System.IO.File.Copy(path, dest, overwrite: true);
                            copied++;
                            sb.AppendLine($"✔ Đã copy: {label}{uid}");
                        }
                        else
                        {
                            sb.AppendLine($"✘ Không tồn tại: {label}{uid}");
                        }
                    }
                    string result = sb.ToString();
                    result = result.Replace("Đã copy: 0", $"Đã copy: {copied}");
                    ShowBackupResultDialog($"Copy backup {BackupTypeLabel(type)}", result);
                    break;
                }
            }
        }

        private static string BackupTypeLabel(BackupType t) => t switch
        {
            BackupType.Profile => "profile",
            BackupType.Device  => "device",
            _                  => "profile và device"
        };

        private void ShowBackupResultDialog(string title, string content)
        {
            var txt = new AntdUI.Input
            {
                Multiline        = true,
                ReadOnly         = true,
                Text             = content,
                Dock             = DockStyle.Fill,
                Font             = new System.Drawing.Font("Consolas", 9f)
            };
            var panel = new System.Windows.Forms.Panel { Width = 520, Height = 340 };
            txt.Dock = DockStyle.Fill;
            panel.Controls.Add(txt);
            AntdUI.Modal.open(new AntdUI.Modal.Config(_form, title, (Control)panel)
            {
                OkText     = "Đóng",
                CancelText = "",
                Width      = 560,
            });
        }

        // ── Dọn dẹp backup dư thừa ───────────────────────────────────────────
        private void CleanupRedundantBackups()
        {
            var selectedUids = dataGridView1.SelectedRows
                .Cast<DataGridViewRow>()
                .Select(r => r.DataBoundItem as Account)
                .OfType<Account>()
                .Where(a => !string.IsNullOrEmpty(a.Uid))
                .Select(a => a.Uid!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (!selectedUids.Any()) { AntdHelper.MsgWarn(_form, "Vui lòng bôi đen tài khoản cần giữ lại."); return; }

            string profileDir = GetBackupDir(BackupType.Profile);
            string deviceDir  = GetBackupDir(BackupType.Device);

            var toDelete = new List<string>();
            foreach (var dir in new[] { profileDir, deviceDir })
            {
                if (!System.IO.Directory.Exists(dir)) continue;
                foreach (var file in System.IO.Directory.GetFiles(dir, "*.tar.gz"))
                {
                    string uid = System.IO.Path.GetFileNameWithoutExtension(System.IO.Path.GetFileNameWithoutExtension(file));
                    if (!selectedUids.Contains(uid))
                        toDelete.Add(file);
                }
            }

            if (!toDelete.Any()) { AntdHelper.MsgSuccess(_form, "Không có file dư thừa cần dọn."); return; }
            if (!AntdHelper.Confirm(_form, "Xác nhận dọn dẹp", $"Sẽ xóa {toDelete.Count} file backup dư thừa. Tiếp tục?")) return;

            int deleted = 0;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Đã xóa: 0/{toDelete.Count}");
            sb.AppendLine(new string('─', 60));
            foreach (var f in toDelete)
            {
                try { System.IO.File.Delete(f); deleted++; sb.AppendLine($"✔ Đã xóa: {f}"); }
                catch (Exception ex) { sb.AppendLine($"✘ Lỗi ({f}): {ex.Message}"); }
            }
            string result = sb.ToString().Replace("Đã xóa: 0", $"Đã xóa: {deleted}");
            ShowBackupResultDialog("Dọn dẹp backup dư thừa", result);
        }

        // ══════════════════════════════════════════════════════════════════════
        // ĐỒNG BỘ TỪ TOOL KHÁC (MaxCare / FPlus / MetaMax)
        // ══════════════════════════════════════════════════════════════════════
        private void OpenSyncFromOtherTool(string tool)
        {
            try
            {
                var f = new fSyncOtherTool(tool, _platform);
                f.ShowDialog(_form);
                if (f.IsOk)
                {
                    _ = LoadFolders();
                    _ = LoadAccounts();
                }
            }
            catch (Exception ex)
            {
                AntdHelper.MsgError(_form, $"Lỗi mở đồng bộ {tool}: {ex.Message}");
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // 3. LỌC TRÙNG TÀI KHOẢN (với xác nhận đưa vào thùng rác)
        // ══════════════════════════════════════════════════════════════════════
        private void FilterDuplicateAccounts()
        {
            var seen       = new HashSet<string>();
            var duplicates = new List<Account>();
            foreach (var a in _accounts)
            {
                string key = a.Uid ?? "";
                if (!string.IsNullOrEmpty(key) && !seen.Add(key))
                    duplicates.Add(a);
            }
            if (!duplicates.Any()) { AntdHelper.MsgSuccess(_form, "Không có tài khoản trùng uid."); return; }

            if (!AntdHelper.Confirm(_form, "Lọc trùng tài khoản",
                $"Tìm thấy {duplicates.Count} tài khoản trùng uid.\nĐưa {duplicates.Count} tài khoản này vào thùng rác?")) return;

            var ids = duplicates.Select(a => a.Id).ToList();
            if (_accountContext.UpdateIsViewFalse(ids))
            {
                _ = LoadAccounts();
                AntdHelper.MsgSuccess(_form, $"Đã đưa {duplicates.Count} tài khoản trùng vào thùng rác.");
            }
            else AntdHelper.MsgError(_form, "Đã xảy ra lỗi khi cập nhật.");
        }

        // ══════════════════════════════════════════════════════════════════════
        // 4. XÓA TK BỊ CHECKPOINT
        // ══════════════════════════════════════════════════════════════════════
        private void TrashCheckpointAccounts()
        {
            var ids = _accounts
                .Where(x => x.IsView && x.State != null && x.State.StartsWith("CP", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (!ids.Any()) { AntdHelper.MsgWarn(_form, "Không có tài khoản bị checkpoint."); return; }
            if (!AntdHelper.Confirm(_form, "Xóa tk bị checkpoint",
                $"Đưa {ids.Count} tài khoản bị checkpoint vào thùng rác?")) return;
            if (_accountContext.UpdateIsViewFalse(ids.Select(a => a.Id).ToList()))
            {
                _ = LoadAccounts();
                AntdHelper.MsgSuccess(_form, $"Đã đưa {ids.Count}/{_accounts.Count} tài khoản vào thùng rác.");
            }
            else AntdHelper.MsgError(_form, "Đã xảy ra lỗi.");
        }

        // ══════════════════════════════════════════════════════════════════════
        // 5. XÓA TK BỊ CHẶN TƯƠNG TÁC
        // ══════════════════════════════════════════════════════════════════════
        private void TrashBlockedAccounts()
        {
            var ids = _accounts
                .Where(x => x.IsView && x.State?.Equals("Block", StringComparison.OrdinalIgnoreCase) == true)
                .ToList();
            if (!ids.Any()) { AntdHelper.MsgWarn(_form, "Không có tài khoản bị chặn tương tác."); return; }
            if (!AntdHelper.Confirm(_form, "Xóa tk bị chặn tương tác",
                $"Đưa {ids.Count} tài khoản bị Block vào thùng rác?")) return;
            if (_accountContext.UpdateIsViewFalse(ids.Select(a => a.Id).ToList()))
            {
                _ = LoadAccounts();
                AntdHelper.MsgSuccess(_form, $"Đã đưa {ids.Count}/{_accounts.Count} tài khoản vào thùng rác.");
            }
            else AntdHelper.MsgError(_form, "Đã xảy ra lỗi.");
        }

        // ══════════════════════════════════════════════════════════════════════
        // 6. XÓA TK VÀO THÙNG RÁC (bôi đen)
        // ══════════════════════════════════════════════════════════════════════
        private void TrashSelectedAccounts()
        {
            var ids = dataGridView1.SelectedRows
                .Cast<DataGridViewRow>()
                .Select(r => r.DataBoundItem as Account)
                .OfType<Account>()
                .Where(a => a.IsView)
                .Select(a => a.Id)
                .ToList();
            if (!ids.Any()) { AntdHelper.MsgWarn(_form, "Vui lòng bôi đen tài khoản cần xóa vào thùng rác."); return; }
            int total = dataGridView1.SelectedRows.Count;
            if (!AntdHelper.Confirm(_form, "Xóa vào thùng rác", $"Đưa {ids.Count}/{total} tài khoản vào thùng rác?")) return;
            if (_accountContext.UpdateIsViewFalse(ids))
            {
                _ = LoadAccounts();
                AntdHelper.MsgSuccess(_form, $"Đã đưa {ids.Count}/{total} tài khoản vào thùng rác.");
            }
            else AntdHelper.MsgError(_form, "Đã xảy ra lỗi.");
        }

        // ══════════════════════════════════════════════════════════════════════
        // 7. XÓA TÀI KHOẢN VĨNH VIỄN (yêu cầu mật khẩu Subdy)
        // ══════════════════════════════════════════════════════════════════════
        private void DeleteAccountsPermanently()
        {
            var ids = dataGridView1.SelectedRows
                .Cast<DataGridViewRow>()
                .Select(r => r.DataBoundItem as Account)
                .OfType<Account>()
                .Select(a => a.Id)
                .ToList();
            if (!ids.Any()) { AntdHelper.MsgWarn(_form, "Vui lòng bôi đen tài khoản cần xóa vĩnh viễn."); return; }
            if (!AntdHelper.Confirm(_form, "Xóa vĩnh viễn",
                $"Bạn sắp xóa vĩnh viễn {ids.Count} tài khoản khỏi database.\nHành động này không thể hoàn tác. Tiếp tục?")) return;
            if (!VerifySubdyPassword()) return;
            if (_accountContext.DeleteByIds(ids))
            {
                _ = LoadAccounts();
                AntdHelper.MsgSuccess(_form, $"Đã xóa vĩnh viễn {ids.Count} tài khoản.");
            }
            else AntdHelper.MsgError(_form, "Đã xảy ra lỗi khi xóa.");
        }

        // Dropdown kịch bản (toolbar): mass-apply NameScript cho tất cả account trong bindingList hiện tại.
        public void ApplyScriptToAll(string scriptName)
        {
            if (string.IsNullOrEmpty(scriptName) || bindingList == null) return;
            var toUpdate = new List<Account>();
            foreach (var acc in bindingList)
            {
                if (acc == null) continue;
                if (acc.NameScript == scriptName) continue;
                acc.NameScript = scriptName;
                toUpdate.Add(acc);
            }
            if (toUpdate.Any()) _accountContext.Update(toUpdate);
            dataGridView1.Refresh();
        }

        // Dropdown "Tùy chọn": reload bindingList từ DB để restore NameScript gốc.
        public void RestoreScriptsFromDb() => _ = LoadAccounts();

        #region ==== Tour targets ====
        // Nhóm (folder): nút quản lý nhóm là AntdUI.Button được tạo động bởi SsaTheme
        public Control TourBtnFolderManager => this.Controls.Find("ssaBtnFolderMgr", true).FirstOrDefault();
        public Control TourCboGroup => select1;                 // Dropdown chọn nhóm
        // Kịch bản & cài đặt
        public Control TourBtnJobSettings => button4;           // Cài đặt jobs
        public Control TourBtnGeneralSettings => button5;       // Cài đặt chung
        public Control TourBtnInteract => button6;              // Tương tác
        // Chạy / thêm tài khoản / tìm kiếm
        public Control TourBtnRun => button7;                   // Chạy
        public Control TourBtnStop => button8;                  // Dừng
        public Control TourBtnImportAccount => button16;        // + Thêm tài khoản
        public Control TourInputSearch => input6;               // Tìm kiếm
        // Danh sách
        public Control TourCboFilter => cboFilterAccount;       // Lọc tài khoản
        public Control TourBtnReload => button9;                // Tải lại
        public Control TourBtnToggleCols => this.Controls.Find("ssaBtnColumns", true).FirstOrDefault(); // "Hiển thị" (tạo động)
        public Control TourDataGrid => dataGridView1;           // Bảng tài khoản
        public ToolStrip TourToolStripStats => toolStrip1;      // Thanh thống kê
        #endregion
    }
}