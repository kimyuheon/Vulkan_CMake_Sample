using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery
{
    public sealed class SolidEditing : Feature
    {
        public override string Category => "편집";
        public override string Group => "솔리드";
        public override string Icon => "◢";
        public override string Title => "필렛·모따기·셸·면 밀기";
        public override string Summary =>
            "선택한 솔리드의 모서리를 굴리고(필렛) 깎고(모따기), 속을 비운다(셸). 면 밀기는 **면 선택 모드**에서 " +
            "광선으로 면을 고른 뒤 법선 방향으로 민다. 거리 0 이하는 크기에 맞춰 자동.";
        public override string[] Apis => new[]
        {
            "CAD_EdgeFillet", "CAD_EdgeChamfer", "CAD_Shell", "CAD_SetSubobjectSelectionMode", "CAD_PickSubobject",
            "CAD_PushPullSelectedFace", "CAD_InsetSelectedFace", "CAD_AddEdgeBevel",
        };

        uint _fillet, _chamfer, _shell, _push;

        protected override void Setup()
        {
            _fillet = Tint(Box(0, 0, 2, 2, 1.5f), 0.85f, 0.55f, 0.35f);
            Caption(-1, -1.8f, "필렛");
            _chamfer = Tint(Box(3.5f, 0, 2, 2, 1.5f), 0.45f, 0.70f, 0.90f);
            Caption(2.5f, -1.8f, "모따기");
            _shell = Tint(Box(7, 0, 2, 2, 1.5f), 0.55f, 0.80f, 0.45f);
            Caption(6, -1.8f, "셸");
            _push = Tint(Box(10.5f, 0, 2, 2, 1), 0.75f, 0.55f, 0.85f);
            Caption(9.5f, -1.8f, "면 밀기");
            CAD_ClearSelection();
            Scene.Frame();

            Ui.Section("선택에 적용");
            Ui.Button("필렛 0.3", () => On(_fillet, () => CAD_EdgeFillet(0.3f), "필렛"));
            Ui.Button("모따기 0.3", () => On(_chamfer, () => CAD_EdgeChamfer(0.3f), "모따기"));
            Ui.Button("셸 두께 0.15", () => On(_shell, () => CAD_Shell(0.15f), "셸"));
            Ui.Section("면");
            Ui.Button("윗면을 위로 0.8 밀기", () => TopFace(() => CAD_PushPullSelectedFace(0.8f), "면 밀기"));
            Ui.Button("윗면 안쪽 링(인셋) 0.3", () => TopFace(() => CAD_InsetSelectedFace(0.3f), "인셋"));
            Ui.Section("B-Rep 기록형");
            Ui.Button("모든 볼록 모서리 필렛 기록 추가", () =>
                Log("AddEdgeBevel → " + CAD_AddEdgeBevel(_chamfer, true, 0.1f, true, 0) + $", 기록 {CAD_GetEdgeBevelCount(_chamfer)}개"));
        }

        void On(uint id, System.Func<bool> op, string name)
        {
            CAD_ClearSelection();
            CAD_SelectObject(id, false);
            Log($"{name} → {(op() ? "성공" : "실패 — " + Cad.StatusMessage())}");
            CAD_ClearSelection();
        }

        // 면 선택 모드 → 위에서 아래로 쏜 광선이 맞는 면(= 윗면)을 고른다 → 연산 → 객체 모드로 복귀
        void TopFace(System.Func<bool> op, string name)
        {
            CAD_SetSubobjectSelectionMode(1);
            bool hit = CAD_PickSubobject(10.5f, 0, 10, 0, 0, -1);
            Log($"{name}: 면 고르기 {(hit ? "성공" : "실패")}, B-Rep 면 id {CAD_GetSelectedBRepFaceId()}");
            if (hit) Log($"{name} → {(op() ? "성공" : "실패")}");
            CAD_SetSubobjectSelectionMode(0);
            CAD_ClearSelection();
        }

        public override void Demo()
        {
            On(_fillet, () => CAD_EdgeFillet(0.3f), "필렛");
            On(_chamfer, () => CAD_EdgeChamfer(0.3f), "모따기");
            On(_shell, () => CAD_Shell(0.15f), "셸");
            TopFace(() => CAD_PushPullSelectedFace(0.8f), "면 밀기");
        }
    }
}
