using System.Collections.Generic;
using System.IO;
using System.Linq;
using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery
{
    public sealed class Particles : Feature
    {
        public override string Category => "로봇·시뮬";
        public override string Group => "효과";
        public override string Icon => "✺";
        public override string Title => "파티클·이펙트";
        public override string Summary =>
            "불·연기·불꽃 프리셋으로 방출기를 만들고 재생한다(정지 상태로 생성됨). 설정은 부분 JSON 으로 바꾼다 — 준 키만 바뀐다. " +
            "effects/ 폴더의 *.json 프리셋 라이브러리와 Effekseer(.efkefc) 이펙트도 같은 재생 API 로 다룬다.";
        public override string[] Apis => new[]
        {
            "CAD_CreateParticleEmitter", "CAD_SetParticlePlayback", "CAD_GetParticleSettingsJson", "CAD_SetParticleSettingsJson",
            "CAD_GetParticlePresetCount", "CAD_CreateParticleEmitterFromPreset", "CAD_CreateExternalEffect", "CAD_GetParticleLiveCount",
        };

        readonly List<uint> _emitters = new List<uint>();
        uint _fire;
        double _logTimer;

        protected override void Setup()
        {
            // unitsPerMeter: 미터 장면은 1, mm 도면은 1000
            _fire = Add(CAD_CreateParticleEmitter(0, 0, 0, 0, 1), "불");
            Add(CAD_CreateParticleEmitter(1, 3, 0, 0, 1), "연기");
            Add(CAD_CreateParticleEmitter(2, 6, 0, 0, 1), "불꽃");
            Tint(Box(3, 0, 9, 1.5f, 0.1f, -0.1f), 0.3f, 0.3f, 0.32f);
            foreach (var id in _emitters) CAD_SetParticlePlayback(id, 1);
            CAD_ClearSelection();
            Scene.Frame(0);

            Ui.Section("재생");
            Ui.Button("전부 재생", () => _emitters.ForEach(id => CAD_SetParticlePlayback(id, 1)));
            Ui.Button("전부 일시정지", () => _emitters.ForEach(id => CAD_SetParticlePlayback(id, 0)));
            Ui.Button("전부 리셋", () => _emitters.ForEach(id => CAD_SetParticlePlayback(id, 2)));

            Ui.Section("불 설정 (부분 JSON)");
            Ui.Slider("rate (초당 입자)", 10, 600, 120, v => Set($"{{\"rate\":{v:0}}}"), 0);
            Ui.Slider("speed", 0.1f, 5, 1, v => Set($"{{\"speed\":{v:0.##}}}"), 2);
            Ui.Slider("size", 0.02f, 1, 0.2f, v => Set($"{{\"size\":{v:0.###}}}"), 2);
            Ui.Button("지금 설정 JSON", () => Log(Str((b, n) => CAD_GetParticleSettingsJson(_fire, b, n), 2048)));

            Ui.Section("라이브러리");
            uint pc = CAD_GetParticlePresetCount();
            var names = Enumerable.Range(0, (int)pc).Select(i => Str((b, n) => CAD_GetParticlePresetName((uint)i, b, n))).ToArray();
            Ui.Choice($"effects/*.json 프리셋 ({pc}개)", names, -1, i =>
            {
                string path = Str((b, n) => CAD_GetParticlePresetPath((uint)i, b, n));
                uint id = Add(CAD_CreateParticleEmitterFromPreset(path, 0, 3, 0, 1), names[i]);
                CAD_SetParticlePlayback(id, 1);
            });
            string ext = Path.Combine(Ui.AssetDir, "effects", "external");
            var efk = Directory.Exists(ext) ? Directory.GetFiles(ext, "*.efkefc") : new string[0];
            Ui.Choice($"Effekseer 이펙트 ({efk.Length}개)", efk.Select(Path.GetFileNameWithoutExtension).ToArray(), -1, i =>
            {
                uint id = Add(CAD_CreateExternalEffect(efk[i], 6, 3, 0, 1), Path.GetFileName(efk[i]));
                CAD_SetParticlePlayback(id, 1);
            });
        }

        uint Add(uint id, string name)
        {
            Check(name, id);
            if (id != 0) _emitters.Add(id);
            return id;
        }

        void Set(string json)
        {
            if (!CAD_SetParticleSettingsJson(_fire, json)) Log("설정 실패 — " + Cad.LastError());
        }

        protected override void Update(double dt)
        {
            _logTimer += dt;
            if (_logTimer < 3) return;
            _logTimer = 0;
            Log("살아 있는 입자: " + string.Join(" · ", _emitters.Select(id => CAD_GetParticleLiveCount(id))));
        }
    }
}
