using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace VulkanCAD.Gallery.Wpf
{
    // 왼쪽 패널 — [노드 트리 | 도면층] 탭. 어떤 기능을 보고 있든 항상 있다.
    // 엔진 ImGui 패널(lot_scene_tree_panel / lot_layer_panel)은 호스트 임베드에서 안 뜨므로 호스트가 그린다.
    public sealed class ScenePanel : DockPanel
    {
        readonly SceneTreeView _tree = new SceneTreeView();
        readonly LayerList _layers = new LayerList();
        readonly DockPanel _layerPage = new DockPanel { Visibility = Visibility.Collapsed };
        readonly TextBlock _tabTree, _tabLayers;
        readonly Border _lineTree, _lineLayers;
        readonly SceneWatcher _watcher = new SceneWatcher();

        public ScenePanel()
        {
            Background = (Brush)FindResource("Panel");
            var tabs = new StackPanel { Orientation = Orientation.Horizontal, Background = (Brush)FindResource("MenuBar") };
            (_tabTree, _lineTree) = Tab(tabs, "노드 트리", () => Show(true));
            (_tabLayers, _lineLayers) = Tab(tabs, "도면층", () => Show(false));
            SetDock(tabs, Dock.Top);
            Children.Add(tabs);

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(4) };
            buttons.Children.Add(SmallButton("+ 새 도면층", () => SceneOps.NewLayer(LayerTable.Read())));
            buttons.Children.Add(SmallButton("선택 → 이 층", () => { if (_layers.SelectedLayerId != uint.MaxValue) SceneOps.AssignSelected(_layers.SelectedLayerId); }));
            SetDock(buttons, Dock.Bottom);
            _layerPage.Children.Add(buttons);
            _layerPage.Children.Add(_layers);

            var pages = new Grid();
            pages.Children.Add(_tree);
            pages.Children.Add(_layerPage);
            Children.Add(pages);
            Show(true);
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
            _tree.Visibility = tree ? Visibility.Visible : Visibility.Collapsed;
            _layerPage.Visibility = tree ? Visibility.Collapsed : Visibility.Visible;
            _tabTree.Foreground = (Brush)FindResource(tree ? "Text" : "TextDim");
            _tabLayers.Foreground = (Brush)FindResource(tree ? "TextDim" : "Text");
            _lineTree.Visibility = tree ? Visibility.Visible : Visibility.Hidden;
            _lineLayers.Visibility = tree ? Visibility.Hidden : Visibility.Visible;
        }

        // 리본 탭과 같은 모양 — 글자 + 활성 밑줄
        (TextBlock, Border) Tab(Panel host, string text, System.Action onClick)
        {
            var label = new TextBlock { Text = text, Margin = new Thickness(14, 6, 14, 4) };
            var line = new Border { Height = 2, Background = (Brush)FindResource("Accent"), Margin = new Thickness(6, 0, 6, 0) };
            var stack = new StackPanel { Cursor = Cursors.Hand };
            stack.Children.Add(label);
            stack.Children.Add(line);
            stack.MouseLeftButtonUp += (s, e) => onClick();
            host.Children.Add(stack);
            return (label, line);
        }

        Button SmallButton(string text, System.Action onClick)
        {
            var b = new Button { Content = text, Style = (Style)FindResource("FlatButton"), Margin = new Thickness(2), HorizontalContentAlignment = HorizontalAlignment.Center };
            b.Click += (s, e) => onClick();
            return b;
        }
    }
}
