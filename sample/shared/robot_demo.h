// robot_demo — 로봇 팔 샘플의 공용 로직 (iOS·Android 가 같이 쓴다).
//
// 화면(슬라이더·버튼)은 각 플랫폼이 그리고, "무엇을 움직이는가" 는 여기 한 곳에 둔다.
// URDF 관절을 사람이 다루기 좋은 **조작 항목(control)** 으로 바꿔 보여 준다:
//   - 회전 관절 → 도(°) 단위 슬라이더
//   - 손가락 직선 관절(prismatic) 여러 개 → "그리퍼" 하나(0~100 %)로 묶음
//
// 엔진 C API(CAD_*)만 부르므로 엔진 생성(CAD_CreateEngine) 뒤에 쓴다.
// 이 헤더는 엔진 헤더를 끌어오지 않는다 — Swift 브리징 헤더에 그대로 넣을 수 있게.
#ifndef ROBOT_DEMO_H
#define ROBOT_DEMO_H

#include <stdbool.h>

#ifdef __cplusplus
extern "C" {
#endif

/* models/demo_arm.urdf 를 불러와 화면에 맞춘다. 이미 불러왔으면 그대로 true. */
bool        RobotDemo_Load(void);

/* 조작 항목 — 0..count-1. 라벨·단위는 UTF-8, 다음 Load 전까지 유효. */
int         RobotDemo_ControlCount(void);
const char* RobotDemo_ControlLabel(int i);
const char* RobotDemo_ControlUnit(int i);   /* "°" 또는 "%" */
float       RobotDemo_ControlMin(int i);
float       RobotDemo_ControlMax(int i);
float       RobotDemo_GetControl(int i);
/* 값을 넣는다(범위 밖이면 잘림). 손으로 움직이면 자동 재생은 멈춘다. */
void        RobotDemo_SetControl(int i, float value);

/* 모든 관절 0 자세. */
void        RobotDemo_Home(void);

/* 자동 재생 — 관절마다 다른 속도로 천천히 흔든다. 매 프레임 Update 를 불러야 움직인다. */
void        RobotDemo_SetPlaying(bool on);
bool        RobotDemo_IsPlaying(void);
/* 매 프레임(CAD_Tick 전에) 호출. dt = 지난 프레임부터 초. */
void        RobotDemo_Update(double dtSeconds);

#ifdef __cplusplus
}
#endif

#endif /* ROBOT_DEMO_H */
