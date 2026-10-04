# VulkanCAD — Qt6 크로스플랫폼 호스트 샘플

Qt6 QML 창에 VulkanCAD 엔진을 임베드하는 CMake 프로젝트입니다. Windows, Ubuntu(X11/XWayland), macOS에서 같은 Qt 소스를 사용하며 `VulkanCADCore` C API로 엔진을 연결합니다.

`run_qml.sh` 없이 Qt Creator에서 프로젝트를 열고 바로 구성·빌드·실행할 수 있습니다.

## 구조

```text
ApplicationWindow (QML)
├── 상단 ToolBar → CadCommands → VulkanCAD C API
├── 좌측 도구 패널
└── VulkanViewport (QQuickItem, C++)
    ├── 네이티브 자식 QWindow 생성
    ├── winId(HWND / XID / NSView) → CAD_AttachView
    ├── QTimer(16ms) → CAD_Tick
    └── Qt 입력 이벤트 → CAD_OnMouse* / CAD_OnKey*
```

Qt 6.4에서도 동작하도록 Vulkan을 QML Scene Graph에 직접 합성하지 않고 네이티브 자식 창에 렌더링합니다. 따라서 툴바와 패널은 3D 뷰포트 바깥에 배치합니다.

## 준비

**엔진 라이브러리는 레포에 들어 있지 않습니다**(`sdk/*.so|dll|dylib` 는 `.gitignore`).
헤더와 에셋만 들어 있으므로, 먼저 라이브러리를 `sdk/` 바로 아래에 채워야 합니다 — 둘 중 하나:

- 엔진 레포(`../3dEngine`)를 Release 로 빌드 → 엔진 CMake 가 `sdk/` 로 자동 복사
- 또는 GitHub Releases 에서 받아 `sdk/` 바로 아래에 두기

채우고 나면 CMake 설정은 따로 없습니다(`VULKANCAD_ENGINE_BUILD` 기본값이 `sdk/`).
그 밖에 필요한 패키지는 아래와 같습니다.

1. Qt 6.4 이상 — Quick, QML, Controls 모듈
2. Vulkan 헤더(`libvulkan-dev`) — Qt6 Gui 가 구성 단계에서 찾습니다
3. Vulkan 드라이버(ICD) — 예: `mesa-vulkan-drivers`.
   로더(`libvulkan.so.1`)는 `sdk/` 에 동봉돼 있지만 드라이버는 시스템 것이 필요합니다

Ubuntu 24.04 패키지 예시:

```bash
sudo apt install qt6-base-dev qt6-declarative-dev \
    qml6-module-qtquick qml6-module-qtquick-controls \
    qml6-module-qtquick-templates qml6-module-qtquick-layouts \
    qml6-module-qtquick-window qml6-module-qt-labs-platform \
    libvulkan-dev mesa-vulkan-drivers
```

`qt-labs-platform`은 풀다운 메뉴용입니다. Windows/macOS 공식 Qt 설치본에는 기본 포함이고, 데비안 계열만 패키지가 쪼개져 있어 따로 설치합니다.

## Qt Creator에서 실행

1. Qt Creator에서 이 폴더의 `CMakeLists.txt`를 엽니다.
2. Desktop Qt 6.4 이상 Kit를 선택합니다.
3. `qml_host`를 시작 프로젝트로 선택하고 실행합니다.

**라이브러리를 `sdk/` 에 채워 뒀다면 별도 설정은 없습니다.** `VULKANCAD_ENGINE_BUILD`
기본값이 레포 안 `sdk/` 를 가리킵니다. 비어 있으면 구성 단계에서 바로 오류가 납니다.

다른 위치의 SDK 를 쓰려면 Qt Creator 의 CMake Configuration 에 추가합니다. 단 **그 폴더에
`models/` `fonts/` `textures/` 도 함께 있어야 합니다** — 찾은 라이브러리의 폴더가 rpath 이자
런타임 에셋 경로가 되기 때문입니다.

```text
VULKANCAD_ENGINE_BUILD=/absolute/path/to/3dEngine/build
```

CMake가 다음 작업을 자동 처리합니다.

- 실제 공유 라이브러리가 있는 폴더(Debug/Release 포함)를 엔진 에셋 경로로 기록
- Linux/macOS의 공유 라이브러리 rpath 설정
- Windows의 `VulkanCADCore.dll`을 Qt 실행파일 옆으로 복사
- Linux에서 Qt xcb 백엔드 선택
- `VULKAN_SDK`가 설정돼 있으면 validation layer 경로 설정
- macOS에서 MoltenVK ICD 경로 설정

## 터미널에서 직접 실행

```bash
cd sample/qml_test
cmake -S . -B build
cmake --build build
./build/qml_host
```

Windows의 Visual Studio 생성기에서는 실행파일이 `build/Debug` 또는 `build/Release` 아래 생성될 수 있습니다.

`run_qml.sh`는 이전 Ubuntu 환경과의 호환을 위해 남겨 둔 선택 사항이며 필수 실행 경로가 아닙니다.

## 플랫폼 참고

- Windows: Qt `winId()`의 HWND를 엔진에 전달합니다.
- Ubuntu: 엔진의 X11 surface를 사용하므로 Wayland에서도 Qt가 XWayland/xcb로 실행됩니다.
- macOS: Qt `winId()`가 나타내는 NSView를 MoltenVK 엔진에 전달합니다.

## 조작

- 좌클릭: 선택
- 우클릭 드래그: 궤도 회전
- 중간 클릭 드래그: 이동
- 휠: 확대/축소
- 상단/좌측 버튼: 생성, 스케치, 뷰 전환, Undo/Redo
