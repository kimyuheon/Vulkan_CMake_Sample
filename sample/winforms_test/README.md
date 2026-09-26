# VulkanCAD — WinForms 호스트 샘플 (.NET 8)

WinForms 창 안에 VulkanCAD 엔진을 임베드하는 최소 샘플. `wpf_test` 와 같은 화면·같은 기능이고,
`VulkanCADCore.dll` 의 C API 를 P/Invoke 로 호출한다. `CadApi.cs` 는 WPF 샘플과 같은 파일(네임스페이스만 다름).

WPF 와 다른 점은 하나 — WinForms 컨트롤은 처음부터 자기 HWND 가 있어서 `HwndHost` 로 자식 창을 따로 만들 필요 없이
**컨트롤의 `Handle` 을 그대로 엔진에 넘긴다.**

## 구조

| 파일 | 역할 |
|------|------|
| `CadApi.cs` | `VulkanCADCore.dll` 의 `CAD_*` P/Invoke 선언 (wpf_test 와 동일) |
| `VulkanPanel.cs` | 렌더 영역 `Control` — `OnHandleCreated` 에서 `CAD_AttachView(Handle)` → `CAD_CreateEngine`, `WndProc` 에서 마우스/키 라우팅 |
| `MainForm.cs` | 메뉴 · 툴바(그리기/편집/뷰) · 왼쪽 정보 · 상태바 (디자이너 없이 코드로) + 유휴 루프 → `CAD_Tick` |
| `Program.cs` | 진입점 (`ApplicationConfiguration.Initialize` — 고DPI PerMonitorV2) |

## 핵심 계약 (엔진 측)

```
CAD_SetRuntimeAssetPath(exe 폴더)   // 엔진 생성 전
CAD_AttachView(hwnd, w, h)          // 렌더할 HWND 를 먼저 등록
CAD_CreateEngine()                  // 그 HWND 에 Vulkan 렌더
CAD_Tick()                          // 매 프레임 1회 (Application.Idle + PeekMessage 루프)
CAD_ResizeView(w, h)                // 크기 변경 시 (Tick 안에서 ClientSize 비교)
CAD_OnMouse* / CAD_OnKeyDownVK      // 입력 (VK 코드는 내부에서 GLFW 로 변환)
```

## 빌드 & 실행

```
cd sample/winforms_test
dotnet build -c Debug
dotnet run -c Debug
```

post-build 이 `../../sdk` 의 `VulkanCADCore.dll` + `models/ textures/ fonts/` 를 출력 폴더로 복사한다
(`CadCoreBuildDir` 로 경로 조정). 실행 파일은 `bin/Debug/net8.0-windows/VulkanCadWinForms.exe`.

## WinForms 에서 챙긴 것

- **배경 안 칠하기**: `ControlStyles.Opaque | UserPaint | AllPaintingInWmPaint`, 더블버퍼 끔 — 안 하면 GDI 가 Vulkan 화면을 덮어 깜빡인다.
- **키를 엔진으로**: `IsInputKey` 가 전부 `true` — 방향키·Tab(도구 옵션 순환)·ESC 가 폼의 포커스 이동으로 새지 않는다.
  툴바 버튼은 `TabStop=false` 이고 누른 뒤 포커스를 렌더 영역으로 돌린다.
- **버튼 크기**: `AutoSizeMode.GrowAndShrink` — 기본(GrowOnly)은 너비 75 아래로 안 줄어 툴바가 창 밖으로 넘친다.
- **도킹 순서**: 나중에 `Controls.Add` 한 것부터 자리를 잡는다 → 렌더(Fill)를 먼저, 메뉴를 마지막에.
- **x64 고정**: 네이티브 DLL 이 x64 이므로 `PlatformTarget=x64`. AnyCPU 로 두면 BadImageFormat.

## 미구현 / 다음 단계 (wpf_test 와 같음)

- 엔진 → UI 콜백(`CAD_SetOnSelectionChanged` 등)은 `CadApi.cs` 에 선언만 있고 연결 안 함.
- 마우스 modifiers(Ctrl/Shift) 는 0 고정 — 다중선택 등 필요 시 비트 채우기.
