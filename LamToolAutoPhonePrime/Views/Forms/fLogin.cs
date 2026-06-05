using AntdUI;
using LamToolAutoPhonePrime.Utils.Design;
using Sunny.Subdy.Common.API;
using Sunny.Subdy.Common.API.Jobs.GoLike;
using Sunny.Subdy.Common.API.Model;
using Sunny.Subdy.Common.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LamToolAutoPhonePrime.Views.Forms
{
    public partial class fLogin : AntdUI.Window
    {
        public fLogin()
        {
            InitializeComponent();
            var cached = TempLoginStorage.Load();
            txt_search.Text = cached.Username;
            input1.Text = cached.Password;
            this.Load += FLogin_Load;
            button5.Click += btnLogin_Click;
        }
        private void FLogin_Load(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txt_search.Text) && !string.IsNullOrEmpty(input1.Text))
            {
                btnLogin_Click(null, null);
            }
        }
        private void btnLogin_Click(object sender, EventArgs e)
        {
            button5.Enabled = false;
            try
            {
                User user = SubdyClient.Login(txt_search.Text.Trim(), input1.Text.Trim());
                // Lấy thêm pending coin ngay sau login để hiện cùng số dư.
                string access_token = SubdyClient.GetTokenAutoQN(txt_search.Text.Trim(), input1.Text.Trim());
                user.Token_QN =access_token;
                try
                {
                  

                    var report = new GoLikeClient().GetCoinReport(user.Token);
                    if (report.CurrentCoin >= 0)
                    {
                        user.Balance = report.CurrentCoin;
                        user.PendingBalance = report.PendingCoin;
                    }
                }
                catch { /* ignore — login vẫn thành công */ }
                Globals.User = user;
                new TempLoginStorage
                {
                    Username = txt_search.Text,
                    Password = input1.Text
                }.Save();
                Program.SetStartup(true);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                ErrorHandler.Show(this, ex, "Đăng nhập thất bại");
                button5.Enabled = true;
            }
        }
        private void button9_Click(object sender, EventArgs e)
        {
            OpenLink("https://app.golike.net/register");
        }
        private void OpenLink(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                AntdUI.Notification.warn(this, "QN Thông Báo", "Không thể mở link: " + ex.Message, AntdUI.TAlignFrom.TR, Font);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenLink("https://app.golike.net/register");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            OpenLink("https://app.golike.net/register");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenLink("https://app.golike.net/register");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            OpenLink("https://app.golike.net/register");
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            using (var frm = new fRegister())
            {
                frm.ShowDialog(this);
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {

        }

        private void button6_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }
    }
    public class TempLoginStorage
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";

        private static string FilePath => Path.Combine(Path.GetTempPath(), "Subdy_LoginCache.json");

        public static TempLoginStorage Load()
        {
            if (!File.Exists(FilePath))
                return new TempLoginStorage();

            try
            {
                var json = File.ReadAllText(FilePath);
                var parts = json.Split('|');
                return new TempLoginStorage
                {
                    Username = parts.ElementAtOrDefault(0) ?? "",
                    Password = parts.ElementAtOrDefault(1) ?? ""
                };
            }
            catch
            {
                return new TempLoginStorage();
            }
        }

        public void Save()
        {
            try
            {
                File.WriteAllText(FilePath, $"{Username}|{Password}");
            }
            catch { }
        }

        public static void Clear()
        {
            try
            {
                if (File.Exists(FilePath))
                    File.Delete(FilePath);
            }
            catch { }
        }

        /// <summary>
        /// Auto-login bằng credential cache (nếu có). Không hiện UI.
        /// Set Globals.User khi thành công, trả về true. Sai cache → trả false, không throw.
        /// </summary>
        public static bool TryAutoLogin()
        {
            var cached = Load();
            if (string.IsNullOrWhiteSpace(cached.Username) || string.IsNullOrWhiteSpace(cached.Password))
                return false;

            try
            {
                var user = SubdyClient.Login(cached.Username.Trim(), cached.Password.Trim());
                if (user == null) return false;

                try
                {
                    user.Token_QN = SubdyClient.GetTokenAutoQN(cached.Username.Trim(), cached.Password.Trim());
                }
                catch { /* token-qn có thể fail nhưng login chính vẫn dùng được */ }

                try
                {
                    var report = new GoLikeClient().GetCoinReport(user.Token);
                    if (report.CurrentCoin >= 0)
                    {
                        user.Balance = report.CurrentCoin;
                        user.PendingBalance = report.PendingCoin;
                    }
                }
                catch { /* ignore */ }

                Globals.User = user;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
