// 자동 생성 — 직접 고치지 말 것. 원본: LotCAD_API.h
// 생성기: tools/gen_csharp_binding.py (엔진 빌드의 SDK 내보내기 단계에서 Windows 일 때만 실행)

using System;
using System.Runtime.InteropServices;

namespace LotCAD
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void CAD_SetOnSelectionChanged_cb();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void CAD_SetOnObjectCreated_cb(uint id);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void CAD_SetOnObjectDeleted_cb(uint id);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void CAD_SetOnDocumentDirty_cb([MarshalAs(UnmanagedType.I1)] bool dirty);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void CAD_SetOnPrompt_cb(IntPtr text);

    public static partial class CadApi
    {
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_SetOnSelectionChanged(CAD_SetOnSelectionChanged_cb cb);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_SetOnObjectCreated(CAD_SetOnObjectCreated_cb cb);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_SetOnObjectDeleted(CAD_SetOnObjectDeleted_cb cb);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_SetOnDocumentDirty(CAD_SetOnDocumentDirty_cb cb);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_SetOnPrompt(CAD_SetOnPrompt_cb cb);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_RequestStartExtrude();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_RequestStartArcSketch();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_RequestCycleArcMode();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_RequestStartPolylineSketch();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_RequestOpenFileDialog();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_RequestZoomExtents();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_RequestSelectAll();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_RequestDeleteSelected();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_RequestClearAll();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_RequestAddCube();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_RequestStartLightPlacement();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_RequestFocusSelected();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_RequestToggleProjection();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_RequestToggleDemoLighting();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_RequestCycleMaterial();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_RequestRemoveMaterial();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_RequestAdjustTextureScale(float delta);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_RequestAdjustLineWidth(float deltaPx);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_RequestSetView(int viewType);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_RequestSetProjection([MarshalAs(UnmanagedType.I1)] bool ortho);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_RequestSetViewDirection(float dirX, float dirY, float dirZ);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_RequestSetViewPivot(float x, float y, float z, int axis);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_RequestClearViewPivot();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_GetViewPivot(out float x, out float y, out float z, out int axis);
    }
}
