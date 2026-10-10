// RibbonBar.cpp: 리본 컨트롤 구현 — 배치(Layout)·그리기(OnPaint)·마우스
//

#include "pch.h"
#include "RibbonBar.h"
#include "DarkTheme.h"

#include <gdiplus.h>
#pragma comment(lib, "gdiplus.lib")

using namespace Gdiplus;

namespace {
	// 설계 크기 (96 DPI 픽셀) — 엔진 리본과 같은 비율: 탭 한 줄 + 3줄 격자 + 이름 띠.
	constexpr int kTabH        = 26;
	constexpr int kTabPadX     = 12;
	constexpr int kPadTop      = 5;
	constexpr int kRows        = 3;    // 그룹당 작은 버튼 줄 수
	constexpr int kSmallH      = 22;
	constexpr int kCellGap     = 2;
	constexpr int kGridH       = kSmallH * kRows + kCellGap * (kRows - 1);
	constexpr int kCaptionGap  = 3;
	constexpr int kCaptionH    = 17;
	constexpr int kPadBottom   = 4;
	constexpr int kGroupGap    = 9;    // 그룹 사이 (세로 구분선 양쪽)
	constexpr int kLargeMinW   = 54;
	constexpr int kLargeLabelH = 18;   // 큰 버튼 아래 이름 칸
	constexpr int kSmallIconW  = 24;   // 작은 버튼의 아이콘 칸 (글자는 그 오른쪽)

	Color ToColor(COLORREF c, BYTE alpha = 255)
	{
		return Color(alpha, GetRValue(c), GetGValue(c), GetBValue(c));
	}

	RectF ToRectF(const CRect& r)
	{
		return RectF((REAL)r.left, (REAL)r.top, (REAL)r.Width(), (REAL)r.Height());
	}

	void RoundRect(GraphicsPath& path, const CRect& r, float radius)
	{
		const float d = radius * 2;
		const RectF f = ToRectF(r);
		path.AddArc(f.X,               f.Y,                d, d, 180, 90);
		path.AddArc(f.GetRight() - d,  f.Y,                d, d, 270, 90);
		path.AddArc(f.GetRight() - d,  f.GetBottom() - d,  d, d,   0, 90);
		path.AddArc(f.X,               f.GetBottom() - d,  d, d,  90, 90);
		path.CloseFigure();
	}

	void DrawLabel(Graphics& g, const std::wstring& text, const Gdiplus::Font& font, const CRect& r,
	              COLORREF color, StringAlignment align)
	{
		StringFormat fmt;
		fmt.SetAlignment(align);
		fmt.SetLineAlignment(StringAlignmentCenter);
		fmt.SetFormatFlags(StringFormatFlagsNoWrap);
		fmt.SetTrimming(StringTrimmingNone);
		SolidBrush brush(ToColor(color));
		g.DrawString(text.c_str(), (INT)text.size(), &font, ToRectF(r), &fmt, &brush);
	}
}

BEGIN_MESSAGE_MAP(CRibbonBar, CWnd)
	ON_WM_PAINT()
	ON_WM_ERASEBKGND()
	ON_WM_MOUSEMOVE()
	ON_MESSAGE(WM_MOUSELEAVE, &CRibbonBar::OnMouseLeave)
	ON_WM_LBUTTONDOWN()
	ON_WM_LBUTTONUP()
	ON_WM_DESTROY()
END_MESSAGE_MAP()


// ── 구성 ────────────────────────────────────────────────────────────────

void CRibbonBar::AddTab(const wchar_t* title)
{
	m_tabs.push_back({ title, {}, CRect() });
}

void CRibbonBar::AddGroup(const wchar_t* caption)
{
	ASSERT(!m_tabs.empty());   // AddTab 먼저
	m_tabs.back().groups.push_back({ caption, {}, CRect() });
}

void CRibbonBar::AddLarge(const wchar_t* label, RibbonIcon icon, Action action, IsOn isOn)
{
	ASSERT(!m_tabs.empty() && !m_tabs.back().groups.empty());   // AddGroup 먼저
	m_tabs.back().groups.back().buttons.push_back({ label, icon, true, std::move(action), std::move(isOn), CRect() });
}

void CRibbonBar::AddSmall(const wchar_t* label, RibbonIcon icon, Action action, IsOn isOn)
{
	ASSERT(!m_tabs.empty() && !m_tabs.back().groups.empty());
	m_tabs.back().groups.back().buttons.push_back({ label, icon, false, std::move(action), std::move(isOn), CRect() });
}

BOOL CRibbonBar::Create(CWnd* parent, UINT id)
{
	GdiplusStartupInput startup;
	GdiplusStartup(&m_gdiplusToken, &startup, nullptr);

	// DPI 배율과 글꼴. 맑은 고딕 — 한글이 깔끔하게 나오는 기본 UI 글꼴.
	{
		CClientDC dc(parent);
		m_scale = dc.GetDeviceCaps(LOGPIXELSY) / 96.0f;
	}
	LOGFONT lf = {};
	wcscpy_s(lf.lfFaceName, L"Malgun Gothic");
	lf.lfQuality = CLEARTYPE_QUALITY;
	lf.lfHeight  = -S(12);   // 9pt
	m_font.CreateFontIndirect(&lf);
	lf.lfHeight  = -S(11);
	m_captionFont.CreateFontIndirect(&lf);

	CRect rc;
	parent->GetClientRect(&rc);
	rc.bottom = rc.top + Height();
	const CString cls = AfxRegisterWndClass(0, ::LoadCursor(nullptr, IDC_ARROW));
	if (!CWnd::Create(cls, nullptr, WS_CHILD | WS_VISIBLE | WS_CLIPSIBLINGS, rc, parent, id))
		return FALSE;

	Layout();
	return TRUE;
}

int CRibbonBar::Height() const
{
	return S(kTabH + kPadTop + kGridH + kCaptionGap + kCaptionH + kPadBottom);
}

void CRibbonBar::OnDestroy()
{
	CWnd::OnDestroy();
	if (m_gdiplusToken) { GdiplusShutdown(m_gdiplusToken); m_gdiplusToken = 0; }
}


// ── 배치 ────────────────────────────────────────────────────────────────
// 글자 폭에 맞춰 모든 탭의 버튼 자리를 한 번에 정한다(창 폭과 무관 — 넘치면 오른쪽이 잘린다).
// 큰 버튼은 격자 높이를 다 쓰는 한 칸, 작은 버튼은 위→아래로 3줄을 채우고 다음 열로 간다.

void CRibbonBar::Layout()
{
	CClientDC dc(this);
	CFont* old = dc.SelectObject(&m_font);
	auto textW = [&](const std::wstring& s) -> int { return dc.GetTextExtent(s.c_str(), (int)s.size()).cx; };

	const int top = S(kTabH + kPadTop);
	int tabX = S(6);

	for (Tab& tab : m_tabs)
	{
		const int tw = textW(tab.title) + S(kTabPadX) * 2;
		tab.rc = CRect(tabX, 0, tabX + tw, S(kTabH));
		tabX += tw;

		int x = S(8);
		for (Group& group : tab.groups)
		{
			int cursor   = x;
			int row      = 0;
			int colW     = 0;
			size_t colStart = 0;   // 지금 채우는 작은 버튼 열의 첫 버튼

			// 열이 끝나면 그 열의 버튼 폭을 가장 넓은 것에 맞춘다(호버 판이 고르게).
			auto closeColumn = [&](size_t end) {
				for (size_t i = colStart; i < end; ++i)
					group.buttons[i].rc.right = group.buttons[i].rc.left + colW;
				cursor += colW + S(kCellGap) * 2;
				row = 0; colW = 0;
			};

			for (size_t i = 0; i < group.buttons.size(); ++i)
			{
				Button& b = group.buttons[i];
				if (b.large)
				{
					if (row > 0) closeColumn(i);
					const int w = (std::max)(S(kLargeMinW), textW(b.label) + S(14));
					b.rc = CRect(cursor, top, cursor + w, top + S(kGridH));
					cursor += w + S(kCellGap) * 2;
				}
				else
				{
					if (row == 0) colStart = i;
					const int w = S(kSmallIconW) + textW(b.label) + S(10);
					const int y = top + row * S(kSmallH + kCellGap);
					b.rc = CRect(cursor, y, cursor + w, y + S(kSmallH));
					colW = (std::max)(colW, w);
					if (++row == kRows) closeColumn(i + 1);
				}
			}
			if (row > 0) closeColumn(group.buttons.size());

			dc.SelectObject(&m_captionFont);
			const int captionW = textW(group.caption) + S(12);
			dc.SelectObject(&m_font);

			const int groupW = (std::max)(cursor - S(kCellGap) * 2 - x, captionW);
			group.rc = CRect(x, top, x + groupW, top + S(kGridH));
			x += groupW + S(kGroupGap) * 2 + 1;   // 구분선 + 양쪽 간격
		}
	}
	dc.SelectObject(old);
}


// ── 그리기 ──────────────────────────────────────────────────────────────

BOOL CRibbonBar::OnEraseBkgnd(CDC*) { return TRUE; }   // OnPaint 가 전부 칠한다 (깜빡임 방지)

void CRibbonBar::OnPaint()
{
	CPaintDC dc(this);
	CRect client;
	GetClientRect(&client);

	// 메모리 DC 에 그린 뒤 한 번에 복사 — 마우스 호버마다 다시 그려도 깜빡이지 않는다.
	CDC mem;
	mem.CreateCompatibleDC(&dc);
	CBitmap bmp;
	bmp.CreateCompatibleBitmap(&dc, client.Width(), client.Height());
	CBitmap* oldBmp = mem.SelectObject(&bmp);
	{
		Graphics g(mem.GetSafeHdc());
		g.SetSmoothingMode(SmoothingModeAntiAlias);
		g.SetTextRenderingHint(TextRenderingHintClearTypeGridFit);
		Gdiplus::Font font(mem.GetSafeHdc(), (HFONT)m_font.GetSafeHandle());
		Gdiplus::Font captionFont(mem.GetSafeHdc(), (HFONT)m_captionFont.GetSafeHandle());

		// 바탕: 탭 줄(더 어둡게) · 본문 · 맨 아래 1px 선
		SolidBrush tabBg(ToColor(DarkTheme::TabBarBg)), bodyBg(ToColor(DarkTheme::BodyBg));
		g.FillRectangle(&tabBg,  0, 0, client.Width(), S(kTabH));
		g.FillRectangle(&bodyBg, 0, S(kTabH), client.Width(), client.Height() - S(kTabH));
		Pen sep(ToColor(DarkTheme::Separator), 1.0f);
		g.DrawLine(&sep, 0.0f, client.bottom - 0.5f, (REAL)client.right, client.bottom - 0.5f);

		// 탭 — 글자 + 활성 탭 밑줄
		SolidBrush accent(ToColor(DarkTheme::Accent));
		for (int i = 0; i < (int)m_tabs.size(); ++i)
		{
			const Tab& tab = m_tabs[i];
			const bool active = (i == m_activeTab);
			DrawLabel(g, tab.title, font, tab.rc,
			         (active || i == m_hotTab) ? DarkTheme::Text : DarkTheme::TextDim, StringAlignmentCenter);
			if (active)
				g.FillRectangle(&accent, tab.rc.left + S(4), tab.rc.bottom - S(2), tab.rc.Width() - S(8), S(2));
		}

		if (m_activeTab < (int)m_tabs.size())
		{
			SolidBrush captionBg(ToColor(DarkTheme::CaptionBg));
			for (const Group& group : m_tabs[m_activeTab].groups)
			{
				// 그룹 이름 띠 + 오른쪽 구분선
				const CRect band(group.rc.left - S(3), group.rc.bottom + S(kCaptionGap),
				                 group.rc.right + S(3), group.rc.bottom + S(kCaptionGap + kCaptionH));
				GraphicsPath bandPath;
				RoundRect(bandPath, band, (float)S(3));
				g.FillPath(&captionBg, &bandPath);
				DrawLabel(g, group.caption, captionFont, band, DarkTheme::TextDim, StringAlignmentCenter);

				const REAL sepX = (REAL)(group.rc.right + S(kGroupGap));
				g.DrawLine(&sep, sepX, (REAL)group.rc.top + S(2), sepX, (REAL)band.bottom);

				for (const Button& b : group.buttons)
				{
					// 판: 켜짐(강조색) > 누름 > 호버. 켜진 버튼은 테두리도 강조색.
					const bool on      = b.isOn && b.isOn();
					const bool pressed = (&b == m_pressed) && (&b == m_hot);
					const bool hot     = (&b == m_hot);
					GraphicsPath plate;
					RoundRect(plate, b.rc, (float)S(b.large ? 5 : 4));
					if (on) {
						SolidBrush fill(ToColor(DarkTheme::Accent, pressed ? 140 : 80));
						g.FillPath(&fill, &plate);
						Pen edge(ToColor(DarkTheme::Accent, 230), 1.0f);
						g.DrawPath(&edge, &plate);
					} else if (pressed || hot) {
						SolidBrush fill(ToColor(pressed ? DarkTheme::Pressed : DarkTheme::Hover));
						g.FillPath(&fill, &plate);
					}

					if (b.large)
					{
						// 큰 아이콘 위, 이름 아래 (오피스·오토캐드 리본식)
						const float iconCy = b.rc.top + (b.rc.Height() - S(kLargeLabelH)) * 0.5f;
						DrawRibbonIcon(g, b.icon, (float)b.rc.CenterPoint().x, iconCy, 12.0f * m_scale, DarkTheme::Text);
						const CRect label(b.rc.left, b.rc.bottom - S(kLargeLabelH + 1), b.rc.right, b.rc.bottom - S(1));
						DrawLabel(g, b.label, font, label, DarkTheme::Text, StringAlignmentCenter);
					}
					else
					{
						// 작은 아이콘 왼쪽, 이름 오른쪽
						DrawRibbonIcon(g, b.icon, (float)(b.rc.left + S(kSmallIconW / 2 + 1)), (float)b.rc.CenterPoint().y,
						               7.0f * m_scale, DarkTheme::Text);
						const CRect label(b.rc.left + S(kSmallIconW), b.rc.top, b.rc.right, b.rc.bottom);
						DrawLabel(g, b.label, font, label, DarkTheme::Text, StringAlignmentNear);
					}
				}
			}
		}
	}   // Graphics 를 먼저 닫아야 메모리 DC 에 다 그려진다
	dc.BitBlt(0, 0, client.Width(), client.Height(), &mem, 0, 0, SRCCOPY);
	mem.SelectObject(oldBmp);
}


// ── 마우스 ──────────────────────────────────────────────────────────────

CRibbonBar::Button* CRibbonBar::HitButton(CPoint pt)
{
	if (m_activeTab >= (int)m_tabs.size()) return nullptr;
	for (Group& group : m_tabs[m_activeTab].groups)
		for (Button& b : group.buttons)
			if (b.rc.PtInRect(pt)) return &b;
	return nullptr;
}

int CRibbonBar::HitTab(CPoint pt) const
{
	for (int i = 0; i < (int)m_tabs.size(); ++i)
		if (m_tabs[i].rc.PtInRect(pt)) return i;
	return -1;
}

void CRibbonBar::OnMouseMove(UINT, CPoint point)
{
	if (!m_tracking) {   // 창 밖으로 나가면 WM_MOUSELEAVE 로 호버를 끈다
		TRACKMOUSEEVENT tme = { sizeof(tme), TME_LEAVE, GetSafeHwnd(), 0 };
		m_tracking = TrackMouseEvent(&tme) != FALSE;
	}
	Button* hot    = HitButton(point);
	const int tab  = HitTab(point);
	if (hot != m_hot || tab != m_hotTab) {
		m_hot = hot;
		m_hotTab = tab;
		Invalidate(FALSE);
	}
}

LRESULT CRibbonBar::OnMouseLeave(WPARAM, LPARAM)
{
	m_tracking = false;
	m_hot = nullptr;
	m_hotTab = -1;
	Invalidate(FALSE);
	return 0;
}

void CRibbonBar::OnLButtonDown(UINT, CPoint point)
{
	const int tab = HitTab(point);
	if (tab >= 0 && tab != m_activeTab) {
		m_activeTab = tab;
		m_hot = nullptr;
		Invalidate(FALSE);
		return;
	}
	if (Button* b = HitButton(point)) {
		m_pressed = b;
		SetCapture();   // 누른 채 밖으로 끌었다 놓으면 실행하지 않도록 뗄 때까지 잡아 둔다
		Invalidate(FALSE);
	}
}

void CRibbonBar::OnLButtonUp(UINT, CPoint point)
{
	if (!m_pressed) return;
	Button* b = m_pressed;
	m_pressed = nullptr;
	ReleaseCapture();
	if (HitButton(point) == b && b->action)
		b->action();
	Invalidate(FALSE);   // 토글 버튼의 켜짐 상태가 바뀌었을 수 있다
}
