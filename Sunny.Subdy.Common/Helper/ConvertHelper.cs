using Sunny.Subdy.Common.ControlMethod;
using AntdUI;
using System.Windows.Forms;
using CommonMethod = AntdUI.CommonMethod;

namespace Sunny.Subdy.Common.Helper
{
    public static class ConvertHelper
    {
        public static string ToMoneyString(this double value)
        {
            return value.ToString("#,0.###");
        }
        public static string ToMoneyString(this int value)
        {
            return value.ToString("#,0.###");
        }
        public static string CapitalizeEachWord(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            return string.Join(" ", input
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(word =>
                    char.ToUpper(word[0]) + word.Substring(1).ToLower()
                ));
        }
        public static void CopyFormat(string type, DataGridView data)
        {
            List<string> lines = new List<string>();
            string[] splitTypes = type.Split('|');

            try
            {
                // Lấy rows có DataBoundItem.Checked == true (nếu model có property này).
                // Convention của project: action dựa vào checkbox, không phải hàng bôi đen.
                // Fallback về SelectedRows nếu model không có property Checked.
                var targetRows = GetCheckedRows(data);
                foreach (DataGridViewRow row in targetRows)
                {
                    List<string> fields = new List<string>();

                    foreach (string typeItem in splitTypes)
                    {
                        if (string.IsNullOrEmpty(typeItem))
                        {
                            fields.Add(typeItem);
                            continue;
                        }
                        string cellValue = "";

                        // Tìm cột có DataPropertyName khớp với typeItem
                        var column = data.Columns
                            .Cast<DataGridViewColumn>()
                            .FirstOrDefault(c => c.DataPropertyName.ToLower() == typeItem.ToLower());

                        if (column != null)
                        {
                            cellValue = Convert.ToString(row.Cells[column.Index].Value) ?? "";
                        }

                        fields.Add(cellValue);
                    }

                    lines.Add(string.Join("|", fields));
                }

                string result = string.Join("\n", lines);
                if (string.IsNullOrEmpty(result)) { CommonMethod.ShowMessageWarning("Vui lòng tick checkbox tài khoản cần copy."); return; }
                Clipboard.SetText(result);
                CommonMethod.ShowMessageSuccess($"Copy thành công {lines.Count} tài khoản.");
            }
            catch (Exception ex)
            {
                CommonMethod.ShowConfirmWarning($"Có lỗi xảy ra, vui lòng báo admin! [{ex.Message}]");
            }
        }

        private static IEnumerable<DataGridViewRow> GetCheckedRows(DataGridView data)
        {
            System.Reflection.PropertyInfo checkedProp = null;
            bool propResolved = false;

            foreach (DataGridViewRow row in data.Rows)
            {
                if (row.IsNewRow) continue;
                var item = row.DataBoundItem;
                if (item == null) continue;

                if (!propResolved)
                {
                    checkedProp = item.GetType().GetProperty("Checked",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    propResolved = true;
                }

                if (checkedProp == null) break;

                if (checkedProp.PropertyType == typeof(bool)
                    && (bool)checkedProp.GetValue(item) == true)
                {
                    yield return row;
                }
            }

            // Fallback: model không có property Checked → giữ behavior cũ (SelectedRows).
            if (checkedProp == null)
            {
                foreach (DataGridViewRow row in data.SelectedRows)
                    yield return row;
            }
        }
    }
    public enum CopyType
    {
        Empty,
        Custum,
        Uid,
        Password,
        _2FA,
        Token,
        Cookie,
        Proxy,
        Email,
        PassMail,
        MailAdress,
        Status,
    }
}
