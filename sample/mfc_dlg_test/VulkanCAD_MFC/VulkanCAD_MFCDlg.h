
// VulkanCAD_MFCDlg.h: 헤더 파일
//

#pragma once


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


// CVulkanCADMFCDlg 대화 상자
class CVulkanCADMFCDlg : public CDialogEx
{
// 생성입니다.
public:
	CVulkanCADMFCDlg(CWnd* pParent = nullptr);	// 표준 생성자입니다.

	bool GetProjection();

// 대화 상자 데이터입니다.
#ifdef AFX_DESIGN_TIME
	enum { IDD = IDD_VULKANCAD_MFC_DIALOG };
#endif

	protected:
	virtual void DoDataExchange(CDataExchange* pDX);	// DDX/DDV 지원입니다.


// 구현입니다.
protected:
	HICON m_hIcon;

	// 생성된 메시지 맵 함수
	virtual BOOL OnInitDialog();
	afx_msg void OnPaint();
	afx_msg HCURSOR OnQueryDragIcon();
	afx_msg void OnSize(UINT nType, int cx, int cy);
	DECLARE_MESSAGE_MAP()

	CRenderPane m_view;                 // subclassed IDC_CAD_VIEW
	int m_viewLeft = 0, m_viewTop = 0;  // view anchor (design offset)
	int m_marginRight = 0, m_marginBottom = 0;
public:
	afx_msg void OnBnClickedBtnCube();

	// ── 풀다운 메뉴 (IDR_MAINMENU) ──
	// 버튼은 자주 쓰는 것만 남기고, 나머지는 전부 여기로.
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
public:
	afx_msg void OnBnClickedBtnAllSelect();
	afx_msg void OnBnClickedBtnDel();
	afx_msg void OnBnClickedBtnZoom();
	afx_msg void OnBnClickedBtnIso();
	afx_msg void OnBnClickedBtnUndo();
	afx_msg void OnBnClickedChkProjection();
};
