using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace VulkanCadWpf
{
    // WPF 안에 Vulkan 렌더용 자식 HWND 를 만들고 엔진(VulkanCADCore.dll)에 붙이는 호스트.
    //  - BuildWindowCore: 자식 창 생성 → CAD_AttachView(hwnd) → CAD_CreateEngine()  (API 계약 순서)
    //  - 자식 창 WndProc 에서 마우스/키보드 → CAD_On* 로 라우팅
    //  - MainWindow 가 CompositionTarget.Rendering 마다 Tick() 호출
    public sealed class VulkanHost : HwndHost
    {
        // Win32 상수
        const int WS_CHILD = 0x40000000, WS_VISIBLE = 0x10000000, WS_CLIPCHILDREN = 0x02000000;
        const uint CS_OWNDC = 0x0020;
        const int IDC_ARROW = 32512;
        const uint WM_LBUTTONDOWN = 0x0201, WM_LBUTTONUP = 0x0202;
        const uint WM_RBUTTONDOWN = 0x0204, WM_RBUTTONUP = 0x0205;
        const uint WM_MBUTTONDOWN = 0x0207, WM_MBUTTONUP = 0x0208;
        const uint WM_MOUSEMOVE = 0x0200, WM_MOUSEWHEEL = 0x020A;
        const uint WM_KEYDOWN = 0x0100, WM_KEYUP = 0x0101;

        IntPtr _hwnd;
        HostWndProcDelegate _wndProcKeepAlive;   // GC 방지 (네이티브가 포인터 보유)
        bool _engineReady;
        int _lastW, _lastH;   // 마지막으로 엔진에 알린 픽셀 크기

        public bool EngineReady => _engineReady;

        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            _wndProcKeepAlive = HostWndProc;
            var wc = new WNDCLASSEX
            {
                cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
                style = CS_OWNDC,
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcKeepAlive),
                hInstance = GetModuleHandle(null),
                hCursor = LoadCursor(IntPtr.Zero, IDC_ARROW),
                lpszClassName = "VulkanCadHostWnd",
            };
            RegisterClassEx(ref wc);

            _hwnd = CreateWindowEx(0, "VulkanCadHostWnd", "",
                WS_CHILD | WS_VISIBLE | WS_CLIPCHILDREN,
                0, 0, 1, 1, hwndParent.Handle, IntPtr.Zero, GetModuleHandle(null), IntPtr.Zero);

            // 계약: AttachView(핸들) 를 먼저 등록한 뒤 CreateEngine() 이 그 HWND 에 렌더.
            CadApi.CAD_AttachView(_hwnd, 1, 1);
            _engineReady = CadApi.CAD_CreateEngine();
            if (_engineReady) CadApi.CAD_SetIgnoreCloseRequest(true);  // 창 닫기는 WPF 가 관리

            return new HandleRef(this, _hwnd);
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            if (_engineReady)
            {
                CadApi.CAD_DetachView();
                CadApi.CAD_DestroyEngine();
                _engineReady = false;
            }
            if (_hwnd != IntPtr.Zero) { DestroyWindow(_hwnd); _hwnd = IntPtr.Zero; }
        }

        public void Tick()
        {
            if (!_engineReady) return;
            SyncSize();          // 실제 HWND 픽셀 크기에 스왑체인 맞춤 (DPI 무관)
            CadApi.CAD_Tick();
        }

        // 자식 HWND 의 실제 클라이언트 픽셀 크기를 읽어, 변했을 때만 엔진에 알림.
        // 엔진 getExtent() 가 이 값을 스왑체인 크기로 쓰므로 물리 픽셀이어야 정확히 채워짐.
        public void SyncSize()
        {
            if (!_engineReady || _hwnd == IntPtr.Zero) return;
            if (!GetClientRect(_hwnd, out RECT r)) return;
            int w = r.R - r.L, h = r.B - r.T;
            if (w <= 0 || h <= 0) return;
            if (w == _lastW && h == _lastH) return;   // 변화 없으면 스왑체인 재빌드 안 함
            _lastW = w; _lastH = h;
            CadApi.CAD_ResizeView(w, h);
        }

        IntPtr HostWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (_engineReady)
            {
                int x = (short)((long)lParam & 0xFFFF);
                int y = (short)(((long)lParam >> 16) & 0xFFFF);
                switch (msg)
                {
                    case WM_LBUTTONDOWN: SetFocus(hWnd); CadApi.CAD_OnMouseDown(0, x, y, 0); return IntPtr.Zero;
                    case WM_LBUTTONUP:   CadApi.CAD_OnMouseUp(0, x, y, 0); return IntPtr.Zero;
                    case WM_RBUTTONDOWN: SetFocus(hWnd); CadApi.CAD_OnMouseDown(1, x, y, 0); return IntPtr.Zero;
                    case WM_RBUTTONUP:   CadApi.CAD_OnMouseUp(1, x, y, 0); return IntPtr.Zero;
                    case WM_MBUTTONDOWN: SetFocus(hWnd); CadApi.CAD_OnMouseDown(2, x, y, 0); return IntPtr.Zero;
                    case WM_MBUTTONUP:   CadApi.CAD_OnMouseUp(2, x, y, 0); return IntPtr.Zero;
                    case WM_MOUSEMOVE:   CadApi.CAD_OnMouseMove(x, y); return IntPtr.Zero;
                    case WM_MOUSEWHEEL:
                    {
                        short delta = (short)(((long)wParam >> 16) & 0xFFFF);
                        // wheel 좌표는 스크린 기준 → 클라이언트로 변환
                        var p = new POINT { X = x, Y = y };
                        ScreenToClient(hWnd, ref p);
                        CadApi.CAD_OnMouseWheel(p.X, p.Y, 0, delta / 120.0);
                        return IntPtr.Zero;
                    }
                    case WM_KEYDOWN: CadApi.CAD_OnKeyDownVK((int)wParam); return IntPtr.Zero;
                    case WM_KEYUP:   CadApi.CAD_OnKeyUpVK((int)wParam); return IntPtr.Zero;
                }
            }
            return DefWindowProc(hWnd, msg, wParam, lParam);
        }

        // ── Win32 interop ──
        delegate IntPtr HostWndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)] struct POINT { public int X; public int Y; }
        [StructLayout(LayoutKind.Sequential)] struct RECT { public int L, T, R, B; }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct WNDCLASSEX
        {
            public uint cbSize, style;
            public IntPtr lpfnWndProc;
            public int cbClsExtra, cbWndExtra;
            public IntPtr hInstance, hIcon, hCursor, hbrBackground;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpszMenuName;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpszClassName;
            public IntPtr hIconSm;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern ushort RegisterClassEx(ref WNDCLASSEX wc);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr CreateWindowEx(int exStyle, string cls, string name, int style,
            int x, int y, int w, int h, IntPtr parent, IntPtr menu, IntPtr inst, IntPtr param);
        [DllImport("user32.dll")] static extern bool DestroyWindow(IntPtr hWnd);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] static extern IntPtr LoadCursor(IntPtr inst, int id);
        [DllImport("user32.dll")] static extern IntPtr SetFocus(IntPtr hWnd);
        [DllImport("user32.dll")] static extern bool ScreenToClient(IntPtr hWnd, ref POINT p);
        [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr hWnd, out RECT r);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern IntPtr GetModuleHandle(string name);
    }
}
