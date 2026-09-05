# VulkanCAD — Android 샘플 (Gradle + NDK)

Android 기기/에뮬레이터에서 VulkanCAD 엔진을 띄우는 최소 샘플.
엔진 전체를 `libvulkancad.so` 로 크로스컴파일하고 `SurfaceView` 에 Vulkan 렌더한다.
크로스플랫폼(Windows/macOS/Linux 호스트) 빌드 지원.

## 구조

| 파일 | 역할 |
|------|------|
| `run_android.sh` | 에뮬레이터 실행 + 빌드 + 설치 + 앱 실행 (한 방에) |
| `app/src/main/AndroidManifest.xml` | `MainActivity` 를 런처(`action.MAIN`)로 지정 |
| `.../java/.../MainActivity.kt` | ⭐ **진입점** — 에셋 filesDir 추출 → 툴바 + 렌더뷰 구성 |
| `.../java/.../VulkanSurfaceView.kt` | SurfaceView + Choreographer(vsync) Tick + 터치 |
| `.../java/.../CadNative.kt` | JNI 선언 + `System.loadLibrary("vulkancad")` |
| `.../java/.../CadMobileBridge.kt` | 모바일 OS 기능(클립보드/사진/OCR) 연결 |
| `app/src/main/cpp/CMakeLists.txt` | 엔진 소스 → Android `.so` (GLOB·제외 목록·`LOT_NO_IMGUI`/`LOT_PLATFORM_IOS`) |
| `app/src/main/cpp/android_jni.cpp` | JNI(`CadNative`) ↔ C API(`CAD_*`). ANativeWindow 로 AttachView/Tick/터치 |
| `app/src/main/cpp/android_stubs.cpp` | 데스크톱 UI 클래스(LotUiManager 등) no-op 스텁 (ImGui 없이) |

안드로이드엔 `main()` 이 없다 — **`MainActivity.onCreate()` 가 시작점**이고, 흐름은:

```
MainActivity.onCreate()          에셋 추출 → nativeSetAssetPath → 툴바/렌더뷰 구성
  └→ VulkanSurfaceView           Surface 생성 → nativeSurfaceCreated(surface, w, h)
       └→ android_jni.cpp        CAD_AttachView + CAD_CreateEngine
            └→ FirstApp (엔진)   매 프레임 nativeTick() → CAD_Tick()
```

## 사전 준비

1. **Android Studio** + SDK Manager 에서:
   - **NDK (Side by side)**, **CMake**, **Android Emulator**, SDK Platform (API 34+)
2. **VulkanSdk** — 레포 부모 폴더에 `../VulkanSdk/{Win|Apple|Linux}` (glm/GLFW 헤더용).
   vulkan 헤더는 NDK 가 제공하므로 별도 불필요.
3. **런타임 에셋** — 별도 준비가 필요 없다. Gradle 이 레포의 `sdk/` 에서 `models/ fonts/
   textures/` 를 APK assets 로 자동 복사한다. 셰이더는 라이브러리에 내장돼 있다.

## 빌드 & 실행

### 방법 1 — `run_android.sh` (권장, 한 방에)

에뮬레이터 실행 → APK 빌드 → 설치 → 앱 실행까지 전부 처리한다. (macOS / Linux)

```bash
cd samples/android_test
./run_android.sh                 # 첫 번째 AVD 로 실행
./run_android.sh Pixel_7         # AVD 이름 지정
./run_android.sh --list          # 사용 가능한 AVD 목록만 출력
```

하는 일:
1. 붙은 기기가 없으면 AVD 를 `-gpu host` 로 띄우고 부팅 완료까지 대기 (Vulkan 렌더용)
2. `./gradlew installDebug` 로 빌드 + 설치
   (`JAVA_HOME` 없으면 Android Studio 번들 JBR 자동 사용)
3. `am start` 로 `MainActivity` 실행

실행되면 상단 툴바(큐브/전체선택/삭제/줌/Iso/Undo)와 3D 뷰가 뜬다.
**한 손가락 드래그 = 뷰 회전, 탭 = 선택.**

### 방법 2 — Android Studio

1. `samples/android_test/` 를 **Open** → Gradle Sync
   - ⚠️ `app/build.gradle.kts` 의 `ndkVersion` 을 **설치된 NDK 버전**으로 맞출 것
2. 기기/에뮬레이터 선택 → ▶ **Run**

### 방법 3 — APK 만 빌드

```bash
cd samples/android_test
./gradlew assembleDebug          # APK → app/build/outputs/apk/debug/
```

### 상태 확인 / 로그

```bash
ADB=~/Library/Android/sdk/platform-tools/adb
$ADB shell pidof com.vulkancad.androidtest      # 살아있으면 pid 출력
$ADB logcat -d | grep -iE "vulkancad|FATAL"     # 엔진/크래시 로그
$ADB shell screencap -p /sdcard/s.png && $ADB pull /sdcard/s.png   # 화면 캡처
```

## 플랫폼별 주의

### ABI
- **x86_64** : 인텔 맥 / Windows 에뮬레이터
- **arm64-v8a** : 애플 실리콘 에뮬레이터 / 실제 안드로이드 기기
- 기본으로 **둘 다 빌드** (`abiFilters`). 빌드 시간 줄이려면 필요한 것만 남길 것.

### 에뮬레이터 가속 (하이퍼바이저)
- **Windows** : BIOS 에서 가상화(**VT-x/SVM**) **Enabled** 필수 + AEHD 또는 WHPX(Windows Hypervisor Platform).
  가상화 꺼져 있으면 에뮬레이터 안 뜸.
- **macOS** : **Hypervisor.framework 내장** — BIOS 설정 불필요. 애플 실리콘은 arm64 이미지로 네이티브 실행(빠름).
- **실제 기기** : 가상화 불필요. USB 디버깅만 켜면 됨.

### 검증 현황
- ✅ NDK + CMake 로 `libvulkancad.so` 링크 성공 (Windows 호스트, x86_64)
- ✅ `gradle assembleDebug` → `app-debug.apk` BUILD SUCCESSFUL
- ✅ **에뮬레이터 실행 확인** (2026-09-02, macOS 호스트 / Pixel_7 arm64):
  `run_android.sh` 로 빌드·설치·실행 → 그리드·좌표축·ViewCube·툴바 렌더 정상

## ⚠️ 엔진 수정 후 안드로이드가 깨질 때

안드로이드는 **데스크톱과 다른 CMakeLists 를 쓰고 ImGui 가 없다.** 엔진 쪽을 고치면
데스크톱은 멀쩡한데 안드로이드만 깨지는 일이 잦다. 증상별 대응:

| 증상 | 원인 | 대응 |
|------|------|------|
| `FirstApp::*` 등 **대량 undefined symbol** | 엔진에 **새 소스 폴더**가 생겼는데 안드로이드 `CMakeLists.txt` 의 `file(GLOB ...)` 에 빠짐 | `app/src/main/cpp/CMakeLists.txt` 의 GLOB 목록을 **데스크톱 `CMakeLists.txt` 와 대조**해 누락 폴더 추가 |
| `fatal error: 'imgui.h' file not found` | 안드로이드는 `LOT_NO_IMGUI` 라 ImGui 헤더가 없는데, 그 파일이 **가드 없이** include | 해당 파일을 `#ifndef LOT_NO_IMGUI` 로 감싸고 `#ifdef LOT_NO_IMGUI` 쪽에 no-op 구현 (`lot_dimension_panel.cpp` 가 표준 예시) |
| `LotUiManager::render ... does not match any declaration` | 데스크톱 UI 헤더의 **시그니처가 바뀌었는데** `android_stubs.cpp` 가 옛 시그니처 | 헤더 선언과 1:1로 맞춰 스텁 수정 |
| 제외된 파일의 심볼 undefined (`saveModelFileDialog` 등) | `CMakeLists.txt` 제외 목록의 파일이 제공하던 함수가 새로 **호출되기 시작** | `android_stubs.cpp` 에 no-op 스텁 추가 — **실제로 undefined 로 뜬 것만** (안 뜬 걸 넣으면 중복 심볼) |

> 팁: 에러는 한 번에 다 안 나온다. 고치고 다시 `./run_android.sh` → 다음 에러 → 반복.
> 데스크톱 빌드(`cmake --build build`)도 함께 돌려 회귀가 없는지 확인할 것.

## 미구현 / 다음

- 엔진 → UI 콜백(선택 알림 등), 멀티터치 제스처 매핑 정교화(핀치 줌/2손가락 팬)
- 에셋을 AAssetManager 직접 로딩(현재는 filesDir 추출 방식)
