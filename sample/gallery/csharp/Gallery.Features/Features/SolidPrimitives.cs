using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery
{
    public sealed class SolidPrimitives : Feature
    {
        public override string Category => "만들기";
        public override string Group => "3D";
        public override string Icon => "⬢";
        public override string Title => "솔리드 기본 도형";
        public override string Summary =>
            "상자·구·원기둥·원뿔·토러스·쐐기·피라미드·관을 값으로 만든다. " +
            "(x,y,z) 는 밑면 중심이고 축은 +Z. 반환값은 객체 id 다(0 = 실패).";
        public override string[] Apis => new[]
        {
            "CAD_CreateBox", "CAD_CreateSphere", "CAD_CreateCylinder", "CAD_CreateCone", "CAD_CreateTorus",
            "CAD_CreateWedge", "CAD_CreatePyramid", "CAD_CreateTube", "CAD_SetColor",
        };

        protected override void Setup()
        {
            const float g = 3f;   // 간격
            Make("상자", Box(0 * g, 0, 2, 2, 2), 0.85f, 0.45f, 0.35f);
            Make("구", CAD_CreateSphere(1 * g, 0, 0, 1), 0.35f, 0.65f, 0.90f);
            Make("원기둥", CAD_CreateCylinder(2 * g, 0, 0, 1, 2), 0.45f, 0.80f, 0.45f);
            Make("원뿔", CAD_CreateCone(3 * g, 0, 0, 1, 2), 0.95f, 0.75f, 0.30f);
            Make("토러스", CAD_CreateTorus(0 * g, g, 0, 1, 0.3f), 0.75f, 0.45f, 0.85f);
            Make("쐐기", CAD_CreateWedge(1 * g, g, 0, 2, 2, 1.5f), 0.90f, 0.55f, 0.55f);
            Make("피라미드", CAD_CreatePyramid(2 * g, g, 0, 1.2f, 2, 4, 0), 0.55f, 0.75f, 0.75f);
            Make("관", CAD_CreateTube(3 * g, g, 0, 1, 0.7f, 2), 0.70f, 0.70f, 0.70f);
            CAD_ClearSelection();
            Scene.Frame();

            Ui.Section("하나 더 만들기");
            Ui.Button("상자 (2×1×0.5)", () => Make("상자", Box(0, -g, 2, 1, 0.5f), 0.85f, 0.45f, 0.35f));
            Ui.Button("구 (r=0.5)", () => Make("구", CAD_CreateSphere(g, -g, 0, 0.5f), 0.35f, 0.65f, 0.90f));
            Ui.Button("육각 기둥 (피라미드, 윗면=밑면)", () => Make("육각기둥", CAD_CreatePyramid(2 * g, -g, 0, 1, 2, 6, 1), 0.6f, 0.6f, 0.9f));
            Ui.Note("만든 직후 새 객체가 선택된다. 다른 기능(불리언 등)이 선택을 입력으로 쓰기 때문이다.");
        }

        void Make(string name, uint id, float r, float g, float b)
        {
            if (id == 0) { Log($"{name}: 실패 — {Cad.LastError()}"); return; }
            CAD_SetColor(id, r, g, b);
            Log($"{name} → id {id}");
        }
    }
}
