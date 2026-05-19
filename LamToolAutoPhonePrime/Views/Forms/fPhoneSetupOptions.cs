using AntdUI;
using LamToolAutoPhonePrime.Utils.Design;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace LamToolAutoPhonePrime.Views.Forms
{
    /// <summary>
    /// Popup chứa các tuỳ chọn "Cài đặt phone" — thay cho checkbox dày đặc trên panel phải.
    /// Trả về <see cref="PhoneSetupOptions"/> qua property <see cref="Result"/> khi OK.
    /// Hủy → DialogResult.Cancel, Result == null.
    /// </summary>
    public class fPhoneSetupOptions : AntdUI.Window
    {
        private static readonly string ConfigPath =
            Path.Combine(AppContext.BaseDirectory, "Config", "phone-setup-options.json");

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public PhoneSetupOptions? Result { get; private set; }

        // Initial values (pre-populated từ caller để giữ trạng thái lần trước nếu cần)
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public PhoneSetupOptions Initial { get; set; } = new PhoneSetupOptions();

        private CheckBox _chkCaiDatBanDau   = null!;
        private CheckBox _chkTatAmThanh     = null!;
        private CheckBox _chkNgonNguEng     = null!;
        private CheckBox _chkCaiFacebook    = null!;
        private CheckBox _chkKhoiDong       = null!;
        private CheckBox _chkTatGPS         = null!;
        private CheckBox _chkCaiTLC         = null!;
        private CheckBox _chkCapQuyenTLC    = null!;
        private NumericUpDown _nudBright    = null!;
        private NumericUpDown _nudPin       = null!;

        public fPhoneSetupOptions()
        {
            BuildUi();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            // Ưu tiên config đã lưu trên đĩa; fallback về Initial nếu chưa có
            var saved = TryLoad();
            var src = saved ?? Initial;
            _chkCaiDatBanDau.Checked   = src.CaiDatBanDau;
            _chkTatAmThanh.Checked     = src.TatAmThanh;
            _chkNgonNguEng.Checked     = src.NgonNguEng;
            _chkCaiFacebook.Checked    = src.CaiFacebook;
            _chkKhoiDong.Checked       = src.KhoiDong;
            _chkTatGPS.Checked         = src.TatGPS;
            _chkCaiTLC.Checked         = src.CaiGolikeHelper;
            _chkCapQuyenTLC.Checked    = src.CapQuyenGolikeHelper;
            _nudBright.Value           = Clamp(src.BrightnessPercent, 0, 100);
            _nudPin.Value              = Clamp(src.PinPercent, 0, 999);
        }

        private static int Clamp(int v, int min, int max) => v < min ? min : (v > max ? max : v);

        /// <summary>Đọc config đã lưu (nếu có). Trả null nếu chưa có / lỗi.</summary>
        public static PhoneSetupOptions? TryLoad()
        {
            try
            {
                if (!File.Exists(ConfigPath)) return null;
                var json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<PhoneSetupOptions>(json);
            }
            catch { return null; }
        }

        private static void TrySave(PhoneSetupOptions options)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
                var json = JsonSerializer.Serialize(options, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
            }
            catch { /* swallow — config save lỗi không nên chặn flow */ }
        }

        private void BuildUi()
        {
            Text             = "Cài đặt ban đầu";
            Size             = new Size(480, 460);
            StartPosition    = FormStartPosition.CenterParent;
            FormBorderStyle  = FormBorderStyle.FixedDialog;
            MaximizeBox      = false;
            MinimizeBox      = false;
            ShowIcon         = false;
            BackColor        = ColorPalette.Surface;
            Font             = FontScale.Body9;

            // ── Body (Fill — add đầu tiên)
            var body = new System.Windows.Forms.Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = ColorPalette.Surface,
                Padding   = new Padding(Spacing.Xl, Spacing.Lg, Spacing.Xl, Spacing.Lg),
            };

            int y = Spacing.Md;
            int W = 200;

            // Row helpers
            _chkCaiDatBanDau = MkChk("Cài đặt ban đầu", 0, y, W);
            body.Controls.Add(_chkCaiDatBanDau);
            var lblBright = MkLbl("Chỉnh sáng", 210, y + 4);
            body.Controls.Add(lblBright);
            _nudBright = new NumericUpDown
            {
                Minimum = 0, Maximum = 100, Value = 100, Width = 70,
                Location = new Point(296, y), Font = FontScale.Body9,
            };
            body.Controls.Add(_nudBright);
            body.Controls.Add(MkLbl("%", 372, y + 4));
            y += 34;

            _chkTatAmThanh = MkChk("Tắt âm thanh", 0, y, W);
            body.Controls.Add(_chkTatAmThanh);
            var lblPin = MkLbl("Set % pin", 210, y + 4);
            body.Controls.Add(lblPin);
            _nudPin = new NumericUpDown
            {
                Minimum = 0, Maximum = 999, Value = 999, Width = 70,
                Location = new Point(296, y), Font = FontScale.Body9,
            };
            body.Controls.Add(_nudPin);
            body.Controls.Add(MkLbl("%", 372, y + 4));
            y += 34;

            _chkNgonNguEng = MkChk("Cài ngôn ngữ English", 0, y, W);
            body.Controls.Add(_chkNgonNguEng);
            _chkTatGPS = MkChk("Tắt GPS", 210, y, W);
            body.Controls.Add(_chkTatGPS);
            y += 34;

            _chkCaiFacebook = MkChk("Cài app Facebook", 0, y, W);
            body.Controls.Add(_chkCaiFacebook);
            _chkCaiTLC = MkChk("Cài GolikeHelper", 210, y, W);
            body.Controls.Add(_chkCaiTLC);
            y += 34;

            _chkKhoiDong = MkChk("Khởi động lại máy", 0, y, W);
            body.Controls.Add(_chkKhoiDong);
            _chkCapQuyenTLC = MkChk("Cấp quyền GolikeHelper", 210, y, W);
            body.Controls.Add(_chkCapQuyenTLC);
            y += 34;

            // Inner positioning needs offset for body.Padding
            foreach (Control c in body.Controls)
            {
                c.Left += Spacing.Xl;
                c.Top  += Spacing.Lg;
            }

            Controls.Add(body);

            // ── Footer (Bottom)
            var footer = new System.Windows.Forms.Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 60,
                BackColor = ColorPalette.SurfaceAlt,
                Padding   = new Padding(Spacing.Xl, Spacing.Md, Spacing.Xl, Spacing.Md),
            };

            var btnApply = new AntdUI.Button
            {
                Text   = "Áp dụng",
                Size   = new Size(120, 36),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
            };
            ButtonStyle.ApplyPrimary(btnApply);
            btnApply.Click += (_, __) =>
            {
                Result = new PhoneSetupOptions
                {
                    CaiDatBanDau         = _chkCaiDatBanDau.Checked,
                    TatAmThanh           = _chkTatAmThanh.Checked,
                    NgonNguEng           = _chkNgonNguEng.Checked,
                    CaiFacebook          = _chkCaiFacebook.Checked,
                    KhoiDong             = _chkKhoiDong.Checked,
                    TatGPS               = _chkTatGPS.Checked,
                    CaiGolikeHelper      = _chkCaiTLC.Checked,
                    CapQuyenGolikeHelper = _chkCapQuyenTLC.Checked,
                    BrightnessPercent    = (int)_nudBright.Value,
                    PinPercent           = (int)_nudPin.Value,
                };
                TrySave(Result);
                DialogResult = DialogResult.OK;
                Close();
            };

            var btnCancel = new AntdUI.Button
            {
                Text   = "Hủy",
                Size   = new Size(100, 36),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
            };
            ButtonStyle.ApplySecondary(btnCancel);
            btnCancel.Click += (_, __) =>
            {
                Result = null;
                DialogResult = DialogResult.Cancel;
                Close();
            };

            footer.Controls.Add(btnApply);
            footer.Controls.Add(btnCancel);

            footer.Resize += (_, __) =>
            {
                int rx = footer.Width - Spacing.Xl;
                btnApply.Location  = new Point(rx - btnApply.Width, 12);
                rx -= btnApply.Width + Spacing.Sm;
                btnCancel.Location = new Point(rx - btnCancel.Width, 12);
            };

            Controls.Add(footer);

            // ── Header (Top — add cuối)
            var header = new System.Windows.Forms.Panel
            {
                Dock      = DockStyle.Top,
                Height    = 48,
                BackColor = ColorPalette.Primary,
                Padding   = new Padding(Spacing.Xl, 0, Spacing.Xl, 0),
            };
            var lblHeader = new System.Windows.Forms.Label
            {
                Text      = "Cài đặt phone",
                ForeColor = Color.White,
                Font      = new Font(FontScale.FamilyName, 12F, FontStyle.Bold),
                AutoSize  = true,
                Location  = new Point(Spacing.Xl, 14),
                BackColor = Color.Transparent,
            };
            header.Controls.Add(lblHeader);
            Controls.Add(header);
        }

        private static CheckBox MkChk(string text, int x, int y, int w) => new CheckBox
        {
            Text     = text,
            AutoSize = false,
            Size     = new Size(w, 28),
            Location = new Point(x, y),
            Font     = FontScale.Body9,
            ForeColor = ColorPalette.TextPrimary,
            BackColor = Color.Transparent,
        };

        private static System.Windows.Forms.Label MkLbl(string text, int x, int y) => new System.Windows.Forms.Label
        {
            Text     = text,
            AutoSize = true,
            Location = new Point(x, y),
            Font     = FontScale.Body9,
            ForeColor = ColorPalette.TextPrimary,
            BackColor = Color.Transparent,
        };
    }

    /// <summary>DTO truyền qua giữa fPhoneSetupOptions popup ↔ ucManagerDevices.</summary>
    public class PhoneSetupOptions
    {
        public bool CaiDatBanDau { get; set; }
        public bool TatAmThanh { get; set; }
        public bool NgonNguEng { get; set; }
        public bool CaiFacebook { get; set; }
        public bool KhoiDong { get; set; }
        public bool TatGPS { get; set; }
        public bool CaiGolikeHelper { get; set; }
        public bool CapQuyenGolikeHelper { get; set; }
        public int BrightnessPercent { get; set; } = 100;
        public int PinPercent { get; set; } = 999;

        public bool AnyChecked =>
            CaiDatBanDau || TatAmThanh || NgonNguEng || CaiFacebook ||
            KhoiDong || TatGPS || CaiGolikeHelper || CapQuyenGolikeHelper;
    }
}
