
// VulkanCAD_MFCDlg.cpp: 구현 파일
//

#include "pch.h"
#include "framework.h"
#include "VulkanCAD_MFC.h"
#include "VulkanCAD_MFCDlg.h"
#include "afxdialogex.h"

#include "../../../sdk/include/VulkanCAD_API.h"

#include <string>

#ifdef _DEBUG
#define new DEBUG_NEW
#endif


// ---- Render pane (engine embed) ----
namespace {
	int CurrentModifiers()
	{
		int m = 0;
		if (GetKeyState(VK_SHIFT)   & 0x8000) m |= 0x0001;  // GLFW_MOD_SHIFT
		if (GetKeyState(VK_CONTROL) & 0x8000) m |= 0x0002;  // GLFW_MOD_CONTROL
		if (GetKeyState(VK_MENU)    & 0x8000) m |= 0x0004;  // GLFW_MOD_ALT
		return m;
	}
}

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
	ON_WM_DESTROY()
	ON_WM_NCHITTEST()
END_MESSAGE_MAP()

// Static 컨트롤 기본 HTTRANSPARENT → 마우스가 부모로 샘. HTCLIENT 로 잡아 이 창이 직접 받게.
LRESULT CRenderPane::OnNcHitTest(CPoint) { return HTCLIENT; }

void CRenderPane::AttachEngine()
{
	if (m_engineReady) return;
	CRect rc; GetClientRect(&rc);
	int w = rc.Width()  > 1 ? rc.Width()  : 1;
	int h = rc.Height() > 1 ? rc.Height() : 1;
	CAD_AttachView(GetSafeHwnd(), w, h);   // register HWND, then engine renders here

	// Asset loading independent of the working directory (F5 vs Ctrl+F5 vs double-click
	// all differ): point the engine at the exe's own folder, where PostBuild copied
	// models/fonts/textures. Must be set before CAD_CreateEngine.
	{
		wchar_t exePathW[MAX_PATH] = {};
		GetModuleFileNameW(nullptr, exePathW, MAX_PATH);
		std::wstring dirW(exePathW);
		size_t slash = dirW.find_last_of(L"\\/");
		if (slash != std::wstring::npos) dirW.resize(slash);   // strip exe filename
		char dirA[MAX_PATH * 3] = {};
		WideCharToMultiByte(CP_UTF8, 0, dirW.c_str(), -1, dirA, sizeof(dirA), nullptr, nullptr);
		CAD_SetRuntimeAssetPath(dirA);
	}

	m_engineReady = CAD_CreateEngine();
	if (m_engineReady) {
		CAD_SetViewportLayout(0);          // 0=Single. 기본 Quad(4분할)는 pane 활성화 클릭이
		                                   // 선택 클릭을 먹어 "여러 번 눌러야" 됨 → 단일뷰로.
		SetTimer(1, 16, nullptr);          // ~60fps
	}
}

void CRenderPane::DetachEngine()
{
	if (!m_engineReady) return;
	KillTimer(1);
	CAD_DetachView();
	CAD_DestroyEngine();
	m_engineReady = false;
}

void CRenderPane::OnSize(UINT nType, int cx, int cy)
{
	CWnd::OnSize(nType, cx, cy);
	if (m_engineReady && cx > 0 && cy > 0) CAD_ResizeView(cx, cy);
}
void CRenderPane::OnTimer(UINT_PTR)                { if (m_engineReady && !CAD_ShouldClose()) CAD_Tick(); }
// SetCapture 로 드래그(궤도회전/팬)가 패널 밖으로 나가도 move 를 계속 받음. 버튼 뗄 때 해제.
void CRenderPane::OnLButtonDown(UINT, CPoint p)    { SetFocus(); SetCapture(); CAD_OnMouseDown(0, p.x, p.y, CurrentModifiers()); }
void CRenderPane::OnLButtonUp(UINT, CPoint p)      { CAD_OnMouseUp(0, p.x, p.y, CurrentModifiers()); ReleaseCapture(); }
void CRenderPane::OnRButtonDown(UINT, CPoint p)    { SetFocus(); SetCapture(); CAD_OnMouseDown(1, p.x, p.y, CurrentModifiers()); }
void CRenderPane::OnRButtonUp(UINT, CPoint p)      { CAD_OnMouseUp(1, p.x, p.y, CurrentModifiers()); ReleaseCapture(); }
void CRenderPane::OnMButtonDown(UINT, CPoint p)    { SetFocus(); SetCapture(); CAD_OnMouseDown(2, p.x, p.y, CurrentModifiers()); }
void CRenderPane::OnMButtonUp(UINT, CPoint p)      { CAD_OnMouseUp(2, p.x, p.y, CurrentModifiers()); ReleaseCapture(); }
void CRenderPane::OnMouseMove(UINT, CPoint p)      { CAD_OnMouseMove(p.x, p.y); }
BOOL CRenderPane::OnMouseWheel(UINT, short zDelta, CPoint pt)
{
	// 휠 이벤트의 pt 는 화면 좌표 → 클라이언트로 변환. 엔진이 이 x,y 로 포인터 위치를
	// 갱신(줌 to 커서)하므로 반드시 실제 커서 좌표를 넘겨야 함 (0,0 넘기면 좌상단 기준 줌).
	ScreenToClient(&pt);
	CAD_OnMouseWheel(pt.x, pt.y, 0.0, zDelta / 120.0);
	return TRUE;
}
BOOL CRenderPane::OnEraseBkgnd(CDC*)               { return TRUE; }               // engine draws; skip bkgnd erase
void CRenderPane::OnPaint()                        { ValidateRect(nullptr); }     // Vulkan present shows content
void CRenderPane::OnDestroy()                      { DetachEngine(); CWnd::OnDestroy(); }


// CVulkanCADMFCDlg 대화 상자



CVulkanCADMFCDlg::CVulkanCADMFCDlg(CWnd* pParent /*=nullptr*/)
	: CDialogEx(IDD_VULKANCAD_MFC_DIALOG, pParent)
{
	m_hIcon = AfxGetApp()->LoadIcon(IDR_MAINFRAME);
}

void CVulkanCADMFCDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialogEx::DoDataExchange(pDX);
}

BEGIN_MESSAGE_MAP(CVulkanCADMFCDlg, CDialogEx)
	ON_WM_PAINT()
	ON_WM_QUERYDRAGICON()
	ON_WM_SIZE()
	ON_BN_CLICKED(IDC_BTN_CUBE, &CVulkanCADMFCDlg::OnBnClickedBtnCube)
	// 메뉴 명령은 ON_COMMAND — 버튼(ON_BN_CLICKED)과 달리 알림 코드가 없다.
	ON_COMMAND(ID_CAD_OPEN,       &CVulkanCADMFCDlg::OnCadOpen)
	ON_COMMAND(ID_CAD_SAVEAS,     &CVulkanCADMFCDlg::OnCadSaveAs)
	ON_COMMAND(ID_CAD_EXPORT,     &CVulkanCADMFCDlg::OnCadExport)
	ON_COMMAND(ID_CAD_EXIT,       &CVulkanCADMFCDlg::OnCadExit)
	ON_COMMAND(ID_CAD_UNDO,       &CVulkanCADMFCDlg::OnCadUndo)
	ON_COMMAND(ID_CAD_REDO,       &CVulkanCADMFCDlg::OnCadRedo)
	ON_COMMAND(ID_CAD_SELECTALL,  &CVulkanCADMFCDlg::OnCadSelectAll)
	ON_COMMAND(ID_CAD_DELETE,     &CVulkanCADMFCDlg::OnCadDelete)
	ON_COMMAND(ID_CAD_CLEARALL,   &CVulkanCADMFCDlg::OnCadClearAll)
	ON_COMMAND(ID_CAD_ZOOM,       &CVulkanCADMFCDlg::OnCadZoom)
	ON_COMMAND(ID_CAD_ISO,        &CVulkanCADMFCDlg::OnCadIso)
	ON_COMMAND(ID_CAD_TOP,        &CVulkanCADMFCDlg::OnCadTop)
	ON_COMMAND(ID_CAD_FRONT,      &CVulkanCADMFCDlg::OnCadFront)
	ON_COMMAND(ID_CAD_RIGHT,      &CVulkanCADMFCDlg::OnCadRight)
	ON_COMMAND(ID_CAD_PROJECTION, &CVulkanCADMFCDlg::OnCadProjection)
	ON_COMMAND(ID_CAD_CUBE,       &CVulkanCADMFCDlg::OnCadCube)
	ON_COMMAND(ID_CAD_SPHERE,     &CVulkanCADMFCDlg::OnCadSphere)
	ON_COMMAND(ID_CAD_CYLINDER,   &CVulkanCADMFCDlg::OnCadCylinder)
	ON_COMMAND(ID_CAD_CONE,       &CVulkanCADMFCDlg::OnCadCone)
	ON_BN_CLICKED(IDC_BTN_ALL_SELECT, &CVulkanCADMFCDlg::OnBnClickedBtnAllSelect)
	ON_BN_CLICKED(IDC_BTN_DEL, &CVulkanCADMFCDlg::OnBnClickedBtnDel)
	ON_BN_CLICKED(IDC_BTN_ZOOM, &CVulkanCADMFCDlg::OnBnClickedBtnZoom)
	ON_BN_CLICKED(IDC_BTN_ISO, &CVulkanCADMFCDlg::OnBnClickedBtnIso)
	ON_BN_CLICKED(IDC_BTN_UNDO, &CVulkanCADMFCDlg::OnBnClickedBtnUndo)
	ON_BN_CLICKED(IDC_CHK_PROJECTION, &CVulkanCADMFCDlg::OnBnClickedChkProjection)
END_MESSAGE_MAP()


// CVulkanCADMFCDlg 메시지 처리기

BOOL CVulkanCADMFCDlg::OnInitDialog()
{
	CDialogEx::OnInitDialog();

	// 이 대화 상자의 아이콘을 설정합니다.  응용 프로그램의 주 창이 대화 상자가 아닐 경우에는
	//  프레임워크가 이 작업을 자동으로 수행합니다.
	SetIcon(m_hIcon, TRUE);			// 큰 아이콘을 설정합니다.
	SetIcon(m_hIcon, FALSE);		// 작은 아이콘을 설정합니다.

	// Subclass IDC_CAD_VIEW as the engine render pane, then embed the engine.
	m_view.SubclassDlgItem(IDC_CAD_VIEW, this);

	// Capture the view's design offset/margins for all-sides anchoring on resize.
	CRect dlgRc; GetClientRect(&dlgRc);
	CRect viewRc; m_view.GetWindowRect(&viewRc); ScreenToClient(&viewRc);
	m_viewLeft     = viewRc.left;
	m_viewTop      = viewRc.top;
	m_marginRight  = dlgRc.right  - viewRc.right;
	m_marginBottom = dlgRc.bottom - viewRc.bottom;

	m_view.AttachEngine();   // CAD_AttachView + CAD_CreateEngine + timer

	if (GetProjection())
		GetDlgItem(IDC_CHK_PROJECTION)->SetWindowTextW(L"Ortho");
	else
		GetDlgItem(IDC_CHK_PROJECTION)->SetWindowTextW(L"Perspective");

	return TRUE;  // 포커스를 컨트롤에 설정하지 않으면 TRUE를 반환합니다.
}

void CVulkanCADMFCDlg::OnSize(UINT nType, int cx, int cy)
{
	CDialogEx::OnSize(nType, cx, cy);
	// Anchor the render view to fill from its design top-left to the client's bottom-right.
	if (m_view.GetSafeHwnd() && nType != SIZE_MINIMIZED)
	{
		int w = cx - m_marginRight  - m_viewLeft;
		int h = cy - m_marginBottom - m_viewTop;
		if (w > 1 && h > 1) m_view.MoveWindow(m_viewLeft, m_viewTop, w, h);
	}
}

// 대화 상자에 최소화 단추를 추가할 경우 아이콘을 그리려면
//  아래 코드가 필요합니다.  문서/뷰 모델을 사용하는 MFC 애플리케이션의 경우에는
//  프레임워크에서 이 작업을 자동으로 수행합니다.

void CVulkanCADMFCDlg::OnPaint()
{
	if (IsIconic())
	{
		CPaintDC dc(this); // 그리기를 위한 디바이스 컨텍스트입니다.

		SendMessage(WM_ICONERASEBKGND, reinterpret_cast<WPARAM>(dc.GetSafeHdc()), 0);

		// 클라이언트 사각형에서 아이콘을 가운데에 맞춥니다.
		int cxIcon = GetSystemMetrics(SM_CXICON);
		int cyIcon = GetSystemMetrics(SM_CYICON);
		CRect rect;
		GetClientRect(&rect);
		int x = (rect.Width() - cxIcon + 1) / 2;
		int y = (rect.Height() - cyIcon + 1) / 2;

		// 아이콘을 그립니다.
		dc.DrawIcon(x, y, m_hIcon);
	}
	else
	{
		CDialogEx::OnPaint();
	}
}

// 사용자가 최소화된 창을 끄는 동안에 커서가 표시되도록 시스템에서
//  이 함수를 호출합니다.
HCURSOR CVulkanCADMFCDlg::OnQueryDragIcon()
{
	return static_cast<HCURSOR>(m_hIcon);
}



// ── 파일 대화상자 공용 ───────────────────────────────────────────────────
// 임베드에선 창 소유자가 호스트라, 엔진 자체 다이얼로그가 아니라 호스트의
// CFileDialog 를 쓴다(모달 처리·최근 폴더·부모창 관계가 자연스럽다).
// 엔진에는 **경로만** 넘긴다 — 포맷 판별은 확장자로 엔진이 한다.
bool CVulkanCADMFCDlg::PickPath(bool save, const TCHAR* filter, CString& outPath)
{
	DWORD flags = OFN_EXPLORER | OFN_HIDEREADONLY;
	flags |= save ? OFN_OVERWRITEPROMPT : OFN_FILEMUSTEXIST;
	CFileDialog dlg(save ? FALSE : TRUE, nullptr, nullptr, flags, filter, this);
	const bool ok = (dlg.DoModal() == IDOK);
	if (ok) outPath = dlg.GetPathName();
	m_view.SetFocus();   // 다이얼로그가 가져간 포커스를 3D 뷰로 돌려준다
	return ok;
}

// 실패 사유는 엔진이 상태 메시지로 남긴다(포맷 미지원·깨진 파일·선택 없음 등).
void CVulkanCADMFCDlg::ReportFailure(const TCHAR* fallback)
{
	char msg[512] = {};
	CAD_GetStatusMessage(msg, sizeof(msg));
	if (msg[0]) {
		wchar_t msgW[512] = {};
		MultiByteToWideChar(CP_UTF8, 0, msg, -1, msgW, 512);
		AfxMessageBox(msgW, MB_ICONWARNING);
	} else {
		AfxMessageBox(fallback, MB_ICONWARNING);
	}
}

namespace {
	// CAD_* 파일 API 는 UTF-8 경로를 받는다. 유니코드 빌드의 wchar_t 를 변환해 넘긴다.
	// (ANSI 로 넘기면 한글·이모지 폴더에서 실패)
	struct Utf8Path {
		char buf[MAX_PATH * 4] = {};
		explicit Utf8Path(const CString& w) {
			WideCharToMultiByte(CP_UTF8, 0, w, -1, buf, sizeof(buf), nullptr, nullptr);
		}
	};
}

// ── 파일 ────────────────────────────────────────────────────────────────
void CVulkanCADMFCDlg::OnCadOpen()
{
	// CAD_OpenFile 이 받는 확장자 그대로. .lot 은 씬 전체, 나머지는 현재 문서에 추가.
	static const TCHAR kFilter[] =
		_T("CAD 파일|*.lot;*.obj;*.stl;*.dxf;*.ply;*.gltf;*.glb|")
		_T("프로젝트 (*.lot)|*.lot|")
		_T("메시 (*.obj;*.stl;*.gltf;*.glb)|*.obj;*.stl;*.gltf;*.glb|")
		_T("도면 (*.dxf)|*.dxf|")
		_T("점군 (*.ply)|*.ply|")
		_T("모든 파일 (*.*)|*.*||");

	CString path;
	if (!PickPath(false, kFilter, path)) return;
	if (CAD_OpenFile(Utf8Path(path).buf)) CAD_RequestZoomExtents();  // 연 것이 화면 밖일 수 있다
	else                                  ReportFailure(_T("열기 실패"));
}

void CVulkanCADMFCDlg::OnCadSaveAs()
{
	// .lot 만 씬 전체(선·솔리드·문서 상태)를 담는 네이티브 형식이다.
	static const TCHAR kFilter[] = _T("프로젝트 (*.lot)|*.lot|모든 파일 (*.*)|*.*||");
	CString path;
	if (!PickPath(true, kFilter, path)) return;
	if (path.Find(_T('.')) < 0) path += _T(".lot");   // 확장자로 포맷이 정해진다
	if (!CAD_SaveAs(Utf8Path(path).buf, false)) ReportFailure(_T("저장 실패"));
}

void CVulkanCADMFCDlg::OnCadExport()
{
	// 내보내기는 확장자로 포맷이 갈린다 — 필터 순서가 곧 기본 확장자다.
	static const TCHAR kFilter[] =
		_T("STL (*.stl)|*.stl|")
		_T("DXF (*.dxf)|*.dxf|")
		_T("OBJ (*.obj)|*.obj|")
		_T("glTF (*.glb)|*.glb||");
	CString path;
	if (!PickPath(true, kFilter, path)) return;
	if (path.Find(_T('.')) < 0) path += _T(".stl");
	if (!CAD_ExportFile(Utf8Path(path).buf, false)) ReportFailure(_T("내보내기 실패"));
}

void CVulkanCADMFCDlg::OnCadExit() { EndDialog(IDCANCEL); }

// ── 편집 ────────────────────────────────────────────────────────────────
void CVulkanCADMFCDlg::OnCadUndo()      { CAD_Undo(); }
void CVulkanCADMFCDlg::OnCadRedo()      { CAD_Redo(); }
void CVulkanCADMFCDlg::OnCadSelectAll() { CAD_RequestSelectAll(); }
void CVulkanCADMFCDlg::OnCadDelete()    { CAD_RequestDeleteSelected(); }
void CVulkanCADMFCDlg::OnCadClearAll()  { CAD_RequestClearAll(); }

// ── 보기 ────────────────────────────────────────────────────────────────
void CVulkanCADMFCDlg::OnCadZoom()  { CAD_RequestZoomExtents(); }
void CVulkanCADMFCDlg::OnCadIso()   { CAD_RequestSetView(3); }  // 0=Front 1=Top 2=Right 3=Iso
void CVulkanCADMFCDlg::OnCadTop()   { CAD_RequestSetView(1); }
void CVulkanCADMFCDlg::OnCadFront() { CAD_RequestSetView(0); }
void CVulkanCADMFCDlg::OnCadRight() { CAD_RequestSetView(2); }

// 하단 버튼과 같은 상태를 봐야 한다 — 체크박스를 뒤집고 그 결과를 엔진에 넘긴다.
// (엔진에만 토글을 보내면 버튼 라벨이 실제 투영과 어긋난다)
void CVulkanCADMFCDlg::OnCadProjection()
{
	CheckDlgButton(IDC_CHK_PROJECTION,
	               GetProjection() ? BST_UNCHECKED : BST_CHECKED);
	OnBnClickedChkProjection();
}

// ── 만들기 ──────────────────────────────────────────────────────────────
// 큐브만 전용 API 가 있고 나머지는 명령 이름으로 부른다 — 엔진 명령이 200개가 넘어
// 하나씩 C API 를 뚫는 대신 CAD_ExecuteCommand 로 가는 것이 이 샘플의 방침이다.
void CVulkanCADMFCDlg::OnCadCube()     { CAD_RequestAddCube(); }
void CVulkanCADMFCDlg::OnCadSphere()   { CAD_ExecuteCommand("sphere"); }
void CVulkanCADMFCDlg::OnCadCylinder() { CAD_ExecuteCommand("cylinder"); }
void CVulkanCADMFCDlg::OnCadCone()     { CAD_ExecuteCommand("cone"); }

void CVulkanCADMFCDlg::OnBnClickedBtnCube()      { CAD_RequestAddCube(); }
void CVulkanCADMFCDlg::OnBnClickedBtnAllSelect() { CAD_RequestSelectAll(); }
void CVulkanCADMFCDlg::OnBnClickedBtnDel()       { CAD_RequestDeleteSelected(); }
void CVulkanCADMFCDlg::OnBnClickedBtnZoom()      { CAD_RequestZoomExtents(); }
void CVulkanCADMFCDlg::OnBnClickedBtnIso()       { CAD_RequestSetView(3); }  // 0=Front 1=Top 2=Right 3=Iso
void CVulkanCADMFCDlg::OnBnClickedBtnUndo()      { CAD_Undo(); }


void CVulkanCADMFCDlg::OnBnClickedChkProjection()
{
	// 체크 = Orthographic(직교), 해제 = Perspective(원근).
	// CAD_RequestSetProjection(true=ortho / false=perspective)
	if (GetProjection())
		GetDlgItem(IDC_CHK_PROJECTION)->SetWindowTextW(L"Ortho");
	else
		GetDlgItem(IDC_CHK_PROJECTION)->SetWindowTextW(L"Perspective");

	CAD_RequestSetProjection(GetProjection() ? true : false);
}

bool CVulkanCADMFCDlg::GetProjection()
{
	BOOL ortho = false;
	ortho = (IsDlgButtonChecked(IDC_CHK_PROJECTION) == BST_CHECKED);
	return ortho;
}
