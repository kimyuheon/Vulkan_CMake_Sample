using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery
{
    public sealed class Floorplan : Feature
    {
        public override string Category => "건축";
        public override string Group => "벽체";
        public override string Icon => "⌂";
        public override string Title => "방·평면도·문·창";
        public override string Summary =>
            "공간(사각형) 목록만 주면 겹치는 벽을 하나로 합쳐 벽체를 세운다. 문·창은 공간 번호와 벽(또는 두 공간의 공유벽)으로 지정한다. " +
            "평면도 이미지에서 벽을 찾아 세울 수도 있다(AI 없이). 단위는 m.";
        public override string[] Apis => new[]
        {
            "CAD_CreateRoom", "CAD_CreateFloorplan", "CAD_CreateFloorplanWithOpenings", "CAD_CreateWallGraph",
            "CAD_CreateFloorplanFromImage",
        };

        protected override void Setup()
        {
            Scene.Frame();
            Ui.Section("만들기");
            Ui.Button("방 하나 (4×3 m)", Room);
            Ui.Button("평면도 — 공간 3개 + 문·창", WithOpenings);
            Ui.Button("벽 그래프 — 선분 목록으로", WallGraph);
            Ui.Button("평면도 이미지에서 벽 세우기…", () =>
            {
                string p = Ui.PickOpenFile("이미지|*.png;*.jpg;*.jpeg");
                if (p == null) return;
                var ids = new uint[256];
                // widthMm 0 = 문 폭(900mm)으로 축척 추정, underlayImage = 원본을 바닥에 깔기
                Log($"이미지 → 벽 {CAD_CreateFloorplanFromImage(p, 0, 2.4f, true, ids, (uint)ids.Length)}개");
                Scene.Frame();
            });
        }

        void Room()
        {
            var walls = new uint[4];   // 남·북·서·동
            Log("CreateRoom → " + CAD_CreateRoom(-8, 0, 0, 4, 3, 2.4f, 0.15f, walls) + $"  벽 id {string.Join(",", walls)}");
            Scene.Frame();
        }

        // 거실(0) | 침실(1) 나란히, 그 위에 주방(2). 문 = 거실-침실 공유벽, 창 = 거실 남쪽 벽
        void WithOpenings()
        {
            float[] rects =
            {
                0, 0, 5, 4,     // 0 거실
                5, 0, 8, 4,     // 1 침실
                0, 4, 8, 6.5f,  // 2 주방
            };
            var openings = new[]
            {
                new CAD_FloorplanOpeningDesc { type = 0, roomAIndex = 0, roomBIndex = 1, width = 0.9f, height = 2.1f },
                new CAD_FloorplanOpeningDesc { type = 0, roomAIndex = 0, roomBIndex = 2, width = 0.9f, height = 2.1f, alignment = 1, offset = 1 },
                new CAD_FloorplanOpeningDesc { type = 1, roomAIndex = 0, roomBIndex = -1, side = 0, width = 2.0f, height = 1.2f, sillHeight = 0.9f },
                new CAD_FloorplanOpeningDesc { type = 1, roomAIndex = 1, roomBIndex = -1, side = 3, width = 1.2f, height = 1.2f, sillHeight = 0.9f },
            };
            var ids = new uint[64];
            uint n = CAD_CreateFloorplanWithOpenings(0, 0, 0, rects, 3, 2.4f, 0.15f, openings, (uint)openings.Length, ids, (uint)ids.Length);
            Log($"평면도 → 벽 조각 {n}개 (문·창 구멍을 뺀 뒤)");
            Scene.Frame();
        }

        void WallGraph()
        {
            // [x1,y1,x2,y2,두께] × n — 두께 0 이하면 기본 두께
            float[] segs =
            {
                0, 0, 6, 0, 0.2f,
                6, 0, 6, 4, 0.2f,
                6, 4, 0, 4, 0.2f,
                0, 4, 0, 0, 0.2f,
                3, 0, 3, 2.5f, 0.1f,
            };
            var ids = new uint[32];
            Log($"벽 그래프 → 벽 {CAD_CreateWallGraph(0, -9, 0, segs, 5, 2.4f, 0.15f, ids, (uint)ids.Length)}개");
            Scene.Frame();
        }

        public override void Demo() { WithOpenings(); Room(); WallGraph(); }
    }
}
