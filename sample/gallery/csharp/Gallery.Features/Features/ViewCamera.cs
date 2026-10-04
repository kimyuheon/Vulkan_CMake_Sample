using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery
{
    public sealed class ViewCamera : Feature
    {
        public override string Category => "보기";
        public override string Group => "카메라";
        public override string Icon => "◈";
        public override string Title => "뷰·카메라·화면 분할";
        public override string Summary =>
            "표준 뷰·임의 방향·궤도 회전·회전 중심을 코드로 다루고, 화면을 1·2·3·4 칸으로 나누고, 표시 스타일을 바꾼다. " +
            "뷰 요청은 다음 틱에 적용된다.";
        public override string[] Apis => new[]
        {
            "CAD_RequestSetView", "CAD_RequestSetViewDirection", "CAD_OrbitCamera", "CAD_RequestSetViewPivot",
            "CAD_SetViewportLayout", "CAD_SetActiveViewport", "CAD_SetVisualStyle", "CAD_SetSelectionStyle",
            "CAD_RequestSetProjection",
        };

        bool _turntable;

        protected override void Setup()
        {
            Tint(Box(0, 0, 2, 2, 2), 0.85f, 0.55f, 0.35f);
            Tint(CAD_CreateSphere(3, 0, 0, 1), 0.45f, 0.70f, 0.90f);
            Tint(CAD_CreateCone(0, 3, 0, 1, 2), 0.55f, 0.80f, 0.45f);
            Tint(CAD_CreateTorus(3, 3, 0, 1, 0.3f), 0.75f, 0.55f, 0.85f);
            CAD_ClearSelection();
            Scene.Frame();

            Ui.Section("뷰");
            string[] views = { "정면", "평면", "우측면", "등각", "좌측면", "저면", "배면" };
            Ui.Choice("표준 뷰", views, 3, i => { CAD_RequestSetView(i); CAD_RequestZoomExtents(); });
            Ui.Button("앞·우·위 꼭짓점 방향 (1,-1,1)", () => CAD_RequestSetViewDirection(1, -1, 1));
            Ui.Toggle("직교 투영", false, CAD_RequestSetProjection);
            Ui.Toggle("턴테이블 (매 프레임 Z축 궤도 회전)", false, on => _turntable = on);
            Ui.Button("회전 중심 = 구 중심, Z축 고정", () => CAD_RequestSetViewPivot(3, 0, 1, 2));
            Ui.Button("회전 중심 해제", CAD_RequestClearViewPivot);

            Ui.Section("화면 분할");
            Ui.Choice("배치", new[] { "단일", "좌우", "3분할", "4분할" }, 0, CAD_SetViewportLayout);
            Ui.Choice("활성 칸 (뷰 명령이 적용될 곳)", new[] { "0", "1", "2", "3" }, 0, CAD_SetActiveViewport);

            Ui.Section("표시");
            Ui.Choice("스타일", new[] { "음영", "음영+모서리", "와이어프레임", "와이어+모서리", "숨은선" }, 0, CAD_SetVisualStyle);
            Ui.Choice("선택 강조", new[] { "외곽선", "모서리(CAD식)" }, CAD_GetSelectionStyle(), CAD_SetSelectionStyle);
            Ui.Toggle("그리드", true, CAD_SetGridEnabled);
        }

        protected override void Update(double dt)
        {
            if (_turntable) CAD_OrbitCamera(0, 0, 1, (float)(dt * 30));   // 초당 30°
        }

        public override void Demo()
        {
            CAD_SetViewportLayout(3);
            CAD_SetVisualStyle(1);
        }
    }
}
