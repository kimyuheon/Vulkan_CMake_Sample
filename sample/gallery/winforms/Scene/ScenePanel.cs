using System.Drawing;
using System.Windows.Forms;

namespace VulkanCAD.Gallery.WinForms
{
    // 왼쪽 패널 — [노드 트리 | 도면층] 탭. 어떤 기능을 보고 있든 항상 있다.
    // 엔진 ImGui 패널(lot_scene_tree_panel / lot_layer_panel)은 호스트 임베드에서 안 뜨므로 호스트가 그린다.
    public sealed class ScenePanel : Panel
    {
        readonly SceneTreeView _tree = new SceneTreeView { Dock = DockStyle.Fill };
        readonly LayerGrid _layers = new LayerGrid { Dock = DockStyle.Fill };
        readonly Panel _layerPage = new Panel { Dock = DockStyle.Fill, Visible = false };
        readonly Label _tabTree, _tabLayers;
        readonly SceneWatcher _watcher = new SceneWatcher();

        public ScenePanel()
        {
            BackColor = Theme.Panel;
            var tabs = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 28, BackColor = Theme.MenuBar, Padding = new Padding(4, 0, 0, 0) };
            _tabTree = Tab("노드 트리", true);
            _tabLayers = Tab("도면층", false);
            tabs.Controls.AddRange(new Control[] { _tabTree, _tabLayers });
            _tabTree.Click += (s, e) => Show(true);
            _tabLayers.Click += (s, e) => Show(false);

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 34, BackColor = Theme.Panel, Padding = new Padding(4) };
            buttons.Controls.Add(SmallButton("+ 새 도면층", () => SceneOps.NewLayer(LayerTable.Read())));
            buttons.Controls.Add(SmallButton("선택 → 이 층", () => { if (_layers.SelectedLayerId != uint.MaxValue) SceneOps.AssignSelected(_layers.SelectedLayerId); }));
            _layerPage.Controls.Add(_layers);
            _layerPage.Controls.Add(buttons);

            Controls.Add(_tree);
            Controls.Add(_layerPage);
            Controls.Add(tabs);
        }

        public void Start() => _watcher.Start();
        public void Stop() => _watcher.Stop();

        // 매 프레임. 바뀐 게 있을 때만 JSON 을 읽어 다시 그린다.
        public void Poll()
        {
            if (!_watcher.Poll(out bool selectionOnly)) return;
            try
            {
                var tree = SceneTree.Read();
                if (selectionOnly) _tree.RefreshSelection(tree); else _tree.Rebuild(tree);
                _layers.Fill(LayerTable.Read());
            }
            catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine("트리 갱신 실패: " + ex.Message); }   // 트리 때문에 앱이 죽지 않게
        }

        public void ShowTree(bool tree) => Show(tree);

        void Show(bool tree)
        {
            _tree.Visible = tree;
            _layerPage.Visible = !tree;
            _tabTree.ForeColor = tree ? Theme.Text : Theme.TextDim;
            _tabLayers.ForeColor = tree ? Theme.TextDim : Theme.Text;
            _tabTree.Invalidate(); _tabLayers.Invalidate();
        }

        // 리본 탭과 같은 모양 — 글자 + 활성 밑줄
        static Label Tab(string text, bool active)
        {
            var l = new Label
            {
                Text = text, AutoSize = false, Width = 90, Height = 28, TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = active ? Theme.Text : Theme.TextDim, Cursor = Cursors.Hand, Margin = new Padding(0),
            };
            l.Paint += (s, e) =>
            {
                if (l.ForeColor == Theme.Text)
                    using (var b = new SolidBrush(Theme.Accent)) e.Graphics.FillRectangle(b, 6, l.Height - 2, l.Width - 12, 2);
            };
            return l;
        }

        static Button SmallButton(string text, System.Action onClick)
        {
            var b = new Button
            {
                Text = text, AutoSize = true, FlatStyle = FlatStyle.Flat, BackColor = Theme.Control, ForeColor = Theme.Text,
                Margin = new Padding(2), TabStop = false,
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Theme.ButtonHover;
            b.Click += (s, e) => onClick();
            return b;
        }
    }
}
