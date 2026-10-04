using System;
using System.Windows.Forms;
using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery.WinForms
{
    // 엔진이 그리는 영역. WinForms 컨트롤은 자기 HWND 가 있으므로 그대로 엔진에 넘긴다.
    //   OnHandleCreated : CAD_SetRuntimeAssetPath → CAD_AttachView(Handle) → CAD_CreateEngine  (이 순서가 계약)
    //   WndProc         : 마우스·키보드 → CAD_On*
    //   Tick()          : 호스트의 유휴 루프가 매 프레임 부른다
    public sealed class VulkanPanel : Control
    {
        const int CS_OWNDC = 0x0020;
        const int WM_LBUTTONDOWN = 0x0201, WM_LBUTTONUP = 0x0202;
        const int WM_RBUTTONDOWN = 0x0204, WM_RBUTTONUP = 0x0205;
        const int WM_MBUTTONDOWN = 0x0207, WM_MBUTTONUP = 0x0208;
        const int WM_MOUSEMOVE = 0x0200, WM_MOUSEWHEEL = 0x020A;
        const int WM_KEYDOWN = 0x0100, WM_KEYUP = 0x0101;

        int _lastW, _lastH;

        public bool EngineReady { get; private set; }
        public string AssetDir { get; } = AppDomain.CurrentDomain.BaseDirectory;

        public VulkanPanel()
        {
            // WinForms 가 배경을 칠하지 않게 — Vulkan 화면 위로 GDI 가 덮어 깜빡이는 것을 막는다.
            SetStyle(ControlStyles.Opaque | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.Selectable, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, false);
            TabStop = true;
        }

        protected override CreateParams CreateParams
        {
            get { var cp = base.CreateParams; cp.ClassStyle |= CS_OWNDC; return cp; }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            // 에셋 경로는 exe 폴더로 — 작업 폴더는 실행 방식(F5·더블클릭)마다 달라진다.
            CAD_SetRuntimeAssetPath(AssetDir);
            CAD_AttachView(Handle, Math.Max(1, ClientSize.Width), Math.Max(1, ClientSize.Height));
            EngineReady = CAD_CreateEngine();
            if (EngineReady) CAD_SetIgnoreCloseRequest(true);   // 창 닫기는 WinForms 가 관리
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            if (EngineReady)
            {
                CAD_DetachView();
                CAD_DestroyEngine();
                EngineReady = false;
            }
            base.OnHandleDestroyed(e);
        }

        public void Tick()
        {
            if (!EngineReady) return;
            int w = ClientSize.Width, h = ClientSize.Height;
            // 크기가 바뀐 때만 알린다(스왑체인 재생성은 비싸다). 최소화하면 0 이라 건너뛴다.
            if (w > 0 && h > 0 && (w != _lastW || h != _lastH))
            {
                _lastW = w; _lastH = h;
                CAD_ResizeView(w, h);
            }
            CAD_Tick();
        }

        // 방향키·Tab·Enter·ESC 도 폼이 가로채지 않고 엔진에 오게 (Tab = 도구 옵션, ESC = 취소)
        protected override bool IsInputKey(Keys keyData) => true;

        static int Modifiers()
        {
            int m = 0;
            if ((ModifierKeys & Keys.Shift) != 0) m |= 1;
            if ((ModifierKeys & Keys.Control) != 0) m |= 2;
            if ((ModifierKeys & Keys.Alt) != 0) m |= 4;
            return m;
        }

        protected override void WndProc(ref Message m)
        {
            if (EngineReady)
            {
                long lp = (long)m.LParam;
                int x = (short)(lp & 0xFFFF), y = (short)((lp >> 16) & 0xFFFF);
                switch (m.Msg)
                {
                    case WM_LBUTTONDOWN: Down(0, x, y); return;
                    case WM_RBUTTONDOWN: Down(1, x, y); return;
                    case WM_MBUTTONDOWN: Down(2, x, y); return;
                    case WM_LBUTTONUP: Up(0, x, y); return;
                    case WM_RBUTTONUP: Up(1, x, y); return;
                    case WM_MBUTTONUP: Up(2, x, y); return;
                    case WM_MOUSEMOVE: CAD_OnMouseMove(x, y); return;
                    case WM_MOUSEWHEEL:
                        short delta = (short)(((long)m.WParam >> 16) & 0xFFFF);
                        var p = PointToClient(new System.Drawing.Point(x, y));   // 휠 좌표는 화면 기준
                        CAD_OnMouseWheel(p.X, p.Y, 0, delta / 120.0);
                        return;
                    case WM_KEYDOWN: CAD_OnKeyDownVK((int)m.WParam); return;
                    case WM_KEYUP: CAD_OnKeyUpVK((int)m.WParam); return;
                }
            }
            base.WndProc(ref m);
        }

        // 드래그(회전·팬)가 패널 밖으로 나가도 계속 받도록 마우스를 잡는다.
        void Down(int button, int x, int y) { Focus(); Capture = true; CAD_OnMouseDown(button, x, y, Modifiers()); }
        void Up(int button, int x, int y) { CAD_OnMouseUp(button, x, y, Modifiers()); Capture = false; }
    }
}
