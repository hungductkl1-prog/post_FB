using AntdUI;
using Facebook_Farm_NewFeed_PostStory.Utils;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Services;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;
using System.Windows.Forms;
using Facebook_Farm_NewFeed_PostStory.Utils.Design;

namespace Facebook_Farm_NewFeed_PostStory.Views.Forms
{
    public partial class fSelectBrandModel : AntdUI.Window
    {
        public string Brands = string.Empty;
        public fSelectBrandModel(string brand)
        {
            InitializeComponent();
            LoadBrands(brand);
            checkBox1.CheckedChanged += checkBox1_CheckedChanged;
            dgvDevices.CellClick += DgvDevices_CellClick;
            FontUtil.ApplyFontToAllControls(this);
        }
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            var isChecked = checkBox1.Checked;
            foreach (DataGridViewRow row in dgvDevices.Rows)
            {
                row.Cells["colCheckbox_Device"].Value = isChecked;
            }
        }
        private void LoadBrands(string brands)
        {
            dgvDevices.Rows.Clear();
            int i = 1;
            int indexChecked = 1;
            foreach (var brand in DeviceServices.Brands.Split('|'))
            {
                bool isCheked = brands.Contains(brand);
                if (isCheked)
                {
                    indexChecked++;
                }
                dgvDevices.Rows.Add(brands.Contains(brand), i, brand);
                i++;
            }
            if (indexChecked == 39)
            {
                checkBox1.Checked = true;
            }
            else
            {
                checkBox1.Checked = false;
            }
            label1.Text = $"Đã chọn ({indexChecked}/38)";
        }
        private void DgvDevices_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                var row = dgvDevices.Rows[e.RowIndex];

                // Toggle checkbox ở cột đầu tiên
                bool current = Convert.ToBoolean(row.Cells["colCheckbox_Device"].Value);
                row.Cells["colCheckbox_Device"].Value = !current;

                // Đếm lại số lượng brand đã chọn
                int checkedCount = 0;
                foreach (DataGridViewRow r in dgvDevices.Rows)
                {
                    if (Convert.ToBoolean(r.Cells["colCheckbox_Device"].Value))
                        checkedCount++;
                }

                checkBox1.Checked = (checkedCount == dgvDevices.Rows.Count);
                label1.Text = $"Đã chọn ({checkedCount}/38)";
            }
        }
        private void btn_setting_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button9_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dgvDevices.Rows)
            {
                if (!Convert.ToBoolean(row.Cells["colCheckbox_Device"].Value)) continue;
                Brands += row.Cells["colStatus"].Value.ToString() + "|";
            }
            if (string.IsNullOrEmpty(Brands))
            {
                AntdHelper.NotifyWarn(this, "Cảnh báo", "Vui lòng chọn ít nhất một brand.");
                return;
            }
            Brands = Brands.TrimEnd('|');
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
