using System;
using System.Collections.Generic;
using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery
{
    public sealed class CustomCommands : Feature
    {
        public override string Category => "확장";
        public override string Group => "명령";
        public override string Icon => "⌘";
        public override string Title => "명령·UI 항목 등록";
        public override string Summary =>
            "호스트(또는 플러그인)가 엔진에 **새 명령**을 등록하면 명령행·CAD_ExecuteCommand 로 부를 수 있다. " +
            "UI 항목(메뉴·툴바·리본)은 이름과 명령만 올리고, 그리는 건 엔진(단독 실행) 또는 호스트(임베드)가 목록을 읽어 한다.";
        public override string[] Apis => new[]
        {
            "CAD_RegisterCommand", "CAD_UnregisterCommandsByOwner", "CAD_AddUiItem", "CAD_GetUiItemCount",
            "CAD_GetUiItemTitle", "CAD_GetUiItemCommand", "CAD_RemoveUiItemsByOwner",
        };

        const uint Owner = 9001;   // 이 샘플이 올린 것을 한 번에 지우기 위한 소유자 번호
        // 콜백 델리게이트는 필드로 — GC 가 걷어가면 엔진이 죽은 함수를 부른다.
        readonly List<CAD_CommandFn> _keepAlive = new List<CAD_CommandFn>();
        int _stars;

        protected override void Setup()
        {
            Register("star", "별 그리기", Star);
            Register("flower", "꽃 그리기", Flower);
            CAD_AddUiItem(0, "갤러리", "별 그리기", "star", "★", Owner);       // 메뉴
            CAD_AddUiItem(2, "갤러리/모양", "꽃 그리기", "flower", "✿", Owner); // 툴바
            Scene.Frame(1);

            Ui.Section("명령으로 부르기");
            Ui.Input("명령", "star", "실행", c => Log($"> {c}: {(CAD_ExecuteCommand(c) ? "실행" : "모르는 명령")}"));
            Ui.Section("엔진에 등록된 UI 항목 → 호스트가 그린 버튼");
            // 호스트 임베드에서는 엔진이 메뉴를 안 그린다 — 목록을 읽어 자기 컨트롤로 그린다(아래 버튼들)
            uint n = CAD_GetUiItemCount();
            for (uint i = 0; i < n; i++)
            {
                if (CAD_GetUiItemKind(i) == 1) continue;   // 구분선
                string title = Str((b, c) => CAD_GetUiItemTitle(i, b, c));
                string cmd = Str((b, c) => CAD_GetUiItemCommand(i, b, c));
                string icon = Str((b, c) => CAD_GetUiItemIcon(i, b, c));
                string path = Str((b, c) => CAD_GetUiItemPath(i, b, c));
                Ui.Button($"{icon} {title}   ({path} → {cmd})", () => CAD_ExecuteCommand(cmd));
            }
            Log($"등록된 명령 {CAD_GetRegisteredCommandCount()}개, UI 항목 {n}개");
        }

        void Register(string name, string title, Action body)
        {
            CAD_CommandFn fn = _ => { Log($"[{name}] 콜백 호출됨"); body(); };   // 엔진 스레드(= UI 스레드)에서 불린다
            _keepAlive.Add(fn);
            Log($"RegisterCommand(\"{name}\") → {CAD_RegisterCommand(name, title, fn, IntPtr.Zero, Owner)}");
        }

        void Star()
        {
            int k = _stars++;
            var p = new float[10 * 3];
            for (int i = 0; i < 10; i++)
            {
                float r = i % 2 == 0 ? 1.5f : 0.6f, a = MathF.PI / 2 + i * MathF.PI / 5;
                p[i * 3] = k * 3.5f + r * MathF.Cos(a); p[i * 3 + 1] = r * MathF.Sin(a);
            }
            Tint(CAD_CreatePolyline(p, 10, true), 1, 0.85f, 0.3f);
        }

        void Flower()
        {
            int k = _stars++;
            for (int i = 0; i < 6; i++)
            {
                float a = i * MathF.PI / 3;
                Tint(CAD_CreateCircle(k * 3.5f + MathF.Cos(a) * 0.7f, MathF.Sin(a) * 0.7f, 0, 0.6f, 0, 0, 1), 1, 0.5f, 0.7f);
            }
        }

        public override void Leave()
        {
            // 올린 것은 소유자 번호로 한 번에 거둔다 — 안 거두면 사라진 콜백을 부르게 된다
            CAD_UnregisterCommandsByOwner(Owner);
            CAD_RemoveUiItemsByOwner(Owner);
            base.Leave();
        }

        public override void Demo()
        {
            Log("star → " + CAD_ExecuteCommand("star"));
            Later(2, () => Log("flower → " + CAD_ExecuteCommand("flower")));
            Later(4, () => Log($"객체 {CAD_GetObjectCount()}개"));
            Later(2, CAD_RequestZoomExtents);
        }
    }
}
