using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery.WinForms
{
    // 도면층 창 — CAD_GetLayersJson 을 표로. 칸을 바꾸면 바로 엔진에 넣는다(CAD_SetLayer*).
    //   보이기 · 잠금 · 색(클릭 → 색 고르기) · 이름 · 불투명도 · 선종류 · 객체 수
    public sealed class LayerGrid : DataGridView
    {
        LayerTable _table = new LayerTable();
        uint[] _linetypeIds = new uint[0];
        string[] _linetypeNames = new string[0];
        bool _filling;

        public LayerGrid()
        {
            BackgroundColor = Theme.Panel;
            GridColor = Theme.Border;
            BorderStyle = BorderStyle.None;
            RowHeadersVisible = false;
            AllowUserToAddRows = AllowUserToDeleteRows = AllowUserToResizeRows = false;
            SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            MultiSelect = false;
            EnableHeadersVisualStyles = false;
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Theme.MenuBar, ForeColor = Theme.TextDim, SelectionBackColor = Theme.MenuBar, Font = Theme.Small,
            };
            DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Theme.Panel, ForeColor = Theme.Text, SelectionBackColor = Theme.ButtonActive,
                SelectionForeColor = Theme.Text, Font = Theme.Body,
            };
            RowTemplate.Height = 24;

            Columns.Add(new DataGridViewCheckBoxColumn { Name = "Visible", HeaderText = "👁", Width = 28 });
            Columns.Add(new DataGridViewCheckBoxColumn { Name = "Locked", HeaderText = "🔒", Width = 28 });
            Columns.Add(new DataGridViewTextBoxColumn { Name = "Color", HeaderText = "색", Width = 30, ReadOnly = true });
            Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "이름", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 70 });
            Columns.Add(new DataGridViewTextBoxColumn { Name = "Opacity", HeaderText = "불투명", Width = 46 });
            Columns.Add(new DataGridViewComboBoxColumn { Name = "Linetype", HeaderText = "선종류", Width = 74, FlatStyle = FlatStyle.Flat,
                                                          DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing });
            Columns.Add(new DataGridViewTextBoxColumn { Name = "Count", HeaderText = "객체", Width = 36, ReadOnly = true });
        }

        // 고른 층이 없으면 uint.MaxValue (0 은 기본 도면층이라 "없음" 으로 못 쓴다)
        public uint SelectedLayerId => CurrentRow?.Tag is LayerInfo l ? l.Id : uint.MaxValue;

        public void Fill(LayerTable table)
        {
            if (IsCurrentCellInEditMode) return;   // 고치는 도중엔 덮어쓰지 않는다
            _table = table;
            LoadLinetypes();
            _filling = true;
            uint keep = SelectedLayerId;
            Rows.Clear();
            foreach (var l in table.Layers)
            {
                int r = Rows.Add(l.Visible, l.Locked, "", l.Name, l.Opacity.ToString("0.##"),
                                 l.Linetype, l.SelectedCount > 0 ? $"{l.ObjectCount}·{l.SelectedCount}" : l.ObjectCount.ToString());
                var row = Rows[r];
                row.Tag = l;
                var c = Color.FromArgb((int)(l.R * 255), (int)(l.G * 255), (int)(l.B * 255));
                row.Cells["Color"].Style = new DataGridViewCellStyle { BackColor = c, SelectionBackColor = c };
                row.Cells["Name"].ReadOnly = l.Fixed;   // 도면층 0 은 이름·색 고정
                if (l.Id == keep) CurrentCell = row.Cells["Name"];
            }
            _filling = false;
        }

        void LoadLinetypes()
        {
            _linetypeIds = Cad.Ids((b, n) => CAD_GetLinetypeIds(b, n));
            _linetypeNames = _linetypeIds.Select(id => Str((b, n) => CAD_GetLinetypeName(id, b, n))).ToArray();
            var col = (DataGridViewComboBoxColumn)Columns["Linetype"];
            col.Items.Clear();
            col.Items.AddRange(_linetypeNames);
        }

        // 체크 상자·콤보는 고르는 즉시 반영되게
        protected override void OnCurrentCellDirtyStateChanged(EventArgs e)
        {
            base.OnCurrentCellDirtyStateChanged(e);
            if (IsCurrentCellDirty && CurrentCell is DataGridViewCheckBoxCell or DataGridViewComboBoxCell)
                CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        protected override void OnCellValueChanged(DataGridViewCellEventArgs e)
        {
            base.OnCellValueChanged(e);
            if (_filling || e.RowIndex < 0 || Rows[e.RowIndex].Tag is not LayerInfo l) return;
            var v = Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
            switch (Columns[e.ColumnIndex].Name)
            {
                case "Visible": CAD_SetLayerVisible(l.Id, (bool)v); break;
                case "Locked": CAD_SetLayerLocked(l.Id, (bool)v); break;
                case "Name": if (!CAD_RenameLayer(l.Id, v as string ?? "")) Rows[e.RowIndex].Cells[e.ColumnIndex].Value = l.Name; break;
                case "Opacity":
                    if (float.TryParse(v as string, out float o)) CAD_SetLayerOpacity(l.Id, Math.Clamp(o, 0.05f, 1));
                    break;
                case "Linetype":
                    int i = Array.IndexOf(_linetypeNames, v as string);
                    if (i >= 0) CAD_SetLayerLinetype(l.Id, _linetypeIds[i]);
                    break;
            }
        }

        protected override void OnCellClick(DataGridViewCellEventArgs e)
        {
            base.OnCellClick(e);
            if (e.RowIndex < 0 || Columns[e.ColumnIndex].Name != "Color" || Rows[e.RowIndex].Tag is not LayerInfo l || l.Fixed) return;
            using var dlg = new ColorDialog { Color = Color.FromArgb((int)(l.R * 255), (int)(l.G * 255), (int)(l.B * 255)), FullOpen = true };
            if (dlg.ShowDialog(this) == DialogResult.OK)
                CAD_SetLayerColor(l.Id, dlg.Color.R / 255f, dlg.Color.G / 255f, dlg.Color.B / 255f);
        }

        // 숫자 아닌 값을 넣었을 때 기본 오류 상자를 막는다
        protected override void OnDataError(bool displayErrorDialogIfNoHandler, DataGridViewDataErrorEventArgs e) => e.Cancel = true;
    }
}
