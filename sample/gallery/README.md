# VulkanCAD 기능 갤러리 (C# — WinForms · WPF)

엔진 기능을 **기능 하나 = 파일 하나**로 보여 주는 앱입니다. 리본에서 기능을 고르면 장면이 그 기능의
예제로 바뀌고, 오른쪽 패널에 설명·핵심 API·조작 버튼·로그가 나옵니다. `▶ 시연` 을 누르면 핵심 동작이
차례로 실행됩니다.

WinForms 판과 WPF 판은 **같은 기능 코드**를 씁니다. 화면을 붙이는 방법만 다릅니다.

왼쪽에는 어떤 기능을 보든 **노드 트리**와 **도면층** 창이 늘 있습니다.
- 노드 트리: 파일 → glTF/FBX 노드 → 객체 → 피처 기록. 체크 = 보이기, 클릭 = 선택(Ctrl = 추가), F2 = 이름, Del = 삭제
- 도면층: 보이기 · 잠금 · 색 · 이름 · 불투명도 · 선종류 · 객체 수

엔진의 ImGui 패널은 호스트에 붙이면 뜨지 않습니다(임베드에선 ImGui 컨텍스트가 없다). 그래서 같은 내용을
`CAD_GetSceneTreeJson` · `CAD_GetLayersJson` 으로 읽어 호스트 컨트롤로 그리고, 장면이 바뀐 때만(`CAD_GetSceneRevision`) 다시 읽습니다.

## 실행

```bash
# 사전 준비: sdk/VulkanCADCore.dll (엔진 Release 빌드 또는 GitHub Releases) — 루트 README 참고
dotnet build sample/gallery/VulkanCAD.Gallery.sln -c Release

sample/gallery/winforms/bin/Release/net8.0-windows/VulkanCAD.Gallery.WinForms.exe
sample/gallery/wpf/bin/Release/net8.0-windows/VulkanCAD.Gallery.Wpf.exe
```

Visual Studio 에서는 `VulkanCAD.Gallery.sln` 을 열고 시작 프로젝트를 WinForms 또는 WPF 로 고릅니다.
빌드가 끝나면 엔진 DLL 과 `models/ textures/ fonts/ effects/ languages/` 가 exe 옆으로 복사됩니다.

## 구조

```
sample/gallery/
├── csharp/
│   ├── Gallery.Features/         ← 기능 모음 (화면을 모른다 — 두 갤러리가 같이 씀)
│   │   ├── Features/*.cs         기능 하나 = 파일 하나  ★ 여기를 보면 된다
│   │   ├── Feature.cs            기능의 틀 (Setup · Demo · Update · Leave)
│   │   ├── IFeatureUi            기능이 호스트에 요청하는 UI (버튼·슬라이더·로그…) — Feature.cs 안
│   │   ├── Scene.cs              기능을 바꿀 때 장면 초기화
│   │   ├── Scene/                노드 트리·도면층 창 모델과 동작 (두 호스트 공용)
│   │   ├── Cad.cs                C API 를 짧게 쓰는 도우미 (id 목록·오류 문구)
│   │   ├── FeatureCatalog.cs     기능 목록 = 리본 순서
│   │   └── GalleryRibbon.cs      리본 내용 (홈 탭 공통 명령 + 기능 탭)
│   └── CopySdkRuntime.targets    sdk/ → exe 옆 복사
├── winforms/                     ← WinForms 호스트
│   ├── VulkanPanel.cs            엔진을 컨트롤 HWND 에 붙이기 + 입력
│   ├── RibbonBar.cs              엔진 ImGui 리본과 같은 모양 (GDI+)
│   ├── Scene/                    왼쪽 패널 — 노드 트리(TreeView) · 도면층(DataGridView)
│   └── FeaturePanel.cs           IFeatureUi 구현
└── wpf/                          ← WPF 호스트
    ├── VulkanHost.cs             HwndHost 로 자식 HWND 를 만들어 엔진에 붙이기 + 입력
    ├── RibbonBar.cs              같은 리본 (OnRender)
    ├── Scene/                    왼쪽 패널 — 노드 트리(TreeView) · 도면층(줄 목록)
    └── FeaturePanel.cs           IFeatureUi 구현
```

C API 선언은 `sdk/csharp/*.cs` 를 그대로 포함합니다(엔진 빌드가 헤더에서 생성). **여러분의 앱도 같은 방법**이면 됩니다:

```xml
<ItemGroup>
  <Compile Include="경로\sdk\csharp\*.cs" />
</ItemGroup>
```

## 기능 목록

| 탭 | 기능 | 소스 | 핵심 API |
|----|------|------|----------|
| 만들기 | 2D 도형 | [Shapes2D.cs](csharp/Gallery.Features/Features/Shapes2D.cs) | `CAD_CreateLine` `CAD_CreateCircle` `CAD_CreateArc` `CAD_CreateRectangle` |
| 만들기 | 문자·치수·해치 | [TextAndAnnotation.cs](csharp/Gallery.Features/Features/TextAndAnnotation.cs) | `CAD_CreateText` `CAD_CreateDimension` `CAD_CreateLeader` `CAD_CreateHatch` |
| 만들기 | 솔리드 기본 도형 | [SolidPrimitives.cs](csharp/Gallery.Features/Features/SolidPrimitives.cs) | `CAD_CreateBox` `CAD_CreateSphere` `CAD_CreateCylinder` `CAD_CreateCone` |
| 만들기 | 스케치 → 솔리드 | [SketchToSolid.cs](csharp/Gallery.Features/Features/SketchToSolid.cs) | `CAD_CreateExtrude` `CAD_Revolve` `CAD_Sweep` `CAD_Loft` |
| 만들기 | 직접 만든 메시 | [CustomMesh.cs](csharp/Gallery.Features/Features/CustomMesh.cs) | `CAD_CreateMesh` `CAD_ReplaceMesh` |
| 만들기 | 대화형 도구·명령행 | [InteractiveTools.cs](csharp/Gallery.Features/Features/InteractiveTools.cs) | `CAD_RequestStartLineSketch` `CAD_ExecuteCommand` `CAD_SetOnPrompt` |
| 편집 | 이동·회전·축척 | [Transform.cs](csharp/Gallery.Features/Features/Transform.cs) | `CAD_SetPosition` `CAD_SetRotationAxisAngle` `CAD_SetScale` `CAD_SetTransform` |
| 편집 | 배열·정렬 | [Arrays.cs](csharp/Gallery.Features/Features/Arrays.cs) | `CAD_Array3DRect` `CAD_Array3DPolar` `CAD_ArrayRect` `CAD_Align3D` |
| 편집 | Jig (끌어서 놓기) | [Jig.cs](csharp/Gallery.Features/Features/Jig.cs) | `CAD_JigBeginOne` `CAD_JigBeginVariants` `CAD_JigNextVariant` `CAD_JigMoveTo` |
| 편집 | 오프셋·대칭·자르기·연장 | [Edit2D.cs](csharp/Gallery.Features/Features/Edit2D.cs) | `CAD_Offset` `CAD_Mirror` `CAD_Trim` `CAD_Extend` |
| 편집 | 불리언·간섭 | [BooleanOps.cs](csharp/Gallery.Features/Features/BooleanOps.cs) | `CAD_BooleanUnionIds` `CAD_BooleanIntersectionIds` `CAD_BooleanSubtract` `CAD_CheckInterference` |
| 편집 | 필렛·모따기·셸·면 밀기 | [SolidEditing.cs](csharp/Gallery.Features/Features/SolidEditing.cs) | `CAD_EdgeFillet` `CAD_EdgeChamfer` `CAD_Shell` `CAD_PushPullSelectedFace` |
| 편집 | Undo·트랜잭션 | [UndoTransaction.cs](csharp/Gallery.Features/Features/UndoTransaction.cs) | `CAD_BeginTransaction` `CAD_EndTransaction` `CAD_RollbackTransaction` `CAD_Undo` |
| 파라메트릭 | B-Rep 피처 (컷·보스·치수) | [BRepFeatures.cs](csharp/Gallery.Features/Features/BRepFeatures.cs) | `CAD_CutExtrudeFromSketch` `CAD_BossExtrudeFromSketch` `CAD_SetFeatureDimension` |
| 파라메트릭 | 스케치 구속 | [SketchConstraints.cs](csharp/Gallery.Features/Features/SketchConstraints.cs) | `CAD_AddSketchConstraint` `CAD_SetSketchConstraintValue` `CAD_GetSketchDiagnosis` |
| 객체·속성 | 객체 조회·속성 JSON | [ObjectQuery.cs](csharp/Gallery.Features/Features/ObjectQuery.cs) | `CAD_GetObjectJson` `CAD_SetObjectProperty` `CAD_QueryObjects` `CAD_SelectByQuery` |
| 객체·속성 | 도면층·선종류·투명도 | [LayersLinetypes.cs](csharp/Gallery.Features/Features/LayersLinetypes.cs) | `CAD_CreateLayer` `CAD_SetObjectColorByLayer` `CAD_SetLayerOpacity` `CAD_CreateLinetype` |
| 객체·속성 | 재질·조명 | [Materials.cs](csharp/Gallery.Features/Features/Materials.cs) | `CAD_GetMaterialName` `CAD_ApplyMaterial` `CAD_SetDemoLighting` |
| 객체·속성 | 이벤트·점 받기·호버 | [EventsAndPicking.cs](csharp/Gallery.Features/Features/EventsAndPicking.cs) | `CAD_AddEventListener` `CAD_BeginGetPoint` `CAD_TryGetPickResult` `CAD_GetHoveredObject` |
| 보기 | 뷰·카메라·화면 분할 | [ViewCamera.cs](csharp/Gallery.Features/Features/ViewCamera.cs) | `CAD_RequestSetView` `CAD_OrbitCamera` `CAD_SetViewportLayout` `CAD_SetVisualStyle` |
| 보기 | 실시간 단면 | [Section.cs](csharp/Gallery.Features/Features/Section.cs) | `CAD_SetSection` `CAD_SetSectionBox` `CAD_ExtractSectionTo2D` |
| 보기 | 도면 뷰·출력 | [DrawingViews.cs](csharp/Gallery.Features/Features/DrawingViews.cs) | `CAD_CreateDrawingViews` `CAD_CreateSectionView` `CAD_CreateDetailView` `CAD_Plot` |
| 파일 | 열기·저장·내보내기·탭 | [FilesDocuments.cs](csharp/Gallery.Features/Features/FilesDocuments.cs) | `CAD_OpenFile` `CAD_SaveAs` `CAD_ExportFile` `CAD_OpenDocument` |
| 파일 | 이미지 붙이기·OCR | [ImagesOcr.cs](csharp/Gallery.Features/Features/ImagesOcr.cs) | `CAD_AttachImageFromFile` `CAD_AttachImageFromMemory` `CAD_AddOcrLine` |
| 로봇·시뮬 | 로봇 팔 (URDF) | [RobotArm.cs](csharp/Gallery.Features/Features/RobotArm.cs) | `CAD_LoadUrdf` `CAD_GetJointInfo` `CAD_SetJointValues` |
| 로봇·시뮬 | 런타임 리깅 (URDF 없이) | [Rigging.cs](csharp/Gallery.Features/Features/Rigging.cs) | `CAD_RigAttach` `CAD_GetObjectRobot` `CAD_SetJointValue` |
| 로봇·시뮬 | 점군 (라이다 스캔) | [PointCloud.cs](csharp/Gallery.Features/Features/PointCloud.cs) | `CAD_CreatePointCloud` `CAD_UpdatePointCloud` `CAD_SetPointCloudSize` |
| 로봇·시뮬 | 외부 위치 연결 (link) | [ExternalPose.cs](csharp/Gallery.Features/Features/ExternalPose.cs) | `CAD_LinkStart` `CAD_LinkSend` `CAD_ApplyExternalPose` |
| 로봇·시뮬 | 파티클·이펙트 | [Particles.cs](csharp/Gallery.Features/Features/Particles.cs) | `CAD_CreateParticleEmitter` `CAD_SetParticleSettingsJson` `CAD_CreateExternalEffect` |
| 로봇·시뮬 | 경로 주행 (내비) | [Navigation.cs](csharp/Gallery.Features/Features/Navigation.cs) | `CAD_NavBuildMap` `CAD_NavAddAgent` `CAD_NavSetGoal` `CAD_NavGetStateJson` |
| 건축 | 방·평면도·문·창 | [Floorplan.cs](csharp/Gallery.Features/Features/Floorplan.cs) | `CAD_CreateRoom` `CAD_CreateFloorplanWithOpenings` `CAD_CreateWallGraph` |
| 분석 | 메시 검사·수리 | [MeshTools.cs](csharp/Gallery.Features/Features/MeshTools.cs) | `CAD_CheckMesh` `CAD_RepairMeshes` `CAD_DecimateMeshes` `CAD_RemeshMeshes` |
| 분석 | 분해도·조립 설명서 | [ExplodeView.cs](csharp/Gallery.Features/Features/ExplodeView.cs) | `CAD_ExplodeView` |
| 확장 | 명령·UI 항목 등록 | [CustomCommands.cs](csharp/Gallery.Features/Features/CustomCommands.cs) | `CAD_RegisterCommand` `CAD_AddUiItem` `CAD_GetUiItemCount` |
| 확장 | 사용자 객체 (딱지 + 메시) | [PluginEntity.cs](csharp/Gallery.Features/Features/PluginEntity.cs) | `CAD_SetPluginTag` `CAD_ReplaceMesh` `CAD_SetOnPluginEntityChanged` |
| 확장 | AI 액션·MCP 도구 | [AiMcp.cs](csharp/Gallery.Features/Features/AiMcp.cs) | `CAD_RunAiActionsJson` `CAD_McpCall` `CAD_AiSubmit` |

## 알아 두면 좋은 엔진 규칙

- **요청형 명령은 다음 틱에 실행된다.** `CAD_RequestXxx`·요청형 불리언(`CAD_BooleanUnion` 등)·뷰 변경은 큐에 들어가
  다음 `CAD_Tick` 에서 **그때의 선택**으로 돈다. 한 프레임에 여러 개를 몰아 부르면 마지막 선택으로 다 돈다 — 프레임을 띄울 것
  (`Feature.Later(frames, …)` 참고). 불리언은 **바로 실행형**(`CAD_BooleanUnionIds`·`CAD_BooleanIntersectionIds`·`CAD_BooleanSubtract`)을
  쓰면 이 문제가 없고 결과 id 도 돌려받는다.
- **`CAD_CreateBox` 의 (x,y,z) 는 상자 중심**이다. 다른 솔리드(구·원기둥·원뿔·토러스…)는 밑면 중심.
- **콜백 델리게이트는 필드로 붙들어 둘 것.** 지역 변수로 넘기면 GC 가 걷어가 엔진이 사라진 함수를 부른다.
- **문자열은 UTF-8.** `sdk/csharp` 선언이 이미 그렇게 마샬링한다 — 한글 경로·문자 그대로 넘기면 된다.
- 콜백은 `CAD_Tick` 안, **UI 스레드**에서 불린다. 콜백에서 화면을 바로 고쳐도 된다.

## 스크린샷 모드

모든 기능을 차례로 열어 `▶ 시연` 까지 실행하고 화면(PNG)·로그(TXT)를 남긴 뒤 종료합니다.
README 그림을 만들거나 엔진을 바꾼 뒤 기능이 그대로 동작하는지 볼 때 씁니다.

```bash
VulkanCAD.Gallery.WinForms.exe --shots out/            # 전부
VulkanCAD.Gallery.Wpf.exe --shots out/ --only Section,RobotArm
```
