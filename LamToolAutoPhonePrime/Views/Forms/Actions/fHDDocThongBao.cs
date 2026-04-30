using AntdUI;
using LamToolAutoPhonePrime.Utils;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Json;
using Sunny.Subdy.Common.Models;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;

namespace LamToolAutoPhonePrime.Views.Forms.Actions
{
    public partial class fHDDocThongBao : Form
    {
        private string scriptId;
        private string actionId;
        private JsonHelper jsonConfig;
        private ScriptActionContext _context;
        public fHDDocThongBao(string scriptId, string actionId = "")
        {
            InitializeComponent();
            _context = new ScriptActionContext();
            this.scriptId = scriptId;
            this.actionId = actionId;
            string configJson = "";
            if (string.IsNullOrEmpty(actionId))
            {
                var index = _context.GetCountName(FacebookFarmingType.DictionariesAction[FacebookFarmingType.HDDocThongBao]);
                if (index == 0)
                {
                    txtTenHanhDong.Text = FacebookFarmingType.DictionariesAction[FacebookFarmingType.HDDocThongBao];
                }
                else
                {
                    txtTenHanhDong.Text = $"{FacebookFarmingType.DictionariesAction[FacebookFarmingType.HDDocThongBao]} ({(index)})";
                }
                btnSave.Text = "Thêm";
            }
            else
            {
                btnSave.Text = "Lưu";
                var action = _context.GetById(Guid.Parse(actionId));
                txtTenHanhDong.Text = action.Name;
                configJson = action?.Json ?? "";
            }
            jsonConfig = new JsonHelper(configJson, isJsonString: true);
            FontUtil.ApplyFontToAllControls(this);
        }

        private void fHDDocThongBao_Load(object sender, EventArgs e)
        {
            try
            {
                nudSoLuongFrom.Value = jsonConfig.GetIntType("nudSoLuongFrom", 5);
                nudSoLuongTo.Value = jsonConfig.GetIntType("nudSoLuongTo", 10);
                nudDelayFrom.Value = jsonConfig.GetIntType("nudDelayFrom", 5);
                nudDelayTo.Value = jsonConfig.GetIntType("nudDelayTo", 10);
                ckbXoaThongBaoSpam.Checked = jsonConfig.GetBooleanValue("ckbXoaThongBaoSpam", false);
            }
            catch { }
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
            jsonConfig.AddValue("nudSoLuongFrom", nudSoLuongFrom.Value);
            jsonConfig.AddValue("nudSoLuongTo", nudSoLuongTo.Value);
            jsonConfig.AddValue("nudDelayFrom", nudDelayFrom.Value);
            jsonConfig.AddValue("nudDelayTo", nudDelayTo.Value);
            jsonConfig.AddValue("ckbXoaThongBaoSpam", ckbXoaThongBaoSpam.Checked);

            string configJson = jsonConfig.GetJsonString();

            if (string.IsNullOrEmpty(actionId)) // thêm mới
            {
                if (CommonMethod.ShowConfirmWarning("Bạn có muốn thêm hành động mới?"))
                {
                    var scriptAction = _context.GetByScriptId(Guid.Parse(scriptId));
                    var index = (scriptAction != null && scriptAction.Count > 0)
                      ? scriptAction.Max(a => a.ByOrder) + 1
                      : 1;
                    var action = new ScriptAction
                    {
                        Id = Guid.NewGuid(),
                        Name = actionName,
                        Type = FacebookFarmingType.HDDocThongBao,
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


    }

}
