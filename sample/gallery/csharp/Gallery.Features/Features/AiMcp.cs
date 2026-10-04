using System.Linq;
using System.Text.Json;
using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery
{
    public sealed class AiMcp : Feature
    {
        public override string Category => "확장";
        public override string Group => "AI";
        public override string Icon => "✦";
        public override string Title => "AI 액션·MCP 도구";
        public override string Summary =>
            "세 가지 길: ① 호스트가 자기 LLM(Claude API 등)에서 받은 **액션 JSON** 을 바로 실행(RunAiActionsJson, Undo 한 단계) " +
            "② MCP 도구를 TCP 없이 직접 호출(McpCall) ③ 엔진이 OpenAI 호환 서버에 직접 묻기(AiSubmit, 비동기).";
        public override string[] Apis => new[]
        {
            "CAD_RunAiActionsJson", "CAD_GetMcpToolsJson", "CAD_McpCall", "CAD_AiSetEndpoint", "CAD_AiSubmit",
            "CAD_AiGetStatus", "CAD_AiGetLastAnswer", "CAD_AiGetNotes",
        };

        const string SampleActions =
            "{\"actions\":[" +
            "{\"type\":\"draw_sketch\",\"shape\":\"polygon\",\"center\":[0,0,0],\"radius\":2,\"sides\":6}," +
            "{\"type\":\"create_primitive\",\"shape\":\"cube\",\"count\":5,\"position\":\"random\",\"scale\":\"random\",\"color\":\"random\"}," +
            "{\"type\":\"view_control\",\"op\":\"zoom_extents\"}]}";

        string[] _tools = new string[0];
        int _tool = -1;
        bool _waiting;

        protected override void Setup()
        {
            Scene.Frame();

            Ui.Section("① 액션 JSON 실행");
            Ui.Input("액션 JSON", SampleActions, "실행", RunActions);
            Ui.Note("형식은 엔진 lot_ai_prompt.h 의 계약: {\"actions\":[{\"type\":…}], \"answer\":\"…\"}. 배열만 넘겨도 된다.");

            Ui.Section("② MCP 도구");
            string toolsJson = Str(CAD_GetMcpToolsJson, 65536);
            if (toolsJson != null)
            {
                using var doc = JsonDocument.Parse(toolsJson);
                var arr = doc.RootElement.ValueKind == JsonValueKind.Array ? doc.RootElement
                        : doc.RootElement.TryGetProperty("tools", out var t) ? t : default;
                if (arr.ValueKind == JsonValueKind.Array)
                    _tools = arr.EnumerateArray().Select(x => x.GetProperty("name").GetString()).ToArray();
            }
            Ui.Choice($"도구 ({_tools.Length}개)", _tools, -1, i => _tool = i);
            Ui.Input("인자 JSON", "{}", "호출", args =>
            {
                if (_tool < 0) { Log("도구를 고를 것"); return; }
                string r = Str((b, n) => CAD_McpCall(_tools[_tool], args, b, n), 65536);
                Log($"{_tools[_tool]} → {(r == null ? "실패" : r.Length > 600 ? r.Substring(0, 600) + " …" : r)}");
            });

            Ui.Section("③ LLM 서버에 직접 (OpenAI 호환)");
            Ui.Input("서버 주소", "http://127.0.0.1:8080", "연결", url => Log("AiSetEndpoint → " + CAD_AiSetEndpoint(url, null)));
            Ui.Input("요청", "빨간 원기둥 3개를 한 줄로 세워줘", "보내기", p =>
            {
                if (CAD_AiSubmit(p)) { _waiting = true; Log("보냄 — 답을 기다리는 중"); }
                else Log("보내기 실패 — " + Cad.LastError());
            });
        }

        void RunActions(string json)
        {
            string notes = Str((b, n) => CAD_RunAiActionsJson(json, b, n), 4096);
            Log(notes == null ? "실패 — " + Cad.LastError() : "결과:\n" + notes);
        }

        protected override void Update(double dt)
        {
            if (!_waiting) return;
            int s = CAD_AiGetStatus();   // 0 대기 1 처리중 2 완료 3 오류 (2·3 은 한 번만)
            if (s == 2) { _waiting = false; Log("답: " + Str(CAD_AiGetLastAnswer, 4096)); Log(Str(CAD_AiGetNotes, 4096)); }
            if (s == 3) { _waiting = false; Log("오류: " + Cad.LastError()); }
        }

        public override void Leave()
        {
            if (_waiting) CAD_AiCancel();
            base.Leave();
        }

        public override void Demo() => RunActions(SampleActions);
    }
}
