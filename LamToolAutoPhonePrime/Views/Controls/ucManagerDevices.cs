using AntdUI;
using AutoAndroid;
using Emgu.CV.Structure;
using LamToolAutoPhonePrime;
using LamToolAutoPhonePrime.Utils;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Logs;
using Sunny.Subdy.Common.Services;
using System;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO.Compression;
using System.Reflection;
using System.Text;

namespace Sunny.Subdy.UI.View.Pages
{
    public partial class ucManagerDevices : UserControl
    {
        private CancellationTokenSource cancellationTokenSource;
        AntdUI.IContextMenuStripItem[] menulist = { };
        private int _lastCheckedCount = -1;
        public ucManagerDevices(Form form)
        {
            InitializeComponent();

            TryEnableDoubleBuffer(dataGridView1);

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
            dataGridViewCheckBoxColumn1.DataPropertyName = nameof(DeviceModel.Checked);
            // Events
            dataGridView1.SelectionChanged += DataGridView_SelectionChanged;
            dataGridView1.CellFormatting += uiDataGridView1_CellFormatting;
            dataGridView1.RowTemplate.Height = Math.Max(22, dataGridView1.RowTemplate.Height);

            // Ensure double buffering
            typeof(DataGridView).InvokeMember("DoubleBuffered",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
                null, dataGridView1, new object[] { true });

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





            // Light custom border (paint once)
            dataGridView1.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, dataGridView1.ClientRectangle,
                    Color.White, 1, ButtonBorderStyle.Solid, // Left
                    SystemColors.AppWorkspace, 1, ButtonBorderStyle.Solid, // Top
                    Color.White, 1, ButtonBorderStyle.Solid, // Right
                    Color.White, 1, ButtonBorderStyle.Solid  // Bottom
                );
            };

            LoadColumnsDataGridView();

            dataGridView1.MouseClick += Control_MouseClick;

            dataGridView1.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (dataGridView1.IsCurrentCellDirty)
                {
                    dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };

            FontUtil.ApplyFontToAllControls(this);
        }
        private void TryEnableDoubleBuffer(DataGridView dgv)
        {
            try
            {
                typeof(DataGridView).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(dgv, true, null);
            }
            catch { /* best-effort */ }
        }



        private void LoadColumnsDataGridView()
        {
            var style = new DataGridViewCellStyle
            {
                Font = new Font(FontUtil._fontSemiBold, 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            dataGridViewCheckBoxColumn1.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font(FontUtil._fontSemiBold, 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            dataGridViewCheckBoxColumn1.DataPropertyName = nameof(DeviceModel.Checked);
            dataGridViewTextBoxColumn1.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font(FontUtil._fontSemiBold, 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            dataGridViewTextBoxColumn1.ToolTipText = "Số thứ tự trong bảng";
            dataGridViewTextBoxColumn1.DataPropertyName = nameof(DeviceModel.Index);

            var columns = new List<(string Name, string Header, string Tooltip)>
                    {
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

                colDefs.Add(CreateColumnsDataGridView(
                    col.Name,
                    header,
                    tooltip,
                    visible,
                    col.Name == nameof(DeviceModel.Status) ? 300 : 100,
                    col.Name == nameof(DeviceModel.Status) ? DataGridViewAutoSizeColumnMode.Fill : DataGridViewAutoSizeColumnMode.None,
                    style
                ));
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

            // Optimize: only update row.DefaultCellStyle once per row
            var typeCell = row.Cells[$"col_{nameof(DeviceModel.TypeColor)}"].Value;
            if (typeCell != null && !string.IsNullOrEmpty(typeCell.ToString()))
            {
                string raw = typeCell.ToString();

                if (int.TryParse(raw, out int type))
                {
                    switch (type)
                    {
                        case 1:
                            row.DefaultCellStyle.ForeColor = Color.Red;
                            row.DefaultCellStyle.SelectionForeColor = Color.White;
                            break;
                        case 2:
                            row.DefaultCellStyle.ForeColor = Color.Green;
                            row.DefaultCellStyle.SelectionForeColor = Color.White;
                            break;
                        default:
                            row.DefaultCellStyle.ForeColor = ColorTranslator.FromHtml("#1A1A1A");
                            row.DefaultCellStyle.SelectionForeColor = Color.White;
                            break;
                    }
                }
                else if (bool.TryParse(raw, out bool isSpecialBack))
                {
                    row.DefaultCellStyle.BackColor = isSpecialBack ? Color.MediumSpringGreen : Color.White;
                }
            }

            // Update checked count only when changed
            var checkVal = row.Cells[dataGridViewCheckBoxColumn1.Name].Value;
            if (checkVal != null && bool.TryParse(checkVal.ToString(), out bool check))
            {
                int currentChecked = DeviceServices.DeviceModels.Count(x => x.Checked);
                if (currentChecked != _lastCheckedCount)
                {
                    _lastCheckedCount = currentChecked;
                    toolStripLabel10.Text = $"{currentChecked}";
                }
            }
        }

        private async Task LoadDevices()
        {
            try
            {
                // Bind via BindingSource to make UI refresh smoother
                if (dataGridView1.InvokeRequired)
                {
                    dataGridView1.BeginInvoke((Delegate)(() =>
                    {
                        var bindingList = new SortableBindingList<DeviceModel>(DeviceServices.DeviceModels);
                        var bs = new BindingSource { DataSource = bindingList };
                        dataGridView1.DataSource = bs;
                    }));
                }
                else
                {
                    var bindingList = new SortableBindingList<DeviceModel>(DeviceServices.DeviceModels);
                    var bs = new BindingSource { DataSource = bindingList };
                    dataGridView1.DataSource = bs;
                }

                toolStripLabel8.Text = $"{DeviceServices.DeviceModels.Count.ToMoneyString()}";
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
                    ProcessHelper.RunAdbCommand($"-s {device.Serial} shell wm size 1440x2560");
                    ProcessHelper.RunAdbCommand($"-s {device.Serial} shell wm density 560");
                }
            }
            string folderPath = LdPlayerHelper.GetPathFolder().Replace("dnplayer.exe", "");
            if (!Directory.Exists(folderPath)) return;
            var indexs = LdPlayerHelper.GetIndex(Path.Combine(folderPath, "ldconsole.exe"));
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
                        LdPlayerHelper.Close(Path.Combine(folderPath, "ldconsole.exe"), index.ToString());
                        LdPlayerHelper.Config(fileConfig);
                        LdPlayerHelper.Open(Path.Combine(folderPath, "ldconsole.exe"), index.ToString());
                    }
                }));
            }

            await Task.WhenAll(tasks);
            LdPlayerHelper.SortWnd(folderPath);
            DeviceServices.ADBKill();
            DeviceServices.GetDeviceModels();

            if (dataGridView1.InvokeRequired)
            {
                dataGridView1.BeginInvoke((Delegate)(() =>
                {
                    var bindingList = new SortableBindingList<DeviceModel>(DeviceServices.DeviceModels);
                    dataGridView1.DataSource = new BindingSource { DataSource = bindingList };
                }));
            }
            else
            {
                var bindingList = new SortableBindingList<DeviceModel>(DeviceServices.DeviceModels);
                dataGridView1.DataSource = new BindingSource { DataSource = bindingList };
            }

            toolStripLabel8.Text = $"{DeviceServices.DeviceModels.Count.ToMoneyString()}";
        }

        private async void ucManagerDevices_Load(object sender, EventArgs e)
        {
            Enable(false);
            await LoadDevices();
            Enable(true);
        }
        private void Enable(bool enable)
        {
            button53.Enabled = enable;
            button1.Enabled = enable;
            button2.Enabled = enable;
            button3.Enabled = !enable;
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            Enable(false);
            DeviceServices.ADBKill();
            DeviceServices.GetDeviceModels();
            await LoadDevices();
            Enable(true);
        }

        private async void button53_Click(object sender, EventArgs e)
        {
            Enable(false);
            DeviceServices.GetDeviceModels();
            await LoadDevices();
            Enable(true);
        }
        private void CreateMenuStrip()
        {
            string tick1svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"m424-312 282-282-56-56-226 226-114-114-56 56 170 170ZM200-120q-33 0-56.5-23.5T120-200v-560q0-33 23.5-56.5T200-840h560q33 0 56.5 23.5T840-760v560q0 33-23.5 56.5T760-120H200Zm0-80h560v-560H200v560Zm0-560v560-560Z\"/></svg>";

            menulist = new AntdUI.IContextMenuStripItem[]
          {
                        new AntdUI.ContextMenuStripItem("Chọn").SetIcon(tick1svg).SetSub
                        (
                            new AntdUI.ContextMenuStripItem("Tất cả").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M268-240 42-466l57-56 170 170 56 56-57 56Zm226 0L268-466l56-57 170 170 368-368 56 57-424 424Zm0-226-57-56 198-198 57 56-198 198Z\"/></svg>"),
                            new AntdUI.ContextMenuStripItem("Bôi đen").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M655-200 513-342l56-56 85 85 170-170 56 57-225 226Zm0-320L513-662l56-56 85 85 170-170 56 57-225 226ZM80-280v-80h360v80H80Zm0-320v-80h360v80H80Z\"/></svg>")
                        ),
                         new AntdUI.ContextMenuStripItem("Bỏ chọn tất cả").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M200-120q-33 0-56.5-23.5T120-200v-560q0-33 23.5-56.5T200-840h560q33 0 56.5 23.5T840-760v560q0 33-23.5 56.5T760-120H200Zm0-80h560v-560H200v560Z\"/></svg>"),
                         new AntdUI.ContextMenuStripItem("Sao lưu debug").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M480-200q66 0 113-47t47-113v-160q0-66-47-113t-113-47q-66 0-113 47t-47 113v160q0 66 47 113t113 47Zm-80-120h160v-80H400v80Zm0-160h160v-80H400v80Zm80 40Zm0 320q-65 0-120.5-32T272-240H160v-80h84q-3-20-3.5-40t-.5-40h-80v-80h80q0-20 .5-40t3.5-40h-84v-80h112q14-23 31.5-43t40.5-35l-64-66 56-56 86 86q28-9 57-9t57 9l88-86 56 56-66 66q23 15 41.5 34.5T688-640h112v80h-84q3 20 3.5 40t.5 40h80v80h-80q0 20-.5 40t-3.5 40h84v80H688q-32 56-87.5 88T480-120Z\"/></svg>"),
                         new AntdUI.ContextMenuStripItem("Chức năng").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#666666\"><path d=\"M520-600v-240h320v240H520ZM120-440v-400h320v400H120Zm400 320v-400h320v400H520Zm-400 0v-240h320v240H120Zm80-400h160v-240H200v240Zm400 320h160v-240H600v240Zm0-480h160v-80H600v80ZM200-200h160v-80H200v80Zm160-320Zm240-160Zm0 240ZM360-280Z\"/></svg>").SetSub
                        (
                              new AntdUI.ContextMenuStripItem("Hiển thị thiết bị").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" width=\"24px\" height=\"24px\" viewBox=\"0 0 64 64\" aria-hidden=\"true\" role=\"img\" class=\"iconify iconify--emojione\" preserveAspectRatio=\"xMidYMid meet\">\r\n  <circle cx=\"32\" cy=\"32\" r=\"30\" fill=\"#f42f4c\"/>\r\n  <path fill=\"#ffe62e\" d=\"M32 39l9.9 7l-3.7-11.4l9.8-7.4H35.8L32 16l-3.7 11.2H16l9.8 7.4L22.1 46z\"/>\r\n</svg>"),
                            new AntdUI.ContextMenuStripItem("Chuyển ngôn ngữ máy về tiếng việt").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" width=\"24px\" height=\"24px\" viewBox=\"0 0 64 64\" aria-hidden=\"true\" role=\"img\" class=\"iconify iconify--emojione\" preserveAspectRatio=\"xMidYMid meet\">\r\n  <circle cx=\"32\" cy=\"32\" r=\"30\" fill=\"#f42f4c\"/>\r\n  <path fill=\"#ffe62e\" d=\"M32 39l9.9 7l-3.7-11.4l9.8-7.4H35.8L32 16l-3.7 11.2H16l9.8 7.4L22.1 46z\"/>\r\n</svg>"),
                            new AntdUI.ContextMenuStripItem("Chuyển ngôn ngữ máy về tiếng anh").SetIcon("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"24px\" height=\"24px\" viewBox=\"0 -4 28 28\" fill=\"none\">\r\n  <g clip-path=\"url(#clip0_503_3486)\">\r\n    <rect width=\"28\" height=\"20\" rx=\"2\" fill=\"white\"/>\r\n    <mask id=\"mask0_503_3486\" style=\"mask-type:alpha\" maskUnits=\"userSpaceOnUse\" x=\"0\" y=\"0\" width=\"28\" height=\"20\">\r\n      <rect width=\"28\" height=\"20\" rx=\"2\" fill=\"white\"/>\r\n    </mask>\r\n    <g mask=\"url(#mask0_503_3486)\">\r\n      <path fill-rule=\"evenodd\" clip-rule=\"evenodd\" d=\"M28 0H0V1.33333H28V0ZM28 2.66667H0V4H28V2.66667ZM0 5.33333H28V6.66667H0V5.33333ZM28 8H0V9.33333H28V8ZM0 10.6667H28V12H0V10.6667ZM28 13.3333H0V14.6667H28V13.3333ZM0 16H28V17.3333H0V16ZM28 18.6667H0V20H28V18.6667Z\" fill=\"#D02F44\"/>\r\n      <rect width=\"12\" height=\"9.33333\" fill=\"#46467F\"/>\r\n      <g filter=\"url(#filter0_d_503_3486)\">\r\n        </g></g></g></svg>")
                        ),
          };
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
        private async void RightKey(AntdUI.ContextMenuStripItem it)
        {
            dataGridView1.Enabled = false;
            if (it.Text.Equals("Tất cả"))
            {
                DeviceServices.DeviceModels.ForEach(x => x.Checked = true);
            }
            else
            if (it.Text.Equals("Bôi đen"))
            {
                DeviceServices.DeviceModels.ForEach(x => x.Checked = false);
                foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                {
                    if (row.DataBoundItem is DeviceModel account)
                    {
                        account.Checked = true;
                    }
                }
            }
            else if (it.Text.Equals("Hiển thị thiết bị"))
            {
                List<DeviceModel> selectedDevices = new List<DeviceModel>();
                foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                {
                    if (row.DataBoundItem is DeviceModel account)
                    {
                        selectedDevices.Add(account);   
                    }
                }
                Form1 form = new Form1(selectedDevices);
                form.Show(this);
            }
            else
                if (it.Text.Equals("Bỏ chọn tất cả"))
            {
                DeviceServices.DeviceModels.ForEach(x => x.Checked = false);
            }
            else
                if (it.Text.Equals("Sao lưu debug"))
            {
                List<Task> tasks = new List<Task>();
                foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                {
                    if (row.DataBoundItem is DeviceModel device)
                    {
                        tasks.Add(Task.Run(() =>
                        {
                            var client = new ADBClient(device);
                            try
                            {
                                var targetColor = Color.FromArgb(2, 5, 10);
                                Point point = Point.Empty;
                                for (int i = 0; i < 15; i++)
                                {
                                    var filtereds = client.FindColorCoordinates(targetColor, tolerance: 0);
                                    if (!filtereds.Any())
                                    {
                                        Thread.Sleep(1000);
                                        continue;
                                    }
                                    var filtered = filtereds.FindAll(r => r.Rx == 3 && (r.Ry == 2 || r.Ry == 3));
                                    if (!filtered.Any())
                                    {
                                        Thread.Sleep(1000);
                                        continue;
                                    }
                                    point = filtered.First().Center;
                                    client.Click(point.X, point.Y);

                                    break;
                                }
                                // Lấy XML và Screenshot
                                string xml = client.GetXMLSource();

                                // Tạo thư mục
                                string folderName = $"{DateTime.Now:HH-mm-ss dd.MM.yyyy} {device.Serial}";
                                string tempPath = Path.Combine(Path.GetTempPath(), folderName);
                                Directory.CreateDirectory(tempPath);

                                // Lưu XML
                                File.WriteAllText(Path.Combine(tempPath, "xml.xml"), xml, Encoding.UTF8);

                                // Lưu ảnh
                                string imagePath = Path.Combine(tempPath, "screenshot.png");
                                AutoAndroid.FileHelper.DownImage($"{client.ATX._url}/screenshot/0", imagePath);

                                // Tạo file zip ở desktop
                                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                                string zipPath = Path.Combine(desktop, $"{folderName}.zip");
                                if (File.Exists(zipPath)) File.Delete(zipPath);
                                ZipFile.CreateFromDirectory(tempPath, zipPath);

                                // Xóa thư mục tạm
                                Directory.Delete(tempPath, true);
                                client.LogHelper.SUCCESS("Đã lưu ở màn hình.");
                            }
                            catch (Exception ex)
                            {
                                LogManager.Error(ex);
                                client.LogHelper.ERROR(ex.Message);
                            }
                        }));
                    }
                }
                await Task.WhenAll(tasks);

            }
            else
                if (it.Text.Equals("Chuyển ngôn ngữ máy về tiếng việt"))
            {
                List<Task> tasks = new List<Task>();
                foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                {
                    if (row.DataBoundItem is DeviceModel device)
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            var client = new ADBClient(device);
                            try
                            {
                                ChangeLanguageService changeLanguage = new ChangeLanguageService(client);
                                await changeLanguage.Change("vi", "VN");
                                await client.TurnOnADBKeyboard();
                                client.LogHelper.SUCCESS("Đã chuyển ngôn ngữ máy về tiếng việt.");
                            }
                            catch (Exception ex)
                            {
                                LogManager.Error(ex);
                                client.LogHelper.ERROR(ex.Message);
                            }
                        }));
                    }
                }
                await Task.WhenAll(tasks);

            }
            else
                if (it.Text.Equals("Chuyển ngôn ngữ máy về tiếng anh"))
            {
                List<Task> tasks = new List<Task>();
                foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                {
                    if (row.DataBoundItem is DeviceModel device)
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            var client = new ADBClient(device);
                            try
                            {
                                ChangeLanguageService changeLanguage = new ChangeLanguageService(client);
                                await changeLanguage.Change("en", "US");
                                await client.TurnOnADBKeyboard();
                                client.LogHelper.SUCCESS("Đã chuyển ngôn ngữ máy về tiếng anh.");
                            }
                            catch (Exception ex)
                            {
                                LogManager.Error(ex);
                                client.LogHelper.ERROR(ex.Message);
                            }
                        }));
                    }
                }
                await Task.WhenAll(tasks);

            }
            dataGridView1.Enabled = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (!DeviceServices.DeviceModels.Any(x => x.Checked))
            {
                CommonMethod.ShowMessageWarning("Vui lòng chọn ít nhất 1 thiết bị để bắt đầu");
                return;
            }
            Form parentForm = this.FindForm();
            if (parentForm != null)
            {
                parentForm.DialogResult = DialogResult.OK;
                parentForm.Close();
            }
            button2.Visible = false;
            button3.Visible = false;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Form parentForm = this.FindForm();
            if (parentForm != null)
            {
                button2.Visible = false;
                button3.Visible = false;
                parentForm.DialogResult = DialogResult.Cancel;
                parentForm.Close();
            }
        }
    }
}
