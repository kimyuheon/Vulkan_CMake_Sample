// ToolPanel.cpp: 도구 패널 구현 — 컨트롤 쌓기·다크 색칠·알림을 콜백으로
//

#include "pch.h"
#include "ToolPanel.h"
#include "DarkTheme.h"

#include <uxtheme.h>
#include <cwchar>

namespace {
	// 설계 크기 (96 DPI 픽셀)
	constexpr int kWidth   = 270;
	constexpr int kHeaderH = 32;
	constexpr int kLineH   = 17;   // 상태 글 한 줄
	constexpr int kSliderSteps = 1000;
}

BEGIN_MESSAGE_MAP(CToolPanel, CWnd)
	ON_WM_PAINT()
	ON_WM_ERASEBKGND()
	ON_WM_CTLCOLOR()
	ON_WM_HSCROLL()
	ON_WM_DRAWITEM()
	ON_WM_LBUTTONUP()
	ON_WM_TIMER()
END_MESSAGE_MAP()


// ── 만들기·열기·닫기 ────────────────────────────────────────────────────

BOOL CToolPanel::Create(CWnd* parent, UINT id)
{
	{
		CClientDC dc(parent);
		m_scale = dc.GetDeviceCaps(LOGPIXELSY) / 96.0f;
	}
	LOGFONT lf = {};
	wcscpy_s(lf.lfFaceName, L"Malgun Gothic");
	lf.lfQuality = CLEARTYPE_QUALITY;
	lf.lfHeight  = -S(12);
	m_font.CreateFontIndirect(&lf);
	lf.lfWeight  = FW_BOLD;
	m_boldFont.CreateFontIndirect(&lf);
	m_bodyBrush.CreateSolidBrush(DarkTheme::BodyBg);
	m_inputBrush.CreateSolidBrush(DarkTheme::InputBg);

	const CString cls = AfxRegisterWndClass(0, ::LoadCursor(nullptr, IDC_ARROW));
	return CWnd::Create(cls, nullptr, WS_CHILD | WS_CLIPCHILDREN, CRect(0, 0, Width(), 100), parent, id);
}

int CToolPanel::Width() const { return S(kWidth); }

void CToolPanel::Open(const wchar_t* title, int kind)
{
	Close();
	m_title = title;
	m_kind  = kind;
	m_y     = S(kHeaderH) + S(4);
	ShowWindow(SW_SHOW);
	SetTimer(1, 250, nullptr);
	Invalidate();
}

void CToolPanel::Close()
{
	if (m_kind == 0) return;
	KillTimer(1);
	auto closed = std::move(onClose);   // 콜백 안에서 다시 Open 해도 꼬이지 않게 먼저 떼어 둔다
	onClose = nullptr;
	onTick  = nullptr;
	m_kind  = 0;
	ClearControls();
	ShowWindow(SW_HIDE);
	if (closed) closed();
}

void CToolPanel::ClearControls()
{
	for (auto& c : m_controls)
		if (c->GetSafeHwnd()) c->DestroyWindow();
	m_controls.clear();
	m_textColor.clear();
	m_clicks.clear();
	m_toggles.clear();
	m_choices.clear();
	m_sliders.clear();
	m_status = nullptr;
}


// ── 컨트롤 쌓기 ─────────────────────────────────────────────────────────
// 모두 위에서 아래로 m_y 를 내려가며 놓는다. 폭은 패널 폭에서 양쪽 여백을 뺀 것.

CStatic* CToolPanel::AddLabel(const wchar_t* text, int height, COLORREF color, CFont* font)
{
	CStatic* st = Keep(new CStatic);
	st->Create(text, WS_CHILD | WS_VISIBLE | SS_LEFT | SS_NOPREFIX,
	           CRect(ContentLeft(), m_y, ContentLeft() + ContentWidth(), m_y + height), this, m_nextId++);
	st->SetFont(font);
	m_textColor[st->GetSafeHwnd()] = color;
	m_y += height;
	return st;
}

void CToolPanel::AddSection(const wchar_t* text)
{
	m_y += S(8);
	AddLabel(text, S(20), DarkTheme::Accent, &m_boldFont);
	m_y += S(2);
}

void CToolPanel::AddNote(const wchar_t* text, int lines)
{
	AddLabel(text, lines * S(kLineH), DarkTheme::TextDim, &m_font);
	m_y += S(4);
}

void CToolPanel::AddChoice(const wchar_t* label, const std::vector<std::wstring>& items, int selected,
                           std::function<void(int)> onChange)
{
	AddLabel(label, S(18), DarkTheme::TextDim, &m_font);
	const UINT id = m_nextId++;
	CComboBox* combo = Keep(new CComboBox);
	// 높이는 펼친 목록까지 — 닫힌 상자 높이는 글꼴이 정한다
	combo->Create(WS_CHILD | WS_VISIBLE | WS_VSCROLL | WS_TABSTOP | CBS_DROPDOWNLIST,
	              CRect(ContentLeft(), m_y, ContentLeft() + ContentWidth(), m_y + S(240)), this, id);
	combo->SetFont(&m_font);
	for (const std::wstring& item : items) combo->AddString(item.c_str());
	combo->SetCurSel(selected);
	SetWindowTheme(combo->GetSafeHwnd(), L"DarkMode_CFD", nullptr);   // 어두운 콤보 (Windows 10 1809+)
	m_choices[id] = std::move(onChange);

	CRect rc;
	combo->GetWindowRect(&rc);
	m_y += rc.Height() + S(6);
}

int CToolPanel::AddSlider(const wchar_t* label, float min, float max, float value, int decimals,
                          std::function<void(float)> onChange)
{
	Slider s{ nullptr, nullptr, label, min, max, decimals, std::move(onChange) };
	s.label = AddLabel(SliderText(s, value).c_str(), S(18), DarkTheme::TextDim, &m_font);

	const UINT id = m_nextId++;
	s.ctrl = Keep(new CSliderCtrl);
	s.ctrl->Create(WS_CHILD | WS_VISIBLE | WS_TABSTOP | TBS_HORZ | TBS_NOTICKS,
	               CRect(ContentLeft() - S(4), m_y, ContentLeft() + ContentWidth() + S(4), m_y + S(24)), this, id);
	s.ctrl->SetRange(0, kSliderSteps);
	s.ctrl->SetPos((int)((value - min) / (max - min) * kSliderSteps + 0.5f));
	m_sliders[id] = std::move(s);
	m_y += S(28);
	return (int)id;
}

void CToolPanel::AddToggle(const wchar_t* label, bool value, std::function<void(bool)> onChange)
{
	const UINT id = m_nextId++;
	CButton* box = Keep(new CButton);
	box->Create(label, WS_CHILD | WS_VISIBLE | WS_TABSTOP | BS_AUTOCHECKBOX,
	            CRect(ContentLeft(), m_y, ContentLeft() + ContentWidth(), m_y + S(22)), this, id);
	box->SetFont(&m_font);
	box->SetCheck(value ? BST_CHECKED : BST_UNCHECKED);
	// 테마를 끄면 체크박스 글자가 WM_CTLCOLORSTATIC 색을 따른다 (테마 상태에선 늘 검은 글자라 어두운 바탕에 안 보인다)
	SetWindowTheme(box->GetSafeHwnd(), L"", L"");
	m_textColor[box->GetSafeHwnd()] = DarkTheme::Text;
	m_toggles[id] = std::move(onChange);
	m_y += S(24);
}

void CToolPanel::AddButton(const wchar_t* text, std::function<void()> onClick)
{
	const UINT id = m_nextId++;
	CButton* button = Keep(new CButton);
	button->Create(text, WS_CHILD | WS_VISIBLE | WS_TABSTOP | BS_OWNERDRAW,   // 다크로 직접 그린다 (OnDrawItem)
	               CRect(ContentLeft(), m_y, ContentLeft() + ContentWidth(), m_y + S(26)), this, id);
	button->SetFont(&m_font);
	m_clicks[id] = std::move(onClick);
	m_y += S(30);
}

void CToolPanel::AddStatus(int lines)
{
	m_y += S(6);
	m_status = AddLabel(L"", lines * S(kLineH), DarkTheme::TextDim, &m_font);
}

void CToolPanel::SetStatus(const std::wstring& text)
{
	if (!m_status) return;
	CString now;
	m_status->GetWindowText(now);
	if (now != text.c_str()) m_status->SetWindowText(text.c_str());   // 같은 글이면 안 다시 그린다 (깜빡임)
}

void CToolPanel::SetSliderValue(int slider, float value)
{
	auto it = m_sliders.find((UINT)slider);
	if (it == m_sliders.end()) return;
	Slider& s = it->second;
	s.ctrl->SetPos((int)((value - s.min) / (s.max - s.min) * kSliderSteps + 0.5f));
	s.label->SetWindowText(SliderText(s, value).c_str());
}

std::wstring CToolPanel::SliderText(const Slider& s, float value) const
{
	wchar_t buf[128];
	swprintf_s(buf, L"%s:  %.*f", s.name.c_str(), s.decimals, value);
	return buf;
}


// ── 알림 → 콜백 ─────────────────────────────────────────────────────────
// 콜백은 복사해 두고 부른다 — 콜백 안에서 패널을 닫으면(Close) 원본 map 이 비워지기 때문.

BOOL CToolPanel::OnCommand(WPARAM wParam, LPARAM lParam)
{
	const UINT id   = LOWORD(wParam);
	const UINT code = HIWORD(wParam);
	if (code == BN_CLICKED && m_clicks.count(id)) {
		auto fn = m_clicks[id];
		fn();
		return TRUE;
	}
	if (code == BN_CLICKED && m_toggles.count(id)) {
		auto fn = m_toggles[id];
		fn(((CButton*)GetDlgItem(id))->GetCheck() == BST_CHECKED);
		return TRUE;
	}
	if (code == CBN_SELCHANGE && m_choices.count(id)) {
		auto fn = m_choices[id];
		fn(((CComboBox*)GetDlgItem(id))->GetCurSel());
		return TRUE;
	}
	return CWnd::OnCommand(wParam, lParam);
}

void CToolPanel::OnHScroll(UINT nSBCode, UINT, CScrollBar* pScrollBar)
{
	if (!pScrollBar || nSBCode == TB_ENDTRACK) return;   // 끌기가 끝날 때 한 번 더 오는 알림은 건너뛴다
	auto it = m_sliders.find((UINT)pScrollBar->GetDlgCtrlID());
	if (it == m_sliders.end()) return;
	const Slider& s = it->second;
	const float value = s.min + (s.max - s.min) * s.ctrl->GetPos() / kSliderSteps;
	s.label->SetWindowText(SliderText(s, value).c_str());
	auto fn = s.onChange;
	if (fn) fn(value);
}

void CToolPanel::OnTimer(UINT_PTR)
{
	auto fn = onTick;
	if (fn) fn();
}


// ── 그리기 ──────────────────────────────────────────────────────────────

BOOL CToolPanel::OnEraseBkgnd(CDC*) { return TRUE; }

void CToolPanel::OnPaint()
{
	CPaintDC dc(this);   // WS_CLIPCHILDREN — 컨트롤 자리는 컨트롤이 그린다
	CRect rc;
	GetClientRect(&rc);
	dc.FillSolidRect(rc, DarkTheme::BodyBg);
	dc.FillSolidRect(CRect(0, 0, rc.right, S(kHeaderH)), DarkTheme::TabBarBg);   // 제목 줄
	dc.FillSolidRect(CRect(0, 0, 1, rc.bottom), DarkTheme::Separator);           // 3D 뷰와의 경계

	dc.SetBkMode(TRANSPARENT);
	CFont* old = dc.SelectObject(&m_boldFont);
	dc.SetTextColor(DarkTheme::Text);
	CRect titleRc(S(12), 0, rc.right - S(36), S(kHeaderH));
	dc.DrawText(m_title.c_str(), (int)m_title.size(), &titleRc, DT_SINGLELINE | DT_VCENTER | DT_END_ELLIPSIS | DT_NOPREFIX);

	m_closeRc = CRect(rc.right - S(34), 0, rc.right, S(kHeaderH));
	dc.SelectObject(&m_font);
	dc.SetTextColor(DarkTheme::TextDim);
	CRect x = m_closeRc;
	dc.DrawText(L"✕", 1, &x, DT_SINGLELINE | DT_CENTER | DT_VCENTER);
	dc.SelectObject(old);
}

void CToolPanel::OnLButtonUp(UINT, CPoint point)
{
	if (m_closeRc.PtInRect(point)) Close();
}

HBRUSH CToolPanel::OnCtlColor(CDC* pDC, CWnd* pWnd, UINT nCtlColor)
{
	if (nCtlColor == CTLCOLOR_EDIT || nCtlColor == CTLCOLOR_LISTBOX) {   // 콤보의 목록
		pDC->SetTextColor(DarkTheme::Text);
		pDC->SetBkColor(DarkTheme::InputBg);
		return m_inputBrush;
	}
	// 이름표·체크박스·슬라이더 바탕
	auto it = m_textColor.find(pWnd->GetSafeHwnd());
	pDC->SetTextColor(it != m_textColor.end() ? it->second : DarkTheme::Text);
	pDC->SetBkColor(DarkTheme::BodyBg);
	return m_bodyBrush;
}

void CToolPanel::OnDrawItem(int nIDCtl, LPDRAWITEMSTRUCT lpDis)
{
	if (lpDis->CtlType != ODT_BUTTON) { CWnd::OnDrawItem(nIDCtl, lpDis); return; }

	CDC* dc = CDC::FromHandle(lpDis->hDC);
	const CRect rc(lpDis->rcItem);
	const bool pressed = (lpDis->itemState & ODS_SELECTED) != 0;
	const bool focused = (lpDis->itemState & ODS_FOCUS) != 0;

	dc->FillSolidRect(rc, DarkTheme::BodyBg);
	CBrush brush(pressed ? DarkTheme::Pressed : DarkTheme::Hover);
	CPen   pen(PS_SOLID, 1, focused ? DarkTheme::Accent : DarkTheme::Separator);
	CBrush* oldBrush = dc->SelectObject(&brush);
	CPen*   oldPen   = dc->SelectObject(&pen);
	dc->RoundRect(rc, CPoint(S(6), S(6)));
	dc->SelectObject(oldPen);
	dc->SelectObject(oldBrush);

	CString text;
	GetDlgItem(nIDCtl)->GetWindowText(text);
	CFont* old = dc->SelectObject(&m_font);
	dc->SetBkMode(TRANSPARENT);
	dc->SetTextColor(DarkTheme::Text);
	CRect textRc = rc;
	dc->DrawText(text, &textRc, DT_SINGLELINE | DT_CENTER | DT_VCENTER | DT_NOPREFIX);
	dc->SelectObject(old);
}
