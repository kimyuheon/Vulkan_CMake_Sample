using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery
{
    public sealed class Shapes2D : Feature
    {
        public override string Category => "만들기";
        public override string Group => "2D";
        public override string Icon => "✎";
        public override string Title => "2D 도형";
        public override string Summary =>
            "선·원·호·사각형·정다각형·폴리선·타원·스플라인을 좌표로 만든다. " +
            "평면 도형은 법선(normal)으로 놓일 면을 정한다 — (0,0,1) = XY 평면.";
        public override string[] Apis => new[]
        {
            "CAD_CreateLine", "CAD_CreateCircle", "CAD_CreateArc", "CAD_CreateRectangle", "CAD_CreatePolygon",
            "CAD_CreatePolyline", "CAD_CreateEllipse", "CAD_CreateSpline", "CAD_GetLength", "CAD_GetArea",
        };

        protected override void Setup()
        {
            Report("선", CAD_CreateLine(0, 0, 0, 3, 2, 0));
            Report("원", CAD_CreateCircle(6, 1, 0, 1.2f, 0, 0, 1));
            Report("호 (시작·경유·끝)", CAD_CreateArc(9, 0, 0, 10.5f, 1.5f, 0, 12, 0, 0));
            Report("사각형 (대각 두 점)", CAD_CreateRectangle(0, 4, 0, 3, 6, 0));
            Report("육각형", CAD_CreatePolygon(6, 5, 0, 1.2f, 6, 0, 0, 1, 1, 0, 0));
            Report("폴리선 (닫힘)", CAD_CreatePolyline(new float[] { 9, 4, 0, 12, 4, 0, 12, 6, 0, 10.5f, 7, 0, 9, 6, 0 }, 5, true));
            Report("타원", CAD_CreateEllipse(1.5f, 10, 0, 1.8f, 0, 0, 0.5f, 0, 0, 1));
            Report("스플라인", CAD_CreateSpline(new float[] { 4.5f, 9, 0, 6, 11, 0, 7.5f, 9, 0, 9, 11, 0, 10.5f, 9, 0, 12, 11, 0 }, 6, false));
            CAD_ClearSelection();
            Scene.Frame(1);   // 평면도(Top)로

            Ui.Section("보기");
            Ui.Button("평면 (Top)", () => Scene.Frame(1));
            Ui.Button("등각 (Iso)", () => Scene.Frame(3));
            Ui.Section("측정");
            Ui.Button("선택한 것의 길이·면적 합", () =>
                Log($"선택 {CAD_GetSelectedCount()}개 — 길이 {CAD_GetSelectionLength():0.###}, 면적 {CAD_GetSelectionArea():0.###}"));
            Ui.Note("화면에서 도형을 클릭(Shift = 추가 선택)한 뒤 측정 버튼을 누른다.");
        }

        void Report(string name, uint id)
        {
            if (id == 0) { Log($"{name}: 실패 — {Cad.LastError()}"); return; }
            string len = CAD_GetLength(id, out float l) ? $"길이 {l:0.###}" : "";
            string area = CAD_GetArea(id, out float a) ? $" 면적 {a:0.###}" : "";
            Log($"{name} → id {id}  {len}{area}");
        }
    }
}
