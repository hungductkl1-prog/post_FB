using AntdUI;
using Facebook_Farm_NewFeed_PostStory.Utils;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;
using Facebook_Farm_NewFeed_PostStory.Utils.Design;

namespace Facebook_Farm_NewFeed_PostStory.Views.Forms
{
    public partial class fFolder : AntdUI.Window
    {
        private FolderContext _folderContext;
        private ScriptContext _scriptContext;
        private string _type = "";
        private string _platform = "";
        public fFolder(string type, string platform)
        {
            InitializeComponent();
            _type = type;
            _platform = platform;
            label1.Text = "Tên:";
            switch (_type)
            {
                case "AddFolder":
                    {
                        button9.Text = "Thêm";
                        windowBar.Text = $"Thêm nhóm {platform}";
                        break;
                    }
                case "AddScript":
                    {
                        button9.Text = "Thêm";
                        windowBar.Text = $"Thêm kịch bản {platform}";
                        break;
                    }
            }
            FontUtil.ApplyFontToAllControls(this);
        }
        public fFolder(string type, string nameFolder, string platform)
        {
            InitializeComponent();
            _type = type;
            _platform = platform;
            switch (_type)
            {
                case "EditFolder":
                    {
                        button9.Text = "Lưu";
                        windowBar.Text = $"Đổi tên nhóm {platform}";
                        label1.Text = "Tên cũ:";
                        label2.Text = "Tên mới:";
                        textBox1.ReadOnly = true;
                        textBox1.Text = nameFolder;
                        label2.Visible = true;
                        textBox2.Visible = true;
                        break;
                    }
                case "EditScript":
                    {
                        button9.Text = "Lưu";
                        windowBar.Text = $"Đổi tên kịch bản {platform}";
                        label1.Text = "Tên cũ:";
                        label2.Text = "Tên mới:";
                        textBox1.ReadOnly = true;
                        textBox1.Text = nameFolder;
                        label2.Visible = true;
                        textBox2.Visible = true;
                        break;
                    }
            }
            FontUtil.ApplyFontToAllControls(this);
        }


        private void button9_Click(object sender, EventArgs e)
        {
            switch (_type)
            {
                case "AddFolder":
                    {
                        if (string.IsNullOrEmpty(textBox1.Text))
                        {
                            AntdHelper.NotifyWarn(this, "Cảnh báo", "Tên nhóm không được bỏ trống!"); return;
                        }
                        _folderContext = new FolderContext();
                        var folder = _folderContext.GetByName(textBox1.Text.Trim(), _platform);
                        if (folder != null)
                        {
                            AntdHelper.NotifyWarn(this, "Cảnh báo", "Tên nhóm đã tồn tại!"); return;
                        }
                        var newFolder = new Folder
                        {
                            Id = Guid.NewGuid(),
                            Name = textBox1.Text.Trim(),
                            Type = _platform,
                            Count = "0",
                            DateCreate = DateTime.Now.ToString("dd/MM/yyyy")

                        };
                        if (_folderContext.Add(newFolder))
                        {
                            AntdHelper.NotifySuccess(this, "Thành công", $"Tạo nhóm {textBox1.Text.Trim()} thành công"); this.Close();

                        }
                        else
                        {
                            AntdHelper.NotifyError(this, "Thao tác thất bại", $"Tạo nhóm {textBox1.Text.Trim()} thất bại");
                            return;
                        }

                        break;
                    }
                case "EditFolder":
                    {
                        _folderContext = new FolderContext();
                        var folder = _folderContext.GetByName(textBox2.Text.Trim(), _platform);
                        if (folder != null)
                        {
                            AntdHelper.NotifyWarn(this, "Cảnh báo", "Tên nhóm đã tồn tại!"); return;
                        }
                        folder = _folderContext.GetByName(textBox1.Text.Trim(), _platform);
                        folder.Name = textBox2.Text.Trim();
                        if (_folderContext.Update(folder))
                        {
                            AntdHelper.NotifySuccess(this, "Thành công", $"Đổi tên nhóm thành công"); this.Close();

                        }
                        else
                        {
                            AntdHelper.NotifyError(this, "Thao tác thất bại", $"Đổi tên nhóm thất bại");
                            return;
                        }
                        break;
                    }
                case "AddScript":
                    {
                        if (string.IsNullOrEmpty(textBox1.Text))
                        {
                            AntdHelper.NotifyWarn(this, "Cảnh báo", "Tên kịch bản không được bỏ trống!"); return;
                        }
                        _scriptContext = new ScriptContext();
                        var folder = _scriptContext.GetByName(textBox1.Text.Trim(), _platform);
                        if (folder != null)
                        {
                            AntdHelper.NotifyWarn(this, "Cảnh báo", "Tên kịch bản đã tồn tại!"); return;
                        }
                        var newFolder = new Script
                        {
                            Id = Guid.NewGuid(),
                            Name = textBox1.Text.Trim(),
                            Platform = _platform,
                            DateCreate = DateTime.Now.ToString("dd/MM/yyyy")

                        };
                        if (_scriptContext.Add(newFolder))
                        {
                            AntdHelper.NotifySuccess(this, "Thành công", $"Tạo kịch bản {textBox1.Text.Trim()} thành công"); this.Close();

                        }
                        else
                        {
                            AntdHelper.NotifyError(this, "Thao tác thất bại", $"Tạo kịch bản {textBox1.Text.Trim()} thất bại");
                            return;
                        }

                        break;
                    }
                case "EditScript":
                    {
                        _scriptContext = new ScriptContext();
                        var folder = _scriptContext.GetByName(textBox2.Text.Trim(), _platform);
                        if (folder != null)
                        {
                            AntdHelper.NotifyWarn(this, "Cảnh báo", "Tên kịch bản đã tồn tại!"); return;
                        }
                        folder = _scriptContext.GetByName(textBox1.Text.Trim(), _platform);
                        folder.Name = textBox2.Text.Trim();
                        if (_scriptContext.Update(folder))
                        {
                            AntdHelper.NotifySuccess(this, "Thành công", $"Đổi tên kịch bản thành công"); this.Close();

                        }
                        else
                        {
                            AntdHelper.NotifyError(this, "Thao tác thất bại", $"Đổi tên kịch bản thất bại");
                            return;
                        }
                        break;
                    }
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
    }
}
