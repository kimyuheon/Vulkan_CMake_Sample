using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery
{
    public sealed class SketchToSolid : Feature
    {
        public override string Category => "만들기";
        public override string Group => "3D";
        public override string Icon => "⇧";
        public override string Title => "스케치 → 솔리드";
        public override string Summary =>
            "닫힌 2D 스케치로 솔리드를 만든다. 돌출(높이), 회전체(축), 스윕(경로를 따라), 로프트(단면 여러 개를 이음). " +
            "돌출·회전체·스윕은 피처 기록이 남아 스케치를 고치면 따라 바뀐다.";
        public override string[] Apis => new[] { "CAD_CreateExtrude", "CAD_Revolve", "CAD_Sweep", "CAD_Loft" };

        uint _rect, _profile, _sweepProfile, _path;
        uint[] _sections;

        protected override void Setup()
        {
            // 돌출용 사각형
            _rect = CAD_CreateRectangle(0, 0, 0, 2, 1.5f, 0);
            Caption(0, -0.8f, "돌출");

            // 회전체용 단면 — XZ 평면(y=0)의 닫힌 폴리선, 축은 x=3.5 의 세로선
            _profile = CAD_CreatePolyline(new float[]
            {
                4.0f, 0, 0,   4.8f, 0, 0,   4.8f, 0, 0.4f,   4.3f, 0, 0.9f,   4.4f, 0, 1.8f,   4.0f, 0, 1.8f,
            }, 6, true);
            Tint(CAD_CreateLine(3.5f, 0, -0.2f, 3.5f, 0, 2.2f), 0.9f, 0.4f, 0.4f);   // 축 표시
            Caption(3, -0.8f, "회전체");

            // 스윕 — 원 단면 + 3D 경로
            _sweepProfile = CAD_CreateCircle(7, 0, 0, 0.2f, 1, 0, 0);
            _path = CAD_CreatePolyline(new float[] { 7, 0, 0, 8, 0, 0.5f, 8.5f, 1, 1.5f, 8.5f, 2, 2 }, 4, false);
            Caption(6.8f, -0.8f, "스윕");

            // 로프트 — 높이마다 다른 단면 세 개
            _sections = new[]
            {
                CAD_CreateRectangle(10, 0, 0, 12, 2, 0),
                CAD_CreateCircle(11, 1, 1.2f, 0.7f, 0, 0, 1),
                CAD_CreateCircle(11, 1, 2.2f, 0.3f, 0, 0, 1),
            };
            Caption(10, -0.8f, "로프트");
            CAD_ClearSelection();
            Scene.Frame();

            Ui.Section("만들기");
            Ui.Button("돌출 — 사각형 높이 1.2", Extrude);
            Ui.Button("회전체 — 단면을 축 둘레로 360°", Revolve);
            Ui.Button("스윕 — 원을 경로를 따라", Sweep);
            Ui.Button("로프트 — 단면 세 개를 이음", Loft);
            Ui.Note("로프트는 **선택**을 입력으로 쓴다: 단면들을 선택한 뒤 CAD_Loft().");
        }

        void Extrude() { Tint(Check("돌출", CAD_CreateExtrude(_rect, 1.2f)), 0.85f, 0.55f, 0.35f); }
        void Revolve() { Tint(Check("회전체", CAD_Revolve(_profile, 3.5f, 0, 0, 3.5f, 0, 1, 48)), 0.45f, 0.70f, 0.90f); }
        void Sweep() { Tint(Check("스윕", CAD_Sweep(_sweepProfile, _path)), 0.55f, 0.80f, 0.45f); }

        void Loft()
        {
            CAD_ClearSelection();
            foreach (var id in _sections) CAD_SelectObject(id, true);
            Log(CAD_Loft() ? "로프트 → 성공 (새 솔리드가 선택됨)" : "로프트 실패 — " + Cad.StatusMessage());
        }

        public override void Demo()
        {
            Extrude(); Revolve(); Sweep(); Loft();
            CAD_ClearSelection();
        }
    }
}
