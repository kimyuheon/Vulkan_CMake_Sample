// VulkanCAD_MFCDlg_Ribbon.cpp: 리본 구성 — 어떤 탭·그룹에 어떤 엔진 명령을 둘지
//
// 엔진(3dEngine) 리본과 같은 탭(홈 / 2D / 3D)·같은 그룹·같은 명령이다.
// 버튼을 더하거나 빼려면 이 파일만 고치면 된다 — 그리기·마우스는 RibbonBar.cpp 가 맡는다.
//
// ⚠️ 엔진 명령 중 **자체 패널·대화상자를 여는 것**(array, dp, section, nav, particle …)은
//    임베드(호스트 창 안)에선 그 창이 그려지지 않아 눌러도 아무 일이 없어 보인다.
//    그런 기능은 명령행에서 값을 묻는 명령(3a, dim …)이나 C API + 호스트 UI 로 대신한다.

#include "pch.h"
#include "framework.h"
#include "VulkanCAD_MFC.h"
#include "VulkanCAD_MFCDlg.h"

#include "CadText.h"   // VulkanCAD_API.h + UTF-8 도우미

#include <algorithm>
#include <string>
#include <vector>

namespace {
	// 대부분의 버튼은 엔진 명령행 이름으로 부른다(명령행에 그대로 쳐도 같은 동작).
	// 엔진 명령이 200개가 넘어 하나씩 C API 를 뚫는 대신 CAD_ExecuteCommand 로 가는 것이 이 샘플의 방침이다.
	CRibbonBar::Action Command(const char* name)
	{
		return [name] { CAD_ExecuteCommand(name); };
	}
}

void CVulkanCADMFCDlg::BuildRibbon()
{
	CRibbonBar& r = m_ribbon;
	using I = RibbonIcon;

	// ── 홈 ──────────────────────────────────────────────────────────────
	r.AddTab(L"홈");

	r.AddGroup(L"기즈모");   // 화면 핸들을 끌어 변형 — 기준점을 묻지 않는다
	r.AddLarge(L"이동", I::GizmoMove,   Command("gm"));
	r.AddLarge(L"회전", I::GizmoRotate, Command("gr"));
	r.AddLarge(L"축척", I::GizmoScale,  Command("gs"));

	r.AddGroup(L"편집");     // 기준점 → 목적지(또는 값 입력)
	r.AddLarge(L"이동", I::Move,   Command("m"));
	r.AddSmall(L"회전", I::Rotate, Command("ro"));
	r.AddSmall(L"축척", I::Scale,  Command("sc"));
	r.AddSmall(L"복사", I::Copy,   Command("cp"));
	r.AddSmall(L"배열", I::Array,  Command("3a"));   // ar 은 대화상자 — 3a 는 명령행에 "r 열 행 층 dx dy dz" 를 묻는다

	r.AddGroup(L"선택");
	r.AddLarge(L"전체 선택",   I::SelectAll, [] { CAD_RequestSelectAll(); });
	r.AddSmall(L"삭제",        I::Delete,    [] { CAD_RequestDeleteSelected(); });
	r.AddSmall(L"모두 지우기", I::ClearAll,  [] { CAD_RequestClearAll(); });

	r.AddGroup(L"실행");
	r.AddLarge(L"실행취소", I::Undo, [] { CAD_Undo(); });
	r.AddLarge(L"다시실행", I::Redo, [] { CAD_Redo(); });

	r.AddGroup(L"뷰");
	r.AddLarge(L"전체보기", I::ZoomExtents, [] { CAD_RequestZoomExtents(); });
	r.AddSmall(L"선택 맞춤", I::Focus,      [] { CAD_RequestFocusSelected(); });
	// 토글 — 상태바 Persp/Ortho 버튼과 같은 상태를 본다(켜짐 = 직교 투영).
	// 상태바의 '직교(F8)' 는 그리기 방향 고정(직교 추적)이라 다른 기능 — 이름을 구분한다.
	r.AddSmall(L"직교 투영", I::Projection, [this] { OnCadProjection(); }, [this] { return m_ortho; });
	r.AddSmall(L"아이소",    I::ViewIso,    [] { CAD_RequestSetView(3); });   // 0=Front 1=Top 2=Right 3=Iso
	r.AddSmall(L"평면",      I::ViewTop,    [] { CAD_RequestSetView(1); });
	r.AddSmall(L"정면",      I::ViewFront,  [] { CAD_RequestSetView(0); });
	r.AddSmall(L"우측면",    I::ViewRight,  [] { CAD_RequestSetView(2); });

	// ── 2D ──────────────────────────────────────────────────────────────
	r.AddTab(L"2D");

	r.AddGroup(L"그리기");
	r.AddLarge(L"선",       I::Line,      Command("l"));
	r.AddSmall(L"사각형",   I::Rectangle, Command("rec"));
	r.AddSmall(L"원",       I::Circle,    Command("c"));
	r.AddSmall(L"정다각형", I::Polygon,   Command("pol"));
	r.AddSmall(L"호",       I::Arc,       Command("a"));
	r.AddSmall(L"폴리선",   I::Polyline,  Command("pl"));
	r.AddSmall(L"문자",     I::Text,      Command("text"));
	r.AddSmall(L"치수",     I::Dimension, Command("dim"));   // 정렬 치수 (dp 는 치수 패널이라 임베드에선 안 보인다)

	r.AddGroup(L"수정");
	r.AddLarge(L"오프셋", I::Offset,  Command("of"));
	r.AddSmall(L"대칭",   I::Mirror,  Command("mi"));
	r.AddSmall(L"자르기", I::Trim,    Command("tr"));
	r.AddSmall(L"연장",   I::Extend,  Command("ex"));
	r.AddSmall(L"필렛",   I::Fillet,  Command("fil"));
	r.AddSmall(L"모따기", I::Chamfer, Command("cha"));

	// ── 3D ──────────────────────────────────────────────────────────────
	r.AddTab(L"3D");

	r.AddGroup(L"프리미티브");
	r.AddLarge(L"박스",   I::Box,      Command("b"));                 // 3단계 스케치
	r.AddSmall(L"큐브",   I::Box,      [] { CAD_RequestAddCube(); }); // 기본 크기로 바로
	r.AddSmall(L"구",     I::Sphere,   Command("sphere"));
	r.AddSmall(L"원기둥", I::Cylinder, Command("cyl"));
	r.AddSmall(L"원뿔",   I::Cone,     Command("cone"));
	r.AddSmall(L"토러스", I::Torus,    Command("torus"));

	r.AddGroup(L"형상");     // 닫힌 스케치 → 솔리드
	r.AddLarge(L"돌출",   I::Extrude, Command("x"));
	r.AddSmall(L"회전체", I::Revolve, Command("rev"));
	r.AddSmall(L"스윕",   I::Sweep,   Command("sw"));
	r.AddSmall(L"로프트", I::Loft,    Command("lo"));
	r.AddSmall(L"셸",     I::Shell,   Command("sh"));

	r.AddGroup(L"편집");
	r.AddLarge(L"합집합",   I::Union,      Command("union"));
	// 두 번 누르는 버튼 — 첫 번째에 기준을 기억하는 동안 켜짐(강조색)으로 보인다.
	r.AddSmall(L"차집합",   I::Difference, [this] { SubtractStep(); }, [this] { return !m_subtractBase.empty(); });
	r.AddSmall(L"교집합",   I::Intersect,  Command("intersect"));
	r.AddSmall(L"슬라이스", I::Slice,      Command("sl"));   // 단면 평면으로 자른다 — 평면은 '도구 > 단면' 패널에서

	// ── 도구 — 엔진에선 자체 패널이 여는 기능들. 임베드에선 그 패널이 안 보여 호스트 패널(ToolPanel)로 연다 ──
	r.AddTab(L"도구");

	r.AddGroup(L"보기");
	r.AddLarge(L"단면", I::Section, [this] { TogglePanel(kPanelSection); },
	           [this] { return m_panel.Kind() == kPanelSection; });

	r.AddGroup(L"시뮬레이션");
	r.AddLarge(L"내비",   I::Navigation, [this] { TogglePanel(kPanelNav); },
	           [this] { return m_panel.Kind() == kPanelNav; });
	r.AddLarge(L"파티클", I::Particles,  [this] { TogglePanel(kPanelParticles); },
	           [this] { return m_panel.Kind() == kPanelParticles; });
}

// 차집합 — 엔진의 차집합은 기준(A)·빼기(B)를 고르는 대화상자를 여는데 임베드에선 그 창이 안 보인다.
// 엔진의 선택 목록은 id 순(std::set)이라 "먼저 고른 것" 도 알 수 없다.
// 그래서 오토캐드 SUBTRACT 처럼 두 단계로 받는다.
//   1) 기준 솔리드 선택 → 차집합   : 기준을 기억
//   2) 뺄 솔리드 선택   → 차집합   : CAD_BooleanDifference 실행
void CVulkanCADMFCDlg::SubtractStep()
{
	std::vector<uint32_t> selected(CAD_GetSelectedCount());
	for (uint32_t i = 0; i < (uint32_t)selected.size(); ++i)
		CAD_GetSelectedObjectId(i, &selected[i]);

	if (m_subtractBase.empty()) {
		if (selected.empty()) {
			m_commandLine.AppendLog(L"차집합: 기준이 될 솔리드를 먼저 선택하고 다시 누르세요");
			return;
		}
		m_subtractBase = selected;
		m_commandLine.AppendLog(L"차집합: 기준 " + std::to_wstring(selected.size()) +
		                        L"개 기억함 — 뺄 솔리드를 선택하고 차집합을 다시 누르세요 (빈 선택이면 취소)");
		return;
	}

	// 기준으로 고른 것은 뺄 목록에서 제외 (기준을 선택한 채 뺄 것을 추가로 고른 경우)
	std::vector<uint32_t> tools;
	for (uint32_t id : selected)
		if (std::find(m_subtractBase.begin(), m_subtractBase.end(), id) == m_subtractBase.end())
			tools.push_back(id);

	if (tools.empty()) {
		m_commandLine.AppendLog(L"차집합: 취소");
	} else {
		// 바로 실행형 — 그 자리에서 계산해 결과 개수를 돌려준다. 실패면 아무것도 안 바꾸고 0 + 이유.
		// keepOriginals=false(원본 삭제), perBase=false(기준들을 한 덩어리로 — 오토캐드 SUBTRACT 와 같음)
		const uint32_t made = CAD_BooleanSubtract(m_subtractBase.data(), (uint32_t)m_subtractBase.size(),
		                                          tools.data(), (uint32_t)tools.size(),
		                                          false, false, nullptr, 0);
		if (made > 0)
			m_commandLine.AppendLog(L"차집합: 기준 " + std::to_wstring(m_subtractBase.size()) +
			                        L"개에서 " + std::to_wstring(tools.size()) + L"개를 뺌");
		else
			m_commandLine.AppendLog(L"차집합 실패: " + CadLastError());
	}
	m_subtractBase.clear();
}
