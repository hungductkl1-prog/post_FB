using AntdUI;
using Facebook_Farm_NewFeed_PostStory.Utils;
using Sunny.Subdy.Common.Helper;

namespace Facebook_Farm_NewFeed_PostStory.Views.Forms
{
    public partial class fSelectMultiFolder : Facebook_Farm_NewFeed_PostStory.Utils.BaseForm
    {
        private readonly string _platform;
        private readonly List<string> _folders;
        public bool Saved { get; private set; } = false;

        public fSelectMultiFolder(string platform, List<string> folders)
        {
            InitializeComponent();
            _platform = platform;
            _folders = folders;
            FontUtil.ApplyFontToAllControls(this); Facebook_Farm_NewFeed_PostStory.Utils.Design.VietnameseFont.Enforce(this);

            // Load các folder đã chọn trước đó
            string folderPath = Path.Combine(Path.GetTempPath(), $"folders_{_platform}.txt");
            var previousSelected = new HashSet<string>();
            if (File.Exists(folderPath))
                previousSelected = new HashSet<string>(File.ReadAllLines(folderPath).Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)));

            // Populate grid
            foreach (var f in _folders)
            {
                var row = new DataGridViewRow();
                row.CreateCells(dataGridView1);
                var chk = new DataGridViewCheckBoxCell { Value = previousSelected.Contains(f) };
                row.Cells[0] = chk;
                row.Cells[1].Value = dataGridView1.Rows.Count + 1;
                row.Cells[2].Value = f;
                dataGridView1.Rows.Add(row);
            }

            UpdateCount();
        }

        private void UpdateCount()
        {
            int total = dataGridView1.Rows.Count;
            int selected = dataGridView1.Rows.Cast<DataGridViewRow>()
                .Count(r => r.Cells[0].Value is true);
            lblCount.Text = $"Đã chọn {selected}/{total}";
        }

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 0) UpdateCount();
        }

        private void dataGridView1_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dataGridView1.IsCurrentCellDirty)
                dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void chkAll_CheckedChanged(object sender, EventArgs e)
        {
            bool val = chkAll.Checked;
            foreach (DataGridViewRow row in dataGridView1.Rows)
                row.Cells[0].Value = val;
            dataGridView1.RefreshEdit();
            UpdateCount();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            var selected = dataGridView1.Rows.Cast<DataGridViewRow>()
                .Where(r => r.Cells[0].Value is true)
                .Select(r => r.Cells[2].Value?.ToString() ?? "")
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();

            string folderPath = Path.Combine(Path.GetTempPath(), $"folders_{_platform}.txt");
            File.WriteAllLines(folderPath, selected);
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
