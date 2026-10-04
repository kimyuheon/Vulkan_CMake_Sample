using System;
using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery
{
    public sealed class Transform : Feature
    {
        public override string Category => "편집";
        public override string Group => "변환";
        public override string Icon => "✥";
        public override string Title => "이동·회전·축척";
        public override string Summary =>
            "객체 하나의 위치·회전·축척을 값으로 넣고 읽는다. 외부가 매 프레임 자세를 밀어넣을 땐 위치+회전을 한 번에 " +
            "넣는 CAD_SetTransform(쿼터니언 qx,qy,qz,qw — ROS 순서)을 쓴다. 아래 '선택 대상' 명령은 Undo 가 남는 편집이다.";
        public override string[] Apis => new[]
        {
            "CAD_SetPosition", "CAD_SetRotationAxisAngle", "CAD_SetScale", "CAD_SetTransform", "CAD_GetTransform",
            "CAD_RequestMoveSelected", "CAD_RequestRotateSelected", "CAD_RequestScaleSelected", "CAD_RequestCopySelected",
        };

        uint _box;
        float _x, _y, _z = 0.3f, _yaw, _scale = 1;
        bool _spin;
        double _t;

        protected override void Setup()
        {
            _box = Tint(Check("상자", Box(0, 0, 1.5f, 1, 0.6f)), 0.85f, 0.55f, 0.35f);
            Tint(Box(4, 0, 1, 1, 1), 0.5f, 0.6f, 0.7f);   // 비교용
            CAD_ClearSelection();
            Scene.Frame();

            Ui.Section("값으로 (주황 상자)");
            Ui.Slider("X", -4, 4, 0, v => { _x = v; Apply(); });
            Ui.Slider("Y", -4, 4, 0, v => { _y = v; Apply(); });
            Ui.Slider("Z (상자 중심)", 0.3f, 3, 0.3f, v => { _z = v; Apply(); });
            Ui.Slider("Z축 회전 (°)", -180, 180, 0, v => { _yaw = v; Apply(); }, 0);
            Ui.Slider("축척", 0.2f, 3, 1, v => { _scale = v; CAD_SetScale(_box, v, v, v); }, 2);
            Ui.Toggle("돌리기 (매 프레임 SetTransform)", false, on => _spin = on);
            Ui.Button("지금 변환 읽기 (GetTransform)", Read);

            Ui.Section("선택 대상 편집 (Undo 가능)");
            Ui.Button("선택 → +X 로 1 이동", () => CAD_RequestMoveSelected(1, 0, 0, 1));
            Ui.Button("선택 → Z축 45° 회전", () => CAD_RequestRotateSelected(0, 0, 1, 45));
            Ui.Button("선택 → 1.5배", () => CAD_RequestScaleSelected(1.5f));
            Ui.Button("선택 → +Y 로 1.5 간격 3개 복사", () => CAD_RequestCopySelected(0, 1, 0, 1.5f, 3));
            Ui.Note("화면에서 객체를 클릭해 선택한 뒤 누른다. 이 명령들은 요청형이라 다음 프레임에 적용된다.");
        }

        // 위치 + Z축 회전을 한 호출로. 쿼터니언(Z축 θ) = (0, 0, sin θ/2, cos θ/2)
        void Apply()
        {
            float h = _yaw * MathF.PI / 360f;
            CAD_SetTransform(_box, _x, _y, _z, 0, 0, MathF.Sin(h), MathF.Cos(h));
        }

        void Read()
        {
            CAD_GetTransform(_box, out float x, out float y, out float z, out float qx, out float qy, out float qz, out float qw);
            CAD_GetScale(_box, out float sx, out _, out _);
            Log($"위치 ({x:0.##}, {y:0.##}, {z:0.##})  쿼터니언 ({qx:0.###}, {qy:0.###}, {qz:0.###}, {qw:0.###})  축척 {sx:0.##}");
        }

        protected override void Update(double dt)
        {
            if (!_spin) return;
            _t += dt;
            _yaw = (float)(_t * 90 % 360);
            _z = 0.5f + 0.5f * MathF.Sin((float)_t * 2);
            Apply();
        }

        public override void Demo()
        {
            _x = 1; _y = 1.5f; _z = 0.5f; _yaw = 30; Apply();
            CAD_SelectObject(_box, false);
            CAD_RequestCopySelected(0, 1, 0, 1.5f, 2);
            Later(3, Read);
        }
    }
}
