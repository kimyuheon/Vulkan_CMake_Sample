using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery.Wpf
{
    // WPF 안에 엔진이 그릴 자식 HWND 를 만든다. WPF 요소는 자기 HWND 가 없어서 HwndHost 로 하나 만들어 넘긴다.
    //   BuildWindowCore : 자식 창 생성 → CAD_SetRuntimeAssetPath → CAD_AttachView → CAD_CreateEngine  (이 순서가 계약)
    //   HostWndProc     : 마우스·키보드 → CAD_On*
    //   Tick()          : 창이 CompositionTarget.Rendering 마다 부른다
    // ⚠️ HwndHost 위에는 WPF 요소를 겹칠 수 없다(airspace). 리본·패널은 화면 옆에 둔다.
    public sealed class VulkanHost : HwndHost
    {
        const int WS_CHILD = 0x40000000, WS_VISIBLE = 0x10000000, WS_CLIPCHILDREN = 0x02000000;
        const uint CS_OWNDC = 0x0020;
        const int IDC_ARROW = 32512;
        const uint WM_LBUTTONDOWN = 0x0201, WM_LBUTTONUP = 0x0202, WM_RBUTTONDOWN = 0x0204, WM_RBUTTONUP = 0x0205;
        const uint WM_MBUTTONDOWN = 0x0207, WM_MBUTTONUP = 0x0208, WM_MOUSEMOVE = 0x0200, WM_MOUSEWHEEL = 0x020A;
        const uint WM_KEYDOWN = 0x0100, WM_KEYUP = 0x0101;

        IntPtr _hwnd;
        WndProcFn _wndProcKeepAlive;   // 네이티브가 포인터를 들고 있으므로 GC 에서 지킨다
        int _lastW, _lastH;

        public bool EngineReady { get; private set; }
        public string AssetDir { get; } = AppDomain.CurrentDomain.BaseDirectory;

        protected override HandleRef BuildWindowCore(HandleRef parent)
        {
            _wndProcKeepAlive = HostWndProc;
            var wc = new WNDCLASSEX
            {
                cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
                style = CS_OWNDC,
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcKeepAlive),
                hInstance = GetModuleHandle(null),
                hCursor = LoadCursor(IntPtr.Zero, IDC_ARROW),
                lpszClassName = "VulkanCadGalleryHost",
            };
            RegisterClassEx(ref wc);
            _hwnd = CreateWindowEx(0, "VulkanCadGalleryHost", "", WS_CHILD | WS_VISIBLE | WS_CLIPCHILDREN,
                                   0, 0, 1, 1, parent.Handle, IntPtr.Zero, GetModuleHandle(null), IntPtr.Zero);

            CAD_SetRuntimeAssetPath(AssetDir);   // exe 폴더 — 작업 폴더는 실행 방식마다 달라진다
            CAD_AttachView(_hwnd, 1, 1);
            EngineReady = CAD_CreateEngine();
            if (EngineReady) CAD_SetIgnoreCloseRequest(true);   // 창 닫기는 WPF 가 관리
            return new HandleRef(this, _hwnd);
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            if (EngineReady)
            {
                CAD_DetachView();
                CAD_DestroyEngine();
                EngineReady = false;
            }
            if (_hwnd != IntPtr.Zero) { DestroyWindow(_hwnd); _hwnd = IntPtr.Zero; }
        }

        public void Tick()
        {
            if (!EngineReady) return;
            // 크기는 자식 HWND 의 실제 픽셀로 — WPF 의 DIP 크기와 다르다(DPI 배율)
            if (GetClientRect(_hwnd, out RECT r))
            {
                int w = r.R - r.L, h = r.B - r.T;
                if (w > 0 && h > 0 && (w != _lastW || h != _lastH)) { _lastW = w; _lastH = h; CAD_ResizeView(w, h); }
            }
            CAD_Tick();
        }

        public void FocusEngine() => SetFocus(_hwnd);

        static int Modifiers()
        {
            int m = 0;
            if (GetKeyState(0x10) < 0) m |= 1;   // Shift
            if (GetKeyState(0x11) < 0) m |= 2;   // Ctrl
            if (GetKeyState(0x12) < 0) m |= 4;   // Alt
            return m;
        }

        IntPtr HostWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (EngineReady)
            {
                int x = (short)((long)lParam & 0xFFFF), y = (short)(((long)lParam >> 16) & 0xFFFF);
                switch (msg)
                {
                    case WM_LBUTTONDOWN: Down(hWnd, 0, x, y); return IntPtr.Zero;
                    case WM_RBUTTONDOWN: Down(hWnd, 1, x, y); return IntPtr.Zero;
                    case WM_MBUTTONDOWN: Down(hWnd, 2, x, y); return IntPtr.Zero;
                    case WM_LBUTTONUP: Up(0, x, y); return IntPtr.Zero;
                    case WM_RBUTTONUP: Up(1, x, y); return IntPtr.Zero;
                    case WM_MBUTTONUP: Up(2, x, y); return IntPtr.Zero;
                    case WM_MOUSEMOVE: CAD_OnMouseMove(x, y); return IntPtr.Zero;
                    case WM_MOUSEWHEEL:
                        short delta = (short)(((long)wParam >> 16) & 0xFFFF);
                        var p = new POINT { X = x, Y = y };
                        ScreenToClient(hWnd, ref p);   // 휠 좌표는 화면 기준
                        CAD_OnMouseWheel(p.X, p.Y, 0, delta / 120.0);
                        return IntPtr.Zero;
                    case WM_KEYDOWN: CAD_OnKeyDownVK((int)wParam); return IntPtr.Zero;
                    case WM_KEYUP: CAD_OnKeyUpVK((int)wParam); return IntPtr.Zero;
                }
            }
            return DefWindowProc(hWnd, msg, wParam, lParam);
        }

        // 드래그(회전·팬)가 창 밖으로 나가도 계속 받도록 마우스를 잡는다
        static void Down(IntPtr hWnd, int button, int x, int y) { SetFocus(hWnd); SetCapture(hWnd); CAD_OnMouseDown(button, x, y, Modifiers()); }
        static void Up(int button, int x, int y) { CAD_OnMouseUp(button, x, y, Modifiers()); ReleaseCapture(); }

        // ── Win32 ──
        delegate IntPtr WndProcFn(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        [StructLayout(LayoutKind.Sequential)] struct POINT { public int X, Y; }
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
        static extern IntPtr CreateWindowEx(int exStyle, string cls, string name, int style, int x, int y, int w, int h,
                                            IntPtr parent, IntPtr menu, IntPtr inst, IntPtr param);
        [DllImport("user32.dll")] static extern bool DestroyWindow(IntPtr hWnd);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] static extern IntPtr LoadCursor(IntPtr inst, int id);
        [DllImport("user32.dll")] static extern IntPtr SetFocus(IntPtr hWnd);
        [DllImport("user32.dll")] static extern IntPtr SetCapture(IntPtr hWnd);
        [DllImport("user32.dll")] static extern bool ReleaseCapture();
        [DllImport("user32.dll")] static extern short GetKeyState(int vk);
        [DllImport("user32.dll")] static extern bool ScreenToClient(IntPtr hWnd, ref POINT p);
        [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr hWnd, out RECT r);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern IntPtr GetModuleHandle(string name);
    }
}
