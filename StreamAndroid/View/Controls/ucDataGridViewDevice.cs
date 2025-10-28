using StreamAndroid.Services;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Logs;
using Sunny.Subdy.Common.Services;
using Sunny.Subdy.Data.Models;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

namespace StreamAndroid
{
    public partial class ucDataGridViewDevice : UserControl
    {
        public event EventHandler<DeviceSelectionChangedEventArgs> SelectionChangedEvent;
        public event EventHandler<DeviceSelectionChangedEventArgs> CheckedChangedEvent;
        public SortableBindingList<DeviceModel> bindingList;
        public ucDataGridViewDevice()
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


            dataGridView1.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (dataGridView1.IsCurrentCellDirty)
                {
                    dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };
            bindingList = new SortableBindingList<DeviceModel>(DeviceManagerService.DeviceModels);
            dataGridView1.DataSource = bindingList;
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
            dataGridViewTextBoxColumn1.DataPropertyName = nameof(DeviceModel.Id);

            var columns = new List<(string Name, string Header, string Tooltip)>
                    {
                        (nameof(DeviceModel.Serial), nameof(DeviceModel.Serial), "DeviceId thiết bị"),
                        (nameof(DeviceModel.NameDevice), "Tên", "Tên thiết bị"),
                        (nameof(DeviceModel.OS), nameof(DeviceModel.OS), "Android version"),
                        (nameof(DeviceModel.State), "Tình trạng", "Tình trạng thiết bị"),
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

        public bool _suppressSelectionChanged;
        private void DataGridView_SelectionChanged(object sender, EventArgs e)
        {
            if (_suppressSelectionChanged) return;

            // Lấy danh sách devices được select
            var selectedDevices = dataGridView1.SelectedRows
                .Cast<DataGridViewRow>()
                .Select(row => row.DataBoundItem as DeviceModel)
                .Where(device => device != null)
                .ToList();

            // Raise event với thông tin chi tiết
            SelectionChangedEvent?.Invoke(this, new DeviceSelectionChangedEventArgs
            {
                SelectedDevices = selectedDevices,
                SelectionCount = selectedDevices.Count
            });
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
            var checkedDevices = dataGridView1.Rows
     .Cast<DataGridViewRow>()
     .Select(row => row.DataBoundItem as DeviceModel)
     .Where(device => device != null && device.Checked)
     .ToList();
            CheckedChangedEvent?.Invoke(this, new DeviceSelectionChangedEventArgs
            {
                SelectedDevices = checkedDevices,
                SelectionCount = checkedDevices.Count
            });
        }
    }
}
