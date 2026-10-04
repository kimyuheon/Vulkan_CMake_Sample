using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery
{
    public sealed class MeshTools : Feature
    {
        public override string Category => "분석";
        public override string Group => "메시";
        public override string Icon => "△";
        public override string Title => "메시 검사·수리";
        public override string Summary =>
            "가져온 메시(STL·OBJ)의 열린 모서리·구멍·뒤집힌 면을 검사하고 고친다. 매끈하게(부피 유지), 삼각형 줄이기(QEM), " +
            "틈 꿰매기, 두께 주기(열린 판 → 솔리드), 등방 리메시. 전부 Undo 한 단계.";
        public override string[] Apis => new[]
        {
            "CAD_CheckMesh", "CAD_RepairMeshes", "CAD_SmoothMeshes", "CAD_DecimateMeshes", "CAD_StitchMeshes",
            "CAD_ThickenMeshes", "CAD_RemeshMeshes",
        };

        uint _mesh;

        protected override void Setup()
        {
            Ui.Section("대상");
            Ui.Choice("모델", new[] { "smooth_vase.obj", "flat_vase.obj", "cube.obj", "quad.obj (열린 판)" }, -1, i =>
            {
                Scene.Reset(Title);
                string f = new[] { "smooth_vase.obj", "flat_vase.obj", "cube.obj", "quad.obj" }[i];
                Load(f);
            });
            Ui.Section("검사·수리");
            Ui.Button("검사", Check);
            Ui.Button("수리 (붙이기·중복 제거·방향 통일·구멍 메움)", () => Do("수리", ids => CAD_RepairMeshes(ids, 1, true)));
            Ui.Button("틈 꿰매기 (허용 오차 자동)", () => Do("꿰매기", ids => CAD_StitchMeshes(ids, 1, 0)));
            Ui.Section("다듬기");
            Ui.Button("매끈하게 (5회)", () => Do("매끈하게", ids => CAD_SmoothMeshes(ids, 1, 5)));
            Ui.Button("삼각형 30% 만 남기기", () => Do("줄이기", ids => CAD_DecimateMeshes(ids, 1, 0.3)));
            Ui.Button("리메시 (지금 평균 모서리 길이)", () => Do("리메시", ids => CAD_RemeshMeshes(ids, 1, 0)));
            Ui.Button("두께 주기 0.05 (열린 메시만)", () => Do("두께", ids => CAD_ThickenMeshes(ids, 1, 0.05)));
            Load("smooth_vase.obj");
        }

        void Load(string file)
        {
            uint before = CAD_GetObjectCount();
            if (!CAD_OpenFile("models/" + file)) { Log("열기 실패 — " + Cad.StatusMessage()); return; }
            var ids = Cad.AllObjectIds();
            _mesh = ids.Length > 0 ? ids[^1] : 0;
            Tint(_mesh, 0.75f, 0.75f, 0.8f);
            CAD_SetVisualStyle(1);
            Log($"{file} → id {_mesh}");
            Scene.Frame();
            Check();
        }

        void Check()
        {
            if (!CAD_CheckMesh(_mesh, out var m)) { Log("메시가 아니다"); return; }
            Log($"삼각형 {m.triangles} 정점 {m.vertices} | 열린 모서리 {m.openEdges} 비다양체 {m.nonManifoldEdges} " +
                $"뒤집힘 {m.flippedEdges} 구멍 {m.holes} 덩어리 {m.components} | 닫힘 {m.closed} 정상 {m.ok} | 부피 {m.volume:0.###}");
        }

        void Do(string name, System.Func<uint[], uint> op)
        {
            uint n = op(new[] { _mesh });
            Log($"{name} → 바뀐 객체 {n}개");
            Check();
        }

        public override void Demo() => Do("줄이기", ids => CAD_DecimateMeshes(ids, 1, 0.3));
    }
}
