using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery
{
    public sealed class BRepFeatures : Feature
    {
        public override string Category => "파라메트릭";
        public override string Group => "피처";
        public override string Icon => "⧉";
        public override string Title => "B-Rep 피처 (컷·보스·치수)";
        public override string Summary =>
            "돌출 솔리드에 스케치로 컷(포켓·관통)과 보스(더하기)를 쌓고, 피처 치수로 다시 만든다. " +
            "기록이 남으므로 치수·높이를 바꾸면 형상이 재생성된다(솔리드웍스 Instant3D 식).";
        public override string[] Apis => new[]
        {
            "CAD_GetBRepInfo", "CAD_SetBRepExtrudeHeight", "CAD_CutExtrudeFromSketch", "CAD_BossExtrudeFromSketch",
            "CAD_SetBRepBossHeight", "CAD_GetFeatureDimension", "CAD_SetFeatureDimension", "CAD_SetFeatureDimensionsVisible",
        };

        uint _solid, _cutSketch, _bossSketch;

        protected override void Setup()
        {
            // 바닥 사각형 → 돌출 높이 1
            uint rect = CAD_CreateRectangle(0, 0, 0, 6, 4, 0);
            _solid = Tint(Check("돌출", CAD_CreateExtrude(rect, 1)), 0.6f, 0.7f, 0.85f);
            // 윗면(z=1) 위의 닫힌 스케치 두 개 — 컷용 원, 보스용 사각형
            _cutSketch = CAD_CreateCircle(1.5f, 2, 1, 0.7f, 0, 0, 1);
            _bossSketch = CAD_CreateRectangle(3.5f, 1, 1, 5, 3, 1);
            CAD_ClearSelection();
            Scene.Frame();

            Ui.Section("피처 쌓기");
            Ui.Button("원으로 관통 컷", Cut);
            Ui.Button("사각형으로 보스 높이 0.8", Boss);
            Ui.Section("다시 만들기");
            Ui.Slider("돌출 높이", 0.3f, 3, 1, v => CAD_SetBRepExtrudeHeight(_solid, v), 2);
            Ui.Slider("보스 높이 (음수 = 아래로)", -1, 2, 0.8f, v => CAD_SetBRepBossHeight(_solid, 0, v), 2);
            Ui.Toggle("피처 치수 보이기 (더블클릭하면 값 입력)", false, CAD_SetFeatureDimensionsVisible);
            Ui.Button("피처 치수 목록", ListDims);
            Ui.Button("정보 (부피·면 수)", Info);
        }

        void Cut() => Log("컷 → " + CAD_CutExtrudeFromSketch(_solid, _cutSketch, 0, true) + $", 컷 {CAD_GetBRepCutCount(_solid)}개");
        void Boss() => Log("보스 → " + CAD_BossExtrudeFromSketch(_solid, _bossSketch, 0.8f) + $", 보스 {CAD_GetBRepBossCount(_solid)}개");

        void ListDims()
        {
            string[] kinds = { "가로", "세로", "높이", "지름", "변" };
            uint n = CAD_GetFeatureDimensionCount(_solid);
            for (uint i = 0; i < n; i++)
                if (CAD_GetFeatureDimension(_solid, i, out int kind, out float v, out uint sketch))
                    Log($"  [{i}] {kinds[kind]} = {v:0.###}  (스케치 {sketch})");
            Log($"피처 치수 {n}개 — CAD_SetFeatureDimension(id, index, 값) 으로 바꾼다");
        }

        void Info()
        {
            if (!CAD_GetBRepInfo(_solid, out var info)) { Log("B-Rep 정보 없음"); return; }
            Log($"종류 {info.featureType}  면 {info.faceCount} 모서리 {info.edgeCount} 꼭짓점 {info.vertexCount}  부피 {info.volume:0.###}  겉넓이 {info.surfaceArea:0.###}");
        }

        public override void Demo()
        {
            Cut(); Boss();
            CAD_SetFeatureDimensionsVisible(true);
            CAD_SelectObject(_solid, false);
            ListDims(); Info();
        }
    }
}
