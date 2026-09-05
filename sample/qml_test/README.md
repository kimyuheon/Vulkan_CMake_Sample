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

1. Qt 6.4 이상에서 Quick, QML, Controls 모듈을 설치합니다.
2. 레포 루트에서 공유 엔진을 먼저 빌드합니다.

```bash
cmake -B build
cmake --build build --target VulkanCADCoreShared
```

Ubuntu 24.04의 Qt 패키지 예시:

```bash
sudo apt install qt6-base-dev qt6-declarative-dev \
    qml6-module-qtquick qml6-module-qtquick-controls \
    qml6-module-qtquick-templates qml6-module-qtquick-layouts \
    qml6-module-qtquick-window qml6-module-qt-labs-platform
```

`qt-labs-platform`은 풀다운 메뉴용입니다. Windows/macOS 공식 Qt 설치본에는 기본 포함이고, 데비안 계열만 패키지가 쪼개져 있어 따로 설치합니다.

## Qt Creator에서 실행

1. Qt Creator에서 이 폴더의 `CMakeLists.txt`를 엽니다.
2. Desktop Qt 6.4 이상 Kit를 선택합니다.
3. CMake 옵션 `VULKANCAD_ENGINE_BUILD`가 엔진의 `build` 폴더인지 확인합니다.
4. `qml_host`를 시작 프로젝트로 선택하고 실행합니다.

기본 경로는 레포 루트의 `build`입니다. 별도 빌드 폴더라면 Qt Creator의 CMake Configuration에 다음 값을 추가합니다.

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
cd samples/qml_test
cmake -S . -B build -DVULKANCAD_ENGINE_BUILD=../../build
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
