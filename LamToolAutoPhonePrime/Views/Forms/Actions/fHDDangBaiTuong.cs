using AntdUI;
using LamToolAutoPhonePrime.Utils;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Json;
using Sunny.Subdy.Common.Models;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;

namespace LamToolAutoPhonePrime.Views.Forms.Actions
{
    public partial class fHDDangBaiTuong : Form
    {
        private string scriptId;
        private string actionId;
        private ConfigHelper jsonConfig;
        private ScriptActionContext _context;
        public fHDDangBaiTuong(string scriptId, string actionId = "")
        {
            InitializeComponent();
            FontUtil.ApplyFontToAllControls(this);
            LamToolAutoPhonePrime.Utils.Design.FormResponsiveHelper.MakeScrollable(this);
            _context = new ScriptActionContext();
            this.scriptId = scriptId;
            this.actionId = actionId;
            string configJson = "";
            if (string.IsNullOrEmpty(actionId))
            {
                var index = _context.GetCountName(FacebookFarmingType.DictionariesAction[FacebookFarmingType.HDDangBaiTuong]);
                if (index == 0)
                {
                    txtTenHanhDong.Text = FacebookFarmingType.DictionariesAction[FacebookFarmingType.HDDangBaiTuong];
                }
                else
                {
                    txtTenHanhDong.Text = $"{FacebookFarmingType.DictionariesAction[FacebookFarmingType.HDDangBaiTuong]} ({(index)})";
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
            LoadEnable();
        }
        private void LoadEnable()
        {
            plVanBan.Enabled = A2009002.Checked;
            panel2.Enabled = checkBox2.Checked;

            panel3.Enabled = checkBox3.Checked;
            plAnh.Enabled = ckbAnh.Checked;

            EDA1511C.Enabled = checkBox6.Checked;
            E31CEB31.Enabled = checkBox11.Checked;
            SubdyHelper.UpdateItemCount(txtLinks, D397662B);
            SubdyHelper.UpdateItemCount(textBox1, label5);
            SubdyHelper.UpdateItemCount(textBox2, label14);
            SubdyHelper.UpdateItemCount(txtComments, lblStatus);
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
                        Type = FacebookFarmingType.HDDangBaiTuong,
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
            SubdyHelper.UpdateItemCount(txtLinks, D397662B);
            SubdyHelper.UpdateItemCount(textBox1, label5);
            SubdyHelper.UpdateItemCount(textBox2, label14);
            SubdyHelper.UpdateItemCount(txtComments, lblStatus);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog f = new System.Windows.Forms.FolderBrowserDialog();
            if (f.ShowDialog() == DialogResult.OK)
            {
                txtPathImageComment.Text = f.SelectedPath;
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog f = new System.Windows.Forms.FolderBrowserDialog();
            if (f.ShowDialog() == DialogResult.OK)
            {
                txtPathAnh.Text = f.SelectedPath;
            }
        }
    }
}
