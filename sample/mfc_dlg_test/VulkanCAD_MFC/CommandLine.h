#pragma once
// 명령행 — 3D 뷰 아래의 오토캐드식 명령 창. 다크 테마.
//
//   ┌ 기록 ───────────────────────────────────────────┐
//   │ 명령: l                                          │
//   │ 첫 번째 점 지정:                                 │
//   ├──────────────────────────────────────────────────┤
//   │ 다음 점 지정:  [ 입력란 ........................ ]│   ← 안내문(앰버) + 입력란
//   └──────────────────────────────────────────────────┘
//
// 입력은 엔진 명령행과 같은 규칙으로 CAD_ExecuteCommand 에 넘긴다.
//   - 도구가 없으면 명령 이름(line / l / 선 …), 도구가 진행 중이면 값(2,3 / 50 …)
//   - 빈 Enter: 진행 중 도구엔 Enter 키(폴리선 끝내기·선택 확정), 유휴면 직전 명령 반복
//   - Esc: 진행 중 도구 취소,  ↑/↓: 입력 기록
// 안내문은 엔진이 CAD_SetOnPrompt 로 알려준다(바뀔 때만, CAD_Tick 안에서).

#include <string>
#include <vector>

class CCommandLine : public CWnd
{
public:
	BOOL Create(CWnd* parent, UINT id);
	int  Height() const;   // 화면 DPI 를 반영한 픽셀 높이 — 부모가 3D 뷰를 이만큼 줄인다

	// 3D 뷰가 키보드를 쥐고 있을 때 부모가 넘겨준다 — 오토캐드처럼 뷰에서 바로 타이핑.
	void TypeChar(wchar_t ch);   // 입력란으로 포커스를 옮기고 글자를 넣는다
	void Submit();               // Enter
	void Cancel();               // Esc

	void AppendLog(const std::wstring& line);

protected:
	BOOL PreTranslateMessage(MSG* pMsg) override;   // 입력란의 Enter/Esc/↑/↓ — 대화상자가 먹기 전에

	afx_msg void   OnPaint();
	afx_msg BOOL   OnEraseBkgnd(CDC* pDC);
	afx_msg void   OnSize(UINT nType, int cx, int cy);
	afx_msg HBRUSH OnCtlColor(CDC* pDC, CWnd* pWnd, UINT nCtlColor);
	afx_msg void   OnDestroy();
	afx_msg void   OnInputFocusChanged();   // 입력란 테두리를 포커스 따라 강조색으로
	DECLARE_MESSAGE_MAP()

private:
	CEdit  m_log;      // 기록 (읽기 전용, 여러 줄)
	CEdit  m_input;    // 입력란 (한 줄)
	CFont  m_font;
	CBrush m_inputBrush;

	std::wstring              m_prompt;           // 지금 안내문 — 비면 "명령:"
	std::wstring              m_lastLogged;       // 같은 안내문을 기록에 두 번 남기지 않게
	std::vector<std::wstring> m_history;          // 사용자가 친 명령 (↑/↓)
	int                       m_historyPos = -1;  // -1 = 기록 탐색 중 아님

	float m_scale = 1.0f;   // 96 DPI = 1.0
	int   m_lineH = 16;     // 글꼴 한 줄 높이 (픽셀)
	CRect m_labelRc;        // 안내문 자리
	CRect m_inputFrame;     // 입력란 테두리 상자

	int  S(int px) const { return (int)(px * m_scale + 0.5f); }
	void Layout();
	void Run(const std::wstring& text);
	void RecallHistory(int dir);
	void SetPrompt(const std::wstring& text);

	static CCommandLine* s_instance;           // 엔진 콜백은 C 함수 포인터라 사용자 데이터가 없다
	static void OnEnginePrompt(const char* utf8);
};
