using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery.WinForms
{
    // 갤러리 창 — 위 리본(엔진 ImGui 리본과 같은 모양) · 가운데 엔진 화면 · 오른쪽 기능 패널 · 아래 상태줄.
    // 기능 전환과 프레임 루프만 여기 있고, 기능 내용은 전부 Gallery.Features 에 있다.
    public sealed partial class MainForm : Form
    {
        readonly VulkanPanel _view = new VulkanPanel { Dock = DockStyle.Fill, BackColor = Color.Black };
        readonly FeaturePanel _panel = new FeaturePanel { Dock = DockStyle.Right, Width = 340 };
        readonly RibbonBar _ribbon = new RibbonBar { Dock = DockStyle.Top };
        readonly ScenePanel _scene = new ScenePanel { Dock = DockStyle.Left, Width = 330 };
        readonly Label _status = new Label { Dock = DockStyle.Fill, ForeColor = Theme.TextDim, TextAlign = ContentAlignment.MiddleLeft };
        readonly Label _counts = new Label { Dock = DockStyle.Right, Width = 220, ForeColor = Theme.TextCode, TextAlign = ContentAlignment.MiddleRight };

        readonly IReadOnlyList<Feature> _features = FeatureCatalog.Create();
        readonly Stopwatch _clock = Stopwatch.StartNew();
        Feature _current;
        double _lastTime;

        public MainForm()
        {
            Text = "VulkanCAD 기능 갤러리 — WinForms";
            ClientSize = new Size(1680, 940);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.Bg;
            Font = Theme.Body;

            _panel.FocusTarget = _view;
            _panel.AssetDir = _view.AssetDir;
            _ribbon.SetTabs(GalleryRibbon.Build(_features, Select, () => _current), active: 1);
            _ribbon.ItemClicked += () => _view.Focus();   // 누른 뒤 키보드는 엔진으로(ESC·명령 입력)

            var statusBar = new Panel { Dock = DockStyle.Bottom, Height = 26, BackColor = Theme.MenuBar, Padding = new Padding(10, 0, 10, 0) };
            statusBar.Controls.Add(_status);
            statusBar.Controls.Add(_counts);
            var split = new Panel { Dock = DockStyle.Right, Width = 1, BackColor = Theme.Border };
            var leftSplit = new Panel { Dock = DockStyle.Left, Width = 1, BackColor = Theme.Border };

            // 도킹은 나중에 넣은 것부터 자리를 잡는다 → 채움(엔진 화면)을 먼저
            Controls.Add(_view);
            Controls.Add(split);
            Controls.Add(_panel);
            Controls.Add(leftSplit);
            Controls.Add(_scene);
            Controls.Add(_ribbon);
            Controls.Add(statusBar);

            Shown += (s, e) =>
            {
                if (!_view.EngineReady) { _status.Text = "엔진 생성 실패 — VulkanCADCore.dll 과 에셋 폴더를 확인하세요"; return; }
                _scene.Start();
                if (!StartShotsIfRequested()) Select(_features[0]);
            };
            Application.Idle += OnIdle;
            FormClosed += (s, e) => { Application.Idle -= OnIdle; _current?.Leave(); _scene.Stop(); };
        }

        // 기능 전환: 이전 기능 정리 → 장면 비우기 → 패널 비우기 → 새 기능 시작
        void Select(Feature f)
        {
            if (f == _current) return;
            _current?.Leave();
            _current = f;
            _ribbon.SelectTab(f.Category);
            _ribbon.Invalidate();
            Scene.Reset(f.Title);
            _panel.Show(f);
            if (f.HasDemo) _panel.Button("▶ 시연 (핵심 동작을 차례로 실행)", f.Demo);
            try { f.Enter(_panel); }
            catch (Exception ex) { _panel.Log("⚠ 시작 실패: " + ex.Message); }
            _view.Focus();
        }

        // ── 프레임 루프 ────────────────────────────────────────────────────
        // 메시지가 없을 때마다 돈다. 스왑체인이 V-Sync 라 CAD_Tick 이 화면 주기에 맞춰 기다려 준다.
        void OnIdle(object sender, EventArgs e)
        {
            while (!IsDisposed && _view.EngineReady && !PeekMessage(out _, IntPtr.Zero, 0, 0, 0))
                Frame();
        }

        void Frame()
        {
            double now = _clock.Elapsed.TotalSeconds;
            double dt = Math.Min(now - _lastTime, 0.1);   // 창을 끄는 동안 멈췄다 와도 한 번에 튀지 않게
            _lastTime = now;
            try { _current?.Tick(dt); }
            catch (Exception ex) { _panel.Log("⚠ " + ex.Message); }
            _view.Tick();
            _scene.Poll();
            UpdateStatus();
        }

        string _lastStatus, _lastCounts;
        void UpdateStatus()
        {
            string s = Cad.TransientMessage();
            if (string.IsNullOrEmpty(s)) s = Cad.StatusMessage();
            if (s != _lastStatus) _status.Text = _lastStatus = s;
            string c = $"객체 {CAD_GetObjectCount()} · 선택 {CAD_GetSelectedCount()}";
            if (c != _lastCounts) _counts.Text = _lastCounts = c;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MSG { public IntPtr hwnd; public uint message; public IntPtr wParam, lParam; public uint time; public int x, y; }

        [DllImport("user32.dll")]
        static extern bool PeekMessage(out MSG msg, IntPtr hWnd, uint filterMin, uint filterMax, uint remove);
    }
}
