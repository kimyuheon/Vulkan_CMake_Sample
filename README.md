# VulkanCAD 샘플

VulkanCAD 엔진을 여러 UI 프레임워크에서 호스팅하는 예제 모음입니다.
헤더·에셋은 `sdk/` 에 들어 있습니다. **데스크톱 라이브러리 — Windows(`VulkanCADCore.dll` + `.lib`) ·
macOS(`libVulkanCADCore.dylib`) · Linux(`libVulkanCADCore.so`) — 도 들어 있어** [Vulkan SDK](https://vulkan.lunarg.com/)만
설치하면 clone 후 데스크톱 샘플을 바로 빌드·실행할 수 있습니다(아래 [플랫폼별 준비물](#플랫폼별-준비물)).
모바일(iOS 정적 라이브러리 · Android `.so`)은 GitHub Releases 에서 받아 `sdk/` 바로 아래에 둡니다.

(동봉된 데스크톱 바이너리는 공개용 스냅숏(Release — Windows·macOS 는 엔진 3f29209)으로, 평소 개발 중 빌드 결과는 git 이 추적하지 않습니다 — `.gitignore` 참고.
안드로이드는 엔진 `.so` 를 iOS 처럼 Releases 에서 받습니다.)

```
3dEngine_Sample/
├── sdk/                    ← 엔진 배포본 (빌드에 필요한 전부)
│   ├── include/            VulkanCAD_API.h — 공개 C API
│   ├── csharp/             C# 선언 (헤더에서 자동 생성, Windows) — C# 앱은 이 *.cs 를 포함
│   ├── VulkanCADCore.dll/.lib  Windows 라이브러리 (동봉)
│   ├── libVulkanCADCore.dylib  macOS 라이브러리 (동봉, arm64 · macOS 14+)
│   ├── libVulkanCADCore.so     Linux 라이브러리 (동봉)
│   ├── lib-ios-sim/        iOS 정적 라이브러리 (시뮬레이터) — Releases 에서 받음 (아래 참조)
│   ├── lib-ios-device/     iOS 정적 라이브러리 (실기)       — Releases 에서 받음
│   ├── lib-android/        Android 엔진 .so (arm64-v8a · x86_64) — Releases 에서 받음
│   └── models/ textures/ fonts/    런타임 에셋
└── sample/
    ├── cpp_api_test/       C++ 콘솔 — API 최소 예제
    ├── mfc_dlg_test/       MFC (다이얼로그)
    ├── wpf_test/           WPF (C# P/Invoke)
    ├── qml_test/           Qt / QML
    ├── swift_api_test/     Swift (macOS, SwiftUI + AppKit)
    ├── swift_ios_test/     Swift (iOS)
    └── android_test/       Android (Kotlin + JNI) — 엔진 .so 는 Releases
```

## 엔진과의 계약

샘플은 **`sdk/include/VulkanCAD_API.h` 하나만** 참조합니다. 엔진 내부 헤더는 쓰지 않습니다.
API는 `extern "C"` 라 C# `DllImport`, Swift, Kotlin JNI 어디서든 그대로 부를 수 있습니다.

```cpp
CAD_AttachView(hwnd, w, h);   // 호스트 창을 엔진에 넘긴다
CAD_CreateEngine();           // Vulkan 초기화
CAD_Tick();                   // 매 프레임
CAD_ExecuteCommand("box");    // 명령행과 같은 입구 — 명령 200개 이상
```

## 빌드

각 샘플은 `sdk/` 를 **상대 경로**로 찾습니다. 다른 위치의 SDK를 쓰려면 아래 변수를 바꾸면 됩니다.

| 샘플 | 빌드 | SDK 경로 변수 |
|------|------|--------------|
| cpp_api_test | `cmake -B build && cmake --build build` | `-DVULKANCAD_SDK=<경로>` |
| qml_test | `cmake -B build && cmake --build build` | `-DVULKANCAD_ENGINE_BUILD=<경로>` |
| mfc_dlg_test | Visual Studio | `EngineOut` (vcxproj) |
| wpf_test | `dotnet build` | `CadCoreBuildDir` (csproj) |
| swift_api_test | `swift build` | `Package.swift` 의 `buildDirectory` |
| swift_ios_test | `xcodegen` → Xcode | `project.yml` 의 라이브러리 검색 경로 |
| android_test | `./gradlew assembleDebug` (또는 `./run_android.sh`) | `sdk/lib-android/<ABI>/` |

### 플랫폼별 라이브러리 배치

**라이브러리는 `sdk/` 바로 아래**, 에셋과 같은 층에 둡니다. 샘플들이 "라이브러리가 있는 폴더
= 에셋 폴더"를 전제하기 때문입니다 (qml_test 는 그 경로를 컴파일에 박고, mfc_dlg_test·wpf_test·winforms_test 는 거기서
DLL 과 `models/` 를 같이 꺼내 실행 파일 옆으로 복사합니다).

| OS | 파일 | 실행 시 라이브러리를 찾는 법 |
|----|------|------------------------------|
| Windows | `VulkanCADCore.dll` + `VulkanCADCore.lib` | rpath 가 없어 **exe 옆에 복사**해야 함 (샘플이 자동으로 함) |
| Linux | `libVulkanCADCore.so` | rpath 로 `sdk/` 를 직접 참조 — 복사 불필요 |
| macOS | `libVulkanCADCore.dylib` | `@rpath` — `sdk/` 와 Vulkan SDK 설치 위치(`/usr/local/lib`) 를 참조. Vulkan 로더(`libvulkan.1.dylib`)는 OS 에도 레포에도 없어 **Vulkan SDK 설치가 필수** |

Windows 는 링크에 import library(`.lib`)가 따로 필요합니다. `sdk/VulkanCADCore.lib` 로 함께 들어 있습니다.

### iOS 정적 라이브러리는 Releases 에서 내려받기

iOS 는 공유 라이브러리 대신 정적 라이브러리(시뮬레이터용·실기용 각 약 20MB + MoltenVK)를 링크합니다.
레포에는 넣지 않고 [Releases](https://github.com/kimyuheon/Vulkan_CMake_Sample/releases/tag/ios-sdk-2026.10.05) 에 올려 둡니다.

```bash
# 레포 루트에서 — zip 을 sdk/ 에 풀면 아래 두 폴더가 채워진다
gh release download ios-sdk-2026.10.05 -R kimyuheon/Vulkan_CMake_Sample -p '*.zip'
unzip -o VulkanCAD-iOS-SDK-*.zip -d sdk/
#   sdk/lib-ios-sim/      libVulkanCADCoreStatic.a  libmanifold.a  libMoltenVK.a   (Apple Silicon 맥의 시뮬레이터, arm64)
#   sdk/lib-ios-device/   libVulkanCADCoreStatic.a  libmanifold.a  libMoltenVK.a   (iPhone·iPad, arm64)
```

`gh` 가 없으면 위 Releases 페이지에서 zip 을 받아 같은 곳에 풀면 됩니다.
MoltenVK(Apache-2.0)도 zip 에 들어 있어 **Vulkan SDK 설치가 필요 없습니다**.
iOS 샘플을 안 쓰신다면 받지 않아도 됩니다.

### Android 엔진 라이브러리도 Releases 에서 내려받기

```bash
# 레포 루트에서
gh release download android-sdk-2026.10.05 -R kimyuheon/Vulkan_CMake_Sample -p '*.zip'
unzip -o VulkanCAD-Android-SDK-*.zip -d sdk/
#   sdk/lib-android/arm64-v8a/libVulkanCADCore.so   (실기 · 애플 실리콘 에뮬레이터)
#   sdk/lib-android/x86_64/libVulkanCADCore.so      (인텔·Windows 에뮬레이터)
```

자세한 실행 방법은 [sample/android_test/README.md](sample/android_test/README.md).

### 런타임 에셋

라이브러리만으로는 동작하지 않습니다 — `fonts/` 가 없으면 치수·문자가 아예 생성되지 않습니다
(글리프 메시를 만들어야 해서 폰트가 없으면 `createDimension` 이 0 을 반환합니다).

**셰이더는 없습니다.** SPIR-V 는 라이브러리에 내장돼 있어 별도 파일이 필요 없습니다.

## 플랫폼별 준비물

| | 필요한 것 |
|---|---|
| **공통** | [Vulkan SDK](https://vulkan.lunarg.com/) (Windows·Linux 는 GPU 드라이버만으로도 로더가 잡히지만 SDK 를 권장 — 검증 레이어 포함) |
| **Windows** | Visual Studio 2022 (MFC 워크로드), .NET 8 SDK (WPF·WinForms), 최신 GPU 드라이버 |
| **Ubuntu** | `build-essential cmake libvulkan-dev vulkan-tools` |
| **Qt** | Qt 6 (`qml_test`) |
| **macOS** | Xcode 15+, **Vulkan SDK — 설치할 때 "시스템 전역 설치"(System Global Installation)** 를 고를 것 (`/usr/local/lib` 에 로더·MoltenVK 가 들어간다) |
| **iOS** | Xcode 15+, Releases 의 iOS SDK zip (엔진 + MoltenVK 정적 라이브러리 — Vulkan SDK 불필요) |
| **Android** | Android Studio(NDK·CMake·Emulator), Releases 의 Android SDK(엔진 `.so`) — Vulkan 은 기기에 기본 포함 |

### Ubuntu

```bash
sudo apt install build-essential cmake libvulkan-dev vulkan-tools
vulkaninfo | head            # 드라이버가 잡히는지 확인
cd sample/cpp_api_test && cmake -B build && cmake --build build
./build/cpp_api_test
```

Qt 샘플은 `qt6-base-dev qt6-declarative-dev` 가 추가로 필요합니다.

### macOS

```bash
# 1) Vulkan SDK 설치 (https://vulkan.lunarg.com/sdk/home#mac) — "System Global Installation" 체크
ls /usr/local/lib/libvulkan.1.dylib /usr/local/share/vulkan/icd.d/   # 로더·MoltenVK 가 잡혔는지 확인
# 2) clone 후 실행
cd sample/swift_api_test && swift run VulkanCADSamples
```

Xcode 로 열려면 `open sample/swift_api_test/Package.swift` → 스킴 `VulkanCADSamples` → ⌘R.
Xcode 에서 실행하면 Metal API Validation 이 MoltenVK 를 멈출 수 있습니다 — 스킴 Edit Scheme → Run → Diagnostics 에서 끄세요.

### Windows

Visual Studio 에서 `sample/mfc_dlg_test/VulkanCAD_MFC/VulkanCAD_MFC.sln` 을 열고 **x64** 로 빌드합니다.
C# 은 `sample/wpf_test` · `sample/winforms_test` 에서 `dotnet build -c Release`.
네이티브 라이브러리가 x64 라 프로세스도 x64 여야 합니다 (WPF 샘플도 x64 고정).
DLL 과 에셋은 빌드 후 실행 파일 옆으로 자동 복사됩니다.

## 조작

데스크톱은 명령행에 명령을 타이핑합니다 (`line`, `box`, `dim` …).
모바일은 **가상 커서 + [선택] 버튼** 방식이라 데스크톱 도구를 그대로 씁니다.

| | 1손가락 | 2손가락 | 3손가락 | 핀치 |
|---|---|---|---|---|
| 평소 | 이동 | 궤도회전 | 궤도회전 | 줌 |
| 도구 진행 중 | 커서 이동 | 이동 | 궤도회전 | 줌 |

값 입력은 숫자패드로 합니다. 접두가 없으면 상대 좌표, `=` 는 절대 좌표,
값 하나만 치면 커서 방향으로의 거리입니다 (예: `3000`, `1500,800`, `=1500,800`).

## 라이선스

이 저장소와 VulkanCAD SDK 는 **MIT 라이선스**입니다 ([LICENSE](LICENSE)). 무료로 사용·수정할 수 있고,
커스텀해서 상업적으로 판매해도 됩니다. 저작권 고지만 유지하면 됩니다.

`sdk/` 에 동봉된 서드파티 라이브러리와 에셋(MoltenVK, Manifold, ImGui, 폰트, 샘플 모델 등)은
각자의 라이선스를 따릅니다. 재배포 시 [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) 를
함께 포함하세요. 일부 샘플 모델은 라이선스가 불명확해 배포 전 제거를 권장합니다 (문서 참조).
