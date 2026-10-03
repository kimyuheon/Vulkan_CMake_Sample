# 엔진 C API 요청서 — 기능 샘플용

요청: 샘플 레포(3dEngine_Sample) 세션 · 2026-10-03
대상: 엔진 레포(3dEngine) `api/VulkanCAD_API.h` / `api/VulkanCAD_API.cpp`

## 배경

샘플 레포에 기능별 샘플(로봇 팔 · 파티클 · 3D 평면도 · Jig · 단면도/도면 뷰 · 내비 · AI)을
**모든 호스트**(MFC · MFC 대화상자 · WPF · WinForms · Qt · macOS Swift · iOS · Android)로 만든다.
기능 로직은 `sample/shared/<기능>_demo.{h,cpp}` 한 벌(C API 만 호출)에 두고 각 호스트는 화면만 그린다
(로봇 팔이 이미 이 구조 — `sample/shared/robot_demo.cpp`).

지금은 여러 기능이 **명령어(`CAD_ExecuteCommand`)나 ImGui 패널로만** 닿는다. 샘플이 막히는 이유:
- 값을 숫자로 지정하거나 결과·상태를 **읽어 올 수 없다** (슬라이더·목록 UI 를 못 만든다).
- ImGui 패널은 iOS(`LOT_NO_IMGUI`)·임베드 호스트에서 **아예 안 뜬다** → 단면·내비·파티클 패널 기능이 모바일에서 불가.
- 일부 명령은 **마우스 클릭**을 기다린다(`navgo`, 인자 없는 `viewsection`).

아래 함수 이름·인자는 제안이다. 내부 구조에 맞게 바꿔도 되지만 **아래 공통 규칙**은 지켜 주면 좋겠다.

## 공통 규칙 (기존 관례 그대로)

| 항목 | 규칙 |
|------|------|
| 실패 | id/개수 반환은 `0`, bool 은 `false`, 이유는 `CAD_GetLastError`(공용 `g_lastError`, cpp:2749) |
| 문자열 출력 | `int Fn(..., char* out, int cap)` — 전체 길이 반환, `out=NULL` 이면 길이만, 실패 -1 (`copyOutUtf8`) |
| 배열 출력 | `uint32_t Fn(T* out, uint32_t cap)` — NULL/0 이면 개수만(2회 호출 규약) |
| 복잡한 상태 | JSON 문자열 입출력 (`CAD_GetObjectJson`·`CAD_QueryObjects` 처럼). C#·Swift·Kotlin 모두 구조체 마샬링 없이 쓸 수 있다 |
| 구조체 | 꼭 필요하면 blittable(POD, 고정 크기)만 — WPF/WinForms P/Invoke |
| 스레드 | 엔진 스레드에서, `CAD_Tick` 바깥에서 호출 (기존 Create*/Set* 와 같음) |
| ⭐ 다시 그리기 | 장면·상태를 바꾸는 setter 는 전부 `requestRedraw()`(first_app.h:1348) — iOS render-on-demand·데스크톱 유휴 대기에서 화면이 안 따라온다. 로봇 관절 setter 에 넣은 것과 같은 방식 |
| ImGui 무관 | 패널 객체가 상태를 들고 있어도(예: `LotSectionDialog`) API 는 iOS 에서 동작해야 한다 |
| undo | 사용자가 되돌리고 싶을 변경(뷰 생성·파티클 설정 등)은 기존 Command 로 기록 |
| 단위 | 기존 API 와 맞춘다. 섞이면(평면도 m ↔ 이미지 mm) 주석에 명시 |
| 빌드 | 새 소스 폴더가 생기면 iOS `ios_stubs.cpp`·Android 샘플 CMake GLOB 영향 — 시그니처 바꾼 스텁은 같이 갱신 |

---

## 1. 파티클 (우선순위 P1 — 샘플 바로 착수 가능)

현재: 생성(`CAD_CreateParticleEmitter`·`FromPreset`)·재생(`CAD_SetParticlePlayback`)만. 설정값을 못 읽고 못 바꾸고, 프리셋 목록도 없다
(라이브러리 `prepareParticleLibrary` particle.cpp:162 는 패널이 보일 때만 로드).

```c
/* 설정 — particle_preset_io.h 의 JSON 스키마 v1 그대로 (rate, lifetime, speed, radius, spread, size,
 * acceleration, start_color, end_color, capacity, seed, style, unit). 부분 JSON 이면 그 키만 바꾼다. undo 1단계. */
CAD_API int      CAD_GetParticleSettingsJson(uint32_t id, char* out, int cap);
CAD_API bool     CAD_SetParticleSettingsJson(uint32_t id, const char* json);
CAD_API uint32_t CAD_GetParticleLiveCount(uint32_t id);

/* 프리셋 라이브러리 — effects/*.json (+ effects/external/*.efk*). 패널과 무관하게 로드. */
CAD_API uint32_t CAD_GetParticlePresetCount(void);
CAD_API int      CAD_GetParticlePresetName(uint32_t index, char* out, int cap);   /* 표시 이름 */
CAD_API int      CAD_GetParticlePresetPath(uint32_t index, char* out, int cap);   /* CreateParticleEmitterFromPreset 에 넣을 경로 */
CAD_API bool     CAD_SaveParticlePreset(uint32_t id, const char* path);
```
- `CAD_GetObjectJson` 에도 파티클 설정을 넣어 주면 좋다(지금 lot_object_json.cpp:163 에서 건너뜀).

샘플 사용 예 (shared/particle_demo):
```c
uint32_t n = CAD_GetParticlePresetCount();            // 목록 UI
char path[512]; CAD_GetParticlePresetPath(0, path, sizeof path);
uint32_t fx = CAD_CreateParticleEmitterFromPreset(path, 0,0,0, 1.0f);
CAD_SetParticlePlayback(fx, 1);
CAD_SetParticleSettingsJson(fx, "{\"rate\":400,\"start_color\":[1,0.4,0.1]}");   // 슬라이더
```

## 2. Jig (P1)

현재: `CAD_JigBegin*`·`CAD_JigState`·`CAD_JigCancel`·변형(variant)은 있다. 끌기·확정이 마우스(`TransformTool`)에만 묶여 있다.
모바일(가상 커서 없이 버튼으로)·자동 시연에서 쓰려면:

```c
CAD_API bool CAD_JigMoveTo(float x, float y, float z);    /* 현재 지그를 월드 점으로 (OSnap 미적용) */
CAD_API bool CAD_JigCommit(void);                         /* 왼쪽 클릭과 같은 확정 */
CAD_API bool CAD_JigSetBasePoint(float x, float y, float z); /* 이동/복사 기준점 — 지금은 늘 bbox 중심 */
/* 현재 미리보기 변환 — 이동량·회전각(도)·배율. 지그 아닐 때 false */
CAD_API bool CAD_JigGetDelta(float* dx, float* dy, float* dz, float* angleDeg, float* scale);
```
- 상태 변화(확정·취소·변형 바뀜)는 `CAD_AddEventListener` 종류로 하나 추가해 주면 폴링이 필요 없다(선택).

샘플 사용 예:
```c
uint32_t id = CAD_CreateBox(0,0,0, 1,1,1);
CAD_JigBeginOne(0 /*move*/, id, 0,0,0);
CAD_JigMoveTo(2, 1, 0);           // 화면 버튼·조이스틱으로
CAD_JigCommit();
```

## 3. 3D 평면도 — 사진/이미지 → 벽 (P2)

현재: 직사각형 방(`CAD_CreateRoom`·`CAD_CreateFloorplan`·`WithOpenings`, 단위 m)은 있다.
이미지 → 벽 파이프라인(`floorplan_image.h` `loadFloorplanImage`·`extractFloorplanWalls`)과 벽 그래프 생성
(`FirstApp::createWallGraph` first_app.h:1158)은 **AI 워커 안에서만** 돈다. 입력도 파일 경로뿐.

```c
/* 이미지에서 벽만 뽑는다(장면 변경 없음). widthMm=0 이면 문 폭(900mm)으로 축척 추정.
 * 출력 JSON: {"widthMm":..,"depthMm":..,"walls":[{"x1":..,"y1":..,"x2":..,"y2":..,"thicknessMm":..,"exterior":true}],"summary":".."} */
CAD_API int  CAD_FloorplanExtractFromImage(const char* path, float widthMm, char* outJson, int cap);
CAD_API int  CAD_FloorplanExtractFromRgba(const unsigned char* rgba, int w, int h, float widthMm,
                                          char* outJson, int cap);   /* 모바일 카메라·사진 선택기용 */
/* 벽 그래프 → 3D 벽. segs = [x1,y1,x2,y2,thickness] × count (단위 명시 — 기존 평면도 API 와 같은 m 권장).
 * thickness<=0 이면 defaultThickness. 반환 = 만든 벽 수, outIds 에 최대 cap 개. undo 1단계. */
CAD_API uint32_t CAD_CreateWallGraph(float ox, float oy, float oz, const float* segs, uint32_t count,
                                     float height, float defaultThickness, uint32_t* outIds, uint32_t cap);
```
- 편의 함수(선택): `CAD_CreateFloorplanFromImage(path, widthMm, height, bool underlayImage, outIds, cap)` —
  AI 의 `create_wall_graph` 처리(lot_ai_actions.cpp:634)처럼 원본 이미지를 바닥에 깔기까지.

샘플 사용 예:
```c
char json[65536];
CAD_FloorplanExtractFromRgba(rgba, w, h, 0, json, sizeof json);   // 미리보기·벽 수 표시
// 사용자가 확인 → 호스트가 JSON 의 walls 를 segs 로 바꿔
CAD_CreateWallGraph(0,0,0, segs, n, 2.4f, 0.1f, ids, cap);
```

## 4. 단면도 / 도면 뷰 (P2)

현재: `section` 명령은 ImGui 패널을 띄울 뿐(command.cpp:689). 상태 `SectionState`(lot_section.h:19)는
`LotSectionDialog` 안에 있어 밖에서 못 바꾼다 — iOS 는 패널이 없어 **단면을 켤 방법 자체가 없다**.
도면 뷰(`createDrawingViews`·`createSectionView`·`createDetailView`, first_app.h:1385-1414)는 public 이지만 C API 가 없다.

```c
/* 실시간 단면 (축 정렬 v1 — lot_section.h:10 그대로) */
CAD_API bool CAD_SetSection(int mode /*0 off 1 plane 2 slice 3 box*/, int axis /*0 X 1 Y 2 Z*/,
                            float pos, bool flip, float thickness /*slice*/);
CAD_API bool CAD_SetSectionBox(float minX, float minY, float minZ, float maxX, float maxY, float maxZ);
CAD_API bool CAD_SetSectionOptions(bool showArrows, bool selectedOnly);
CAD_API int  CAD_GetSectionStateJson(char* out, int cap);      /* mode, axis, pos, flip, thickness, box, 모델 범위 */
/* 현재 단면 → 2D 윤곽 객체(beginSection2DExtract 와 같음). 반환 = 만든 객체 수 */
CAD_API uint32_t CAD_ExtractSectionTo2D(bool includeBehind);

/* 도면 뷰 — 장면 객체가 아니어서 CAD_GetObjectIds 에 안 나온다 → 목록 API 필요 */
CAD_API uint32_t CAD_CreateDrawingViews(const uint32_t* ids, uint32_t count, float scale); /* 반환 = 뷰 수 */
CAD_API uint32_t CAD_CreateSectionView(float ax, float ay, float bx, float by);           /* 0 = 실패 */
CAD_API uint32_t CAD_CreateDetailView(float cx, float cy, float radius, float factor);
CAD_API bool     CAD_MoveDrawingView(uint32_t viewId, float dx, float dy);
CAD_API void     CAD_ClearDrawingViews(void);
CAD_API int      CAD_GetDrawingViewsJson(char* out, int cap);   /* DrawingViewDef 요약 + bounds */
```

샘플 사용 예:
```c
CAD_SetSection(1 /*plane*/, 0 /*X*/, 0.25f, false, 0);   // 슬라이더로 pos 이동
CAD_ExtractSectionTo2D(false);
uint32_t ids[] = { part };
CAD_CreateDrawingViews(ids, 1, 0.5f);                     // 3각법 기본 뷰
CAD_CreateSectionView(-50, 0, 50, 0);                     // A-A 단면
```

## 5. 내비(경로 주행) (P2)

현재: 내비 메서드·상태가 전부 `private`(first_app.h:2155-2460, 테스트만 `NavTestAccess` friend 로 접근).
명령(`navmap`·`navrun`·`navstop` …)은 있지만 `navgo` 는 마우스 클릭, 에이전트별 시작/정지는 패널 버튼에만 있다.
마우스 없는 본체 `applyNavGoal(vec3)`(nav.cpp:1076)이 이미 있으니 감싸면 된다.

```c
/* 격자 — paramsJson: NavGridParams(cellSize, agentRadius, minZ, maxZ, margin, maxCells). NULL = 기본값 */
CAD_API bool     CAD_NavBuildMap(const char* paramsJson);
CAD_API void     CAD_NavShowGrid(bool on);

/* 에이전트 — 장면 객체를 주행 대상으로 */
CAD_API uint32_t CAD_NavAddAgent(uint32_t objectId, bool isVehicle);   /* 반환 agentId, 0 실패 */
CAD_API uint32_t CAD_NavAddSelected(void);                              /* 반환 = 추가 수 */
CAD_API bool     CAD_NavRemoveAgent(uint32_t agentId);
CAD_API void     CAD_NavClearAgents(void);
/* speed, scanRange, radius(0=auto), showRays, checked */
CAD_API bool     CAD_NavSetAgentParams(uint32_t agentId, const char* json);

/* 목표·주행 — agentId 0 = 체크된 에이전트 전부(없으면 활성 행). 패널 규약과 같게 */
CAD_API bool     CAD_NavSetGoal(uint32_t agentId, float x, float y, float z);  /* 경로 계획까지, 출발은 안 함 */
CAD_API bool     CAD_NavStart(uint32_t agentId);
CAD_API bool     CAD_NavStop(uint32_t agentId);
CAD_API bool     CAD_NavReset(uint32_t agentId);

/* 상태 — LotNavPanel::View·AgentRow(lot_nav_panel.h:18-62) 를 그대로 JSON 으로 +
 * 에이전트별 위치·yaw(0=+Y 시계, CAD_ApplyExternalPose 와 같은 규약)·경로 길이·도착 여부 */
CAD_API int      CAD_NavGetStateJson(char* out, int cap);
CAD_API uint32_t CAD_NavGetPath(uint32_t agentId, float* outXY, uint32_t capPoints);   /* 2회 호출 */
```
- 도착 이벤트를 `CAD_AddEventListener` 종류로 주면 좋다(선택 — 없으면 상태 JSON 폴링).

샘플 사용 예:
```c
CAD_NavBuildMap(NULL);
uint32_t a = CAD_NavAddAgent(robotId, false);
CAD_NavSetGoal(a, 5.0f, 3.0f, 0);    // 화면 탭 위치 → CAD_BeginGetPoint 로 얻은 점
CAD_NavStart(a);
// 매 프레임: CAD_NavGetStateJson → 진행 표시
```

## 6. AI (P3)

현재: 프롬프트 입력은 명령행 `submitCommandLine` 실패 시 폴백(command.cpp:497)과 AI 창뿐. 로컬 llama-server
(127.0.0.1:8080)를 `LotAILauncher` 가 posix_spawn/Job 으로 띄운다(모바일 불가). MCP 도구는 TCP link 로만.
⚠️ `CAD_ExecuteCommand` 주석(VulkanCAD_API.cpp:530)은 AI 폴백이 된다고 하지만 실제로는 안 된다 — 주석 정정 필요.

```c
/* 엔드포인트 — 모바일은 원격 서버. url 예 "http://192.168.0.10:8080", model 은 서버가 쓰는 이름(NULL=기본) */
CAD_API bool CAD_AiSetEndpoint(const char* url, const char* model);
CAD_API bool CAD_AiStartLocalServer(void);        /* 데스크톱 전용, 모바일 false + GetLastError */

/* 비동기 요청 — 결과 액션은 다음 틱들에서 트랜잭션 하나로 적용(지금 processAIActions 그대로) */
CAD_API bool CAD_AiSubmit(const char* prompt);
CAD_API bool CAD_AiSubmitWithImage(const char* prompt, const char* imagePath);
CAD_API int  CAD_AiGetStatus(void);               /* 0 idle 1 busy 2 done 3 error (2·3 은 한 번만) */
CAD_API int  CAD_AiGetLastAnswer(char* out, int cap);
CAD_API int  CAD_AiGetNotes(char* out, int cap);  /* 액션별 결과 노트 */
CAD_API void CAD_AiCancel(void);

/* 동기 — 호스트가 자기 LLM(Claude API 등)을 직접 부르고, 받은 액션 JSON 만 엔진에 넘기는 경로.
 * MCP ai_actions(mcp_tools.cpp:215) 와 같은 처리. 반환 = notes 길이 */
CAD_API int  CAD_RunAiActionsJson(const char* actionsJson, char* outNotes, int cap);
/* MCP 도구를 TCP 없이 직접 — CAD_GetMcpToolsJson 짝 */
CAD_API int  CAD_McpCall(const char* toolName, const char* argsJson, char* out, int cap);
```

샘플 사용 예:
```c
CAD_AiSetEndpoint("http://127.0.0.1:8080", NULL);
CAD_AiSubmit("가로 4m 세로 3m 방에 문 하나");
// 매 프레임: if (CAD_AiGetStatus() == 2) CAD_AiGetLastAnswer(buf, cap);
// 또는 호스트 LLM 경로:
CAD_RunAiActionsJson("[{\"type\":\"create_room\",\"width\":4,\"depth\":3}]", notes, cap);
```

## 7. 로봇 팔 — 추가 요청 없음

`CAD_LoadUrdf`·`CAD_SetJointValues` 로 충분(관절 setter 의 `requestRedraw` 는 반영됨, a4b8f98).

---

## 우선순위 / 전달 방식

| 순서 | 기능 | 이유 |
|------|------|------|
| P1 | 파티클, Jig | 작은 래퍼 — 샘플 바로 착수 |
| P2 | 평면도 이미지, 단면도/도면 뷰, 내비 | 내부 함수는 있음, private·패널에 묶인 것을 꺼내는 작업 |
| P3 | AI | 엔드포인트·비동기 상태·모바일 범위 정리 필요 |

- 기능 하나씩 끝날 때마다: 헤더 → `sdk/include/VulkanCAD_API.h` 복사, macOS dylib·iOS lib(`build_ios.sh`) 갱신,
  가능하면 `tests/capi_*_e2e.cpp` 에 호출 시험 한 개.
- 시그니처를 바꾸면 샘플 세션에 알려 주면 된다 — 샘플은 C API 만 쓴다.
