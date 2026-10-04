using System;
using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery
{
    public sealed class ExternalPose : Feature
    {
        public override string Category => "로봇·시뮬";
        public override string Group => "센서";
        public override string Icon => "⌖";
        public override string Title => "외부 위치 연결 (link)";
        public override string Summary =>
            "바깥 프로세스(태그 추적·로봇·모션캡처)가 TCP 로 JSON 한 줄씩 위치를 보내면 객체가 따라 움직인다. " +
            "소켓 없이 호스트가 직접 넣을 수도 있다(ApplyExternalPose). clamp 는 벽에 막히면 벽 앞에 서고, twin 은 그대로 비춘다.";
        public override string[] Apis => new[] { "CAD_LinkStart", "CAD_LinkPort", "CAD_LinkSend", "CAD_LinkStop", "CAD_ApplyExternalPose" };

        uint _walker;
        bool _drive, _clamp = true;
        double _t;
        int _blockedLogged;

        protected override void Setup()
        {
            Tint(Box(0, 0, 0.3f, 6, 2), 0.4f, 0.42f, 0.48f);   // 가운데 벽
            _walker = Tint(CAD_CreateCylinder(-3, 0, 0, 0.35f, 1.7f), 0.95f, 0.6f, 0.2f);
            CAD_SetObjectName(_walker, "작업자");
            CAD_ClearSelection();
            Scene.Frame(1);

            Ui.Section("직접 넣기 (소켓 없이)");
            Ui.Toggle("원 궤도로 움직이기 (매 프레임 ApplyExternalPose)", false, on => _drive = on);
            Ui.Toggle("clamp — 벽에 막힘", true, on => _clamp = on);
            Ui.Section("TCP 문 (127.0.0.1)");
            Ui.Button("열기 (빈 포트 자동)", () =>
            {
                if (!CAD_LinkStart(0, true)) { Log("실패 — " + Cad.LastError()); return; }
                int port = CAD_LinkPort();
                Log($"포트 {port} 에서 대기. 예) 파이썬:");
                Log($"  import socket,json; s=socket.create_connection(('127.0.0.1',{port}))");
                Log("  s.sendall((json.dumps({'type':'pose','id':'작업자','x':2,'y':1,'yaw':90})+'\\n').encode())");
            });
            Ui.Button("붙은 클라이언트에 ping 보내기", () => Log("LinkSend → " + CAD_LinkSend("{\"type\":\"ping\"}")));
            Ui.Button("닫기", CAD_LinkStop);
            Ui.Note("위치 (x,y) 는 객체의 **경계 중심**, yaw 는 도(0 = +Y, 시계 방향 +).");
        }

        protected override void Update(double dt)
        {
            if (!_drive) return;
            _t += dt;
            float a = (float)_t * 0.8f;
            float x = 3 * MathF.Cos(a), y = 2 * MathF.Sin(a);
            float yaw = 90 - a * 180 / MathF.PI;   // 진행 방향을 바라보게
            CAD_ApplyExternalPose(_walker, x, y, yaw, _clamp, out bool blocked);
            if (blocked && _blockedLogged++ % 60 == 0) Log("벽에 막힘 (clamp)");
        }

        public override void Demo()
        {
            CAD_ApplyExternalPose(_walker, 2.5f, 1, 45, false, out _);
        }

        public override void Leave()
        {
            CAD_LinkStop();
            base.Leave();
        }
    }
}
