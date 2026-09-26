using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace VulkanCadWinForms
{
    // WPF 샘플(MainWindow.xaml)과 같은 화면 — 메뉴 · 툴바(그리기/편집/뷰) · 왼쪽 정보 · 렌더 영역 · 상태바.
    // 디자이너 파일 없이 코드로만 만든다(한 파일에서 전체가 보이게).
    public sealed class MainForm : Form
    {
        static readonly Color Bg        = ColorTranslator.FromHtml("#1E1F22");
        static readonly Color BarBg     = ColorTranslator.FromHtml("#232529");
        static readonly Color BtnBg     = ColorTranslator.FromHtml("#2B2D31");
        static readonly Color BtnHover  = ColorTranslator.FromHtml("#3A3E44");
        static readonly Color Accent    = ColorTranslator.FromHtml("#2D6CDF");
        static readonly Color TextMain  = ColorTranslator.FromHtml("#E6E6E6");
        static readonly Color TextDim   = ColorTranslator.FromHtml("#7C818B");
        static readonly Color TextBody  = ColorTranslator.FromHtml("#B8BCC4");
        static readonly Color TextInfo  = ColorTranslator.FromHtml("#8CB4FF");
        static readonly Color Line      = ColorTranslator.FromHtml("#2E3136");

        readonly VulkanPanel _render = new VulkanPanel { Dock = DockStyle.Fill, BackColor = Color.Black };
        readonly Label _status = new Label { Text = "준비", AutoSize = true, ForeColor = TextBody, Dock = DockStyle.Left };
        readonly Label _info = new Label { Text = "", AutoSize = true, ForeColor = TextInfo, Dock = DockStyle.Right };
        readonly Label _sceneInfo = new Label { Text = "객체 0 · 선택 0", AutoSize = true, ForeColor = TextInfo };
        string _lastSceneInfo = "";

        public MainForm()
        {
            Text = "VulkanCAD — WinForms 호스트";
            ClientSize = new Size(1120, 740);
            BackColor = Bg;
            Font = new Font("맑은 고딕", 9.5f);
            StartPosition = FormStartPosition.CenterScreen;

            // 런타임 에셋(models/textures/fonts) 경로 지정 — exe 폴더 기준.
            // 셰이더는 라이브러리에 SPIR-V 로 내장돼 폴더가 아예 없다. CWD 는 실행 방식마다
            // 달라지므로(F5 / Ctrl+F5 / 더블클릭) 조건 없이 항상 설정한다. 엔진 생성(= 렌더 영역 핸들 생성) 전이어야 함.
            bool assetsOk = CadApi.CAD_SetRuntimeAssetPath(CadApi.Utf8(AppDomain.CurrentDomain.BaseDirectory));

            // 도킹은 나중에 넣은 것부터 자리를 잡는다 → 채움(렌더)을 먼저, 메뉴를 마지막에.
            Controls.Add(_render);
            Controls.Add(BuildSidePanel());
            Controls.Add(BuildStatusBar());
            Controls.Add(BuildToolbar());
            var menu = BuildMenu();
            Controls.Add(menu);
            MainMenuStrip = menu;

            Shown += (s, e) =>
            {
                _status.Text = !_render.EngineReady ? "엔진 생성 실패 — DLL/에셋 확인"
                             : assetsOk             ? "엔진 준비됨"
                                                    : "엔진 준비됨 (에셋 경로 실패 — 치수/문자 미동작)";
                _info.Text = _render.EngineReady ? "● 렌더링" : "";
                _render.Focus();
            };

            // 매 프레임 Tick — 메시지가 없을 때마다 돈다(유휴 루프). 스왑체인이 V-Sync 라 CAD_Tick 이
            // 화면 주기에 맞춰 기다려 주므로 CPU 를 헛돌리지 않는다.
            Application.Idle += OnIdle;
            FormClosed += (s, e) => Application.Idle -= OnIdle;
        }

        void OnIdle(object sender, EventArgs e)
        {
            while (!IsDisposed && !PeekMessage(out _, IntPtr.Zero, 0, 0, 0))
            {
                _render.Tick();
                string info = $"객체 {CadApi.CAD_GetObjectCount()} · 선택 {CadApi.CAD_GetSelectedCount()}";
                if (info != _lastSceneInfo) { _sceneInfo.Text = info; _lastSceneInfo = info; }
                if (!_render.EngineReady) break;   // 엔진이 없으면 돌 것이 없다
            }
        }

        // ── 메뉴 ──
        MenuStrip BuildMenu()
        {
            var menu = new MenuStrip { BackColor = Bg, ForeColor = TextMain, Padding = new Padding(4, 2, 0, 2) };
            menu.Items.Add(Menu("파일(&F)",
                Item("모두 지움", (s, e) => CadApi.CAD_RequestClearAll()),
                null,
                Item("종료", (s, e) => Close())));
            menu.Items.Add(Menu("편집(&E)",
                Item("실행 취소 (Undo)", (s, e) => CadApi.CAD_Undo()),
                Item("다시 실행 (Redo)", (s, e) => CadApi.CAD_Redo()),
                null,
                Item("전체 선택", (s, e) => CadApi.CAD_RequestSelectAll()),
                Item("선택 삭제", (s, e) => CadApi.CAD_RequestDeleteSelected())));
            menu.Items.Add(Menu("뷰(&V)",
                Item("정면", (s, e) => CadApi.CAD_RequestSetView(0)),
                Item("평면", (s, e) => CadApi.CAD_RequestSetView(1)),
                Item("Isometric", (s, e) => CadApi.CAD_RequestSetView(3)),
                null,
                Item("줌 맞춤", (s, e) => CadApi.CAD_RequestZoomExtents()),
                Item("투영 전환 (Ortho/Persp)", (s, e) => CadApi.CAD_RequestToggleProjection())));
            return menu;
        }

        static ToolStripMenuItem Menu(string text, params ToolStripItem[] items)
        {
            var m = new ToolStripMenuItem(text) { ForeColor = TextMain };
            foreach (var it in items) m.DropDownItems.Add(it ?? new ToolStripSeparator());
            return m;
        }

        static ToolStripMenuItem Item(string text, EventHandler onClick) =>
            new ToolStripMenuItem(text, null, onClick) { ForeColor = ColorTranslator.FromHtml("#1B1B1B") };

        // ── 툴바 ──
        Control BuildToolbar()
        {
            var bar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, Height = 44, BackColor = BarBg, WrapContents = false,
                Padding = new Padding(8, 7, 8, 0),
            };
            bar.Controls.Add(GroupLabel("그리기"));
            bar.Controls.Add(ToolButton("큐브", (s, e) => CadApi.CAD_CreateBox(0, 0, 0, 1, 1, 1), accent: true));
            bar.Controls.Add(ToolButton("선", (s, e) => CadApi.CAD_RequestStartLineSketch()));
            bar.Controls.Add(ToolButton("사각형", (s, e) => CadApi.CAD_RequestStartRectangleSketch()));
            bar.Controls.Add(ToolButton("원", (s, e) => CadApi.CAD_RequestStartCircleSketch()));
            bar.Controls.Add(ToolButton("폴리라인", (s, e) => CadApi.CAD_RequestStartPolylineSketch()));

            bar.Controls.Add(Separator());
            bar.Controls.Add(GroupLabel("편집"));
            bar.Controls.Add(ToolButton("전체선택", (s, e) => CadApi.CAD_RequestSelectAll()));
            bar.Controls.Add(ToolButton("삭제", (s, e) => CadApi.CAD_RequestDeleteSelected()));
            bar.Controls.Add(ToolButton("↶ Undo", (s, e) => CadApi.CAD_Undo()));
            bar.Controls.Add(ToolButton("↷ Redo", (s, e) => CadApi.CAD_Redo()));

            bar.Controls.Add(Separator());
            bar.Controls.Add(GroupLabel("뷰"));
            bar.Controls.Add(ToolButton("정면", (s, e) => CadApi.CAD_RequestSetView(0)));
            bar.Controls.Add(ToolButton("평면", (s, e) => CadApi.CAD_RequestSetView(1)));
            bar.Controls.Add(ToolButton("Iso", (s, e) => CadApi.CAD_RequestSetView(3)));
            bar.Controls.Add(ToolButton("줌맞춤", (s, e) => CadApi.CAD_RequestZoomExtents()));
            bar.Controls.Add(ToolButton("투영", (s, e) => CadApi.CAD_RequestToggleProjection()));
            return bar;
        }

        Button ToolButton(string text, EventHandler onClick, bool accent = false)
        {
            var b = new Button
            {
                // GrowAndShrink: 기본(GrowOnly)은 너비 75 아래로 안 줄어 툴바가 창 밖으로 넘친다
                Text = text, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(0, 30),
                Margin = new Padding(2, 0, 2, 0),
                Padding = new Padding(4, 0, 4, 0), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
                BackColor = accent ? Accent : BtnBg, ForeColor = accent ? Color.White : TextMain,
                Font = accent ? new Font(Font, FontStyle.Bold) : Font,
                TabStop = false,   // 버튼이 포커스를 가져가면 키 입력이 렌더 영역(엔진)으로 안 간다
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = accent ? Accent : BtnHover;
            b.FlatAppearance.MouseDownBackColor = Accent;
            b.Click += onClick;
            b.Click += (s, e) => _render.Focus();   // 누른 뒤 키보드는 다시 엔진으로(명령행 입력·ESC)
            return b;
        }

        static Label GroupLabel(string text) => new Label
        {
            Text = text, AutoSize = true, ForeColor = TextDim, Font = new Font("맑은 고딕", 8f),
            Margin = new Padding(10, 8, 6, 0),
        };

        static Control Separator() => new Panel
        {
            Width = 1, Height = 22, BackColor = BtnHover, Margin = new Padding(8, 4, 8, 0),
        };

        // ── 상태바 ──
        Control BuildStatusBar()
        {
            var bar = new Panel { Dock = DockStyle.Bottom, Height = 28, BackColor = BarBg, Padding = new Padding(12, 6, 12, 0) };
            bar.Controls.Add(_status);
            bar.Controls.Add(_info);
            return bar;
        }

        // ── 왼쪽 정보 ──
        Control BuildSidePanel()
        {
            var side = new FlowLayoutPanel
            {
                Dock = DockStyle.Left, Width = 230, BackColor = Bg, FlowDirection = FlowDirection.TopDown,
                WrapContents = false, Padding = new Padding(14),
            };
            side.Controls.Add(new Label { Text = "VulkanCAD", AutoSize = true, ForeColor = ColorTranslator.FromHtml("#F0F0F0"),
                                          Font = new Font("맑은 고딕", 12f, FontStyle.Bold) });
            side.Controls.Add(new Label { Text = "WinForms 호스트 샘플", AutoSize = true, ForeColor = TextDim,
                                          Font = new Font("맑은 고딕", 8.25f), Margin = new Padding(3, 1, 3, 0) });
            side.Controls.Add(Rule());
            side.Controls.Add(new Label { Text = "조작", AutoSize = true, ForeColor = ColorTranslator.FromHtml("#9AA0AA"),
                                          Font = new Font("맑은 고딕", 8.25f, FontStyle.Bold) });
            side.Controls.Add(new Label { Text = "우클릭 드래그 — 회전\n중클릭 드래그 — 팬\n휠 — 줌\n좌클릭 — 선택", AutoSize = true,
                                          ForeColor = TextBody, Margin = new Padding(3, 4, 3, 0) });
            side.Controls.Add(Rule());
            side.Controls.Add(new Label { Text = "씬", AutoSize = true, ForeColor = ColorTranslator.FromHtml("#9AA0AA"),
                                          Font = new Font("맑은 고딕", 8.25f, FontStyle.Bold) });
            _sceneInfo.Margin = new Padding(3, 4, 3, 0);
            side.Controls.Add(_sceneInfo);
            return side;
        }

        static Control Rule() => new Panel { Height = 1, Width = 200, BackColor = Line, Margin = new Padding(3, 12, 3, 12) };

        // ── Win32 ──
        [StructLayout(LayoutKind.Sequential)]
        struct MSG { public IntPtr hwnd; public uint message; public IntPtr wParam, lParam; public uint time; public int x, y; }

        [DllImport("user32.dll")]
        static extern bool PeekMessage(out MSG msg, IntPtr hWnd, uint filterMin, uint filterMax, uint remove);
    }
}
