// CadStatusBar.cpp: 상태바 구현 — 배치·그리기·마우스·툴팁
//

#include "pch.h"
#include "CadStatusBar.h"
#include "DarkTheme.h"

namespace {
	// 설계 크기 (96 DPI 픽셀) — 엔진 상태바와 같은 28px 줄
	constexpr int kBarH     = 28;
	constexpr int kButtonH  = 20;
	constexpr int kButtonMinW = 54;
	constexpr int kTextPadX = 10;
	constexpr int kGap      = 4;
	constexpr int kRightPad = 8;

	// 켜진 토글의 바탕 — 강조색을 바탕색에 섞는다 (리본의 켜진 버튼과 같은 느낌, GDI 는 반투명이 없어서)
	COLORREF Mix(COLORREF a, COLORREF b, int percentB)
	{
		auto ch = [&](int ca, int cb) { return (ca * (100 - percentB) + cb * percentB) / 100; };
		return RGB(ch(GetRValue(a), GetRValue(b)), ch(GetGValue(a), GetGValue(b)), ch(GetBValue(a), GetBValue(b)));
	}
}

BEGIN_MESSAGE_MAP(CCadStatusBar, CWnd)
	ON_WM_PAINT()
	ON_WM_ERASEBKGND()
	ON_WM_SIZE()
	ON_WM_MOUSEMOVE()
	ON_MESSAGE(WM_MOUSELEAVE, &CCadStatusBar::OnMouseLeave)
	ON_WM_LBUTTONDOWN()
	ON_WM_LBUTTONUP()
	ON_WM_TIMER()
END_MESSAGE_MAP()


// ── 구성 ────────────────────────────────────────────────────────────────

void CCadStatusBar::AddToggle(const wchar_t* label, IsOn isOn, std::function<void()> toggle, const wchar_t* tip)
{
	const std::wstring text(label);
	m_items.push_back({ [text] { return text; }, std::move(isOn),
	                    [toggle](const CRect&) { toggle(); }, tip ? tip : L"", CRect() });
}

void CCadStatusBar::AddButton(Label label, OnClick onClick, const wchar_t* tip, IsOn isOn)
{
	m_items.push_back({ std::move(label), std::move(isOn), std::move(onClick), tip ? tip : L"", CRect() });
}

BOOL CCadStatusBar::Create(CWnd* parent, UINT id)
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

	const CString cls = AfxRegisterWndClass(0, ::LoadCursor(nullptr, IDC_ARROW));
	if (!CWnd::Create(cls, nullptr, WS_CHILD | WS_VISIBLE, CRect(0, 0, 100, Height()), parent, id))
		return FALSE;

	// 버튼마다 툴팁 영역 하나 — 자리는 Layout 이 맞춘다
	m_tip.Create(this);
	for (int i = 0; i < (int)m_items.size(); ++i)
		m_tip.AddTool(this, m_items[i].tip.c_str(), CRect(0, 0, 1, 1), i + 1);
	m_tip.SetMaxTipWidth(S(360));   // 여러 줄 설명 허용

	// 상태는 버튼 말고도 바뀐다(명령행에 ortho off, fdim …) — 0.5초마다 다시 그려 따라간다.
	// 엔진에 묻는 값은 Get 함수 몇 개라 가볍다.
	SetTimer(1, 500, nullptr);

	Layout();
	return TRUE;
}

void CCadStatusBar::OnTimer(UINT_PTR)
{
	Invalidate(FALSE);
}

int CCadStatusBar::Height() const { return S(kBarH); }

void CCadStatusBar::Refresh()
{
	if (!GetSafeHwnd()) return;
	Layout();
	Invalidate(FALSE);
}


// ── 배치 ────────────────────────────────────────────────────────────────
// 글자 폭에 맞춘 버튼들을 오른쪽 끝에서부터 채운다(엔진 상태바와 같이 오른쪽 정렬).

void CCadStatusBar::OnSize(UINT nType, int cx, int cy)
{
	CWnd::OnSize(nType, cx, cy);
	Layout();
}

void CCadStatusBar::Layout()
{
	if (!GetSafeHwnd()) return;
	CRect client;
	GetClientRect(&client);

	CClientDC dc(this);
	CFont* old = dc.SelectObject(&m_font);
	std::vector<int> widths;
	int total = 0;
	for (const Item& it : m_items) {
		const std::wstring text = it.label();
		const int w = (std::max)(S(kButtonMinW), (int)dc.GetTextExtent(text.c_str(), (int)text.size()).cx + S(kTextPadX) * 2);
		widths.push_back(w);
		total += w + S(kGap);
	}
	dc.SelectObject(old);

	int x = client.right - S(kRightPad) - (total - S(kGap));
	const int top = (client.Height() - S(kButtonH)) / 2;
	for (int i = 0; i < (int)m_items.size(); ++i) {
		m_items[i].rc = CRect(x, top, x + widths[i], top + S(kButtonH));
		if (m_tip.GetSafeHwnd()) m_tip.SetToolRect(this, i + 1, m_items[i].rc);
		x += widths[i] + S(kGap);
	}
}


// ── 그리기 ──────────────────────────────────────────────────────────────

BOOL CCadStatusBar::OnEraseBkgnd(CDC*) { return TRUE; }

void CCadStatusBar::OnPaint()
{
	CPaintDC dc(this);
	CRect client;
	GetClientRect(&client);

	CDC mem;
	mem.CreateCompatibleDC(&dc);
	CBitmap bmp;
	bmp.CreateCompatibleBitmap(&dc, client.Width(), client.Height());
	CBitmap* oldBmp  = mem.SelectObject(&bmp);
	CFont*   oldFont = mem.SelectObject(&m_font);

	mem.FillSolidRect(client, DarkTheme::TabBarBg);
	mem.FillSolidRect(CRect(0, 0, client.right, 1), DarkTheme::Separator);   // 위 경계선
	mem.SetBkMode(TRANSPARENT);

	for (int i = 0; i < (int)m_items.size(); ++i) {
		const Item& it = m_items[i];
		const bool on      = it.isOn && it.isOn();
		const bool hot     = (i == m_hot);
		const bool pressed = (i == m_pressed) && hot;

		// 바탕: 켜짐 = 강조색 섞음, 꺼짐 = 본문색. 호버·누름은 한 단계씩 밝게.
		COLORREF fill   = on ? Mix(DarkTheme::BodyBg, DarkTheme::Accent, 35) : DarkTheme::BodyBg;
		COLORREF border = on ? DarkTheme::Accent : DarkTheme::Separator;
		if (pressed)  fill = on ? Mix(DarkTheme::BodyBg, DarkTheme::Accent, 55) : DarkTheme::Pressed;
		else if (hot) fill = on ? Mix(DarkTheme::BodyBg, DarkTheme::Accent, 45) : DarkTheme::Hover;

		CBrush brush(fill);
		CPen   pen(PS_SOLID, 1, border);
		CBrush* oldBrush = mem.SelectObject(&brush);
		CPen*   oldPen   = mem.SelectObject(&pen);
		mem.RoundRect(it.rc, CPoint(S(6), S(6)));
		mem.SelectObject(oldPen);
		mem.SelectObject(oldBrush);

		// 꺼진 토글은 글자를 흐리게 — 켜짐/꺼짐이 색만이 아니라 밝기로도 구분되게
		mem.SetTextColor((it.isOn && !on) ? DarkTheme::TextDim : DarkTheme::Text);
		const std::wstring text = it.label();
		CRect textRc = it.rc;
		mem.DrawText(text.c_str(), (int)text.size(), &textRc, DT_SINGLELINE | DT_CENTER | DT_VCENTER | DT_NOPREFIX);
	}

	dc.BitBlt(0, 0, client.Width(), client.Height(), &mem, 0, 0, SRCCOPY);
	mem.SelectObject(oldFont);
	mem.SelectObject(oldBmp);
}


// ── 마우스 ──────────────────────────────────────────────────────────────

BOOL CCadStatusBar::PreTranslateMessage(MSG* pMsg)
{
	if (m_tip.GetSafeHwnd()) m_tip.RelayEvent(pMsg);
	return CWnd::PreTranslateMessage(pMsg);
}

int CCadStatusBar::HitItem(CPoint pt) const
{
	for (int i = 0; i < (int)m_items.size(); ++i)
		if (m_items[i].rc.PtInRect(pt)) return i;
	return -1;
}

void CCadStatusBar::OnMouseMove(UINT, CPoint point)
{
	if (!m_tracking) {
		TRACKMOUSEEVENT tme = { sizeof(tme), TME_LEAVE, GetSafeHwnd(), 0 };
		m_tracking = TrackMouseEvent(&tme) != FALSE;
	}
	const int hot = HitItem(point);
	if (hot != m_hot) { m_hot = hot; Invalidate(FALSE); }
}

LRESULT CCadStatusBar::OnMouseLeave(WPARAM, LPARAM)
{
	m_tracking = false;
	m_hot = -1;
	Invalidate(FALSE);
	return 0;
}

void CCadStatusBar::OnLButtonDown(UINT, CPoint point)
{
	m_pressed = HitItem(point);
	if (m_pressed >= 0) { SetCapture(); Invalidate(FALSE); }
}

void CCadStatusBar::OnLButtonUp(UINT, CPoint point)
{
	const int i = m_pressed;
	m_pressed = -1;
	if (i < 0) return;
	ReleaseCapture();
	if (HitItem(point) == i && m_items[i].onClick) {
		CRect screenRc = m_items[i].rc;
		ClientToScreen(&screenRc);
		m_items[i].onClick(screenRc);
	}
	Refresh();   // 글자(Persp↔Ortho, 스타일 이름)가 바뀌면 폭도 바뀐다
}
