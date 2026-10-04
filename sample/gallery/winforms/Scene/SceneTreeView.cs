using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace VulkanCAD.Gallery.WinForms
{
    // 노드 트리 — CAD_GetSceneTreeJson 을 WinForms TreeView 로.
    //   체크 = 보이기 · 클릭 = 선택(Ctrl = 추가) · 객체 줄 F2/두 번 클릭 = 이름 바꾸기 · 오른쪽 클릭 = 메뉴
    // 장면이 바뀌면 통째로 다시 만들되, 펼침 상태는 노드 key 로 기억해 되살린다.
    public sealed class SceneTreeView : TreeView
    {
        SceneTree _tree = new SceneTree();
        readonly HashSet<string> _collapsed = new HashSet<string>();
        readonly Dictionary<string, System.Windows.Forms.TreeNode> _byKey = new Dictionary<string, System.Windows.Forms.TreeNode>();
        bool _building;   // 다시 만드는 중엔 체크·선택 이벤트를 엔진으로 보내지 않는다

        public SceneTreeView()
        {
            BackColor = Theme.Panel;
            ForeColor = Theme.Text;
            BorderStyle = BorderStyle.None;
            CheckBoxes = true;
            LabelEdit = true;
            HideSelection = false;
            FullRowSelect = true;
            ShowLines = false;
            ItemHeight = 22;
            Font = Theme.Body;
            ContextMenuStrip = BuildMenu();
        }

        public void Rebuild(SceneTree tree)
        {
            _tree = tree;
            _building = true;
            BeginUpdate();
            Nodes.Clear();
            _byKey.Clear();
            if (tree.Root != null) Nodes.Add(Make(tree.Root));
            foreach (var tn in _byKey.Values)
                if (Model(tn).Kind == "feature") HideCheckBox(tn);   // 피처 기록 줄은 객체가 아니라 보이기가 없다
            EndUpdate();
            _building = false;
        }

        // TreeView 는 줄마다 체크 상자를 끌 수 없어서 Win32 로 상태 이미지를 지운다
        void HideCheckBox(System.Windows.Forms.TreeNode tn)
        {
            const int TVIF_STATE = 0x8, TVIS_STATEIMAGEMASK = 0xF000, TV_FIRST = 0x1100, TVM_SETITEMW = TV_FIRST + 63;
            var item = new TVITEM { mask = TVIF_STATE, hItem = tn.Handle, stateMask = TVIS_STATEIMAGEMASK, state = 0 };
            SendMessage(Handle, TVM_SETITEMW, IntPtr.Zero, ref item);
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        struct TVITEM
        {
            public int mask;
            public IntPtr hItem;
            public int state, stateMask;
            public IntPtr pszText;
            public int cchTextMax, iImage, iSelectedImage, cChildren;
            public IntPtr lParam;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref TVITEM item);

        // 선택만 바뀐 경우 — 다시 만들지 않고 색만 고친다
        public void RefreshSelection(SceneTree tree)
        {
            _tree = tree;
            foreach (var n in tree.Nodes)
                if (n.IsObject && _byKey.TryGetValue(n.Key, out var tn)) Style(tn, n);
        }

        System.Windows.Forms.TreeNode Make(TreeNode n)
        {
            var tn = new System.Windows.Forms.TreeNode(Label(n)) { Tag = n };
            _byKey[n.Key] = tn;
            tn.Checked = n.IsObject ? n.Visible : n.Shown > 0;
            Style(tn, n);
            foreach (int c in n.Children) tn.Nodes.Add(Make(_tree.Nodes[c]));
            if (_collapsed.Contains(n.Key)) tn.Collapse(); else tn.Expand();
            return tn;
        }

        static string Label(TreeNode n) => n.Kind switch
        {
            "object" => n.Label,
            "feature" => "· " + n.Label,
            _ => $"{n.Label}  ({n.Shown}/{n.Total})",
        };

        static void Style(System.Windows.Forms.TreeNode tn, TreeNode n)
        {
            tn.ForeColor = n.Kind == "feature" ? Theme.TextDim : n.Locked ? Theme.TextDim : Theme.Text;
            tn.BackColor = n.Selected ? Theme.ButtonActive : Theme.Panel;
            if (n.Kind is "scene" or "file" or "node") tn.NodeFont = Theme.Section;
        }

        TreeNode Model(System.Windows.Forms.TreeNode tn) => tn?.Tag as TreeNode;

        // ── 사용자 동작 → 엔진 ──
        protected override void OnAfterCollapse(TreeViewEventArgs e) { if (!_building) _collapsed.Add(Model(e.Node).Key); base.OnAfterCollapse(e); }
        protected override void OnAfterExpand(TreeViewEventArgs e) { if (!_building) _collapsed.Remove(Model(e.Node).Key); base.OnAfterExpand(e); }

        protected override void OnNodeMouseClick(TreeNodeMouseClickEventArgs e)
        {
            base.OnNodeMouseClick(e);
            SelectedNode = e.Node;
            if (e.Button != MouseButtons.Left || e.X < e.Node.Bounds.Left) return;   // 체크 상자·펼침 단추 클릭은 선택이 아니다
            var n = Model(e.Node);
            if (n.Kind == "feature") return;
            SceneOps.Select(_tree.ObjectIds(n), (ModifierKeys & Keys.Control) != 0);
        }

        protected override void OnAfterCheck(TreeViewEventArgs e)
        {
            base.OnAfterCheck(e);
            if (_building || e.Action == TreeViewAction.Unknown) return;
            SceneOps.SetVisible(_tree.ObjectIds(Model(e.Node)), e.Node.Checked);
        }

        protected override void OnBeforeLabelEdit(NodeLabelEditEventArgs e)
        {
            base.OnBeforeLabelEdit(e);
            if (Model(e.Node)?.IsObject != true) e.CancelEdit = true;   // 묶음·피처 줄은 이름이 엔진 것이 아니다
        }

        protected override void OnAfterLabelEdit(NodeLabelEditEventArgs e)
        {
            base.OnAfterLabelEdit(e);
            if (e.Label == null) return;   // ESC
            if (!SceneOps.Rename(Model(e.Node).ObjectId, e.Label)) e.CancelEdit = true;   // 이름이 겹치면 엔진이 거절
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.F2 && SelectedNode != null) SelectedNode.BeginEdit();
            if (e.KeyCode == Keys.Delete && SelectedNode != null) SceneOps.Delete(_tree.ObjectIds(Model(SelectedNode)));
        }

        ContextMenuStrip BuildMenu()
        {
            var m = new ContextMenuStrip();
            List<uint> Ids() => SelectedNode == null ? new List<uint>() : _tree.ObjectIds(Model(SelectedNode));
            m.Items.Add("여기로 줌", null, (s, e) => SceneOps.ZoomTo(Ids()));
            m.Items.Add("보이기", null, (s, e) => SceneOps.SetVisible(Ids(), true));
            m.Items.Add("숨기기", null, (s, e) => SceneOps.SetVisible(Ids(), false));
            m.Items.Add("이름 바꾸기 (F2)", null, (s, e) => SelectedNode?.BeginEdit());
            m.Items.Add(new ToolStripSeparator());
            m.Items.Add("삭제 (Del)", null, (s, e) => SceneOps.Delete(Ids()));
            return m;
        }
    }
}
