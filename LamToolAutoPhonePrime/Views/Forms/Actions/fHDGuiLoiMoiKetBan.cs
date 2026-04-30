using AntdUI;
using LamToolAutoPhonePrime.Utils;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Json;
using Sunny.Subdy.Common.Models;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;
using System.Windows.Forms;

namespace LamToolAutoPhonePrime.Views.Forms.Actions
{
    public partial class fHDGuiLoiMoiKetBan : Form
    {
        private string scriptId;
        private string actionId;
        private ConfigHelper jsonConfig;
        private ScriptActionContext _context;
        public fHDGuiLoiMoiKetBan(string scriptId, string actionId = "")
        {
            InitializeComponent();
            FontUtil.ApplyFontToAllControls(this);
            ckbDefault.CheckedChanged += ckbDefault_CheckedChanged;
            ckbChiDinh.CheckedChanged += ckbDefault_CheckedChanged;
            ckbTuKhoa.CheckedChanged += ckbDefault_CheckedChanged;
            txtLinks.TextChanged += txtLinks_TextChanged;
            _context = new ScriptActionContext();
            this.scriptId = scriptId;
            this.actionId = actionId;
            string configJson = "";
            if (string.IsNullOrEmpty(actionId))
            {
                var index = _context.GetCountName(FacebookFarmingType.DictionariesAction[FacebookFarmingType.HDGuiLoiMoiKetBan]);
                if (index == 0)
                {
                    txtTenHanhDong.Text = FacebookFarmingType.DictionariesAction[FacebookFarmingType.HDGuiLoiMoiKetBan];
                }
                else
                {
                    txtTenHanhDong.Text = $"{FacebookFarmingType.DictionariesAction[FacebookFarmingType.HDGuiLoiMoiKetBan]} ({(index)})";
                }
                btnSave.Text = "Thêm";
            }
            else
            {
                var action = _context.GetById(Guid.Parse(actionId));
                txtTenHanhDong.Text = action.Name;
                configJson = action?.Json ?? "";
                btnSave.Text = "Lưu";
            }
            jsonConfig = new ConfigHelper(this, configJson);
            btnCancel.Click += btnCancel_Click;
            btnSave.Click += btnSave_Click;
            btn_setting.Click += btnCancel_Click;
            LoadEnable();
        }
        private void LoadEnable()
        {
            groupBox2.Enabled = !ckbDefault.Checked;
            if (ckbChiDinh.Checked)
            {
                label18.Text = "Danh sách uid profile (0):";
                numericUpDown1.Enabled = false;
                numericUpDown2.Enabled = false;
            }
            else if (ckbTuKhoa.Checked)
            {
                label18.Text = "Danh sách từ khóa (0):";
                label15.Text = "Số lượng kết bạn/từ khóa:";
                numericUpDown1.Enabled = true;
                numericUpDown2.Enabled = true;
            }
            else if (radioButton1.Checked)
            {
                label18.Text = "Danh sách uid group (0):";
                label15.Text = "Số lượng kết bạn/group:";
                numericUpDown1.Enabled = true;
                numericUpDown2.Enabled = true;
            }
            SubdyHelper.UpdateItemCount(txtLinks, label18);
        }

        private void ckbDefault_CheckedChanged(object sender, EventArgs e)
        {
            LoadEnable();
        }
        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            string actionName = txtTenHanhDong.Text.Trim();
            if (actionName == "")
            {
                AntdHelper.NotifyWarn(this, "Cảnh báo", "Vui lòng nhập tên hành động!");
                return;
            }
            string configJson = jsonConfig.GetJsonString();

            if (string.IsNullOrEmpty(actionId))
            {
                if (CommonMethod.ShowConfirmWarning("Bạn có muốn thêm hành động mới?"))
                {
                    var scriptAction = _context.GetByScriptId(Guid.Parse(scriptId));
                    var index = (scriptAction != null && scriptAction.Count > 0) ? scriptAction.Max(a => a.ByOrder) + 1 : 1;
                    var action = new ScriptAction
                    {
                        Id = Guid.NewGuid(),
                        Name = actionName,
                        Type = FacebookFarmingType.HDGuiLoiMoiKetBan,
                        Json = configJson,
                        Platform = PlatformModel.Facebook,
                        ScriptId = Guid.Parse(scriptId),
                        ByOrder = index
                    };
                    if (_context.Add(action))
                    {
                        DialogResult = DialogResult.OK;
                        Close();
                    }
                    else
                    {
                        AntdHelper.NotifyError(this, "Thao tác thất bại", "Thêm thất bại, vui lòng thử lại sau!");
                    }
                }
            }
            else // cập nhật
            {
                if (CommonMethod.ShowConfirmWarning("Bạn có muốn cập nhật hành động?"))
                {
                    var scriptAction = _context.GetByScriptId(Guid.Parse(scriptId));
                    var index = (scriptAction != null && scriptAction.Count > 0) ? scriptAction.Max(a => a.ByOrder) + 1 : 1;
                    var action = _context.GetById(Guid.Parse(actionId));
                    action.Name = actionName;
                    action.Json = configJson;
                    if (_context.Update(action))
                    {
                        DialogResult = DialogResult.OK;
                        Close();
                    }
                    else
                    {
                        AntdHelper.NotifyError(this, "Thao tác thất bại", "Cập nhật thất bại, vui lòng thử lại sau!");
                    }
                }
            }
        }

        private void txtLinks_TextChanged(object sender, EventArgs e)
        {
            SubdyHelper.UpdateItemCount(txtLinks, label18);
        }

        private void btnCancel_Click_1(object sender, EventArgs e)
        {

        }
    }
}
