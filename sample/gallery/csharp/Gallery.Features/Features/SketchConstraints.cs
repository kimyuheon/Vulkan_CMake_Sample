using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery
{
    public sealed class SketchConstraints : Feature
    {
        public override string Category => "파라메트릭";
        public override string Group => "스케치";
        public override string Icon => "⊥";
        public override string Title => "스케치 구속";
        public override string Summary =>
            "삐뚤게 그린 선 네 개와 원에 조건(일치·수평·수직·거리·반지름)을 걸면 엔진이 풀어서 정확한 직사각형이 된다. " +
            "구동 치수 값을 바꾸면 다시 풀린다. 진단은 남은 자유도와 과잉·충돌 여부를 알려 준다.";
        public override string[] Apis => new[]
        {
            "CAD_AddSketchConstraint", "CAD_SetSketchConstraintValue", "CAD_SolveSketchConstraints",
            "CAD_GetSketchDiagnosis", "CAD_GetSketchConstraintInfo",
        };

        uint _l1, _l2, _l3, _l4, _circle, _width, _height, _radius;

        protected override void Setup()
        {
            // 일부러 삐뚤게 — 구속이 바로잡는다
            _l1 = CAD_CreateLine(0, 0, 0, 4.3f, 0.4f, 0);
            _l2 = CAD_CreateLine(4.3f, 0.4f, 0, 3.8f, 2.7f, 0);
            _l3 = CAD_CreateLine(3.8f, 2.7f, 0, 0.3f, 3.2f, 0);
            _l4 = CAD_CreateLine(0.3f, 3.2f, 0, 0, 0, 0);
            _circle = Tint(CAD_CreateCircle(2.3f, 1.2f, 0, 0.4f, 0, 0, 1), 0.45f, 0.70f, 0.90f);
            CAD_ClearSelection();
            Scene.Frame(1);

            Ui.Section("구속");
            Ui.Button("구속 걸고 풀기", Constrain);
            _widthSlider = Ui.Slider("가로 (구동 치수)", 1, 8, 4, v => SetValue(_width, v), 2);
            _heightSlider = Ui.Slider("세로 (구동 치수)", 1, 6, 2.5f, v => SetValue(_height, v), 2);
            Ui.Slider("원 반지름", 0.2f, 1.2f, 0.6f, v => SetValue(_radius, v), 2);
            Ui.Button("진단 (자유도·상태)", Diagnose);
            Ui.Note("구속은 점 번호로 건다: 선 0=시작 1=끝, 원 0=중심. 한 점만 고정하므로 전체가 정해진다.");
        }

        IValue<float> _widthSlider, _heightSlider;

        void Constrain()
        {
            if (_width != 0) { Log("이미 걸려 있다"); return; }
            // 끝점끼리 붙이기
            Add(CAD_SKETCH_COINCIDENT, _l1, 1, _l2, 0);
            Add(CAD_SKETCH_COINCIDENT, _l2, 1, _l3, 0);
            Add(CAD_SKETCH_COINCIDENT, _l3, 1, _l4, 0);
            Add(CAD_SKETCH_COINCIDENT, _l4, 1, _l1, 0);
            // 방향
            Add(CAD_SKETCH_HORIZONTAL, _l1, 0, 0, 0);
            Add(CAD_SKETCH_HORIZONTAL, _l3, 0, 0, 0);
            Add(CAD_SKETCH_VERTICAL, _l2, 0, 0, 0);
            Add(CAD_SKETCH_VERTICAL, _l4, 0, 0, 0);
            // 위치 고정 + 크기
            Add(CAD_SKETCH_FIXED, _l1, 0, 0, 0);
            _width = Add(CAD_SKETCH_DISTANCE, _l1, 0, _l1, 1, 4);
            _height = Add(CAD_SKETCH_DISTANCE, _l2, 0, _l2, 1, 2.5f);
            // 원: 반지름 + 사각형 왼쪽 아래 꼭짓점에서의 가로·세로 거리
            _radius = Add(CAD_SKETCH_RADIUS, _circle, 0, 0, 0, 0.6f);
            Add(CAD_SKETCH_DISTANCE_X, _l1, 0, _circle, 0, 2);
            Add(CAD_SKETCH_DISTANCE_Y, _l1, 0, _circle, 0, 1.25f);
            Log("풀기 → " + CAD_SolveSketchConstraints() + $", 구속 {CAD_GetSketchConstraintCount()}개");
            Diagnose();
        }

        uint Add(int type, uint a, int pa, uint b, int pb, float value = 0)
        {
            uint id = CAD_AddSketchConstraint(type, a, pa, b, pb, value);
            if (id == 0) Log($"구속 {type} 실패 — {Cad.LastError()}");
            return id;
        }

        void SetValue(uint constraint, float v)
        {
            if (constraint == 0) { Log("먼저 '구속 걸고 풀기'"); return; }
            if (!CAD_SetSketchConstraintValue(constraint, v)) Log($"값 {v:0.##} 실패 — 충돌 {CAD_GetLastSketchConflict(null, 0)}개");
        }

        void Diagnose()
        {
            string[] status = { "과소 구속", "완전 구속", "과잉", "충돌", "잘못된 참조" };
            int n = CAD_GetSketchDiagnosis(_l1, out int dof, out int st, null, 0);
            Log(n < 0 ? "진단 실패" : $"자유도 {dof}, 상태: {status[st]}, 문제 구속 {n}개");
        }

        public override void Demo()
        {
            Constrain();
            SetValue(_width, 5.5f); _widthSlider.Value = 5.5f;
            SetValue(_height, 3); _heightSlider.Value = 3;
        }
    }
}
