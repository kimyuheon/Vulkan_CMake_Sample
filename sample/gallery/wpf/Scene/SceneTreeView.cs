using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace VulkanCAD.Gallery.Wpf
{
    // 노드 트리 — CAD_GetSceneTreeJson 을 WPF TreeView 로.
    //   체크 = 보이기 · 이름 클릭 = 선택(Ctrl = 추가) · 객체 줄 F2 = 이름 바꾸기 · Del = 삭제 · 오른쪽 클릭 = 메뉴
    // 장면이 바뀌면 통째로 다시 만들되, 펼침 상태는 노드 key 로 기억해 되살린다.
    public sealed class SceneTreeView : TreeView
    {
        SceneTree _tree = new SceneTree();
        readonly HashSet<string> _collapsed = new HashSet<string>();
        readonly Dictionary<string, TreeViewItem> _byKey = new Dictionary<string, TreeViewItem>();

        public SceneTreeView()
        {
            Background = (Brush)FindResource("Panel");
            BorderThickness = new Thickness(0);
            Foreground = (Brush)FindResource("Text");
            ContextMenu = BuildMenu();
        }

        public void Rebuild(SceneTree tree)
        {
            _tree = tree;
            Items.Clear();
            _byKey.Clear();
            if (tree.Root != null) Items.Add(Make(tree.Root));
        }

        // 선택만 바뀐 경우 — 다시 만들지 않고 줄 배경만 고친다
        public void RefreshSelection(SceneTree tree)
        {
            _tree = tree;
            foreach (var n in tree.Nodes)
                if (n.IsObject && _byKey.TryGetValue(n.Key, out var item)) Highlight(item, n);
        }

        TreeViewItem Make(TreeNode n)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal };
            if (n.Kind != "feature")   // 피처 기록 줄은 객체가 아니라 보이기가 없다
            {
                var check = new CheckBox { IsChecked = n.IsObject ? n.Visible : n.Shown > 0, Margin = new Thickness(0, 0, 4, 0), VerticalAlignment = VerticalAlignment.Center };
                check.Click += (s, e) => SceneOps.SetVisible(_tree.ObjectIds(n), check.IsChecked == true);
                row.Children.Add(check);
            }
            var label = new TextBlock
            {
                Text = n.Kind switch { "object" => n.Label, "feature" => "· " + n.Label, _ => $"{n.Label}  ({n.Shown}/{n.Total})" },
                Foreground = (Brush)FindResource(n.Kind == "feature" || n.Locked ? "TextDim" : "Text"),
                FontWeight = n.Kind is "scene" or "file" or "node" ? FontWeights.Bold : FontWeights.Normal,
                Padding = new Thickness(2, 1, 6, 1),
            };
            row.Children.Add(label);

            var item = new TreeViewItem { Header = row, Tag = n, IsExpanded = !_collapsed.Contains(n.Key), Foreground = Foreground };
            item.Expanded += (s, e) => { if (e.OriginalSource == item) _collapsed.Remove(n.Key); };
            item.Collapsed += (s, e) => { if (e.OriginalSource == item) _collapsed.Add(n.Key); };
            label.MouseLeftButtonDown += (s, e) =>
            {
                if (n.Kind == "feature") return;
                SceneOps.Select(_tree.ObjectIds(n), (Keyboard.Modifiers & ModifierKeys.Control) != 0);
            };
            _byKey[n.Key] = item;
            Highlight(item, n);
            foreach (int c in n.Children) item.Items.Add(Make(_tree.Nodes[c]));
            return item;
        }

        void Highlight(TreeViewItem item, TreeNode n)
        {
            if (item.Header is StackPanel row && row.Children[^1] is TextBlock label)
                label.Background = n.Selected ? (Brush)FindResource("ButtonActive") : Brushes.Transparent;
        }

        TreeNode Current => (SelectedItem as TreeViewItem)?.Tag as TreeNode;
        List<uint> CurrentIds() => Current == null ? new List<uint>() : _tree.ObjectIds(Current);

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.F2) BeginRename();
            if (e.Key == Key.Delete) SceneOps.Delete(CurrentIds());
        }

        // 객체 줄의 글자를 입력란으로 바꾼다. Enter = 확정, ESC·포커스 이탈 = 취소
        void BeginRename()
        {
            if (Current?.IsObject != true || SelectedItem is not TreeViewItem item || item.Header is not StackPanel row) return;
            var label = (TextBlock)row.Children[^1];
            var box = new TextBox { Text = label.Text, MinWidth = 120 };
            int at = row.Children.Count - 1;
            row.Children[at] = box;
            uint id = Current.ObjectId;
            void End(bool commit)
            {
                if (row.Children[at] != box) return;
                row.Children[at] = label;
                if (commit && !SceneOps.Rename(id, box.Text)) label.Text += "  (이름 중복)";
            }
            box.KeyDown += (s, e) => { if (e.Key == Key.Enter) End(true); if (e.Key == Key.Escape) End(false); e.Handled = true; };
            box.LostFocus += (s, e) => End(false);
            box.Loaded += (s, e) => { box.Focus(); box.SelectAll(); };
        }

        ContextMenu BuildMenu()
        {
            var m = new ContextMenu();
            void Add(string text, System.Action a) { var i = new MenuItem { Header = text }; i.Click += (s, e) => a(); m.Items.Add(i); }
            Add("여기로 줌", () => SceneOps.ZoomTo(CurrentIds()));
            Add("보이기", () => SceneOps.SetVisible(CurrentIds(), true));
            Add("숨기기", () => SceneOps.SetVisible(CurrentIds(), false));
            Add("이름 바꾸기 (F2)", BeginRename);
            m.Items.Add(new Separator());
            Add("삭제 (Del)", () => SceneOps.Delete(CurrentIds()));
            return m;
        }
    }
}
