#pragma once
// 상태바 — 창 맨 아래 한 줄, 오른쪽 끝에 토글 버튼이 놓인다. 다크 테마.
//
// 엔진(3dEngine) 하단 상태바의 오른쪽 버튼 줄을 옮긴 것이다.
//   [치수] [그리드] [셰이딩 ▾] [Ortho] [직교(F8)] [극좌표(F10)] [객체스냅 ▾]
// 켜진 토글은 리본의 켜진 버튼과 같은 강조색으로 칠한다.
//
// 이 클래스는 CAD 를 모른다 — 버튼마다 글자·켜짐 여부·누를 때 할 일을 함수로 받는다.
// 어떤 버튼을 둘지는 VulkanCAD_MFCDlg_StatusBar.cpp 에서 정한다.
// (MFC 의 CStatusBar 와 이름이 겹치지 않게 CCadStatusBar.)

#include <functional>
#include <string>
#include <vector>

class CCadStatusBar : public CWnd
{
public:
	using Label   = std::function<std::wstring()>;          // 상태에 따라 바뀌는 글자 (Persp ↔ Ortho)
	using IsOn    = std::function<bool()>;
	using OnClick = std::function<void(const CRect& screenRc)>;   // 버튼의 화면 좌표 — 팝업 메뉴 위치용

	// ── 구성 (Create 전에) — 추가한 순서대로 왼쪽→오른쪽, 묶음 전체는 오른쪽 끝에 붙는다 ──
	void AddToggle(const wchar_t* label, IsOn isOn, std::function<void()> toggle, const wchar_t* tip);
	// 글자가 바뀌거나 메뉴를 여는 버튼. isOn 을 주면 토글처럼 켜짐 표시도 한다 (객체스냅 ▾).
	void AddButton(Label label, OnClick onClick, const wchar_t* tip, IsOn isOn = nullptr);

	BOOL Create(CWnd* parent, UINT id);
	int  Height() const;
	void Refresh();   // 상태가 밖에서 바뀌었을 때 (메뉴·리본으로 투영 전환 등) — 글자 폭이 바뀔 수 있어 다시 배치

protected:
	BOOL PreTranslateMessage(MSG* pMsg) override;   // 툴팁에 마우스 메시지 전달

	afx_msg void    OnPaint();
	afx_msg BOOL    OnEraseBkgnd(CDC* pDC);
	afx_msg void    OnSize(UINT nType, int cx, int cy);
	afx_msg void    OnMouseMove(UINT nFlags, CPoint point);
	afx_msg LRESULT OnMouseLeave(WPARAM, LPARAM);
	afx_msg void    OnLButtonDown(UINT nFlags, CPoint point);
	afx_msg void    OnLButtonUp(UINT nFlags, CPoint point);
	afx_msg void    OnTimer(UINT_PTR nIDEvent);   // 명령행(ortho/polar/osnap …)으로 바뀐 상태도 따라 그린다
	DECLARE_MESSAGE_MAP()

private:
	struct Item {
		Label        label;
		IsOn         isOn;      // 없으면 토글이 아닌 일반 버튼
		OnClick      onClick;
		std::wstring tip;
		CRect        rc;
	};
	std::vector<Item> m_items;
	int  m_hot      = -1;
	int  m_pressed  = -1;
	bool m_tracking = false;

	float        m_scale = 1.0f;
	CFont        m_font;
	CToolTipCtrl m_tip;

	int  S(int px) const { return (int)(px * m_scale + 0.5f); }
	void Layout();
	int  HitItem(CPoint pt) const;
};
