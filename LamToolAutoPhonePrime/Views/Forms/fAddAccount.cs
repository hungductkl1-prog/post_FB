using LamToolAutoPhonePrime.Utils;
using LamToolAutoPhonePrime.Utils.Design;
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
        private Sunny.Subdy.Common.Json.ConfigHelper _configHelper;
        public fAddAccount(string platform, bool add)
        {
            InitializeComponent();
            _add = add;
            _platform = platform;
            _folderContext = new FolderContext();
            LoadFormats();
            LoadCombobox();
            _configHelper = new Sunny.Subdy.Common.Json.ConfigHelper(this, this.Name, onLoad: new System.Action(() =>
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
            const int SLOT_COUNT = 10;
            List<string> listField = Globals.GetFieldsToImportExport();

            cbxs.Clear();
            flowLayoutPanel1.Controls.Clear();

            for (int i = 0; i < SLOT_COUNT; i++)
            {
                ComboBox cbx = new ComboBox();
                cbx.DropDownStyle = ComboBoxStyle.DropDownList;
                cbx.Width = 108;
                cbx.Items.AddRange(listField.ToArray());
                cbx.SelectedIndex = 0; // mặc định rỗng
                cbx.SelectedValueChanged += cbx_SelectedIndexChanged;
                cbxs.Add(cbx);
            }

            // Slot đầu luôn là UID, không thể đổi
            cbxs[0].Items.Clear();
            cbxs[0].Items.Add(Fields.Uid);
            cbxs[0].SelectedIndex = 0;

            flowLayoutPanel1.Controls.AddRange(cbxs.ToArray());
        }

        private async Task uiSymbolButton1_ClickSafe()
        {
            if (string.IsNullOrWhiteSpace(txtLines.Text))
            {
                AntdHelper.NotifyWarn(this, "Thiếu dữ liệu", "Danh sách tài khoản không được để trống.");
                return;
            }

            SetInputsEnabled(false);
            try
            {
                await AntdHelper.WithLoading(this,
                    _add ? "Đang thêm tài khoản..." : "Đang cập nhật tài khoản...",
                    async () =>
                    {
                        List<string> lines = txtLines.Lines
                            .Where(line => !string.IsNullOrWhiteSpace(line))
                            .ToList();

                        if (_add) await AddAccounts(lines);
                        else      await UpdateAccounts(lines);

                        AntdHelper.NotifySuccess(
                            this,
                            _add ? "Đã thêm" : "Đã cập nhật",
                            $"{lines.Count} tài khoản đã được xử lý.");
                    });
            }
            catch (Exception ex)
            {
                ErrorHandler.Show(this, ex,
                    _add ? "Thêm tài khoản thất bại" : "Cập nhật tài khoản thất bại");
            }
            finally
            {
                SetInputsEnabled(true);
            }
        }

        private void SetInputsEnabled(bool enabled)
        {
            panel1.Enabled    = enabled;
            panel3.Enabled    = enabled;
            txtLines.ReadOnly = !enabled;
            button1.Enabled   = enabled;
            button2.Enabled   = enabled;
            button9.Enabled   = enabled;
            select8.Enabled   = enabled;
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
                        case Fields.UserAgent:
                            account.UserAgent = value;
                            break;
                        case Fields.PassMailRecover:
                            account.PassPrivateEmailAddress = value;
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
                        if (!string.IsNullOrEmpty(account.PassPrivateEmailAddress)) accountNew.PassPrivateEmailAddress = account.PassPrivateEmailAddress;
                        accounts.Add(accountNew);
                    }
                }
            }
            if (accounts.Count > 0)
            {
                if (accountContext.Update(accounts))
                {
                    AntdHelper.NotifySuccess(this, "Thành công", $"Đã cập nhật {accounts.Count} tài khoản.");
                    this.Close();
                }
                else
                {
                    AntdHelper.NotifyError(this, "Thao tác thất bại", "Cập nhật tài khoản thất bại.");
                }
            }
            else
            {
                AntdHelper.NotifyError(this, "Thao tác thất bại", "Không có tài khoản nào đã được lưu để cập nhật");
            }
        }

        private async Task AddAccounts(List<string> lines)
        {
            string selectText = string.Empty;
            string[] fieldMap = Array.Empty<string>();
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() =>
                {
                    selectText = select8.Text ?? string.Empty;
                    fieldMap = cbxs.Select(c => c.SelectedItem?.ToString() ?? string.Empty).ToArray();
                }));
            }
            else
            {
                selectText = select8.Text ?? string.Empty;
                fieldMap = cbxs.Select(c => c.SelectedItem?.ToString() ?? string.Empty).ToArray();
            }

            string nameFolder = selectText != "[ Không cần nhóm ]" ? selectText : string.Empty;
            List<Account> accounts = new List<Account>();
            string namefolder = selectText.Trim() == "[ Không cần nhóm ]" ? "" : selectText.Trim();
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                Account account = new Account();
                account.NameFolder = namefolder;
                account.Platformt = _platform;
                string[] parts = line.Split('|');

                for (int i = 0; i < fieldMap.Length && i < parts.Length; i++)
                {
                    string field = fieldMap[i];
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
                        case Fields.UserAgent:
                            account.UserAgent = value;
                            break;
                        case Fields.PassMailRecover:
                            account.PassPrivateEmailAddress = value;
                            break;
                    }
                }
                if (!string.IsNullOrEmpty(account.Uid) || !string.IsNullOrEmpty(account.Email))
                {
                    account.Uid = string.IsNullOrEmpty(account.Uid) ? account.Email : account.Uid;
                    account.NameFolder = nameFolder;
                    account.Id = Guid.NewGuid();
                    account.NameScript = Sunny.Subdy.Data.Context.ScriptNames.FarmXuVip;
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
                    AntdHelper.NotifySuccess(this, "Thành công", $"Đã thêm {accountsToAdd.Count} tài khoản mới vào.");
                }
                else
                {
                    AntdHelper.NotifyError(this, "Thao tác thất bại", "Thêm tài khoản thất bại.");
                }
            }
            else
            {
                AntdHelper.NotifyError(this, "Thao tác thất bại", "Dữ liệu bị trùng.");
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
            AutoDetectFields();
        }

        private void AutoDetectFields()
        {
            var firstLine = txtLines.Lines.FirstOrDefault(l => !string.IsNullOrWhiteSpace(l));
            if (string.IsNullOrEmpty(firstLine)) return;

            string[] parts = firstLine.Split('|');
            if (parts.Length < 2) return;

            var detected = new string[parts.Length];
            var used = new HashSet<string>();

            for (int i = 0; i < parts.Length; i++)
            {
                string val = parts[i].Trim();
                detected[i] = DetectField(val, i, detected);
                used.Add(detected[i]);
            }

            // Sau Email thì PassMail (nếu Email đã detect và cột kế chưa detect rõ)
            for (int i = 0; i < detected.Length - 1; i++)
            {
                if (detected[i] == Fields.Email && string.IsNullOrEmpty(detected[i + 1]))
                {
                    detected[i + 1] = Fields.PassMail;
                }
                if (detected[i] == Fields.Uid && string.IsNullOrEmpty(detected[i + 1]))
                {
                    detected[i + 1] = Fields.Password;
                }
            }

            // Cột cuối cùng chưa detect: nếu có dạng proxy thì gán Proxy
            for (int i = 0; i < detected.Length; i++)
            {
                if (string.IsNullOrEmpty(detected[i]))
                {
                    string val = parts[i].Trim();
                    if (IsProxy(val)) detected[i] = Fields.Proxy;
                }
            }

            // Áp dụng lên combobox (bỏ qua cbxs[0] vì luôn là UID)
            for (int i = 1; i < cbxs.Count && i < detected.Length; i++)
            {
                if (!string.IsNullOrEmpty(detected[i]))
                {
                    var item = cbxs[i].Items.Cast<string>().FirstOrDefault(x => x == detected[i]);
                    if (item != null)
                        cbxs[i].SelectedItem = item;
                }
            }
        }

        private string DetectField(string val, int index, string[] alreadyDetected)
        {
            if (index == 0) return Fields.Uid;

            if (val.StartsWith("EAA", StringComparison.OrdinalIgnoreCase))
                return Fields.Token;

            if (val.Contains("@") && val.Contains("."))
                return Fields.Email;

            if (val.Length > 20 && val.Contains("=") && val.Contains(";"))
                return Fields.Cookie;

            if (IsProxy(val))
                return Fields.Proxy;

            // Chuỗi toàn số dài >= 6: UID
            if (val.All(char.IsDigit) && val.Length >= 6)
                return Fields.Uid;

            // Mozilla user agent
            if (val.StartsWith("Mozilla/", StringComparison.OrdinalIgnoreCase))
                return Fields.UserAgent;

            return string.Empty;
        }

        private bool IsProxy(string val)
        {
            // Dạng: ip:port hoặc ip:port:user:pass hoặc http://...
            if (val.StartsWith("http://") || val.StartsWith("socks5://") || val.StartsWith("socks4://"))
                return true;
            var proxyParts = val.Split(':');
            if (proxyParts.Length >= 2 && int.TryParse(proxyParts[1].Split('@').Last(), out _))
                return true;
            return false;
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
