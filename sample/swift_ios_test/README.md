# VulkanCAD iOS Swift Sample

iOS Simulator / 실기에서 Vulkan CAD 엔진을 띄우는 SwiftUI 데모.
macOS 의 `swift_api_test/VulkanCADSwiftNativeViewTest` 를 iOS 로 미러링.

엔진 코어는 CMake 로 정적 라이브러리(`libVulkanCADCoreStatic.a`) 로 빌드하고,
이 Swift 앱이 그걸 링크해서 실행한다. Xcode 프로젝트(`.xcodeproj`) 는 `project.yml`
기반으로 **xcodegen** 이 생성한다 (git 미포함 — 각 머신에서 재생성).

---

## 1회 셋업

### 전제

```bash
# 1) Xcode 활성 (Command Line Tools 아님)
xcode-select -p          # → /Applications/Xcode.app/Contents/Developer
                         # 아니면: sudo xcode-select -s /Applications/Xcode.app/Contents/Developer

# 2) 도구
brew install xcodegen cmake

# 3) 외부 의존성 — 레포 부모 폴더에 ../VulkanSdk 가 있어야 함 (git 미포함)
```

### 엔진 정적 라이브러리 빌드

> ⚠️ `cmake -B` (configure) 는 머신마다 1회 필수. 폴더가 없으면 `cmake --build` 가 실패한다.
> 설정(iphoneos vs iphonesimulator)은 configure 시점에 폴더의 CMakeCache.txt 에 기록된다.

#### 방법 1 — `build_ios.sh` (권장)

configure + 빌드를 한 번에 처리한다. **새 .cpp/.h 를 추가했을 때도 이걸 쓰면 GLOB 이 갱신**된다.

```bash
./build_ios.sh sim       # 시뮬레이터만  → build-ios-sim
./build_ios.sh device    # 실기만        → build-ios-device
./build_ios.sh           # 양쪽 다
```

**엔진 루트(`3dEngine/`) 와 이 샘플 폴더 어느 쪽에서 실행해도 된다** — 샘플 폴더의
`build_ios.sh` 는 엔진 루트 원본을 호출하는 래퍼라 결과(`build-ios-sim` / `build-ios-device`)는
항상 엔진 루트에 생긴다. 빌드 옵션을 바꿀 일이 있으면 **엔진 루트 쪽 원본**을 고칠 것.

#### 방법 2 — cmake 직접

**시뮬레이터용** (`build-ios-sim`):
```bash
cd 3dEngine
cmake -B build-ios-sim -G Xcode \
    -DCMAKE_SYSTEM_NAME=iOS \
    -DCMAKE_OSX_SYSROOT=iphonesimulator \
    -DCMAKE_OSX_ARCHITECTURES=arm64 \
    -DCMAKE_OSX_DEPLOYMENT_TARGET=15.0
cmake --build build-ios-sim --target VulkanCADCore
```

**실기용** (`build-ios-device`) — iPhone 에 올릴 때만:
```bash
cd 3dEngine
cmake -B build-ios-device -G Xcode \
    -DCMAKE_SYSTEM_NAME=iOS \
    -DCMAKE_OSX_SYSROOT=iphoneos \
    -DCMAKE_OSX_ARCHITECTURES=arm64 \
    -DCMAKE_OSX_DEPLOYMENT_TARGET=15.0
cmake --build build-ios-device --target VulkanCADCore
```

`project.yml` 이 SDK 별로 자동 분기한다:
- 시뮬레이터 빌드 → `build-ios-sim/Debug-iphonesimulator` lib + MoltenVK `ios-arm64_x86_64-simulator`
- 실기 빌드 → `build-ios-device/Debug-iphoneos` lib + MoltenVK `ios-arm64`

### Xcode 프로젝트 생성

```bash
cd 3dEngine/samples/swift_ios_test
xcodegen generate
open VulkanCADSwiftiOSTest.xcodeproj
```

---

## 시뮬레이터 실행

### Xcode 로
1. Xcode 상단 디바이스 = **iPhone 17** (또는 arm64 시뮬레이터)
2. **⌘R**

### 명령줄로 (Xcode 안 열고)

```bash
cd 3dEngine/samples/swift_ios_test
DEV="iPhone 16 Pro"                       # xcrun simctl list devices available 로 확인
BUNDLE="com.vulkancad.iossample"

# 1) 앱 빌드
xcodebuild -project VulkanCADSwiftiOSTest.xcodeproj -scheme VulkanCADSwiftiOSTest \
           -sdk iphonesimulator -configuration Debug \
           -destination "platform=iOS Simulator,name=$DEV" build

# 2) 시뮬레이터 부팅 + 설치 + 실행
xcrun simctl boot "$DEV"; open -a Simulator
APP=$(find ~/Library/Developer/Xcode/DerivedData -name "VulkanCADSwiftiOSTest.app" \
      -path "*Build/Products/Debug-iphonesimulator*" | grep -v Index.noindex | head -1)
xcrun simctl install "$DEV" "$APP"
xcrun simctl launch --console "$DEV" "$BUNDLE"     # --console 로 엔진 로그까지 확인
```

> `find` 에서 `Index.noindex` 경로를 걸러야 한다 — 그건 인덱싱용 산출물이라
> Bundle ID 가 없어 `simctl install` 이 "Missing bundle ID" 로 실패한다.

화면 캡처:
```bash
xcrun simctl io "$DEV" screenshot /tmp/shot.png
```

## 실기(iPhone) 실행

> 무료 Personal Team 으로 가능. 단 앱이 7일 후 만료 → 재설치 필요.

1. **iPhone 준비** (1회)
   - USB 연결 → "이 컴퓨터 신뢰"
   - Settings → Privacy & Security → **Developer Mode 켜기** → 재시작

2. **Xcode 사인** (1회)
   - 프로젝트 → TARGETS → VulkanCADSwiftiOSTest → **Signing & Capabilities**
   - "Automatically manage signing" 체크
   - **Team** → 본인 Apple ID (Personal Team). 없으면 "Add an Account…" 로 로그인
   - Bundle ID 충돌 시 `com.본인이름.vulkancad` 등 고유값으로 변경
     (영구 반영하려면 `project.yml` 의 `PRODUCT_BUNDLE_IDENTIFIER` 수정 후 `xcodegen generate`)

3. **실행**
   - Xcode 디바이스 = 본인 iPhone → ⌘R
   - iPhone "신뢰 안된 개발자" → Settings → General → VPN & Device Management → 신뢰 → 다시 ⌘R

---

## 엔진 코드 수정 후 재빌드 규칙

**C++ 엔진 수정 시 → 해당 타겟 lib 을 먼저 재빌드한 뒤 Xcode ⌘R.**

```bash
# 시뮬레이터로 작업 중이면
cmake --build build-ios-sim --target VulkanCADCore
# 실기로 작업 중이면
cmake --build build-ios-device --target VulkanCADCore
```

> Swift 코드만 바꿀 땐 Xcode ⌘R 만 하면 됨 (lib 재빌드 불필요).
> 새 C API 함수를 추가했는데 "Undefined symbol" 이 나면 = lib 재빌드 안 한 것.

---

## ⚠️ 엔진 수정 후 iOS 가 깨질 때

iOS 는 **ImGui 가 없고**(`LOT_NO_IMGUI`) 데스크톱 UI 파일들이 빌드에서 제외된다
(`lot_window` / `lot_file_dialog` / `lot_main_menu` / `lot_status_bar` / `lot_ui_manager` /
`lot_toolbar` / `lot_properties_panel` / `ui_input_capture`). 그래서 **데스크톱은 멀쩡한데
iOS 만 깨지는** 일이 잦다. 증상별 대응:

| 증상 | 원인 | 대응 |
|------|------|------|
| `fatal error: 'imgui.h' file not found` | 그 파일이 **가드 없이** ImGui 를 include | `#ifndef LOT_NO_IMGUI` 로 감싸고 `#ifdef LOT_NO_IMGUI` 쪽에 no-op 구현 (`lot_dimension_panel.cpp` 가 표준 예시) |
| `out-of-line definition of 'render' does not match any declaration` | 데스크톱 UI 헤더 **시그니처가 바뀌었는데** `ios_stubs.cpp` 가 옛 시그니처 | 헤더 선언과 1:1 로 맞춰 `ios_stubs.cpp` 수정 |
| 제외된 파일의 심볼 `Undefined symbol` (`saveModelFileDialog`, `openImageFileDialog` 등) | 제외 목록 파일이 제공하던 함수가 새로 **호출되기 시작** | `ios_stubs.cpp` 에 no-op 스텁 추가 — **실제로 undefined 로 뜬 것만** (안 뜬 걸 넣으면 중복 심볼) |
| `Library 'manifold' not found` (실기) | `build-ios-device` 가 오래돼 manifold 가 없음 | `./build_ios.sh device` 로 device 쪽 lib + manifold 재생성 |
| 새 `.cpp` 를 추가했는데 링크 에러 | CMake GLOB 이 안 갱신됨 | `./build_ios.sh sim`(또는 `device`) — 스크립트가 configure 부터 다시 함 |

> 에러는 한 번에 다 안 나온다. 고치고 다시 빌드 → 다음 에러 → 반복.
> 데스크톱 빌드(`cmake --build build`)도 같이 돌려 회귀가 없는지 확인할 것.

---

## 조작 (터치)

| 제스처 | 동작 |
|--------|------|
| 1손가락 드래그 (빈 곳) | 카메라 궤도 회전 |
| 1손가락 드래그 (기즈모 핸들 위) | 선택 객체 이동 (기즈모) |
| 2손가락 드래그 | 카메라 평행 이동 (pan) |
| 핀치 | 줌 |
| 탭 | 객체 선택 |

> 시뮬레이터에선 ⌥(Option) 키로 멀티터치(2손가락/핀치) 흉내.

---

## 현재 동작 / 남은 작업

**동작함**: 데모 씬 렌더, 터치 카메라 조작, 객체 선택, 이동 기즈모(MOVE).

**남은 작업**:
- 회전/축척 기즈모 모드 전환 (키보드 R/S 대신 SwiftUI 버튼 필요)
- SwiftUI 메뉴/패널 (스케치 도구, 속성 등)
- OSnap 인디케이터는 도구 활성 시에만 표시 (현재 트리거 UI 없음)

---

## 파일 구조

```
swift_ios_test/
├── README.md
├── project.yml                                     ← xcodegen 입력
├── Info.plist                                      ← xcodegen 이 생성/관리 (.gitignore)
└── Sources/
    ├── CVulkanCAD/module.modulemap                 ← Swift → C API 브릿지
    └── VulkanCADSwiftiOSTest/
        ├── VulkanCADApp.swift                       ← @main SwiftUI entry
        ├── ContentView.swift                        ← 본문 + CADisplayLink 렌더 루프 + scenePhase
        ├── VulkanCADViewRepresentable.swift         ← SwiftUI ↔ UIKit 어댑터
        ├── VulkanCADMetalView.swift                 ← UIView + CAMetalLayer + 제스처
        └── VulkanCADEngine.swift                    ← C API Swift 래퍼
```

> `build-ios-sim/`, `build-ios-device/`, `VulkanCADSwiftiOSTest.xcodeproj/`, `Info.plist`,
> `build_xcode/` 는 전부 .gitignore — 각 머신에서 cmake / xcodegen 으로 재생성.
