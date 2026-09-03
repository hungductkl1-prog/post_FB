using AntdUI;
using Emgu.CV.Ocl;
using Facebook_Farm_NewFeed_PostStory.Utils;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Data.Context;
using Facebook_Farm_NewFeed_PostStory.Utils.Design;

using CommonMethod = Sunny.Subdy.Common.ControlMethod.CommonMethod;
namespace Facebook_Farm_NewFeed_PostStory.Views.Forms
{
    public partial class fImportProxy : Facebook_Farm_NewFeed_PostStory.Utils.BaseForm
    {
        private AccountContext context;
        private List<Guid> listId;
        public fImportProxy(List<Guid> ids)
        {
            InitializeComponent();
            listId = ids;
            context = new AccountContext();
            FontUtil.ApplyFontToAllControls(this); Facebook_Farm_NewFeed_PostStory.Utils.Design.VietnameseFont.Enforce(this);
            cbbTypeProxy.SelectedIndex = 0;
        }

        private async void btnAdd_Click(object sender, EventArgs e)
        {
            var proxies = txtLines.Lines
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrEmpty(p))
                .ToList();

            if (proxies.Count == 0)
            {
                AntdHelper.NotifyWarn(this, "Cảnh báo", "Vui lòng nhập proxy!");
                return;
            }

            // Mặc định là "Lần lượt": giữ nguyên từng dòng proxy theo thứ tự nhập.
            if (E82D5414.Checked)
            {
                proxies = SubdyHelper.Shuffle(proxies);
            }

            int repeatCount = (int)FE1FAE23.Value;
            bool skipExisting = ckbKhongNhapTaiKhoanDaCo.Checked;

            var accounts = context.GetByIds(listId);
            if (accounts == null || accounts.Count == 0)
            {
                AntdHelper.NotifyWarn(this, "Cảnh báo", "Không có tài khoản nào được chọn!");
                return;
            }

            // GetByIds dùng IN (...) nên không đảm bảo thứ tự. Khôi phục đúng thứ tự
            // tài khoản được truyền từ danh sách checkbox trên giao diện.
            var accountById = accounts.ToDictionary(account => account.Id);
            accounts = listId
                .Where(id => accountById.ContainsKey(id))
                .Select(id => accountById[id])
                .ToList();

            int maxUpdate = proxies.Count * repeatCount;
            int updatedCount = 0;

            if (!CommonMethod.ShowConfirmWarning($"Bạn có muốn thêm {proxies.Count} proxy (mỗi proxy {repeatCount} lần) vào {accounts.Count} tài khoản?"))
                return;

            await AntdHelper.WithLoading(this, "Đang cập nhật proxy...", async () =>
            {
                await System.Threading.Tasks.Task.Run(() =>
                {
                    int proxyIndex = 0;
                    int usedCountForCurrentProxy = 0;

                    foreach (var acc in accounts)
                    {
                        if (updatedCount >= maxUpdate)
                            break; // chỉ update tối đa proxyCount * repeatCount tài khoản

                        if (skipExisting && !string.IsNullOrEmpty(acc.Proxy))
                            continue;

                        acc.Proxy = proxies[proxyIndex];
                        acc.Status = $"Đã cập nhật proxy: {acc.Proxy} vào tài khoản.";
                        updatedCount++;
                        usedCountForCurrentProxy++;

                        if (usedCountForCurrentProxy >= repeatCount)
                        {
                            proxyIndex++;
                            usedCountForCurrentProxy = 0;
                        }

                        if (proxyIndex >= proxies.Count)
                            break; // hết proxy
                    }

                    if (updatedCount > 0)
                        context.Update(accounts);
                });
            });

            if (updatedCount > 0)
            {
                AntdHelper.NotifySuccess(this, "Thành công", $"Đã cập nhật proxy cho {updatedCount} tài khoản (mỗi proxy dùng {repeatCount} lần).");
            }
            else
            {
                AntdHelper.NotifyWarn(this, "Cảnh báo", "Không có tài khoản nào được cập nhật.");
            }
        }

        private void txtLines_TextChanged(object sender, EventArgs e)
        {
            SubdyHelper.UpdateItemCount(txtLines, lblStatus);
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btn_setting_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
