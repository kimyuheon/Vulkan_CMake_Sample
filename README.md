# VulkanCAD 샘플

[VulkanCAD 엔진](https://github.com/kimyuheon/Vulkan_CMake)을 여러 UI 프레임워크에서 호스팅하는 예제 모음입니다.
**엔진 소스 없이** 이 레포만 클론하면 빌드됩니다 — 필요한 것은 `sdk/` 안에 다 들어 있습니다.

```
3dEngine_Sample/
├── sdk/                    ← 엔진 배포본 (빌드에 필요한 전부)
│   ├── include/            VulkanCAD_API.h — 공개 C API
│   ├── libVulkanCADCore.*  공유 라이브러리 (.dylib / .so / .dll + .lib)
│   ├── lib-ios-sim/        iOS 정적 라이브러리 (시뮬레이터) — Releases 에서 받음
│   ├── lib-ios-device/     iOS 정적 라이브러리 (실기)       — Releases 에서 받음
│   └── models/ textures/ fonts/    런타임 에셋
└── sample/
    ├── cpp_api_test/       C++ 콘솔 — API 최소 예제
    ├── mfc_test/           MFC (SDI)
    ├── mfc_dlg_test/       MFC (다이얼로그)
    ├── wpf_test/           WPF (C# P/Invoke)
    ├── qml_test/           Qt / QML
    ├── swift_api_test/     Swift (macOS, SwiftUI + AppKit)
    ├── swift_ios_test/     Swift (iOS)
    └── android_test/       Android (Kotlin + JNI)
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
| mfc_test / mfc_dlg_test | Visual Studio | `EngineOut` (vcxproj) |
| wpf_test | `dotnet build` | `CadCoreBuildDir` (csproj) |
| swift_api_test | `swift build` | `Package.swift` 의 `buildDirectory` |
| swift_ios_test | `xcodegen` → Xcode | `project.yml` 의 라이브러리 검색 경로 |
| android_test | `./gradlew assembleDebug` | `jniLibs/` |

### 플랫폼별 라이브러리 배치

**라이브러리는 `sdk/` 바로 아래**, 에셋과 같은 층에 둡니다. 샘플들이 "라이브러리가 있는 폴더
= 에셋 폴더"를 전제하기 때문입니다 (qml_test 는 그 경로를 컴파일에 박고, mfc_test 는 거기서
DLL 과 `models/` 를 같이 꺼내 실행 파일 옆으로 복사합니다).

| OS | 파일 | 실행 시 라이브러리를 찾는 법 |
|----|------|------------------------------|
| Windows | `VulkanCADCore.dll` + `VulkanCADCore.lib` | rpath 가 없어 **exe 옆에 복사**해야 함 (샘플이 자동으로 함) |
| Linux | `libVulkanCADCore.so` | rpath 로 `sdk/` 를 직접 참조 — 복사 불필요 |
| macOS | `libVulkanCADCore.dylib` | 동일 (`@rpath`) |

Windows 는 링크에 import library(`.lib`)가 따로 필요합니다. 엔진을 Windows 에서 빌드하면
`.dll` 과 함께 `sdk/` 에 들어갑니다.

### iOS 정적 라이브러리는 별도 내려받기

iOS 는 공유 라이브러리를 쓸 수 없어 정적 라이브러리를 링크하는데, 하나가 **216MB** 라
GitHub 파일 한도(100MB)를 넘습니다. 그래서 레포에 없고 **Releases** 에 올려 둡니다.

```bash
# Releases 에서 받아 압축을 풀면 아래 두 폴더가 채워진다
sdk/lib-ios-sim/      libVulkanCADCoreStatic.a  libmanifold.a
sdk/lib-ios-device/   libVulkanCADCoreStatic.a  libmanifold.a
```

iOS 샘플을 안 쓰신다면 받지 않아도 됩니다. 나머지 샘플은 레포만으로 빌드됩니다.

### 런타임 에셋

라이브러리만으로는 동작하지 않습니다 — `fonts/` 가 없으면 치수·문자가 아예 생성되지 않습니다
(글리프 메시를 만들어야 해서 폰트가 없으면 `createDimension` 이 0 을 반환합니다).

**셰이더는 없습니다.** SPIR-V 는 라이브러리에 내장돼 있어 별도 파일이 필요 없습니다.

## 플랫폼별 준비물

| | 필요한 것 |
|---|---|
| **공통** | Vulkan 드라이버 (Windows/Linux) 또는 MoltenVK (macOS/iOS) |
| **Windows** | Visual Studio 2022 (MFC 워크로드), .NET 8 SDK (WPF) |
| **Ubuntu** | `build-essential cmake libvulkan-dev vulkan-tools` |
| **Qt** | Qt 6 (`qml_test`) |
| **macOS/iOS** | Xcode 15+, `xcodegen` (iOS), Vulkan SDK(MoltenVK) |
| **Android** | NDK, Android Studio 번들 JDK |

### Ubuntu

```bash
sudo apt install build-essential cmake libvulkan-dev vulkan-tools
vulkaninfo | head            # 드라이버가 잡히는지 확인
cd sample/cpp_api_test && cmake -B build && cmake --build build
./build/cpp_api_test
```

Qt 샘플은 `qt6-base-dev qt6-declarative-dev` 가 추가로 필요합니다.

### Windows

Visual Studio 에서 `sample/mfc_test/VulkanCadMfc.sln` 을 열고 **x64** 로 빌드합니다.
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
