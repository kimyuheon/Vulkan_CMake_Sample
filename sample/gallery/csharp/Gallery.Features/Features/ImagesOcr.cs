using System.IO;
using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery
{
    public sealed class ImagesOcr : Feature
    {
        public override string Category => "파일";
        public override string Group => "이미지";
        public override string Icon => "▨";
        public override string Title => "이미지 붙이기·OCR";
        public override string Summary =>
            "도면 밑에 깔 이미지를 XY 평면에 붙인다 — 파일 경로로, 또는 RGBA 픽셀을 메모리로. " +
            "OCR 은 호스트(Windows OCR·Vision 등)가 인식한 줄과 사각형만 넘기면 그 뒤 화면 흐름은 엔진이 한다.";
        public override string[] Apis => new[]
        {
            "CAD_AttachImageFromFile", "CAD_AttachImageFromMemory", "CAD_BeginOcrLines", "CAD_AddOcrLine", "CAD_EndOcrLines",
        };

        protected override void Setup()
        {
            Scene.Frame(1);
            Ui.Section("붙이기");
            Ui.Button("파일에서 (textures/ 의 벽돌 이미지)", () =>
            {
                string p = Path.Combine(Ui.AssetDir, "textures", "Bricks104_4K-PNG_Color.png");
                Log("이미지 id " + CAD_AttachImageFromFile(p, 6, 0, 0, 0));
                CAD_RequestZoomExtents();
            });
            Ui.Button("파일 고르기…", () =>
            {
                string p = Ui.PickOpenFile("이미지|*.png;*.jpg;*.jpeg;*.bmp");
                if (p != null) { Log("이미지 id " + CAD_AttachImageFromFile(p, 0, 0, 0, 0)); CAD_RequestZoomExtents(); }
            });
            Ui.Button("메모리에서 (코드로 만든 평면도 그림) + OCR 줄", MemoryWithOcr);
            Ui.Note("OCR 사각형은 **이미지 픽셀 좌표**(좌상단 원점). 넣은 순서 = 읽는 순서.");
        }

        // 256×160 짜리 간단한 "평면도" 그림을 픽셀로 그려 넘긴다 — 사진 앱이 경로 없이 데이터를 줄 때의 경로
        void MemoryWithOcr()
        {
            const int w = 256, h = 160;
            var rgba = new byte[w * h * 4];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    bool wall = x < 6 || x >= w - 6 || y < 6 || y >= h - 6 || (x >= 125 && x < 131 && (y < 60 || y > 100));
                    byte v = wall ? (byte)40 : (byte)235;
                    int i = (y * w + x) * 4;
                    rgba[i] = v; rgba[i + 1] = v; rgba[i + 2] = v; rgba[i + 3] = 255;
                }
            uint img = CAD_AttachImageFromMemory(rgba, w, h, "코드로 만든 평면도", 8, 0, 0, 0);
            Log("이미지 id " + img);

            CAD_BeginOcrLines(img);
            CAD_AddOcrLine("거실 3600", 30, 60, 110, 84);
            CAD_AddOcrLine("침실 2700", 150, 60, 230, 84);
            CAD_EndOcrLines();   // 여기서 화면에 인식 영역 네모가 뜬다
            Log("OCR 줄 2개 주입");
            CAD_RequestZoomExtents();
        }

        public override void Demo() => MemoryWithOcr();
    }
}
