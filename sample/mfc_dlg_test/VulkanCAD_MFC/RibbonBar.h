#pragma once
// 리본 — 탭 한 줄 + 그룹(큰 버튼 + 작은 버튼 3줄 격자) + 그룹 이름 띠. 다크 테마.
//
// 엔진(3dEngine) 의 ImGui 리본을 MFC 로 옮긴 자체 그리기 컨트롤이다.
// MFC 의 CMFCRibbonBar 는 CFrameWndEx 에만 붙어서 대화상자에선 쓸 수 없다.
//
// 이 클래스는 CAD 를 모른다 — 버튼마다 실행할 함수(람다)를 받는다.
// 어떤 탭에 어떤 명령을 둘지는 VulkanCAD_MFCDlg_Ribbon.cpp 에서 정한다.
//
//   m_ribbon.AddTab(L"홈");
//   m_ribbon.AddGroup(L"실행");
//   m_ribbon.AddLarge(L"실행취소", RibbonIcon::Undo, [] { CAD_Undo(); });
//   m_ribbon.Create(this, IDC_RIBBON);

#include "RibbonIcons.h"

#include <functional>
#include <string>
#include <vector>

class CRibbonBar : public CWnd
{
public:
	using Action = std::function<void()>;
	using IsOn   = std::function<bool()>;   // 켜짐(토글) 상태 — 참이면 강조색으로 그린다

	// ── 구성 (Create 전에) — 마지막에 추가한 탭/그룹에 이어 붙는다 ──
	void AddTab(const wchar_t* title);
	void AddGroup(const wchar_t* caption);
	void AddLarge(const wchar_t* label, RibbonIcon icon, Action action, IsOn isOn = nullptr);
	void AddSmall(const wchar_t* label, RibbonIcon icon, Action action, IsOn isOn = nullptr);

	BOOL Create(CWnd* parent, UINT id);
	int  Height() const;   // 화면 DPI 를 반영한 픽셀 높이 — 부모가 3D 뷰를 이만큼 아래에 둔다

protected:
	afx_msg void    OnPaint();
	afx_msg BOOL    OnEraseBkgnd(CDC* pDC);
	afx_msg void    OnMouseMove(UINT nFlags, CPoint point);
	afx_msg LRESULT OnMouseLeave(WPARAM, LPARAM);
	afx_msg void    OnLButtonDown(UINT nFlags, CPoint point);
	afx_msg void    OnLButtonUp(UINT nFlags, CPoint point);
	afx_msg void    OnDestroy();
	DECLARE_MESSAGE_MAP()

private:
	struct Button {
		std::wstring label;
		RibbonIcon   icon;
		bool         large;
		Action       action;
		IsOn         isOn;
		CRect        rc;
	};
	struct Group {
		std::wstring        caption;
		std::vector<Button> buttons;
		CRect               rc;   // 버튼 영역(이름 띠 제외)
	};
	struct Tab {
		std::wstring       title;
		std::vector<Group> groups;
		CRect              rc;    // 탭 줄의 글자 칸
	};

	std::vector<Tab> m_tabs;
	int     m_activeTab = 0;
	int     m_hotTab    = -1;
	Button* m_hot       = nullptr;   // 마우스가 올라간 버튼
	Button* m_pressed   = nullptr;   // 누른 채인 버튼 (뗄 때 같은 버튼이면 실행)
	bool    m_tracking  = false;     // TrackMouseEvent 로 WM_MOUSELEAVE 를 기다리는 중

	float     m_scale = 1.0f;        // 96 DPI = 1.0
	CFont     m_font, m_captionFont;
	ULONG_PTR m_gdiplusToken = 0;

	int     S(int px) const { return (int)(px * m_scale + 0.5f); }   // 설계 픽셀 → 화면 픽셀
	void    Layout();
	Button* HitButton(CPoint pt);
	int     HitTab(CPoint pt) const;
};
