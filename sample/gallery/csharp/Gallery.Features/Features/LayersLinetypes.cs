using System.Linq;
using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery
{
    public sealed class LayersLinetypes : Feature
    {
        public override string Category => "객체·속성";
        public override string Group => "도면층";
        public override string Icon => "☰";
        public override string Title => "도면층·선종류·투명도";
        public override string Summary =>
            "도면층을 만들어 객체를 나눠 담고, 층 단위로 색(ByLayer)·숨김·잠금·투명도·선종류를 바꾼다. " +
            "선종류는 표준 8종(id 1..8)이 기본으로 있고, 무늬 배열로 사용자 선종류를 추가한다.";
        public override string[] Apis => new[]
        {
            "CAD_CreateLayer", "CAD_SetObjectLayer", "CAD_SetLayerColor", "CAD_SetObjectColorByLayer",
            "CAD_SetLayerVisible", "CAD_SetLayerLocked", "CAD_SetLayerOpacity", "CAD_SetLayerLinetype",
            "CAD_CreateLinetype", "CAD_SetObjectLinetype", "CAD_GetLinetypeIds",
        };

        uint _walls, _furniture, _center;

        protected override void Setup()
        {
            _walls = Layer("벽", 0.9f, 0.45f, 0.4f);
            _furniture = Layer("가구", 0.4f, 0.7f, 0.95f);
            _center = Layer("중심선", 0.95f, 0.85f, 0.3f);

            // 벽 = 상자 4개, 가구 = 원기둥 2개, 중심선 = 선 2개
            foreach (var (x, y, sx, sy) in new[] { (3f, 0f, 6f, 0.2f), (3f, 4f, 6f, 0.2f), (0f, 2f, 0.2f, 4f), (6f, 2f, 0.2f, 4f) })
                Put(Box(x, y, sx, sy, 2), _walls);
            Put(CAD_CreateCylinder(2, 2, 0, 0.5f, 0.8f), _furniture);
            Put(CAD_CreateCylinder(4, 2, 0, 0.5f, 0.8f), _furniture);
            Put(CAD_CreateLine(-0.5f, 2, 0, 6.5f, 2, 0), _center);
            Put(CAD_CreateLine(3, -0.5f, 0, 3, 4.5f, 0), _center);
            CAD_SetLayerLinetype(_center, 3);   // 3 = Center (일점쇄선)
            CAD_ClearSelection();
            Scene.Frame();

            Ui.Section("벽 층");
            Ui.Toggle("보이기", true, v => CAD_SetLayerVisible(_walls, v));
            Ui.Toggle("잠그기 (선택·편집 불가)", false, v => CAD_SetLayerLocked(_walls, v));
            Ui.Slider("투명도 (1 = 불투명)", 0.1f, 1, 1, v => CAD_SetLayerOpacity(_walls, v), 2);
            Ui.Button("층 색 → 초록 (ByLayer 객체가 따라 바뀜)", () => CAD_SetLayerColor(_walls, 0.4f, 0.8f, 0.45f));

            Ui.Section("선종류");
            var ids = Cad.Ids((b, n) => CAD_GetLinetypeIds(b, n));
            var names = ids.Select(id => Str((b, n) => CAD_GetLinetypeName(id, b, n))).ToArray();
            Ui.Choice("중심선 층 선종류", names, System.Array.IndexOf(ids, 3u), i => CAD_SetLayerLinetype(_center, ids[i]));
            Ui.Slider("전역 선종류 축척 (LTSCALE)", 0.1f, 3, CAD_GetLinetypeScale(), v => CAD_SetLinetypeScale(v), 2);
            Ui.Button("사용자 선종류 만들기 (긴선-점-점)", () =>
            {
                // 양수 = 선, 음수 = 빈칸, 0 = 점
                var pattern = new float[] { 0.5f, -0.1f, 0, -0.1f, 0, -0.1f };
                uint lt = CAD_CreateLinetype("긴선점점", pattern, pattern.Length, "긴선 + 점 둘");
                Log($"선종류 id {lt}");
                if (lt != 0) CAD_SetLayerLinetype(_center, lt);
            });
            Log($"도면층 {CAD_GetLayerIds(null, 0)}개 (0번 기본층 포함), 선종류 {ids.Length}개");
        }

        uint Layer(string name, float r, float g, float b)
        {
            uint id = CAD_CreateLayer(name);
            CAD_SetLayerColor(id, r, g, b);
            return id;
        }

        // 객체를 층에 넣고 색을 ByLayer 로 — 층 색을 따라간다
        static void Put(uint obj, uint layer)
        {
            CAD_SetObjectLayer(obj, layer);
            CAD_SetObjectColorByLayer(obj, true);
        }

        public override void Demo()
        {
            CAD_SetLayerOpacity(_walls, 0.35f);
            CAD_SetLayerColor(_furniture, 0.95f, 0.6f, 0.2f);
        }
    }
}
