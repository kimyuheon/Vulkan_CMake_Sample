# LotCAD — Android 샘플 (Gradle + NDK)

Android 기기/에뮬레이터에서 LotCAD 엔진을 띄우는 샘플.
엔진은 **미리 빌드된 `libLotCADCore.so`**(Releases)를 링크만 하고, 이 레포에서는 JNI·샘플 로직만 컴파일한다.
`SurfaceView` 에 Vulkan 으로 렌더한다. 크로스플랫폼(Windows/macOS/Linux 호스트) 빌드 지원.

## 구조

| 파일 | 역할 |
|------|------|
Gradle 모듈 3개 — 엔진 연결은 `engine` 한 곳에만 두고, 앱 모듈은 그것만 참조한다.

| 모듈 | 내용 |
|------|------|
| `engine` | 미리 빌드된 엔진 `.so` + JNI(`liblotcad.so`), 렌더 뷰·런타임 에셋 (Android 라이브러리) |
| `app` | 기본 테스트 앱 — 그리기·치수·파일 열기 (`com.lotcad.androidtest`) |
| `robot` | 🆕 로봇 팔 샘플 — URDF 관절 슬라이더 (`com.lotcad.robotarm`). 로직은 [`../shared/robot_demo`](../shared) (iOS·macOS 와 공용) |

| 파일 | 역할 |
|------|------|
| `run_android.sh` | 에뮬레이터 실행 + 빌드 + 설치 + 앱 실행 (한 방에). `--robot` 이면 로봇 팔 앱 |
| `engine/.../VulkanSurfaceView.kt` | SurfaceView + Choreographer(vsync) Tick + 터치. `onEngineReady`·`onBeforeTick` 훅 |
| `engine/.../CadNative.kt` | JNI 선언 + `System.loadLibrary("lotcad")` |
| `engine/.../EngineAssets.kt` | APK assets → filesDir 추출 + 엔진 에셋 경로 지정 (앱마다 onCreate 에서 한 번) |
| `engine/src/main/cpp/CMakeLists.txt` | `sdk/lib-android/<ABI>/libLotCADCore.so` 를 IMPORTED 로 링크 + JNI·샘플 공용 로직을 `liblotcad.so` 로 |
| `engine/src/main/cpp/android_jni.cpp` | JNI(`CadNative`) ↔ C API(`CAD_*`). ANativeWindow 로 AttachView/Tick/터치 |
| `app/.../MainActivity.kt` | ⭐ 테스트 앱 **진입점** — 툴바 + 렌더뷰 구성 |
| `app/.../CadMobileBridge.kt` | 모바일 OS 기능(클립보드/사진/OCR) 연결 |
| `robot/.../RobotActivity.kt` | 로봇 팔 화면 — 3D 뷰 + [재생]·[홈 자세]·[화면 맞춤] + 관절 슬라이더 |
| `robot/src/main/cpp/robot_jni.cpp` | JNI(`RobotNative`) ↔ `RobotDemo_*`. `liblotcad.so` 에 같이 빌드(엔진 상태는 엔진 `.so` 하나라 두 앱이 공유) |

> Kotlin 패키지 `com.lotcad.androidtest` 는 `engine` 모듈에도 그대로다 — JNI 함수 이름이 이 패키지에 묶여 있다.

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
2. **엔진 라이브러리** — 레포 루트에서 Releases 의 Android SDK 를 받아 `sdk/` 에 푼다:
   ```bash
   gh release download android-sdk-2026.10.05 -R kimyuheon/Vulkan_CMake_Sample -p '*.zip'
   unzip -o LotCAD-Android-SDK-*.zip -d sdk/      # → sdk/lib-android/{arm64-v8a,x86_64}/libLotCADCore.so
   ```
   Vulkan 은 Android 7.0+ 기기에 기본 포함 — 따로 설치할 것이 없다.
3. **런타임 에셋** — 별도 준비가 필요 없다. Gradle 이 레포의 `sdk/` 에서 `models/ fonts/
   textures/` 를 APK assets 로 자동 복사한다. 셰이더는 라이브러리에 내장돼 있다.

## 빌드 & 실행

### 방법 1 — `run_android.sh` (권장, 한 방에)

에뮬레이터 실행 → APK 빌드 → 설치 → 앱 실행까지 전부 처리한다. (macOS / Linux)

```bash
cd samples/android_test
./run_android.sh                 # 첫 번째 AVD 로 실행
./run_android.sh Pixel_7         # AVD 이름 지정
./run_android.sh --robot         # 로봇 팔 샘플 (AVD 이름과 같이 써도 됨)
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
   - NDK 버전은 설치된 것 중 최신을 자동으로 고른다(`engine/build.gradle.kts`)
   - 실행 구성에서 `app`(테스트 앱) 또는 `robot`(로봇 팔)을 고른다
2. 기기/에뮬레이터 선택 → ▶ **Run**

### 방법 3 — APK 만 빌드

```bash
cd samples/android_test
./gradlew assembleDebug          # APK → app/build/outputs/apk/debug/
```

### 상태 확인 / 로그

```bash
ADB=~/Library/Android/sdk/platform-tools/adb
$ADB shell pidof com.lotcad.androidtest      # 살아있으면 pid 출력
$ADB logcat -d | grep -iE "lotcad|FATAL"     # 엔진/크래시 로그
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
- ✅ 미리 빌드된 엔진 `.so`(엔진 b17c935, Release) 링크 → `app`·`robot` APK 빌드 (macOS 호스트)
- ✅ **에뮬레이터 실행 확인** (2026-10-05, Pixel_7 arm64): 테스트 앱 · 로봇 팔(재생·슬라이더) 정상

## 엔진 라이브러리를 새로 만들 때 (엔진 개발자용)

`libLotCADCore.so` 는 엔진 레포의 `build_android.sh` 가 만든다(ABI 별 Release, 공개 C API 만 내보냄).
엔진 레포를 이 레포 옆(`../3dEngine`)에 두고 실행하면 `sdk/lib-android/<ABI>/` 로 자동 복사된다.
엔진을 고쳐 안드로이드 빌드가 깨질 때(새 소스 폴더·스텁 시그니처 등)의 대응도 엔진 레포 `android/` 쪽 일이다.

## 미구현 / 다음

- 엔진 → UI 콜백(선택 알림 등), 멀티터치 제스처 매핑 정교화(핀치 줌/2손가락 팬)
- 에셋을 AAssetManager 직접 로딩(현재는 filesDir 추출 방식)
