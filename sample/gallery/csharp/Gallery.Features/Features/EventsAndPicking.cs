using System;
using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery
{
    public sealed class EventsAndPicking : Feature
    {
        public override string Category => "객체·속성";
        public override string Group => "이벤트";
        public override string Icon => "⚡";
        public override string Title => "이벤트·점 받기·호버";
        public override string Summary =>
            "엔진 → 호스트 알림(선택 변경·생성·삭제·문서 변경)을 콜백으로 받고, 사용자에게 점이나 객체를 클릭받는다" +
            "(AutoLISP getpoint / entsel). 콜백은 CAD_Tick 안 UI 스레드에서 불려 화면을 바로 고쳐도 된다.";
        public override string[] Apis => new[]
        {
            "CAD_SetOnSelectionChanged", "CAD_AddEventListener", "CAD_RemoveEventListener", "CAD_BeginGetPoint",
            "CAD_BeginGetEntSel", "CAD_IsPicking", "CAD_TryGetPickResult", "CAD_GetHoveredObject",
        };

        // 콜백 델리게이트는 반드시 필드로 — 지역 변수면 GC 가 걷어가 엔진이 죽은 함수를 부른다.
        CAD_SetOnSelectionChanged_cb _onSelection;
        CAD_EventFn _onEvent;
        uint _listener;
        bool _picking;
        uint _lastHover;

        protected override void Setup()
        {
            for (int i = 0; i < 5; i++) Tint(CAD_CreateSphere(i * 2, 0, 0, 0.6f), 0.3f + i * 0.15f, 0.6f, 0.9f - i * 0.12f);
            CAD_ClearSelection();
            Scene.Frame();

            // ① 한 칸짜리 알림 — 호스트 하나가 쓰는 단순한 방법
            _onSelection = () => Log($"[선택 변경] {CAD_GetSelectedCount()}개");
            CAD_SetOnSelectionChanged(_onSelection);

            // ② 목록형 구독 — 여러 개 동시에. kinds: 1=생성 2=삭제 4=선택 8=문서변경
            _onEvent = (kind, id, user) =>
            {
                string what = kind switch { 1 => "생성", 2 => "삭제", 8 => "문서변경", _ => kind.ToString() };
                if (kind != 4) Log($"[이벤트] {what} id {id}");
            };
            _listener = CAD_AddEventListener(1 | 2 | 8, _onEvent, IntPtr.Zero, 0);

            Ui.Section("점·객체 받기");
            Ui.Button("점 하나 클릭받기 (getpoint)", () => { CAD_BeginGetPoint("구를 놓을 점을 클릭"); _picking = true; });
            Ui.Button("객체 하나 클릭받기 (entsel)", () => { CAD_BeginGetEntSel("지울 객체를 클릭"); _picking = true; });
            Ui.Note("클릭하면 결과를 로그에 찍고 동작한다. ESC = 취소.");
            Ui.Section("호버");
            Ui.Toggle("호버 강조", CAD_GetHoverHighlight(), CAD_SetHoverHighlight);
            Ui.Note("마우스를 구 위에 올리면 아래 로그에 id 가 나온다(CAD_GetHoveredObject 폴링).");
        }

        protected override void Update(double dt)
        {
            // 픽: 대기 중이면 IsPicking=true. 끝나면 TryGetPickResult 가 한 번 true(완료) 또는 false(취소)
            if (_picking && !CAD_IsPicking())
            {
                _picking = false;
                if (CAD_TryGetPickResult(out float x, out float y, out float z, out uint id))
                {
                    if (id == 0) { Log($"점 ({x:0.##}, {y:0.##}, {z:0.##}) → 구 놓기"); CAD_CreateSphere(x, y, z, 0.4f); }
                    else { Log($"객체 {id} → 삭제"); CAD_DeleteObject(id); }
                }
                else Log("취소됨");
            }
            uint h = CAD_GetHoveredObject();
            if (h != _lastHover) { _lastHover = h; if (h != 0) Log($"호버: id {h}"); }
        }

        public override void Leave()
        {
            CAD_CancelPick();
            CAD_SetOnSelectionChanged(null);
            CAD_RemoveEventListener(_listener);
            base.Leave();
        }

        public override void Demo()
        {
            Box(0, 3, 1, 1, 1);           // → [이벤트] 생성
            CAD_RequestSelectAll();       // → [선택 변경] (다음 틱)
        }
    }
}
