using Facebook_Farm_NewFeed_PostStory.Utils;
using Sunny.Subdy.Common.Helper;
using System.Diagnostics;

namespace Facebook_Farm_NewFeed_PostStory
{
    public partial class fShowThongBao : AntdUI.Window
    {
        bool close = false;
        public fShowThongBao(string title, string message)
        {
            InitializeComponent();
            alert10.TextTitle = title;
            alert10.Text = message;
            this.FormClosing += fShowThongBao_FormClosing;
            FontUtil.ApplyFontToAllControls(this);
            _ = UpdateUI(120);
        }
        private async Task UpdateUI(int second)
        {
            for (int i = second; i >= 0; i--)   // chạy từ 120 xuống 0
            {
                if (this.IsDisposed || close) break;

                try
                {
                    if (this.IsHandleCreated)   // kiểm tra handle trước khi invoke
                    {
                        this.BeginInvoke(new Action(() =>
                        {
                            button9.Text = $"Xác nhận ({i})";
                        }));
                    }
                }
                catch (ObjectDisposedException) { break; }

                await Task.Delay(1000);
            }

            if (!this.IsDisposed)
            {
                this.BeginInvoke(new Action(() => this.Close()));
            }
        }
        private void fShowThongBao_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.DialogResult == DialogResult.None) // chỉ khi user bấm X
            {
                e.Cancel = true;
                this.Hide();  // chỉ ẩn chứ không dispose
            }
        }
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            //  Debug.WriteLine("fShowThongBao đã đóng, dispose xong");
        }
        private void timer1_Tick(object sender, EventArgs e)
        {

        }

        private void button9_Click(object sender, EventArgs e)
        {
            close = true;
            DialogResult = DialogResult.OK;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.None;
            close = true;
            this.Close();
        }
    }
}
