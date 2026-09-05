using System;
using System.Runtime.InteropServices;

namespace VulkanCadWpf
{
    // VulkanCADCore.dll 의 extern "C" CAD_* 함수 P/Invoke 선언.
    // DLL 은 CMake 의 VulkanCADCoreShared 타겟(OUTPUT_NAME=VulkanCADCore) 산출물.
    // 빌드 출력 폴더에 VulkanCADCore.dll + models/ textures/ fonts/ 가 있어야 함.
    internal static class CadApi
    {
        const string DLL = "VulkanCADCore.dll";

        // ── 생명주기 / 뷰 ──
        [DllImport(DLL)] public static extern bool CAD_CreateEngine();
        [DllImport(DLL)] public static extern void CAD_DestroyEngine();
        [DllImport(DLL)] public static extern bool CAD_Tick();
        [DllImport(DLL)] public static extern bool CAD_ShouldClose();
        [DllImport(DLL)] public static extern void CAD_SetIgnoreCloseRequest(bool ignore);
        // CharSet.Ansi 는 한글 경로를 깨뜨린다(엔진은 UTF-8 을 기대). byte[] 로 넘긴다.
        [DllImport(DLL)] public static extern bool CAD_SetRuntimeAssetPath(byte[] pathUtf8);
        [DllImport(DLL)] public static extern void CAD_AttachView(IntPtr nativeHandle, int width, int height);
        [DllImport(DLL)] public static extern void CAD_ResizeView(int width, int height);
        [DllImport(DLL)] public static extern void CAD_DetachView();

        // ── 입력 ──
        [DllImport(DLL)] public static extern void CAD_OnMouseDown(int button, double x, double y, int modifiers);
        [DllImport(DLL)] public static extern void CAD_OnMouseUp(int button, double x, double y, int modifiers);
        [DllImport(DLL)] public static extern void CAD_OnMouseMove(double x, double y);
        [DllImport(DLL)] public static extern void CAD_OnMouseWheel(double x, double y, double deltaX, double deltaY);
        [DllImport(DLL)] public static extern void CAD_OnKeyDownVK(int vkCode);
        [DllImport(DLL)] public static extern void CAD_OnKeyUpVK(int vkCode);

        // ── 툴바 명령 ──
        [DllImport(DLL)] public static extern uint CAD_CreateBox(float x, float y, float z, float sx, float sy, float sz);
        [DllImport(DLL)] public static extern void CAD_RequestStartLineSketch();
        [DllImport(DLL)] public static extern void CAD_RequestStartRectangleSketch();
        [DllImport(DLL)] public static extern void CAD_RequestStartCircleSketch();
        // ── 명령 실행 ──
        // 엔진 명령 200여 개를 이름으로 전부 쓸 수 있다. 아래 CAD_RequestXxx 는
        // 자주 쓰는 것만 뽑아 둔 것이라, 없는 기능은 이걸로 부르면 된다.
        //   CadApi.CAD_ExecuteCommand("trim");
        //   CadApi.CAD_ExecuteCommand("offset");
        [DllImport(DLL, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_ExecuteCommand(string name);

        // ── 알림(콜백) ──
        // ⚠️ 델리게이트를 **필드로 붙들어 둬야** 한다. 지역 변수로 넘기면 GC 가 걷어가
        //    엔진이 이미 사라진 함수를 부른다(그 순간 프로세스가 죽는다).
        //    호출은 CAD_Tick 안 = UI 스레드이므로 콜백에서 화면을 바로 고쳐도 된다.
        public delegate void VoidCb();
        public delegate void IdCb(uint id);
        public delegate void BoolCb([MarshalAs(UnmanagedType.I1)] bool value);
        public delegate void TextCb(IntPtr utf8);

        [DllImport(DLL)] public static extern void CAD_SetOnSelectionChanged(VoidCb cb);
        [DllImport(DLL)] public static extern void CAD_SetOnObjectCreated(IdCb cb);
        [DllImport(DLL)] public static extern void CAD_SetOnObjectDeleted(IdCb cb);
        [DllImport(DLL)] public static extern void CAD_SetOnDocumentDirty(BoolCb cb);
        [DllImport(DLL)] public static extern void CAD_SetOnPrompt(TextCb cb);

        // ── 측정 ── 상태바에 "길이 3,600" / "면적 12.5" 를 띄울 때. 월드 좌표 기준.
        [DllImport(DLL)] [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_GetLength(uint id, out float outLength);
        [DllImport(DLL)] [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_GetArea(uint id, out float outArea);
        [DllImport(DLL)] public static extern float CAD_GetSelectionLength();
        [DllImport(DLL)] public static extern float CAD_GetSelectionArea();

        // ── 내보내기 ── 확장자로 포맷이 정해진다 (.stl .dxf .glb .obj .lot)
        [DllImport(DLL, CharSet = CharSet.Ansi)] [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_ExportFile(string path,
                                                 [MarshalAs(UnmanagedType.I1)] bool selectedOnly);

        [DllImport(DLL)] public static extern void CAD_RequestStartPolylineSketch();
        [DllImport(DLL)] public static extern void CAD_RequestAddCube();
        [DllImport(DLL)] public static extern void CAD_RequestSelectAll();
        [DllImport(DLL)] public static extern void CAD_RequestDeleteSelected();
        [DllImport(DLL)] public static extern void CAD_RequestClearAll();
        [DllImport(DLL)] public static extern void CAD_RequestZoomExtents();
        [DllImport(DLL)] public static extern void CAD_RequestSetView(int viewType);   // 0=Front 1=Top 2=Right 3=Iso
        [DllImport(DLL)] public static extern void CAD_RequestToggleProjection();
        [DllImport(DLL)] public static extern void CAD_Undo();
        [DllImport(DLL)] public static extern void CAD_Redo();

        // ── 파일 ──
        // 경로는 UTF-8 로 넘긴다. C# 의 기본 문자열 마샬링(Ansi)은 한글 경로에서 깨지므로
        // CharSet 을 쓰지 않고 byte[] 로 직접 넘긴다 (Utf8 헬퍼 참고).
        [DllImport(DLL)] public static extern bool CAD_SaveAs(byte[] pathUtf8, bool selectedOnly);
        [DllImport(DLL)] public static extern bool CAD_OpenFile(byte[] pathUtf8);

        // ── 편집 명령 (전부 현재 선택에 적용, 각각 undo 1스텝) ──
        [DllImport(DLL)] public static extern bool CAD_Loft();
        [DllImport(DLL)] public static extern bool CAD_Shell(float thickness);          // <=0 = 자동
        [DllImport(DLL)] public static extern bool CAD_Explode(bool keepOriginal);
        [DllImport(DLL)] public static extern bool CAD_EdgeFillet(float distance);      // <=0 = 자동
        [DllImport(DLL)] public static extern bool CAD_EdgeChamfer(float distance);
        [DllImport(DLL)] public static extern bool CAD_ArrayRect(int cols, int rows, float dx, float dy);
        [DllImport(DLL)] public static extern bool CAD_ArrayPolar(int count, float angleDeg,
                                                                 float centerX, float centerY, float centerZ,
                                                                 bool rotateItems);

        // ── 조회 ──
        [DllImport(DLL)] public static extern uint CAD_GetObjectCount();
        [DllImport(DLL)] public static extern uint CAD_GetSelectedCount();

        // 한글 경로를 위한 UTF-8 변환 — 위 파일 API 에 넘길 때 반드시 이걸 거친다.
        public static byte[] Utf8(string s) =>
            System.Text.Encoding.UTF8.GetBytes((s ?? string.Empty) + "\0");
    }
}
