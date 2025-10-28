using AntdUI;
using LamToolAutoPhonePrime.Utils;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Json;
using Sunny.Subdy.Common.Models;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;

namespace LamToolAutoPhonePrime.Views.Forms.Actions
{
    public partial class fHDOnOff2FA : Form
    {
        private string scriptId;
        private string actionId;
        private ConfigHelper jsonConfig;
        private ScriptActionContext _context;
        public fHDOnOff2FA(string scriptId, string actionId = "")
        {
            InitializeComponent();
            FontUtil.ApplyFontToAllControls(this);
            rbBat2FA.CheckedChanged += ckbDefault_CheckedChanged;
            rbTat2FA.CheckedChanged += ckbDefault_CheckedChanged;
            _context = new ScriptActionContext();
            this.scriptId = scriptId;
            this.actionId = actionId;
            string configJson = "";
            if (string.IsNullOrEmpty(actionId))
            {
                var index = _context.GetCountName(FacebookFarmingType.DictionariesAction[FacebookFarmingType.HDOnOff2FA]);
                if (index == 0)
                {
                    txtTenHanhDong.Text = FacebookFarmingType.DictionariesAction[FacebookFarmingType.HDOnOff2FA];
                }
                else
                {
                    txtTenHanhDong.Text = $"{FacebookFarmingType.DictionariesAction[FacebookFarmingType.HDOnOff2FA]} ({(index)})";
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
            DDBBFF9D.Enabled = rbBat2FA.Checked;
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
                        Type = FacebookFarmingType.HDOnOff2FA,
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
    }
}
