
// LotCAD_MFCDlg.h: 헤더 파일
//

#pragma once

#include "RibbonBar.h"
#include "CommandLine.h"
#include "CadStatusBar.h"
#include "ToolPanel.h"

#include <cstdint>
#include <vector>

// Render pane: engine embed target. Subclassed onto IDC_CAD_VIEW.
// Owns engine lifecycle + per-frame tick + mouse forwarding.
class CRenderPane : public CWnd
{
public:
	bool m_engineReady = false;
	void AttachEngine();          // CAD_AttachView + CAD_CreateEngine + timer (once)
	void DetachEngine();          // KillTimer + CAD_DetachView + CAD_DestroyEngine	
protected:
	afx_msg void OnSize(UINT nType, int cx, int cy);
	afx_msg void OnTimer(UINT_PTR nIDEvent);
	afx_msg void OnLButtonDown(UINT nFlags, CPoint point);
	afx_msg void OnLButtonUp(UINT nFlags, CPoint point);
	afx_msg void OnRButtonDown(UINT nFlags, CPoint point);
	afx_msg void OnRButtonUp(UINT nFlags, CPoint point);
	afx_msg void OnMButtonDown(UINT nFlags, CPoint point);
	afx_msg void OnMButtonUp(UINT nFlags, CPoint point);
	afx_msg void OnMouseMove(UINT nFlags, CPoint point);
	afx_msg BOOL OnMouseWheel(UINT nFlags, short zDelta, CPoint pt);
	afx_msg BOOL OnEraseBkgnd(CDC* pDC);
	afx_msg void OnPaint();
	afx_msg void OnDestroy();
	// Static control returns HTTRANSPARENT by default → mouse passes to parent.
	// Return HTCLIENT so this pane actually receives WM_L/R/M BUTTON / MOUSEMOVE / WHEEL.
	afx_msg LRESULT OnNcHitTest(CPoint point);
	DECLARE_MESSAGE_MAP()
};


// CLotCADMFCDlg 대화 상자
class CLotCADMFCDlg : public CDialogEx
{
// 생성입니다.
public:
	CLotCADMFCDlg(CWnd* pParent = nullptr);	// 표준 생성자입니다.

// 대화 상자 데이터입니다.
#ifdef AFX_DESIGN_TIME
	enum { IDD = IDD_LOTCAD_MFC_DIALOG };
#endif

	protected:
	virtual void DoDataExchange(CDataExchange* pDX);	// DDX/DDV 지원입니다.
	// 3D 뷰에서 친 글자·Enter·Esc 를 명령행으로 보낸다 (대화상자가 Enter 로 닫히지 않게).
	virtual BOOL PreTranslateMessage(MSG* pMsg);
	// 확인 버튼이 없는 CAD 창 — Enter 가 어디서 눌려도 대화상자를 닫지 않게 막는다. 닫기는 X·파일>끝내기.
	virtual void OnOK() {}


// 구현입니다.
protected:
	HICON m_hIcon;

	// 생성된 메시지 맵 함수
	virtual BOOL OnInitDialog();
	afx_msg void OnPaint();
	afx_msg HCURSOR OnQueryDragIcon();
	afx_msg void OnSize(UINT nType, int cx, int cy);
	DECLARE_MESSAGE_MAP()

	CRibbonBar    m_ribbon;             // 위쪽 리본 (탭 홈/2D/3D) — 구성은 LotCAD_MFCDlg_Ribbon.cpp
	CRenderPane   m_view;               // subclassed IDC_CAD_VIEW
	CCommandLine  m_commandLine;        // 3D 뷰 아래 명령행 (오토캐드식)
	CCadStatusBar m_statusBar;          // 맨 아래 토글 줄 — 구성은 LotCAD_MFCDlg_StatusBar.cpp
	CToolPanel    m_panel;              // 오른쪽 도구 패널 (단면·내비·파티클) — 내용은 LotCAD_MFCDlg_Panels.cpp

	enum PanelKind { kPanelNone = 0, kPanelSection, kPanelNav, kPanelParticles };
	void TogglePanel(int kind);         // 리본 버튼 — 같은 패널이면 닫고 아니면 연다
	void OpenSectionPanel();
	void OpenNavPanel();
	void OpenParticlePanel();
	void RelayoutNow();                 // 패널이 열리고 닫힐 때 — 3D 뷰 폭을 다시 정한다
	int m_viewLeft = 0, m_viewTop = 0;  // view anchor (리본 아래)
	int m_marginRight = 0;

	// 상태바·리본이 보여 줄 상태 중 엔진 SDK 에 읽는 함수(Get)가 없는 것 — 호스트가 들고 있는다(처음 값 = 엔진 기본값).
	// 직교 추적·극좌표·객체스냅·피처 치수는 Get 이 있어 엔진에 직접 묻는다.
	bool m_ortho       = true;    // 투영: true=직교(Ortho) false=원근(Persp) — 엔진 기본은 CAD 식 직교
	bool m_gridOn      = true;
	int  m_visualStyle = 0;       // 0=셰이딩 1=셰이딩+모서리 2=와이어프레임 3=와이어(모서리) 4=숨은선

	void BuildRibbon();                 // 탭·그룹·버튼 추가 (LotCAD_MFCDlg_Ribbon.cpp)
	void SubtractStep();                // 리본 차집합 — 두 번 눌러 기준 → 뺄 것 (LotCAD_MFCDlg_Ribbon.cpp)
	std::vector<uint32_t> m_subtractBase;   // 차집합 1단계에서 기억한 기준 솔리드
	void BuildStatusBar();              // 토글 추가 (LotCAD_MFCDlg_StatusBar.cpp)
	void LayoutChildren(int cx, int cy);
	void SetOrtho(bool ortho);          // 투영 전환 — 메뉴·리본·상태바가 모두 이것을 부른다
	void ToggleOSnap();                 // 객체스냅(F3) — 상태바 메뉴·F3 키
	void ToggleOrthoTracking();         // 직교(F8) — 상태바 버튼·F8 키
	void TogglePolarTracking();         // 극좌표(F10) — 상태바 버튼·F10 키
public:
	// ── 풀다운 메뉴 (IDR_MAINMENU) ──
	// 그리기·편집 명령은 리본에, 파일 입출력은 메뉴에.
	// 파일 선택은 호스트(MFC)가 맡고 엔진에는 경로만 넘긴다.
	afx_msg void OnCadOpen();
	afx_msg void OnCadSaveAs();
	afx_msg void OnCadExport();
	afx_msg void OnCadExit();
	afx_msg void OnCadUndo();
	afx_msg void OnCadRedo();
	afx_msg void OnCadSelectAll();
	afx_msg void OnCadDelete();
	afx_msg void OnCadClearAll();
	afx_msg void OnCadZoom();
	afx_msg void OnCadIso();
	afx_msg void OnCadTop();
	afx_msg void OnCadFront();
	afx_msg void OnCadRight();
	afx_msg void OnCadProjection();
	afx_msg void OnCadCube();
	afx_msg void OnCadSphere();
	afx_msg void OnCadCylinder();
	afx_msg void OnCadCone();

private:
	// 메뉴 파일 항목이 공유하는 열기/저장 구현 (save=true 면 저장 대화상자)
	bool PickPath(bool save, const TCHAR* filter, CString& outPath);
	void ReportFailure(const TCHAR* fallback);
};
