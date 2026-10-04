using System.Text.Json;
using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery
{
    public sealed class ExplodeView : Feature
    {
        public override string Category => "분석";
        public override string Group => "조립";
        public override string Icon => "⇱";
        public override string Title => "분해도·조립 설명서";
        public override string Summary =>
            "조립품을 부품이 서로 부딪히지 않는 순서와 방향으로 풀어 보여 준다(solve). 진행도(amount 0~1)·재생·풍선 번호·부품표(BOM)·" +
            "PDF 조립 설명서까지 한 함수 CAD_ExplodeView 에 JSON {\"op\":…} 로 넘긴다 — AI 액션·MCP 도구와 같은 창구.";
        public override string[] Apis => new[] { "CAD_ExplodeView" };

        IValue<float> _amount;

        protected override void Setup()
        {
            if (!CAD_OpenFile("models/GearboxAssy.glb")) Log("열기 실패 — " + Cad.StatusMessage());
            Scene.Frame();

            Ui.Section("분해");
            Ui.Button("순서 풀기 (solve)", () => Op("{\"op\":\"solve\"}"));
            Ui.Button("자동 방향 (auto)", () => Op("{\"op\":\"auto\"}"));
            _amount = Ui.Slider("진행도", 0, 1, 0, v => Op($"{{\"op\":\"amount\",\"t\":{v:0.###}}}", quiet: true), 2);
            Ui.Button("재생 (2초)", () => Op("{\"op\":\"play\",\"seconds\":2}"));
            Ui.Button("되돌리기 재생", () => Op("{\"op\":\"back\",\"seconds\":2}"));
            Ui.Button("분해 지우기", () => Op("{\"op\":\"clear\"}"));
            Ui.Section("문서");
            Ui.Toggle("풍선 번호", false, v => Op($"{{\"op\":\"balloons\",\"show\":{(v ? "true" : "false")}}}"));
            Ui.Toggle("부품표 (BOM)", false, v => Op($"{{\"op\":\"bom\",\"show\":{(v ? "true" : "false")}}}"));
            Ui.Button("부품표 CSV 저장…", () =>
            {
                string p = Ui.PickSaveFile("CSV|*.csv", "부품표.csv");
                if (p != null) Op(JsonSerializer.Serialize(new { op = "bom", path = p }));
            });
            Ui.Button("조립 설명서 PDF…", () =>
            {
                string p = Ui.PickSaveFile("PDF|*.pdf", "조립설명서.pdf");
                if (p != null) Op(JsonSerializer.Serialize(new { op = "manual", path = p, paper = "a4" }));
            });
            Ui.Button("상태", () => Op("{\"op\":\"status\"}"));
        }

        // 결과 JSON 의 ok·error·t·steps 만 짧게 찍는다
        void Op(string args, bool quiet = false)
        {
            string r = Str((b, n) => CAD_ExplodeView(args, b, n), 8192);
            if (quiet || r == null) return;
            using var doc = JsonDocument.Parse(r);
            var root = doc.RootElement;
            if (root.TryGetProperty("error", out var e)) { Log($"{args} → 오류: {e}"); return; }
            string steps = root.TryGetProperty("steps", out var s) ? s.ToString() : "?";
            string parts = root.TryGetProperty("parts", out var p) ? p.GetArrayLength().ToString() : "?";
            string items = root.TryGetProperty("items", out var it) ? it.GetArrayLength().ToString() : "?";
            Log($"{args} → 단계 {steps}, 부품 {parts}, 품목 {items}");
            if (root.TryGetProperty("t", out var t)) _amount.Value = t.GetSingle();
        }

        public override void Demo()
        {
            Op("{\"op\":\"solve\"}");
            Op("{\"op\":\"amount\",\"t\":0.8}");
            Op("{\"op\":\"balloons\",\"show\":true}");
        }
    }
}
