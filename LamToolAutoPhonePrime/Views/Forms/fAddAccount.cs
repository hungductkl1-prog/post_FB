using LamToolAutoPhonePrime.Utils;
using Sunny.Subdy.Common.ControlMethod;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Models;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;

namespace LamToolAutoPhonePrime.Views.Forms
{
    public partial class fAddAccount : AntdUI.Window
    {
        private FolderContext _folderContext;
        private AccountContext _accountContext;
        List<ComboBox> cbxs = new List<ComboBox>();
        private bool _add = true;
        private string _platform = string.Empty;
        string FormatFile = string.Empty;
        public fAddAccount(string platform, bool add)
        {
            InitializeComponent();
            _add = add;
            _platform = platform;
            _folderContext = new FolderContext();
            LoadFormats();
            LoadCombobox();
            new Sunny.Subdy.Common.Json.ConfigHelper(this, this.Name, onLoad: new System.Action(() =>
            {
                txtLines.Text = "";

            }), shouldExit: false);
            FormatFile = $"accounts-format{platform}.txt";
            ControlHelper.LoadFormatFromFile(FormatFile, cbxs);
            FontUtil.ApplyFontToAllControls(this);
        }
        private void LoadFormats()
        {
            select8.Items.Clear();
            select8.Items.Add("[ Không cần nhóm ]");
            var formats = _folderContext.GetByType(_platform);

            if (formats != null && formats.Count > 0)
            {
                foreach (var format in formats)
                {
                    select8.Items.Add(format.Name);
                }
            }
            if (string.IsNullOrEmpty(select8.Text))
            {
                select8.SelectedIndex = 0;
            }
        }
        private void LoadCombobox()
        {
            List<string> listField = Globals.GetFieldsToImportExport();
            for (int i = 0; i < listField.Count - 1; i++)
            {
                ComboBox cbx = new ComboBox();
                cbx.DropDownStyle = ComboBoxStyle.DropDownList;
                cbx.Width = 100;
                cbx.Items.AddRange(listField.ToArray());
                if (i < listField.Count - 1)
                {
                    cbx.SelectedIndex = i + 1;
                }
                cbx.SelectedValueChanged += cbx_SelectedIndexChanged;
                cbxs.Add(cbx);
            }

            cbxs[0].Items.Clear();
            cbxs[0].Items.Add(Fields.Uid);
            cbxs[0].SelectedIndex = 0;

            flowLayoutPanel1.Controls.AddRange(cbxs.ToArray());
        }

        private async Task uiSymbolButton1_ClickSafe()
        {
            try
            {
                panel1.Enabled = false;
                panel3.Enabled = false;
                txtLines.ReadOnly = true;
                if (string.IsNullOrEmpty(txtLines.Text.Trim()))
                {
                    CommonMethod.ShowMessageWarning("Danh sách tài khoản không được để trống.");
                    return;
                }
                this.Invoke(new Action(async () =>
                {
                    txtLines.ReadOnly = true;
                    button1.Enabled = false;
                    button2.Enabled = false;
                    button9.Enabled = false;
                    select8.Enabled = false;
                }));
               
                List<string> lines = new List<string>();
                lines = txtLines.Lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
                if (_add)
                {
                    this.Invoke(new Action(async () =>
                    {
                        await AddAccounts(lines);
                    }));

                }
                else
                {
                    await UpdateAccounts(lines);

                }
                this.Invoke(new Action(async () =>
                {
                    txtLines.ReadOnly = false;
                    button1.Enabled = true;
                    button2.Enabled = true;
                    button9.Enabled = true;
                    select8.Enabled = true;
                }));
              
            }
            catch (Exception ex)
            {
                CommonMethod.ShowMessageError(ex.Message);
            }
            finally
            {
                panel1.Enabled = true;
                panel3.Enabled = true;
                txtLines.ReadOnly = false;
            }

        }
        private async Task UpdateAccounts(List<string> lines)
        {
            List<Account> accounts = new List<Account>();
            AccountContext accountContext = new AccountContext();
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                Account account = new Account();
                string[] parts = line.Split('|');

                for (int i = 0; i < cbxs.Count && i < parts.Length; i++)
                {
                    string field = string.Empty;
                    if (cbxs[i].InvokeRequired)
                    {
                        cbxs[i].Invoke(new Action(() =>
                        {
                            field = cbxs[i].SelectedItem?.ToString() ?? string.Empty;
                        }));
                    }
                    else
                    {
                        field = cbxs[i].SelectedItem?.ToString() ?? string.Empty;
                    }
                    string value = parts[i].Trim();

                    switch (field)
                    {
                        case Fields.Uid:
                            account.Uid = value;
                            break;
                        case Fields.Password:
                            account.Password = value;
                            break;
                        case Fields.Phone:
                            account.Phone = value;
                            break;
                        case Fields._2FA:
                            account.TowFA = value;
                            break;
                        case Fields.Cookie:
                            account.Cookie = value;
                            break;
                        case Fields.Token:
                            account.Token = value;
                            break;
                        case Fields.Proxy:
                            account.Proxy = value;
                            break;
                        case Fields.Email:
                            account.Email = value;
                            break;
                        case Fields.PassMail:
                            account.PassMail = value;
                            break;
                        case Fields.Username:
                            account.UserName = value;
                            break;
                    }
                }
                if (!string.IsNullOrEmpty(account.Uid))
                {
                    var accountNew = accountContext.GetByUid(account.Uid);
                    if (accountNew != null)
                    {
                        if (!string.IsNullOrEmpty(account.Password)) accountNew.Password = account.Password;
                        if (!string.IsNullOrEmpty(account.Phone)) accountNew.Phone = account.Phone;
                        if (!string.IsNullOrEmpty(account.TowFA)) accountNew.TowFA = account.TowFA;
                        if (!string.IsNullOrEmpty(account.Cookie)) accountNew.Cookie = account.Cookie;
                        if (!string.IsNullOrEmpty(account.Token)) accountNew.Token = account.Token;
                        if (!string.IsNullOrEmpty(account.Proxy)) accountNew.Proxy = account.Proxy;
                        if (!string.IsNullOrEmpty(account.Email)) accountNew.Email = account.Email;
                        if (!string.IsNullOrEmpty(account.PassMail)) accountNew.PassMail = account.PassMail;
                        if (!string.IsNullOrEmpty(account.UserAgent)) accountNew.UserAgent = account.UserAgent;
                        if (!string.IsNullOrEmpty(account.UserName)) accountNew.UserName = account.UserName;
                        accounts.Add(accountNew);
                    }
                }
            }
            if (accounts.Count > 0)
            {
                if (accountContext.Update(accounts))
                {
                    CommonMethod.ShowMessageSuccess($"Đã cập nhật {accounts.Count} tài khoản.");
                    this.Close();
                }
                else
                {
                    CommonMethod.ShowMessageError("Cập nhật tài khoản thất bại.");
                }
            }
            else
            {
                CommonMethod.ShowMessageError("Không có tài khoản nào đã được lưu để cập nhật");
            }
        }

        private async Task AddAccounts(List<string> lines)
        {
            string nameFolder = string.Empty;
            if (select8.Text != "[ Không cần nhóm ]")
            {
                nameFolder = select8.Text;
            }
            List<Account> accounts = new List<Account>();
            string namefolder = select8.Text.Trim() == "[ Không cần nhóm ]" ? "" : select8.Text.Trim();
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                Account account = new Account();
                account.NameFolder = namefolder;
                account.Platformt = _platform;
                string[] parts = line.Split('|');

                for (int i = 0; i < cbxs.Count && i < parts.Length; i++)
                {
                    string field = cbxs[i].SelectedItem?.ToString() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(field)) continue;
                    string value = parts[i].Trim();

                    switch (field)
                    {
                        case Fields.Uid:
                            account.Uid = value;
                            break;
                        case Fields.Password:
                            account.Password = value;
                            break;
                        case Fields.Phone:
                            account.Phone = value;
                            break;
                        case Fields._2FA:
                            account.TowFA = value;
                            break;
                        case Fields.Cookie:
                            account.Cookie = value;
                            break;
                        case Fields.Token:
                            account.Token = value;
                            break;
                        case Fields.Proxy:
                            account.Proxy = value;
                            break;
                        case Fields.Email:
                            account.Email = value;
                            break;
                        case Fields.PassMail:
                            account.PassMail = value;
                            break;
                        case Fields.MailClientId:
                            account.MailClientId = value;
                            break;
                        case Fields.MailRefreshToken:
                            account.MailRefreshToken = value;
                            break;
                        case Fields.Username:
                            account.UserName = value;
                            break;
                    }
                }
                if (!string.IsNullOrEmpty(account.Uid) || !string.IsNullOrEmpty(account.Email))
                {
                    account.Uid = string.IsNullOrEmpty(account.Uid) ? account.Email : account.Uid;
                    account.NameFolder = nameFolder;
                    account.Id = Guid.NewGuid();
                    accounts.Add(account);
                }
            }
            var accountContext = new AccountContext();
            var accountsOld = accountContext.GetAll(new List<string> { nameFolder }, _platform, true);
            var oldUids = new HashSet<string>(accountsOld.Select(a => a.Uid));
            var accountsToAdd = accounts
                .Where(a => !string.IsNullOrEmpty(a.Uid) && !oldUids.Contains(a.Uid))
                .ToList();
            if (accountsToAdd.Count > 0)
            {
                if (accountContext.AddRange(accountsToAdd))
                {
                    CommonMethod.ShowMessageSuccess($"Đã thêm {accountsToAdd.Count} tài khoản mới vào.");
                }
                else
                {
                    CommonMethod.ShowMessageError("Thêm tài khoản thất bại.");
                }
            }
            else
            {
                CommonMethod.ShowMessageError("Dữ liệu bị trùng.");
            }
        }
        private void cbx_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (sender is ComboBox cbx)
            {
                // Nếu SelectedItem là rỗng, không nên xử lý tiếp
                if (cbx.SelectedItem == null || string.IsNullOrWhiteSpace(cbx.SelectedItem.ToString()))
                    return;

                // Tìm combo khác có cùng giá trị được chọn
                var cbx1 = cbxs
                    .Where(c => c != cbx && c.SelectedItem?.ToString() == cbx.SelectedItem.ToString())
                    .FirstOrDefault();

                if (cbx1 != null)
                {
                    cbx1.SelectedItem = Fields.Empty;
                }
            }
        }
        private void txtType_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadCombobox();
        }
        private void txtLines_TextChanged_1(object sender, EventArgs e)
        {
            label3.Text = $"Danh sách tài khoản ({txtLines.Lines.Count()}):";

        }
        private void uiSymbolButton2_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        private void btn_global_SelectedValueChanged(object sender, AntdUI.ObjectNEventArgs e)
        {

        }

        private void btn_global_Click(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Maximized)
                this.WindowState = FormWindowState.Normal;
            else
                this.WindowState = FormWindowState.Maximized;
        }

        private void btn_setting_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            fFolder f = new fFolder("AddFolder", _platform);
            f.ShowDialog();
            LoadFormats();
        }

        private void select8_SelectedIndexChanged(object sender, AntdUI.IntEventArgs e)
        {

        }

        private void button9_Click(object sender, EventArgs e)
        {
            ControlHelper.SaveFormatToFile(FormatFile, cbxs);
            _ = uiSymbolButton1_ClickSafe();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btn_setting_Click_2(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btn_global_SelectedValueChanged_2(object sender, AntdUI.ObjectNEventArgs e)
        {
            if (this.WindowState == FormWindowState.Maximized)
                this.WindowState = FormWindowState.Normal;
            else
                this.WindowState = FormWindowState.Maximized;
        }

        private void btn_global_Click_1(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Maximized)
                this.WindowState = FormWindowState.Normal;
            else
                this.WindowState = FormWindowState.Maximized;
        }
    }
}
