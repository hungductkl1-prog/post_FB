using AntdUI;
using Facebook_Farm_NewFeed_PostStory.Utils;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Json;
using Sunny.Subdy.Common.Models;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;

namespace Facebook_Farm_NewFeed_PostStory.Views.Forms.Actions
{
    public partial class fPandoraNgheNhac : Form
    {
        private string scriptId;
        private string actionId;
        private ConfigHelper jsonConfig;
        private ScriptActionContext _context;

        public fPandoraNgheNhac(string scriptId, string actionId = "")
        {
            InitializeComponent();
            FontUtil.ApplyFontToAllControls(this);
            Facebook_Farm_NewFeed_PostStory.Utils.Design.VietnameseFont.Enforce(this);
            _context = new ScriptActionContext();
            this.scriptId = scriptId;
            this.actionId = actionId;

            string configJson = "";
            if (string.IsNullOrEmpty(actionId))
            {
                var index = _context.GetCountName(PandoraFarmingType.DictionariesAction[PandoraFarmingType.HDNgheNhac]);
                if (index == 0)
                    txtTenHanhDong.Text = PandoraFarmingType.DictionariesAction[PandoraFarmingType.HDNgheNhac];
                else
                    txtTenHanhDong.Text = $"{PandoraFarmingType.DictionariesAction[PandoraFarmingType.HDNgheNhac]} ({index})";
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
            btnCancel.Click += (s, e) => Close();
            btnSave.Click += btnSave_Click;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            string actionName = txtTenHanhDong.Text.Trim();
            if (string.IsNullOrEmpty(actionName))
            {
                AntdHelper.NotifyWarn(this, "Cảnh báo", "Vui lòng nhập tên hành động!");
                return;
            }

            if (nudListenMinutesFrom.Value > nudListenMinutesTo.Value)
            {
                AntdHelper.NotifyWarn(this, "Cảnh báo", "Thời gian bắt đầu không được lớn hơn thời gian kết thúc!");
                return;
            }

            // Ô URL là danh sách nhiều dòng; mỗi dòng một URL. Kiểm tra có ít nhất
            // một dòng hợp lệ (chứa "://") để không lưu hành động rỗng.
            var urls = txtPandoraUrl.Text
                .Split(new[] { '\r', '\n', '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0 && x.Contains("://"))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (urls.Count == 0)
            {
                AntdHelper.NotifyWarn(this, "Cảnh báo", "Vui lòng nhập ít nhất một URL playlist/bài hát (mỗi dòng một URL)!");
                return;
            }

            // Ghi lại danh sách đã làm sạch (bỏ dòng rỗng/trùng/dòng không phải URL).
            txtPandoraUrl.Text = string.Join(Environment.NewLine, urls);

            string configJson = jsonConfig.GetJsonString();

            if (string.IsNullOrEmpty(actionId))
            {
                if (CommonMethod.ShowConfirmWarning("Bạn có muốn thêm hành động mới?"))
                {
                    var scriptAction = _context.GetByScriptId(Guid.Parse(scriptId));
                    var index = (scriptAction != null && scriptAction.Count > 0) ? scriptAction.Max(a => a.ByOrder) + 1 : 1;
                    var action = new ScriptAction
                    {
                        Id = Guid.NewGuid(),
                        Name = actionName,
                        Type = PandoraFarmingType.HDNgheNhac,
                        Json = configJson,
                        Platform = PlatformModel.Pandora,
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
                        AntdHelper.NotifyError(this, "Thao tác thất bại", "Thêm thất bại, vui lòng thử lại sau!");
                    }
                }
            }
            else
            {
                if (CommonMethod.ShowConfirmWarning("Bạn có muốn cập nhật hành động?"))
                {
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
                        AntdHelper.NotifyError(this, "Thao tác thất bại", "Cập nhật thất bại, vui lòng thử lại sau!");
                    }
                }
            }
        }
    }
}
