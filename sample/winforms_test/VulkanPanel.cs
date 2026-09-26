using System;
using System.Windows.Forms;

namespace VulkanCadWinForms
{
    // WinForms 컨트롤의 HWND 에 엔진(VulkanCADCore.dll)을 붙이는 렌더 영역.
    // WPF 샘플의 VulkanHost(HwndHost) 와 같은 일을 하지만, WinForms 컨트롤은 처음부터 자기 HWND 가
    // 있으므로 자식 창을 따로 만들 필요가 없다.
    //  - OnHandleCreated: CAD_AttachView(Handle) → CAD_CreateEngine()  (API 계약 순서)
    //  - WndProc 에서 마우스/키보드 → CAD_On* 로 라우팅
    //  - MainForm 이 유휴 루프(Application.Idle)마다 Tick() 호출
    public sealed class VulkanPanel : Control
    {
        const int CS_OWNDC = 0x0020;
        const int WM_LBUTTONDOWN = 0x0201, WM_LBUTTONUP = 0x0202;
        const int WM_RBUTTONDOWN = 0x0204, WM_RBUTTONUP = 0x0205;
        const int WM_MBUTTONDOWN = 0x0207, WM_MBUTTONUP = 0x0208;
        const int WM_MOUSEMOVE = 0x0200, WM_MOUSEWHEEL = 0x020A;
        const int WM_KEYDOWN = 0x0100, WM_KEYUP = 0x0101;

        bool _engineReady;
        int _lastW, _lastH;   // 마지막으로 엔진에 알린 픽셀 크기

        public bool EngineReady => _engineReady;

        public VulkanPanel()
        {
            // 배경을 WinForms 가 칠하지 않게 — Vulkan 이 그린 화면 위로 GDI 가 덮어 깜빡이는 것을 막는다.
            SetStyle(ControlStyles.Opaque | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.Selectable, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, false);
            TabStop = true;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ClassStyle |= CS_OWNDC;
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            // 계약: AttachView(핸들) 를 먼저 등록한 뒤 CreateEngine() 이 그 HWND 에 렌더.
            CadApi.CAD_AttachView(Handle, Math.Max(1, ClientSize.Width), Math.Max(1, ClientSize.Height));
            _engineReady = CadApi.CAD_CreateEngine();
            if (_engineReady) CadApi.CAD_SetIgnoreCloseRequest(true);  // 창 닫기는 WinForms 가 관리
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            if (_engineReady)
            {
                CadApi.CAD_DetachView();
                CadApi.CAD_DestroyEngine();
                _engineReady = false;
            }
            base.OnHandleDestroyed(e);
        }

        public void Tick()
        {
            if (!_engineReady) return;
            SyncSize();          // 실제 HWND 픽셀 크기에 스왑체인 맞춤
            CadApi.CAD_Tick();
        }

        // 클라이언트 픽셀 크기가 변했을 때만 엔진에 알림(변화 없으면 스왑체인 재빌드 안 함).
        // 최소화하면 0 이 되므로 그땐 알리지 않는다.
        void SyncSize()
        {
            int w = ClientSize.Width, h = ClientSize.Height;
            if (w <= 0 || h <= 0) return;
            if (w == _lastW && h == _lastH) return;
            _lastW = w; _lastH = h;
            CadApi.CAD_ResizeView(w, h);
        }

        // 방향키·Tab·Enter·ESC 도 폼의 포커스 이동/버튼 처리로 새지 않고 엔진에 오게 한다
        // (Tab = 도구 옵션 순환, ESC = 취소).
        protected override bool IsInputKey(Keys keyData) => true;

        protected override void WndProc(ref Message m)
        {
            if (_engineReady)
            {
                long lp = (long)m.LParam;
                int x = (short)(lp & 0xFFFF);
                int y = (short)((lp >> 16) & 0xFFFF);
                switch (m.Msg)
                {
                    case WM_LBUTTONDOWN: Focus(); CadApi.CAD_OnMouseDown(0, x, y, 0); m.Result = IntPtr.Zero; return;
                    case WM_LBUTTONUP:   CadApi.CAD_OnMouseUp(0, x, y, 0); m.Result = IntPtr.Zero; return;
                    case WM_RBUTTONDOWN: Focus(); CadApi.CAD_OnMouseDown(1, x, y, 0); m.Result = IntPtr.Zero; return;
                    case WM_RBUTTONUP:   CadApi.CAD_OnMouseUp(1, x, y, 0); m.Result = IntPtr.Zero; return;
                    case WM_MBUTTONDOWN: Focus(); CadApi.CAD_OnMouseDown(2, x, y, 0); m.Result = IntPtr.Zero; return;
                    case WM_MBUTTONUP:   CadApi.CAD_OnMouseUp(2, x, y, 0); m.Result = IntPtr.Zero; return;
                    case WM_MOUSEMOVE:   CadApi.CAD_OnMouseMove(x, y); m.Result = IntPtr.Zero; return;
                    case WM_MOUSEWHEEL:
                    {
                        short delta = (short)(((long)m.WParam >> 16) & 0xFFFF);
                        // wheel 좌표는 스크린 기준 → 클라이언트로 변환
                        var p = PointToClient(new System.Drawing.Point(x, y));
                        CadApi.CAD_OnMouseWheel(p.X, p.Y, 0, delta / 120.0);
                        m.Result = IntPtr.Zero;
                        return;
                    }
                    case WM_KEYDOWN: CadApi.CAD_OnKeyDownVK((int)m.WParam); m.Result = IntPtr.Zero; return;
                    case WM_KEYUP:   CadApi.CAD_OnKeyUpVK((int)m.WParam); m.Result = IntPtr.Zero; return;
                }
            }
            base.WndProc(ref m);
        }
    }
}
