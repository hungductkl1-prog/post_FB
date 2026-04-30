using AntdUI;
using LamToolAutoPhonePrime.Utils;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Json;
using Sunny.Subdy.Common.Models;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;

namespace LamToolAutoPhonePrime.Views.Forms.Actions
{
    /// <summary>
    /// Base form cho mọi action Instagram phase 1.
    /// Layout tối giản: tên hành động + số lượng (from/to) + delay (from/to) + Save/Cancel.
    /// Khi cần UI riêng, kế thừa và thêm control vào panel1.
    /// </summary>
    public partial class fIGActionStub : Form
    {
        protected readonly string scriptId;
        protected readonly string actionId;
        protected readonly string actionType;
        protected JsonHelper jsonConfig;
        protected ScriptActionContext _context;

        public fIGActionStub(string scriptId, string actionType, string actionId = "")
        {
            InitializeComponent();
            _context = new ScriptActionContext();
            this.scriptId = scriptId;
            this.actionId = actionId;
            this.actionType = actionType;

            string configJson = "";
            string defaultName = InstagramFarmingType.DictionariesAction.TryGetValue(actionType, out var n) ? n : actionType;
            windowBar.Text = "Cấu hình " + defaultName;

            if (string.IsNullOrEmpty(actionId))
            {
                var index = _context.GetCountName(defaultName);
                txtTenHanhDong.Text = index == 0 ? defaultName : $"{defaultName} ({index})";
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
            this.Load += (s, e) => LoadFromJson();
            btnCancel.Click += (s, e) => Close();
            btnSave.Click += BtnSave_Click;
        }

        protected virtual void LoadFromJson()
        {
            try
            {
                nudSoLuongFrom.Value = jsonConfig.GetIntType("nudSoLuongFrom", 5);
                nudSoLuongTo.Value = jsonConfig.GetIntType("nudSoLuongTo", 10);
                nudDelayFrom.Value = jsonConfig.GetIntType("nudDelayFrom", 5);
                nudDelayTo.Value = jsonConfig.GetIntType("nudDelayTo", 10);
            }
            catch { }
        }

        protected virtual void SaveToJson()
        {
            jsonConfig.AddValue("nudSoLuongFrom", nudSoLuongFrom.Value);
            jsonConfig.AddValue("nudSoLuongTo", nudSoLuongTo.Value);
            jsonConfig.AddValue("nudDelayFrom", nudDelayFrom.Value);
            jsonConfig.AddValue("nudDelayTo", nudDelayTo.Value);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            string actionName = txtTenHanhDong.Text.Trim();
            if (actionName == "")
            {
                AntdHelper.NotifyWarn(this, "Cảnh báo", "Vui lòng nhập tên hành động!");
                return;
            }
            SaveToJson();
            string configJson = jsonConfig.GetJsonString();

            if (string.IsNullOrEmpty(actionId))
            {
                if (!CommonMethod.ShowConfirmWarning("Bạn có muốn thêm hành động mới?")) return;
                var scriptAction = _context.GetByScriptId(Guid.Parse(scriptId));
                var index = (scriptAction != null && scriptAction.Count > 0)
                    ? scriptAction.Max(a => a.ByOrder) + 1
                    : 1;
                var action = new ScriptAction
                {
                    Id = Guid.NewGuid(),
                    Name = actionName,
                    Type = actionType,
                    Json = configJson,
                    Platform = PlatformModel.Instagram,
                    ScriptId = Guid.Parse(scriptId),
                    ByOrder = index
                };
                if (_context.Add(action))
                {
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else AntdHelper.NotifyError(this, "Thao tác thất bại", "Thêm thất bại, vui lòng thử lại sau!");
            }
            else
            {
                if (!CommonMethod.ShowConfirmWarning("Bạn có muốn cập nhật hành động?")) return;
                var action = _context.GetById(Guid.Parse(actionId));
                action.Name = actionName;
                action.Json = configJson;
                if (_context.Update(action))
                {
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else AntdHelper.NotifyError(this, "Thao tác thất bại", "Cập nhật thất bại, vui lòng thử lại sau!");
            }
        }
    }
}
