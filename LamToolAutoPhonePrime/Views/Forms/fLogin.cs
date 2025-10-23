using AntdUI;
using Sunny.Subdy.Common.API;
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
                User user = LamToolClient.Authentication(txt_search.Text.Trim(), input1.Text.Trim());
                Globals.User = user;
                new TempLoginStorage
                {
                    Username = txt_search.Text,
                    Password = input1.Text
                }.Save();
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                CommonMethod.ShowMessageError(ex.Message);
                button5.Enabled = true;
            }
        }
        private void button9_Click(object sender, EventArgs e)
        {
            OpenLink("https://www.facebook.com/groups/lamtool.net");
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
                AntdUI.Notification.warn(this, "LamTool Thông Báo", "Không thể mở link: " + ex.Message, AntdUI.TAlignFrom.TR, Font);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenLink("https://t.me/lamtool_net");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            OpenLink("https://www.tiktok.com/@lamtool.net?");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenLink("https://www.youtube.com/channel/UCJoKRG-V3-QaGGlisVKEscQ");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            OpenLink("https://zalo.me/g/uubote459");
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenLink("https://lamtool.net/register");
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

        private static string FilePath => Path.Combine(Path.GetTempPath(), "LamTool_LoginCache.json");

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
    }
}
