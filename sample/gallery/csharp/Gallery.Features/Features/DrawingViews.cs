using System.Text.Json;
using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery
{
    public sealed class DrawingViews : Feature
    {
        public override string Category => "보기";
        public override string Group => "도면";
        public override string Icon => "⊟";
        public override string Title => "도면 뷰·출력";
        public override string Summary =>
            "3D 솔리드에서 정면·평면·우측면(3각법)·등각 뷰를 만들고, 단면도(A-A)·상세도를 더한다. 뷰는 3D 를 고치면 따라온다. " +
            "PDF(벡터)·PNG 로 출력한다. 뷰 위치는 뷰 목록 JSON 의 bounds 로 안다.";
        public override string[] Apis => new[]
        {
            "CAD_CreateDrawingViews", "CAD_CreateSectionView", "CAD_CreateDetailView", "CAD_GetDrawingViewsJson",
            "CAD_MoveDrawingView", "CAD_Plot",
        };

        uint _part;

        protected override void Setup()
        {
            // 구멍과 보스가 있는 부품
            uint rect = CAD_CreateRectangle(0, 0, 0, 60, 40, 0);
            _part = CAD_CreateExtrude(rect, 15);
            CAD_CutExtrudeFromSketch(_part, CAD_CreateCircle(18, 20, 15, 8, 0, 0, 1), 0, true);
            CAD_BossExtrudeFromSketch(_part, CAD_CreateRectangle(35, 10, 15, 52, 30, 15), 12);
            Tint(_part, 0.6f, 0.7f, 0.85f);
            CAD_ClearSelection();
            Scene.Frame();

            Ui.Section("뷰");
            Ui.Button("3각법 뷰 + 등각 만들기", Create);
            Ui.Button("정면도를 가로지르는 단면도 A-A", SectionView);
            Ui.Button("정면도 구멍 부분 상세도 (2배)", DetailView);
            Ui.Button("뷰 지우기", CAD_ClearDrawingViews);
            Ui.Button("뷰 목록 JSON", () => Log(Str(CAD_GetDrawingViewsJson, 4096)));
            Ui.Section("출력");
            Ui.Button("PDF 로 출력 (A3 가로, 흑백)…", () => Plot("PDF|*.pdf", "도면.pdf"));
            Ui.Button("PNG 로 출력…", () => Plot("PNG|*.png", "도면.png"));
            Ui.Note("출력은 지금 뷰 방향 그대로 흰 종이에 그린다 — 평면(Top) 뷰에서 줌 맞춤 후 출력하면 도면이 된다.");
        }

        void Create()
        {
            Log($"CreateDrawingViews → 뷰 {CAD_CreateDrawingViews(null, 0, 1)}개");
            CAD_RequestSetView(1);
            CAD_RequestZoomExtents();
        }

        // 뷰 목록에서 정면도(base, dir 이 -Y 쪽)의 경계를 찾는다
        bool FrontBounds(out float minX, out float minY, out float maxX, out float maxY)
        {
            minX = minY = maxX = maxY = 0;
            string json = Str(CAD_GetDrawingViewsJson, 4096);
            if (string.IsNullOrEmpty(json)) return false;
            using var doc = JsonDocument.Parse(json);
            foreach (var v in doc.RootElement.EnumerateArray())
            {
                if (v.GetProperty("kind").GetString() != "base") continue;
                string name = v.TryGetProperty("name", out var n) ? n.GetString() : "";
                if (!name.Contains("정면") && !name.ToLower().Contains("front")) continue;
                var b = v.GetProperty("bounds");
                minX = b.GetProperty("min")[0].GetSingle(); minY = b.GetProperty("min")[1].GetSingle();
                maxX = b.GetProperty("max")[0].GetSingle(); maxY = b.GetProperty("max")[1].GetSingle();
                return true;
            }
            return false;
        }

        void SectionView()
        {
            if (!FrontBounds(out float x0, out float y0, out float x1, out float y1)) { Log("정면도가 없다 — 먼저 뷰를 만들 것"); return; }
            float cx = (x0 + x1) / 2 - (x1 - x0) * 0.2f;
            Log($"단면도 → 뷰 id {CAD_CreateSectionView(cx, y1 + 5, cx, y0 - 5)}");
        }

        void DetailView()
        {
            if (!FrontBounds(out float x0, out float y0, out float x1, out float y1)) { Log("정면도가 없다 — 먼저 뷰를 만들 것"); return; }
            Log($"상세도 → 뷰 id {CAD_CreateDetailView(x0 + (x1 - x0) * 0.3f, (y0 + y1) / 2, (x1 - x0) * 0.15f, 2)}");
        }

        void Plot(string filter, string name)
        {
            string path = Ui.PickSaveFile(filter, name);
            if (path == null) return;
            Log(CAD_Plot(path, false, true, 0, 0, 0) ? "출력 → " + path : "출력 실패 — " + Cad.StatusMessage());
        }

        public override void Demo()
        {
            Create();
            SectionView();
            DetailView();
            Later(3, CAD_RequestZoomExtents);
        }
    }
}
