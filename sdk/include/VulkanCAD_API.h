#pragma once

// VulkanCAD C API boundary rules:
// - Expose only C-friendly types: bool, int, uint32_t, float, double, const char*, void* handles.
// - Do not expose C++/STL/GLM/GLFW/Vulkan engine types.
// - Do not return internal object pointers.
// - Do not access FirstApp private members from the API implementation.
// - Route mutating commands through FirstApp public request methods.

#include <stdint.h>

#ifndef __cplusplus
#include <stdbool.h>
#endif

#if defined(_WIN32)
    #if defined(VULKANCAD_API_STATIC)
        #define CAD_API
    #elif defined(VULKANCAD_API_EXPORTS)
        #define CAD_API __declspec(dllexport)
    #else
        #define CAD_API __declspec(dllimport)
    #endif
#else
    #define CAD_API __attribute__((visibility("default")))
#endif

#ifdef __cplusplus
extern "C" {
#endif

CAD_API bool CAD_CreateEngine(void);
CAD_API void CAD_DestroyEngine(void);
CAD_API bool CAD_Tick(void);
CAD_API bool CAD_ShouldClose(void);
CAD_API void CAD_SetIgnoreCloseRequest(bool ignore);
CAD_API bool CAD_SetRuntimeAssetPath(const char* path);
CAD_API void CAD_AttachView(void* nativeHandle, int width, int height);
CAD_API void CAD_ResizeView(int width, int height);
CAD_API void CAD_DetachView(void);

CAD_API void CAD_OnMouseDown(int button, double x, double y, int modifiers);
CAD_API void CAD_OnMouseUp(int button, double x, double y, int modifiers);
CAD_API void CAD_OnMouseMove(double x, double y);
CAD_API void CAD_OnMouseWheel(double x, double y, double deltaX, double deltaY);
CAD_API void CAD_OnKeyDown(int key, int modifiers);
CAD_API void CAD_OnKeyUp(int key, int modifiers);

// ── Win32 호스트(MFC 등) 편의용 키 입력 ──
// 엔진 단축키는 GLFW 키코드 기준인데 Win32 는 VK_* 코드를 준다. WM_KEYDOWN/WM_KEYUP 의
// nChar(VK 코드)를 그대로 넘기면 내부에서 GLFW 키코드로 변환 + Ctrl/Shift/Alt 수정자를
// GetKeyState 로 자동 감지해 라우팅한다. (Windows 전용 — 호스트는 변환 신경 안 써도 됨)
#if defined(_WIN32)
CAD_API void CAD_OnKeyDownVK(int vkCode);
CAD_API void CAD_OnKeyUpVK(int vkCode);
#endif

// 터치 입력 라우팅: (x,y) 에 Transform Gizmo 핸들이 있으면 1(true), 없으면 0.
// 호출자(예: iOS)가 1손가락 드래그 시작 시 호출해 기즈모 조작(left) vs 궤도회전(right) 결정.
CAD_API int  CAD_GizmoHitTest(double x, double y);
// 기즈모 핸들 hit 허용범위를 터치용으로 확대 (앱 시작 시 한 번 호출).
CAD_API void CAD_SetGizmoTouchMode(bool enabled);
// OSnap(끝점/중점/중심…) 허용범위를 터치용으로 확대 — 12px → 약 42px.
// 손가락 접촉 크기(44pt)에 맞춘 값. 기즈모와 마찬가지로 앱 시작 시 한 번 호출.
CAD_API void CAD_SetSnapTouchMode(bool enabled);

// 치수 **문자**를 줌과 무관하게 화면상 같은 크기로 그릴지 (기본 false).
// 모바일에서 치수는 도면 요소보다 "측정 도구" 에 가깝다 — 줌아웃하면 글자가 같이 작아져
// 값을 못 읽는 게 문제였다. 켜면 글자가 항상 읽히는 크기로 남는다(선·화살표는 그대로).
// 데스크톱은 끄는 게 맞다 — 도면 출력에선 월드 크기여야 한다.
// 매 프레임 비용은 없다(플래그를 정점에 굽는다). 전환 시 기존 치수를 한 번 다시 만든다.
CAD_API void CAD_SetDimensionTextScreenFixed(bool enabled);
CAD_API bool CAD_GetDimensionTextScreenFixed(void);
// 진행 중인 치수 도구의 점 개수. 비활성이면 -1.
// 모바일 커서 UI 의 [선택] 버튼 문구/종료 판단에 쓴다 — 클라이언트가 클릭을 세면
// 엔진이 클릭을 거부하는 경우와 어긋나므로 반드시 엔진 값을 본다.
CAD_API int  CAD_GetDimensionPointCount(void);
// 진행 중인 도구의 안내 문구("첫 번째 점을 지정" 등). 도구가 없으면 빈 문자열.
// 모바일 커서 UI 가 **도구 종류와 무관하게** 활성 여부/안내를 알기 위해 매 틱 폴링한다.
// 반환: 복사한 길이(널 제외). buf 가 작으면 잘라서 복사한다.
CAD_API int  CAD_GetPrompt(char* buf, int bufLen);

CAD_API void CAD_RequestStartBoxSketch(void);
CAD_API void CAD_RequestStartLineSketch(void);
CAD_API void CAD_RequestStartRectangleSketch(void);
CAD_API void CAD_RequestStartCircleSketch(void);
CAD_API void CAD_RequestStartPolygonSketch(void);
CAD_API void CAD_RequestCyclePolygonSides(void);
/* ── 명령 실행 ────────────────────────────────────────────────────────────
 *
 * 엔진의 **모든 명령**을 이름으로 실행한다. 아래 CAD_RequestXxx 들은 자주 쓰는 것만
 * 개별 함수로 뽑아 둔 것이고, 엔진에는 명령이 200개가 넘는다(trim / offset / fillet /
 * ocr / navmap / imageattach …). 이 함수 하나면 그 전부가 열리고, 새 명령을 추가할
 * 때마다 API 를 늘리지 않아도 된다.
 *
 *   CAD_ExecuteCommand("trim");        // 자르기
 *   CAD_ExecuteCommand("offset");      // 오프셋
 *   CAD_ExecuteCommand("2,3");         // 진행 중인 도구에 좌표/값 전달
 *
 * 이름은 하단 명령행에 치는 것과 **똑같다**(영문·짧은 별칭·한글 모두).
 * 진행 중인 도구가 있으면 값 입력으로 전달된다 — 명령행과 완전히 같은 경로.
 * 반환: 알 수 없는 이름이면 false. 값 입력으로 소비된 경우도 true.
 */
CAD_API bool CAD_ExecuteCommand(const char* name);

/* ── 알림(콜백) ───────────────────────────────────────────────────────────
 *
 * 엔진 → 호스트 UI 방향의 알림. 이게 없으면 호스트는 **매 프레임 물어보는 수밖에**
 * 없다(선택이 바뀌었는지 알려고 GetSelectedCount 를 계속 호출하는 식).
 * 속성 패널·씬 트리·제목표시줄의 * 표시 같은 걸 붙이려면 사실상 필수다.
 * ObjectARX 의 리액터(AcDbDatabaseReactor)와 같은 역할.
 *
 * ⚠️ 호출 시점은 **CAD_Tick 안**이다 — 프레임당 한 번, 엔진과 같은 스레드.
 *    그래서 콜백 안에서 다른 CAD_* 를 불러도 안전하다.
 *    변화가 **한 프레임에 여러 번** 나도 한 번으로 합쳐 알린다(UI 갱신엔 충분).
 * nullptr 를 주면 해제된다.
 */
CAD_API void CAD_SetOnSelectionChanged(void (*cb)(void));
CAD_API void CAD_SetOnObjectCreated  (void (*cb)(uint32_t id));
CAD_API void CAD_SetOnObjectDeleted  (void (*cb)(uint32_t id));
CAD_API void CAD_SetOnDocumentDirty  (void (*cb)(bool dirty));
/* 하단 안내문(명령 프롬프트/임시 메시지)이 바뀔 때. 호스트 상태바에 그대로 띄우면 된다. */
CAD_API void CAD_SetOnPrompt         (void (*cb)(const char* text));

CAD_API void CAD_RequestStartExtrude(void);
CAD_API void CAD_RequestStartArcSketch(void);
CAD_API void CAD_RequestCycleArcMode(void);
CAD_API void CAD_RequestStartPolylineSketch(void);
CAD_API void CAD_RequestOpenFileDialog(void);
CAD_API void CAD_RequestZoomExtents(void);
CAD_API void CAD_RequestSelectAll(void);
CAD_API void CAD_RequestDeleteSelected(void);
CAD_API void CAD_RequestClearAll(void);
CAD_API void CAD_RequestAddCube(void);
CAD_API void CAD_RequestStartLightPlacement(void);
CAD_API void CAD_RequestFocusSelected(void);
CAD_API void CAD_RequestToggleProjection(void);
CAD_API void CAD_RequestToggleDemoLighting(void);
CAD_API void CAD_RequestCycleMaterial(void);
CAD_API void CAD_RequestRemoveMaterial(void);
CAD_API void CAD_RequestAdjustTextureScale(float delta);
CAD_API void CAD_RequestAdjustLineWidth(float deltaPx);
// viewType: 0=Front 1=Top 2=Right 3=Isometric 4=Left 5=Bottom 6=Back
CAD_API void CAD_RequestSetView(int viewType);
CAD_API void CAD_RequestSetProjection(bool ortho);

// 임의 방향 시점 — 뷰큐브의 모서리(2면)/꼭짓점(3면)처럼 표준 6뷰가 아닌 방향에서 보기.
// dir = 타겟에서 카메라를 향하는 월드 방향(정규화 불필요). 예: (1,-1,1)=앞·우·위 꼭짓점.
CAD_API void CAD_RequestSetViewDirection(float dirX, float dirY, float dirZ);

// 뷰 회전 피벗 (viewucs) — 작업평면 UCS 와 별개로, 화면을 돌릴 때의 중심/축을 지정한다.
//   axis: -1=축 자유(피벗만 고정), 0=X 축 고정, 1=Y, 2=Z (축 고정 시 그 축 기준 턴테이블 회전)
CAD_API void CAD_RequestSetViewPivot(float x, float y, float z, int axis);
CAD_API void CAD_RequestClearViewPivot(void);
// 현재 피벗 조회 — 설정돼 있으면 true. out 인자는 null 허용.
CAD_API bool CAD_GetViewPivot(float* x, float* y, float* z, int* axis);

// ── 문서 탭 (멀티 문서) ──────────────────────────────────────────────
// 여러 파일을 동시에 열고 탭으로 전환한다. 호스트(WPF/Qt)가 **자기 탭 UI** 를 그릴 수 있도록
// 목록 조회 + 전환/열기/닫기를 모두 노출한다. 전환은 상태 swap 이라 비용이 거의 없다.
CAD_API int  CAD_GetDocumentCount(void);
CAD_API int  CAD_GetActiveDocument(void);
// 탭 라벨용 — 표시 이름(파일명 또는 "제목 없음"). 길이 반환, -1=실패. buf 는 null 허용(길이만 조회).
CAD_API int  CAD_GetDocumentTitle(int index, char* outUtf8, int cap);
CAD_API int  CAD_GetDocumentPath (int index, char* outUtf8, int cap);
// 저장되지 않은 변경이 있는지 — 탭에 * 표시하거나 닫기 확인에 사용.
CAD_API bool CAD_IsDocumentDirty(int index);
// 새 빈 문서 추가 후 활성화. 반환 = 새 문서 인덱스.
CAD_API int  CAD_NewDocument(const char* titleUtf8);
// 파일을 새 탭으로 열기. 반환 = 문서 인덱스, 실패 -1.
CAD_API int  CAD_OpenDocument(const char* pathUtf8);
CAD_API bool CAD_ActivateDocument(int index);
// 닫기 — 마지막 문서는 닫지 않고 내용을 비운다(항상 문서 1개 유지).
CAD_API bool CAD_CloseDocument(int index);

// ── Jig (오토캐드 AcEdJig 스타일) ────────────────────────────────────
// 객체 id 를 물려 시작하면 그 객체가 **커서를 따라다니고**, 좌클릭에 그 자리로 확정된다
// (undo 1스텝). ESC 로 원복. OSnap·직교 트랙킹(F8)·콘솔 값 입력이 그대로 적용된다.
//   mode: 0=이동 1=회전 2=축척 3=복사
//   회전/축척은 (px,py,pz) 가 피벗. 이동/복사는 무시하고 선택 전체의 시각 중심을 기준점으로 잡는다.
// 반환 false = 유효한 객체가 없어 시작 못 함.
CAD_API bool CAD_JigBegin(int mode, const uint32_t* ids, int count,
                          float px, float py, float pz);
CAD_API bool CAD_JigBeginOne(int mode, uint32_t id, float px, float py, float pz);
// 상태 폴링 — 0=비활성 1=끌는 중 2=직전 확정됨 3=직전 취소됨 (2/3 은 1회만 반환).
CAD_API int  CAD_JigState(void);
CAD_API void CAD_JigCancel(void);

// ── 변형(variant) Jig — 끌면서 Tab 으로 **형상 자체를 교체** ──────────
// 후보 객체들을 미리 만들어 id 배열로 넘기면, 첫 후보가 커서를 따라오고 **Tab** 마다 다음 후보로
// 갈아끼워진다(문 좌/우열림, 볼트 규격 등). 비활성 후보는 씬에서 잠시 빠져 보이지 않는다.
//   확정 → 선택된 후보만 남고 나머지는 삭제 (AcEdJig 의 "성공 시에만 남긴다" 규약)
//   취소 → 후보 전부 삭제 (임시 형상이라 씬에 남기지 않음)
// 콜백이 필요 없어 C#/Qt 어디서든 그대로 쓸 수 있다.
CAD_API bool CAD_JigBeginVariants(int mode, const uint32_t* ids, int count,
                                  float px, float py, float pz);
CAD_API bool CAD_JigNextVariant(void);   // Tab 과 동일 (호스트 버튼으로 전환할 때)
CAD_API int  CAD_JigVariantIndex(void);  // 현재 후보 번호, -1 = 변형 Jig 아님
CAD_API uint32_t CAD_JigVariantId(void); // 지금 끌고 있는 후보 id (없으면 0)
CAD_API void CAD_RequestMoveSelected(float dirX, float dirY, float dirZ, float distance);
CAD_API void CAD_RequestRotateSelected(float axisX, float axisY, float axisZ, float angleDeg);
CAD_API void CAD_RequestScaleSelected(float factor);
CAD_API void CAD_RequestCopySelected(float dirX, float dirY, float dirZ,
                                     float distance, int count);
CAD_API void CAD_RequestColorSelected(float r, float g, float b);

CAD_API uint32_t CAD_CreateBox(float x, float y, float z,
                               float sx, float sy, float sz);

// origin = floor center; width/depth = interior clear size; all values are metres.
// outWallIds must point to storage for 4 ids. Order: south, north, west, east.
CAD_API bool CAD_CreateRoom(float originX, float originY, float originZ,
                            float width, float depth, float height,
                            float wallThickness, uint32_t* outWallIds);

// rectBounds contains spaceCount groups of [minX,minY,maxX,maxY], local to origin.
// Returns the total number of unique walls created and copies up to outCapacity ids.
CAD_API uint32_t CAD_CreateFloorplan(float originX, float originY, float originZ,
                                     const float* rectBounds, uint32_t spaceCount,
                                     float height, float wallThickness,
                                     uint32_t* outWallIds, uint32_t outCapacity);

// type: 0=door, 1=window. roomBIndex >= 0 targets a shared wall;
// roomBIndex < 0 uses side (0=south, 1=north, 2=west, 3=east).
// alignment: 0=center, 1=start(west/south), 2=end(east/north).
typedef struct CAD_FloorplanOpeningDesc {
    int type;
    uint32_t roomAIndex;
    int32_t roomBIndex;
    int side;
    float width;
    float height;
    float sillHeight;
    int alignment;
    float offset;
} CAD_FloorplanOpeningDesc;

// Returns the number of solid wall pieces after openings are removed.
CAD_API uint32_t CAD_CreateFloorplanWithOpenings(
    float originX, float originY, float originZ,
    const float* rectBounds, uint32_t spaceCount,
    float height, float wallThickness,
    const CAD_FloorplanOpeningDesc* openings, uint32_t openingCount,
    uint32_t* outWallIds, uint32_t outCapacity);

// ── 문자(Text) + 편집(textedit) — 모든 프론트엔드 공통 진입점 ──
CAD_API uint32_t CAD_CreateText(float x, float y, float z,
                                const char* utf8, float heightMM);
// 선형 치수 — 측정점 p1/p2 + 치수선 위치점(dl) + 작업평면 법선(n). 반환=객체 id(0=실패).
CAD_API uint32_t CAD_CreateDimension(float p1x, float p1y, float p1z,
                                     float p2x, float p2y, float p2z,
                                     float dlx, float dly, float dlz,
                                     float nx, float ny, float nz);

CAD_API bool     CAD_BeginTextEdit(uint32_t id);
CAD_API bool     CAD_IsTextEditing(void);
CAD_API void     CAD_TextEditInsert(const char* utf8);   // 커서 위치에 삽입(여러 글자 가능)
CAD_API void     CAD_TextEditBackspace(void);
CAD_API void     CAD_TextEditDelete(void);
CAD_API void     CAD_TextEditMoveCaret(int delta);       // -1/+1 등
CAD_API void     CAD_TextEditCommit(void);               // undo 적재 + 종료
CAD_API void     CAD_TextEditCancel(void);               // 원복 + 종료
CAD_API bool     CAD_SetTextContent(uint32_t id, const char* utf8);  // 비대화형 변경(undo)
CAD_API int      CAD_GetTextContent(uint32_t id, char* outUtf8, int cap);  // 길이 반환, -1=실패

CAD_API uint32_t CAD_CreateLine(float x1, float y1, float z1,
                                float x2, float y2, float z2);
CAD_API uint32_t CAD_CreateCircle(float cx, float cy, float cz,
                                  float radius,
                                  float normalX, float normalY, float normalZ);
CAD_API uint32_t CAD_CreatePolygon(float cx, float cy, float cz,
                                   float radius,
                                   int sides,
                                   float normalX, float normalY, float normalZ,
                                   float firstVertexDirX,
                                   float firstVertexDirY,
                                   float firstVertexDirZ);
CAD_API uint32_t CAD_CreateArc(float startX, float startY, float startZ,
                               float throughX, float throughY, float throughZ,
                               float endX, float endY, float endZ);
CAD_API uint32_t CAD_CreatePolyline(const float* xyz,
                                    uint32_t pointCount,
                                    bool closed);
CAD_API uint32_t CAD_CreateRectangle(float x0, float y0, float z0,
                                     float x2, float y2, float z2);
CAD_API uint32_t CAD_CreateExtrude(uint32_t sourceId, float height);
CAD_API bool CAD_DeleteObject(uint32_t id);
CAD_API bool CAD_SelectObject(uint32_t id, bool additive);
CAD_API void CAD_ClearSelection(void);
CAD_API uint32_t CAD_GetObjectCount(void);
CAD_API bool CAD_GetPosition(uint32_t id, float* x, float* y, float* z);
CAD_API bool CAD_SetPosition(uint32_t id, float x, float y, float z);
CAD_API bool CAD_GetScale(uint32_t id, float* sx, float* sy, float* sz);
CAD_API bool CAD_SetScale(uint32_t id, float sx, float sy, float sz);
CAD_API bool CAD_GetRotationAxisAngle(uint32_t id,
                                      float* axisX, float* axisY, float* axisZ,
                                      float* angleRadians);
CAD_API bool CAD_SetRotationAxisAngle(uint32_t id,
                                      float axisX, float axisY, float axisZ,
                                      float angleRadians);

// ── 위치 + 회전을 한 호출로 (쿼터니언) ──
// 외부가 매 프레임 자세를 밀어넣는 경로 — 로봇 관절, 모션캡처, 시뮬레이터, ROS Pose.
// SetPosition + SetRotationAxisAngle 을 따로 부르면 호출이 두 배가 되고, 두 호출
// 사이에 프레임이 끼면 "옮겨졌지만 아직 안 돌아간" 한 프레임이 보인다.
//
// ⚠️ 쿼터니언 성분 순서는 **(qx, qy, qz, qw)** — ROS geometry_msgs/Quaternion 과 동일.
//    (glm::quat 생성자는 (w,x,y,z) 순이라 내부에서 바꿔 넣는다. 혼동 주의)
// 정규화는 엔진이 한다. 길이 0 / NaN 이면 대입하지 않고 false.
//
// 좌표계 참고 — 엔진은 ROS(REP-103)와 같은 Z-up 우수좌표계라 축 변환이 필요 없다.
// 다만 전방 축이 다르다: ROS +X 전방, 엔진 +Y 전방 → 필요하면 yaw 90도만 보정.
CAD_API bool CAD_SetTransform(uint32_t id,
                              float x, float y, float z,
                              float qx, float qy, float qz, float qw);
CAD_API bool CAD_GetTransform(uint32_t id,
                              float* x, float* y, float* z,
                              float* qx, float* qy, float* qz, float* qw);

// ── 관절 로봇 (URDF) ──
// URDF 는 ROS 의 로봇 정의 포맷이지만 파일 자체는 XML 이라 **ROS 없이도** 읽고 그린다.
// 좌표계도 맞는다 — URDF 와 이 엔진 둘 다 Z-up 우수좌표계·미터라 축 변환이 없다.
//
//   uint32_t r = CAD_LoadUrdf("models/demo_arm.urdf");   // 0 = 실패, 아니면 로봇 인덱스+1
//   CAD_SetJointValue(r, CAD_FindJoint(r, "joint_elbow"), 0.7f);   // 라디안
//
// 조인트는 1자유도다 — URDF <axis> 가 축이고 값은 그 축의 회전각(revolute, 라디안)
// 또는 이동거리(prismatic, 미터). 한계가 있으면 엔진이 잘라 넣는다.
//
// 로봇 핸들은 **1부터** — 0 을 실패로 쓰기 위해서다. 내부 인덱스 = handle-1.
// 지원 형상: box / cylinder / sphere. mesh(STL/DAE) 링크는 아직 건너뛴다.
CAD_API uint32_t CAD_LoadUrdf(const char* path);
CAD_API uint32_t CAD_GetRobotCount(void);
CAD_API uint32_t CAD_GetJointCount(uint32_t robot);
// 조인트 이름 → 인덱스. 없으면 -1. (ROS 는 이름으로 오므로 필요)
CAD_API int      CAD_FindJoint(uint32_t robot, const char* jointName);
CAD_API int      CAD_GetJointName(uint32_t robot, int jointIdx, char* outUtf8, int cap);
// 값 대입/조회. 반환은 한계로 잘린 **실제 적용값**.
CAD_API float    CAD_SetJointValue(uint32_t robot, int jointIdx, float value);
CAD_API float    CAD_GetJointValue(uint32_t robot, int jointIdx);
// ── 여러 관절을 한 번에 ──
// ROS 의 sensor_msgs/JointState 는 관절 전부를 한 메시지에 담아 초당 수십 번 보낸다.
// 하나씩 넣으면 관절 수만큼 계층 트리를 다시 푸느라 헛일을 한다 — 여기선 **마지막에 한 번만** 푼다.
// 반환: 실제로 적용된 개수(범위 밖·고정 조인트는 건너뜀).
CAD_API uint32_t CAD_SetJointValues(uint32_t robot, const int* jointIdx,
                                    const float* values, uint32_t count);
// 이름으로 받는 판 — ROS 는 인덱스가 아니라 이름으로 온다. JointState 콜백에 그대로 꽂힌다.
//   CAD_SetJointValuesByName(r, msg.name.data(), msg.position.data(), msg.name.size());
CAD_API uint32_t CAD_SetJointValuesByName(uint32_t robot, const char* const* names,
                                          const float* values, uint32_t count);
// 한계와 종류(0=fixed 1=revolute 2=continuous 3=prismatic). hasLimit=false 면 lower/upper 무의미.
CAD_API bool     CAD_GetJointInfo(uint32_t robot, int jointIdx,
                                  int* outType, float* outLower, float* outUpper, bool* outHasLimit);
// 축/종류 지정 — 엔진 안에서 리깅 (URDF 로 읽은 값을 바꿔 보기).
// 축은 길이 0 이면 무시하고 false. 내부에서 정규화한다.
CAD_API bool     CAD_SetJointAxis(uint32_t robot, int jointIdx, float x, float y, float z);
CAD_API bool     CAD_GetJointAxis(uint32_t robot, int jointIdx, float* x, float* y, float* z);
// type: 0=fixed 1=revolute 2=continuous 3=prismatic.
// 종류가 바뀌면 한계는 버린다 — 각도(라디안)와 길이(미터)는 단위가 달라 그대로 쓰면 위험하다.
CAD_API bool     CAD_SetJointType(uint32_t robot, int jointIdx, int type);
// 축의 **위치**(피벗). 부모 링크 좌표 기준. 방향만으로는 어디를 중심으로 도는지 못 정한다.
CAD_API bool     CAD_SetJointOrigin(uint32_t robot, int jointIdx, float x, float y, float z);
CAD_API bool     CAD_GetJointOrigin(uint32_t robot, int jointIdx, float* x, float* y, float* z);
// 링크 이름 → 그 링크의 씬 객체 ID (형상 없는 링크는 0). 색/재질 변경 등에 사용.
CAD_API uint32_t CAD_GetLinkObject(uint32_t robot, const char* linkName);
// 씬에서 로봇 전부 제거.
CAD_API void     CAD_ClearRobots(void);

// ── 런타임 리깅 — URDF 없이 씬 객체를 부모-자식으로 붙이기 ──
// "큐브 9개로 로봇 만들기". 붙여도 부품은 제자리에 그대로 있는다(월드 변환을 계층으로 옮김).
// type: 0=고정 1=회전 2=연속회전 3=직선. axis 는 붙일 때의 축 방향.
// pivot(월드) = 축의 **위치**. usePivot=false 면 자식이 있는 자리를 쓴다.
// 회전 중심이 부품 중심이라는 법이 없다 — 문 경첩은 모서리에 있다.
CAD_API bool     CAD_RigAttach(uint32_t parentObj, uint32_t childObj,
                               float axisX, float axisY, float axisZ, int type,
                               float pivotX, float pivotY, float pivotZ, bool usePivot);
// 이 부품과 그 **아래 가지 전체**를 떼어낸다. 각자 보이는 자리를 유지한다.
CAD_API bool     CAD_RigDetach(uint32_t obj);
// 이 객체가 속한 로봇 핸들 (0 = 안 붙어 있음).
CAD_API uint32_t CAD_GetObjectRobot(uint32_t obj);

// ── 폴리선 정점 목록 (PEDIT 대응) ──
// 닫기·열기·반전·정점 삽입/삭제는 전부 "정점 목록을 바꾸는 일" 이라, 하위 명령마다
// 함수를 늘리는 대신 목록을 통째로 읽고 쓰게 한다. 호스트가 받아서 고쳐 되돌려주면 된다.
//
//   uint32_t n = CAD_GetPolylinePoints(id, NULL, 0, NULL);   // 개수 질의
//   float* buf = malloc(n * 3 * sizeof(float));
//   bool closed = false;
//   CAD_GetPolylinePoints(id, buf, n, &closed);
//   ... 고친 뒤 ...
//   CAD_SetPolylinePoints(id, buf, n, closed);               // undo 1스텝으로 적재
//
// xyz 는 3 × count 개 float. 좌표는 객체 로컬 (그립/스케치와 같은 공간).
// Set 은 정점이 2개 미만이면 실패. Polyline 이 아닌 객체는 0 / false.
CAD_API uint32_t CAD_GetPolylinePoints(uint32_t id, float* outXyz, uint32_t cap, bool* outClosed);
CAD_API bool     CAD_SetPolylinePoints(uint32_t id, const float* xyz, uint32_t count, bool closed);

// ── 점군 (라이다 / 스캐너) ──
// 생성과 갱신이 분리돼 있다. 센서는 초당 수십 번 갱신하는데, 그때마다 객체를 지웠다
// 만들면 ID 가 바뀌어 선택·트랜스폼이 매번 풀린다. 한 번 만들고 계속 Update 한다.
//
//   uint32_t pc = CAD_CreatePointCloud();
//   // 매 스캔마다
//   CAD_UpdatePointCloud(pc, xyz, rgba, count);
//
// xyz  : 3 × count 개의 float (x,y,z 반복). 객체 로컬 좌표 — 센서 위치/자세는
//        CAD_SetTransform 으로 따로 준다(점을 미리 변환하지 말 것. 그러면 매 프레임
//        count 개를 CPU 에서 곱해야 한다).
// rgba : 4 × count 개의 uint8. NULL 이면 객체 색으로 단색 표시(LaserScan 처럼 색이 없는 소스).
// count: 점 개수. 0 이면 비운다.
//
// ⚠️ 규모 전제: **수만~수십만 점**. 수백만 점(측량 스캔)은 옥트리 LOD 가 있어야 하고,
//    그건 이 API 위에 얹을 별도 레이어다 — 함수 모양은 그때도 바뀌지 않는다.
// 갱신은 GPU 스톨 없이 host-visible 버퍼 memcpy 로 처리된다(매 프레임 호출해도 안전).
CAD_API uint32_t CAD_CreatePointCloud(void);
// Particle presets: 0 fire, 1 smoke, 2 sparks. Inserted stopped.
// unitsPerMeter = 1 for metre drawings, 1000 for millimetre drawings.
// Call on the engine thread, outside CAD_Tick (same rule as CAD_CreateBox).
CAD_API uint32_t CAD_CreateParticleEmitter(int preset, float x, float y, float z, float unitsPerMeter);
// Load a VulkanCAD v1 JSON preset (not Unity/Unreal/Effekseer), create stopped.
// UTF-8 path. Invalid/unsupported file returns 0; never modifies the source file.
CAD_API uint32_t CAD_CreateParticleEmitterFromPreset(const char* path, float x, float y, float z, float unitsPerMeter);
// state: 0 pause (preserves particles), 1 play/resume, 2 reset/stop.
CAD_API bool CAD_SetParticlePlayback(uint32_t id, int state);
// Load an Effekseer .efkefc/.efk with referenced resources. Inserted stopped.
// CAD_SetParticlePlayback also controls these objects. Files embed in .lot.
CAD_API uint32_t CAD_CreateExternalEffect(const char* path,float x,float y,float z,float unitsPerMeter);
CAD_API bool     CAD_UpdatePointCloud(uint32_t id,
                                      const float* xyz,
                                      const unsigned char* rgba,
                                      uint32_t count);
// 화면 픽셀 크기 (1~64 로 클램프). 기본 2.
CAD_API bool     CAD_SetPointCloudSize(uint32_t id, float pointSizePx);
CAD_API uint32_t CAD_GetPointCloudCount(uint32_t id);

CAD_API uint32_t CAD_GetSelectedCount(void);
CAD_API bool CAD_GetSelectedObjectId(uint32_t index, uint32_t* id);

// ── 객체 조회/대입 (ObjectARX 스타일) ──
// 전체 객체 ID 열거 — outIds 에 최대 cap 개 채우고 총 개수 반환. cap=0/outIds=NULL → 개수만 질의.
// (2단계: 먼저 개수 질의 → 버퍼 할당 → 다시 호출)
CAD_API uint32_t CAD_GetObjectIds(uint32_t* outIds, uint32_t cap);
// 종류: 0=Mesh 1=Line 2=Polyline 3=Circle 4=Arc 5=Text 6=Dimension
//       7=PointCloud 8=ParticleEmitter, 없으면 -1.
CAD_API int      CAD_GetObjectKind(uint32_t id);
CAD_API bool     CAD_ObjectExists(uint32_t id);
// 이름 — 씬 안에서 **유일**해야 한다. 중복이면 false (이름은 사람과 외부 API 가
// 부품을 가리키는 유일한 수단이라, 겹치면 어느 쪽인지 알 수 없다). 빈 이름은 허용.
CAD_API bool     CAD_SetObjectName(uint32_t id, const char* utf8);
CAD_API int      CAD_GetObjectName(uint32_t id, char* outUtf8, int cap);   // 길이 반환, -1=실패
CAD_API uint32_t CAD_FindObjectByName(const char* utf8);                   // 0 = 없음
CAD_API bool     CAD_GetColor(uint32_t id, float* r, float* g, float* b);
CAD_API bool     CAD_SetColor(uint32_t id, float r, float g, float b);
// 월드 AABB (min/max). GetCenter = 0.5*(min+max). 대형 임포트 메시는 AABB 폴백으로 유효.
CAD_API bool     CAD_GetBoundsWorld(uint32_t id,
                                    float* minX, float* minY, float* minZ,
                                    float* maxX, float* maxY, float* maxZ);
CAD_API bool     CAD_GetCenterWorld(uint32_t id, float* x, float* y, float* z);

/* ── 측정 ─────────────────────────────────────────────────────────────────
 *
 * 호스트 상태바에 "길이 3,600" / "면적 12.5" 를 띄우려면 필요하다.
 * **월드 좌표로** 잰다 — 축척이 걸린 객체도 화면에 보이는 값과 맞는다.
 *
 * 길이: 선/폴리선 = 변의 합(닫혀 있으면 마지막→첫 변 포함), 원 = 2πr, 호 = rθ.
 * 면적: 닫힌 폴리선 = 신발끈 공식(3D 평면판), 원 = πr², 호 = 부채꼴.
 * 메시·문자·점군은 false (그 종류엔 정의되지 않는다).
 */
/* ── 내보내기 ─────────────────────────────────────────────────────────────
 *
 * **확장자로 포맷이 정해진다** — .stl .dxf .glb .obj .lot
 * 전에는 CAD_ExportObj 하나뿐이라 나머지 포맷을 호스트에서 쓸 수가 없었다.
 * selectedOnly=true 면 선택된 것만(선택이 비었으면 실패).
 * 대화상자를 띄우지 않으므로 경로는 호스트가 정한다.
 */
CAD_API bool     CAD_ExportFile(const char* path, bool selectedOnly);

/* ── 클립보드 (모바일용) ──────────────────────────────────────────────────
 *
 * 데스크톱은 ImGui 백엔드가 OS 클립보드로 연결해 주지만, iOS/안드로이드는 그게 없다.
 * 클립보드가 Swift 의 UIPasteboard / Kotlin 의 ClipboardManager 쪽에 있어서
 * C++ 에서 직접 못 만진다. 그래서 **호스트가 대신 처리**한다.
 *
 *   복사      : 엔진이 복사할 때 등록한 콜백이 불린다 → 호스트가 OS 클립보드에 넣는다
 *   붙여넣기  : 호스트가 CAD_PasteText 로 밀어 넣는다 (엔진이 물어보지 않는다)
 *
 * 붙여넣기를 "미는" 이유: 엔진이 동기로 물어보면 호스트가 돌려주는 문자열의 수명을
 * 누가 책임지는지가 애매해진다. 모바일 UX 도 "붙여넣기 버튼을 누른다" 라 미는 쪽이 맞다.
 * 데스크톱에서도 등록하면 이쪽이 우선한다(임베드 앱이 자기 정책을 쓰고 싶을 때).
 */
CAD_API void     CAD_SetOnCopyText(void (*cb)(const char* utf8));
CAD_API void     CAD_PasteText(const char* utf8);

/* ── 이미지 붙이기 ────────────────────────────────────────────────────────
 *
 * 도면 밑에 깔 이미지(평면도 사진 등)를 XY 평면에 붙인다.
 * 엔진의 imageattach 명령은 **파일 대화상자를 띄우므로 모바일에서 못 쓴다** —
 * 호스트가 사진 앱(PHPicker / Intent)에서 고른 뒤 여기로 넘긴다.
 *
 *   FromFile   : 경로로. 데스크톱·모바일 공통.
 *   FromMemory : RGBA8 픽셀을 직접. 사진 앱이 경로 대신 데이터를 주는 경우
 *                (iOS PHPicker 가 그렇다) 파일로 떨구지 않고 바로 넘길 수 있다.
 *
 * widthWorld = 0 이면 긴 변이 10 단위가 되게 자동. 종횡비는 항상 보존한다.
 * 반환: 만들어진 객체 id (실패 0).
 */
CAD_API uint32_t CAD_AttachImageFromFile(const char* pathUtf8, float widthWorld,
                                         float originX, float originY, float originZ);
CAD_API uint32_t CAD_AttachImageFromMemory(const unsigned char* rgba, int width, int height,
                                           const char* displayName, float widthWorld,
                                           float originX, float originY, float originZ);

/* ── OCR 결과 주입 (모바일용) ─────────────────────────────────────────────
 *
 * 모바일은 **OS 내장 OCR 을 쓴다** — iOS Vision, 안드로이드 ML Kit.
 * Tesseract(45MB)를 올릴 이유가 없고, 내장 쪽이 한글·혼합 글자에서 더 정확하다.
 * 다만 둘 다 Swift/Kotlin API 라 C++ 에서 부르기 번거로우므로, **호스트가 인식하고
 * 결과만 넘긴다.** 그 뒤 흐름(네모 표시 → 드래그로 줄 고르기 → 커서 배치)은
 * 데스크톱과 완전히 같다.
 *
 * imageId : CAD_AttachImage* 로 붙인 이미지
 * 사각형  : 그 **이미지의 픽셀 좌표**(좌상단 원점). Vision 의 정규화 좌표를 쓰면
 *           호스트에서 픽셀로 바꿔 넘겨야 한다.
 * 호출 순서대로 = 읽는 순서. "시작~끝 사이 전부" 선택이 이 순서를 따른다.
 *
 *   CAD_BeginOcrLines(imageId);
 *   CAD_AddOcrLine("거실 3600", 29, 29, 192, 64);
 *   CAD_EndOcrLines();          // 여기서 화면에 네모가 뜬다
 */
CAD_API void     CAD_BeginOcrLines(uint32_t imageId);
CAD_API void     CAD_AddOcrLine(const char* utf8, int x0, int y0, int x1, int y1);
CAD_API void     CAD_EndOcrLines(void);

CAD_API bool     CAD_GetLength(uint32_t id, float* outLength);
CAD_API bool     CAD_GetArea  (uint32_t id, float* outArea);
/* 선택 전체의 길이·면적 합. 선택이 비었거나 잴 게 없으면 0 을 돌려준다. */
CAD_API float    CAD_GetSelectionLength(void);
CAD_API float    CAD_GetSelectionArea(void);

// ── 대화형 픽 (AutoLISP getpoint / ObjectARX acedGetEntsel 대응) ──
// 비동기: Begin 후 사용자가 클릭할 때까지 대기. 호스트가 매 프레임 bool 폴링.
//   IsPicking=true            → 대기 중
//   IsPicking=false + Try=true  → 완료 (좌표/엔티티 소비)
//   IsPicking=false + Try=false → 취소(ESC)
// EntSel 은 빈 곳/비객체 클릭을 통과 안 시킴(유효 객체만 완료). Point 는 어디든 좌표 반환.
CAD_API void CAD_BeginGetPoint(const char* prompt);
CAD_API void CAD_BeginGetEntSel(const char* prompt);
CAD_API bool CAD_IsPicking(void);
// 완료됐으면 true(1회 소비) + 좌표(+EntSel 은 outId). Point 는 outId=0. 대기/취소면 false.
CAD_API bool CAD_TryGetPickResult(float* x, float* y, float* z, uint32_t* outId);
CAD_API void CAD_CancelPick(void);

// ── 내보내기 (Export) ──
// selectedOnly=false → 씬 전체, true → 선택 객체만. 월드 좌표로 baking. 성공 시 true.
CAD_API bool CAD_ExportObj(const char* path, bool selectedOnly);

// ── 저장 / 내보내기 / 가져오기 ──────────────────────────────────────────
// 확장자로 갈린다. 엔진 안의 분기(saveByExtension / loadModelFromPath)를 그대로 쓴다 —
// 호스트가 포맷을 판별해 다른 함수를 고를 필요가 없고, 새 포맷이 붙어도 API 는 그대로다.
//
// CAD_SaveAs  : .lot(프로젝트) · .stl · .dxf · .obj · .glb/.gltf
//               .lot 은 씬 전체(선·솔리드·로봇·문서 상태)를 담는 네이티브 형식이고,
//               나머지는 내보내기다. selectedOnly=true 면 선택 객체만.
//               ⚠️ 점군은 아직 .lot 에 안 담긴다 — 저장해도 스캔은 빠진다.
// CAD_OpenFile: .lot · .obj · .stl · .dxf · .ply(점군) · .gltf/.glb
//               **현재 문서에 추가**한다(새 탭을 만들지 않음). 새 탭으로 열려면
//               CAD_OpenDocument 를 쓴다.
//
// 둘 다 실패 시 false 를 반환하고 사유는 CAD_GetStatusMessage 로 읽는다.
CAD_API bool CAD_SaveAs(const char* pathUtf8, bool selectedOnly);
CAD_API bool CAD_OpenFile(const char* pathUtf8);

// ── 편집 명령 ───────────────────────────────────────────────────────────
// 전부 **현재 선택**에 적용되고 각각 undo 1스텝으로 들어간다. 화면 클릭이 필요 없는
// (인자만으로 결정되는) 명령들만 여기 노출한다 — 트림/연장/오프셋 방향처럼 커서가 있어야
// 뜻이 정해지는 것들은 대화형이라 별도 픽 API(CAD_BeginGetPoint 등)를 거쳐야 한다.

// 로프트 — 선택된 **닫힌 단면 2개 이상**을 축 순서로 이어 솔리드 생성.
// 단면이 지그재그로 놓여 순서를 직접 주고 싶으면 화면에서 클릭 순서로 지정해야 한다.
CAD_API bool CAD_Loft(void);

// 셸 — 선택 솔리드의 속을 비우고 두께만 남긴다. thickness<=0 이면 크기에 맞춰 자동.
CAD_API bool CAD_Shell(float thickness);

// 분해(EXPLODE) — 선택 객체를 구성 요소로 쪼갠다.
//   문자                → 글리프 윤곽선 폴리선
//   폴리선/사각형/다각형 → 개별 선분 (닫힌 도형은 마지막↔첫 정점 구간까지)
// 선·원·호는 더 쪼갤 수 없어 그대로 남는다. keepOriginal=true 면 원본을 지우지 않는다.
// 여러 종류가 섞여 있어도 undo 는 한 번이다. 쪼갤 것이 하나도 없으면 false.
CAD_API bool CAD_Explode(bool keepOriginal);

// 모서리 필렛/챔퍼 — 선택 솔리드의 볼록 모서리를 굴리거나(fillet) 깎는다(chamfer).
// distance<=0 이면 객체 크기에 맞춰 자동.
CAD_API bool CAD_EdgeFillet(float distance);
CAD_API bool CAD_EdgeChamfer(float distance);

// 직사각형 배열 — cols(가로 개수) × rows(세로 개수), dx/dy 간격(음수 = 반대 방향).
// 원본이 (0,0) 칸을 차지하므로 실제 생성 개수는 cols*rows-1.
// 평면은 활성 뷰에서 정해진다 (Top=XY, Front=XZ, Right=YZ).
CAD_API bool CAD_ArrayRect(int cols, int rows, float dx, float dy);

// 원형 배열 — count = **원본 포함** 총 개수, angleDeg = 총 채움 각(360 = 한 바퀴).
// center = 회전 중심(월드), rotateItems=false 면 항목 방향을 유지한 채 위치만 옮긴다.
// 회전축은 활성 뷰의 평면 법선이다.
CAD_API bool CAD_ArrayPolar(int count, float angleDeg,
                            float centerX, float centerY, float centerZ,
                            bool rotateItems);

// ── 멀티뷰포트 (화면 분할) ──
// layout: 0=Single(단일) 1=Dual(좌우) 2=Triple 3=Quad(2x2)
CAD_API void CAD_SetViewportLayout(int layout);
CAD_API int  CAD_GetViewportLayout(void);
// 활성(클릭) 뷰포트 칸 인덱스 0~3.
CAD_API void CAD_SetActiveViewport(int index);
CAD_API int  CAD_GetActiveViewport(void);

// ── Revolve 도구 ──
CAD_API void CAD_RequestStartRevolve(void);

// ── Undo / Redo ──
CAD_API void CAD_Undo(void);
CAD_API void CAD_Redo(void);
CAD_API bool CAD_CanUndo(void);
CAD_API bool CAD_CanRedo(void);

// ── 뷰/표시 상태 토글 ──
CAD_API void CAD_SetGridEnabled(bool enabled);
CAD_API void CAD_SetDemoLighting(bool enabled);
// style: 0=Shaded 1=ShadedEdge 2=Wireframe 3=WireframeEdge 4=HiddenLine
CAD_API void CAD_SetVisualStyle(int style);

// 선택 하이라이트 스타일: 0=Silhouette(JFA 외곽선), 1=Edge(feature edge 노란색, CAD 방식)
CAD_API void CAD_SetSelectionStyle(int style);
CAD_API int  CAD_GetSelectionStyle(void);

// 호버 강조 — 마우스가 올라간 객체를 연한 하늘색 엣지로 표시(선택은 노란색).
// 기본 켬. 도면 작업 중 시선 분산이 싫거나 초대형 씬에서 부담될 때 끌 수 있다.
CAD_API void CAD_SetHoverHighlight(bool enabled);
CAD_API bool CAD_GetHoverHighlight(void);
// 현재 커서 아래 객체 id (없으면 0). 호스트가 상태바·툴팁에 이름을 띄우는 용도.
CAD_API unsigned int CAD_GetHoveredObject(void);

// 호버 표시 스타일 — 색(0~1) + 선 두께(px, 0.5~10 로 클램프).
// ⚠️ 두께는 GPU 가 wide line 을 지원할 때만 반영된다(미지원 기기는 1px 고정).
CAD_API void CAD_SetHoverStyle(float r, float g, float b, float width);
// 선택 표시 두께 (색은 노란색 고정 — CAD 관례).
CAD_API void CAD_SetSelectionLineWidth(float width);

// 벽 충돌 (Walk / 캐릭터 모드). 끄면 벽을 통과한다.
// ⚠️ 정적 지오메트리 대상의 캐릭터 충돌이지 물리엔진이 아니다 — 물체는 떨어지지 않는다.
CAD_API void CAD_SetCollisionEnabled(bool enabled);
CAD_API bool CAD_GetCollisionEnabled(void);

// 명령 프롬프트(현재 대화형 도구 안내). buf 에 UTF-8 로 복사, 반환=문자열 길이(널 제외).
// 호스트가 자기 상태바에 표시하려고 매 프레임 폴링. buf/bufLen 0 이면 길이만 반환.
CAD_API int  CAD_GetStatusMessage(char* buf, int bufLen);

// 일회성 안내 (몇 초 뒤 스스로 사라짐). 프롬프트와 **다른 값**이다 —
// 프롬프트는 "지금 뭘 해야 하는지"(상시), 이쪽은 "방금 왜 안 됐는지"(일시).
//   예: "각도: 선을 클릭하세요", "선: 커서를 방향으로 옮긴 뒤 거리를 입력하세요"
// 없거나 만료됐으면 빈 문자열(길이 0). 데스크톱은 ImGui 상태바가 그리지만 모바일은
// 표시할 곳이 없어 이게 유일한 경로다 — 안 띄우면 클릭이 씹힌 것처럼 보인다.
CAD_API int  CAD_GetTransientMessage(char* buf, int bufLen);

// ── 3D 불리언 (CSG) ──
// Union/Intersection 은 현재 선택된 메시 전체 대상. keepOriginals=false 면 원본 삭제.
CAD_API void CAD_BooleanUnion(bool keepOriginals);
CAD_API void CAD_BooleanIntersection(bool keepOriginals);
// Difference: union(baseIds) − union(subtractIds). id 배열 + 개수 전달.
CAD_API void CAD_BooleanDifference(const uint32_t* baseIds, uint32_t baseCount,
                                   const uint32_t* subtractIds, uint32_t subtractCount,
                                   bool keepOriginals);

#ifdef __cplusplus
}
#endif
