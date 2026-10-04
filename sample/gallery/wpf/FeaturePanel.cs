using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace VulkanCAD.Gallery.Wpf
{
    // 오른쪽 패널 — 기능 설명 + 기능이 요청한 컨트롤(IFeatureUi) + 로그.
    // 기능은 이 클래스를 모르고 IFeatureUi 로만 말한다 — WinForms 갤러리의 FeaturePanel 과 같은 역할.
    public sealed class FeaturePanel : DockPanel, IFeatureUi
    {
        readonly TextBlock _title = new TextBlock { FontSize = 17, FontWeight = FontWeights.Bold };
        readonly TextBlock _summary = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 6) };
        readonly TextBlock _apis = new TextBlock { FontFamily = new FontFamily("Consolas"), FontSize = 11.5 };
        readonly TextBlock _source = new TextBlock { FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 4) };
        readonly StackPanel _controls = new StackPanel { Margin = new Thickness(14, 4, 14, 10) };
        readonly TextBox _log = new TextBox
        {
            Height = 160, IsReadOnly = true, TextWrapping = TextWrapping.Wrap, BorderThickness = new Thickness(0),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto, FontFamily = new FontFamily("Consolas"), FontSize = 11.5,
        };

        public Action FocusEngine { get; set; }
        public string AssetDir { get; set; }
        public event Action<string> Logged;

        public FeaturePanel()
        {
            Background = (Brush)FindResource("Panel");
            TextElement.SetForeground(this, (Brush)FindResource("Text"));
            _summary.Foreground = _source.Foreground = (Brush)FindResource("TextDim");
            _apis.Foreground = (Brush)FindResource("TextCode");
            _log.Background = (Brush)FindResource("Bg");

            var header = new StackPanel { Margin = new Thickness(14, 12, 14, 4) };
            header.Children.Add(_title);
            header.Children.Add(_summary);
            header.Children.Add(_apis);
            header.Children.Add(_source);
            SetDock(header, Dock.Top);
            SetDock(_log, Dock.Bottom);
            Children.Add(header);
            Children.Add(_log);
            Children.Add(new ScrollViewer { Content = _controls, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        }

        public void Show(Feature f)
        {
            _title.Text = f.Title;
            _summary.Text = f.Summary;
            _apis.Text = string.Join("\n", f.Apis);
            _source.Text = "소스: " + f.SourceFile;
            _controls.Children.Clear();
            ClearLog();
        }

        // ── IFeatureUi ──

        public void Section(string title) =>
            Add(new TextBlock { Text = title, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 12, 0, 4) });

        public void Note(string text) =>
            Add(new TextBlock { Text = text, FontSize = 11, TextWrapping = TextWrapping.Wrap, Foreground = (Brush)FindResource("TextDim"), Margin = new Thickness(0, 4, 0, 4) });

        public void Button(string label, Action onClick)
        {
            var b = new Button { Content = label, Style = (Style)FindResource("FlatButton") };
            b.Click += (s, e) => { Run(onClick); FocusEngine?.Invoke(); };
            Add(b);
        }

        public IValue<float> Slider(string label, float min, float max, float value, Action<float> onChange, int decimals = 1)
        {
            var name = new TextBlock { Text = label, Foreground = (Brush)FindResource("TextDim") };
            var val = new TextBlock { HorizontalAlignment = HorizontalAlignment.Right };
            var head = new Grid { Margin = new Thickness(0, 6, 0, 0) };
            head.Children.Add(name);
            head.Children.Add(val);
            var s = new Slider { Minimum = min, Maximum = max, Value = value };
            string fmt = "0." + new string('#', Math.Max(1, decimals));
            val.Text = value.ToString(fmt);
            var box = new ValueBox<float>(() => (float)s.Value, x => s.Value = x);
            s.ValueChanged += (o, e) =>
            {
                val.Text = s.Value.ToString(fmt);
                if (!box.Setting) Run(() => onChange((float)s.Value));
            };
            Add(head);
            Add(s);
            return box;
        }

        public IValue<bool> Toggle(string label, bool value, Action<bool> onChange)
        {
            var c = new CheckBox { Content = label, IsChecked = value };
            var box = new ValueBox<bool>(() => c.IsChecked == true, x => c.IsChecked = x);
            c.Click += (s, e) => { if (!box.Setting) Run(() => onChange(c.IsChecked == true)); };
            Add(c);
            return box;
        }

        public IValue<int> Choice(string label, string[] items, int index, Action<int> onChange)
        {
            Add(new TextBlock { Text = label, Foreground = (Brush)FindResource("TextDim"), Margin = new Thickness(0, 6, 0, 0) });
            var cb = new ComboBox { ItemsSource = items, SelectedIndex = Math.Clamp(index, -1, items.Length - 1) };
            var box = new ValueBox<int>(() => cb.SelectedIndex, x => cb.SelectedIndex = x);
            cb.SelectionChanged += (s, e) => { if (!box.Setting && cb.SelectedIndex >= 0) Run(() => onChange(cb.SelectedIndex)); };
            Add(cb);
            return box;
        }

        public IValue<string> Input(string label, string value, string buttonText, Action<string> onSubmit)
        {
            Add(new TextBlock { Text = label, Foreground = (Brush)FindResource("TextDim"), Margin = new Thickness(0, 6, 0, 0) });
            var row = new DockPanel { Margin = new Thickness(0, 2, 0, 2) };
            var tb = new TextBox { Text = value };
            var b = new Button { Content = buttonText, Style = (Style)FindResource("AccentButton") };
            void Submit() => Run(() => onSubmit(tb.Text));
            b.Click += (s, e) => Submit();
            tb.KeyDown += (s, e) => { if (e.Key == Key.Enter) { Submit(); e.Handled = true; } };
            DockPanel.SetDock(b, Dock.Right);
            row.Children.Add(b);
            row.Children.Add(tb);
            Add(row);
            return new ValueBox<string>(() => tb.Text, x => tb.Text = x);
        }

        public void Log(string line)
        {
            _log.AppendText(line + Environment.NewLine);
            _log.ScrollToEnd();
            Logged?.Invoke(line);
        }

        public void ClearLog() => _log.Clear();

        // WinForms 식 필터("설명|*.a;*.b") 를 그대로 받는다 — WPF 대화상자도 같은 문법이다
        public string PickOpenFile(string filter)
        {
            var dlg = new OpenFileDialog { Filter = filter };
            return dlg.ShowDialog(Window.GetWindow(this)) == true ? dlg.FileName : null;
        }

        public string PickSaveFile(string filter, string defaultFileName)
        {
            var dlg = new SaveFileDialog { Filter = filter, FileName = defaultFileName };
            return dlg.ShowDialog(Window.GetWindow(this)) == true ? dlg.FileName : null;
        }

        void Add(UIElement e) => _controls.Children.Add(e);

        // 기능 코드의 예외가 앱을 죽이지 않게 — 로그로 보여 준다
        void Run(Action a)
        {
            try { a(); }
            catch (Exception ex) { Log("⚠ " + ex.Message); }
        }
    }

    // IValue<T> 를 컨트롤 하나에 잇는다. 코드에서 Value 를 넣는 동안엔 Setting 이 켜져 기능으로 되돌아가지 않는다.
    sealed class ValueBox<T> : IValue<T>
    {
        readonly Func<T> _get;
        readonly Action<T> _set;
        public bool Setting { get; private set; }
        public ValueBox(Func<T> get, Action<T> set) { _get = get; _set = set; }
        public T Value
        {
            get => _get();
            set { Setting = true; try { _set(value); } finally { Setting = false; } }
        }
    }
}
