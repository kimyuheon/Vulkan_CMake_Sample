using System;
using System.Text.Json;
using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery
{
    public sealed class Navigation : Feature
    {
        public override string Category => "로봇·시뮬";
        public override string Group => "주행";
        public override string Icon => "➤";
        public override string Title => "경로 주행 (내비)";
        public override string Summary =>
            "장면에서 지나갈 수 있는 격자 지도를 만들고, 객체를 주행 대상으로 등록해 목적지까지 장애물을 피해 가게 한다. " +
            "상태는 JSON 하나로 읽는다(위치·향·경로 길이·도착 여부).";
        public override string[] Apis => new[]
        {
            "CAD_NavBuildMap", "CAD_NavShowGrid", "CAD_NavAddAgent", "CAD_NavSetGoal", "CAD_NavStart", "CAD_NavStop",
            "CAD_NavReset", "CAD_NavGetStateJson",
        };

        uint _robot, _agent;
        double _poll;
        static readonly (float x, float y)[] Goals = { (5, 5), (-5, 5), (5, -5), (-5, -5) };
        int _goal;

        protected override void Setup()
        {
            // 방 + 장애물
            foreach (var (x, y, sx, sy) in new[] { (0f, 7f, 14f, 0.3f), (0f, -7f, 14f, 0.3f), (7f, 0f, 0.3f, 14f), (-7f, 0f, 0.3f, 14f),
                                                   (0f, 2f, 8f, 0.4f), (-2f, -3f, 0.4f, 6f), (3f, -2.5f, 3f, 3f) })
                Tint(Box(x, y, sx, sy, 1.2f), 0.4f, 0.42f, 0.48f);
            _robot = Tint(CAD_CreateCylinder(-5, -5, 0, 0.4f, 0.6f), 0.95f, 0.6f, 0.2f);
            CAD_ClearSelection();
            Scene.Frame();

            Ui.Section("지도·대상");
            Ui.Button("지도 만들기 + 격자 보이기", Build);
            Ui.Section("주행");
            Ui.Choice("목적지", new[] { "오른쪽 위", "왼쪽 위", "오른쪽 아래", "왼쪽 아래" }, 0, i => _goal = i);
            Ui.Button("출발", Go);
            Ui.Button("멈춤", () => CAD_NavStop(_agent));
            Ui.Button("처음 자리로", () => CAD_NavReset(_agent));
            Ui.Slider("속도", 0.5f, 6, 2, v => CAD_NavSetAgentParams(_agent, $"{{\"speed\":{v:0.#}}}"), 1);
            Ui.Toggle("탐지 광선 보이기", false, v => CAD_NavSetAgentParams(_agent, $"{{\"showRays\":{(v ? "true" : "false")}}}"));
        }

        void Build()
        {
            // paramsJson null = 자동(격자 크기·로봇 반경을 장면에서 추정)
            Log("지도 → " + CAD_NavBuildMap(null));
            CAD_NavShowGrid(true);
            if (_agent == 0) _agent = CAD_NavAddAgent(_robot, false);
            Log($"주행 대상 id {_agent}");
        }

        void Go()
        {
            if (_agent == 0) Build();
            var (x, y) = Goals[_goal];
            if (!CAD_NavSetGoal(_agent, x, y, 0)) { Log("길 없음 — " + Cad.LastError()); return; }
            Log($"경로 계획됨 → 출발 {CAD_NavStart(_agent)}  (경로 점 수는 상태 JSON 의 pathPoints)");
        }

        protected override void Update(double dt)
        {
            if (_agent == 0) return;
            _poll += dt;
            if (_poll < 1) return;
            _poll = 0;
            string json = Str(CAD_NavGetStateJson, 4096);
            if (json == null) return;
            using var doc = JsonDocument.Parse(json);
            foreach (var a in doc.RootElement.GetProperty("agents").EnumerateArray())
            {
                if (!a.GetProperty("driving").GetBoolean() && !a.GetProperty("arrived").GetBoolean()) continue;
                var p = a.GetProperty("position");
                Log($"위치 ({p[0].GetSingle():0.0}, {p[1].GetSingle():0.0})  향 {a.GetProperty("yaw").GetSingle():0}°  " +
                    (a.GetProperty("arrived").GetBoolean() ? "도착" : "주행 중"));
            }
        }

        public override void Demo()
        {
            Build();
            _goal = 0;
            Go();
        }

        public override void Leave()
        {
            CAD_NavClearAgents();
            CAD_NavShowGrid(false);
            base.Leave();
        }
    }
}
