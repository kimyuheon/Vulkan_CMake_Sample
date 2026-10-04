using System;
using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery
{
    public sealed class Jig : Feature
    {
        public override string Category => "편집";
        public override string Group => "변환";
        public override string Icon => "☝";
        public override string Title => "Jig (끌어서 놓기)";
        public override string Summary =>
            "AutoCAD AcEdJig 처럼 객체가 커서를 따라다니다 클릭에 확정된다(ESC 취소, Undo 한 단계). " +
            "변형 Jig 는 후보 여러 개를 Tab(또는 NextVariant)으로 갈아끼운다 — 문 좌/우 열림, 볼트 규격 같은 선택에.";
        public override string[] Apis => new[]
        {
            "CAD_JigBeginOne", "CAD_JigBeginVariants", "CAD_JigNextVariant", "CAD_JigMoveTo", "CAD_JigCommit",
            "CAD_JigCancel", "CAD_JigState", "CAD_JigGetDelta",
        };

        uint _box;
        uint[] _variants;
        bool _autoDrive;
        double _t;
        int _lastState;

        protected override void Setup()
        {
            _box = Tint(Box(0, 0, 1, 1, 1), 0.85f, 0.55f, 0.35f);
            Tint(Box(0, 0, 8, 8, 0.05f, -0.05f), 0.3f, 0.32f, 0.36f);   // 바닥판
            CAD_ClearSelection();
            Scene.Frame();

            Ui.Section("이동 Jig");
            Ui.Button("시작 — 상자가 커서를 따라온다", () => Begin(0));
            Ui.Button("회전 Jig 시작 (피벗 = 원점)", () => Begin(1));
            Ui.Toggle("코드로 끌기 (원을 그리며 JigMoveTo)", false, on => _autoDrive = on);
            Ui.Button("확정 (클릭과 같음)", () => Log("Commit → " + CAD_JigCommit()));
            Ui.Button("취소 (ESC 와 같음)", CAD_JigCancel);

            Ui.Section("변형 Jig");
            Ui.Button("후보 3개(상자·원기둥·구)로 시작", BeginVariants);
            Ui.Button("다음 후보 (Tab 과 같음)", () => Log($"후보 {(CAD_JigNextVariant() ? CAD_JigVariantIndex() : -1)}"));
            Ui.Note("확정하면 고른 후보만 남고 나머지는 지워진다. 취소하면 후보 전부 지워진다.");
        }

        void Begin(int mode)
        {
            Log((mode == 0 ? "이동" : "회전") + " Jig → " + CAD_JigBeginOne(mode, _box, 0, 0, 0));
        }

        void BeginVariants()
        {
            _variants = new[]
            {
                Tint(Box(0, 3, 1, 1, 1), 0.85f, 0.55f, 0.35f),
                Tint(CAD_CreateCylinder(0, 3, 0, 0.5f, 1), 0.45f, 0.70f, 0.90f),
                Tint(CAD_CreateSphere(0, 3, 0, 0.5f), 0.55f, 0.80f, 0.45f),
            };
            Log("변형 Jig → " + CAD_JigBeginVariants(0, _variants, _variants.Length, 0, 0, 0));
        }

        protected override void Update(double dt)
        {
            int s = CAD_JigState();
            if (s != _lastState)
            {
                if (s == 2) Log("확정됨");
                if (s == 3) Log("취소됨");
                _lastState = s;
            }
            if (!_autoDrive || s != 1) return;
            _t += dt;
            CAD_JigMoveTo(2.5f * MathF.Cos((float)_t), 2.5f * MathF.Sin((float)_t), 0);
            if (CAD_JigGetDelta(out float dx, out float dy, out _, out float ang, out _) && (int)(_t * 4) % 8 == 0)
                Ui.Log($"미리보기 이동 ({dx:0.0}, {dy:0.0}) 회전 {ang:0}°");
        }

        public override void Demo()
        {
            BeginVariants();
            CAD_JigNextVariant();
            CAD_JigMoveTo(2, 1, 0);
        }

        public override void Leave()
        {
            CAD_JigCancel();
            base.Leave();
        }
    }
}
