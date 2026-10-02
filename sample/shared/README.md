# shared — 플랫폼 공용 샘플 로직

기능 샘플의 "무엇을 하는가" 를 C++ 한 벌로 두고, 각 플랫폼은 화면만 그린다.
엔진 C API(`CAD_*`)만 부르며, 헤더는 엔진 헤더를 끌어오지 않아 Swift 브리징 헤더에 그대로 들어간다.

| 파일 | 샘플 | 쓰는 곳 |
|------|------|---------|
| `robot_demo.h/.cpp` | 로봇 팔 — `models/demo_arm.urdf` 를 불러와 관절을 조작 항목으로 묶고(회전 관절 = 도, 손가락 = 그리퍼 %), 자동 재생 | Android `android_test/robot` · iOS `swift_ios_test` 의 `RobotArmiOS` · macOS `swift_api_test` 의 `RobotArmMac` |

## 플랫폼별로 붙는 방식

| 플랫폼 | 소스 포함 | 호출 |
|--------|-----------|------|
| Android | `engine/src/main/cpp/CMakeLists.txt` 가 엔진 `.so` 에 같이 빌드 (엔진 전역 상태를 공유해야 해서 `.so` 하나) | `robot/src/main/cpp/robot_jni.cpp` → Kotlin `RobotNative` |
| iOS | `project.yml` 의 `RobotArmiOS` 타깃 sources 에 `../shared` | 브리징 헤더 `Sources/RobotArm/RobotArm-Bridging-Header.h` |
| macOS | SwiftPM 은 패키지 밖 경로를 못 받아 `Sources/RobotDemo/` 에 심볼릭 링크 → C++ 타깃 `RobotDemo` | `import RobotDemo` |

## 호스트가 지킬 것

- 엔진 생성(`CAD_CreateEngine`) **뒤에** `RobotDemo_Load()`. 다시 불러도 안전하다(엔진이 새로 만들어졌으면 다시 로드).
- 매 프레임 `CAD_Tick` **전에** `RobotDemo_Update(dt)` — 자동 재생이 여기서 진행된다.
- 관절 대입은 엔진이 다시 그리기를 표시하므로(iOS render-on-demand 포함) 호스트가 따로 깨울 필요가 없다.
