# Swift (macOS) 샘플

`sdk/libVulkanCADCore.dylib` 를 C API 로 부르는 macOS 샘플입니다. 실행 파일이 둘입니다.

| 타깃 | 내용 |
|------|------|
| `VulkanCADSwiftApiTest` | 콘솔. 엔진이 자기 GLFW 창을 만들고, API 몇 개를 호출해 보는 최소 예제 |
| `VulkanCADSwiftNativeViewTest` | AppKit 앱. `NSView` 를 `CAD_AttachView()` 로 엔진에 넘기고, **풀다운 메뉴 · 리본 · 도킹 도구모음 · 명령행**을 네이티브로 꾸민 CAD 스타일 창 |

```bash
cd sample/swift_api_test
swift run VulkanCADSwiftNativeViewTest     # AppKit 앱
swift run VulkanCADSwiftApiTest            # 콘솔
```

`Package.swift` 가 `../../sdk` 를 링크·rpath 경로로 넣으므로 따로 복사할 것이 없습니다.
런타임 에셋(`models/ textures/ fonts/`)도 `sdk/` 에서 읽습니다 — `CAD_SetRuntimeAssetPath()` 를
`CAD_CreateEngine()` 앞에 부릅니다.

## 네이티브 앱 구성

```
┌ 풀다운 메뉴 — 파일 · 편집 · 뷰 · 그리기 · 수정 · 솔리드 · 재질 · 창 · 도움말 ┐
│ 리본 — 홈 / 그리기 / 수정 / 솔리드 / 뷰 탭, 탭마다 그룹                        │
├──────┬──────────────────────────────────────────────────────────────────────┤
│ 도구 │ Vulkan 뷰                                                            │
│ 모음 │                                                                      │
├──────┴──────────────────────────────────────────────────────────────────────┤
│ 명령: [ line ]  첫 번째 점을 지정                                  선택 2    │
└─────────────────────────────────────────────────────────────────────────────┘
```

| 파일 | 역할 |
|------|------|
| `ToolCatalog.swift` | **명령 정의 한 벌.** 메뉴·리본·도구모음이 전부 이걸 참조한다. 엔진 툴바(기본·2D·3D)의 구성을 옮겼고, 버튼이 부르는 이름은 엔진 명령행 별칭과 같다(`line`, `rec`, `pp` …) |
| `MainMenu.swift` | 풀다운 메뉴. 단축키(⌘Z, ⇧⌘Z, ⌘A, ⌘N, ⌘O, ⌘S, ⌘1~4 …) 포함 |
| `RibbonView.swift` | 탭 + 그룹 리본. 창이 좁으면 가로 스크롤 |
| `DockToolbarView.swift` | 도킹 도구모음. 표준·그리기·수정·솔리드·뷰·탐색·기즈모·재질 8개 띠가 **각각** 좌/우 도킹·숨김된다. 손잡이를 창 반대편으로 끌거나 우클릭, 또는 뷰 > 도구모음 메뉴. 배치는 UserDefaults 에 기억 |
| `CommandBarView.swift` | 하단 명령행 + 프롬프트. ↑↓ 이전 명령, Esc 취소 |
| `AppDelegate.swift` | 창 조립, 도구 실행 입구(`runTool`), 파일 대화상자, 메뉴 활성/체크 상태, 플러그인 UI 붙이기 |
| `VulkanCADEngine.swift` | C API 래퍼. 콜백(프롬프트·선택·문서 변경)은 전역 보관소를 거쳐 Swift 클로저로 |
| `VulkanCADView.swift` | 마우스·키보드를 엔진 좌표/키코드로 변환해 전달 |

### 버튼 하나가 엔진까지 가는 길

```
NSMenuItem / NSButton  ──▶  AppDelegate.runTool(_:)  ──▶  ToolCatalog.byId[id].run(app)
                                                            ├─ cmd(...)  → CAD_ExecuteCommand("line")
                                                            └─ api(...)  → CAD_Undo(), NSOpenPanel + CAD_OpenDocument() …
```

- **엔진 명령으로 되는 것**은 `CAD_ExecuteCommand` 하나로 부릅니다(명령 200개 이상). 새 버튼을 달 때
  API 를 늘릴 필요가 없습니다.
- **대화상자가 필요한 것**(열기·저장)은 엔진 명령 `open`/`save` 가 ImGui 대화상자라 임베드에선 못 쓰므로
  `NSOpenPanel`/`NSSavePanel` 을 띄우고 `CAD_OpenDocument` / `CAD_OpenFile` / `CAD_SaveAs` 를 부릅니다.
- **Undo 가능 여부, 격자 켜짐, 도킹 위치** 같은 메뉴 상태는 `validateMenuItem` 이 열 때마다 계산합니다.

### 플러그인 항목

플러그인이 `CAD_AddUiItem` 으로 올린 메뉴·툴바·리본 항목을 엔진 생성 뒤 `CAD_GetUiItem*` 로 읽어
같은 자리에 덧붙입니다(`AppDelegate.installPluginUi`). 경로 `"건축/벽체"` 는 메뉴에선
상위/하위 메뉴, 리본에선 탭/그룹이 됩니다. 도움말 > 플러그인 폴더 불러오기로 실행 중에 더 올릴 수 있습니다.

## 입력 전달

- 마우스: 좌/우/중 버튼 누름·뗌·드래그, 이동, 휠
- 키보드: Esc, Delete, Enter, 방향키, 숫자 1~4(시점), 영문 및 출력 가능한 ASCII
- ⌘ 조합은 메뉴 단축키가 먼저 받습니다. 뷰에 포커스가 있을 때 숫자·문자 키는 엔진으로 갑니다.

## 참고

- `building for macOS-14.0, but linking with dylib ... built for newer version` 경고는 패키지 배포 타깃과
  dylib 빌드 SDK 가 달라서입니다. 링크는 되며, 배포 시엔 맞추는 게 좋습니다.
- `Cannot index window tabs due to missing main bundle identifier` 는 `.app` 번들이 아닌 SwiftPM 실행 파일로
  띄워서 나오는 AppKit 경고입니다.
