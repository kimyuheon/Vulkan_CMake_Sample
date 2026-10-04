using System.Linq;
using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery
{
    public sealed class BooleanOps : Feature
    {
        public override string Category => "편집";
        public override string Group => "솔리드";
        public override string Icon => "⊖";
        public override string Title => "불리언·간섭";
        public override string Summary =>
            "바로 실행형(…Ids · Subtract)은 넘긴 id 로 그 자리에서 계산하고 결과 id 를 돌려준다 — 선택과 무관, 한 프레임에 여러 번 불러도 된다. " +
            "입력에 문제가 있으면 아무것도 바꾸지 않고 0 + 이유(GetLastError). 차집합은 기준마다 따로(perBase)도 된다.";
        public override string[] Apis => new[]
        {
            "CAD_BooleanUnionIds", "CAD_BooleanIntersectionIds", "CAD_BooleanSubtract", "CAD_CheckInterference",
            "CAD_BooleanUnion (요청형)",
        };

        uint _uA, _uB, _iA, _iB, _dA, _dB, _kA, _kB;
        uint[] _plates, _cutter;
        bool _keep;

        protected override void Setup()
        {
            (_uA, _uB) = Pair(0, "합집합");
            (_iA, _iB) = Pair(4, "교집합");
            (_dA, _dB) = Pair(8, "차집합");
            _kA = Tint(Box(12, 0, 2, 2, 1.5f), 0.6f, 0.6f, 0.6f);
            _kB = Tint(Box(13.2f, 0.8f, 2, 2, 1.5f, 0.5f), 0.6f, 0.6f, 0.6f);
            Caption(12, -1.8f, "간섭 검사");
            // 판 3장 + 그 셋을 꿰뚫는 원기둥 하나 — 기준마다 따로 빼기
            _plates = new[] { Tint(Box(0, -5, 2, 2, 0.3f), 0.75f, 0.55f, 0.85f), Tint(Box(0, -5, 2, 2, 0.3f, 0.8f), 0.75f, 0.55f, 0.85f),
                              Tint(Box(0, -5, 2, 2, 0.3f, 1.6f), 0.75f, 0.55f, 0.85f) };
            _cutter = new[] { Tint(CAD_CreateCylinder(0, -5, -0.2f, 0.5f, 2.4f), 0.45f, 0.70f, 0.90f) };
            Caption(-1, -6.8f, "판 3장 − 원기둥 (기준마다)");
            CAD_ClearSelection();
            Scene.Frame();

            Ui.Section("바로 실행형 (결과 id 를 돌려받는다)");
            Ui.Button("합집합 — UnionIds", Union);
            Ui.Button("교집합 — IntersectionIds", Intersect);
            Ui.Button("차집합 — 상자 − 구", Subtract);
            Ui.Button("판 3장 − 원기둥, 기준마다 따로 (perBase)", SubtractPerBase);
            Ui.Toggle("원본 남기기 (keepOriginals)", false, v => _keep = v);
            Ui.Button("일부러 실패 — 없는 id 를 넘김", () =>
                Log($"UnionIds([{_uA}, 99999]) → {CAD_BooleanUnionIds(new[] { _uA, 99999u }, 2, false)}, 이유: {Cad.LastError()}"));

            Ui.Section("간섭");
            Ui.Button("간섭 검사 + 겹친 덩어리 만들기", Interference);

            Ui.Section("요청형 (예전 방식 — 선택 기준, 다음 프레임에 실행)");
            Ui.Button("합집합 요청 — 선택한 것들", () => { CAD_BooleanUnion(_keep); Log("요청함 — 결과 id 는 알 수 없다"); });
            Ui.Note("요청형은 반환값이 없고 '그때의 선택'으로 돈다. 결과가 필요하면 위 바로 실행형을 쓴다.");
        }

        (uint, uint) Pair(float x, string label)
        {
            uint a = Tint(Box(x, 0, 2, 2, 2), 0.85f, 0.55f, 0.35f);
            uint b = Tint(CAD_CreateSphere(x + 1, 0.6f, 0.4f, 1.1f), 0.45f, 0.70f, 0.90f);
            Caption(x, -1.8f, label);
            return (a, b);
        }

        void Report(string what, uint result) =>
            Log(result != 0 ? $"{what} → 결과 id {result}" : $"{what} → 실패: {Cad.LastError()}");

        void Union() => Report("합집합", CAD_BooleanUnionIds(new[] { _uA, _uB }, 2, _keep));
        void Intersect() => Report("교집합", CAD_BooleanIntersectionIds(new[] { _iA, _iB }, 2, _keep));

        void Subtract()
        {
            var outIds = new uint[4];
            uint n = CAD_BooleanSubtract(new[] { _dA }, 1, new[] { _dB }, 1, _keep, false, outIds, (uint)outIds.Length);
            Report("차집합", n > 0 ? outIds[0] : 0);
        }

        void SubtractPerBase()
        {
            var outIds = new uint[8];
            uint n = CAD_BooleanSubtract(_plates, (uint)_plates.Length, _cutter, 1, _keep, true, outIds, (uint)outIds.Length);
            Log(n > 0 ? $"기준마다 차집합 → 결과 {n}개: {string.Join(", ", outIds.Take((int)n))}" : "실패: " + Cad.LastError());
        }

        void Interference()
        {
            var ids = new[] { _kA, _kB };
            var info = new CAD_InterferenceInfo[8];
            uint n = CAD_CheckInterference(ids, 2, true, info, (uint)info.Length);
            Log($"간섭 쌍 {n}개");
            for (int i = 0; i < n && i < info.Length; i++)
                Log($"  {info[i].objectA} ↔ {info[i].objectB}  부피 {info[i].volume:0.###}  덩어리 id {info[i].resultId}");
        }

        // 바로 실행형이라 한 프레임에 이어 불러도 된다
        public override void Demo()
        {
            Union(); Intersect(); Subtract(); SubtractPerBase(); Interference();
            CAD_ClearSelection();
        }
    }
}
