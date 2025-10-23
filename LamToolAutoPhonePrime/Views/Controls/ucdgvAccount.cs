using AntdUI;
using AutoAndroid;
using LamToolAutoPhonePrime.Utils;
using LamToolAutoPhonePrime.Views.Forms;
using Sunny.Subd.Core.Facebook;
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
        private readonly ScriptContext _scriptContext;

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

            // Visual setup
            dataGridView1.EnableHeadersVisualStyles = false;
            dataGridView1.BorderStyle = BorderStyle.None;
            dataGridView1.AutoGenerateColumns = false;
            dataGridView1.AlternatingRowsDefaultCellStyle.BackColor = Color.White;

            var defaultFont = new Font(FontUtil._fontSemiBold, 9F, FontStyle.Bold);
            dataGridView1.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = ColorTranslator.FromHtml("#1A1A1A"),
                SelectionBackColor = Color.FromArgb(0, 120, 215),
                SelectionForeColor = Color.White,
                Font = defaultFont
            };
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

            // Ensure double buffering
            typeof(DataGridView).InvokeMember("DoubleBuffered",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
                null, dataGridView1, new object[] { true });

            menulist = null;
            CreateMenuStrip();
            try
            {
                typeof(DataGridView).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(dataGridView1, true, null);
                this.GetType().GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
                   ?.SetValue(this, true, null);
            }
            catch
            {
                // ignore in designer if reflection not allowed
            }

            dataGridViewCheckBoxColumn1.Width = 40;
            dataGridViewCheckBoxColumn1.MinimumWidth = 40;
            dataGridViewCheckBoxColumn1.Resizable = DataGridViewTriState.True;

            dataGridViewTextBoxColumn1.Width = 40;
            dataGridViewTextBoxColumn1.MinimumWidth = 40;
            dataGridViewTextBoxColumn1.Resizable = DataGridViewTriState.False;
            tableLayoutPanel1.Resize += tableLayoutPanel1_Resize;
            ControlHelper.LoadConfigColums(dataGridView1, new List<string> { nameof(Account.Id), nameof(Account.ColorType), nameof(Account.Running) });
            FontUtil.ApplyFontToAllControls(this);
        }

        private void tableLayoutPanel1_Resize(object sender, EventArgs e)
        {
            AdjustTableLayoutColumns();
        }

        private void LoadColumnsDataGridView()
        {
            var style = new DataGridViewCellStyle
            {
                Font = new Font(FontUtil._fontSemiBold, 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215)
            };

            dataGridViewCheckBoxColumn1.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font(FontUtil._fontSemiBold, 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            dataGridViewTextBoxColumn1.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font(FontUtil._fontSemiBold, 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
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
                                                                                                                                                                                                                                                                                        (nameof(Account.Result), "Xu", "Xu của server job", true),
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
            int columnCount = 3;
            int fixedWidth = 320;
            int totalFixedWidth = columnCount * fixedWidth;

            int extraSpace = tableLayoutPanel1.Width - totalFixedWidth;
            int spacing = Math.Max(0, extraSpace / (columnCount - 1)); // 2 gaps between 3 columns

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

            // move panels into main columns (0,2,4)
            tableLayoutPanel1.Controls.Clear();
            tableLayoutPanel1.Controls.Add(panel1, 0, 0);
            tableLayoutPanel1.Controls.Add(panel2, 2, 0);
            tableLayoutPanel1.Controls.Add(panel3, 4, 0);
        }

        public void SaveConfig()
        {
            _configHelper?.ControlClosing(null, null);
        }

        private void DataGridView_SelectionChanged(object sender, EventArgs e)
        {
            if (_suppressSelectionChanged) return;
            int selectedRowCount = dataGridView1.SelectedRows.Count;
            ControlHelper.SetToolStripLabelTextSafe(toolStripLabel4, selectedRowCount.ToMoneyString());
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
                    // Background for running
                    row.DefaultCellStyle.BackColor = acc.Running ? Color.Khaki : Color.White;

                    // Foreground color based on ColorType
                    switch (acc.ColorType)
                    {
                        case 1:
                            row.DefaultCellStyle.ForeColor = Color.Red;
                            break;
                        case 2:
                            row.DefaultCellStyle.ForeColor = Color.Green;
                            break;
                        default:
                            row.DefaultCellStyle.ForeColor = ColorTranslator.FromHtml("#1A1A1A");
                            break;
                    }

                    // Ensure selection colors remain readable
                    row.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 215);
                    row.DefaultCellStyle.SelectionForeColor = Color.White;
                }
            }
            catch
            {
                // Swallow to avoid drawing issues
            }
        }

        private void uiDataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            // Keep it lightweight: only handle a few special columns to avoid repeated heavy work
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            // Example: ensure STT column is centered (if it exists)
            var col = dataGridView1.Columns[e.ColumnIndex];
            if (col != null && col.DataPropertyName == nameof(Account.STT))
            {
                e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            // When the Checked column is being formatted, update checked count in a throttled manner:
            if (_colIndexChecked != -1 && e.ColumnIndex == _colIndexChecked)
            {
                // Update label for checked accounts (lightweight)
                ControlHelper.SetToolStripLabelTextSafe(toolStripLabel6, $"{_accounts.Count(x => x.Checked)}");
            }

            // When Running column is being formatted, update running count label occasionally
            if (_colIndexRunning != -1 && e.ColumnIndex == _colIndexRunning)
            {
                ControlHelper.SetToolStripLabelTextSafe(toolStripLabel2, $"{_accounts.Count(x => x.Running)}");
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
            var folders = await Task.Run(() => _folderContext.GetByType(_platform));
            Invoke(new Action(async () =>
            {
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
                select1.SelectedIndex = 0;
                await LoadAccounts();
            }));
        }

        private async Task LoadJobService()
        {
            await Task.Run(() =>
            {
                var items = new List<string>();
                switch (_platform)
                {
                    case PlatformModel.Instagram:
                        items.AddRange(JobServices.TypesInstagram);
                        break;
                    case PlatformModel.Facebook:
                        {
                            if (Globals.User.Role != "admin")
                            {
                                JobServices.TypesFacebook.Remove(JobServices.GoLike);
                                JobServices.TypesFacebook.Remove(JobServices.SeedingVip);
                            }
                            items.AddRange(JobServices.TypesFacebook);
                            break;
                        }
                    case PlatformModel.TikTok:
                        items.AddRange(JobServices.TypesTikTok);
                        break;
                }

                var typejobs = JobServices.GetTypeJobByPlatformt(_platform)
                           .Select(j => j.ToLower())
                           .ToHashSet();
                foreach (ToolStripItem item in toolStripDropDownButton1.DropDownItems)
                {
                    if (item.Name.Contains("total", StringComparison.OrdinalIgnoreCase))
                        continue;
                    var jobName = item.Name.Split('_')[0].ToLower();
                    item.Visible = typejobs.Contains(jobName);
                }
                var scripts = _scriptContext.GetByPlatform(_platform);
                if (scripts.Count > 0)
                {
                    var scriptNames = scripts
      .Where(x => !string.IsNullOrEmpty(x.Name))
      .Select(x => x.Name)
      .ToList();

                    items.AddRange(scriptNames);
                }
                Invoke(new Action(() =>
                {
                    select4.Items.Clear();
                    select4.Items.AddRange(items.ToArray());
                    if (string.IsNullOrEmpty(select4.Text)) select4.SelectedIndex = 0;
                }));
            });
        }

        private void select1_SelectedIndexChanged(object sender, AntdUI.IntEventArgs e)
        {
            if (select1.SelectedIndex == -1) return;
            switch (select1.Text)
            {
                case "[ Chọn nhiều nhóm ]":
                case "[ Chọn theo uid ]":
                case "[ Tất cả các nhóm ]":
                    button1.Enabled = button2.Enabled = false;
                    break;
                default:
                    button1.Enabled = button2.Enabled = true;
                    break;
            }
            Invoke(new Action(async () =>
            {
                await LoadAccounts();
            }));
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
            if (CommonMethod.ShowConfirmWarning(
                $"Bạn có chắc chắn muốn xóa nhóm tài khoản [{select1.Text}] ?", "Cảnh báo"))
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
                            int index = 1;
                            foreach (var acc in accounts)
                            {
                                if (acc.State == "LIVE") acc.ColorType = 2;
                                else if (acc.State == "DIE" || acc.State == "CP_282" || acc.State == "CP_956") acc.ColorType = 1;
                                acc.STT = index++;
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
                dataGridView1.DataSource = bindingList;

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
            var excludedHeaders = new HashSet<string> {
                                                                                                                                                                                                                                                                                        dataGridViewCheckBoxColumn1.HeaderText,
                                                                                                                                                                                                                                                                                        dataGridViewTextBoxColumn1.HeaderText,
                                                                                                                                                                                                                                                                                        nameof(Account.Uid),
                                                                                                                                                                                                                                                                                        "Trạng thái",
                                                                                                                                                                                                                                                                                        nameof(Account.Id),
                                                                                                                                                                                                                                                                                        nameof(Account.ColorType),
                                                                                                                                                                                                                                                                                        nameof(Account.Running)
                                                                                                                                                                                                                                                                                    };

            var remainingHeaders = dataGridView1.Columns
                .Cast<DataGridViewColumn>()
                .Where(c => !excludedHeaders.Contains(c.HeaderText))
                .Select(c => c.HeaderText)
                .ToList();
            fViewDataGridView f = new fViewDataGridView(remainingHeaders, dataGridView1.Name);
            f.ShowDialog();
            ControlHelper.LoadConfigColums(dataGridView1, new List<string> { nameof(Account.Id), nameof(Account.ColorType), nameof(Account.Running) });
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
            if (SubdyHelper.JobServiceByPlatform(_platform).Contains(select4.Text.Trim()))
            {
                fSettingJob f = new fSettingJob(_platform, select4.Text.Trim());
                f.ShowDialog();
            }
            else
            {
                var script = _scriptContext.GetByName(select4.Text.Trim(), _platform);
                if (script != null)
                {
                    fChiTietKichBan f = new fChiTietKichBan(script.Id);
                    f.ShowDialog();
                    await LoadJobService();
                }
                else
                {
                    CommonMethod.ShowMessageWarning("Vui lòng chọn dịch vụ hoặc kịch bản để thiết lập");
                }
            }
        }

        private void Enable(bool enable)
        {
            button7.Enabled = enable;
            button8.Enabled = !enable;
            button9.Enabled = enable;
            panel1.Enabled = enable;
            panel2.Enabled = enable;
            panel3.Enabled = enable;
            button16.Enabled = enable;
            panel3.Enabled = enable;
        }

        private async Task StartSeedingVip()
        {

        }

        private async void button7_Click(object sender, EventArgs e)
        {
            try
            {
                Enable(false);
                if (!_accounts.Any(x => x.Checked))
                {
                    CommonMethod.ShowMessageWarning("Vui lòng chọn ít nhất 1 tài khoản để bắt đầu");
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
                if (model.JobService == JobServices.SeedingVip)
                {
                    List<string> uids = AccountServices.Accounts.Select(x => x.Uid).ToList();
                    List<JobModel> jobs = new List<JobModel>();
                    var accs = new List<AccountsVip>();
                    foreach (var acc in AccountServices.Accounts)
                    {
                        accs.Add(new AccountsVip
                        {
                            Uid = acc.Uid,
                            State = "Online",
                            DeviceId = Globals.DeviceId,
                            DateAt = DateTime.Now
                        });
                    }
                    var stopWatch = System.Diagnostics.Stopwatch.StartNew();
                    while (!ct.IsCancellationRequested)
                    {
                        foreach (var ac in accs)
                        {
                            ac.DateAt = DateTime.Now;
                        }
                        await LamToolClient.UpsertAccountsVipAsync(accs);
                        jobs = await LamToolClient.GetTaskByUids(uids);
                        if (jobs.Any())
                        {
                            LamToolClient.JobVip.Clear();
                            LamToolClient.JobVip.AddRange(jobs);
                            foreach (var device in DeviceServices.DeviceModels.Where(x => x.Checked))
                            {
                                tasks.Add(Task.Run(async () =>
                                {
                                    await SeedingAccountVip(ct, device, model, jobs);
                                }));
                            }
                            await Task.WhenAll(tasks);
                            tasks.Clear();
                        }
                        AccountServices.Accounts.Clear();
                        AccountServices.Accounts = _accounts.Where(x => x.Checked).ToList();
                        for (int i = 300; i > 0; i--)
                        {
                            if (ct.IsCancellationRequested) break;
                            foreach (var acc in AccountServices.Accounts)
                            {
                                acc.Status = $"Đang chờ {i} giây lấy job...";
                            }
                            await Task.Delay(1000, ct);
                        }
                    }
                }
                else
                {
                    while (_accounts.Any(x => x.Checked))
                    {
                        foreach (var device in DeviceServices.DeviceModels.Where(x => x.Checked))
                        {
                            tasks.Add(Task.Run(async () =>
                            {
                                await RunningThread(ct, device, model);
                            }));
                        }
                        await Task.WhenAll(tasks);
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

        public async Task SeedingAccountVip(CancellationToken ct, DeviceModel device, ConfigModel model, List<JobModel> jobs)
        {
            ADBClient client = new ADBClient(device);
            ChangeLanguageService changeLanguage = new ChangeLanguageService(client);
            await changeLanguage.Change("en", "US");
            await client.TurnOnADBKeyboard();
            MainService service = new MainService(_platform, client, model, ct);
            await service.RunAsync();
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
                model.SettingJob = SettingsTool.GetSettings($"{nameof(fSettingJob)}_{_platform}_{select4.Text.Trim()}", true);
                model.JobService = select4.Text.Trim();
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
                        CommonMethod.ShowMessageError("File Gmail không tồn tại");
                        return null;
                    }
                    var lines = File.ReadAllLines(fileGmail);
                    if (!lines.Any())
                    {
                        CommonMethod.ShowMessageError("File Gmail hết gmail");
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

        private async Task RunningThread(CancellationToken ct, DeviceModel device, ConfigModel model)
        {
            ADBClient client = new ADBClient(device);
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
                client.AppClear(FacebookHander.Package(_platform));
            }
        }

        private bool SeleceterDevice()
        {
            _form._ucDevices.button2.Visible = true;
            _form._ucDevices.button3.Visible = true;
            fAddUsercontrol f = new fAddUsercontrol("SelectDevices", _platform, _form._ucDevices);
            f.ShowDialog();
            _form._ucDevices.Dock = DockStyle.Fill;
            _form.pContent.Controls.Add(_form._ucDevices);
            if (f.DialogResult != DialogResult.OK)
            {
                return false;
            }
            return true;
        }

        private void CreateMenuStrip()
        {
            string tick1svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"m424-312 282-282-56-56-226 226-114-114-56 56 170 170ZM200-120q-33 0-56.5-23.5T120-200v-560q0-33 23.5-56.5T200-840h560q33 0 56.5 23.5T840-760v560q0 33-23.5 56.5T760-120H200Zm0-80h560v-560H200v560Zm0-560v560-560Z\"/></svg>";

            var items = new List<AntdUI.IContextMenuStripItem>
{
new AntdUI.ContextMenuStripItem("Chọn").SetIcon(tick1svg).SetSub
(
new AntdUI.ContextMenuStripItem("Tất cả").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M268-240 42-466l57-56 170 170 56 56-57 56Zm226 0L268-466l56-57 170 170 368-368 56 57-424 424Zm0-226-57-56 198-198 57 56-198 198Z\"/></svg>"),
new AntdUI.ContextMenuStripItem("Bôi đen").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M655-200 513-342l56-56 85 85 170-170 56 57-225 226Zm0-320L513-662l56-56 85 85 170-170 56 57-225 226ZM80-280v-80h360v80H80Zm0-320v-80h360v80H80Z\"/></svg>")
),
new AntdUI.ContextMenuStripItem("Bỏ chọn tất cả").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M200-120q-33 0-56.5-23.5T120-200v-560q0-33 23.5-56.5T200-840h560q33 0 56.5 23.5T840-760v560q0 33-23.5 56.5T760-120H200Zm0-80h560v-560H200v560Z\"/></svg>"),
new AntdUI.ContextMenuStripItem("Sao chép").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M760-200H320q-33 0-56.5-23.5T240-280v-560q0-33 23.5-56.5T320-920h280l240 240v400q0 33-23.5 56.5T760-200ZM560-640v-200H320v560h440v-360H560ZM160-40q-33 0-56.5-23.5T80-120v-560h80v560h440v80H160Zm160-800v200-200 560-560Z\"/></svg>").SetSub
(
new AntdUI.ContextMenuStripItem("Tùy chọn").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M440-160q-17 0-28.5-11.5T400-200v-240L168-736q-15-20-4.5-42t36.5-22h560q26 0 36.5 22t-4.5 42L560-440v240q0 17-11.5 28.5T520-160h-80Zm40-308 198-252H282l198 252Zm0 0Z\"/></svg>"),
new AntdUI.ContextMenuStripItem("Uid").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M480-480q-66 0-113-47t-47-113q0-66 47-113t113-47q66 0 113 47t47 113q0 66-47 113t-113 47ZM160-160v-112q0-34 17.5-62.5T224-378q62-31 126-46.5T480-440q66 0 130 15.5T736-378q29 15 46.5 43.5T800-272v112H160Zm80-80h480v-32q0-11-5.5-20T700-306q-54-27-109-40.5T480-360q-56 0-111 13.5T260-306q-9 5-14.5 14t-5.5 20v32Zm240-320q33 0 56.5-23.5T560-640q0-33-23.5-56.5T480-720q-33 0-56.5 23.5T400-640q0 33 23.5 56.5T480-560Zm0-80Zm0 400Z\"/></svg>"),
new AntdUI.ContextMenuStripItem("Mật khẩu").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M80-200v-80h800v80H80Zm46-242-52-30 34-60H40v-60h68l-34-58 52-30 34 58 34-58 52 30-34 58h68v60h-68l34 60-52 30-34-60-34 60Zm320 0-52-30 34-60h-68v-60h68l-34-58 52-30 34 58 34-58 52 30-34 58h68v60h-68l34 60-52 30-34-60-34 60Zm320 0-52-30 34-60h-68v-60h68l-34-58 52-30 34 58 34-58 52 30-34 58h68v60h-68l34 60-52 30-34-60-34 60Z\"/></svg>"),
new AntdUI.ContextMenuStripItem("2FA").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M240-160h480v-400H240v400Zm240-120q33 0 56.5-23.5T560-360q0-33-23.5-56.5T480-440q-33 0-56.5 23.5T400-360q0 33 23.5 56.5T480-280ZM240-160v-400 400Zm0 80q-33 0-56.5-23.5T160-160v-400q0-33 23.5-56.5T240-640h280v-80q0-83 58.5-141.5T720-920q83 0 141.5 58.5T920-720h-80q0-50-35-85t-85-35q-50 0-85 35t-35 85v80h120q33 0 56.5 23.5T800-560v400q0 33-23.5 56.5T720-80H240Z\"/></svg>"),
new AntdUI.ContextMenuStripItem("Email").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M160-160q-33 0-56.5-23.5T80-240v-480q0-33 23.5-56.5T160-800h640q33 0 56.5 23.5T880-720v480q0 33-23.5 56.5T800-160H160Zm320-280L160-640v400h640v-400L480-440Zm0-80 320-200H160l320 200ZM160-640v-80 480-400Z\"/></svg>"),
new AntdUI.ContextMenuStripItem("Mật khẩu email").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"m658-127-64-47 61-85-99-32 24-77 100 33v-105h80v105l100-33 24 77-99 32 61 85-64 47-62-85-62 85Zm-458 7q-33 0-56.5-23.5T120-200v-560q0-33 23.5-56.5T200-840h560q33 0 56.5 23.5T840-760v226q-19-9-39-14.5t-41-8.5v-203H200v360h168q9 27 30 47t47 28q-3 20-4 40.5t2 40.5q-36-7-67.5-26.5T320-320H200v120h253q7 22 16 42t22 38H200Zm0-80h253-253Zm80-410h400v-80H280v80Zm0 140h237q27-29 60.5-49t72.5-31H280v80Z\"/></svg>"),
new AntdUI.ContextMenuStripItem("Cookie").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M480-80q-83 0-156-31.5T197-197q-54-54-85.5-127T80-480q0-75 29-147t81-128.5q52-56.5 125-91T475-881q21 0 43 2t45 7q-9 45 6 85t45 66.5q30 26.5 71.5 36.5t85.5-5q-26 59 7.5 113t99.5 56q1 11 1.5 20.5t.5 20.5q0 82-31.5 154.5t-85.5 127q-54 54.5-127 86T480-80Zm-60-480q25 0 42.5-17.5T480-620q0-25-17.5-42.5T420-680q-25 0-42.5 17.5T360-620q0 25 17.5 42.5T420-560Zm-80 200q25 0 42.5-17.5T400-420q0-25-17.5-42.5T340-480q-25 0-42.5 17.5T280-420q0 25 17.5 42.5T340-360Zm260 40q17 0 28.5-11.5T640-360q0-17-11.5-28.5T600-400q-17 0-28.5 11.5T560-360q0 17 11.5 28.5T600-320ZM480-160q122 0 216.5-84T800-458q-50-22-78.5-60T683-603q-77-11-132-66t-68-132q-80-2-140.5 29t-101 79.5Q201-644 180.5-587T160-480q0 133 93.5 226.5T480-160Zm0-324Z\"/></svg>"),
new AntdUI.ContextMenuStripItem("Token").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M480-80 120-280v-400l360-200 360 200v400L480-80ZM364-590q23-24 53-37t63-13q33 0 63 13t53 37l120-67-236-131-236 131 120 67Zm76 396v-131q-54-14-87-57t-33-98q0-11 1-20.5t4-19.5l-125-70v263l240 133Zm40-206q33 0 56.5-23.5T560-480q0-33-23.5-56.5T480-560q-33 0-56.5 23.5T400-480q0 33 23.5 56.5T480-400Zm40 206 240-133v-263l-125 70q3 10 4 19.5t1 20.5q0 55-33 98t-87 57v131Z\"/></svg>"),
new AntdUI.ContextMenuStripItem("Proxy").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M480-80 120-280v-400l360-200 360 200v400L480-80ZM364-590q23-24 53-37t63-13q33 0 63 13t53 37l120-67-236-131-236 131 120 67Zm76 396v-131q-54-14-87-57t-33-98q0-11 1-20.5t4-19.5l-125-70v263l240 133Zm40-206q33 0 56.5-23.5T560-480q0-33-23.5-56.5T480-560q-33 0-56.5 23.5T400-480q0 33 23.5 56.5T480-400Zm40 206 240-133v-263l-125 70q3 10 4 19.5t1 20.5q0 55-33 98t-87 57v131Z\"/></svg>"),
new AntdUI.ContextMenuStripItem("Trạng thái").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M280-320q17 0 28.5-11.5T320-360q0-17-11.5-28.5T280-400q-17 0-28.5 11.5T240-360q0 17 11.5 28.5T280-320Zm-40-120h80v-200h-80v200Zm160 80h320v-80H400v80Zm0-160h320v-80H400v80ZM160-160q-33 0-56.5-23.5T80-240v-480q0-33 23.5-56.5T160-800h640q33 0 56.5 23.5T880-720v480q0 33-23.5 56.5T800-160H160Zm0-80h640v-480H160v480Zm0 0v-480 480Z\"/></svg>")
),

new AntdUI.ContextMenuStripItem("Cập nhật dữ liệu").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M467-120q-73-1-136-14t-110-34.5q-47-21.5-74-50T120-280q0 33 27 61.5t74 50Q268-147 331-134t136 14Zm-15-200q-38-2-73.5-6.5t-67.5-12q-32-7.5-60-17.5t-51-23q23 13 51 23t60 17.5q32 7.5 67.5 12T452-320Zm28-279q89 0 179-26.5T760-679q-11-29-100.5-55T480-760q-91 0-178.5 25.5T200-679q14 27 101.5 53.5T480-599Zm220 479h40v-164l72 72 28-28-120-120-120 120 28 28 72-72v164Zm20 80q-83 0-141.5-58.5T520-240q0-83 58.5-141.5T720-440q83 0 141.5 58.5T920-240q0 83-58.5 141.5T720-40ZM443-201q3 22 9 42t15 39q-73-1-136-14t-110-34.5q-47-21.5-74-50T120-280v-400q0-66 105.5-113T480-840q149 0 254.5 47T840-680v187q-19-9-39-15t-41-9v-62q-52 29-124 44t-156 15q-85 0-157-15t-123-44v101q51 47 130.5 62.5T480-400h11q-13 18-22.5 38T452-320q-76-4-141-18.5T200-379v99q7 13 30 26.5t56 24q33 10.5 73.5 18T443-201Z\"/></svg>").SetSub
(
new AntdUI.ContextMenuStripItem("Cập nhật Proxy").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M480-80q-139-35-229.5-159.5T160-516v-244l320-120 320 120v244q0 152-90.5 276.5T480-80Zm0-84q97-30 162-118.5T718-480H480v-315l-240 90v207q0 7 2 18h238v316Z\"/></svg>"),
new AntdUI.ContextMenuStripItem("Cập nhật theo uid").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M400-720q-33 0-56.5-23.5T320-800q0-33 23.5-56.5T400-880q33 0 56.5 23.5T480-800q0 33-23.5 56.5T400-720Zm260 480q42 0 71-29t29-71q0-42-29-71t-71-29q-42 0-71 29t-29 71q0 42 29 71t71 29ZM864-80 756-188q-22 14-46 21t-50 7q-75 0-127.5-52.5T480-340q0-75 52.5-127.5T660-520q75 0 127.5 52.5T840-340q0 26-7 50t-21 46l108 108-56 56Zm-424 0v-121q15 24 35.5 44t44.5 36v41h-80Zm-160 0v-520q-61-5-121-14.5T40-640l20-80q84 23 168.5 31.5T400-680q87 0 171.5-8.5T740-720l20 80q-59 16-119 25.5T520-600v41q-54 35-87 92.5T400-340v10q0 5 1 10h-41v240h-80Z\"/></svg>")
),


new AntdUI.ContextMenuStripItem("Xóa tài khoản vào thùng rác").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M280-720v520-520Zm170 600H280q-33 0-56.5-23.5T200-200v-520h-40v-80h200v-40h240v40h200v80h-40v172q-17-5-39.5-8.5T680-560v-160H280v520h132q6 21 16 41.5t22 38.5Zm-90-160h40q0-63 20-103.5l20-40.5v-216h-80v360Zm160-230q17-11 38.5-22t41.5-16v-92h-80v130ZM680-80q-83 0-141.5-58.5T480-280q0-83 58.5-141.5T680-480q83 0 141.5 58.5T880-280q0 83-58.5 141.5T680-80Zm66-106 28-28-74-74v-112h-40v128l86 86Z\"/></svg>"),
new AntdUI.ContextMenuStripItem("Xóa tài khoản vĩnh viễn").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"m376-300 104-104 104 104 56-56-104-104 104-104-56-56-104 104-104-104-56 56 104 104-104 104 56 56Zm-96 180q-33 0-56.5-23.5T200-200v-520h-40v-80h200v-40h240v40h200v80h-40v520q0 33-23.5 56.5T680-120H280Zm400-600H280v520h400v-520Zm-400 0v520-520Z\"/></svg>"),
new AntdUI.ContextMenuStripItem("Cập nhật token job").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M467-120q-73-1-136-14t-110-34.5q-47-21.5-74-50T120-280q0 33 27 61.5t74 50Q268-147 331-134t136 14Zm-15-200q-38-2-73.5-6.5t-67.5-12q-32-7.5-60-17.5t-51-23q23 13 51 23t60 17.5q32 7.5 67.5 12T452-320Zm28-279q89 0 179-26.5T760-679q-11-29-100.5-55T480-760q-91 0-178.5 25.5T200-679q14 27 101.5 53.5T480-599Zm220 479h40v-164l72 72 28-28-120-120-120 120 28 28 72-72v164Zm20 80q-83 0-141.5-58.5T520-240q0-83 58.5-141.5T720-440q83 0 141.5 58.5T920-240q0 83-58.5 141.5T720-40ZM443-201q3 22 9 42t15 39q-73-1-136-14t-110-34.5q-47-21.5-74-50T120-280v-400q0-66 105.5-113T480-840q149 0 254.5 47T840-680v187q-19-9-39-15t-41-9v-62q-52 29-124 44t-156 15q-85 0-157-15t-123-44v101q51 47 130.5 62.5T480-400h11q-13 18-22.5 38T452-320q-76-4-141-18.5T200-379v99q7 13 30 26.5t56 24q33 10.5 73.5 18T443-201Z\"/></svg>"),
new AntdUI.ContextMenuStripItem("Khôi phục về nhóm cũ").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M440-320h80v-166l64 62 56-56-160-160-160 160 56 56 64-62v166ZM280-120q-33 0-56.5-23.5T200-200v-520h-40v-80h200v-40h240v40h200v80h-40v520q0 33-23.5 56.5T680-120H280Zm400-600H280v520h400v-520Zm-400 0v520-520Z\"/></svg>"),
};

            if (_platform == PlatformModel.Facebook)
            {
                items.Add(
                    new AntdUI.ContextMenuStripItem("Đăng kí tài khoản")
                        .SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M720-400v-120H600v-80h120v-120h80v120h120v80H800v120h-80Zm-360-80q-66 0-113-47t-47-113q0-66 47-113t113-47q66 0 113 47t47 113q0 66-47 113t-113 47ZM40-160v-112q0-34 17.5-62.5T104-378q62-31 126-46.5T360-440q66 0 130 15.5T616-378q29 15 46.5 43.5T680-272v112H40Zm80-80h480v-32q0-11-5.5-20T580-306q-54-27-109-40.5T360-360q-56 0-111 13.5T140-306q-9 5-14.5 14t-5.5 20v32Zm240-320q33 0 56.5-23.5T440-640q0-33-23.5-56.5T360-720q-33 0-56.5 23.5T280-640q0 33 23.5 56.5T360-560Zm0-80Zm0 400Z\"/></svg>")
                );
            }
            menulist = items.ToArray();
        }

        private void Control_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && menulist != null)
            {
                AntdUI.ContextMenuStrip.Config config = new AntdUI.ContextMenuStrip.Config(this, RightKey, menulist);
                config.Font = new Font(FontUtil._fontSemiBold, 8f, FontStyle.Bold);
                AntdUI.ContextMenuStrip.open(config);
            }
        }

        private void RightKey(AntdUI.ContextMenuStripItem it)
        {
            if (it.Text.Equals("Tất cả"))
            {
                _accounts.ForEach(x => x.Checked = true);
            }
            else if (it.Text.Equals("Bôi đen"))
            {
                _accounts.ForEach(x => x.Checked = false);
                foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                {
                    if (row.DataBoundItem is Account account)
                    {
                        account.Checked = true;
                    }
                }
            }
            else if (it.Text.Equals("Bỏ chọn tất cả"))
            {
                _accounts.ForEach(x => x.Checked = false);
            }
            else if (it.Text.Equals("Uid"))
            {
                ConvertHelper.CopyFormat(string.Join("|", nameof(Account.Uid)), dataGridView1);
            }
            else if (it.Text.Equals("Mật khẩu"))
            {
                ConvertHelper.CopyFormat(string.Join("|", nameof(Account.Password)), dataGridView1);
            }
            else if (it.Text.Equals("2FA"))
            {
                ConvertHelper.CopyFormat(string.Join("|", nameof(Account.TowFA)), dataGridView1);
            }
            else if (it.Text.Equals("Email"))
            {
                ConvertHelper.CopyFormat(string.Join("|", nameof(Account.Email)), dataGridView1);
            }
            else if (it.Text.Equals("Mật khẩu email"))
            {
                ConvertHelper.CopyFormat(string.Join("|", nameof(Account.PassMail)), dataGridView1);
            }
            else if (it.Text.Equals("Cookie"))
            {
                ConvertHelper.CopyFormat(string.Join("|", nameof(Account.Cookie)), dataGridView1);
            }
            else if (it.Text.Equals("Token"))
            {
                ConvertHelper.CopyFormat(string.Join("|", nameof(Account.Token)), dataGridView1);
            }
            else if (it.Text.Equals("Proxy"))
            {
                ConvertHelper.CopyFormat(string.Join("|", nameof(Account.Proxy)), dataGridView1);
            }
            else if (it.Text.Equals("Trạng thái"))
            {
                ConvertHelper.CopyFormat(string.Join("|", nameof(Account.Status)), dataGridView1);
            }
            else if (it.Text.Equals("Cập nhật Proxy"))
            {
                List<Guid> ids = new List<Guid>();
                foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                {
                    if (row.DataBoundItem is Account account)
                    {
                        ids.Add(account.Id);
                    }
                }
                if (!ids.Any())
                {
                    CommonMethod.ShowMessageWarning("Vui lòng select dòng cần cập nhật.");
                    return;
                }
                fImportProxy f = new fImportProxy(ids);
                f.ShowDialog();
                _ = LoadAccounts();
            }
            else if (it.Text.Equals("Cập nhật theo uid"))
            {
                fAddAccount fAdd = new fAddAccount(_platform, false);
                fAdd.ShowDialog();
                _ = LoadAccounts();
            }
            
            else if (it.Text.Equals("Xóa tài khoản vĩnh viễn"))
            {
                List<Guid> ids = new List<Guid>();
                foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                {
                    if (row.DataBoundItem is Account account)
                    {
                        ids.Add(account.Id);
                    }
                }
                if (!ids.Any())
                {
                    CommonMethod.ShowMessageWarning("Vui lòng select dòng cần xóa.");
                    return;
                }
                if (!CommonMethod.ShowConfirmWarning($"Bạn có chắc chắn muốn xóa {ids.Count} tài khoản."))
                {
                    return;
                }
                if (_accountContext.DeleteByIds(ids))
                {
                    CommonMethod.ShowMessageSuccess($"Đã xóa thành công.");
                }
                else
                {
                    CommonMethod.ShowMessageWarning($"Đã xảy ra lỗi.");
                }
                _ = LoadAccounts();
            }
            else if (it.Text.Equals("Xóa tài khoản vào thùng rác"))
            {
                List<Guid> ids = new List<Guid>();
                foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                {
                    if (row.DataBoundItem is Account account)
                    {
                        if (!account.IsView)
                        {
                            continue;
                        }
                        ids.Add(account.Id);
                    }
                }
                if (!ids.Any())
                {
                    CommonMethod.ShowMessageWarning("Vui lòng select dòng cần xóa tài khoản vào thùng rác.");
                    return;
                }
                if (!CommonMethod.ShowConfirmWarning($"Bạn có chắc chắn muốn xóa {ids.Count} tài khoản vào thùng rác."))
                {
                    return;
                }
                if (_accountContext.UpdateIsViewFalse(ids))
                {
                    CommonMethod.ShowMessageSuccess($"Đã xóa thành công.");
                }
                else
                {
                    CommonMethod.ShowMessageWarning($"Đã xảy ra lỗi.");
                }
                _ = LoadAccounts();
            }
            else if (it.Text.Equals("Khôi phục về nhóm cũ"))
            {
                List<Guid> ids = new List<Guid>();
                foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                {
                    if (row.DataBoundItem is Account account)
                    {
                        if (account.IsView)
                        {
                            continue;
                        }
                        ids.Add(account.Id);
                    }
                }
                if (!ids.Any())
                {
                    CommonMethod.ShowMessageWarning("Vui lòng select dòng cần khôi phục tài khoản.");
                    return;
                }
                if (!CommonMethod.ShowConfirmWarning($"Bạn có chắc chắn muốn khôi phục {ids.Count} tài khoản."))
                {
                    return;
                }
                if (_accountContext.UpdateIsViewTrue(ids))
                {
                    CommonMethod.ShowMessageSuccess($"Đã khôi phục thành công.");
                }
                else
                {
                    CommonMethod.ShowMessageWarning($"Đã xảy ra lỗi.");
                }
                _ = LoadAccounts();
            }
            else if (it.Text.Equals("Cập nhật token job"))
            {
                List<string> ids = new List<string>();
                foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                {
                    if (row.DataBoundItem is Account account)
                    {
                        ids.Add(account.Id.ToString());
                    }
                }
                if (!ids.Any())
                {
                    CommonMethod.ShowMessageWarning("Vui lòng select dòng cần cập nhật.");
                    return;
                }
                fUpdateData f = new fUpdateData(ids, fUpdateData.TokenJob, select4.Text);
                f.ShowDialog();
                _ = LoadAccounts();
            }
            else if (it.Text.Equals("Đăng kí tài khoản"))
            {
                _ = Reg();
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
            var rows = _jobHistoryContext.GetJobTotals(_platform, select4.Text.Trim(), DateTime.Now.ToString("dd/MM/yyyy"));

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
                foreach (ToolStripMenuItem item in toolStripDropDownButton1.DropDownItems)
                {
                    string name = item.Name.Split("_").First().ToLower();
                    if (name != kv.Key) continue;
                    item.Text = text;
                    ControlHelper.SetToolStripMenuItemTextSafe(item, text);
                    break;
                }
            }
            ControlHelper.SetToolStripLabelTextSafe(toolStripLabel16, today.ToMoneyString());
            ControlHelper.SetToolStripMenuItemTextSafe(JobTotal_toolStripMenuItem, $"Job Total: {success}/{fail}");
        }

        private void button8_Click(object sender, EventArgs e)
        {
            Globals.CancellationTokenSource.Cancel();
            button8.Enabled = false;
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
            dataGridView1.Invoke((Delegate)(() =>
            {
                bindingList = new SortableBindingList<Account>(accounts);
                dataGridView1.DataSource = bindingList;
            }));
        }

        private async void button6_Click(object sender, EventArgs e)
        {
            if (_platform == PlatformModel.Facebook)
            {
                new fQuanLyKichBan(PlatformModel.Facebook).ShowDialog();
                await LoadJobService();
            }
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
    }

}