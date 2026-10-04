using System.Collections.Generic;
using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery
{
    public sealed class Materials : Feature
    {
        public override string Category => "객체·속성";
        public override string Group => "표현";
        public override string Icon => "◐";
        public override string Title => "재질·조명";
        public override string Summary =>
            "textures/ 폴더의 이미지가 곧 재질 목록이다(이름 = 파일 이름, 확장자 없이). 이름에 metal·steel 이 들면 금속으로 칠한다. " +
            "null/빈 이름이면 재질을 뺀다.";
        public override string[] Apis => new[]
        {
            "CAD_GetMaterialCount", "CAD_GetMaterialName", "CAD_ApplyMaterial", "CAD_SetDemoLighting", "CAD_SetObjectOpacity",
        };

        readonly List<uint> _objs = new List<uint>();
        string[] _names;

        protected override void Setup()
        {
            uint n = CAD_GetMaterialCount();
            _names = new string[n];
            for (uint i = 0; i < n; i++) _names[i] = Str((b, c) => CAD_GetMaterialName(i, b, c));
            Log($"재질 {n}개: {string.Join(", ", _names)}");

            // 재질마다 상자 하나 + 구 하나
            for (int i = 0; i < _names.Length; i++)
            {
                uint box = Box(i * 3, 0, 2, 2, 2);
                uint ball = CAD_CreateSphere(i * 3, 3, 0, 1);
                CAD_ApplyMaterial(box, _names[i]);
                CAD_ApplyMaterial(ball, _names[i]);
                _objs.Add(box); _objs.Add(ball);
                Caption(i * 3 - 1, -1.8f, _names[i], 0.25f);
            }
            CAD_ClearSelection();
            Scene.Frame();

            Ui.Section("전체에 적용");
            Ui.Choice("재질", _names, 0, i => { foreach (var id in _objs) CAD_ApplyMaterial(id, _names[i]); });
            Ui.Button("재질 빼기", () => { foreach (var id in _objs) CAD_ApplyMaterial(id, null); });
            Ui.Section("조명·투명");
            Ui.Toggle("데모 조명", true, CAD_SetDemoLighting);
            Ui.Slider("구 투명도", 0.1f, 1, 1, v => { for (int i = 1; i < _objs.Count; i += 2) CAD_SetObjectOpacity(_objs[i], v); }, 2);
        }
    }
}
