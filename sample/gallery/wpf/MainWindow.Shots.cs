using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery.Wpf
{
    // 스크린샷 모드 — 모든 기능을 차례로 열어 화면을 PNG 로, 로그를 TXT 로 남기고 종료한다.
    //   VulkanCAD.Gallery.Wpf.exe --shots <폴더> [--only 이름1,이름2]
    // README 그림 만들기와 "기능이 실제로 그려지는가" 확인용. 사람이 쓰는 기능은 아니다.
    public partial class MainWindow
    {
        const int ShotFrames = 60;

        bool StartShotsIfRequested()
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "--shots");
            if (i < 0 || i + 1 >= args.Length) return false;
            string dir = Path.GetFullPath(args[i + 1]);
            int j = Array.IndexOf(args, "--only");
            var only = j >= 0 && j + 1 < args.Length ? args[j + 1].Split(',') : null;
            Dispatcher.BeginInvoke(new Action(() => RunShots(dir, only)), DispatcherPriority.Background);
            return true;
        }

        void RunShots(string dir, string[] only)
        {
            Directory.CreateDirectory(dir);
            int n = 0;
            foreach (var f in _features)
            {
                n++;
                string name = f.GetType().Name;
                if (only != null && !only.Contains(name)) continue;

                var log = new StringBuilder();
                void OnLog(string line) => log.AppendLine(line);
                Panel.Logged += OnLog;
                Select(f);
                Pump(ShotFrames / 2);
                if (f.HasDemo)
                {
                    try { f.Demo(); } catch (Exception ex) { log.AppendLine("⚠ Demo: " + ex.Message); }
                    Pump(ShotFrames / 2);
                }
                Panel.Logged -= OnLog;

                string stem = Path.Combine(dir, $"{n:00}_{name}");
                bool ok = CAD_CaptureViewport(stem + ".png", false);
                SaveWindowShot(stem + "_window.png");
                ScenePane.ShowTree(false);   // 도면층 탭도 한 장
                Pump(3);
                SaveWindowShot(stem + "_layers.png");
                ScenePane.ShowTree(true);
                log.AppendLine($"[imgui] context 0x{CAD_GetImGuiContext().ToInt64():X}");   // 호스트 임베드에선 0 이어야 한다
                log.AppendLine($"[status] {Cad.StatusMessage()}");
                log.AppendLine($"[objects] {CAD_GetObjectCount()}  [capture] {(ok ? "ok" : "FAILED")}");
                File.WriteAllText(stem + ".txt", log.ToString(), Encoding.UTF8);
            }
            Close();
        }

        // 프레임을 직접 돌리면서 WPF 가 레이아웃·렌더를 처리할 틈을 준다
        void Pump(int frames)
        {
            for (int k = 0; k < frames; k++)
            {
                Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);   // WPF 가 레이아웃·렌더를 끝낼 때까지
                Frame();
            }
        }

        // 창 전체(리본·패널 포함)를 화면에서 복사한다 — 엔진 화면은 자식 HWND 라 WPF 렌더 타깃에 안 잡힌다
        void SaveWindowShot(string path)
        {
            Activate();
            // WPF 는 렌더 스레드가 따로 화면에 내보낸다 — 찍기 전에 한 번 더 그릴 틈을 준다
            Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            System.Threading.Thread.Sleep(150);
            var hwnd = new WindowInteropHelper(this).Handle;
            GetClientRect(hwnd, out RECT rc);
            var pt = new POINT();
            ClientToScreen(hwnd, ref pt);
            int w = rc.R - rc.L, h = rc.B - rc.T;
            IntPtr screen = GetDC(IntPtr.Zero), mem = CreateCompatibleDC(screen), bmp = CreateCompatibleBitmap(screen, w, h);
            IntPtr old = SelectObject(mem, bmp);
            BitBlt(mem, 0, 0, w, h, screen, pt.X, pt.Y, 0x00CC0020);   // SRCCOPY
            SelectObject(mem, old);
            var src = Imaging.CreateBitmapSourceFromHBitmap(bmp, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            DeleteObject(bmp); DeleteDC(mem); ReleaseDC(IntPtr.Zero, screen);
            var enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(src));
            using var fs = File.Create(path);
            enc.Save(fs);
        }

        [StructLayout(LayoutKind.Sequential)] struct RECT { public int L, T, R, B; }
        [StructLayout(LayoutKind.Sequential)] struct POINT { public int X, Y; }
        [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr h, out RECT r);
        [DllImport("user32.dll")] static extern bool ClientToScreen(IntPtr h, ref POINT p);
        [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr h);
        [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr h, IntPtr dc);
        [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleBitmap(IntPtr dc, int w, int h);
        [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr dc, IntPtr o);
        [DllImport("gdi32.dll")] static extern bool BitBlt(IntPtr dst, int x, int y, int w, int h, IntPtr src, int sx, int sy, int rop);
        [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr o);
        [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr dc);
    }
}
