using AntdUI;
using LamToolAutoPhonePrime.Utils;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Json;
using Sunny.Subdy.Common.Models;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;

using CommonMethod = Sunny.Subdy.Common.ControlMethod.CommonMethod;
namespace LamToolAutoPhonePrime.Views.Forms.Actions
{
    public partial class fHDDangStory : Form
    {
        private string scriptId;
        private string actionId;
        private ConfigHelper jsonConfig;
        private ScriptActionContext _context;
        public fHDDangStory(string scriptId, string actionId = "")
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
                var index = _context.GetCountName(FacebookFarmingType.DictionariesAction[FacebookFarmingType.HDDangStory]);
                if (index == 0)
                {
                    txtTenHanhDong.Text = FacebookFarmingType.DictionariesAction[FacebookFarmingType.HDDangStory];
                }
                else
                {
                    txtTenHanhDong.Text = $"{FacebookFarmingType.DictionariesAction[FacebookFarmingType.HDDangStory]} ({(index)})";
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
            // plAnh chỉ active khi chọn "Đăng ảnh/video"
            plAnh.Enabled = rbDangAnhVideo.Checked;
            // Khối Đăng nhạc (panel3 + ckbCoAnh + plAnhNhac) chỉ active khi chọn "Đăng nhạc"
            panel3.Enabled = rbDangNhac.Checked;
            ckbCoAnh.Enabled = rbDangNhac.Checked;
            plAnhNhac.Enabled = rbDangNhac.Checked && ckbCoAnh.Checked;
            // textarea "Danh sách bài hát" chỉ enable khi "Bài hát chỉ định"
            panel2.Enabled = rbDangNhac.Checked && radioButton5.Checked;
            // Cập nhật số dòng
            SubdyHelper.UpdateItemCount(txtLinks, D397662B);
            SubdyHelper.UpdateItemCount(textBox1, label5);
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
                        Type = FacebookFarmingType.HDDangStory,
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
        }

        private void button5_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog f = new System.Windows.Forms.FolderBrowserDialog();
            if (f.ShowDialog() == DialogResult.OK)
            {
                txtPathAnh.Text = f.SelectedPath;
            }
        }

        private void btnChonAnhNhac_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog f = new System.Windows.Forms.FolderBrowserDialog();
            if (f.ShowDialog() == DialogResult.OK)
            {
                txtPathAnhNhac.Text = f.SelectedPath;
            }
        }
    }
}
