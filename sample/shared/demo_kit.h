// demo_kit — 기능 샘플(파티클·Jig·단면도·평면도·내비·AI)의 공용 로직 + 조작 UI 정의.
//
// 로봇 팔(robot_demo)과 같은 구조로, 무엇을 하는지는 여기 C++ 한 벌에 두고 플랫폼은 화면만 그린다.
// 한 걸음 더 나아가 **조작 항목(슬라이더·버튼·토글·선택지·입력칸·표시줄)도 여기서 정의**한다 —
// 그래서 플랫폼은 "항목 목록을 그리는 범용 패널" 하나만 있으면 모든 기능 샘플을 띄운다.
//
// 호스트가 할 일:
//   1. 엔진 생성(CAD_CreateEngine) 뒤 DemoKit_Start("particle") 같은 식으로 시작.
//   2. 매 프레임 CAD_Tick **전에** DemoKit_Update(dt).
//   3. DemoKit_Revision() 이 바뀌면 항목 목록을 다시 만든다(선택지에 따라 항목이 바뀌는 데모가 있다).
//   4. 몇 프레임에 한 번 값·문구를 다시 읽는다(GetValue/GetText) — 재생 중 값이 엔진에서 바뀐다.
//
// 이 헤더는 엔진 헤더를 끌어오지 않는다 — Swift 브리징 헤더·C# P/Invoke 에 그대로 쓸 수 있게.
#ifndef DEMO_KIT_H
#define DEMO_KIT_H

#include <stdbool.h>

#ifdef __cplusplus
extern "C" {
#endif

/* 조작 항목 종류 */
enum {
    DEMO_HEADER = 0,   /* 묶음 제목(입력 없음) */
    DEMO_SLIDER = 1,   /* Min..Max, GetValue/SetValue */
    DEMO_BUTTON = 2,   /* Press */
    DEMO_TOGGLE = 3,   /* 값 0/1 */
    DEMO_CHOICE = 4,   /* 값 = 고른 번호, ChoiceCount/ChoiceLabel */
    DEMO_TEXT   = 5,   /* 한 줄 입력 — GetText/SetText */
    DEMO_LABEL  = 6,   /* 표시 전용 문구 — GetText (여러 줄 가능) */
    DEMO_FILE   = 7    /* 파일 고르기 버튼 — 호스트가 선택기를 띄우고 고른 경로를 SetText 로 넘긴다 */
};

/* 데모 목록 — id 는 "robot" "particle" "jig" "section" "floorplan" "nav" "ai" */
int         DemoKit_DemoCount(void);
const char* DemoKit_DemoId(int index);
const char* DemoKit_DemoTitle(int index);
const char* DemoKit_DemoSummary(int index);

/* 시작 — 장면을 비우고 그 데모의 장면·항목을 만든다. 다른 데모가 돌고 있으면 정리하고 바꾼다. */
bool        DemoKit_Start(const char* demoId);
const char* DemoKit_CurrentTitle(void);
void        DemoKit_Update(double dtSeconds);
int         DemoKit_Revision(void);          /* 항목 목록이 바뀔 때마다 증가 */
const char* DemoKit_Status(void);            /* 화면 위에 띄울 한 줄 상태 */

/* 항목 — 0..count-1. 문자열은 다음 호출 전까지 유효(필요하면 바로 복사). */
int         DemoKit_ControlCount(void);
int         DemoKit_ControlKind(int i);
const char* DemoKit_ControlLabel(int i);
const char* DemoKit_ControlUnit(int i);
float       DemoKit_ControlMin(int i);
float       DemoKit_ControlMax(int i);
bool        DemoKit_ControlEnabled(int i);
float       DemoKit_GetValue(int i);
void        DemoKit_SetValue(int i, float value);
void        DemoKit_Press(int i);
int         DemoKit_ChoiceCount(int i);
const char* DemoKit_ChoiceLabel(int i, int choice);
const char* DemoKit_GetText(int i);
void        DemoKit_SetText(int i, const char* utf8);

#ifdef __cplusplus
}
#endif

#endif /* DEMO_KIT_H */
