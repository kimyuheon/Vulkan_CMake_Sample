using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery
{
    public sealed class ObjectQuery : Feature
    {
        public override string Category => "객체·속성";
        public override string Group => "조회";
        public override string Icon => "⌕";
        public override string Title => "객체 조회·속성 JSON";
        public override string Summary =>
            "객체를 JSON 으로 읽고, 이름으로 된 속성 하나를 읽고 쓰고(radius·color·position…), " +
            "필터 JSON 으로 찾거나 선택한다. 키 이름이 전부 같아서 AI·MCP·호스트가 한 규칙으로 다룬다.";
        public override string[] Apis => new[]
        {
            "CAD_GetObjectJson", "CAD_GetObjectProperty", "CAD_SetObjectProperty", "CAD_QueryObjects",
            "CAD_SelectByQuery", "CAD_GetSceneJson", "CAD_SetObjectName", "CAD_FindObjectByName", "CAD_GetObjectKind",
        };

        uint _col1;

        protected override void Setup()
        {
            for (int i = 0; i < 4; i++)
            {
                uint c = CAD_CreateCircle(i * 2.5f, 0, 0, 0.3f + 0.15f * i, 0, 0, 1);
                CAD_SetObjectName(c, $"기둥-{i + 1}");
                if (i == 0) _col1 = c;
            }
            for (int i = 0; i < 3; i++) CAD_SetObjectName(Tint(Box(i * 2.5f, 3, 1, 1, 0.5f + i * 0.4f), 0.85f, 0.55f, 0.35f), $"상자-{i + 1}");
            CAD_SetObjectName(CAD_CreateLine(-1, -1.5f, 0, 8.5f, -1.5f, 0), "기준선");
            CAD_ClearSelection();
            Scene.Frame();

            Ui.Section("읽기");
            Ui.Button("\"기둥-1\" 찾아 JSON 보기", () =>
            {
                uint id = CAD_FindObjectByName("기둥-1");
                Log($"id {id}, 종류 {CAD_GetObjectKind(id)} (3=원)");
                Log(Str((b, n) => CAD_GetObjectJson(id, b, n)));
            });
            Ui.Button("\"기둥-1\" 의 radius 만 읽기", () => Log("radius = " + Str((b, n) => CAD_GetObjectProperty(_col1, "radius", b, n))));
            Ui.Button("장면 요약 JSON (앞 5개)", () => Log(Str((b, n) => CAD_GetSceneJson(b, n, 5), 4096)));

            Ui.Section("쓰기 (Undo 한 단계씩)");
            Ui.Button("radius = 1.0", () => Set(_col1, "radius", "1.0"));
            Ui.Button("color = [1, 0.3, 0.3]", () => Set(_col1, "color", "[1,0.3,0.3]"));
            Ui.Button("position = [0, -3, 0]", () => Set(_col1, "position", "[0,-3,0]"));

            Ui.Section("질의 → 선택");
            Ui.Input("필터 JSON", "{\"name\":\"기둥*\"}", "선택", f =>
            {
                uint n = CAD_SelectByQuery(f, false);
                Log(n > 0 || string.IsNullOrEmpty(Cad.LastError()) ? $"{n}개 선택" : "필터 오류: " + Cad.LastError());
            });
            Ui.Note("예: {\"kind\":\"circle\"} · {\"kind\":[\"circle\",\"line\"]} · {\"within\":{\"min\":[0,0,0],\"max\":[5,5,5]}} · {\"pick\":\"largest\",\"kind\":\"mesh\"}");
            Ui.Button("상자 중 가장 큰 것 id (QueryObjects)", () =>
            {
                var ids = Cad.Ids((buf, cap) => CAD_QueryObjects("{\"name\":\"상자*\",\"pick\":\"largest\"}", buf, cap));
                Log("가장 큰 상자: " + string.Join(", ", ids));
            });
        }

        void Set(uint id, string key, string json) =>
            Log($"{key} ← {json}: " + (CAD_SetObjectProperty(id, key, json) ? "성공" : "실패 — " + Cad.LastError()));

        public override void Demo()
        {
            Set(_col1, "radius", "1.0");
            Set(_col1, "color", "[1,0.3,0.3]");
            Log($"\"기둥*\" 선택 → {CAD_SelectByQuery("{\"name\":\"기둥*\"}", false)}개");
        }
    }
}
