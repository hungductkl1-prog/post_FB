using AntdUI;
using LamToolAutoPhonePrime.Utils;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Json;
using Sunny.Subdy.Common.Models;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;
using static AntdUI.Modal;

namespace LamToolAutoPhonePrime.Views.Forms.Actions
{
    public partial class fHDTuongTacBaiViet : Form
    {
        private string scriptId;
        private string actionId;
        private ConfigHelper jsonConfig;
        private ScriptActionContext _context;
        public fHDTuongTacBaiViet(string scriptId, string actionId = "")
        {
            InitializeComponent();
            FontUtil.ApplyFontToAllControls(this);
            ckbChiDinh.CheckedChanged += ckbDefault_CheckedChanged;
            ckbTuKhoa.CheckedChanged += ckbDefault_CheckedChanged;
            ckbInteract.CheckedChanged += ckbDefault_CheckedChanged;
            ckbShareWall.CheckedChanged += ckbDefault_CheckedChanged;
            ckbComment.CheckedChanged += ckbDefault_CheckedChanged;
            rdbComment.CheckedChanged += ckbDefault_CheckedChanged;
            radioButton3.CheckedChanged += ckbDefault_CheckedChanged;
            radioButton2.CheckedChanged += ckbDefault_CheckedChanged;
            ckbAnh.CheckedChanged += ckbDefault_CheckedChanged;
            txtLinks.TextChanged += txtLinks_TextChanged;
            txtComments.TextChanged += txtLines_TextChanged;
            _context = new ScriptActionContext();
            this.scriptId = scriptId;
            this.actionId = actionId;
            string configJson = "";
            if (string.IsNullOrEmpty(actionId))
            {
                var index = _context.GetCountName(FacebookFarmingType.DictionariesAction[FacebookFarmingType.HDTuongTacBaiViet]);
                if (index == 0)
                {
                    txtTenHanhDong.Text = FacebookFarmingType.DictionariesAction[FacebookFarmingType.HDTuongTacBaiViet];
                }
                else
                {
                    txtTenHanhDong.Text = $"{FacebookFarmingType.DictionariesAction[FacebookFarmingType.HDTuongTacBaiViet]} ({(index)})";
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
            if (ckbChiDinh.Checked)
            {
                label18.Text = "Link (0):";
            }
            else if (ckbTuKhoa.Checked)
            {
                label18.Text = "Keyword (0):";
            }
            SubdyHelper.UpdateItemCount(txtLinks, label18);
            SubdyHelper.UpdateItemCount(txtComments, lblStatus);
            plInteract.Enabled = panel1.Enabled = ckbInteract.Checked;
            panel3.Enabled = ckbShareWall.Checked;
            EDA1511C.Enabled = panel4.Enabled = ckbComment.Checked;
            E31CEB31.Enabled = ckbAnh.Checked;
            panel6.Enabled = !rdbComment.Checked;
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
                CommonMethod.ShowMessageWarning("Vui lòng nhập tên hành động!");
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
                        Type = FacebookFarmingType.HDTuongTacBaiViet,
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
                        CommonMethod.ShowMessageError("Thêm thất bại, vui lòng thử lại sau!");
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
                        CommonMethod.ShowMessageError("Cập nhật thất bại, vui lòng thử lại sau!");
                    }
                }
            }
        }

        private void txtLinks_TextChanged(object sender, EventArgs e)
        {
            SubdyHelper.UpdateItemCount(txtLinks, label18);
        }

        private void txtLines_TextChanged(object sender, EventArgs e)
        {
            SubdyHelper.UpdateItemCount(txtComments, lblStatus);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog f = new System.Windows.Forms.FolderBrowserDialog();
            if (f.ShowDialog() == DialogResult.OK)
            {
                txtPathAnh.Text = f.SelectedPath;
            }
        }
    }
}
