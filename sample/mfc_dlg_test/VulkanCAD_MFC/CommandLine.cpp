// CommandLine.cpp: 명령행 구현 — 입력 처리(Submit/Cancel/기록)·엔진 안내문·그리기
//

#include "pch.h"
#include "CommandLine.h"
#include "DarkTheme.h"
#include "CadText.h"

#include <uxtheme.h>
#pragma comment(lib, "uxtheme.lib")

namespace {
	// 설계 크기 (96 DPI 픽셀)
	constexpr int kPad      = 6;
	constexpr int kLogLines = 4;     // 기록 창에 보이는 줄 수
	constexpr int kRowH     = 24;    // 입력 줄 높이
	constexpr int kRowGap   = 5;     // 기록 창과 입력 줄 사이

	constexpr int kLogMax     = 300; // 기록 보관 줄 수 — 넘치면 오래된 것부터 지운다
	constexpr int kHistoryMax = 50;  // ↑/↓ 로 부를 수 있는 명령 수

	constexpr UINT kLogId   = 1;
	constexpr UINT kInputId = 2;

	// 엔진에 진행 중인 도구가 있는가 — 안내문이 있으면 도구가 값을 기다리는 중이다.
	// (콜백으로 받은 m_prompt 는 한 틱 늦을 수 있어, 판단은 엔진에 직접 묻는다.)
	bool EngineToolActive()
	{
		char buf[8];
		return CAD_GetPrompt(buf, sizeof(buf)) > 0;
	}
}

CCommandLine* CCommandLine::s_instance = nullptr;

BEGIN_MESSAGE_MAP(CCommandLine, CWnd)
	ON_WM_PAINT()
	ON_WM_ERASEBKGND()
	ON_WM_SIZE()
	ON_WM_CTLCOLOR()
	ON_WM_DESTROY()
	ON_EN_SETFOCUS(kInputId,  &CCommandLine::OnInputFocusChanged)
	ON_EN_KILLFOCUS(kInputId, &CCommandLine::OnInputFocusChanged)
END_MESSAGE_MAP()


// ── 만들기 ──────────────────────────────────────────────────────────────

BOOL CCommandLine::Create(CWnd* parent, UINT id)
{
	{
		CClientDC dc(parent);
		m_scale = dc.GetDeviceCaps(LOGPIXELSY) / 96.0f;
	}
	LOGFONT lf = {};
	wcscpy_s(lf.lfFaceName, L"Malgun Gothic");
	lf.lfQuality = CLEARTYPE_QUALITY;
	lf.lfHeight  = -S(12);   // 9pt — 리본과 같은 글꼴
	m_font.CreateFontIndirect(&lf);
	{
		CClientDC dc(parent);
		CFont* old = dc.SelectObject(&m_font);
		TEXTMETRIC tm;
		dc.GetTextMetrics(&tm);
		m_lineH = tm.tmHeight;
		dc.SelectObject(old);
	}
	m_inputBrush.CreateSolidBrush(DarkTheme::InputBg);

	const CString cls = AfxRegisterWndClass(0, ::LoadCursor(nullptr, IDC_ARROW));
	if (!CWnd::Create(cls, nullptr, WS_CHILD | WS_VISIBLE | WS_CLIPCHILDREN, CRect(0, 0, 100, Height()), parent, id))
		return FALSE;

	m_log.Create(WS_CHILD | WS_VISIBLE | WS_VSCROLL | ES_MULTILINE | ES_READONLY | ES_AUTOVSCROLL,
	             CRect(), this, kLogId);
	m_input.Create(WS_CHILD | WS_VISIBLE | WS_TABSTOP | ES_AUTOHSCROLL, CRect(), this, kInputId);
	m_log.SetFont(&m_font);
	m_input.SetFont(&m_font);
	m_input.SetLimitText(512);
	// 기록 창 스크롤바를 어둡게 (Windows 10 1809+). 안 되는 OS 에선 밝은 기본 스크롤바로 남는다.
	SetWindowTheme(m_log.GetSafeHwnd(), L"DarkMode_Explorer", nullptr);

	// 엔진이 안내문을 바꿀 때마다 알려준다 — 호출은 CAD_Tick 안(UI 스레드)이라 바로 창을 만져도 된다.
	s_instance = this;
	CAD_SetOnPrompt(&CCommandLine::OnEnginePrompt);

	AppendLog(L"명령을 입력하세요 — 예: l (선), c (원), b (박스).  3D 뷰에서 바로 타이핑해도 됩니다.");
	Layout();
	return TRUE;
}

int CCommandLine::Height() const
{
	return S(kPad) + (kLogLines * m_lineH + S(4)) + S(kRowGap) + S(kRowH) + S(kPad);
}

void CCommandLine::OnDestroy()
{
	CAD_SetOnPrompt(nullptr);
	s_instance = nullptr;
	CWnd::OnDestroy();
}


// ── 배치 ────────────────────────────────────────────────────────────────
// 위: 기록 창(전체 폭).  아래: [안내문][입력란] — 안내문 길이만큼 입력란이 오른쪽으로 밀린다.

void CCommandLine::OnSize(UINT nType, int cx, int cy)
{
	CWnd::OnSize(nType, cx, cy);
	Layout();
}

void CCommandLine::Layout()
{
	if (!m_input.GetSafeHwnd()) return;
	CRect rc;
	GetClientRect(&rc);

	const CRect logRc(S(kPad), S(kPad), rc.right - S(kPad), S(kPad) + kLogLines * m_lineH + S(4));
	m_log.MoveWindow(logRc.left + S(4), logRc.top + S(2), logRc.Width() - S(4), logRc.Height() - S(4));

	const int rowTop = logRc.bottom + S(kRowGap);
	const std::wstring label = m_prompt.empty() ? L"명령:" : m_prompt;
	int labelW;
	{
		CClientDC dc(this);
		CFont* old = dc.SelectObject(&m_font);
		labelW = dc.GetTextExtent(label.c_str(), (int)label.size()).cx;
		dc.SelectObject(old);
	}
	labelW = (std::min)(labelW, rc.Width() * 55 / 100);   // 긴 안내문도 입력란 자리는 남긴다

	m_labelRc    = CRect(S(kPad + 4), rowTop, S(kPad + 4) + labelW, rowTop + S(kRowH));
	m_inputFrame = CRect(m_labelRc.right + S(8), rowTop, rc.right - S(kPad), rowTop + S(kRowH));
	const int editTop = m_inputFrame.top + (m_inputFrame.Height() - m_lineH) / 2;   // 글자를 세로 가운데로
	m_input.MoveWindow(m_inputFrame.left + S(6), editTop, m_inputFrame.Width() - S(12), m_lineH);
	Invalidate(FALSE);
}


// ── 그리기 ──────────────────────────────────────────────────────────────

BOOL CCommandLine::OnEraseBkgnd(CDC*) { return TRUE; }

void CCommandLine::OnPaint()
{
	CPaintDC dc(this);   // WS_CLIPCHILDREN — 기록 창·입력란은 스스로 그린다
	CRect rc;
	GetClientRect(&rc);
	dc.FillSolidRect(rc, DarkTheme::BodyBg);
	dc.FillSolidRect(CRect(0, 0, rc.right, 1), DarkTheme::Separator);   // 3D 뷰와의 경계

	CRect logRc(S(kPad), S(kPad), rc.right - S(kPad), S(kPad) + kLogLines * m_lineH + S(4));
	dc.FillSolidRect(logRc, DarkTheme::InputBg);

	// 입력란 상자 — 포커스가 있으면 테두리를 강조색으로
	const bool focused = (GetFocus() == &m_input);
	dc.FillSolidRect(m_inputFrame, focused ? DarkTheme::Accent : DarkTheme::Separator);
	CRect inner = m_inputFrame;
	inner.DeflateRect(1, 1);
	dc.FillSolidRect(inner, DarkTheme::InputBg);

	// 안내문 — 도구가 값을 기다리면 앰버, 유휴면 흐린 "명령:"
	CFont* old = dc.SelectObject(&m_font);
	dc.SetBkMode(TRANSPARENT);
	dc.SetTextColor(m_prompt.empty() ? DarkTheme::TextDim : DarkTheme::Prompt);
	const std::wstring label = m_prompt.empty() ? L"명령:" : m_prompt;
	CRect labelRc = m_labelRc;
	dc.DrawText(label.c_str(), (int)label.size(), &labelRc, DT_SINGLELINE | DT_VCENTER | DT_END_ELLIPSIS | DT_NOPREFIX);
	dc.SelectObject(old);
}

HBRUSH CCommandLine::OnCtlColor(CDC* pDC, CWnd* pWnd, UINT nCtlColor)
{
	// 읽기 전용 기록 창은 CTLCOLOR_STATIC, 입력란은 CTLCOLOR_EDIT 로 온다 — 둘 다 같은 어두운 바탕.
	const HWND h = pWnd->GetSafeHwnd();
	if (h == m_log.GetSafeHwnd() || h == m_input.GetSafeHwnd()) {
		pDC->SetTextColor(DarkTheme::Text);
		pDC->SetBkColor(DarkTheme::InputBg);
		return m_inputBrush;
	}
	return CWnd::OnCtlColor(pDC, pWnd, nCtlColor);
}

void CCommandLine::OnInputFocusChanged()
{
	InvalidateRect(m_inputFrame, FALSE);
}


// ── 입력 ────────────────────────────────────────────────────────────────

BOOL CCommandLine::PreTranslateMessage(MSG* pMsg)
{
	// 대화상자는 Enter/Esc 를 확인/취소 버튼으로 바꿔 먹는다(IsDialogMessage).
	// MFC 는 포커스 창 → 부모 순으로 PreTranslateMessage 를 부르므로 여기서 먼저 가로챈다.
	if (pMsg->message == WM_KEYDOWN && pMsg->hwnd == m_input.GetSafeHwnd()) {
		switch (pMsg->wParam) {
		case VK_RETURN: Submit();          return TRUE;
		case VK_ESCAPE: Cancel();          return TRUE;
		case VK_UP:     RecallHistory(-1); return TRUE;
		case VK_DOWN:   RecallHistory(+1); return TRUE;
		}
	}
	return CWnd::PreTranslateMessage(pMsg);
}

void CCommandLine::TypeChar(wchar_t ch)
{
	m_input.SetFocus();
	const int len = m_input.GetWindowTextLength();
	m_input.SetSel(len, len);
	m_input.SendMessage(WM_CHAR, ch, 0);
}

void CCommandLine::Submit()
{
	CString s;
	m_input.GetWindowText(s);
	s.Trim();
	m_input.SetWindowText(L"");
	m_historyPos = -1;
	const std::wstring text(s);

	if (text.empty()) {
		if (EngineToolActive()) {
			// 진행 중 도구에 Enter — 폴리선 끝내기, "대상 선택 후 Enter" 확정 등.
			// CAD_ExecuteCommand 는 빈 문자열을 안 받으므로 키로 보낸다.
			CAD_OnKeyDownVK(VK_RETURN);
			CAD_OnKeyUpVK(VK_RETURN);
		} else if (!m_history.empty()) {
			Run(m_history.back());   // 오토캐드처럼 유휴 Enter = 직전 명령 반복
		}
		return;
	}

	if (m_history.empty() || m_history.back() != text) {
		m_history.push_back(text);
		if ((int)m_history.size() > kHistoryMax) m_history.erase(m_history.begin());
	}
	Run(text);
}

void CCommandLine::Cancel()
{
	m_input.SetWindowText(L"");
	m_historyPos = -1;
	if (EngineToolActive()) AppendLog(L"*취소*");
	CAD_ExecuteCommand("esc");   // 엔진이 ESC 키로 바꿔 진행 중 도구를 끝낸다
}

void CCommandLine::Run(const std::wstring& text)
{
	// 유휴면 명령 이름, 도구가 진행 중이면 그 도구에 줄 값(좌표·거리) — 엔진이 알아서 가른다.
	const bool toolActive = EngineToolActive();
	AppendLog((toolActive ? L"  > " : L"명령: ") + text);
	if (!CAD_ExecuteCommand(WideToUtf8(text).c_str()))
		AppendLog(toolActive ? L"  (진행 중인 도구가 이 값을 받지 않았습니다)" : L"알 수 없는 명령: " + text);
}

void CCommandLine::RecallHistory(int dir)
{
	if (m_history.empty()) return;
	const int n = (int)m_history.size();
	if (m_historyPos < 0) m_historyPos = n;   // 처음 ↑ 는 가장 최근 것부터
	m_historyPos = (std::max)(0, (std::min)(n, m_historyPos + dir));
	m_input.SetWindowText(m_historyPos < n ? m_history[m_historyPos].c_str() : L"");   // 끝을 지나면 빈 칸
	const int len = m_input.GetWindowTextLength();
	m_input.SetSel(len, len);
}


// ── 기록·안내문 ─────────────────────────────────────────────────────────

void CCommandLine::AppendLog(const std::wstring& line)
{
	const int len = m_log.GetWindowTextLength();
	m_log.SetSel(len, len);
	m_log.ReplaceSel(((len > 0 ? L"\r\n" : L"") + line).c_str());

	const int lines = m_log.GetLineCount();
	if (lines > kLogMax) {
		const int cut = m_log.LineIndex(lines - kLogMax);
		m_log.SetSel(0, cut);
		m_log.ReplaceSel(L"");
	}
	const int end = m_log.GetWindowTextLength();
	m_log.SetSel(end, end);
	m_log.SendMessage(EM_SCROLLCARET);
}

void CCommandLine::SetPrompt(const std::wstring& text)
{
	// 엔진 명령행처럼, 안내문이 **바뀔 때마다** 기록에도 한 줄 남겨 흐름이 보이게 한다.
	if (!text.empty() && text != m_lastLogged) {
		AppendLog(text);
		m_lastLogged = text;
	}
	if (text.empty()) m_lastLogged.clear();   // 유휴로 돌아가면 다음 안내문을 다시 기록

	m_prompt = text;
	Layout();   // 안내문 길이가 바뀌면 입력란 자리도 바뀐다
}

void CCommandLine::OnEnginePrompt(const char* utf8)
{
	if (s_instance) s_instance->SetPrompt(Utf8ToWide(utf8));
}
