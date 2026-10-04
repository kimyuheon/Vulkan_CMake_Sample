using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery.WinForms
{
    // 스크린샷 모드 — 모든 기능을 차례로 열어 화면을 PNG 로, 로그를 TXT 로 남기고 종료한다.
    //   VulkanCAD.Gallery.WinForms.exe --shots <폴더> [--only 이름1,이름2]
    // README 그림 만들기와 "기능이 실제로 그려지는가" 확인용. 사람이 쓰는 기능은 아니다.
    public sealed partial class MainForm
    {
        const int ShotFrames = 60;   // 기능 시작 후 이만큼 그린 뒤 찍는다(애니메이션·요청형 명령이 돌 시간)

        bool StartShotsIfRequested()
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "--shots");
            if (i < 0 || i + 1 >= args.Length) return false;
            string dir = Path.GetFullPath(args[i + 1]);
            int j = Array.IndexOf(args, "--only");
            var only = j >= 0 && j + 1 < args.Length ? args[j + 1].Split(',') : null;
            BeginInvoke(new Action(() => RunShots(dir, only)));
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
                _panel.Logged += OnLog;
                Select(f);
                for (int k = 0; k < ShotFrames / 2; k++) { Application.DoEvents(); Frame(); }
                if (f.HasDemo)
                {
                    try { f.Demo(); } catch (Exception ex) { log.AppendLine("⚠ Demo: " + ex.Message); }
                    for (int k = 0; k < ShotFrames / 2; k++) { Application.DoEvents(); Frame(); }
                }
                _panel.Logged -= OnLog;

                string stem = Path.Combine(dir, $"{n:00}_{name}");
                bool ok = CAD_CaptureViewport(stem + ".png", false);
                SaveWindowShot(stem + "_window.png");
                _scene.ShowTree(false);   // 도면층 탭도 한 장
                for (int k = 0; k < 3; k++) { Application.DoEvents(); Frame(); }
                SaveWindowShot(stem + "_layers.png");
                _scene.ShowTree(true);
                File.WriteAllText(stem + "_tree.json", Str(CAD_GetSceneTreeJson, 1 << 16) ?? "", Encoding.UTF8);
                log.AppendLine($"[imgui] context 0x{CAD_GetImGuiContext().ToInt64():X}");   // 호스트 임베드에선 0 이어야 한다
                log.AppendLine($"[status] {Cad.StatusMessage()}");
                log.AppendLine($"[objects] {CAD_GetObjectCount()}  [capture] {(ok ? "ok" : "FAILED")}");
                File.WriteAllText(stem + ".txt", log.ToString(), Encoding.UTF8);
            }
            Close();
        }

        // 창 전체(리본·패널 포함). 엔진 화면은 Vulkan 이 그려 DrawToBitmap 에 안 나오므로 화면에서 그대로 복사한다.
        void SaveWindowShot(string path)
        {
            Activate();
            var r = RectangleToScreen(ClientRectangle);
            using var bmp = new System.Drawing.Bitmap(r.Width, r.Height);
            using (var g = System.Drawing.Graphics.FromImage(bmp)) g.CopyFromScreen(r.Location, System.Drawing.Point.Empty, r.Size);
            bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        }
    }
}
