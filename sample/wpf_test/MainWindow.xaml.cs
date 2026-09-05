using System;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace VulkanCadWpf
{
    public partial class MainWindow : Window
    {
        readonly VulkanHost _host = new VulkanHost();

        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Closed += OnClosed;
        }

        void OnLoaded(object sender, RoutedEventArgs e)
        {
            // 런타임 에셋(shaders/models/textures/fonts) 경로 지정 — DLL 출력 폴더 기준.
            string assets = AppDomain.CurrentDomain.BaseDirectory;
            if (Directory.Exists(Path.Combine(assets, "shaders")))
                CadApi.CAD_SetRuntimeAssetPath(CadApi.Utf8(assets));

            // HwndHost 를 렌더 영역에 얹음 → BuildWindowCore 에서 엔진 생성.
            // 크기 동기화는 host.Tick() 안의 SyncSize() 가 실제 HWND 픽셀로 매 프레임 처리.
            RenderHost.Child = _host;

            StatusText.Text = _host.EngineReady ? "엔진 준비됨" : "엔진 생성 실패 — DLL/에셋 확인";
            InfoText.Text = _host.EngineReady ? "● 렌더링" : "";

            // 매 프레임 Tick (WPF 합성 타이밍)
            CompositionTarget.Rendering += (s, ev) =>
            {
                _host.Tick();
                SceneInfo.Text = $"객체 {CadApi.CAD_GetObjectCount()} · 선택 {CadApi.CAD_GetSelectedCount()}";
            };
        }

        void OnClosed(object sender, EventArgs e) => _host.Dispose();

        void OnExit(object s, RoutedEventArgs e) => Close();

        // ── 툴바 핸들러 ──
        void OnAddCube(object s, RoutedEventArgs e)   => CadApi.CAD_CreateBox(0, 0, 0, 1, 1, 1);
        void OnLine(object s, RoutedEventArgs e)       => CadApi.CAD_RequestStartLineSketch();
        void OnRect(object s, RoutedEventArgs e)       => CadApi.CAD_RequestStartRectangleSketch();
        void OnCircle(object s, RoutedEventArgs e)     => CadApi.CAD_RequestStartCircleSketch();
        void OnPolyline(object s, RoutedEventArgs e)   => CadApi.CAD_RequestStartPolylineSketch();
        void OnSelectAll(object s, RoutedEventArgs e)  => CadApi.CAD_RequestSelectAll();
        void OnDelete(object s, RoutedEventArgs e)     => CadApi.CAD_RequestDeleteSelected();
        void OnClearAll(object s, RoutedEventArgs e)   => CadApi.CAD_RequestClearAll();
        void OnUndo(object s, RoutedEventArgs e)       => CadApi.CAD_Undo();
        void OnRedo(object s, RoutedEventArgs e)       => CadApi.CAD_Redo();
        void OnViewFront(object s, RoutedEventArgs e)  => CadApi.CAD_RequestSetView(0);
        void OnViewTop(object s, RoutedEventArgs e)    => CadApi.CAD_RequestSetView(1);
        void OnViewIso(object s, RoutedEventArgs e)    => CadApi.CAD_RequestSetView(3);
        void OnZoom(object s, RoutedEventArgs e)       => CadApi.CAD_RequestZoomExtents();
        void OnToggleProj(object s, RoutedEventArgs e) => CadApi.CAD_RequestToggleProjection();
    }
}
