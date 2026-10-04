using System;
using System.Collections.Generic;
using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery
{
    // sample/shared/robot_demo.cpp(iOS·Android 공용)와 같은 일을 C# 으로.
    public sealed class RobotArm : Feature
    {
        public override string Category => "로봇·시뮬";
        public override string Group => "로봇";
        public override string Icon => "⚙";
        public override string Title => "로봇 팔 (URDF)";
        public override string Summary =>
            "URDF 를 불러와 관절을 슬라이더로 움직인다. 회전 관절은 도(°), 손가락 직선 관절은 그리퍼 하나(0~100%)로 묶는다. " +
            "여러 관절은 CAD_SetJointValues 로 한 번에 넣어야 계층 계산이 한 번만 돈다.";
        public override string[] Apis => new[]
        {
            "CAD_LoadUrdf", "CAD_GetJointCount", "CAD_GetJointInfo", "CAD_GetJointName",
            "CAD_SetJointValues", "CAD_GetJointValue",
        };

        const float RadToDeg = 180f / MathF.PI;

        // 조작 항목 하나 = 회전 관절 하나, 또는 손가락 관절 여럿(그리퍼)
        sealed class Control
        {
            public string Label;
            public int[] Joints;
            public float Lower, Upper;      // 관절 단위(라디안/미터)
            public bool Gripper;            // 값 = 0~100 %
            public float Speed, Phase;      // 자동 재생용
            public IValue<float> Slider;
        }

        uint _robot;
        readonly List<Control> _controls = new List<Control>();
        bool _playing;
        double _time;
        IValue<bool> _playToggle;

        protected override void Setup()
        {
            _robot = CAD_LoadUrdf("models/demo_arm.urdf");   // 상대 경로 = 런타임 에셋 폴더 기준
            if (_robot == 0) { Log("URDF 불러오기 실패 — models/demo_arm.urdf 확인"); return; }
            BuildControls();
            CAD_ClearSelection();
            Scene.Frame();

            Ui.Section("관절");
            foreach (var c in _controls)
            {
                var cc = c;
                float min = c.Gripper ? 0 : c.Lower * RadToDeg, max = c.Gripper ? 100 : c.Upper * RadToDeg;
                c.Slider = Ui.Slider($"{c.Label} ({(c.Gripper ? "%" : "°")})", min, max, 0, v => { _playing = false; _playToggle.Value = false; Apply(cc, v); });
            }
            Ui.Section("동작");
            _playToggle = Ui.Toggle("자동 재생", false, on => _playing = on);
            Ui.Button("원위치 (모든 관절 0)", Home);
            Log($"로봇 핸들 {_robot}, 관절 {CAD_GetJointCount(_robot)}개 → 조작 항목 {_controls.Count}개");
        }

        void BuildControls()
        {
            _controls.Clear();
            var grip = new Control { Label = "그리퍼", Gripper = true, Speed = 0.9f };
            var gripJoints = new List<int>();
            int n = (int)CAD_GetJointCount(_robot);
            for (int j = 0; j < n; j++)
            {
                if (!CAD_GetJointInfo(_robot, j, out int type, out float lo, out float hi, out bool hasLimit)) continue;
                if (type == 0) continue;                // 고정 관절
                if (type == 3)                          // 직선 관절 = 손가락 → 그리퍼로 묶음
                {
                    if (gripJoints.Count == 0) { grip.Lower = lo; grip.Upper = hi; }
                    gripJoints.Add(j);
                    continue;
                }
                string name = Str((buf, cap) => CAD_GetJointName(_robot, j, buf, cap)) ?? $"joint {j}";
                int k = _controls.Count;
                _controls.Add(new Control
                {
                    Label = name.StartsWith("joint_") ? name.Substring(6) : name,
                    Joints = new[] { j },
                    Lower = hasLimit ? lo : -MathF.PI,   // 한계 없는(continuous) 관절은 한 바퀴
                    Upper = hasLimit ? hi : MathF.PI,
                    Speed = 0.35f + 0.12f * (k % 4),
                    Phase = 1.3f * k,
                });
            }
            if (gripJoints.Count > 0) { grip.Joints = gripJoints.ToArray(); _controls.Add(grip); }
        }

        // 화면 값(° 또는 %) → 관절 값(라디안/미터). 관절 여럿이면 한 번에 넣는다.
        void Apply(Control c, float display)
        {
            float j = c.Gripper ? c.Lower + (c.Upper - c.Lower) * (display / 100f) : display / RadToDeg;
            var values = new float[c.Joints.Length];
            Array.Fill(values, j);
            CAD_SetJointValues(_robot, c.Joints, values, (uint)values.Length);
        }

        void Home()
        {
            _playing = false; _playToggle.Value = false;
            foreach (var c in _controls) { Apply(c, 0); c.Slider.Value = 0; }
        }

        protected override void Update(double dt)
        {
            if (!_playing || _robot == 0) return;
            _time += dt;
            foreach (var c in _controls)
            {
                float s = MathF.Sin((float)_time * c.Speed * MathF.PI * 0.5f + c.Phase);
                // 회전 관절은 한계의 40 % 까지만 — 팔이 바닥을 뚫지 않을 만큼
                float v = c.Gripper ? 50 + 50 * s
                                    : 0.4f * MathF.Min(MathF.Abs(c.Lower), MathF.Abs(c.Upper)) * RadToDeg * s;
                Apply(c, v);
                c.Slider.Value = v;
            }
        }
    }
}
