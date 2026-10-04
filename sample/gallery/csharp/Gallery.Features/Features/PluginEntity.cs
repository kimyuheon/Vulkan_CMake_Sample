using System;
using System.Collections.Generic;
using System.Text.Json;
using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery
{
    public sealed class PluginEntity : Feature
    {
        public override string Category => "확장";
        public override string Group => "엔티티";
        public override string Icon => "⚙";
        public override string Title => "사용자 객체 (딱지 + 메시)";
        public override string Summary =>
            "새 객체 종류를 만들지 않는다 — 형상(메시)을 공급하고 딱지(owner·type·JSON)를 붙인다. 값을 바꾸면 ReplaceMesh 로 다시 만든다. " +
            "평범한 메시라 그리기·선택·저장이 그대로 되고, .lot 을 다시 열면 딱지가 살아나 콜백으로 알려 준다.";
        public override string[] Apis => new[]
        {
            "CAD_CreateMesh", "CAD_ReplaceMesh", "CAD_SetPluginTag", "CAD_GetPluginTagData", "CAD_HasPluginTag",
            "CAD_AddEntityProperty", "CAD_SetOnPluginEntityChanged", "CAD_SetOnPluginEntityLoaded", "CAD_SetOnConfirm",
        };

        const string Owner = "GallerySample", Type = "gear";
        uint _gear;
        int _teeth = 12;
        float _radius = 2, _thickness = 0.5f;
        // 콜백 델리게이트는 필드로
        CAD_SetOnPluginEntityChanged_cb _onChanged;
        CAD_SetOnPluginEntityLoaded_cb _onLoaded;
        CAD_SetOnConfirm_cb _onConfirm;
        IValue<float> _teethSlider, _thickSlider;

        protected override void Setup()
        {
            // 특성창 속성 선언 — 단독 실행(ImGui)에선 엔진이 슬라이더를 그린다. 값은 딱지 JSON 의 키.
            CAD_AddEntityProperty(Owner, Type, "teeth", "잇수", 1, 6, 40, 0);
            CAD_AddEntityProperty(Owner, Type, "radius", "반지름", 0, 0.5f, 5, 0);
            _onChanged = (id, o, t, data) => { if (Cad.FromUtf8(o) == Owner) { Load(Cad.FromUtf8(data)); Rebuild(id); } };
            _onLoaded = (id, o, t, data) => Log($"딱지 복원: id {id} {Cad.FromUtf8(o)}/{Cad.FromUtf8(t)} {Cad.FromUtf8(data)}");
            _onConfirm = text => { Log("엔진 질문: " + Cad.FromUtf8(text) + " → 예"); return true; };   // 임베드는 반드시 달 것
            CAD_SetOnPluginEntityChanged(_onChanged);
            CAD_SetOnPluginEntityLoaded(_onLoaded);
            CAD_SetOnConfirm(_onConfirm);

            Build(out var xyz, out var idx);
            _gear = Tint(CAD_CreateMesh(xyz, (uint)(xyz.Length / 3), idx, (uint)idx.Length, null), 0.75f, 0.75f, 0.8f);
            Tag();
            CAD_ClearSelection();
            Scene.Frame();

            Ui.Section("기어 값 (딱지 JSON → 형상 재생성)");
            _teethSlider = Ui.Slider("잇수", 6, 40, _teeth, v => { _teeth = (int)v; Tag(); Rebuild(_gear); }, 0);
            Ui.Slider("반지름", 0.5f, 5, _radius, v => { _radius = v; Tag(); Rebuild(_gear); }, 2);
            _thickSlider = Ui.Slider("두께", 0.1f, 2, _thickness, v => { _thickness = v; Tag(); Rebuild(_gear); }, 2);
            Ui.Button("딱지 읽기", () => Log($"HasPluginTag {CAD_HasPluginTag(_gear)}: {Str((b, n) => CAD_GetPluginTagData(_gear, b, n))}"));
        }

        void Tag() => CAD_SetPluginTag(_gear, Owner, Type,
            JsonSerializer.Serialize(new Dictionary<string, object> { ["teeth"] = _teeth, ["radius"] = _radius, ["thickness"] = _thickness }));

        void Load(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var r = doc.RootElement;
            if (r.TryGetProperty("teeth", out var t)) _teeth = (int)t.GetDouble();
            if (r.TryGetProperty("radius", out var rr)) _radius = rr.GetSingle();
            if (r.TryGetProperty("thickness", out var th)) _thickness = th.GetSingle();
        }

        void Rebuild(uint id)
        {
            Build(out var xyz, out var idx);
            CAD_ReplaceMesh(id, xyz, (uint)(xyz.Length / 3), idx, (uint)idx.Length, null);
        }

        // 톱니 윤곽(2×잇수 꼭짓점 별 모양)을 두께만큼 세운 각기둥 — 위·아래 뚜껑은 중심점 부채꼴
        void Build(out float[] xyz, out uint[] idx)
        {
            int m = _teeth * 4;                       // 윤곽 꼭짓점 수(이 하나에 4개)
            var outline = new (float x, float y)[m];
            for (int i = 0; i < m; i++)
            {
                float a = i * MathF.PI * 2 / m;
                float r = (i % 4 == 1 || i % 4 == 2) ? _radius : _radius * 0.82f;
                outline[i] = (r * MathF.Cos(a), r * MathF.Sin(a));
            }
            var v = new List<float>();
            var t = new List<uint>();
            v.AddRange(new[] { 0f, 0, 0 });            // 0: 아래 중심
            v.AddRange(new[] { 0f, 0, _thickness });   // 1: 위 중심
            foreach (var (x, y) in outline) { v.AddRange(new[] { x, y, 0 }); v.AddRange(new[] { x, y, _thickness }); }
            for (int i = 0; i < m; i++)
            {
                uint b0 = (uint)(2 + i * 2), t0 = b0 + 1, b1 = (uint)(2 + (i + 1) % m * 2), t1 = b1 + 1;
                t.AddRange(new[] { 0u, b1, b0 });      // 아래 뚜껑 (아래를 향하게)
                t.AddRange(new[] { 1u, t0, t1 });      // 위 뚜껑
                t.AddRange(new[] { b0, b1, t1, b0, t1, t0 });   // 옆면
            }
            xyz = v.ToArray();
            idx = t.ToArray();
        }

        public override void Leave()
        {
            CAD_SetOnPluginEntityChanged(null);
            CAD_SetOnPluginEntityLoaded(null);
            CAD_SetOnConfirm(null);
            base.Leave();
        }

        public override void Demo()
        {
            _teeth = 20; _thickness = 0.8f; Tag(); Rebuild(_gear);
            _teethSlider.Value = _teeth; _thickSlider.Value = _thickness;
        }
    }
}
