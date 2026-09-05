# VulkanCAD 샘플

[VulkanCAD 엔진](https://github.com/kimyuheon/Vulkan_CMake)을 여러 UI 프레임워크에서 호스팅하는 예제 모음입니다.
**엔진 소스 없이** 이 레포만 클론하면 빌드됩니다 — 필요한 것은 `sdk/` 안에 다 들어 있습니다.

```
3dEngine_Sample/
├── sdk/                    ← 엔진 배포본 (빌드에 필요한 전부)
│   ├── include/            VulkanCAD_API.h — 공개 C API
│   ├── lib/                공유 라이브러리 (.dylib / .so / .dll + .lib)
│   └── shaders/ models/ textures/ fonts/    런타임 에셋
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
| qml_test | `cmake -B build && cmake --build build` | `-DVULKANCAD_ENGINE_BUILD=<경로>` |
| mfc_test / mfc_dlg_test | Visual Studio | `EngineOut` (vcxproj) |
| wpf_test | `dotnet build` | `CadCoreBuildDir` (csproj) |
| swift_api_test | `swift build` | `Package.swift` 의 `sdkDirectory` |
| swift_ios_test | Xcode | `project.yml` 의 라이브러리 검색 경로 |
| android_test | `./gradlew assembleDebug` | `jniLibs/` |

### 런타임 에셋

DLL만으로는 동작하지 않습니다. `shaders/` 가 없으면 파이프라인 생성부터 실패합니다.
Windows 샘플은 빌드 후 실행 파일 옆으로 자동 복사하고, macOS/Linux 는 rpath 로 `sdk/` 를 직접 참조합니다.

## 플랫폼별 준비물

- **공통**: Vulkan 드라이버 (Windows/Linux) 또는 MoltenVK (macOS/iOS)
- **Windows**: Visual Studio 2022, .NET 8 (WPF)
- **Qt**: Qt 6
- **Android**: NDK, Android Studio 번들 JDK

## 조작

데스크톱은 명령행에 명령을 타이핑합니다 (`line`, `box`, `dim` …).
모바일은 **가상 커서 + [선택] 버튼** 방식이라 데스크톱 도구를 그대로 씁니다.

| | 1손가락 | 2손가락 | 3손가락 | 핀치 |
|---|---|---|---|---|
| 평소 | 이동 | 궤도회전 | 궤도회전 | 줌 |
| 도구 진행 중 | 커서 이동 | 이동 | 궤도회전 | 줌 |

값 입력은 숫자패드로 합니다. 접두가 없으면 상대 좌표, `=` 는 절대 좌표,
값 하나만 치면 커서 방향으로의 거리입니다 (예: `3000`, `1500,800`, `=1500,800`).
