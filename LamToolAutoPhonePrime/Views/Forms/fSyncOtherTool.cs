using AntdUI;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Context.OtherTools;
using Sunny.Subdy.Data.Models;
using System.Windows.Forms;
using CommonMethod = Sunny.Subdy.Common.ControlMethod.CommonMethod;
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;
using LamToolAutoPhonePrime.Utils;
using LamToolAutoPhonePrime.Utils.Design;

namespace LamToolAutoPhonePrime.Views.Forms
{
    public partial class fSyncOtherTool : AntdUI.Window
    {
        private readonly string _tool;
        private readonly string _platform;
        private readonly AccountContext _accountContext;
        private readonly FolderContext _folderContext;
        private const string ConfigFileName = "uid-tool-sync.txt";

        private AntdUI.PageHeader windowBar = null!;
        private AntdUI.Input txtFolder = null!;
        private AntdUI.Button btnBrowse = null!;
        private AntdUI.Radio rdbByFolder = null!;
        private AntdUI.Radio rdbByUid = null!;
        private AntdUI.Select cbbFolder = null!;
        private AntdUI.Input txtUid = null!;
        private AntdUI.Button btnOk = null!;
        private AntdUI.Button btnCancel = null!;

        public bool IsOk { get; private set; }
        public int AddedCount { get; private set; }
        public int UpdatedCount { get; private set; }

        public fSyncOtherTool(string tool, string platform)
        {
            _tool = tool;
            _platform = platform;
            _accountContext = new AccountContext();
            _folderContext = new FolderContext();

            BuildUi();
            FontUtil.ApplyFontToAllControls(this);

            string saved = SafeReadConfig();
            if (!string.IsNullOrEmpty(saved)) txtUid.Text = saved;
        }

        private void BuildUi()
        {
            Text = $"Đồng bộ tool {_tool}";
            ClientSize = new Size(520, 460);
            MinimumSize = new Size(520, 460);
            MaximumSize = new Size(520, 460);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(245, 247, 250);

            windowBar = new AntdUI.PageHeader
            {
                Dock = DockStyle.Top,
                Text = $"Đồng bộ từ {_tool}",
                MDI = true,
                BackColor = Color.White,
                Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
                Size = new Size(520, 36),
            };
            Controls.Add(windowBar);

            var content = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                BackColor = Color.Transparent,
            };
            Controls.Add(content);

            int y = 12;

            var lblFolder = new System.Windows.Forms.Label
            {
                Text = $"Đường dẫn {_tool}:",
                Location = new Point(0, y),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
            };
            content.Controls.Add(lblFolder);
            y += 24;

            txtFolder = new AntdUI.Input
            {
                Location = new Point(0, y),
                Size = new Size(380, 32),
                PlaceholderText = $"Chọn thư mục cài đặt {_tool}...",
            };
            content.Controls.Add(txtFolder);

            btnBrowse = new AntdUI.Button
            {
                Location = new Point(388, y),
                Size = new Size(96, 32),
                Text = "Chọn...",
                Type = AntdUI.TTypeMini.Primary,
                Shape = AntdUI.TShape.Round,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
            };
            btnBrowse.Click += BtnBrowse_Click;
            content.Controls.Add(btnBrowse);
            y += 48;

            rdbByFolder = new AntdUI.Radio
            {
                Location = new Point(0, y),
                Size = new Size(220, 28),
                Text = "Đồng bộ theo nhóm",
                Checked = true,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
            };
            rdbByFolder.CheckedChanged += Rdb_CheckedChanged;
            content.Controls.Add(rdbByFolder);

            rdbByUid = new AntdUI.Radio
            {
                Location = new Point(240, y),
                Size = new Size(220, 28),
                Text = "Đồng bộ theo UID",
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
            };
            rdbByUid.CheckedChanged += Rdb_CheckedChanged;
            content.Controls.Add(rdbByUid);
            y += 36;

            cbbFolder = new AntdUI.Select
            {
                Location = new Point(0, y),
                Size = new Size(484, 32),
                PlaceholderText = "Chọn nhóm tài khoản từ tool nguồn",
            };
            content.Controls.Add(cbbFolder);
            y += 44;

            txtUid = new AntdUI.Input
            {
                Location = new Point(0, y),
                Size = new Size(484, 160),
                Multiline = true,
                PlaceholderText = "Mỗi dòng một UID",
                Enabled = false,
            };
            content.Controls.Add(txtUid);
            y += 168;

            btnOk = new AntdUI.Button
            {
                Location = new Point(260, y),
                Size = new Size(110, 36),
                Text = "Đồng bộ",
                Type = AntdUI.TTypeMini.Success,
                Shape = AntdUI.TShape.Round,
                IconSvg = "SyncOutlined",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
            };
            btnOk.Click += BtnOk_Click;
            content.Controls.Add(btnOk);

            btnCancel = new AntdUI.Button
            {
                Location = new Point(376, y),
                Size = new Size(108, 36),
                Text = "Đóng",
                Type = AntdUI.TTypeMini.Error,
                Shape = AntdUI.TShape.Round,
                IconSvg = "CloseOutlined",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
            };
            btnCancel.Click += (_, __) => Close();
            content.Controls.Add(btnCancel);
        }

        private void Rdb_CheckedChanged(object? sender, AntdUI.BoolEventArgs e) => UpdateMode();

        private void UpdateMode()
        {
            cbbFolder.Enabled = rdbByFolder.Checked;
            txtUid.Enabled = rdbByUid.Checked;
        }

        private void BtnBrowse_Click(object? sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() != DialogResult.OK) return;
            txtFolder.Text = dlg.SelectedPath;
            LoadCategories();
        }

        private void LoadCategories()
        {
            cbbFolder.Items.Clear();
            var folder = txtFolder.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            {
                AntdHelper.NotifyWarn(this, "Cảnh báo", $"Vui lòng kiểm tra lại thư mục: {folder}");
                return;
            }

            try
            {
                List<string> categories = _tool switch
                {
                    OtherToolNames.MaxCare => GetMaxCareFolders(folder),
                    OtherToolNames.FPlus => GetFPlusFolders(folder),
                    OtherToolNames.MetaMax => GetMetaMaxFolders(folder),
                    _ => new List<string>(),
                };

                if (categories.Count == 0)
                {
                    AntdHelper.NotifyWarn(this, "Cảnh báo", $"Không tìm thấy thư mục/nhóm trong {_tool}.");
                    return;
                }

                foreach (var c in categories) cbbFolder.Items.Add(c);
                cbbFolder.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                ErrorHandler.Show(this, ex, "Lỗi đọc dữ liệu {_tool}");
            }
        }

        private List<string> GetMaxCareFolders(string folder)
        {
            var r = new MaxCareReader(folder);
            if (!r.Exists())
            {
                AntdHelper.NotifyWarn(this, "Cảnh báo", "Không tìm thấy file database của MaxCare.");
                return new List<string>();
            }
            return r.GetFolders();
        }

        private List<string> GetFPlusFolders(string folder)
        {
            var r = new FPlusReader(folder);
            if (!r.Exists())
            {
                AntdHelper.NotifyWarn(this, "Cảnh báo", "Không tìm thấy file database của FPlus.");
                return new List<string>();
            }
            return r.GetFolders();
        }

        private List<string> GetMetaMaxFolders(string folder)
        {
            var r = new MetaMaxReader(folder);
            if (!r.Exists())
            {
                AntdHelper.NotifyWarn(this, "Cảnh báo", "Không tìm thấy file database của MetaMax.");
                return new List<string>();
            }
            return r.GetFolders();
        }

        private async void BtnOk_Click(object? sender, EventArgs e)
        {
            string folder = txtFolder.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            {
                AntdHelper.NotifyWarn(this, "Cảnh báo", $"Vui lòng kiểm tra lại thư mục: {folder}");
                return;
            }

            string category = cbbFolder.SelectedIndex >= 0 && cbbFolder.SelectedIndex < cbbFolder.Items.Count
                ? cbbFolder.Items[cbbFolder.SelectedIndex]?.ToString() ?? ""
                : "";

            bool byFolder = rdbByFolder.Checked;
            string[] uids = byFolder
                ? Array.Empty<string>()
                : (txtUid.Text ?? "").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            if (byFolder && string.IsNullOrEmpty(category))
            {
                AntdHelper.NotifyWarn(this, "Cảnh báo", "Vui lòng chọn nhóm tài khoản.");
                return;
            }
            if (!byFolder && uids.Length == 0)
            {
                AntdHelper.NotifyWarn(this, "Cảnh báo", "Vui lòng nhập danh sách UID cần đồng bộ.");
                return;
            }

            SafeWriteConfig(txtUid.Text ?? "");

            string message = "";
            Enabled = false;
            try
            {
                await AntdUI.Spin.open(this, $"Đang đồng bộ từ {_tool}...", async _ =>
                {
                    await Task.Run(() =>
                    {
                        message = byFolder
                            ? SyncByFolder(folder, category)
                            : SyncByUids(folder, uids);
                    });
                });
                IsOk = true;
            }
            catch (Exception ex)
            {
                message = $"Đã xảy ra lỗi: {ex.Message}";
            }
            finally
            {
                Enabled = true;
            }

            if (!string.IsNullOrEmpty(message)) AntdHelper.NotifySuccess(this, "Thành công", message);
            Close();
        }

        private string SyncByFolder(string folderPath, string category)
        {
            var srcAccounts = _tool switch
            {
                OtherToolNames.MaxCare => new MaxCareReader(folderPath).GetAccountsByFolder(category),
                OtherToolNames.FPlus => new FPlusReader(folderPath).GetAccountsByFolder(category),
                OtherToolNames.MetaMax => new MetaMaxReader(folderPath).GetAccountsByFolder(category),
                _ => new List<OtherToolAccount>(),
            };

            if (srcAccounts.Count == 0) return "Không tìm thấy tài khoản nào trong nhóm.";

            EnsureFolderExists(category);
            var existingByUid = _accountContext
                .GetAll(new List<string>(), _platform, isView: null)
                .Where(a => !string.IsNullOrEmpty(a.Uid))
                .GroupBy(a => a.Uid)
                .ToDictionary(g => g.Key, g => g.First());

            var toAdd = new List<Account>();
            var toUpdate = new List<Account>();

            foreach (var src in srcAccounts)
            {
                if (string.IsNullOrEmpty(src.Uid)) continue;
                if (existingByUid.TryGetValue(src.Uid, out var existing))
                {
                    ApplyTo(existing, src);
                    existing.NameFolder = category;
                    toUpdate.Add(existing);
                }
                else
                {
                    var a = new Account
                    {
                        Id = Guid.NewGuid(),
                        Platformt = _platform,
                        NameFolder = category,
                        IsView = true,
                    };
                    ApplyTo(a, src);
                    toAdd.Add(a);
                }
            }

            if (toAdd.Count > 0) _accountContext.AddRange(toAdd);
            if (toUpdate.Count > 0) _accountContext.Update(toUpdate);
            AddedCount = toAdd.Count;
            UpdatedCount = toUpdate.Count;
            return $"Đã thêm {toAdd.Count} tài khoản và cập nhật {toUpdate.Count} tài khoản.";
        }

        private string SyncByUids(string folderPath, string[] uids)
        {
            int updated = 0;
            foreach (var raw in uids)
            {
                var uid = raw.Trim();
                if (string.IsNullOrEmpty(uid)) continue;

                var existing = _accountContext.GetByUid(uid);
                if (existing == null) continue;

                OtherToolAccount? src = _tool switch
                {
                    OtherToolNames.MaxCare => new MaxCareReader(folderPath).GetAccountByUid(uid),
                    OtherToolNames.FPlus => new FPlusReader(folderPath).GetAccountByUid(uid),
                    OtherToolNames.MetaMax => new MetaMaxReader(folderPath).GetAccountByUid(uid),
                    _ => null,
                };
                if (src == null) continue;

                ApplyTo(existing, src);
                _accountContext.Update(existing);
                updated++;
            }
            UpdatedCount = updated;
            return $"Đã cập nhật {updated} tài khoản.";
        }

        private void EnsureFolderExists(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            if (_folderContext.GetByName(name, _platform) != null) return;
            _folderContext.Add(new Folder
            {
                Id = Guid.NewGuid(),
                Name = name,
                Type = _platform,
                Count = "0",
                DateCreate = DateTime.Now.ToString("dd/MM/yyyy"),
            });
        }

        private static void ApplyTo(Account dst, OtherToolAccount src)
        {
            dst.Uid = src.Uid ?? dst.Uid;
            if (!string.IsNullOrEmpty(src.Password)) dst.Password = src.Password;
            if (!string.IsNullOrEmpty(src.TwoFA)) dst.TowFA = src.TwoFA;
            if (!string.IsNullOrEmpty(src.Cookie)) dst.Cookie = src.Cookie;
            if (!string.IsNullOrEmpty(src.Token)) dst.Token = src.Token;
            if (!string.IsNullOrEmpty(src.Proxy)) dst.Proxy = src.Proxy;
            if (!string.IsNullOrEmpty(src.Email)) dst.EmailAddress = src.Email;
            if (!string.IsNullOrEmpty(src.PassMail)) dst.PassMail = src.PassMail;
            if (!string.IsNullOrEmpty(src.UserAgent)) dst.UserAgent = src.UserAgent;
        }

        private static string SafeReadConfig()
        {
            try { return File.Exists(ConfigFileName) ? File.ReadAllText(ConfigFileName) : ""; }
            catch { return ""; }
        }

        private static void SafeWriteConfig(string content)
        {
            try { File.WriteAllText(ConfigFileName, content ?? ""); } catch { }
        }
    }
}
