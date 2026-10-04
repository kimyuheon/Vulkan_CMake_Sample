using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery
{
    // 기능을 바꿀 때 장면을 처음 상태로 돌린다.
    // 기능 하나가 남긴 것(단면·도면 뷰·로봇·진행 중인 도구…)이 다음 기능에 새지 않게 하는 게 목적이다.
    public static class Scene
    {
        public static void Reset(string title)
        {
            // 진행 중인 대화형 상태부터 끝낸다 — 문서를 바꾸기 전에 정리돼야 한다.
            if (CAD_IsTextEditing()) CAD_TextEditCancel();
            CAD_JigCancel();
            CAD_CancelPick();
            while (CAD_TransactionDepth() > 0) CAD_RollbackTransaction();
            // ESC — 진행 중인 도구·OCR 줄 고르기 같은 모드를 끝낸다(키 입력과 같은 경로)
            const int VK_ESCAPE = 0x1B;
            CAD_OnKeyDownVK(VK_ESCAPE);
            CAD_OnKeyUpVK(VK_ESCAPE);
            // 분해도의 풍선·부품표 표시는 문서가 아니라 보기 상태라 따로 끈다
            CAD_ExplodeView("{\"op\":\"balloons\",\"show\":false}", null, 0);
            CAD_ExplodeView("{\"op\":\"bom\",\"show\":false}", null, 0);

            // 문서가 아닌 엔진 상태
            CAD_SetSection(0, 0, 0, false, 0);
            CAD_ClearDrawingViews();
            CAD_NavClearAgents();
            CAD_NavShowGrid(false);
            CAD_LinkStop();
            CAD_ClearRobots();

            // 새 문서를 열고 이전 문서를 닫는다 — 객체뿐 아니라 Undo 기록·도면층도 같이 비워진다.
            // 기능이 탭을 여러 개 열었을 수도 있으니 새 문서 하나만 남긴다.
            CAD_NewDocument(title);
            for (int i = CAD_GetDocumentCount() - 1; i >= 0; i--)
                if (i != CAD_GetActiveDocument()) CAD_CloseDocument(i);

            // 혹시 남은 객체(엔진 기본 데모 등)는 즉시 지운다.
            // CAD_RequestClearAll 은 큐로 가서 다음 틱에 돌기 때문에 방금 만들 객체까지 지워 버린다.
            foreach (uint id in Cad.AllObjectIds()) CAD_DeleteObject(id);

            // 보기 설정
            CAD_SetViewportLayout(0);
            CAD_SetVisualStyle(0);
            CAD_SetSubobjectSelectionMode(0);
            CAD_SetFeatureDimensionsVisible(false);
            CAD_SetGridEnabled(true);
            CAD_RequestClearViewPivot();
            CAD_ClearSelection();
        }

        // 장면을 보기 좋게 — 대부분의 기능이 Setup 끝에 부른다.
        public static void Frame(int view = 3)
        {
            CAD_RequestSetView(view);
            CAD_RequestZoomExtents();
        }
    }
}
