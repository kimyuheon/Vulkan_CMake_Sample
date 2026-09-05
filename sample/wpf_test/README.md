# VulkanCAD — WPF 호스트 샘플 (.NET 8)

WPF 창 안에 VulkanCAD 엔진을 임베드하는 최소 샘플. `VulkanCADCore.dll` 의 C API 를
P/Invoke 로 호출하고, `HwndHost` 로 만든 자식 HWND 에 Vulkan 이 렌더한다.

## 구조

| 파일 | 역할 |
|------|------|
| `CadApi.cs` | `VulkanCADCore.dll` 의 `CAD_*` P/Invoke 선언 |
| `VulkanHost.cs` | `HwndHost` — 자식 HWND 생성 → `CAD_AttachView` → `CAD_CreateEngine`, 마우스/키 라우팅 |
| `MainWindow.xaml(.cs)` | 툴바(생성/편집/뷰) + 렌더 영역 + `CompositionTarget.Rendering` → `CAD_Tick` |

## 핵심 계약 (엔진 측)

```
CAD_AttachView(hwnd, w, h)   // 렌더할 HWND 를 먼저 등록
CAD_CreateEngine()           // 그 HWND(Win32NativeViewWindow)에 Vulkan 렌더
CAD_Tick()                   // 매 프레임 1회
CAD_ResizeView(w, h)         // 크기 변경 시
CAD_OnMouse* / CAD_OnKeyDownVK   // 입력 (VK 코드는 내부에서 GLFW 로 변환)
```

## 빌드 & 실행

1. C++ 엔진 DLL 먼저 빌드 (한 번):
   ```
   cmake --build build --config Debug --target VulkanCADCoreShared
   ```
   → `build/Debug/VulkanCADCore.dll` + `build/Debug/{shaders,models,textures,fonts}/`

2. WPF 샘플 빌드 (post-build 이 DLL+에셋을 출력 폴더로 자동 복사):
   ```
   cd samples/wpf_test
   dotnet build -c Debug
   ```
   - `VulkanCadWpf.csproj` 의 `CadCoreBuildDir` 가 `..\..\build\Debug` 를 가리킴. 경로 다르면 조정.

3. 실행:
   ```
   dotnet run -c Debug
   ```
   또는 `bin/Debug/net8.0-windows/VulkanCadWpf.exe`.

## 주의

- **x64 고정**: 네이티브 DLL 이 x64 이므로 `PlatformTarget=x64`. AnyCPU 로 두면 BadImageFormat.
- `vulkan-1.dll` 은 GPU 드라이버가 System32 에 설치 (재배포 불필요).
- 런타임 에셋(`shaders/` 등)이 exe 옆에 없으면 엔진 생성 실패 → 상태바에 표시.
- **Airspace**: HwndHost 자식 창이 WPF 컨텐츠 위에 떠서, 렌더 영역 위에 WPF 컨트롤을
  겹칠 수 없다(팝업/오버레이는 별도 처리 필요).

## 미구현 / 다음 단계

- **엔진 → WPF 콜백** (`CAD_SetOnSelectionChanged` 등) 없음 → 속성패널 양방향 연동 시 추가 필요.
- 마우스 modifiers(Ctrl/Shift) 전달은 현재 0 고정 — 다중선택 등 필요 시 비트 채우기.
