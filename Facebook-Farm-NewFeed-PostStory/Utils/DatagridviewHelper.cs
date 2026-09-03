using System;
using System.Windows.Forms;

namespace Facebook_Farm_NewFeed_PostStory.Utils
{
    /// <summary>
    /// Port cơ chế cập nhật cell của MaxPhoneFarm (class DatagridviewHelper) sang lưới
    /// non-virtual hiện tại. Set/Get cell an toàn theo thread (Invoke), và khi ghi cột
    /// "Trạng thái" sẽ đồng thời cập nhật <see cref="UpdateStatus"/> (giống MaxPhoneFarm
    /// gọi UpdateStatus.SetStatusEntry khi set cell "cStatus").
    /// </summary>
    public static class DatagridviewHelper
    {
        // Tên cột tương ứng MaxPhoneFarm "cStatus" / "cId" trong lưới hiện tại.
        public const string StatusColumnName = "col_Status";
        public const string IdColumnName = "col_Id";

        public static string GetCellValueByName(DataGridView grid, int rowIndex, string columnName)
        {
            string result = "";
            try
            {
                if (grid.InvokeRequired)
                {
                    grid.Invoke((MethodInvoker)delegate
                    {
                        var v = grid.Rows[rowIndex].Cells[columnName].Value;
                        if (v != null) result = v.ToString();
                    });
                }
                else
                {
                    var v = grid.Rows[rowIndex].Cells[columnName].Value;
                    if (v != null) result = v.ToString();
                }
            }
            catch
            {
            }
            return result;
        }

        public static void SetCellValueByName(DataGridView grid, int rowIndex, string columnName, object value)
        {
            try
            {
                // Giống MaxPhoneFarm: set cột trạng thái thì cập nhật dictionary UpdateStatus.
                if (UpdateStatus.isSaveSettings && columnName == StatusColumnName)
                {
                    string id = GetCellValueByName(grid, rowIndex, IdColumnName);
                    UpdateStatus.SetStatusEntry(id, value?.ToString() ?? "");
                }

                if (grid.InvokeRequired)
                    grid.Invoke((MethodInvoker)delegate { grid.Rows[rowIndex].Cells[columnName].Value = value; });
                else
                    grid.Rows[rowIndex].Cells[columnName].Value = value;
            }
            catch
            {
            }
        }

        public static string GetCellValueByIndex(DataGridView grid, int rowIndex, int columnIndex)
        {
            string result = "";
            try
            {
                if (grid.InvokeRequired)
                {
                    grid.Invoke((MethodInvoker)delegate
                    {
                        var v = grid.Rows[rowIndex].Cells[columnIndex].Value;
                        if (v != null) result = v.ToString();
                    });
                }
                else
                {
                    var v = grid.Rows[rowIndex].Cells[columnIndex].Value;
                    if (v != null) result = v.ToString();
                }
            }
            catch
            {
            }
            return result;
        }

        public static void SetCellValueByIndex(DataGridView grid, int rowIndex, int columnIndex, object value)
        {
            try
            {
                if (grid.InvokeRequired)
                    grid.Invoke((MethodInvoker)delegate { grid.Rows[rowIndex].Cells[columnIndex].Value = value; });
                else
                    grid.Rows[rowIndex].Cells[columnIndex].Value = value;
            }
            catch
            {
            }
        }

        /// <summary>MaxPG CountdownCellByName: đếm ngược hiển thị trên 1 cell.</summary>
        public static void CountdownCellByName(DataGridView grid, int rowIndex, string columnName,
            int seconds = 0, string template = "Đợi {time} giây...")
        {
            try
            {
                int start = Environment.TickCount;
                while ((Environment.TickCount - start) / 1000 - seconds < 0)
                {
                    int remain = seconds - (Environment.TickCount - start) / 1000;
                    SetCellValueByName(grid, rowIndex, columnName, template.Replace("{time}", remain.ToString()));
                    System.Threading.Thread.Sleep(500);
                }
            }
            catch
            {
            }
        }
    }
}
