using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery
{
    public sealed class Edit2D : Feature
    {
        public override string Category => "편집";
        public override string Group => "2D 수정";
        public override string Icon => "✂";
        public override string Title => "오프셋·대칭·자르기·연장";
        public override string Summary =>
            "2D 수정 명령을 클릭 대신 값(점 좌표)으로 부른다 — 도구와 같은 계산, 전부 Undo 한 단계. " +
            "자르기·연장은 '어느 쪽'을 가리키는 점이 필요하다: 지울 부분 / 늘릴 끝 가까이의 점.";
        public override string[] Apis => new[]
        {
            "CAD_Offset", "CAD_Mirror", "CAD_Trim", "CAD_Extend", "CAD_BreakObject", "CAD_Lengthen",
            "CAD_StretchWindowXY", "CAD_Explode", "CAD_GetPolylinePoints", "CAD_SetPolylinePoints",
        };

        static readonly uint[] None = System.Array.Empty<uint>();
        uint _zigzag, _tri, _trimA, _trimB, _extC, _extD, _breakLine, _lenLine, _rect, _poly;

        protected override void Setup()
        {
            // 위 줄: 오프셋 · 대칭 · 자르기 · 연장
            _zigzag = CAD_CreatePolyline(new float[] { 0, 0, 0, 1, 1.5f, 0, 2, 0, 0, 3, 1.5f, 0 }, 4, false);
            Caption(0, -0.7f, "오프셋");
            _tri = CAD_CreatePolyline(new float[] { 4.5f, 0, 0, 5.5f, 0, 0, 4.5f, 1.5f, 0 }, 3, true);
            Tint(CAD_CreateLine(6, -0.3f, 0, 6, 1.8f, 0), 0.9f, 0.4f, 0.4f);   // 대칭선
            Caption(4.5f, -0.7f, "대칭");
            _trimA = CAD_CreateLine(8, 0.75f, 0, 11, 0.75f, 0);
            _trimB = Tint(CAD_CreateLine(9.5f, 0, 0, 9.5f, 1.5f, 0), 0.9f, 0.4f, 0.4f);
            Caption(8, -0.7f, "자르기");
            _extC = CAD_CreateLine(12, 0.75f, 0, 13, 0.75f, 0);
            _extD = Tint(CAD_CreateLine(14.5f, 0, 0, 14.5f, 1.5f, 0), 0.9f, 0.4f, 0.4f);
            Caption(12, -0.7f, "연장");

            // 아래 줄: 끊기 · 길이조정 · 늘이기 · 분해 · 정점 편집
            _breakLine = CAD_CreateLine(0, -3, 0, 3, -3, 0);
            Caption(0, -3.7f, "끊기");
            _lenLine = CAD_CreateLine(4.5f, -3, 0, 5.5f, -3, 0);
            Caption(4.5f, -3.7f, "길이조정");
            _rect = CAD_CreatePolyline(new float[] { 8, -3.5f, 0, 9, -3.5f, 0, 9, -2.5f, 0, 8, -2.5f, 0 }, 4, true);
            Caption(8, -4.2f, "늘이기");
            _poly = CAD_CreatePolygon(13, -3, 0, 0.8f, 5, 0, 0, 1, 0, 1, 0);
            Caption(12, -4.2f, "분해·정점");
            CAD_ClearSelection();
            Scene.Frame(1);

            Ui.Section("만들기형 (새 객체가 생김)");
            Ui.Button("오프셋 — 지그재그를 위쪽으로 0.3", Offset);
            Ui.Button("대칭 — 삼각형을 빨간 선에 비춰 복사", Mirror);
            Ui.Section("고치기형 (원본이 바뀜)");
            Ui.Button("자르기 — 빨간 선 오른쪽 부분 지우기", Trim);
            Ui.Button("연장 — 선 끝을 빨간 선까지", Extend);
            Ui.Button("끊기 — 1~2 사이를 지움", Break);
            Ui.Button("길이조정 — 전체 길이 2.5 로", Lengthen);
            Ui.Button("늘이기 — 사각형 오른쪽 변을 +1", Stretch);
            Ui.Button("분해 — 오각형을 선 다섯 개로", Explode);
            Ui.Button("정점 편집 — 오각형 꼭짓점 하나 끌어내기", EditVertex);
        }

        void Offset()
        {
            var ids = new[] { _zigzag };
            var outIds = new uint[4];
            // (sx,sy,sz) = 어느 쪽으로 복제할지 가리키는 점
            uint n = CAD_Offset(ids, 1, 0.3f, 1.5f, 3, 0, outIds, (uint)outIds.Length);
            Log($"오프셋 → 새 객체 {n}개");
        }

        void Mirror()
        {
            var ids = new[] { _tri };
            var outIds = new uint[4];
            // 미러선 (6,0)→(6,1) + 법선 0 = 지금 작업 평면
            uint n = CAD_Mirror(ids, 1, 6, 0, 0, 6, 1, 0, 0, 0, 0, false, outIds, (uint)outIds.Length);
            Log($"대칭 → 새 객체 {n}개");
        }

        void Trim()
        {
            var boundary = new[] { _trimB };
            // (10.5, 0.75) = 지울 부분 위의 점
            Log("자르기 → " + (CAD_Trim(_trimA, 10.5f, 0.75f, 0, boundary, 1, 0, 0, 1) ? "성공" : "실패"));
        }

        void Extend()
        {
            var boundary = new[] { _extD };
            // (12.9, 0.75) = 늘릴 끝 가까이
            Log("연장 → " + (CAD_Extend(_extC, 12.9f, 0.75f, 0, boundary, 1, 0, 0, 1) ? "성공" : "실패"));
        }

        void Break() => Log("끊기 → " + (CAD_BreakObject(_breakLine, 1, -3, 0, 2, -3, 0) ? "성공 (원본은 두 조각으로)" : "실패"));

        void Lengthen()
        {
            // mode 2 = 전체 길이로 맞춤 (0 = 증분, 1 = 퍼센트), atStart=false = 끝 쪽
            bool ok = CAD_Lengthen(_lenLine, 2, 2.5f, false);
            Log($"길이조정 → {(ok ? "성공" : "실패")}");
            // 형상 갱신은 다음 틱에 반영된다 — 길이는 한 프레임 뒤에 읽는다
            Later(2, () => { CAD_GetLength(_lenLine, out float len); Log($"  지금 길이 {len:0.##}"); });
        }

        void Stretch()
        {
            // 오른쪽 변만 걸치는 교차 창 → 그 안의 편집점만 +X 로
            uint n = CAD_StretchWindowXY(8.8f, -4, 9.2f, -2, 1, 0, 0);
            Log($"늘이기 → {n}개 객체에 걸림");
        }

        void Explode()
        {
            CAD_SelectObject(_poly, false);
            Log("분해 → " + (CAD_Explode(false) ? $"성공, 지금 선택 {CAD_GetSelectedCount()}개" : "실패"));
            CAD_ClearSelection();
        }

        // 정점 목록을 통째로 읽고 → 고쳐서 → 되돌린다 (PEDIT 의 모든 하위 명령이 이 모양)
        void EditVertex()
        {
            if (!CAD_ObjectExists(_poly)) { Log("오각형이 없다(분해했으면 다시 열 것)"); return; }
            uint n = CAD_GetPolylinePoints(_poly, null, 0, out _);
            if (n == 0) { Log("폴리선이 아니다"); return; }
            var xyz = new float[n * 3];
            CAD_GetPolylinePoints(_poly, xyz, n, out bool closed);
            xyz[1] += 1.2f;   // 첫 꼭짓점을 +Y 로 (좌표는 객체 로컬)
            Log($"정점 {n}개 중 첫 점 이동 → " + (CAD_SetPolylinePoints(_poly, xyz, n, closed) ? "성공" : "실패"));
        }

        public override void Demo()
        {
            Offset(); Mirror(); Trim(); Extend(); Break(); Lengthen(); Stretch(); EditVertex();
        }
    }
}
