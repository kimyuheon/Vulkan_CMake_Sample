# VulkanCAD — MFC 호스트 샘플

MFC(다이얼로그식, 코드 생성) 창 안에 VulkanCAD 엔진을 임베드하는 최소 샘플.
`VulkanCADCore.dll` 의 C API 로 엔진을 붙이고, 상단 버튼 스트립(툴바)으로 명령을 보낸다.
WPF/Android/iOS 호스트와 **같은 C API 임베드 패턴** (플랫폼 창 코드만 다름).

## 구조

```
CMainWnd (CFrameWnd, 코드 생성)
├── 상단 버튼 스트립: 큐브 / 전체선택 / 삭제 / 줌 / Iso / Undo  → CAD_Request* / CAD_Undo
└── CRenderPane (자식 CWnd)
       HWND → CAD_AttachView(hwnd, w, h) → CAD_CreateEngine()   (엔진이 이 창에 Vulkan 렌더)
       WM_TIMER(16ms) → CAD_Tick()                              (~60fps)
       WM_SIZE        → CAD_ResizeView(w, h)
       마우스         → CAD_OnMouseDown/Up/Move/Wheel
       종료           → CAD_DetachView() → CAD_DestroyEngine()
```

- `main.cpp` 한 파일 — App / MainWnd / RenderPane 전부. .rc 리소스 불필요(코드 생성).
- 좌클릭=선택, 우클릭 드래그=궤도 회전, 중간클릭=팬, 휠=줌 (엔진 기본 매핑).

## 전제

- **Visual Studio 2022 + "C++를 사용한 데스크톱 개발" + MFC 구성요소**
- **엔진 DLL 먼저 빌드** — 루트에서 `VulkanCADCoreShared` (→ `build/Debug/VulkanCADCore.dll` + `.lib`)
  ```
  cmake -B build && cmake --build build --config Debug --target VulkanCADCoreShared
  ```
  (셰이더 `.spv` / 모델 / 폰트 / 텍스처도 `build/Debug/` 에 생성됨)

## 빌드 & 실행

- **Visual Studio**: `VulkanCadMfc.sln` 열기 → x64 Debug → F5
- **명령줄**:
  ```
  msbuild VulkanCadMfc.vcxproj /p:Configuration=Debug /p:Platform=x64
  x64\Debug\VulkanCadMfc.exe
  ```

빌드 후 **PostBuild** 가 `build/Debug` 의 `VulkanCADCore.dll` + `shaders/models/fonts/textures` 를
출력 폴더(`x64\Debug`)로 자동 복사한다. (엔진은 실행 폴더 기준 상대경로로 에셋 로드)

## 주의

- **x64 전용** — 엔진 DLL 이 x64 라 MFC 앱도 x64.
- 소스에 한글 문자열이 있어 `/utf-8` 컴파일 옵션 사용(vcxproj 에 설정됨) — CP949 오독 방지.
- `UseOfMfc=Dynamic` (MFC 공유 DLL). 개발 PC 엔 MFC 런타임이 있으므로 별도 배포 불필요.

## 검증 현황

- ✅ MSBuild x64 Debug 빌드 클린, exe 생성 + DLL/에셋 자동 복사
- ✅ 실행 시 엔진이 MFC 창에 붙어 렌더(크래시 없음)

## 남은 것 / 확장 아이디어

- 스케치 도구 버튼(선/원/호/폴리곤), 뷰 전환(Front/Top/Right), 투영 토글
- 엔진 → 호스트 콜백(선택 변경 알림 등)으로 상태바 갱신
- 본격 CAD UI 원하면 SDI Doc/View + 도킹/리본으로 확장
