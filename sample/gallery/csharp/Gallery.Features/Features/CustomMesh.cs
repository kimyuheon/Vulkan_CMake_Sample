using System;
using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery
{
    public sealed class CustomMesh : Feature
    {
        public override string Category => "만들기";
        public override string Group => "3D";
        public override string Icon => "▦";
        public override string Title => "직접 만든 메시";
        public override string Summary =>
            "정점·삼각형 배열로 메시를 만들고(CAD_CreateMesh), 매 프레임 형상만 바꾼다(CAD_ReplaceMesh). " +
            "id 가 그대로라 선택·색·변환이 유지된다 — 시뮬레이션 결과나 외부 형상을 흘려 넣을 때의 방법.";
        public override string[] Apis => new[] { "CAD_CreateMesh", "CAD_ReplaceMesh" };

        const int N = 40;          // 격자 한 변의 정점 수
        const float Size = 6;
        uint _mesh;
        float[] _xyz;
        uint[] _idx;
        double _t;
        bool _animate = true;
        float _amp = 0.5f, _freq = 1.2f;

        protected override void Setup()
        {
            BuildGrid();
            Wave(0);
            // normals = null → 삼각형마다 평평하게. 매끈하게 보이려면 정점 법선을 넘긴다.
            _mesh = Tint(Check("메시", CAD_CreateMesh(_xyz, (uint)(_xyz.Length / 3), _idx, (uint)_idx.Length, null)), 0.35f, 0.65f, 0.95f);
            CAD_ClearSelection();
            Scene.Frame();

            Ui.Section("물결");
            Ui.Toggle("애니메이션 (매 프레임 ReplaceMesh)", true, on => _animate = on);
            Ui.Slider("높이", 0, 1.5f, _amp, v => _amp = v, 2);
            Ui.Slider("주파수", 0.2f, 3, _freq, v => _freq = v, 2);
            Log($"정점 {_xyz.Length / 3}개, 삼각형 {_idx.Length / 3}개");
        }

        void BuildGrid()
        {
            _xyz = new float[N * N * 3];
            _idx = new uint[(N - 1) * (N - 1) * 6];
            int k = 0;
            for (int y = 0; y < N - 1; y++)
                for (int x = 0; x < N - 1; x++)
                {
                    uint a = (uint)(y * N + x), b = a + 1, c = a + N, d = c + 1;
                    _idx[k++] = a; _idx[k++] = b; _idx[k++] = d;
                    _idx[k++] = a; _idx[k++] = d; _idx[k++] = c;
                }
        }

        void Wave(double t)
        {
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float px = (x / (float)(N - 1) - 0.5f) * Size, py = (y / (float)(N - 1) - 0.5f) * Size;
                    float r = MathF.Sqrt(px * px + py * py);
                    int i = (y * N + x) * 3;
                    _xyz[i] = px; _xyz[i + 1] = py;
                    _xyz[i + 2] = _amp * MathF.Sin(r * _freq * 2 - (float)t * 3) * MathF.Exp(-r * 0.25f);
                }
        }

        protected override void Update(double dt)
        {
            if (!_animate || _mesh == 0) return;
            _t += dt;
            Wave(_t);
            CAD_ReplaceMesh(_mesh, _xyz, (uint)(_xyz.Length / 3), _idx, (uint)_idx.Length, null);
        }
    }
}
