// LotCAD_MFCDlg_Panels.cpp: 도구 패널 내용 — 단면 · 내비(경로 주행) · 파티클
//
// 엔진의 section / nav / particle 명령은 ImGui 패널을 여는데, 임베드에선 그 패널이 그려지지 않는다.
// 같은 일을 C API(CAD_SetSection* · CAD_Nav* · CAD_*Particle*)로 하는 패널을 여기서 만든다.
// 화면 틀(콤보·슬라이더·버튼 쌓기)은 ToolPanel.cpp, 여기는 "무엇을 부를지" 만.
//
// 패널마다 상태는 shared_ptr 하나에 담아 람다들이 나눠 갖는다 — 패널을 닫으면 컨트롤과 함께 사라진다.
// 데모 장면을 만들지 않는다. 지금 열린 사용자 장면에 그대로 적용한다.

#include "pch.h"
#include "framework.h"
#include "LotCAD_MFC.h"
#include "LotCAD_MFCDlg.h"
#include "CadText.h"

#include <cstdlib>
#include <memory>

namespace {
	// ── 아주 작은 JSON 읽기 ──
	// 이 샘플이 읽는 건 상태 JSON 의 숫자·참거짓 몇 개뿐이라, 라이브러리 없이 키를 찾아 바로 읽는다.
	// (실제 앱이라면 nlohmann/json 같은 파서를 쓰는 편이 낫다.)
	size_t FindKey(const std::string& j, const char* key, size_t from = 0)
	{
		const std::string k = std::string("\"") + key + "\"";
		const size_t p = j.find(k, from);
		return p == std::string::npos ? p : p + k.size();
	}
	double JsonNumber(const std::string& j, const char* key, double fallback, size_t from = 0)
	{
		size_t p = FindKey(j, key, from);
		if (p == std::string::npos || (p = j.find(':', p)) == std::string::npos) return fallback;
		return std::strtod(j.c_str() + p + 1, nullptr);
	}
	bool JsonBool(const std::string& j, const char* key, size_t from = 0)
	{
		size_t p = FindKey(j, key, from);
		if (p == std::string::npos || (p = j.find_first_not_of(" :", p)) == std::string::npos) return false;
		return j.compare(p, 4, "true") == 0;
	}
	bool JsonVec3(const std::string& j, const char* key, float out[3], size_t from = 0)
	{
		size_t p = FindKey(j, key, from);
		if (p == std::string::npos || (p = j.find('[', p)) == std::string::npos) return false;
		const char* c = j.c_str() + p + 1;
		for (int i = 0; i < 3; ++i) {
			char* end = nullptr;
			out[i] = std::strtof(c, &end);
			for (c = end; *c == ',' || *c == ' '; ++c) {}
		}
		return true;
	}
	int JsonCount(const std::string& j, const char* needle)
	{
		int n = 0;
		for (size_t p = j.find(needle); p != std::string::npos; p = j.find(needle, p + 1)) ++n;
		return n;
	}

	std::wstring Format(const wchar_t* fmt, double a, double b = 0, double c = 0)
	{
		wchar_t buf[256];
		swprintf_s(buf, fmt, a, b, c);
		return buf;
	}

	std::vector<uint32_t> SelectedIds()
	{
		std::vector<uint32_t> ids(CAD_GetSelectedCount());
		for (uint32_t i = 0; i < (uint32_t)ids.size(); ++i) CAD_GetSelectedObjectId(i, &ids[i]);
		return ids;
	}
}


// ── 열고 닫기 ────────────────────────────────────────────────────────────

// 리본 버튼 — 같은 패널이 열려 있으면 닫고, 아니면 (다른 패널을 닫고) 연다.
void CLotCADMFCDlg::TogglePanel(int kind)
{
	if (m_panel.Kind() == kind) { m_panel.Close(); return; }   // onClose 가 배치를 다시 한다
	switch (kind) {
	case kPanelSection:   OpenSectionPanel();  break;
	case kPanelNav:       OpenNavPanel();      break;
	case kPanelParticles: OpenParticlePanel(); break;
	}
	RelayoutNow();
}

// 패널이 열리고 닫히면 3D 뷰·명령행 폭이 바뀐다. 리본의 켜짐 표시도 같이.
void CLotCADMFCDlg::RelayoutNow()
{
	CRect rc;
	GetClientRect(&rc);
	LayoutChildren(rc.Width(), rc.Height());
	m_ribbon.Invalidate(FALSE);
}


// ── 단면 ────────────────────────────────────────────────────────────────
// 셰이더가 잘라 보여 주는 **보기 상태**(되돌리기 없음). 엔진 단면 패널과 같은 상태라,
// 패널을 닫아도 단면은 남는다 — 다시 열면 지금 상태에서 이어서 고친다. 끄려면 방식 "끄기".

void CLotCADMFCDlg::OpenSectionPanel()
{
	struct State {
		int   mode = 1, axis = 2;          // 1 평면, Z 축 (평면도처럼 수평으로 자르기)
		bool  flip = false, arrows = true, selectedOnly = false;
		float pos = 0, thickPct = 10;      // 위치(장면 단위), 슬라이스 두께(축 범위의 %)
		float min[3] = { -1, -1, -1 }, max[3] = { 1, 1, 1 };   // 장면 경계 = 슬라이더 범위
		int   posSlider = 0;
		float Extent() const { return (std::max)(max[axis] - min[axis], 1e-6f); }
	};
	auto st = std::make_shared<State>();

	// 상태 JSON — {"mode","axis","pos","flip","thickness","showArrows","selectedOnly","sceneBounds":{"min","max"} …}
	auto readBounds = [st] {
		const std::string j = CadReadJson(CAD_GetSectionStateJson);
		const size_t b = j.find("\"sceneBounds\"");
		if (b != std::string::npos) { JsonVec3(j, "min", st->min, b); JsonVec3(j, "max", st->max, b); }
		return j;
	};
	const std::string j = readBounds();
	if (JsonNumber(j, "mode", 0) != 0) {   // 이미 켜져 있으면 그 상태로 시작
		st->mode = (int)JsonNumber(j, "mode", 1);
		st->axis = (int)JsonNumber(j, "axis", 2);
		st->pos  = (float)JsonNumber(j, "pos", 0);
		st->flip = JsonBool(j, "flip");
		const float t = (float)JsonNumber(j, "thickness", 0);
		if (t > 0) st->thickPct = (std::min)(50.0f, (std::max)(1.0f, t / st->Extent() * 100));
	} else {
		st->pos = (st->min[st->axis] + st->max[st->axis]) / 2;   // 가운데부터
	}
	st->arrows       = JsonBool(j, "showArrows") || FindKey(j, "showArrows") == std::string::npos;
	st->selectedOnly = JsonBool(j, "selectedOnly");

	auto apply = [st] {
		const int a = st->axis;
		if (st->mode == 3) {
			// 상자: 다른 두 축은 장면 가운데 절반, 고른 축은 장면 바닥부터 위치 슬라이더까지
			float lo[3], hi[3];
			for (int i = 0; i < 3; ++i) {
				const float c = (st->min[i] + st->max[i]) / 2, h = (st->max[i] - st->min[i]) / 4;
				lo[i] = c - h; hi[i] = c + h;
			}
			lo[a] = st->min[a];
			hi[a] = (std::max)(st->pos, lo[a] + st->Extent() * 0.01f);
			CAD_SetSectionBox(lo[0], lo[1], lo[2], hi[0], hi[1], hi[2]);
		}
		CAD_SetSection(st->mode, a, st->pos, st->flip, st->Extent() * st->thickPct / 100);
	};
	auto boundsText = [st] {
		return Format(L"장면 범위  X %.1f ~ %.1f", st->min[0], st->max[0]) +
		       Format(L"\nY %.1f ~ %.1f", st->min[1], st->max[1]) +
		       Format(L" · Z %.1f ~ %.1f", st->min[2], st->max[2]) +
		       L"\n닫아도 단면은 남습니다 — 끄려면 방식 '끄기'";
	};
	auto percent = [st] { return (st->pos - st->min[st->axis]) / st->Extent() * 100; };

	CToolPanel& p = m_panel;
	p.Open(L"단면 — 잘라서 속 보기", kPanelSection);

	p.AddSection(L"자르기");
	p.AddChoice(L"방식", { L"끄기", L"평면 — 한쪽을 잘라 냄", L"슬라이스 — 두께만 남김", L"상자 — 상자 안만 남김" },
	            st->mode, [st, apply](int i) { st->mode = i; apply(); });
	p.AddChoice(L"축", { L"X", L"Y", L"Z" }, st->axis, [this, st, apply](int i) {
		st->axis = i;
		st->pos  = (st->min[i] + st->max[i]) / 2;   // 축을 바꾸면 가운데로
		m_panel.SetSliderValue(st->posSlider, 50);
		apply();
	});
	st->posSlider = p.AddSlider(L"위치 (범위의 %)", 0, 100, percent(), 0, [st, apply](float v) {
		st->pos = st->min[st->axis] + st->Extent() * v / 100;
		apply();
	});
	p.AddSlider(L"슬라이스 두께 (범위의 %)", 1, 50, st->thickPct, 0, [st, apply](float v) { st->thickPct = v; apply(); });
	p.AddToggle(L"반대쪽 자르기", st->flip, [st, apply](bool v) { st->flip = v; apply(); });

	p.AddSection(L"표시");
	p.AddToggle(L"방향 화살표", st->arrows, [st](bool v) {
		st->arrows = v; CAD_SetSectionOptions(st->arrows, st->selectedOnly);
	});
	p.AddToggle(L"선택한 객체만 자르기", st->selectedOnly, [st](bool v) {
		st->selectedOnly = v; CAD_SetSectionOptions(st->arrows, st->selectedOnly);
	});

	p.AddSection(L"단면으로 하기");
	p.AddButton(L"단면 → 2D 윤곽 (폴리선)", [this] {
		const uint32_t n = CAD_ExtractSectionTo2D(true);   // true = 뒤쪽 회색 참조선도
		m_panel.SetStatus(n ? L"2D 윤곽 " + std::to_wstring(n) + L"개를 모델 오른쪽 XY 평면에 만들었습니다"
		                    : L"만든 윤곽 없음 — 방식이 '평면' 이고 솔리드를 지나야 합니다\n" + CadLastError());
	});
	p.AddButton(L"슬라이스 — 단면으로 솔리드를 둘로", [] { CAD_ExecuteCommand("sl"); });
	p.AddButton(L"범위 다시 읽기 (장면을 바꾼 뒤)", [this, readBounds, boundsText] {
		readBounds();
		m_panel.SetStatus(boundsText());
	});
	p.AddStatus(4);
	p.SetStatus(boundsText());
	p.onClose = [this] { RelayoutNow(); };

	CAD_SetSectionOptions(st->arrows, st->selectedOnly);
	apply();   // 열자마자 잘린 모습이 보이게
}


// ── 내비 (경로 주행) ─────────────────────────────────────────────────────
// 장면에서 지나갈 수 있는 격자 지도를 만들고, 객체를 주행 대상으로 등록해 목적지까지 장애물을 피해 가게 한다.
// agentId 0 = "체크된 대상 전부" (엔진 패널과 같은 규약). 단위는 장면 단위(m 장면이면 m).

void CLotCADMFCDlg::OpenNavPanel()
{
	struct State {
		bool grid = true, vehicle = false, picking = false;
		std::wstring msg = L"순서: 지도 만들기 → 객체 선택 후 대상 추가 → 목적지 찍기 → 출발";
	};
	auto st = std::make_shared<State>();

	CToolPanel& p = m_panel;
	p.Open(L"내비 — 경로 주행", kPanelNav);

	p.AddSection(L"1. 지도");
	p.AddButton(L"지도 만들기 (장면에서 자동)", [st] {
		// NULL = 격자 크기·로봇 반경을 장면에서 추정. {"cellSize":0.2} 처럼 일부만 줄 수도 있다.
		if (CAD_NavBuildMap(nullptr)) { CAD_NavShowGrid(st->grid); st->msg = L"지도를 만들었습니다"; }
		else                            st->msg = L"지도 실패: " + CadLastError();
	});
	p.AddToggle(L"격자 보이기", st->grid, [st](bool v) { st->grid = v; CAD_NavShowGrid(v); });

	p.AddSection(L"2. 주행 대상");
	p.AddToggle(L"차량으로 추가 (앞바퀴 조향)", st->vehicle, [st](bool v) { st->vehicle = v; });
	p.AddButton(L"선택한 객체를 대상으로 추가", [st] {
		const std::vector<uint32_t> ids = SelectedIds();
		int added = 0;
		for (uint32_t id : ids) if (CAD_NavAddAgent(id, st->vehicle)) ++added;
		st->msg = ids.empty() ? L"먼저 3D 뷰에서 움직일 객체를 선택하세요"
		        : added       ? L"대상 " + std::to_wstring(added) + L"개 추가"
		                      : L"추가 실패: " + CadLastError();
	});
	p.AddButton(L"대상 모두 지우기", [st] { CAD_NavClearAgents(); st->msg = L"대상을 모두 지웠습니다"; });

	p.AddSection(L"3. 주행");
	p.AddButton(L"목적지 찍기 — 3D 뷰 클릭", [this, st] {
		// 엔진에 점 하나를 받아 달라고 하고, 결과는 onTick 에서 확인한다 (안내문은 명령행에 뜬다)
		CAD_BeginGetPoint(u8"내비 목적지: 바닥을 클릭  (ESC 취소)");   // u8 — 엔진은 UTF-8 을 받는다
		st->picking = true;
		st->msg = L"3D 뷰에서 목적지를 클릭하세요";
		m_view.SetFocus();
	});
	p.AddButton(L"출발", [st] {
		if (CAD_NavStart(0)) { st->msg = L"출발"; return; }
		const std::wstring why = CadLastError();   // 엔진이 이유를 안 남길 때도 있다 — 그땐 흔한 원인을 알려 준다
		st->msg = L"출발 실패: " + (why.empty() ? L"목적지를 먼저 찍었는지 확인하세요" : why);
	});
	p.AddButton(L"멈춤",        [st] { CAD_NavStop(0);  st->msg = L"멈춤"; });
	p.AddButton(L"처음 자리로", [st] { CAD_NavReset(0); st->msg = L"처음 자리로 돌려놓았습니다"; });
	p.AddSlider(L"속도 (장면 단위/초)", 0.1f, 10, 2, 1, [](float v) {
		char json[64];
		sprintf_s(json, "{\"speed\":%.2f}", v);   // 준 키만 바뀐다
		CAD_NavSetAgentParams(0, json);
	});
	p.AddToggle(L"탐지 광선 보이기", false, [](bool v) {
		CAD_NavSetAgentParams(0, v ? "{\"showRays\":true}" : "{\"showRays\":false}");
	});
	p.AddStatus(5);

	p.onTick = [this, st] {
		if (st->picking) {
			float x, y, z;
			uint32_t id;
			if (CAD_TryGetPickResult(&x, &y, &z, &id)) {
				st->picking = false;
				st->msg = CAD_NavSetGoal(0, x, y, z) ? Format(L"목적지 (%.1f, %.1f) — 출발을 누르세요", x, y)
				                                     : L"길 없음: " + CadLastError();
			} else if (!CAD_IsPicking()) {
				st->picking = false;
				st->msg = L"목적지 찍기 취소";
			}
		}
		// {"grid":{"ready",…},"agents":[{"agentId","driving","arrived",…}]}
		const std::string j = CadReadJson(CAD_NavGetStateJson);
		m_panel.SetStatus(st->msg + L"\n\n지도: " + (JsonBool(j, "ready") ? L"준비됨" : L"없음") +
		                  L" · 대상 " + std::to_wstring(JsonCount(j, "\"agentId\"")) + L"개\n주행 중 " +
		                  std::to_wstring(JsonCount(j, "\"driving\":true")) + L" · 도착 " +
		                  std::to_wstring(JsonCount(j, "\"arrived\":true")));
	};
	p.onClose = [this, st] {
		if (st->picking) CAD_CancelPick();   // 대상·지도는 장면 상태라 남긴다
		RelayoutNow();
	};
}


// ── 파티클 ──────────────────────────────────────────────────────────────
// 불·연기·불꽃 프리셋(+ effects/*.json 라이브러리)으로 방출기를 만들고 재생한다.
// 재생·설정 대상 = 3D 뷰에서 선택한 방출기, 없으면 이 패널에서 만든 것 전부.

void CLotCADMFCDlg::OpenParticlePanel()
{
	struct State {
		int   kind = 0;                        // 0~2 내장(불·연기·불꽃), 3~ 라이브러리
		float unitsPerMeter = 1;               // m 장면 1, mm 도면 1000
		bool  picking = false;
		std::vector<std::string> presetPaths;  // 라이브러리 프리셋 경로 (kind 3~)
		std::vector<uint32_t>    made;         // 이 패널에서 만든 방출기
		std::wstring msg = L"종류를 고르고 '위치 찍어 만들기' 로 놓으세요";

		std::vector<uint32_t> Targets()
		{
			std::vector<uint32_t> out;
			for (uint32_t id : SelectedIds())
				if (CAD_GetObjectKind(id) == 8) out.push_back(id);   // 8 = ParticleEmitter
			if (out.empty())
				for (uint32_t id : made) if (CAD_ObjectExists(id)) out.push_back(id);   // 지운 것은 건너뛴다
			return out;
		}
		void SetAll(const char* json)
		{
			for (uint32_t id : Targets())
				if (!CAD_SetParticleSettingsJson(id, json)) { msg = L"설정 실패: " + CadLastError(); return; }
		}
	};
	auto st = std::make_shared<State>();

	std::vector<std::wstring> kinds = { L"불", L"연기", L"불꽃" };
	for (uint32_t i = 0; i < CAD_GetParticlePresetCount(); ++i) {   // effects/*.json — 이름순
		char name[256] = {}, path[1024] = {};
		if (CAD_GetParticlePresetName(i, name, sizeof(name)) < 0 || CAD_GetParticlePresetPath(i, path, sizeof(path)) < 0) continue;
		kinds.push_back(L"라이브러리: " + Utf8ToWide(name));
		st->presetPaths.push_back(path);
	}

	CToolPanel& p = m_panel;
	p.Open(L"파티클 — 불·연기·불꽃", kPanelParticles);

	p.AddSection(L"만들기");
	p.AddChoice(L"종류", kinds, 0, [st](int i) { st->kind = i; });
	p.AddChoice(L"장면 단위", { L"m  (1 m = 1)", L"mm  (1 m = 1000)" }, 0,
	            [st](int i) { st->unitsPerMeter = i == 0 ? 1.0f : 1000.0f; });
	p.AddButton(L"위치 찍어 만들기 — 3D 뷰 클릭", [this, st] {
		CAD_BeginGetPoint(u8"파티클 위치: 클릭  (ESC 취소)");
		st->picking = true;
		st->msg = L"3D 뷰에서 위치를 클릭하세요";
		m_view.SetFocus();
	});

	p.AddSection(L"재생");
	p.AddNote(L"대상: 선택한 방출기, 없으면 여기서 만든 것");
	// state: 0 일시정지(입자 유지) · 1 재생 · 2 리셋(정지)
	p.AddButton(L"재생",       [st] { for (uint32_t id : st->Targets()) CAD_SetParticlePlayback(id, 1); });
	p.AddButton(L"일시정지",   [st] { for (uint32_t id : st->Targets()) CAD_SetParticlePlayback(id, 0); });
	p.AddButton(L"처음부터",   [st] { for (uint32_t id : st->Targets()) CAD_SetParticlePlayback(id, 2); });

	p.AddSection(L"설정");
	p.AddNote(L"같은 대상에 — 움직인 값만 바뀐다");
	p.AddSlider(L"초당 입자 수", 10, 600, 120, 0, [st](float v) {
		char json[64]; sprintf_s(json, "{\"rate\":%.0f}", v); st->SetAll(json);
	});
	p.AddSlider(L"속도", 0.1f, 5, 1, 2, [st](float v) {
		char json[64]; sprintf_s(json, "{\"speed\":%.2f}", v); st->SetAll(json);
	});
	p.AddSlider(L"입자 크기", 0.02f, 1, 0.2f, 2, [st](float v) {
		char json[64]; sprintf_s(json, "{\"size\":%.3f}", v); st->SetAll(json);
	});
	p.AddStatus(4);

	p.onTick = [this, st] {
		if (st->picking) {
			float x, y, z;
			uint32_t hit;
			if (CAD_TryGetPickResult(&x, &y, &z, &hit)) {
				st->picking = false;
				// 내장은 번호로, 라이브러리는 프리셋 파일 경로로 만든다. 처음엔 정지 상태라 바로 재생한다.
				const uint32_t id = st->kind < 3
					? CAD_CreateParticleEmitter(st->kind, x, y, z, st->unitsPerMeter)
					: CAD_CreateParticleEmitterFromPreset(st->presetPaths[st->kind - 3].c_str(), x, y, z, st->unitsPerMeter);
				if (id) {
					CAD_SetParticlePlayback(id, 1);
					st->made.push_back(id);
					st->msg = Format(L"방출기를 (%.1f, %.1f, %.1f) 에 만들었습니다", x, y, z);
				} else {
					st->msg = L"만들기 실패: " + CadLastError();
				}
			} else if (!CAD_IsPicking()) {
				st->picking = false;
				st->msg = L"위치 찍기 취소";
			}
		}
		const std::vector<uint32_t> targets = st->Targets();
		std::wstring live;
		for (size_t i = 0; i < targets.size() && i < 6; ++i)
			live += (i ? L" · " : L"") + std::to_wstring(CAD_GetParticleLiveCount(targets[i]));
		m_panel.SetStatus(st->msg + L"\n\n대상 방출기 " + std::to_wstring(targets.size()) + L"개" +
		                  (live.empty() ? L"" : L"\n살아 있는 입자: " + live));
	};
	p.onClose = [this, st] {
		if (st->picking) CAD_CancelPick();   // 방출기는 장면 객체라 남는다 (.lot 에 저장됨)
		RelayoutNow();
	};
}
