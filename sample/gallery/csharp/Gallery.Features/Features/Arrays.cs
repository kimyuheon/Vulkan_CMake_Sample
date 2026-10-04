using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery
{
    public sealed class Arrays : Feature
    {
        public override string Category => "편집";
        public override string Group => "변환";
        public override string Icon => "⁙";
        public override string Title => "배열·정렬";
        public override string Summary =>
            "선택을 격자·원형으로 복제하고, 점 쌍으로 다른 객체에 맞춰 붙인다. " +
            "ArrayRect/ArrayPolar 는 활성 뷰 평면 기준, Array3DRect/Array3DPolar 는 월드 축 기준이다.";
        public override string[] Apis => new[]
        {
            "CAD_Array3DRect", "CAD_Array3DPolar", "CAD_ArrayRect", "CAD_ArrayPolar", "CAD_Align3D",
        };

        uint _cube, _pin, _wedge, _base;

        protected override void Setup()
        {
            _cube = Tint(Box(0, 0, 0.6f, 0.6f, 0.6f), 0.85f, 0.55f, 0.35f);
            Caption(0, -1, "격자 배열");
            _pin = Tint(CAD_CreateCylinder(9, 0, 0, 0.25f, 1), 0.45f, 0.70f, 0.90f);
            Tint(CAD_CreateCylinder(7.5f, 0, 0, 0.1f, 1.5f), 0.9f, 0.4f, 0.4f);   // 회전축 표시
            Caption(6.5f, -2, "원형 배열 (빨간 축)");
            _base = Tint(Box(0, 6, 2, 2, 1), 0.6f, 0.6f, 0.6f);
            _wedge = Tint(CAD_CreateWedge(4, 7, 0, 1, 1, 1), 0.55f, 0.80f, 0.45f);
            Caption(0, 4.5f, "정렬 (쐐기 → 상자 위)");
            CAD_ClearSelection();
            Scene.Frame();

            Ui.Section("월드 기준");
            Ui.Button("격자 4×3×2 (간격 1)", Grid);
            Ui.Button("원형 8개 — 빨간 축 둘레 360°", Polar);
            Ui.Section("뷰 평면 기준");
            Ui.Button("평면 뷰에서 사각 배열 3×3", () =>
            {
                CAD_RequestSetView(1);   // 요청은 다음 틱에 — 뷰가 바뀐 뒤 배열해야 XY 평면이 된다
                Later(2, () => { Select(_cube); Log("ArrayRect → " + CAD_ArrayRect(3, 3, 1, 1)); });
            });
            Ui.Section("정렬");
            Ui.Button("쐐기를 상자 윗면 모서리에 맞춰 붙이기", Align);
        }

        static void Select(uint id) { CAD_ClearSelection(); CAD_SelectObject(id, false); }

        void Grid()
        {
            Select(_cube);
            Log("Array3DRect → " + CAD_Array3DRect(4, 3, 2, 1, 1, 1));
            CAD_ClearSelection();
        }

        void Polar()
        {
            Select(_pin);
            // 축 = 두 점 (7.5,0,0)→(7.5,0,1), count 는 원본 포함
            Log("Array3DPolar → " + CAD_Array3DPolar(8, 360, 7.5f, 0, 0, 7.5f, 0, 1, true));
            CAD_ClearSelection();
        }

        // 점 쌍 2개: 쐐기 밑면 모서리(src) → 상자 윗면 모서리(dst). 1쌍=이동, 2쌍=방향까지, 3쌍=완전 정렬
        // 모서리 좌표는 경계 상자(GetBoundsWorld)에서 읽는다.
        void Align()
        {
            CAD_GetBoundsWorld(_wedge, out float wx, out float wy, out float wz, out _, out _, out _);
            CAD_GetBoundsWorld(_base, out float bx, out float by, out _, out _, out _, out float bTop);
            var ids = new[] { _wedge };
            var src = new float[] { wx, wy, wz, wx + 1, wy, wz };
            var dst = new float[] { bx, by, bTop, bx + 1, by, bTop };
            Log("Align3D → 옮긴 객체 " + CAD_Align3D(ids, 1, src, dst, 2));
        }

        public override void Demo() { Grid(); Polar(); Align(); }
    }
}
