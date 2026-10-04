using System;
using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery
{
    public sealed class PointCloud : Feature
    {
        public override string Category => "로봇·시뮬";
        public override string Group => "센서";
        public override string Icon => "⁘";
        public override string Title => "점군 (라이다 스캔)";
        public override string Summary =>
            "점군은 한 번 만들고 매 스캔마다 Update 한다 — id 가 유지돼 선택·변환이 풀리지 않는다. " +
            "점은 객체 로컬 좌표로 넘기고 센서 자세는 SetTransform 으로 따로 준다(점을 CPU 에서 변환하지 말 것).";
        public override string[] Apis => new[]
        {
            "CAD_CreatePointCloud", "CAD_UpdatePointCloud", "CAD_SetPointCloudSize", "CAD_SetTransform", "CAD_GetPointCloudCount",
        };

        uint _cloud;
        float[] _xyz;
        byte[] _rgba;
        double _t;
        bool _animate = true, _colored = true;
        int _beams = 32, _steps = 360;

        protected override void Setup()
        {
            // 스캔할 방 — 벽 4개 + 기둥 + 상자
            foreach (var (x, y, sx, sy) in new[] { (0f, 6f, 12f, 0.2f), (0f, -6f, 12f, 0.2f), (6f, 0f, 0.2f, 12f), (-6f, 0f, 0.2f, 12f) })
                Tint(Box(x, y, sx, sy, 3), 0.35f, 0.37f, 0.42f);
            Tint(CAD_CreateCylinder(2, 2, 0, 0.6f, 3), 0.35f, 0.37f, 0.42f);
            Tint(Box(-3, 1, 1.5f, 1.5f, 1), 0.35f, 0.37f, 0.42f);

            _cloud = CAD_CreatePointCloud();
            CAD_SetPointCloudSize(_cloud, 3);
            Scan(0);
            Scene.Frame();

            Ui.Section("스캔");
            Ui.Toggle("돌리기 (매 프레임 UpdatePointCloud)", true, on => _animate = on);
            Ui.Toggle("높이별 색 (끄면 객체 색 단색)", true, on => _colored = on);
            Ui.Slider("점 크기 (px)", 1, 8, 3, v => CAD_SetPointCloudSize(_cloud, v), 0);
            Ui.Slider("빔 수", 4, 64, _beams, v => _beams = (int)v, 0);
        }

        // 원점 센서에서 수평 360° × 빔(세로각) 광선을 쏴 방 경계(축에 나란한 상자)에 맞는 점을 만든다
        void Scan(double t)
        {
            int n = _beams * _steps;
            if (_xyz == null || _xyz.Length != n * 3) { _xyz = new float[n * 3]; _rgba = new byte[n * 4]; }
            float spin = (float)t * 0.8f;
            int k = 0;
            for (int b = 0; b < _beams; b++)
            {
                float pitch = (-15 + 30f * b / Math.Max(1, _beams - 1)) * MathF.PI / 180;
                for (int s = 0; s < _steps; s++)
                {
                    float yaw = s * MathF.PI * 2 / _steps + spin;
                    float dx = MathF.Cos(pitch) * MathF.Cos(yaw), dy = MathF.Cos(pitch) * MathF.Sin(yaw), dz = MathF.Sin(pitch);
                    float d = Hit(dx, dy, dz);
                    float x = dx * d, y = dy * d, z = 1.5f + dz * d;
                    _xyz[k * 3] = x; _xyz[k * 3 + 1] = y; _xyz[k * 3 + 2] = z;
                    float h = Math.Clamp(z / 3, 0, 1);   // 높이 → 파랑~빨강
                    _rgba[k * 4] = (byte)(255 * h); _rgba[k * 4 + 1] = (byte)(255 * (1 - MathF.Abs(h - 0.5f) * 2)); _rgba[k * 4 + 2] = (byte)(255 * (1 - h)); _rgba[k * 4 + 3] = 255;
                    k++;
                }
            }
            CAD_UpdatePointCloud(_cloud, _xyz, _colored ? _rgba : null, (uint)n);
        }

        // 센서(0,0,1.5)에서 방향 (dx,dy,dz) 로 가장 가까운 면까지 거리 — 벽·바닥·천장·기둥
        static float Hit(float dx, float dy, float dz)
        {
            float best = 100;
            void Plane(float o, float d) { if (d * o > 0) { float t = o / d; if (t > 0 && t < best) best = t; } }
            Plane(5.9f, dx); Plane(-5.9f, dx); Plane(5.9f, dy); Plane(-5.9f, dy); Plane(-1.5f, dz); Plane(1.5f, dz);
            // 기둥 (2,2) r=0.6
            float a = dx * dx + dy * dy, bq = -2 * (dx * 2 + dy * 2), c = 8 - 0.36f, disc = bq * bq - 4 * a * c;
            if (disc > 0) { float t = (-bq - MathF.Sqrt(disc)) / (2 * a); if (t > 0 && t < best && MathF.Abs(1.5f + dz * t - 1.5f) < 1.5f) best = t; }
            return best;
        }

        protected override void Update(double dt)
        {
            if (!_animate) return;
            _t += dt;
            Scan(_t);
        }
    }
}
