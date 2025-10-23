using AntdUI;
using Emgu.CV.Ocl;
using LamToolAutoPhonePrime.Utils;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Data.Context;

namespace LamToolAutoPhonePrime.Views.Forms
{
    public partial class fImportProxy : AntdUI.Window
    {
        private AccountContext context;
        private List<Guid> listId;
        public fImportProxy(List<Guid> ids)
        {
            InitializeComponent();
            listId = ids;
            context = new AccountContext();
            FontUtil.ApplyFontToAllControls(this);
            cbbTypeProxy.SelectedIndex = 0;
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            var proxies = txtLines.Lines
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrEmpty(p))
                .Distinct()
                .ToList();

            if (proxies.Count == 0)
            {
                CommonMethod.ShowMessageWarning("Vui lòng nhập proxy!");
                return;
            }

            if (E82D5414.Checked)
                proxies = SubdyHelper.Shuffle(proxies);

            int repeatCount = (int)FE1FAE23.Value;
            bool skipExisting = ckbKhongNhapTaiKhoanDaCo.Checked;

            var accounts = context.GetByIds(listId);
            if (accounts == null || accounts.Count == 0)
            {
                CommonMethod.ShowMessageWarning("Không có tài khoản nào được chọn!");
                return;
            }

            int maxUpdate = proxies.Count * repeatCount;
            int updatedCount = 0;

            if (!CommonMethod.ShowConfirmWarning($"Bạn có muốn thêm {proxies.Count} proxy (mỗi proxy {repeatCount} lần) vào {accounts.Count} tài khoản?"))
                return;

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
            {
                context.Update(accounts);
                CommonMethod.ShowMessageSuccess($"Đã cập nhật proxy cho {updatedCount} tài khoản (mỗi proxy dùng {repeatCount} lần).");
            }
            else
            {
                CommonMethod.ShowMessageWarning("Không có tài khoản nào được cập nhật.");
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
