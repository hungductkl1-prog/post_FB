using AntdUI;
using LamToolAutoPhonePrime.Views.Controls;
using Sunny.Subdy.Common.Logs;
using Sunny.Subdy.UI.View.Pages;
using System.Windows.Forms;

namespace LamToolAutoPhonePrime.Utils
{
    /// <summary>
    /// Hướng dẫn sử dụng lần đầu cho user mới: Modal xác nhận → Tour highlight các control chính trên fMain.
    /// </summary>
    public static class UserTourHelper
    {
        private static readonly string FlagPath = Path.Combine(AppContext.BaseDirectory, "configs", "first_run_tour.flag");

        public static bool HasSeenTour()
        {
            try { return File.Exists(FlagPath); }
            catch { return false; }
        }

        public static void MarkTourSeen()
        {
            try
            {
                var dir = Path.GetDirectoryName(FlagPath)!;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(FlagPath, DateTime.Now.ToString("O"));
            }
            catch (Exception ex) { LogManager.Error(ex); }
        }

        public static void ResetTourFlag()
        {
            try { if (File.Exists(FlagPath)) File.Delete(FlagPath); }
            catch (Exception ex) { LogManager.Error(ex); }
        }

        /// <summary>
        /// Mỗi bước tour: control để highlight, tiêu đề + mô tả popover, và tab menu cần active trước khi hiện.
        /// </summary>
        private class TourStep
        {
            public Control Target;
            public string Title;
            public string Description;
            public string MenuButtonName; // btn_android / btn_facebook / btn_history / null
            public TAlign Arrow = TAlign.Bottom;
        }

        /// <summary>
        /// Hiển thị Modal xác nhận, nếu user đồng ý thì chạy Tour. Luôn lưu cờ đã xem để không hỏi lại.
        /// </summary>
        public static void ShowFirstRunPrompt(fMain form)
        {
            // Modal luôn hiển thị mỗi lần mở phần mềm trừ khi user tick "Không hiển thị lần sau".
            var content = BuildPromptContent(out var chkDontShowAgain);

            // Đảm bảo Modal nổi trên form (LayeredFormModal thừa kế TopMost từ form.TopMost).
            // fMain mặc định không TopMost → Modal có thể bị che khi user click ra ngoài.
            bool oldTopMost = form.TopMost;
            form.TopMost = true;
            form.Activate();

            DialogResult result;
            try
            {
                result = AntdUI.Modal.open(new AntdUI.Modal.Config(form,
                    "Hướng dẫn sử dụng",
                    content,
                    TType.Info)
                {
                    OkText = "Bắt đầu hướng dẫn",
                    CancelText = "Bỏ qua",
                });
            }
            finally
            {
                form.TopMost = oldTopMost;
            }

            if (chkDontShowAgain.Checked) MarkTourSeen();

            if (result == DialogResult.OK) StartTour(form);
        }

        private static Control BuildPromptContent(out AntdUI.Checkbox checkbox)
        {
            var panel = new System.Windows.Forms.Panel
            {
                Width = 360,
                Height = 80,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = Color.Transparent,
            };

            var lbl = new System.Windows.Forms.Label
            {
                Text = "Bạn có muốn xem hướng dẫn nhanh các chức năng chính của phần mềm không?",
                AutoSize = false,
                Location = new Point(0, 0),
                Size = new Size(360, 44),
                BackColor = Color.Transparent,
            };

            checkbox = new AntdUI.Checkbox
            {
                Text = "Không hiển thị lần sau",
                Location = new Point(0, 50),
                Size = new Size(360, 24),
            };

            panel.Controls.Add(lbl);
            panel.Controls.Add(checkbox);
            return panel;
        }

        /// <summary>
        /// Chạy tour ngay (không hỏi Modal) — dùng cho menu "Xem lại hướng dẫn".
        /// </summary>
        public static void StartTour(fMain form)
        {
            var steps = BuildSteps(form);
            if (steps.Count == 0) return;

            Form popover = null;

            AntdUI.Tour.open(new AntdUI.Tour.Config(form,
                result =>
                {
                    popover?.Close();
                    popover = null;

                    if (result.Index >= steps.Count)
                    {
                        result.Close();
                        return;
                    }

                    var step = steps[result.Index];

                    // Chuyển tab nếu bước yêu cầu
                    if (!string.IsNullOrEmpty(step.MenuButtonName))
                        ActivateMenu(form, step.MenuButtonName);

                    if (step.Target == null || step.Target.IsDisposed)
                    {
                        LogManager.Info($"[Tour] Bước {result.Index}: target null/disposed → đóng");
                        result.Close();
                        return;
                    }

                    // Ép tạo handle + layout để GetControlPath lấy đúng toạ độ
                    if (!step.Target.IsHandleCreated)
                    {
                        try { var _ = step.Target.Handle; } catch { }
                    }
                    step.Target.Refresh();

                    LogManager.Info($"[Tour] Bước {result.Index}: {step.Title} → target={step.Target.GetType().Name} ({step.Target.Name}) size={step.Target.Size} visible={step.Target.Visible}");
                    result.Set(step.Target);
                },
                p =>
                {
                    // Sau khi tour đã vẽ highlight xong (Rect có giá trị), mở popover trên control
                    if (!p.Rect.HasValue) return;
                    if (p.Index >= steps.Count) return;

                    var step = steps[p.Index];
                    if (step.Target == null || step.Target.IsDisposed) return;

                    try
                    {
                        popover?.Close();
                        bool isLast = p.Index == steps.Count - 1;
                        string footer = isLast
                            ? "Nhấn để kết thúc hướng dẫn."
                            : $"Bước {p.Index + 1}/{steps.Count} — nhấn ra ngoài để tiếp tục.";

                        popover = AntdUI.Popover.open(new AntdUI.Popover.Config(
                            step.Target,
                            step.Title,
                            step.Description + "\n\n" + footer)
                        {
                            ArrowAlign = step.Arrow,
                            Focus = false,
                        });
                    }
                    catch (Exception ex) { LogManager.Error(ex); }
                })
            {
                MaskClosable = true,
                ClickNext = true,
            });
        }

        private static List<TourStep> BuildSteps(fMain form)
        {
            var steps = new List<TourStep>();
            var account = form.UcAccount;
            if (account == null) return steps;

            void Add(Control target, string title, string desc, TAlign arrow = TAlign.Bottom)
            {
                if (target == null) return;
                steps.Add(new TourStep
                {
                    Target = target,
                    Title = title,
                    Description = desc,
                    MenuButtonName = "btn_facebook",
                    Arrow = arrow,
                });
            }

            // Luồng trình bày theo thứ tự thao tác thực tế: nhóm → kịch bản → thêm tài khoản → tìm/lọc → chạy → theo dõi.

            // 1. Nhóm (folder)
            Add(account.TourCboGroup,
                "Chọn nhóm tài khoản",
                "Dropdown chọn nhóm (folder) đang làm việc. Mỗi nhóm chứa một danh sách tài khoản riêng, ví dụ: Làm Job QN, Nuôi-Acc-Mới...");

            Add(account.TourBtnFolderManager,
                "Quản lý nhóm",
                "Nhấn nút thư mục này để mở cửa sổ quản lý nhóm — nơi bạn thêm nhóm mới, đổi tên, xoá nhóm và sắp xếp lại các nhóm đang có.");

            // 2. Cài đặt kịch bản
            Add(account.TourBtnJobSettings,
                "Cài đặt jobs",
                "Mở cửa sổ cấu hình kịch bản cho nhóm: chọn các hành động (like, reaction, follow, like page...), tần suất và thứ tự thực hiện.");

            Add(account.TourBtnGeneralSettings,
                "Cài đặt chung",
                "Thiết lập tham số chung áp dụng cho mọi tài khoản trong nhóm: delay giữa các bước, proxy, giới hạn job/ngày...");

            Add(account.TourBtnInteract,
                "Tương tác",
                "Mở nhanh các form tương tác: đăng bài, buff follow, buff like page, xem reel/story...");

            // 3. Thêm & tìm tài khoản
            Add(account.TourBtnImportAccount,
                "Thêm tài khoản",
                "Import tài khoản Facebook từ file hoặc clipboard theo định dạng chuẩn (uid|password|2fa|cookie).");

            Add(account.TourInputSearch,
                "Tìm kiếm nhanh",
                "Gõ UID, email hoặc tên để lọc nhanh tài khoản trong bảng bên dưới.");

            Add(account.TourCboFilter,
                "Lọc tài khoản",
                "Lọc theo trạng thái live/die/checkpoint hoặc theo các tiêu chí khác để chỉ thao tác trên nhóm tài khoản mong muốn.");

            // 4. Hiển thị & tải lại
            Add(account.TourBtnReload,
                "Tải lại danh sách",
                "Tải lại bảng tài khoản từ cơ sở dữ liệu — dùng khi bạn vừa import hoặc cập nhật từ tool khác.");

            Add(account.TourBtnToggleCols,
                "Hiển thị cột",
                "Nhấn nút \"Hiển thị\" để chọn các cột muốn hiện trong bảng: UID, họ tên, avatar, nhóm, kịch bản, tình trạng, trạng thái... Giúp bảng gọn gàng, tập trung vào thông tin cần theo dõi.");

            // 5. Chạy job
            Add(account.TourBtnRun,
                "Chạy job",
                "Tick chọn các tài khoản cần chạy rồi nhấn đây để tool bắt đầu thực thi kịch bản trên điện thoại đã kết nối.",
                TAlign.Top);

            // 6. Theo dõi kết quả
            Add(account.TourDataGrid,
                "Bảng tài khoản",
                "Nơi hiển thị toàn bộ tài khoản của nhóm hiện tại. Cột 'Chọn' để tick chạy, cột 'Trạng thái' hiển thị lỗi/checkpoint nếu có.",
                TAlign.Top);

            Add(account.TourToolStripStats,
                "Thống kê job",
                "Thanh tổng hợp số liệu: bao nhiêu tài khoản đã chọn, đã chạy, đã hoàn thành hôm nay. Biểu tượng biểu đồ bên phải cho chi tiết theo từng loại hành động (like, follow, comment...).",
                TAlign.Top);

            return steps;
        }

        private static void ActivateMenu(fMain form, string buttonName)
        {
            try
            {
                var pMenu = form.Controls.Find("pMenu", true).FirstOrDefault();
                if (pMenu == null) return;

                foreach (Control container in pMenu.Controls)
                {
                    foreach (Control c in container.Controls)
                    {
                        if (c is System.Windows.Forms.Button b && b.Name == buttonName)
                        {
                            b.PerformClick();
                            return;
                        }
                    }
                }
            }
            catch (Exception ex) { LogManager.Error(ex); }
        }
    }
}
