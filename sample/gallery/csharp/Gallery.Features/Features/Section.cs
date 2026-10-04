using System.Text.Json;
using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery
{
    public sealed class Section : Feature
    {
        public override string Category => "보기";
        public override string Group => "단면";
        public override string Icon => "◩";
        public override string Title => "실시간 단면";
        public override string Summary =>
            "셰이더가 잘라 보여 주는 보기 상태(Undo 없음). 평면(한쪽을 자름)·슬라이스(두께만 남김)·상자 세 가지. " +
            "슬라이더 범위는 상태 JSON 의 sceneBounds 에서 읽는다. 지금 단면을 편집 가능한 2D 윤곽으로 뽑을 수도 있다.";
        public override string[] Apis => new[]
        {
            "CAD_SetSection", "CAD_SetSectionBox", "CAD_SetSectionOptions", "CAD_GetSectionStateJson", "CAD_ExtractSectionTo2D",
        };

        int _mode = 1, _axis;
        bool _flip;
        float _pos, _thick = 0.2f;
        float[] _min = { -1, -1, -1 }, _max = { 1, 1, 1 };
        IValue<float> _posSlider;

        protected override void Setup()
        {
            // 속이 있는 모델 — 바깥 관 + 안쪽 기둥 + 구
            Tint(CAD_CreateTube(0, 0, 0, 2, 1.6f, 3), 0.6f, 0.65f, 0.75f);
            Tint(CAD_CreateCylinder(0, 0, 0, 0.6f, 3.5f), 0.85f, 0.55f, 0.35f);
            Tint(CAD_CreateSphere(0, 0, 3.5f, 0.9f), 0.45f, 0.70f, 0.90f);
            Tint(Box(3.5f, 0, 2, 2, 2), 0.55f, 0.80f, 0.45f);
            CAD_ClearSelection();
            Scene.Frame();
            ReadBounds();

            Ui.Section("단면");
            Ui.Choice("방식", new[] { "끄기", "평면", "슬라이스", "상자" }, _mode, i => { _mode = i; Apply(); });
            Ui.Choice("축", new[] { "X", "Y", "Z" }, _axis, i => { _axis = i; _pos = (_min[i] + _max[i]) / 2; _posSlider.Value = Norm(); Apply(); });
            _posSlider = Ui.Slider("위치 (범위의 %)", 0, 100, 50, v => { _pos = _min[_axis] + (_max[_axis] - _min[_axis]) * v / 100; Apply(); }, 0);
            Ui.Slider("슬라이스 두께", 0.05f, 2, _thick, v => { _thick = v; Apply(); }, 2);
            Ui.Toggle("반대쪽 자르기", false, v => { _flip = v; Apply(); });
            Ui.Toggle("방향 화살표 표시", true, v => CAD_SetSectionOptions(v, false));
            Ui.Section("뽑기");
            Ui.Button("지금 단면 → 2D 윤곽(폴리선)", () => Log($"ExtractSectionTo2D → 객체 {CAD_ExtractSectionTo2D(true)}개 (모델 오른쪽 XY 평면에)"));
            Ui.Button("상태 JSON", () => Log(Str(CAD_GetSectionStateJson)));
            _pos = (_min[_axis] + _max[_axis]) / 2;
            Apply();
        }

        float Norm() => (_pos - _min[_axis]) / (_max[_axis] - _min[_axis]) * 100;

        // 상태 JSON 의 sceneBounds = 슬라이더 범위
        void ReadBounds()
        {
            string json = Str(CAD_GetSectionStateJson);
            if (json == null) return;
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("sceneBounds", out var b)) return;
            for (int i = 0; i < 3; i++)
            {
                _min[i] = b.GetProperty("min")[i].GetSingle();
                _max[i] = b.GetProperty("max")[i].GetSingle();
            }
        }

        void Apply()
        {
            if (_mode == 3)
            {
                // 상자: 장면 경계의 가운데 절반만 남긴다
                float[] c = new float[3], h = new float[3];
                for (int i = 0; i < 3; i++) { c[i] = (_min[i] + _max[i]) / 2; h[i] = (_max[i] - _min[i]) / 4; }
                CAD_SetSectionBox(c[0] - h[0], c[1] - h[1], c[2] - h[2], _pos, c[1] + h[1], c[2] + h[2]);
            }
            CAD_SetSection(_mode, _axis, _pos, _flip, _thick);
        }

        public override void Leave()
        {
            CAD_SetSection(0, 0, 0, false, 0);
            base.Leave();
        }
    }
}
