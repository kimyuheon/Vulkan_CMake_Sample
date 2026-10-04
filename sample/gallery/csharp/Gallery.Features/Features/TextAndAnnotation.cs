using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery
{
    public sealed class TextAndAnnotation : Feature
    {
        public override string Category => "만들기";
        public override string Group => "2D";
        public override string Icon => "A";
        public override string Title => "문자·치수·해치";
        public override string Summary =>
            "문자, 선형 치수, 지시선, 해치(닫힌 경계 안 무늬)를 만든다. 문자는 내용을 바로 바꾸거나(SetTextContent) " +
            "편집 세션(BeginTextEdit → Insert → Commit)으로 한 글자씩 고칠 수 있다 — 둘 다 Undo 한 단계.";
        public override string[] Apis => new[]
        {
            "CAD_CreateText", "CAD_CreateDimension", "CAD_CreateLeader", "CAD_CreateHatch",
            "CAD_SetTextContent", "CAD_GetTextContent", "CAD_BeginTextEdit", "CAD_TextEditInsert", "CAD_TextEditCommit",
        };

        uint _text, _rect, _hole;
        IValue<string> _content;

        protected override void Setup()
        {
            _text = Check("문자", CAD_CreateText(0, 6, 0, "VulkanCAD 문자", 0.6f));

            // 치수: 측정점 두 개 + 치수선이 지날 점 + 평면 법선
            var line = Tint(CAD_CreateLine(0, 0, 0, 4, 0, 0), 0.7f, 0.7f, 0.7f);
            Check("선형 치수", CAD_CreateDimension(0, 0, 0, 4, 0, 0, 2, -1, 0, 0, 0, 1));
            Check("지시선", CAD_CreateLeader(4, 0, 0, 5.5f, 1.5f, 0, "끝점\n(4, 0)", 0.3f));

            // 해치: 바깥 사각형 + 안쪽 원 = 구멍 난 영역
            _rect = CAD_CreateRectangle(7, 0, 0, 11, 3, 0);
            _hole = CAD_CreateCircle(9, 1.5f, 0, 0.8f, 0, 0, 1);
            CAD_ClearSelection();
            Scene.Frame(1);

            Ui.Section("해치");
            Ui.Button("ANSI31 (사선)", () => Hatch("ANSI31", 0.5f, 0));
            Ui.Button("BRICK (벽돌)", () => Hatch("BRICK", 0.3f, 0));
            Ui.Button("SOLID (채움)", () => Hatch("SOLID", 1, 0));
            Ui.Note("경계가 같은 평면이면 한 객체가 되고, 안쪽 루프는 구멍이 된다.");

            Ui.Section("문자 내용");
            _content = Ui.Input("새 내용", "안녕하세요 VulkanCAD", "바꾸기", s =>
            {
                CAD_SetTextContent(_text, s);
                Log("지금 내용: " + Str((b, n) => CAD_GetTextContent(_text, b, n)));
            });
            Ui.Button("편집 세션으로 끝에 \"!\" 덧붙이기", () =>
            {
                if (!CAD_BeginTextEdit(_text)) { Log("편집 시작 실패"); return; }
                CAD_TextEditMoveCaret(10000);   // 끝으로
                CAD_TextEditInsert("!");
                CAD_TextEditCommit();
                Log("지금 내용: " + Str((b, n) => CAD_GetTextContent(_text, b, n)));
            });
        }

        void Hatch(string pattern, float scale, float angle)
        {
            var ids = new[] { _rect, _hole };
            Check($"해치 {pattern}", CAD_CreateHatch(ids, (uint)ids.Length, pattern, scale, angle));
            CAD_ClearSelection();
        }

        public override void Demo() => Hatch("ANSI31", 0.5f, 0);
    }
}
