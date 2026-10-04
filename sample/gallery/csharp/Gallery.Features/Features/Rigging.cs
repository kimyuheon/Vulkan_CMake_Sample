using System;
using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery
{
    public sealed class Rigging : Feature
    {
        public override string Category => "로봇·시뮬";
        public override string Group => "로봇";
        public override string Icon => "⛓";
        public override string Title => "런타임 리깅 (URDF 없이)";
        public override string Summary =>
            "평범한 상자들을 부모-자식으로 붙여 관절을 만든다. 붙여도 부품은 제자리에 있고, 축의 **위치**(피벗)를 따로 준다 — " +
            "문 경첩처럼 회전 중심이 부품 중심이 아닐 때가 많다. 붙인 뒤엔 URDF 로봇과 같은 관절 API 로 움직인다.";
        public override string[] Apis => new[]
        {
            "CAD_RigAttach", "CAD_RigDetach", "CAD_GetObjectRobot", "CAD_GetJointCount", "CAD_SetJointValue",
        };

        uint _base, _arm1, _arm2, _robot;
        bool _animate;
        double _t;
        IValue<float> _s1, _s2;

        protected override void Setup()
        {
            _base = Tint(CAD_CreateCylinder(0, 0, 0, 0.8f, 0.4f), 0.4f, 0.4f, 0.45f);
            _arm1 = Tint(Box(0, 0, 0.4f, 0.4f, 2, 0.4f), 0.95f, 0.65f, 0.25f);           // z 0.4~2.4
            _arm2 = Tint(Box(0, 0.8f, 0.3f, 1.6f, 0.3f, 2.25f), 0.45f, 0.70f, 0.90f);    // 끝에서 앞으로
            CAD_ClearSelection();
            Scene.Frame();

            Ui.Section("붙이기");
            Ui.Button("리깅: 받침 ⟶ 팔1(Z 회전) ⟶ 팔2(X 회전)", Rig);
            Ui.Button("떼기 (팔1 아래 가지 전부)", () => { Log("RigDetach → " + CAD_RigDetach(_arm1)); _robot = 0; });
            Ui.Section("관절 (붙인 뒤)");
            _s1 = Ui.Slider("관절 0 — 팔1 회전 (Z축, °)", -180, 180, 0, v => Joint(0, v), 0);
            _s2 = Ui.Slider("관절 1 — 팔2 꺾기 (X축, °)", -120, 120, 0, v => Joint(1, v), 0);
            Ui.Toggle("움직이기", false, on => _animate = on);
        }

        void Rig()
        {
            // 받침 → 팔1: Z축 회전, 피벗 = 받침 윗면 중심
            bool a = CAD_RigAttach(_base, _arm1, 0, 0, 1, 1, 0, 0, 0.4f, true);
            // 팔1 → 팔2: X축 회전, 피벗 = 팔1 꼭대기
            bool b = CAD_RigAttach(_arm1, _arm2, 1, 0, 0, 1, 0, 0, 2.25f, true);
            _robot = CAD_GetObjectRobot(_arm1);
            Log($"붙이기 {a}/{b} → 로봇 핸들 {_robot}, 관절 {(_robot != 0 ? CAD_GetJointCount(_robot) : 0)}개");
            if (_robot == 0) return;
            for (int j = 0; j < CAD_GetJointCount(_robot); j++)
            {
                int k = j;
                Log($"  관절 {j}: {Str((buf, cap) => CAD_GetJointName(_robot, k, buf, cap))}");
            }
        }

        void Joint(int index, float deg)
        {
            if (_robot == 0) { Log("먼저 리깅"); return; }
            if (index < CAD_GetJointCount(_robot)) CAD_SetJointValue(_robot, index, deg * MathF.PI / 180);
        }

        protected override void Update(double dt)
        {
            if (!_animate || _robot == 0) return;
            _t += dt;
            float a = (float)(_t * 40 % 360) - 180, b = 60 * MathF.Sin((float)_t * 1.7f);
            Joint(0, a); Joint(1, b);
            _s1.Value = a; _s2.Value = b;
        }

        public override void Demo()
        {
            Rig();
            Joint(0, 40); Joint(1, -70);
            _s1.Value = 40; _s2.Value = -70;
        }
    }
}
