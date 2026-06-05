using AntdUI;
using Facebook_Farm_NewFeed_PostStory.Utils;
using Facebook_Farm_NewFeed_PostStory.Views.Forms;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;
using System.Drawing.Drawing2D;

namespace Facebook_Farm_NewFeed_PostStory.Utils.Design
{
    /// <summary>
    /// SSA-styled folder management panel — hiển thị trong Modal.
    /// Thao tác Add/Edit/Delete qua FolderContext + fFolder, không đụng AccountContext.
    /// </summary>
    internal sealed class FolderManagerPanel : UserControl
    {
        private readonly string _platform;
        private readonly FolderContext _folderContext = new FolderContext();
        private readonly Func<Task>? _onChanged;
        private readonly Form? _ownerForm;

        private AntdUI.Label? _lblSub;
        private readonly FlowLayoutPanel _list = new FlowLayoutPanel();
        private readonly System.Windows.Forms.Panel _scroll = new System.Windows.Forms.Panel();

        public FolderManagerPanel(string platform, Func<Task>? onChanged, Form? ownerForm = null)
        {
            _platform  = platform;
            _onChanged = onChanged;
            _ownerForm = ownerForm;

            BackColor = ColorPalette.Surface;
            Size      = new Size(720, 560);
            Padding   = new Padding(Spacing.Lg);
            Font      = FontScale.Body9;

            Build();
            RefreshList();
        }

        private void Build()
        {
            // ── Header ─────────────────────────────────────────────
            var header = new System.Windows.Forms.Panel
            {
                Dock      = DockStyle.Top,
                Height    = 56,
                BackColor = ColorPalette.Surface,
                Padding   = new Padding(0, 0, 0, Spacing.Md),
            };

            var title = new AntdUI.Label
            {
                Text      = "Quản lý nhóm",
                Font      = FontScale.HeadingBold,
                ForeColor = ColorPalette.TextPrimary,
                Location  = new Point(0, 0),
                Size      = new Size(400, 26),
            };
            _lblSub = new AntdUI.Label
            {
                Text      = "",
                Font      = FontScale.Caption8,
                ForeColor = ColorPalette.TextTertiary,
                Location  = new Point(0, 28),
                Size      = new Size(400, 18),
            };
            header.Controls.Add(_lblSub);
            header.Controls.Add(title);

            // ── Footer: note + Thêm nhóm mới ───────────────────────
            var footer = new System.Windows.Forms.Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 70,
                Padding   = new Padding(0, Spacing.Md, 0, 0),
                BackColor = ColorPalette.Surface,
            };

            var addBtn = new AntdUI.Button
            {
                Text        = "+  Thêm nhóm mới",
                Dock        = DockStyle.Fill,
                Ghost       = true,
                BorderWidth = 1F,
                Radius      = Radius.Md,
                Font        = FontScale.Body9Bold,
                ForeColor   = ColorPalette.TextSecondary,
            };
            addBtn.Click += async (_, __) =>
            {
                using var f = new fFolder("AddFolder", _platform);
                f.ShowDialog(_ownerForm ?? FindForm());
                await ReloadAllAsync();
            };
            footer.Controls.Add(addBtn);

            // ── List scroll ────────────────────────────────────────
            _scroll.Dock       = DockStyle.Fill;
            _scroll.AutoScroll = true;
            _scroll.BackColor  = ColorPalette.Surface;
            _scroll.Padding    = new Padding(0, Spacing.Sm, 0, Spacing.Sm);

            _list.Dock          = DockStyle.Top;
            _list.AutoSize      = true;
            _list.AutoSizeMode  = AutoSizeMode.GrowAndShrink;
            _list.FlowDirection = FlowDirection.TopDown;
            _list.WrapContents  = false;
            _list.BackColor     = Color.Transparent;
            _list.Padding       = new Padding(0);
            _list.Margin        = new Padding(0);

            _scroll.Controls.Add(_list);
            _scroll.Resize += (_, __) =>
            {
                _list.Width = _scroll.ClientSize.Width - 4;
                foreach (Control c in _list.Controls) c.Width = _list.Width;
            };

            Controls.Add(_scroll);
            Controls.Add(footer);
            Controls.Add(header);
        }

        internal async Task ReloadAllAsync()
        {
            RefreshList();
            if (_onChanged != null) await _onChanged.Invoke();
        }

        private void RefreshList()
        {
            _list.SuspendLayout();
            _list.Controls.Clear();

            List<Folder> folders;
            try { folders = _folderContext.GetByType(_platform) ?? new List<Folder>(); }
            catch { folders = new List<Folder>(); }

            if (_lblSub != null)
            {
                int total = folders.Sum(f => ParseCount(f.Count));
                _lblSub.Text = $"{folders.Count} nhóm • {total} tài khoản";
            }

            int idx = 0;
            foreach (var folder in folders)
                _list.Controls.Add(BuildRow(folder, idx++));

            _list.ResumeLayout();
        }

        private Control BuildRow(Folder folder, int idx)
        {
            int rowWidth = _list.ClientSize.Width > 0 ? _list.ClientSize.Width : 680;

            var row = new AntdUI.Panel
            {
                Back        = ColorPalette.Surface,
                BackColor   = Color.Transparent,
                Radius      = Radius.Md,
                Height      = 72,
                Width       = rowWidth,
                Margin      = new Padding(0, 0, 0, Spacing.Sm),
                Padding     = new Padding(Spacing.Md),
                BorderWidth = 1F,
                BorderColor = ColorPalette.BorderLight,
            };

            // Owner-drawn color dot (16x16)
            var dotColor = FolderDotColor(idx);
            var dot = new System.Windows.Forms.Panel
            {
                Dock      = DockStyle.Left,
                Width     = 28,
                BackColor = Color.Transparent,
            };
            dot.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                int d = 14;
                var r = new Rectangle((dot.Width - d) / 2, (dot.Height - d) / 2, d, d);
                using var b = new SolidBrush(dotColor);
                e.Graphics.FillEllipse(b, r);
            };

            // Text block
            var textPanel = new System.Windows.Forms.Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding   = new Padding(Spacing.Sm, 2, 0, 0),
            };
            var lblName = new AntdUI.Label
            {
                Text      = folder.Name,
                Font      = FontScale.Body9Bold,
                ForeColor = ColorPalette.TextPrimary,
                Location  = new Point(Spacing.Sm, 2),
                Size      = new Size(rowWidth - 200, 22),
            };
            var lblCount = new AntdUI.Label
            {
                Text      = $"{folder.Count ?? "0"} tài khoản",
                Font      = FontScale.Caption8,
                ForeColor = ColorPalette.TextTertiary,
                Location  = new Point(Spacing.Sm, 26),
                Size      = new Size(rowWidth - 200, 18),
            };
            textPanel.Controls.Add(lblCount);
            textPanel.Controls.Add(lblName);

            // Actions (right)
            var actions = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Right,
                Width = 80,
                BackColor = Color.Transparent,
            };

            var btnEdit = new AntdUI.Button
            {
                Ghost     = true,
                IconSvg   = "EditOutlined",
                Size      = new Size(32, 32),
                Radius    = Radius.Sm,
                Shape     = TShape.Default,
                ForeColor = ColorPalette.TextSecondary,
                Location  = new Point(4, (72 - 32) / 2 - Spacing.Md),
            };
            btnEdit.Click += async (_, __) =>
            {
                using var f = new fFolder("EditFolder", folder.Name ?? "", _platform);
                f.ShowDialog(_ownerForm ?? FindForm());
                await ReloadAllAsync();
            };

            var btnDelete = new AntdUI.Button
            {
                Ghost     = true,
                IconSvg   = "DeleteOutlined",
                Size      = new Size(32, 32),
                Radius    = Radius.Sm,
                Shape     = TShape.Default,
                ForeColor = ColorPalette.TextTertiary,
                Location  = new Point(40, (72 - 32) / 2 - Spacing.Md),
            };
            btnDelete.Click += async (_, __) =>
            {
                var owner = _ownerForm ?? FindForm();
                if (owner != null &&
                    AntdHelper.Confirm(owner, "Cảnh báo", $"Xóa nhóm [{folder.Name}]?"))
                {
                    try
                    {
                        await Task.Run(() => _folderContext.DeleteById(folder.Id));
                        await ReloadAllAsync();
                    }
                    catch (Exception ex)
                    {
                        AntdHelper.MsgError(owner, ex.Message);
                    }
                }
            };

            actions.Controls.Add(btnDelete);
            actions.Controls.Add(btnEdit);

            row.Controls.Add(textPanel);
            row.Controls.Add(actions);
            row.Controls.Add(dot);

            return row;
        }

        private static int ParseCount(string? s)
        {
            if (int.TryParse(s, out int n)) return n;
            return 0;
        }

        private static readonly Color[] _palette = new[]
        {
            Color.FromArgb(82, 196, 26),
            Color.FromArgb(22, 119, 255),
            Color.FromArgb(250, 140, 22),
            Color.FromArgb(146, 84, 222),
            Color.FromArgb(235, 47, 150),
            Color.FromArgb(250, 84, 84),
            Color.FromArgb(19, 194, 194),
        };

        private static Color FolderDotColor(int idx) => _palette[idx % _palette.Length];
    }
}
