using AntdUI;
using Facebook_Farm_NewFeed_PostStory.Utils;
using Sunny.Subdy.Common.Helper;

namespace Facebook_Farm_NewFeed_PostStory.Views.Forms
{
    public partial class fSelectByUid : AntdUI.Window
    {
        private readonly string _platform;
        public bool Saved { get; private set; } = false;

        public fSelectByUid(string platform)
        {
            InitializeComponent();
            _platform = platform;
            FontUtil.ApplyFontToAllControls(this);

            // Load lại uid đã lưu trước đó (nếu có)
            string uidPath = Path.Combine(Path.GetTempPath(), $"uids_{_platform}.txt");
            if (File.Exists(uidPath))
                txtUids.Text = File.ReadAllText(uidPath);

            UpdateCount();
            txtUids.TextChanged += (s, e) => UpdateCount();
        }

        private void UpdateCount()
        {
            int count = txtUids.Lines.Count(l => !string.IsNullOrWhiteSpace(l));
            lblCount.Text = $"Danh sách uid ({count}):";
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            string uidPath = Path.Combine(Path.GetTempPath(), $"uids_{_platform}.txt");
            var lines = txtUids.Lines
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrEmpty(l))
                .Distinct();
            File.WriteAllLines(uidPath, lines);
            Saved = true;
            this.Close();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btn_setting_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
