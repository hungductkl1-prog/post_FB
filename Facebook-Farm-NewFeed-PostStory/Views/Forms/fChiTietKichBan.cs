using AntdUI;
using Facebook_Farm_NewFeed_PostStory.Utils;
using Facebook_Farm_NewFeed_PostStory.Views.Forms.Actions;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Models;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;
using System.Reflection;
using Facebook_Farm_NewFeed_PostStory.Utils.Design;

using CommonMethod = Sunny.Subdy.Common.ControlMethod.CommonMethod;
namespace Facebook_Farm_NewFeed_PostStory.Views.Forms
{
    public partial class fChiTietKichBan : AntdUI.Window
    {
        private int rowIndexFromMouseDown;
        private int rowIndexOfItemUnderMouseToDrop;
        AntdUI.IContextMenuStripItem[] menulist;
        private Guid _idScript;
        private ScriptContext _scriptContext;
        private ScriptActionContext _scriptActionContext;
        private Script _script;
        public List<ScriptAction> _scriptAction;
        private SortableBindingList<ScriptAction> bindingList;
        private List<ScriptAction> _clipboardRows = new();
        public fChiTietKichBan(Guid idScript)
        {
            InitializeComponent();
            _idScript = idScript;
            _scriptAction = new List<ScriptAction>();
            _scriptContext = new ScriptContext();
            _scriptActionContext = new ScriptActionContext();
            _script = _scriptContext.GetById(idScript);
            if (_script == null)
            {
                this.Load += (s, e) =>
                {
                    AntdHelper.NotifyWarn(this, "Cảnh báo", $"Không tìm thấy kịch bản (Id: {idScript}). Vui lòng tải lại danh sách.");
                    this.Close();
                };
                return;
            }
            textBox1.Text = _script.Name;
            bindingList = new SortableBindingList<ScriptAction>(_scriptAction);
            dataGridView1.MultiSelect = false;
            dataGridView1.DefaultCellStyle.BackColor = Color.White;
            dataGridView1.DefaultCellStyle.ForeColor = Color.DarkGray;
            dataGridView1.DefaultCellStyle.Font = FontScale.Body9Bold;
            dataGridView1.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 215);
            dataGridView1.DefaultCellStyle.SelectionForeColor = Color.White;
            dataGridView1.DefaultCellStyle.ForeColor = ColorTranslator.FromHtml("#1A1A1A");
            dataGridView1.AlternatingRowsDefaultCellStyle.BackColor = Color.White;
            dataGridView1.AutoGenerateColumns = false;
            dataGridView1.AllowDrop = true;
            dataGridView1.MouseDown += dataGridView1_MouseDown;
            dataGridView1.DragOver += dataGridView1_DragOver;
            dataGridView1.DragDrop += dataGridView1_DragDrop;
            dataGridView1.KeyDown += dataGridView1_KeyDown;

            menulist = null;
            CreateMenuStrip();
            LoadColumnsDataGridView();
            AddActionButtons();

            FontUtil.ApplyFontToAllControls(this);
            LoadData();
        }

        private void CreateMenuStrip()
        {
            var items = new List<AntdUI.IContextMenuStripItem>
          {
                 new AntdUI.ContextMenuStripItem("Xóa hành động"),
                 new AntdUI.ContextMenuStripItem("Sửa hành động"),
                 new AntdUI.ContextMenuStripItem("Sắp xếp lên trên"),
                 new AntdUI.ContextMenuStripItem("Sắp xếp xuống dưới"),
                 new AntdUI.ContextMenuStripItem("Nhân đôi hành động"),
        };

            menulist = items.ToArray();
        }
        private void LoadData()
        {
            _scriptAction = _scriptActionContext.GetByScriptId(_idScript)
                                               .OrderBy(x => x.ByOrder)
                                               .ToList();

            // Map miêu tả từ dictionary
            foreach (var act in _scriptAction)
            {
                if (FacebookFarmingType.DescriptionAction.TryGetValue(act.Type, out var desc))
                {
                    act.MieuTa = desc;
                }
                else if (InstagramFarmingType.DescriptionAction.TryGetValue(act.Type, out var igDesc))
                {
                    act.MieuTa = igDesc;
                }
                else if (ThreadsFarmingType.DescriptionAction.TryGetValue(act.Type, out var trDesc))
                {
                    act.MieuTa = trDesc;
                }
                else
                {
                    act.MieuTa = "Không có mô tả";
                }
            }

            bindingList = new SortableBindingList<ScriptAction>(_scriptAction);
            dataGridView1.DataSource = bindingList;
            label3.Text = $"Danh sách hành động của kịch bản ({_scriptAction.Count})";
        }

        private void button5_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            try
            {
                Form actionsWindow = _script.Platform switch
                {
                    PlatformModel.Instagram => new fActionsInstagram(_idScript.ToString()),
                    PlatformModel.Threads => (Form)new fActionsThreads(_idScript.ToString()),
                    _ => new fActions(_idScript.ToString())
                };
                this.Cursor = Cursors.Default;
                actionsWindow.ShowDialog();
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
            LoadData();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (ScriptNames.IsBuiltIn(_script.Name))
            {
                AntdHelper.NotifyWarn(this, "Cảnh báo", $"Không thể xóa kịch bản mặc định {_script.Name}.");
                return;
            }
            if (CommonMethod.ShowConfirmWarning("Bạn có chắc chắn muốn xóa kịch bản này không?"))
            {
                if (_scriptContext.DeleteById(_script.Id))
                {
                    _scriptActionContext.DeleteByScriptId(_script.Id);
                    RemapAccountsToFarmXu(_script.Name);
                }

                this.Close();
            }
        }

        private void RemapAccountsToFarmXu(string deletedScriptName)
        {
            if (string.IsNullOrEmpty(deletedScriptName)) return;
            var accountContext = new AccountContext();
            var all = accountContext.GetAll(new List<string>(), _script.Platform, true);
            if (all == null || all.Count == 0) return;
            foreach (var acc in all)
            {
                if (acc.NameScript == deletedScriptName)
                    acc.NameScript = "";
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            fFolder folder = new fFolder("EditScript", _script.Name, _script.Platform);
            folder.ShowDialog();
            _script = _scriptContext.GetById(_idScript);
            textBox1.Text = _script.Name;
        }
        private void LoadColumnsDataGridView()
        {
            var style = new DataGridViewCellStyle
            {
                Font = FontScale.Body9Bold

            };
            style.ForeColor = Color.FromArgb(0, 120, 215);
            dataGridViewTextBoxColumn1.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = FontScale.Body9Bold,
                ForeColor = Color.FromArgb(0, 120, 215),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            dataGridViewTextBoxColumn1.ToolTipText = "Số thứ tự trong bảng";
            dataGridViewTextBoxColumn1.DataPropertyName = nameof(ScriptAction.ByOrder);
            var columns = new List<(string Name, string Header, string Tooltip, bool visible)>
    {
        (nameof(ScriptAction.Name), "Tên", "Tên hành động", true),
        ("MieuTa", "Miêu tả", "Miêu tả hành động", true),
        (nameof(ScriptAction.Id), nameof(ScriptAction.Id), "", false)
    };

            var colDefs = new List<DataGridViewColumn>();

            foreach (var col in columns)
            {

                string header = col.Header;
                string tooltip = col.Tooltip;

                colDefs.Add(CreateColumnsDataGridView(
                    col.Name,
                    header,
                    tooltip,
                    col.visible,
                    col.Name == "MieuTa" ? 300 : 200,
                    col.Name == "MieuTa" ? DataGridViewAutoSizeColumnMode.Fill : DataGridViewAutoSizeColumnMode.None,
                    style
                ));
            }

            dataGridView1.Columns.AddRange(colDefs.ToArray());
        }
        private void AddActionButtons()
        {
            // Cột Sửa
            DataGridViewButtonColumn editButton = new DataGridViewButtonColumn
            {
                Name = "col_Edit",
                HeaderText = "Sửa",
                Text = "Sửa",
                UseColumnTextForButtonValue = true,
                Width = 60
            };

            // Cột Xóa
            DataGridViewButtonColumn deleteButton = new DataGridViewButtonColumn
            {
                Name = "col_Delete",
                HeaderText = "Xóa",
                Text = "Xóa",
                UseColumnTextForButtonValue = true,
                Width = 60
            };

            if (!dataGridView1.Columns.Contains("col_Edit"))
                dataGridView1.Columns.Add(editButton);

            if (!dataGridView1.Columns.Contains("col_Delete"))
                dataGridView1.Columns.Add(deleteButton);

            dataGridView1.CellContentClick -= dataGridView1_CellContentClick;
            dataGridView1.CellContentClick += dataGridView1_CellContentClick;
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var row = dataGridView1.Rows[e.RowIndex].DataBoundItem as ScriptAction;
            if (row == null) return;

            string colName = dataGridView1.Columns[e.ColumnIndex].Name;

            if (colName == "col_Edit")
            {
                OpenPage(row); return;
            }
            else if (colName == "col_Delete")
            {
                if (CommonMethod.ShowConfirmWarning("Bạn có chắc muốn xóa hành động này?"))
                {
                    _scriptActionContext.DeleteById(row.Id);
                    LoadData();
                }
            }
        }
        private void dataGridView1_MouseDown(object sender, MouseEventArgs e)
        {
            var hit = dataGridView1.HitTest(e.X, e.Y);

            // Nếu click vào cột button -> KHÔNG dragdrop
            if (hit.Type == DataGridViewHitTestType.Cell)
            {
                var col = dataGridView1.Columns[hit.ColumnIndex];
                if (col is DataGridViewButtonColumn)
                {
                    return; // bỏ qua drag, để CellContentClick xử lý
                }
            }

            // Chỉ drag nếu click vào row bình thường
            rowIndexFromMouseDown = hit.RowIndex;
            if (rowIndexFromMouseDown != -1)
            {
                dataGridView1.DoDragDrop(
                    dataGridView1.Rows[rowIndexFromMouseDown],
                    DragDropEffects.Move);
            }
        }

        private void dataGridView1_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void dataGridView1_DragDrop(object sender, DragEventArgs e)
        {
            Point clientPoint = dataGridView1.PointToClient(new Point(e.X, e.Y));
            rowIndexOfItemUnderMouseToDrop =
                dataGridView1.HitTest(clientPoint.X, clientPoint.Y).RowIndex;

            if (rowIndexOfItemUnderMouseToDrop < 0 || rowIndexFromMouseDown < 0)
                return;

            // Hoán đổi vị trí trong list
            var item = bindingList[rowIndexFromMouseDown];
            bindingList.RemoveAt(rowIndexFromMouseDown);
            bindingList.Insert(rowIndexOfItemUnderMouseToDrop, item);

            // Cập nhật ByOrder theo thứ tự mới
            for (int i = 0; i < bindingList.Count; i++)
            {
                bindingList[i].ByOrder = i + 1;
                _scriptActionContext.Update(bindingList[i]); // update DB
            }
        }
        private DataGridViewColumn CreateColumnsDataGridView(string dataPropertyName, string header, string toolTip, bool visible, int miniWith, DataGridViewAutoSizeColumnMode size, DataGridViewCellStyle style)
        {
            DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn();
            column.DefaultCellStyle = style;
            column.DataPropertyName = dataPropertyName;
            column.Name = "col_" + dataPropertyName;
            column.HeaderText = header;
            column.ToolTipText = toolTip;
            column.ReadOnly = true;
            column.MinimumWidth = miniWith;
            column.Visible = visible;
            column.AutoSizeMode = size;
            return column;
        }
        private void dataGridView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.C) // Copy
            {
                CopySelectedRows();
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.V) // Paste
            {
                PasteRows();
                e.Handled = true;
            }
        }

        private void CopySelectedRows()
        {
            _clipboardRows.Clear();
            foreach (DataGridViewRow row in dataGridView1.SelectedRows)
            {
                if (row.DataBoundItem is ScriptAction action)
                {

                    // Clone object (tạo bản copy mới để không trùng Id)
                    var clone = new ScriptAction
                    {
                        Id = Guid.NewGuid(),
                        Name = action.Name,
                        Type = action.Type,
                        Json = action.Json,
                        MieuTa = action.MieuTa,
                        Platform = action.Platform,
                        ScriptId = action.ScriptId,
                        ByOrder = action.ByOrder
                    };
                    _clipboardRows.Add(clone);
                }
            }
        }

        private void PasteRows()
        {
            if (_clipboardRows.Count == 0) return;

            foreach (var action in _clipboardRows)
            {
                var actionNew = new ScriptAction
                {
                    Id = Guid.NewGuid(),
                    Name = action.Name,
                    Type = action.Type,
                    Json = action.Json,
                    Platform = action.Platform,
                    ScriptId = action.ScriptId,
                    MieuTa = action.MieuTa
                };
                var scriptAction = _scriptActionContext.GetByScriptId(_script.Id);
                var index = (scriptAction != null && scriptAction.Count > 0)
                  ? scriptAction.Max(a => a.ByOrder) + 1
                  : 1;
                string baseName = action.Name;
                string newName = baseName;
                if (baseName.Contains(" - Copy"))
                {
                    int pos = baseName.IndexOf(" - Copy", StringComparison.Ordinal);
                    baseName = baseName.Substring(0, pos);
                }
                var existingCopies = scriptAction
               .Where(a => a.Name.StartsWith(baseName))
               .Select(a => a.Name)
               .ToList();
                int copyIndex = 0;
                newName = $"{baseName} - Copy";
                while (existingCopies.Contains(newName))
                {
                    copyIndex++;
                    newName = $"{baseName} - Copy{copyIndex}";
                }
                actionNew.Name = newName;
                actionNew.ByOrder = index;
                bindingList.Add(actionNew);
                _scriptActionContext.Add(actionNew);
            }
        }

        private void btn_setting_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btn_global_Click(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Maximized)
                this.WindowState = FormWindowState.Normal;
            else
                this.WindowState = FormWindowState.Maximized;
        }

        private void button9_Click(object sender, EventArgs e)
        {
            LoadData();
        }
        public void OpenPage(ScriptAction action)
        {
            Form form = null;
            switch (action.Type)
            {
                case FacebookFarmingType.HDDocThongBao:
                    form = new fHDDocThongBao(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case FacebookFarmingType.HDXemReel:
                    form = new fHDXemReel(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case FacebookFarmingType.HDXemStory:
                    form = new fHDXemStory(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case FacebookFarmingType.HDXemWatch:
                    form = new fHDXemWatch(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case FacebookFarmingType.HDTuongTacNewfeed:
                    form = new fHDTuongTacNewfeed(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case FacebookFarmingType.HDTuongTacBanBe:
                    form = new fHDTuongTacBanBe(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case FacebookFarmingType.HDTuongTacNhom:
                    form = new fHDTuongTacNhom(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case FacebookFarmingType.HDTuongTacPage:
                    form = new fHDTuongTacPage(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case FacebookFarmingType.HDTuongTacWall:
                    form = new fHDTuongTacWall(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case FacebookFarmingType.HDTuongTacEvent:
                    form = new fHDTuongTacEvent(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case FacebookFarmingType.HDTuongTacBaiViet:
                    form = new fHDTuongTacBaiViet(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case FacebookFarmingType.HDTuongTacVideoLivestream:
                    form = new fHDTuongTacLivestream(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case FacebookFarmingType.HDGuiLoiMoiKetBan:
                    form = new fHDGuiLoiMoiKetBan(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case FacebookFarmingType.HDXacNhanKetBan:
                    form = new fHDXacNhanKetBan(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case FacebookFarmingType.HDHuyKetBan:
                    form = new fHDHuyKetBan(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case FacebookFarmingType.HDThamGiaNhom:
                    form = new fHDThamGiaNhom(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case FacebookFarmingType.HDRoiNhom:
                    form = new fHDRoiNhom(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case FacebookFarmingType.HDTaoNhom:
                    form = new fHDTaoNhom(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case FacebookFarmingType.HDTaoPage:
                    form = new fHDTaoPage(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case FacebookFarmingType.HDDangBaiTuong:
                    form = new fHDDangBaiTuong(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case FacebookFarmingType.HDDangBaiNhom:
                    form = new fHDDangBaiNhom(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case FacebookFarmingType.HDShareBaiNangCao:
                    form = new fHDShareBaiNangCao(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case FacebookFarmingType.HDDangReel:
                    form = new fHDDangReel(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case FacebookFarmingType.HDDangStory:
                    form = new fHDDangStory(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case FacebookFarmingType.HDDanhGiaPage:
                    form = new fHDDanhGiaPage(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case FacebookFarmingType.HDBuffFollowUID:
                    form = new fHDBuffFollowUID(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case FacebookFarmingType.HDMoiBanBeLikePage:
                    form = new fHDMoiBanBeLikePage(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case FacebookFarmingType.HDMoiBanBeVaoNhom:
                    form = new fHDMoiBanBeVaoNhom(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case FacebookFarmingType.HDDoiTen:
                    form = new fHDDoiTen(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case FacebookFarmingType.HDDoiMatKhau:
                    form = new fHDDoiMatKhau(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case FacebookFarmingType.HDOnOff2FA:
                    form = new fHDOnOff2FA(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case FacebookFarmingType.HDXoaSdt:
                    form = new fHDXoaSdt(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case FacebookFarmingType.HDAddMail:
                    form = new fHDAddMail(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case FacebookFarmingType.HDCapNhatThongTin:
                    form = new fHDCapNhatThongTin(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case FacebookFarmingType.HDDangXuatThietBiCu:
                    form = new fHDDangXuatThietBiCu(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case FacebookFarmingType.HDXoaThietBiTinCay:
                    form = new fHDXoaThietBiTinCay(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case FacebookFarmingType.HDBatCheDoChuyenNghiep:
                    form = new fHDBatCheDoChuyenNghiep(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case FacebookFarmingType.HDDongBoDanhBa:
                    form = new fHDDongBoDanhBa(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case FacebookFarmingType.HDTimKiemGoogle:
                    form = new fHDTimKiemGoogle(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case FacebookFarmingType.HDNhanTinBanBe:
                    form = new fHDNhanTinBanBe(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case FacebookFarmingType.HDUpAvatar:
                    form = new fHDUpAvatar(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case FacebookFarmingType.HDUpCover:
                    form = new fHDUpCover(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case FacebookFarmingType.HDNghiGiaiLao:
                    form = new fHDNghiGiaiLao(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case FacebookFarmingType.HDDangBaiPage:
                    form = new fHDDangBaiPage(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case FacebookFarmingType.HDBuffLikePage:
                    form = new fHDBuffLikePage(action.ScriptId.ToString(), action.Id.ToString());
                    break;

                // Instagram
                case InstagramFarmingType.IGXemReel:
                    form = new fIGXemReel(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case InstagramFarmingType.IGXemStory:
                    form = new fIGXemStory(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case InstagramFarmingType.IGTuongTacNewfeed:
                    form = new fIGTuongTacNewfeed(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case InstagramFarmingType.IGDangBai:
                    form = new fIGDangBai(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case InstagramFarmingType.IGDangReel:
                    form = new fIGDangReel(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case InstagramFarmingType.IGDangStory:
                    form = new fIGDangStory(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case InstagramFarmingType.IGFollow:
                    form = new fIGFollow(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case InstagramFarmingType.IGUnfollow:
                    form = new fIGUnfollow(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case InstagramFarmingType.IGNhanTin:
                    form = new fIGNhanTin(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case InstagramFarmingType.IGCapNhatThongTin:
                    form = new fIGCapNhatThongTin(action.ScriptId.ToString(), action.Id.ToString());
                    break;

                // Threads
                case ThreadsFarmingType.TRXemReel:
                    form = new fTRXemReel(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case ThreadsFarmingType.TRXemStory:
                    form = new fTRXemStory(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case ThreadsFarmingType.TRTuongTacNewfeed:
                    form = new fTRTuongTacNewfeed(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case ThreadsFarmingType.TRDangBai:
                    form = new fTRDangBai(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case ThreadsFarmingType.TRDangReel:
                    form = new fTRDangReel(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case ThreadsFarmingType.TRDangStory:
                    form = new fTRDangStory(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case ThreadsFarmingType.TRFollow:
                    form = new fTRFollow(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case ThreadsFarmingType.TRUnfollow:
                    form = new fTRUnfollow(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case ThreadsFarmingType.TRNhanTin:
                    form = new fTRNhanTin(action.ScriptId.ToString(), action.Id.ToString());
                    break;
                case ThreadsFarmingType.TRCapNhatThongTin:
                    form = new fTRCapNhatThongTin(action.ScriptId.ToString(), action.Id.ToString());
                    break;
            }
            if (form == null) return;
            var result = form.ShowDialog();
        }
    }
}
