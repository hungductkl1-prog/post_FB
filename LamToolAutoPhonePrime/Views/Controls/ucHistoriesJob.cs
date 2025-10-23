using AntdUI;
using LamToolAutoPhonePrime.Utils;
using Sunny.Subdy.Common.API;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Models;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LamToolAutoPhonePrime.Views.Controls
{
    public partial class ucHistoriesJob : UserControl
    {
        private readonly JobHistoryContext _Context;
        private List<JobHistory> _jobHistories;
        public ucHistoriesJob()
        {
            _Context = new JobHistoryContext();
            InitializeComponent();
            LoadColumnsDataGridView();
            select4.Items.Clear();
            select4.Items.Add(PlatformModel.Facebook);
            select4.Items.Add(PlatformModel.TikTok);
            select4.Items.Add(PlatformModel.Instagram);
            select4.SelectedIndex = 0;
            SelectPlatform();
            dateTimePicker1.Format = DateTimePickerFormat.Custom;
            dateTimePicker1.CustomFormat = "dd/MM/yyyy";
            dateTimePicker1.Value = DateTime.Now.AddDays(-6);
            dateTimePicker2.Format = DateTimePickerFormat.Custom;
            dateTimePicker2.CustomFormat = "dd/MM/yyyy";
            dateTimePicker2.Value = DateTime.Now;
            FontUtil.ApplyFontToAllControls(this);

            dataGridView1.DefaultCellStyle.BackColor = Color.White;
            dataGridView1.DefaultCellStyle.ForeColor = Color.DarkGray;
            dataGridView1.DefaultCellStyle.Font = new Font(FontUtil._fontSemiBold, 9F, FontStyle.Bold);
            dataGridView1.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 215);
            dataGridView1.DefaultCellStyle.SelectionForeColor = Color.White;
            dataGridView1.DefaultCellStyle.ForeColor = ColorTranslator.FromHtml("#1A1A1A");
            dataGridView1.AlternatingRowsDefaultCellStyle.BackColor = Color.White;
            dataGridView1.AutoGenerateColumns = false;
            typeof(DataGridView).InvokeMember("DoubleBuffered",
    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
    null, dataGridView1, new object[] { true });

            dataGridView1.BorderStyle = BorderStyle.None;
        }
        public void UpdateView()
        {
            (int distinctUidCountFB, int totalCountFB) = _Context.GetUidStatsByPlatform(PlatformModel.Facebook);
            ControlHelper.SetLabelTextSafe(label3, distinctUidCountFB.ToMoneyString());
            ControlHelper.SetLabelTextSafe(label7, totalCountFB.ToMoneyString());
            (distinctUidCountFB, totalCountFB) = _Context.GetUidStatsByPlatform(PlatformModel.TikTok);
            ControlHelper.SetLabelTextSafe(label9, distinctUidCountFB.ToMoneyString());
            ControlHelper.SetLabelTextSafe(label5, totalCountFB.ToMoneyString());
            (distinctUidCountFB, totalCountFB) = _Context.GetUidStatsByPlatform(PlatformModel.Instagram);
            ControlHelper.SetLabelTextSafe(label14, distinctUidCountFB.ToMoneyString());
            ControlHelper.SetLabelTextSafe(label12, totalCountFB.ToMoneyString());
        }
        private void SelectPlatform()
        {
            string value = select4.Text;
            select1.Items.Clear();
            switch (value)
            {
                case PlatformModel.Instagram:
                    select1.Items.AddRange(JobServices.TypesInstagram.ToArray());
                    break;
                case PlatformModel.Facebook:
                    {
                        if (Globals.User.Role != "admin")
                        {
                            JobServices.TypesFacebook.Remove(JobServices.GoLike);
                        }
                        select1.Items.AddRange(JobServices.TypesFacebook.ToArray());

                        break;
                    }

                case PlatformModel.TikTok:
                    select1.Items.AddRange(JobServices.TypesTikTok.ToArray());
                    break;
            }

            select1.SelectedIndex = 0;
            select1.Text = select1.Items[0].ToString();


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
                Font = new Font(FontUtil._fontSemiBold, 9F, FontStyle.Bold)

            };
            style.ForeColor = Color.FromArgb(0, 120, 215);
            dataGridViewTextBoxColumn1.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font(FontUtil._fontSemiBold, 9F, FontStyle.Bold),
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
                    col.Name == nameof(JobHistory.Description) ? 300 : 100,
                    col.Name == nameof(JobHistory.Description) ? DataGridViewAutoSizeColumnMode.Fill : DataGridViewAutoSizeColumnMode.None,
                    style
                ));
            }

            dataGridView1.Columns.AddRange(colDefs.ToArray());
        }

        private void select4_SelectedIndexChanged(object sender, AntdUI.IntEventArgs e)
        {
            SelectPlatform();
        }

        private async void button7_Click(object sender, EventArgs e)
        {
            await LoadAccounts();
        }
        private async Task LoadAccounts()
        {
            string fromStr = dateTimePicker1.Value.ToString("yyyy-MM-dd");
            string toStr = dateTimePicker2.Value.ToString("yyyy-MM-dd");
            string uid = input1.Text.Trim();
            try
            {
                _jobHistories = new List<JobHistory>();

                string query = $@"
        SELECT * FROM {nameof(JobHistory)} 
        WHERE {nameof(JobHistory.Platform)} = @platform 
          AND {nameof(JobHistory.Service)} = @service 
          AND date(substr({nameof(JobHistory.DateTime)}, 7, 4) || '-' || substr({nameof(JobHistory.DateTime)}, 4, 2) || '-' || substr({nameof(JobHistory.DateTime)}, 1, 2))
              BETWEEN @fromDate AND @toDate";
                var parameters = new Dictionary<string, object>
                {
                    ["@platform"] = select4.Text.Trim(),
                    ["@service"] = select1.Text.Trim(),
                    ["@fromDate"] = fromStr,
                    ["@toDate"] = toStr
                };
                if (!string.IsNullOrEmpty(uid))
                {
                    query += $" AND {nameof(JobHistory.Uid)} = @uid";
                    parameters["@uid"] = uid;
                }

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
                ControlHelper.SetToolStripLabelTextSafe(toolStripLabel8, _jobHistories.Count.ToMoneyString());
                dataGridView1.Invoke((Delegate)(() =>
                {
                    var bindingList = new SortableBindingList<JobHistory>(_jobHistories);
                    dataGridView1.DataSource = bindingList;
                }));
                var stateCounts = _jobHistories
                 .GroupBy(x => x.Status)
                 .Select(g => (g.Key ?? "JobFail", g.Count()))
                 .ToList();
                int otherCount = 0;
                List<string> adds = new List<string>();
                foreach (var stateCount in stateCounts)
                {
                    switch (stateCount.Item1)
                    {
                        case "Success":
                            ControlHelper.SetToolStripLabelTextSafe(toolStripLabel10, stateCount.Item2.ToMoneyString());
                            break;
                        default:
                            ControlHelper.SetToolStripLabelTextSafe(toolStripLabel12, stateCount.Item2.ToMoneyString());
                            break;
                    }

                }
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    string type = row.Cells[$"col_{nameof(JobHistory.Status)}"].Value.ToString();
                    switch (type)
                    {
                        case "Success":
                            foreach (DataGridViewCell cell in row.Cells)
                            {
                                cell.Style.ForeColor = Color.Green;
                                cell.Style.SelectionForeColor = Color.Green;
                                cell.Style.SelectionForeColor = Color.White;
                            }
                            break;
                        default:
                            foreach (DataGridViewCell cell in row.Cells)
                            {
                                cell.Style.ForeColor = Color.Red;
                                cell.Style.SelectionForeColor = Color.Red;
                                cell.Style.SelectionForeColor = Color.White;
                            }
                            break;
                    }
                }
            }
        }


        private void input1_TextChanged(object sender, EventArgs e)
        {

        }

        private async void button8_Click(object sender, EventArgs e)
        {
            if(CommonMethod.ShowConfirmWarning("Bạn có chắc muốn xóa toàn bộ lịch sử làm việc hay không?"))
            {
                _Context.ClearAll();
                await LoadAccounts();
            }
        }
    }
}

