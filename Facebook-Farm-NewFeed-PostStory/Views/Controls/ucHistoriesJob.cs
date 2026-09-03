using AntdUI;
using Facebook_Farm_NewFeed_PostStory.Utils;
using Facebook_Farm_NewFeed_PostStory.Utils.Design;
using Sunny.Subd.Core.Services;
using Sunny.Subdy.Common.API;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Models;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Facebook_Farm_NewFeed_PostStory.Views.Controls
{
    public partial class ucHistoriesJob : UserControl
    {
        private readonly JobHistoryContext _Context;
        private List<JobHistory> _jobHistories;
        /// <summary>Trần số dòng nạp vào lưới — bảo vệ RAM/UI khi lịch sử rất lớn.</summary>
        private const int MaxHistoryRows = 5000;
        // date, totalJobs, coin, uniqueUids, liveUids, dieUids, runSeconds
        private List<(DateTime date, int jobs, double coin, int uids, int live, int die, long runSeconds)> _chartData = new();
        private Dictionary<string, (int success, int fail)> _methodStats = new();

        // (removed: timer thời gian hôm nay)

        public ucHistoriesJob()
        {
            _Context = new JobHistoryContext();
            InitializeComponent();
            LoadColumnsDataGridView();
            select4.Items.Clear();
            select4.Items.Add(PlatformModel.Facebook);
            select4.Items.Add(PlatformModel.Instagram);
            select4.Items.Add(PlatformModel.Threads);
            select4.SelectedIndex = 0;
            selectDateRange.Items.Clear();
            selectDateRange.Items.AddRange(new object[] { "Tất cả", "3 tháng", "1 tháng", "7 ngày", "Hôm nay" });
            selectDateRange.SelectedIndex = 3; // mặc định 7 ngày
            FontUtil.ApplyFontToAllControls(this); Facebook_Farm_NewFeed_PostStory.Utils.Design.VietnameseFont.Enforce(this);

            GridStyleHelper.Apply(dataGridView1);
            dataGridView1.AutoGenerateColumns = false;

            // Row coloring: Success = green, Fail = red
            dataGridView1.CellFormatting += DataGridView1_CellFormatting;

            // Bar chart paint
            panelChartArea.Paint += PanelChartArea_Paint;

            // Empty state: hiển thị hướng dẫn khi chưa có lịch sử (dùng token Design)
            dataGridView1.Paint += (s, e) =>
            {
                if (dataGridView1.Rows.Count == 0)
                {
                    var rect = dataGridView1.ClientRectangle;
                    using var iconFont  = new Font("Segoe UI", 32F);
                    using var titleFont = FontScale.HeadingBold;
                    using var hintFont  = FontScale.Body9;

                    string icon  = "📋";
                    string title = "Chưa có lịch sử hoạt động";
                    string hint  = "Lịch sử sẽ tự động cập nhật khi có tác vụ được thực thi";

                    var iconSize  = e.Graphics.MeasureString(icon,  iconFont);
                    var titleSize = e.Graphics.MeasureString(title, titleFont);
                    var hintSize  = e.Graphics.MeasureString(hint,  hintFont);

                    float totalH = iconSize.Height + titleSize.Height + hintSize.Height + 16;
                    float startY = (rect.Height - totalH) / 2;

                    using var grayBrush = new SolidBrush(ColorPalette.TextTertiary);
                    using var darkBrush = new SolidBrush(ColorPalette.TextSecondary);
                    e.Graphics.DrawString(icon,  iconFont,  grayBrush, (rect.Width - iconSize.Width)  / 2, startY);
                    e.Graphics.DrawString(title, titleFont, darkBrush, (rect.Width - titleSize.Width) / 2, startY + iconSize.Height + 8);
                    e.Graphics.DrawString(hint,  hintFont,  grayBrush, (rect.Width - hintSize.Width)  / 2, startY + iconSize.Height + titleSize.Height + 16);
                }
            };
        }
        public void UpdateView()
        {
            (int distinctUidCountFB, int totalCountFB) = _Context.GetUidStatsByPlatform(PlatformModel.Facebook);
            ControlHelper.SetLabelTextSafe(label3, distinctUidCountFB.ToMoneyString());
            ControlHelper.SetLabelTextSafe(label7, totalCountFB.ToMoneyString());
        }

        /// <summary>Cập nhật card tài khoản: tổng uid / live / die.</summary>
        private void UpdateAccountCard(int total, int live, int die)
        {
            string display = $"{total} • Live: {live} • Die: {die}";
            ControlHelper.SetLabelTextSafe(lblCardTimeValue, display);
        }

        private void DataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0 || e.RowIndex >= dataGridView1.Rows.Count) return;
                var statusCell = dataGridView1.Rows[e.RowIndex].Cells[$"col_{nameof(JobHistory.Status)}"];
                string status = statusCell?.Value?.ToString() ?? "";
                e.CellStyle.ForeColor = status == "Success" ? Color.Green : Color.Red;
                e.CellStyle.SelectionForeColor = Color.White;

                var col = dataGridView1.Columns[e.ColumnIndex];
                if (col != null && col.Name == $"col_{nameof(JobHistory.Description)}" && e.Value is string s && !string.IsNullOrEmpty(s))
                {
                    e.Value = SanitizeDescription(s);
                    e.FormattingApplied = true;
                }
            }
            catch
            {
                // Không để exception vỡ paint pipeline của DataGridView
            }
        }

        private static string SanitizeDescription(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            int max = Math.Min(input.Length, 500);
            var sb = new StringBuilder(max);
            for (int i = 0; i < max; i++)
            {
                char c = input[i];
                if (c == '\r' || c == '\n' || c == '\t') { sb.Append(' '); continue; }
                if (c < 0x20) continue;
                if (c == '{' || c == '}') { sb.Append(' '); continue; }
                sb.Append(c);
            }
            if (input.Length > max) sb.Append("...");
            return sb.ToString();
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
        private void LoadColumnsDataGridView()
        {
            var style = new DataGridViewCellStyle
            {
                Font = FontScale.Body9Bold

            };
            style.ForeColor = Color.FromArgb(0, 120, 215);
            dataGridViewTextBoxColumn1.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = FontScale.Body9Bold,
                ForeColor = Color.FromArgb(0, 120, 215),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            dataGridViewTextBoxColumn1.ToolTipText = "Số thứ tự trong bảng";
            dataGridViewTextBoxColumn1.DataPropertyName = nameof(JobHistory.STT);
            var columns = new List<(string Name, string Header, string Tooltip, bool visible)>
    {
        (nameof(JobHistory.Uid), nameof(JobHistory.Uid), "Uid hoặc username tài khoản", true),
        (nameof(JobHistory.IdJob), nameof(JobHistory.IdJob), nameof(JobHistory.IdJob), true),
        (nameof(JobHistory.IdObject), nameof(JobHistory.IdObject), nameof(JobHistory.IdObject), true),
        (nameof(JobHistory.Method),"Loại Job", nameof(JobHistory.IdObject), true),
        (nameof(JobHistory.Coin),"Xu", "Xu kiếm được", true),
        (nameof(JobHistory.Status),"Tình trạng", nameof(JobHistory.IdObject), true),
        (nameof(JobHistory.DateTime),"Thời gian", nameof(JobHistory.IdObject), true),
        (nameof(JobHistory.Description),"Trạng thái", nameof(JobHistory.IdObject), true),
    };

            var colDefs = new List<DataGridViewColumn>();

            foreach (var col in columns)
            {
                string header = col.Header;
                string tooltip = col.Tooltip;

                colDefs.Add(CreateColumnsDataGridView(
                    col.Name,
                    header,
                    tooltip,
                    col.visible,
                    col.Name == nameof(JobHistory.Description) ? 300 : col.Name == nameof(JobHistory.Coin) ? 60 : 100,
                    col.Name == nameof(JobHistory.Description) ? DataGridViewAutoSizeColumnMode.Fill : DataGridViewAutoSizeColumnMode.None,
                    style
                ));
            }

            dataGridView1.Columns.AddRange(colDefs.ToArray());
        }

        private async void button7_Click(object sender, EventArgs e)
        {
            try
            {
                await AntdUI.Spin.open(this, "Đang tải dữ liệu...", async cfg =>
                {
                    await LoadAccounts();
                });
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[button7_Click] {ex}"); }
        }
        private (string fromStr, string toStr) GetDateRange()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            return selectDateRange.Text switch
            {
                "Tất cả"  => ("2000-01-01", today),
                "3 tháng" => (DateTime.Now.AddMonths(-3).ToString("yyyy-MM-dd"), today),
                "1 tháng" => (DateTime.Now.AddMonths(-1).ToString("yyyy-MM-dd"), today),
                "Hôm nay" => (today, today),
                _         => (DateTime.Now.AddDays(-6).ToString("yyyy-MM-dd"), today), // 7 ngày
            };
        }

        private async Task LoadAccounts()
        {
            var (fromStr, toStr) = GetDateRange();
            string uid = input1.Text.Trim();
            try
            {
                _jobHistories = new List<JobHistory>();

                string query = $@"
        SELECT * FROM {nameof(JobHistory)}
        WHERE {nameof(JobHistory.Platform)} = @platform
          AND date(substr({nameof(JobHistory.DateTime)}, 7, 4) || '-' || substr({nameof(JobHistory.DateTime)}, 4, 2) || '-' || substr({nameof(JobHistory.DateTime)}, 1, 2))
              BETWEEN @fromDate AND @toDate";
                var parameters = new Dictionary<string, object>
                {
                    ["@platform"] = select4.Text.Trim(),
                    ["@fromDate"] = fromStr,
                    ["@toDate"] = toStr
                };
                if (!string.IsNullOrEmpty(uid))
                {
                    query += $" AND {nameof(JobHistory.Uid)} = @uid";
                    parameters["@uid"] = uid;
                }

                // Chặn nạp toàn bảng vào RAM: chỉ lấy tối đa MaxHistoryRows bản ghi mới nhất.
                // Lịch sử có thể lên tới hàng trăm nghìn dòng — materialize hết sẽ ngốn RAM + đơ UI.
                query += $" ORDER BY date(substr({nameof(JobHistory.DateTime)}, 7, 4) || '-' || substr({nameof(JobHistory.DateTime)}, 4, 2) || '-' || substr({nameof(JobHistory.DateTime)}, 1, 2)) DESC LIMIT @limit";
                parameters["@limit"] = MaxHistoryRows;

                var accounts = _Context.GetAll(query, parameters);

                if (accounts == null || accounts.Count == 0)
                    return;

                _jobHistories = accounts;
                int index = 1;
                var bindingList = new SortableBindingList<JobHistory>(_jobHistories);
                foreach (var acc in _jobHistories)
                {
                    acc.STT = index++;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi load lịch sử: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Tính summary stats
                int totalJobs = _jobHistories.Count;
                int successJobs = _jobHistories.Count(x => x.Status == "Success");
                int failJobs = totalJobs - successJobs;
                double totalCoin = _jobHistories
                    .Where(x => x.Status == "Success")
                    .Sum(x => { double.TryParse(x.Coin, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var c); return c; });

                // Thống kê tài khoản theo uid (tạm thời)
                var uidList = _jobHistories
                    .Where(x => !string.IsNullOrEmpty(x.Uid))
                    .Select(x => x.Uid!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                int totalAccounts = uidList.Count;
                UpdateAccountCard(totalAccounts, 0, 0);

                // Update 4 summary cards
                ControlHelper.SetLabelTextSafe(lblCardTotalValue, totalJobs.ToMoneyString());
                ControlHelper.SetLabelTextSafe(lblCardSuccessValue, successJobs.ToMoneyString());
                ControlHelper.SetLabelTextSafe(lblCardFailValue, failJobs.ToMoneyString());
                ControlHelper.SetLabelTextSafe(lblCardCoinValue, totalCoin.ToMoneyString());

                // Kiểm tra live/die thực tế với 10 luồng đồng thời
                if (uidList.Count > 0)
                {
                    var liveResult = await CheckLiveService.RunAsync(uidList, cancellationToken: CancellationToken.None);
                    int realLive = liveResult.Count(kv => kv.Value);
                    int realDie = liveResult.Count - realLive;
                    UpdateAccountCard(uidList.Count, realLive, realDie);
                }

                // Update toolbar
                ControlHelper.SetToolStripLabelTextSafe(toolStripLabel8, totalJobs.ToMoneyString());
                ControlHelper.SetToolStripLabelTextSafe(toolStripLabel10, successJobs.ToMoneyString());
                ControlHelper.SetToolStripLabelTextSafe(toolStripLabel12, failJobs.ToMoneyString());
                ControlHelper.SetToolStripLabelTextSafe(toolStripLabel14, totalCoin.ToMoneyString());

                // Cập nhật DataGridView (coloring handled by CellFormatting event)
                dataGridView1.BeginInvoke((Delegate)(() =>
                {
                    var bindingList = new SortableBindingList<JobHistory>(_jobHistories);
                    dataGridView1.SuspendLayout();
                    dataGridView1.DataSource = bindingList;
                    dataGridView1.ResumeLayout();
                }));

                // Chart data: nhóm theo ngày, tính thêm uid/live/die/runSeconds
                _chartData = _jobHistories
                    .Where(x => DateTime.TryParseExact(
                        x.DateTime?.Length >= 10 ? x.DateTime.Substring(0, 10) : x.DateTime,
                        "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out _))
                    .GroupBy(x => DateTime.ParseExact(x.DateTime.Substring(0, 10), "dd/MM/yyyy", null).Date)
                    .OrderBy(g => g.Key)
                    .Select(g =>
                    {
                        double coin = g.Where(j => j.Status == "Success")
                            .Sum(j => { double.TryParse(j.Coin, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var c); return c; });

                        var uidGroups = g.GroupBy(j => j.Uid ?? "").Where(ug => !string.IsNullOrEmpty(ug.Key));
                        int totalUids = uidGroups.Count();
                        int liveUids = uidGroups.Count(ug => ug.Any(j => j.Status == "Success"));
                        int dieUids  = totalUids - liveUids;

                        // Ước tính giờ chạy: mỗi job thành công ~= 1 phút (60s), fail ~= 30s
                        long runSecs = g.Sum(j => j.Status == "Success" ? 60L : 30L);

                        return (g.Key, g.Count(), coin, totalUids, liveUids, dieUids, runSecs);
                    })
                    .ToList();
                panelChartArea.BeginInvoke((Delegate)(() => panelChartArea.Invalidate()));

                // Method stats
                _methodStats = _jobHistories
                    .GroupBy(x => x.Method ?? "Unknown")
                    .ToDictionary(
                        g => g.Key,
                        g => (g.Count(j => j.Status == "Success"), g.Count(j => j.Status != "Success"))
                    );
                flowMethodStats.BeginInvoke((Delegate)(() => RefreshMethodStats()));
            }
        }


        private void input1_TextChanged(object sender, EventArgs e)
        {

        }

        private async void button8_Click(object sender, EventArgs e)
        {
            try
            {
                if (CommonMethod.ShowConfirmWarning("Bạn có chắc muốn xóa toàn bộ lịch sử làm việc hay không?"))
                {
                    _Context.ClearAll();
                    await LoadAccounts();
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[button8_Click] {ex}"); }
        }

        private void RefreshMethodStats()
        {
            flowMethodStats.SuspendLayout();
            // Dispose old controls to avoid handle leaks
            foreach (Control old in flowMethodStats.Controls)
                old.Dispose();
            flowMethodStats.Controls.Clear();

            if (_methodStats == null || _methodStats.Count == 0)
            {
                var emptyLbl = new System.Windows.Forms.Label
                {
                    Text = "Chưa có dữ liệu",
                    ForeColor = Color.DarkGray,
                    Font = new Font("Segoe UI", 8.25F, FontStyle.Italic),
                    AutoSize = false,
                    Size = new Size(196, 24),
                    TextAlign = ContentAlignment.MiddleLeft
                };
                flowMethodStats.Controls.Add(emptyLbl);
                flowMethodStats.ResumeLayout();
                return;
            }

            int panelW = Math.Max(flowMethodStats.ClientSize.Width - 4, 190);

            foreach (var kv in _methodStats.OrderByDescending(x => x.Value.success + x.Value.fail))
            {
                string method = kv.Key ?? string.Empty;
                int success = kv.Value.success;
                int fail = kv.Value.fail;
                int total = success + fail;
                float ratio = total > 0 ? (float)success / total : 0f;
                string methodDisplay = string.IsNullOrEmpty(method)
                    ? "(unknown)"
                    : char.ToUpper(method[0]) + method.Substring(1);

                // Container card
                var card = new AntdUI.Panel
                {
                    Size = new Size(panelW, 58),
                    Back = Color.FromArgb(250, 250, 252),
                    Radius = 8,
                    Padding = new Padding(10, 6, 10, 6),
                    Margin = new Padding(0, 0, 0, 6),
                    BackColor = Color.Transparent
                };

                // Tên method (trái) + counts (phải) trên cùng 1 row
                var headerPanel = new System.Windows.Forms.Panel
                {
                    Dock = DockStyle.Top,
                    Height = 20,
                    BackColor = Color.Transparent
                };

                var lblMethod = new System.Windows.Forms.Label
                {
                    Text = methodDisplay,
                    Font = FontScale.Body9Bold,
                    ForeColor = Color.FromArgb(30, 30, 30),
                    AutoSize = false,
                    Dock = DockStyle.Left,
                    Width = panelW / 2,
                    TextAlign = ContentAlignment.MiddleLeft,
                    BackColor = Color.Transparent
                };

                var lblCounts = new System.Windows.Forms.Label
                {
                    Text = $"✓ {success.ToMoneyString()}  ✗ {fail.ToMoneyString()}",
                    Font = new Font("Segoe UI", FontScale.Body),
                    ForeColor = Color.DarkGray,
                    AutoSize = false,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleRight,
                    BackColor = Color.Transparent
                };

                headerPanel.Controls.Add(lblCounts);
                headerPanel.Controls.Add(lblMethod);

                // Màu bar theo tỉ lệ thành công
                Color fillColor = ratio >= 0.7f
                    ? Color.FromArgb(82, 196, 26)   // xanh lá
                    : ratio >= 0.4f
                        ? Color.FromArgb(250, 173, 20) // vàng
                        : Color.FromArgb(255, 77, 79);  // đỏ

                // AntdUI Progress bar
                var progress = new AntdUI.Progress
                {
                    Dock = DockStyle.Bottom,
                    Height = 8,
                    Value = ratio,
                    Shape = AntdUI.TShapeProgress.Round,
                    Back = Color.FromArgb(220, 220, 220),
                    Fill = fillColor,
                    Margin = new Padding(0, 4, 0, 0)
                };

                card.Controls.Add(progress);
                card.Controls.Add(headerPanel);
                flowMethodStats.Controls.Add(card);
            }

            flowMethodStats.ResumeLayout();
        }

        private void PanelChartArea_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var rect = panelChartArea.ClientRectangle;
            g.Clear(Color.White);

            if (_chartData == null || _chartData.Count == 0)
            {
                using var grayBrush = new SolidBrush(Color.FromArgb(160, 160, 160));
                using var hintFont = new Font("Segoe UI", 9F);
                string msg = "Nhấn Xem để hiển thị biểu đồ";
                var sz = g.MeasureString(msg, hintFont);
                g.DrawString(msg, hintFont, grayBrush, (rect.Width - sz.Width) / 2, (rect.Height - sz.Height) / 2);
                return;
            }

            // padTop chứa nhãn xu phía trên bar; padBottom chứa nhãn ngày
            int padLeft = 52, padRight = 16, padTop = 14, padBottom = 28;
            int chartW = rect.Width - padLeft - padRight;
            int chartH = rect.Height - padTop - padBottom;
            if (chartW <= 0 || chartH <= 0) return;

            double maxCoin = _chartData.Max(x => x.coin);
            if (maxCoin == 0) maxCoin = 1;

            int barCount = _chartData.Count;
            float barWidth = (float)chartW / barCount;
            float barGap = Math.Max(barWidth * 0.12f, 2f);
            float barW = barWidth - barGap * 2;
            if (barW < 2) barW = 2;

            // Fonts dùng chung
            using var gridFont    = new Font("Segoe UI", FontScale.Caption);
            using var labelFont   = new Font("Segoe UI", FontScale.Caption);
            using var dateLabelFont = new Font("Segoe UI", FontScale.Caption, FontStyle.Bold);
            using var metaFont    = new Font("Segoe UI", FontScale.Caption);
            using var gridBrush   = new SolidBrush(Color.FromArgb(150, 150, 150));
            using var coinBrush   = new SolidBrush(Color.FromArgb(0, 90, 180));
            using var hourBrush   = new SolidBrush(Color.FromArgb(30, 144, 255));
            using var dateBrush   = new SolidBrush(Color.FromArgb(70, 70, 70));
            using var liveBrush   = new SolidBrush(Color.FromArgb(82, 196, 26));
            using var dieBrush    = new SolidBrush(Color.FromArgb(255, 77, 79));
            using var uidBrush    = new SolidBrush(Color.FromArgb(120, 120, 120));

            // Grid lines
            using var gridPen = new Pen(Color.FromArgb(230, 230, 230), 1);
            for (int i = 0; i <= 4; i++)
            {
                float y = padTop + chartH - (float)i / 4 * chartH;
                g.DrawLine(gridPen, padLeft, y, padLeft + chartW, y);
                double val = maxCoin * i / 4;
                string valStr = val >= 1000 ? $"{val / 1000:0.#}K" : $"{val:0}";
                var valSz = g.MeasureString(valStr, gridFont);
                g.DrawString(valStr, gridFont, gridBrush, padLeft - valSz.Width - 2, y - valSz.Height / 2);
            }

            // X axis
            using var axisPen = new Pen(Color.FromArgb(200, 200, 200), 1);
            g.DrawLine(axisPen, padLeft, padTop + chartH, padLeft + chartW, padTop + chartH);

            // Bars + labels
            using var barBrush = new SolidBrush(Color.FromArgb(0, 120, 215));

            for (int i = 0; i < barCount; i++)
            {
                var (date, jobs, coin, uids, live, die, runSecs) = _chartData[i];
                float x = padLeft + i * barWidth + barGap;
                float barH = (float)(coin / maxCoin) * chartH;
                float y = padTop + chartH - barH;
                float barCenterX = x + barW / 2;

                // ── Bar với rounded top ──
                if (barH > 0)
                {
                    using var path = new GraphicsPath();
                    float r = Math.Min(4f, barW / 2);
                    path.AddArc(x, y, r * 2, r * 2, 180, 90);
                    path.AddArc(x + barW - r * 2, y, r * 2, r * 2, 270, 90);
                    path.AddLine(x + barW, y + barH, x, y + barH);
                    path.CloseFigure();
                    g.FillPath(barBrush, path);
                }

                // ── Nhãn phía TRÊN bar: xu ──
                if (barW > 18)
                {
                    string coinStr = coin >= 1000 ? $"{coin / 1000:0.#}K xu" : $"{coin:0} xu";
                    var coinSz = g.MeasureString(coinStr, labelFont);
                    float labelY = y - coinSz.Height - 2;
                    if (coinSz.Width <= barW + 8)
                        g.DrawString(coinStr, labelFont, coinBrush, barCenterX - coinSz.Width / 2, labelY);
                }

                float baseY = padTop + chartH;

                // ── Nhãn dưới trục: ngày ──
                string dateStr = date.ToString("dd/MM");
                var dateSz2 = g.MeasureString(dateStr, dateLabelFont);
                g.DrawString(dateStr, dateLabelFont, dateBrush,
                    barCenterX - dateSz2.Width / 2, baseY + 3);
            }
        }
    }
}

