using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery.Wpf
{
    // 도면층 창 — CAD_GetLayersJson 을 줄 목록으로. 칸을 바꾸면 바로 엔진에 넣는다(CAD_SetLayer*).
    //   보이기 · 잠금 · 색(클릭 → 팔레트) · 이름 · 불투명도 · 선종류 · 객체 수(선택 수)
    public sealed class LayerList : DockPanel
    {
        static readonly double[] Widths = { 24, 24, 24, double.NaN, 42, 96, 36 };
        static readonly string[] Headers = { "👁", "🔒", "색", "이름", "불투명", "선종류", "객체" };
        // 색 팔레트 — WPF 에는 색 고르기 대화상자가 없어 자주 쓰는 색을 고르게 한다
        static readonly (float r, float g, float b)[] Palette =
        {
            (0.9f, 0.45f, 0.4f), (0.95f, 0.6f, 0.2f), (0.95f, 0.85f, 0.3f), (0.4f, 0.8f, 0.45f),
            (0.4f, 0.7f, 0.95f), (0.55f, 0.5f, 0.95f), (0.85f, 0.5f, 0.85f), (0.75f, 0.75f, 0.75f),
        };

        readonly StackPanel _rows = new StackPanel();
        uint[] _linetypeIds = new uint[0];
        string[] _linetypeNames = new string[0];
        bool _editing;   // 이름·불투명도 입력 중엔 다시 그리지 않는다

        public uint SelectedLayerId { get; private set; } = uint.MaxValue;   // 0 은 기본 도면층이라 "없음" 은 MaxValue

        public LayerList()
        {
            System.Windows.Documents.TextElement.SetForeground(this, (Brush)FindResource("Text"));   // 객체 수 등 글자 기본색
            var head = Row(Headers.Select(h => (UIElement)new TextBlock { Text = h, FontSize = 11, Foreground = (Brush)FindResource("TextDim") }).ToArray());
            head.Background = (Brush)FindResource("MenuBar");
            SetDock(head, Dock.Top);
            Children.Add(head);
            Children.Add(new ScrollViewer { Content = _rows, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        }

        public void Fill(LayerTable table)
        {
            if (_editing) return;
            _linetypeIds = Cad.Ids((b, n) => CAD_GetLinetypeIds(b, n));
            _linetypeNames = _linetypeIds.Select(id => Str((b, n) => CAD_GetLinetypeName(id, b, n))).ToArray();
            _rows.Children.Clear();
            foreach (var l in table.Layers) _rows.Children.Add(Make(l));
        }

        Grid Make(LayerInfo l)
        {
            var visible = new CheckBox { IsChecked = l.Visible };
            visible.Click += (s, e) => CAD_SetLayerVisible(l.Id, visible.IsChecked == true);
            var locked = new CheckBox { IsChecked = l.Locked };
            locked.Click += (s, e) => CAD_SetLayerLocked(l.Id, locked.IsChecked == true);

            var swatch = new Border
            {
                Width = 16, Height = 16, CornerRadius = new CornerRadius(3), Cursor = l.Fixed ? Cursors.Arrow : Cursors.Hand,
                Background = new SolidColorBrush(Color.FromRgb((byte)(l.R * 255), (byte)(l.G * 255), (byte)(l.B * 255))),
            };
            if (!l.Fixed) swatch.MouseLeftButtonUp += (s, e) => PickColor(swatch, l.Id);   // 도면층 0 은 색 고정

            var name = Edit(l.Name, l.Fixed, text => { if (!CAD_RenameLayer(l.Id, text)) Fill(LayerTable.Read()); });
            var opacity = Edit(l.Opacity.ToString("0.##"), false, text =>
            {
                if (float.TryParse(text, out float o)) CAD_SetLayerOpacity(l.Id, Math.Clamp(o, 0.05f, 1));
            });
            var linetype = new ComboBox { ItemsSource = _linetypeNames, SelectedIndex = Array.IndexOf(_linetypeIds, l.LinetypeId), FontSize = 11 };
            linetype.SelectionChanged += (s, e) => { if (linetype.SelectedIndex >= 0) CAD_SetLayerLinetype(l.Id, _linetypeIds[linetype.SelectedIndex]); };
            var count = new TextBlock { Text = l.SelectedCount > 0 ? $"{l.ObjectCount}·{l.SelectedCount}" : l.ObjectCount.ToString() };

            var row = Row(visible, locked, swatch, name, opacity, linetype, count);
            row.Tag = l.Id;
            row.Background = l.Id == SelectedLayerId ? (Brush)FindResource("ButtonActive") : Brushes.Transparent;
            row.MouseLeftButtonDown += (s, e) =>
            {
                SelectedLayerId = l.Id;
                foreach (Grid r in _rows.Children) r.Background = (uint)r.Tag == l.Id ? (Brush)FindResource("ButtonActive") : Brushes.Transparent;
            };
            return row;
        }

        // 입력란 — Enter·포커스 이탈에 확정
        TextBox Edit(string text, bool readOnly, Action<string> commit)
        {
            var box = new TextBox { Text = text, IsReadOnly = readOnly, BorderThickness = new Thickness(0), Background = Brushes.Transparent, FontSize = 12 };
            box.GotKeyboardFocus += (s, e) => _editing = true;
            box.LostKeyboardFocus += (s, e) => { _editing = false; if (!readOnly && box.Text != text) commit(box.Text); };
            box.KeyDown += (s, e) => { if (e.Key == Key.Enter) Keyboard.ClearFocus(); };
            return box;
        }

        void PickColor(FrameworkElement anchor, uint layerId)
        {
            var menu = new ContextMenu();
            foreach (var (r, g, b) in Palette)
            {
                var item = new MenuItem
                {
                    Header = new Border { Width = 60, Height = 14, Background = new SolidColorBrush(Color.FromRgb((byte)(r * 255), (byte)(g * 255), (byte)(b * 255))) },
                };
                item.Click += (s, e) => CAD_SetLayerColor(layerId, r, g, b);
                menu.Items.Add(item);
            }
            menu.PlacementTarget = anchor;
            menu.IsOpen = true;
        }

        static Grid Row(params UIElement[] cells)
        {
            var g = new Grid { Margin = new Thickness(4, 1, 4, 1), MinHeight = 24 };
            for (int i = 0; i < Widths.Length; i++)
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = double.IsNaN(Widths[i]) ? new GridLength(1, GridUnitType.Star) : new GridLength(Widths[i]) });
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i] is FrameworkElement fe) { fe.VerticalAlignment = VerticalAlignment.Center; fe.Margin = new Thickness(2, 0, 2, 0); }
                Grid.SetColumn(cells[i], i);
                g.Children.Add(cells[i]);
            }
            return g;
        }
    }
}
