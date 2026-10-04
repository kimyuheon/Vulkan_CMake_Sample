using System.Collections.Generic;
using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery
{
    // 노드 트리·도면층 창에서 하는 동작. 전부 기존 C API 로 한다 — 트리 전용 편집 함수는 없다.
    public static class SceneOps
    {
        // 줄 클릭 = 그 아래 객체 전부 선택. additive(Ctrl) 면 기존 선택에 더한다.
        public static void Select(IReadOnlyList<uint> ids, bool additive)
        {
            if (!additive) CAD_ClearSelection();
            foreach (var id in ids) CAD_SelectObject(id, true);
        }

        public static void SetVisible(IReadOnlyList<uint> ids, bool visible)
        {
            foreach (var id in ids) CAD_SetObjectVisible(id, visible);
        }

        // 이름은 장면 안에서 유일해야 한다 — 겹치면 false
        public static bool Rename(uint id, string name) => CAD_SetObjectName(id, name);

        public static void Delete(IReadOnlyList<uint> ids)
        {
            CAD_BeginTransaction("삭제");   // 묶음 삭제를 Undo 한 번으로
            foreach (var id in ids) CAD_DeleteObject(id);
            CAD_EndTransaction();
        }

        public static void ZoomTo(IReadOnlyList<uint> ids)
        {
            Select(ids, false);
            CAD_RequestFocusSelected();
        }

        // ── 도면층 ──
        public static uint NewLayer(LayerTable table)
        {
            // 이름이 겹치면 실패하므로 빈 번호를 찾는다
            for (int i = 1; ; i++)
            {
                string name = "도면층 " + i;
                bool taken = false;
                foreach (var l in table.Layers) if (l.Name == name) { taken = true; break; }
                if (!taken) return CAD_CreateLayer(name);
            }
        }

        public static uint AssignSelected(uint layerId) => CAD_AssignSelectedToLayer(layerId);
    }
}
