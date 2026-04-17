using Sunny.Subdy.Common.ControlMethod;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Json;
using Sunny.Subdy.Common.Logs;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LamToolAutoPhonePrime.Views.Forms
{

    public partial class fUpdateData : Form
    {
        string NameJson = "FormatUpdateProxy";
        List<string> Uids = new List<string>();
        string TypeForm = "";
        public const string Proxy = "proxy";
        public const string TokenJob = "token job";
        private string service = "";
        public fUpdateData(List<string> ids, string typeForm, string platform)
        {
            InitializeComponent();
            Uids = ids;
            TypeForm = typeForm;
            NameJson = typeForm;
            LoadingSetting();
            groupBox1.Text = $"Danh sách {TypeForm} (0):";
            label3.Text = $"Số tài khoản/{TypeForm}:";
            this.Text = $"Nhập {TypeForm}";
            cb_NoProxyAccount.Text = $"Không nhập vào những tài khoản đã có {TypeForm}";
            if (typeForm == "useragent")
            {
                label1.Text = "( Mỗi useragent 1 dòng )";
                label2.Visible = false;
                cbbTypeProxy.Visible = false;
            }
            else if (typeForm == TokenJob)
            {
                groupBox1.Text = $"Danh sách token {platform} (0):";
                label3.Text = $"Số tài khoản/{platform}:";
                label1.Text = $"( Mỗi token {platform} 1 dòng [token])";
                label2.Visible = false;
                cbbTypeProxy.Visible = false;
                cb_NoProxyAccount.Text = $"Không nhập vào những tài khoản đã có tài khoản {platform}";
                this.Text = $"Nhập tài khoản {platform}";
            }
            txtLines_TextChanged(null, null);
            service = platform;
        }
        private void btn_Close_Click(object sender, EventArgs e)
        {
            SaveSetting();
            Close();
        }
        private void LoadingSetting()
        {
            try
            {
                cbbTypeProxy.SelectedIndex = SettingsTool.GetSettings(NameJson, true).GetIntType("cbbTypeProxy", 0);
                nudAccount_Proxy.Value = SettingsTool.GetSettings(NameJson, true).GetIntType("nudAccount_Proxy", 1);
                rdb_LanLuot.Checked = SettingsTool.GetSettings(NameJson, true).GetBooleanValue("rdb_LanLuot", true);
                rdb_Random.Checked = SettingsTool.GetSettings(NameJson, true).GetBooleanValue("rdb_Random", false);
                cb_NoProxyAccount.Checked = SettingsTool.GetSettings(NameJson, true).GetBooleanValue("cb_NoProxyAccount", true);
                if (File.Exists($"{NameJson}.txt"))
                {
                    txtLines.Lines = File.ReadAllLines($"{NameJson}.txt");
                }
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
            }
        }
        private void SaveSetting()
        {
            var config = new JsonHelper(NameJson);
            try
            {
                config.AddValue("cbbTypeProxy", cbbTypeProxy.SelectedIndex);
                config.AddValue("nudAccount_Proxy", nudAccount_Proxy.Value);
                config.AddValue("rdb_LanLuot", rdb_LanLuot.Checked);
                config.AddValue("rdb_Random", rdb_Random.Checked);
                config.AddValue("cb_NoProxyAccount", cb_NoProxyAccount.Checked);
                config.SaveJsonToFile();
                File.WriteAllLines($"{NameJson}.txt", txtLines.Lines);
            }
            catch (Exception ex)
            {
                //   LoggerHelper.ERROR(nameof(fSettings), nameof(LoadingSetting), ex.ToString());
            }
        }

        private void txtLines_TextChanged(object sender, EventArgs e)
        {
            groupBox1.Text = $"Danh sách {TypeForm} ({txtLines.Lines.Count()}):";
        }

        private async void btn_Ok_Click(object sender, EventArgs e)
        {
            SaveSetting();
            if (string.IsNullOrEmpty(txtLines.Text))
            {
                CommonMethod.ShowMessageWarning($"Vui lòng nhập {TypeForm}");
                return;
            }
            string message = string.Empty;
            btn_Close.Enabled = false;
            btn_Ok.Enabled = false;
            await Task.Run(async () =>
            {
                switch (TypeForm)
                {
                    case "proxy":
                        {
                            message = await UpdateProxy();
                            break;
                        }
                    case "useragent":
                        {
                            message = await UpdateUserAgent();
                            break;
                        }
                    case TokenJob:
                        {

                            message = await UpdateAddAcountTTC();
                            break;
                        }
                }
            });

            btn_Close.Enabled = true;
            btn_Ok.Enabled = true;
            CommonMethod.ShowMessageWarning(message);
            Close();
        }
        private async Task<string> UpdateProxy()
        {
            try
            {
                List<string> list = new List<string>();
                foreach (var item in txtLines.Lines)
                {
                    if (string.IsNullOrEmpty(item)) continue;
                    var line = item.Split(':');
                    if (line.Length <= 1) continue;
                    list.Add(item);
                }
                if (!list.Any())
                {
                    return "Không có dữ liệu nào phù hợp";
                }
                if (rdb_Random.Checked)
                {
                    list = SubdyHelper.Shuffle(list);
                }
                int index = Convert.ToInt32(nudAccount_Proxy.Value);
                var context = new AccountContext();
                foreach (var uid in Uids)
                {
                    if (list.Count <= 0)
                    {
                        break;
                    }
                    var account = context.Get(Guid.Parse(uid));
                    if (account == null) continue;
                    if (!string.IsNullOrEmpty(account.Proxy) && cb_NoProxyAccount.Checked) continue;

                    account.Proxy = list[0].ToString();
                    context.Update(account);
                    index--;
                    if (index <= 0)
                    {
                        list.RemoveAt(0);
                        index = Convert.ToInt32(nudAccount_Proxy.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                return "Đã xảy ra lỗi ERROR: " + ex.Message;
            }

            return "Đã thêm proxy thành công";
        }
        private async Task<string> UpdateUserAgent()
        {
            try
            {
                List<string> list = new List<string>();
                foreach (var item in txtLines.Lines)
                {
                    if (string.IsNullOrEmpty(item)) continue;
                    list.Add(item);
                }
                if (!list.Any())
                {
                    return "Không có dữ liệu nào phù hợp";
                }
                if (rdb_Random.Checked)
                {
                    list = SubdyHelper.Shuffle(list);
                }
                int index = Convert.ToInt32(nudAccount_Proxy.Value);
                var context = new AccountContext();
                foreach (var uid in Uids)
                {
                    if (list.Count <= 0)
                    {
                        break;
                    }
                    var account = context.Get(Guid.Parse(uid));
                    if (account == null) continue;
                    if (!string.IsNullOrEmpty(account.UserAgent) && cb_NoProxyAccount.Checked) continue;
                    account.UserAgent = list[0].ToString();
                    context.Get(Guid.Parse(uid));
                    index--;
                    if (index <= 0)
                    {
                        list.RemoveAt(0);
                        index = Convert.ToInt32(nudAccount_Proxy.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                return "Đã xảy ra lỗi ERROR: " + ex.Message;
            }

            return "Đã thêm useragent thành công";
        }
        private async Task<string> UpdateAddAcountTTC()
        {
            try
            {
                List<string> list = new List<string>();
                foreach (var item in txtLines.Lines)
                {
                    if (string.IsNullOrEmpty(item)) continue;
                    list.Add(item);
                }
                if (!list.Any())
                {
                    return "Không có dữ liệu nào phù hợp";
                }
                if (rdb_Random.Checked)
                {
                    list = SubdyHelper.Shuffle(list);
                }
                int index = Convert.ToInt32(nudAccount_Proxy.Value);
                var context = new AccountContext();
                foreach (var uid in Uids)
                {
                    if (list.Count <= 0)
                    {
                        break;
                    }
                    var account = context.Get(Guid.Parse(uid));
                    if (account == null) continue;
                    if (!string.IsNullOrEmpty(account.TokenJob) && cb_NoProxyAccount.Checked) continue;
                    string line = list[0].ToString();
                    account.TokenJob = line;
                    context.Update(account);
                    list.RemoveAt(0);
                    list.Add(line);
                }
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                return "Đã xảy ra lỗi ERROR: " + ex.Message;
            }

            return $"Đã thêm tài khoản {service} thành công";
        }
    }
}
