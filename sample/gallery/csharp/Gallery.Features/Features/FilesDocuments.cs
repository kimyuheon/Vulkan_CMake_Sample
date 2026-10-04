using System;
using System.IO;
using System.Linq;
using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery
{
    public sealed class FilesDocuments : Feature
    {
        public override string Category => "파일";
        public override string Group => "파일";
        public override string Icon => "🗀";
        public override string Title => "열기·저장·내보내기·탭";
        public override string Summary =>
            "포맷은 확장자로 정해진다 — 호스트는 경로만 넘긴다. OpenFile 은 현재 문서에 **추가**, OpenDocument 는 새 탭. " +
            ".lot 는 씬 전체를 담는 네이티브 형식이고 나머지(.stl .obj .glb .dxf)는 내보내기다. 실패 사유는 상태 메시지로.";
        public override string[] Apis => new[]
        {
            "CAD_OpenFile", "CAD_SaveAs", "CAD_ExportFile", "CAD_NewDocument", "CAD_OpenDocument", "CAD_ActivateDocument",
            "CAD_CloseDocument", "CAD_GetDocumentTitle", "CAD_IsDocumentDirty", "CAD_CaptureViewport",
        };

        static readonly string[] Supported = { ".lot", ".obj", ".stl", ".dxf", ".ply", ".gltf", ".glb", ".fbx" };
        string[] _samples;

        protected override void Setup()
        {
            string models = Path.Combine(Ui.AssetDir, "models");
            _samples = Directory.Exists(models)
                ? Directory.GetFiles(models).Where(f => Supported.Contains(Path.GetExtension(f).ToLower())).Select(Path.GetFileName).ToArray()
                : Array.Empty<string>();

            Ui.Section("예제 모델 (sdk/models)");
            Ui.Choice("모델", _samples, -1, i => OpenSample(_samples[i]));
            Ui.Section("파일");
            Ui.Button("열기 (현재 문서에 추가)…", () =>
            {
                string p = Ui.PickOpenFile("CAD 파일|" + string.Join(";", Supported.Select(e => "*" + e)) + "|모든 파일|*.*");
                if (p != null) Report(CAD_OpenFile(p), "열기 " + p);
            });
            Ui.Button("새 탭으로 열기…", () =>
            {
                string p = Ui.PickOpenFile("CAD 파일|" + string.Join(";", Supported.Select(e => "*" + e)));
                if (p != null) Log($"OpenDocument → 탭 {CAD_OpenDocument(p)}");
                Tabs();
            });
            Ui.Button("저장 (.lot)…", () => Save("프로젝트|*.lot", "장면.lot", false));
            Ui.Button("내보내기 (.stl .obj .glb .dxf)…", () => Save("STL|*.stl|OBJ|*.obj|glTF|*.glb|DXF|*.dxf", "장면.stl", true));
            Ui.Button("화면 캡처 PNG…", () =>
            {
                string p = Ui.PickSaveFile("PNG|*.png", "화면.png");
                if (p != null) Report(CAD_CaptureViewport(p, true), "캡처 " + p);
            });

            Ui.Section("문서 탭");
            Ui.Button("새 탭", () => { CAD_NewDocument("새 문서"); Tabs(); });
            Ui.Button("다음 탭으로", () =>
            {
                int n = CAD_GetDocumentCount();
                CAD_ActivateDocument((CAD_GetActiveDocument() + 1) % n);
                Tabs();
            });
            Ui.Button("현재 탭 닫기", () => { CAD_CloseDocument(CAD_GetActiveDocument()); Tabs(); });
            Ui.Button("탭 목록", Tabs);
            Ui.Note("호스트가 자기 탭 UI 를 그릴 때 쓰는 함수들이다. 전환은 상태 교체라 비용이 거의 없다.");
        }

        void OpenSample(string file)
        {
            // 상대 경로 — 엔진은 런타임 에셋 폴더를 기준으로 연다
            Report(CAD_OpenFile("models/" + file), "열기 models/" + file);
            CAD_RequestZoomExtents();
        }

        void Save(string filter, string name, bool export)
        {
            string p = Ui.PickSaveFile(filter, name);
            if (p == null) return;
            Report(export ? CAD_ExportFile(p, false) : CAD_SaveAs(p, false), (export ? "내보내기 " : "저장 ") + p);
        }

        void Report(bool ok, string what) => Log(ok ? what + " — 성공" : what + " — 실패: " + Cad.StatusMessage());

        void Tabs()
        {
            int n = CAD_GetDocumentCount(), active = CAD_GetActiveDocument();
            for (int i = 0; i < n; i++)
            {
                int k = i;
                string title = Str((b, c) => CAD_GetDocumentTitle(k, b, c));
                Log($"  {(i == active ? "▶" : " ")} [{i}] {title}{(CAD_IsDocumentDirty(i) ? " *" : "")}");
            }
        }

        public override void Demo() => OpenSample("CesiumMilkTruck.glb");
    }
}
