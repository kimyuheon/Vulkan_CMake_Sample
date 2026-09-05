// VulkanCAD — MFC 호스트 샘플 (다이얼로그식, 코드 생성)
//   CFrameWnd 안에 상단 버튼 스트립(툴바) + 자식 CRenderPane 을 코드로 만든다.
//   CRenderPane 의 HWND 를 CAD_AttachView 로 엔진에 넘겨 그 창에 Vulkan 렌더.
//   WM_TIMER(~60fps) → CAD_Tick, 마우스 → CAD_OnMouse*, 버튼 → CAD_Request*.
//   WPF/Android/iOS 호스트와 같은 C API 임베드 패턴 (플랫폼 창 코드만 다름).

#include <afxwin.h>
#include <algorithm>

#include "../../api/VulkanCAD_API.h"

// GLFW modifier 비트 (엔진이 기대하는 값)
static int CurrentModifiers() {
    int m = 0;
    if (GetKeyState(VK_SHIFT)   & 0x8000) m |= 0x0001;  // GLFW_MOD_SHIFT
    if (GetKeyState(VK_CONTROL) & 0x8000) m |= 0x0002;  // GLFW_MOD_CONTROL
    if (GetKeyState(VK_MENU)    & 0x8000) m |= 0x0004;  // GLFW_MOD_ALT
    return m;
}

// ─────────────────────────────────────────────────────────────
// 렌더 패널 — 엔진을 이 HWND 에 붙이고 Tick/입력 전달
// ─────────────────────────────────────────────────────────────
class CRenderPane : public CWnd {
public:
    bool engineReady = false;

    void AttachEngine() {
        if (engineReady) return;
        CRect rc; GetClientRect(&rc);
        const int w = std::max<int>(1, rc.Width());
        const int h = std::max<int>(1, rc.Height());
        CAD_AttachView(GetSafeHwnd(), w, h);   // 계약: AttachView(핸들) → CreateEngine 이 그 창에 렌더
        engineReady = CAD_CreateEngine();
        if (engineReady) SetTimer(1, 16, nullptr);   // ~60fps
    }

protected:
    afx_msg void OnSize(UINT type, int cx, int cy) {
        CWnd::OnSize(type, cx, cy);
        if (!engineReady) AttachEngine();
        else if (cx > 0 && cy > 0) CAD_ResizeView(cx, cy);
    }
    afx_msg void OnTimer(UINT_PTR) { if (engineReady && !CAD_ShouldClose()) CAD_Tick(); }

    afx_msg void OnLButtonDown(UINT, CPoint p) { SetFocus(); CAD_OnMouseDown(0, p.x, p.y, CurrentModifiers()); }
    afx_msg void OnLButtonUp  (UINT, CPoint p) { CAD_OnMouseUp  (0, p.x, p.y, CurrentModifiers()); }
    afx_msg void OnRButtonDown(UINT, CPoint p) { CAD_OnMouseDown(1, p.x, p.y, CurrentModifiers()); }
    afx_msg void OnRButtonUp  (UINT, CPoint p) { CAD_OnMouseUp  (1, p.x, p.y, CurrentModifiers()); }
    afx_msg void OnMButtonDown(UINT, CPoint p) { CAD_OnMouseDown(2, p.x, p.y, CurrentModifiers()); }
    afx_msg void OnMButtonUp  (UINT, CPoint p) { CAD_OnMouseUp  (2, p.x, p.y, CurrentModifiers()); }
    afx_msg void OnMouseMove  (UINT, CPoint p) { CAD_OnMouseMove(p.x, p.y); }
    afx_msg BOOL OnMouseWheel (UINT, short zDelta, CPoint) {
        CAD_OnMouseWheel(0.0, 0.0, 0.0, zDelta / 120.0);  // 휠 노치 단위
        return TRUE;
    }
    afx_msg BOOL OnEraseBkgnd(CDC*) { return TRUE; }      // 엔진이 그리므로 배경 지우기 억제
    afx_msg void OnPaint()          { ValidateRect(nullptr); }  // WM_PAINT 무효화만 (Vulkan present 가 표시)

    DECLARE_MESSAGE_MAP()
};

BEGIN_MESSAGE_MAP(CRenderPane, CWnd)
    ON_WM_SIZE()
    ON_WM_TIMER()
    ON_WM_LBUTTONDOWN() ON_WM_LBUTTONUP()
    ON_WM_RBUTTONDOWN() ON_WM_RBUTTONUP()
    ON_WM_MBUTTONDOWN() ON_WM_MBUTTONUP()
    ON_WM_MOUSEMOVE()
    ON_WM_MOUSEWHEEL()
    ON_WM_ERASEBKGND()
    ON_WM_PAINT()
END_MESSAGE_MAP()

// ─────────────────────────────────────────────────────────────
// 메인 프레임 — 상단 버튼 스트립(툴바) + 렌더 패널
// ─────────────────────────────────────────────────────────────
enum {
    IDC_BTN_CUBE = 2001, IDC_BTN_ALL, IDC_BTN_DELETE,
    IDC_BTN_ZOOM, IDC_BTN_ISO, IDC_BTN_UNDO,
    IDC_RENDER = 1000
};
static const int kBarH = 40;

class CMainWnd : public CFrameWnd {
    CRenderPane m_pane;
    CButton m_btnCube, m_btnAll, m_btnDelete, m_btnZoom, m_btnIso, m_btnUndo;

protected:
    afx_msg int OnCreate(LPCREATESTRUCT cs) {
        if (CFrameWnd::OnCreate(cs) == -1) return -1;

        int x = 8;
        auto mk = [&](CButton& b, int id, LPCTSTR text, int wpx) {
            b.Create(text, WS_CHILD | WS_VISIBLE | BS_PUSHBUTTON,
                     CRect(x, 6, x + wpx, 6 + 28), this, id);
            x += wpx + 6;
        };
        mk(m_btnCube,   IDC_BTN_CUBE,   _T("큐브"),     70);
        mk(m_btnAll,    IDC_BTN_ALL,    _T("전체선택"), 90);
        mk(m_btnDelete, IDC_BTN_DELETE, _T("삭제"),     70);
        mk(m_btnZoom,   IDC_BTN_ZOOM,   _T("줌"),       60);
        mk(m_btnIso,    IDC_BTN_ISO,    _T("Iso"),      60);
        mk(m_btnUndo,   IDC_BTN_UNDO,   _T("Undo"),     70);

        m_pane.Create(nullptr, nullptr,
                      WS_CHILD | WS_VISIBLE | WS_CLIPCHILDREN | WS_CLIPSIBLINGS,
                      CRect(0, kBarH, 100, 100), this, IDC_RENDER);
        return 0;
    }

    afx_msg void OnSize(UINT type, int cx, int cy) {
        CFrameWnd::OnSize(type, cx, cy);
        if (m_pane.GetSafeHwnd())
            m_pane.MoveWindow(0, kBarH, cx, std::max<int>(1, cy - kBarH));
    }

    afx_msg void OnClose() {
        if (m_pane.engineReady) {
            m_pane.KillTimer(1);
            CAD_DetachView();
            CAD_DestroyEngine();
            m_pane.engineReady = false;
        }
        CFrameWnd::OnClose();
    }

    afx_msg void OnCube()   { CAD_RequestAddCube(); }
    afx_msg void OnAll()    { CAD_RequestSelectAll(); }
    afx_msg void OnDelete() { CAD_RequestDeleteSelected(); }
    afx_msg void OnZoom()   { CAD_RequestZoomExtents(); }
    afx_msg void OnIso()    { CAD_RequestSetView(3); }   // 0=Front 1=Top 2=Right 3=Iso
    afx_msg void OnUndo()   { CAD_Undo(); }

    DECLARE_MESSAGE_MAP()
};

BEGIN_MESSAGE_MAP(CMainWnd, CFrameWnd)
    ON_WM_CREATE()
    ON_WM_SIZE()
    ON_WM_CLOSE()
    ON_COMMAND(IDC_BTN_CUBE,   &CMainWnd::OnCube)
    ON_COMMAND(IDC_BTN_ALL,    &CMainWnd::OnAll)
    ON_COMMAND(IDC_BTN_DELETE, &CMainWnd::OnDelete)
    ON_COMMAND(IDC_BTN_ZOOM,   &CMainWnd::OnZoom)
    ON_COMMAND(IDC_BTN_ISO,    &CMainWnd::OnIso)
    ON_COMMAND(IDC_BTN_UNDO,   &CMainWnd::OnUndo)
END_MESSAGE_MAP()

// ─────────────────────────────────────────────────────────────
// 앱
// ─────────────────────────────────────────────────────────────
class CMfcApp : public CWinApp {
public:
    BOOL InitInstance() override {
        CMainWnd* wnd = new CMainWnd();
        wnd->Create(nullptr, _T("VulkanCAD — MFC 호스트"),
                    WS_OVERLAPPEDWINDOW, CRect(80, 80, 80 + 1300, 80 + 850));
        m_pMainWnd = wnd;
        wnd->ShowWindow(SW_SHOW);
        wnd->UpdateWindow();
        return TRUE;
    }
};

CMfcApp theApp;
