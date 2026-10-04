using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery.Wpf
{
    // 갤러리 창 — 기능 전환과 프레임 루프만 여기 있고, 기능 내용은 전부 Gallery.Features 에 있다.
    public partial class MainWindow : Window
    {
        readonly IReadOnlyList<Feature> _features = FeatureCatalog.Create();
        readonly Stopwatch _clock = Stopwatch.StartNew();
        Feature _current;
        double _lastTime;

        public MainWindow()
        {
            InitializeComponent();
            Panel.FocusEngine = View.FocusEngine;
            Panel.AssetDir = View.AssetDir;
            Ribbon.SetTabs(GalleryRibbon.Build(_features, Select, () => _current), active: 1);
            Ribbon.ItemClicked += View.FocusEngine;   // 누른 뒤 키보드는 엔진으로(ESC·명령 입력)

            ContentRendered += (s, e) =>
            {
                if (!View.EngineReady) { Status.Text = "엔진 생성 실패 — VulkanCADCore.dll 과 에셋 폴더를 확인하세요"; return; }
                ScenePane.Start();
                if (!StartShotsIfRequested()) Select(_features[0]);
                // WPF 가 화면을 그릴 때마다(≈ 모니터 주사율) 한 프레임
                CompositionTarget.Rendering += OnRendering;
            };
            Closed += (s, e) => { CompositionTarget.Rendering -= OnRendering; _current?.Leave(); ScenePane.Stop(); };
        }

        // 기능 전환: 이전 기능 정리 → 장면 비우기 → 패널 비우기 → 새 기능 시작
        void Select(Feature f)
        {
            if (f == _current) return;
            _current?.Leave();
            _current = f;
            Ribbon.SelectTab(f.Category);
            Ribbon.InvalidateVisual();
            Scene.Reset(f.Title);
            Panel.Show(f);
            if (f.HasDemo) Panel.Button("▶ 시연 (핵심 동작을 차례로 실행)", f.Demo);
            try { f.Enter(Panel); }
            catch (Exception ex) { Panel.Log("⚠ 시작 실패: " + ex.Message); }
            View.FocusEngine();
        }

        void OnRendering(object sender, EventArgs e) => Frame();

        void Frame()
        {
            double now = _clock.Elapsed.TotalSeconds;
            double dt = Math.Min(now - _lastTime, 0.1);   // 창을 끄는 동안 멈췄다 와도 한 번에 튀지 않게
            _lastTime = now;
            try { _current?.Tick(dt); }
            catch (Exception ex) { Panel.Log("⚠ " + ex.Message); }
            View.Tick();
            ScenePane.Poll();
            UpdateStatus();
        }

        void UpdateStatus()
        {
            string s = Cad.TransientMessage();
            if (string.IsNullOrEmpty(s)) s = Cad.StatusMessage();
            if (Status.Text != s) Status.Text = s;
            string c = $"객체 {CAD_GetObjectCount()} · 선택 {CAD_GetSelectedCount()}";
            if (Counts.Text != c) Counts.Text = c;
        }
    }
}
