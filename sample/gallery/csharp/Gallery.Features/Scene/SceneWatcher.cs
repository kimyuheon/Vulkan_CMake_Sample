using System;
using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery
{
    // 노드 트리·도면층 창을 "바뀐 때만" 다시 그리게 알려 준다.
    //   · 객체 추가·삭제·이름·보이기·도면층 → CAD_GetSceneRevision 번호가 커진다(매 프레임 비교만, 비용 없음)
    //   · 선택 변경 → 이벤트 구독(CAD_AddEventListener, 4=선택). 기능들이 쓰는 한 칸짜리 SetOnSelectionChanged 와 겹치지 않는다.
    public sealed class SceneWatcher
    {
        ulong _revision = ulong.MaxValue;
        bool _selectionDirty = true;
        CAD_EventFn _onEvent;   // 콜백은 필드로 붙들어 둔다
        uint _listener;

        public void Start()
        {
            _onEvent = (kind, id, user) => _selectionDirty = true;
            _listener = CAD_AddEventListener(4, _onEvent, IntPtr.Zero, 0);
        }

        public void Stop()
        {
            if (_listener != 0) CAD_RemoveEventListener(_listener);
            _listener = 0;
        }

        // 매 프레임 호출. 장면이 바뀌었으면 true(구조), 선택만 바뀌었으면 selectionOnly=true
        public bool Poll(out bool selectionOnly)
        {
            ulong rev = CAD_GetSceneRevision();
            bool structure = rev != _revision;
            selectionOnly = !structure && _selectionDirty;
            _revision = rev;
            bool any = structure || _selectionDirty;
            _selectionDirty = false;
            return any;
        }

        public void Invalidate() => _revision = ulong.MaxValue;
    }
}
