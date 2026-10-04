#pragma once
// 도구 패널 — 3D 뷰 오른쪽에 붙는 세로 패널(단면·내비·파티클). 다크 테마.
//
// 엔진의 패널(section / nav / particle 명령)은 ImGui 창이라 임베드(호스트 창 안)에선 그려지지 않는다.
// 그래서 같은 기능을 C API 로 부르는 패널을 호스트(MFC)가 직접 그린다.
//
// 이 클래스는 CAD 를 모른다 — 위에서 아래로 컨트롤을 쌓는 틀만 준다(C# 갤러리의 Ui.* 와 같은 모양).
// 어떤 패널에 무엇을 둘지는 VulkanCAD_MFCDlg_Panels.cpp 에서 정한다.
//
//   m_panel.Open(L"단면", kind);
//   m_panel.AddSection(L"자르기");
//   m_panel.AddChoice(L"방식", { L"끄기", L"평면" }, 1, [](int i) { … });
//   m_panel.AddSlider(L"위치", 0, 100, 50, 0, [](float v) { … });
//   m_panel.AddStatus();

#include <functional>
#include <map>
#include <memory>
#include <string>
#include <vector>

class CToolPanel : public CWnd
{
public:
	BOOL Create(CWnd* parent, UINT id);   // 숨긴 채로 만든다 — Open 하면 보인다
	int  Width() const;                   // 화면 DPI 를 반영한 폭

	// 열기 — 이전 내용을 닫고(onClose 호출) 제목을 단다. 이어서 Add* 로 채운다. kind 는 부르는 쪽이 정하는 번호.
	void Open(const wchar_t* title, int kind);
	void Close();                         // X 버튼도 이것을 부른다
	int  Kind() const { return m_kind; }  // 0 = 닫힘

	void AddSection(const wchar_t* text);
	void AddNote(const wchar_t* text, int lines = 1);   // 흐린 설명 글
	void AddChoice(const wchar_t* label, const std::vector<std::wstring>& items, int selected,
	               std::function<void(int)> onChange);
	// 반환 = 슬라이더 번호 — 밖에서 값을 바꿀 때 SetSliderValue 에 준다 (축을 바꾸면 위치를 가운데로 등)
	int  AddSlider(const wchar_t* label, float min, float max, float value, int decimals,
	               std::function<void(float)> onChange);
	void AddToggle(const wchar_t* label, bool value, std::function<void(bool)> onChange);
	void AddButton(const wchar_t* text, std::function<void()> onClick);
	void AddStatus(int lines = 5);        // 아래 상태 글 — SetStatus 로 바꾼다
	void SetStatus(const std::wstring& text);
	void SetSliderValue(int slider, float value);   // 콜백은 부르지 않는다

	std::function<void()> onTick;    // 열려 있는 동안 0.25초마다 — 상태 갱신·점 찍기 결과 확인
	std::function<void()> onClose;   // 닫힐 때 한 번 (X 버튼 포함) — 부모가 배치를 다시 한다

protected:
	BOOL OnCommand(WPARAM wParam, LPARAM lParam) override;   // 버튼·체크·콤보 알림을 콜백으로

	afx_msg void   OnPaint();
	afx_msg BOOL   OnEraseBkgnd(CDC* pDC);
	afx_msg HBRUSH OnCtlColor(CDC* pDC, CWnd* pWnd, UINT nCtlColor);
	afx_msg void   OnHScroll(UINT nSBCode, UINT nPos, CScrollBar* pScrollBar);   // 슬라이더
	afx_msg void   OnDrawItem(int nIDCtl, LPDRAWITEMSTRUCT lpDis);              // 다크 버튼
	afx_msg void   OnLButtonUp(UINT nFlags, CPoint point);                        // 제목 줄 X
	afx_msg void   OnTimer(UINT_PTR nIDEvent);
	DECLARE_MESSAGE_MAP()

private:
	struct Slider {
		CSliderCtrl* ctrl;
		CStatic*     label;
		std::wstring name;
		float        min, max;
		int          decimals;
		std::function<void(float)> onChange;
	};

	int   m_kind = 0;
	int   m_y    = 0;        // 다음 컨트롤을 놓을 y
	UINT  m_nextId = 100;
	std::wstring m_title;
	CRect m_closeRc;

	std::vector<std::unique_ptr<CWnd>>   m_controls;
	std::map<HWND, COLORREF>             m_textColor;    // 컨트롤별 글자색 (머리말 = 강조색, 이름표 = 흐리게)
	std::map<UINT, std::function<void()>>     m_clicks;  // 버튼
	std::map<UINT, std::function<void(bool)>> m_toggles; // 체크
	std::map<UINT, std::function<void(int)>>  m_choices; // 콤보
	std::map<UINT, Slider>                    m_sliders;
	CStatic* m_status = nullptr;

	float  m_scale = 1.0f;
	CFont  m_font, m_boldFont;
	CBrush m_bodyBrush, m_inputBrush;

	int  S(int px) const { return (int)(px * m_scale + 0.5f); }
	int  ContentLeft() const  { return S(12); }
	int  ContentWidth() const { return Width() - S(24); }
	CStatic* AddLabel(const wchar_t* text, int height, COLORREF color, CFont* font);
	template <class T> T* Keep(T* wnd) { m_controls.emplace_back(wnd); return wnd; }
	void ClearControls();
	std::wstring SliderText(const Slider& s, float value) const;
};
