using System;
using System.Drawing;
using System.Windows.Forms;

namespace VulkanCAD.Gallery.WinForms
{
    // 오른쪽 패널 — 기능 설명 + 기능이 요청한 컨트롤(IFeatureUi) + 로그.
    // 기능은 이 클래스를 모르고 IFeatureUi 로만 말한다.
    public sealed class FeaturePanel : Panel, IFeatureUi
    {
        readonly Label _title = new Label { AutoSize = true, Font = Theme.Heading, ForeColor = Theme.Text };
        readonly Label _summary = new Label { AutoSize = true, Font = Theme.Body, ForeColor = Theme.TextDim };
        readonly Label _apis = new Label { AutoSize = true, Font = Theme.Code, ForeColor = Theme.TextCode };
        readonly Label _source = new Label { AutoSize = true, Font = Theme.Small, ForeColor = Theme.TextDim };
        readonly FlowLayoutPanel _controls = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true,
            Padding = new Padding(12, 4, 12, 8), BackColor = Theme.Panel,
        };
        readonly TextBox _log = new TextBox
        {
            Dock = DockStyle.Bottom, Height = 150, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical,
            BackColor = Theme.Bg, ForeColor = Theme.Text, BorderStyle = BorderStyle.None, Font = Theme.Code,
        };

        // 버튼을 누른 뒤 키보드를 엔진으로 돌려줄 대상
        public Control FocusTarget { get; set; }
        public string AssetDir { get; set; }
        public event Action<string> Logged;

        int ContentWidth => Width - 40;

        public FeaturePanel()
        {
            BackColor = Theme.Panel;
            var header = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false,
                Padding = new Padding(12, 12, 12, 4), BackColor = Theme.Panel,
            };
            header.Controls.AddRange(new Control[] { _title, _summary, _apis, _source });
            // 도킹은 나중에 넣은 것부터 자리를 잡는다 → Fill 을 먼저
            Controls.Add(_controls);
            Controls.Add(_log);
            Controls.Add(header);
        }

        public void Show(Feature f)
        {
            _title.Text = f.Title;
            _summary.Text = f.Summary;
            _apis.Text = string.Join("\n", f.Apis);
            _source.Text = "소스: " + f.SourceFile;
            foreach (var l in new[] { _summary, _apis, _source }) l.MaximumSize = new Size(ContentWidth, 0);
            _summary.Margin = new Padding(3, 6, 3, 6);

            _controls.SuspendLayout();
            foreach (Control c in _controls.Controls) c.Dispose();
            _controls.Controls.Clear();
            _controls.ResumeLayout();
            ClearLog();
        }

        // ── IFeatureUi ─────────────────────────────────────────────────────

        public void Section(string title) => Add(new Label
        {
            Text = title, AutoSize = true, Font = Theme.Section, ForeColor = Theme.Text, Margin = new Padding(0, 12, 0, 4),
        });

        public void Note(string text) => Add(new Label
        {
            Text = text, AutoSize = true, Font = Theme.Small, ForeColor = Theme.TextDim,
            MaximumSize = new Size(ContentWidth, 0), Margin = new Padding(0, 4, 0, 4),
        });

        public void Button(string label, Action onClick)
        {
            var b = new Button
            {
                Text = label, Width = ContentWidth, Height = 28, FlatStyle = FlatStyle.Flat, BackColor = Theme.Control,
                ForeColor = Theme.Text, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 2, 0, 2),
                TabStop = false, Cursor = Cursors.Hand,
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Theme.ButtonHover;
            b.FlatAppearance.MouseDownBackColor = Theme.Accent;
            b.Click += (s, e) => { Run(onClick); FocusTarget?.Focus(); };
            Add(b);
        }

        public IValue<float> Slider(string label, float min, float max, float value, Action<float> onChange, int decimals = 1)
        {
            var s = new SliderRow(label, min, max, value, decimals, ContentWidth);
            s.Changed += v => Run(() => onChange(v));
            Add(s);
            return s;
        }

        public IValue<bool> Toggle(string label, bool value, Action<bool> onChange)
        {
            var c = new CheckBox { Text = label, Checked = value, AutoSize = true, ForeColor = Theme.Text, TabStop = false };
            var v = new ValueBox<bool>(() => c.Checked, x => c.Checked = x);
            c.CheckedChanged += (s, e) => { if (!v.Setting) Run(() => onChange(c.Checked)); };
            Add(c);
            return v;
        }

        public IValue<int> Choice(string label, string[] items, int index, Action<int> onChange)
        {
            Add(new Label { Text = label, AutoSize = true, ForeColor = Theme.TextDim, Margin = new Padding(0, 6, 0, 0) });
            var cb = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList, Width = ContentWidth, BackColor = Theme.Control,
                ForeColor = Theme.Text, FlatStyle = FlatStyle.Flat, TabStop = false,
            };
            cb.Items.AddRange(items);
            cb.SelectedIndex = Math.Clamp(index, -1, items.Length - 1);
            var v = new ValueBox<int>(() => cb.SelectedIndex, x => cb.SelectedIndex = x);
            cb.SelectedIndexChanged += (s, e) => { if (!v.Setting) Run(() => onChange(cb.SelectedIndex)); };
            Add(cb);
            return v;
        }

        public IValue<string> Input(string label, string value, string buttonText, Action<string> onSubmit)
        {
            Add(new Label { Text = label, AutoSize = true, ForeColor = Theme.TextDim, Margin = new Padding(0, 6, 0, 0) });
            var row = new TableLayoutPanel { Width = ContentWidth, Height = 28, ColumnCount = 2, Margin = new Padding(0, 2, 0, 2) };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            var tb = new TextBox { Text = value, Dock = DockStyle.Fill, BackColor = Theme.Bg, ForeColor = Theme.Text, BorderStyle = BorderStyle.FixedSingle };
            var b = new Button { Text = buttonText, AutoSize = true, FlatStyle = FlatStyle.Flat, BackColor = Theme.Accent, ForeColor = Color.White, TabStop = false };
            b.FlatAppearance.BorderSize = 0;
            void Submit() => Run(() => onSubmit(tb.Text));
            b.Click += (s, e) => Submit();
            tb.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { Submit(); e.SuppressKeyPress = true; } };
            row.Controls.Add(tb, 0, 0);
            row.Controls.Add(b, 1, 0);
            Add(row);
            return new ValueBox<string>(() => tb.Text, x => tb.Text = x);
        }

        public void Log(string line)
        {
            _log.AppendText(line + Environment.NewLine);
            Logged?.Invoke(line);
        }

        public void ClearLog() => _log.Clear();

        public string PickOpenFile(string filter)
        {
            using var dlg = new OpenFileDialog { Filter = filter };
            return dlg.ShowDialog(this) == DialogResult.OK ? dlg.FileName : null;
        }

        public string PickSaveFile(string filter, string defaultFileName)
        {
            using var dlg = new SaveFileDialog { Filter = filter, FileName = defaultFileName };
            return dlg.ShowDialog(this) == DialogResult.OK ? dlg.FileName : null;
        }

        // ── 내부 ──────────────────────────────────────────────────────────

        void Add(Control c) => _controls.Controls.Add(c);

        // 기능 코드의 예외가 앱을 죽이지 않게 — 로그로 보여 준다.
        void Run(Action a)
        {
            try { a(); }
            catch (Exception ex) { Log("⚠ " + ex.Message); }
        }
    }
}
