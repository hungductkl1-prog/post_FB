using AntdUI;
using Facebook_Farm_NewFeed_PostStory.Utils;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Json;
using Sunny.Subdy.Common.Models;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;

namespace Facebook_Farm_NewFeed_PostStory.Views.Forms.Actions
{
    public partial class fHDTuongTacNhom : Form
    {
        private string scriptId;
        private string actionId;
        private ConfigHelper jsonConfig;
        private ScriptActionContext _context;
        public fHDTuongTacNhom(string scriptId, string actionId = "")
        {
            InitializeComponent();
            FontUtil.ApplyFontToAllControls(this);
            Facebook_Farm_NewFeed_PostStory.Utils.Design.FormResponsiveHelper.MakeScrollable(this);
            ckbInteract.CheckedChanged += ckbDefault_CheckedChanged;
            ckbShareWall.CheckedChanged += ckbDefault_CheckedChanged;
            ckbComment.CheckedChanged += ckbDefault_CheckedChanged;
            rdbComment.CheckedChanged += ckbDefault_CheckedChanged;
            radioButton3.CheckedChanged += ckbDefault_CheckedChanged;
            radioButton2.CheckedChanged += ckbDefault_CheckedChanged;
            ckbAnh.CheckedChanged += ckbDefault_CheckedChanged;
            _context = new ScriptActionContext();
            this.scriptId = scriptId;
            this.actionId = actionId;
            string configJson = "";
            if (string.IsNullOrEmpty(actionId))
            {
                var index = _context.GetCountName(FacebookFarmingType.DictionariesAction[FacebookFarmingType.HDTuongTacNhom]);
                if (index == 0)
                {
                    txtTenHanhDong.Text = FacebookFarmingType.DictionariesAction[FacebookFarmingType.HDTuongTacNhom];
                }
                else
                {
                    txtTenHanhDong.Text = $"{FacebookFarmingType.DictionariesAction[FacebookFarmingType.HDTuongTacNhom]} ({(index)})";
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
                        Type = FacebookFarmingType.HDTuongTacNhom,
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
