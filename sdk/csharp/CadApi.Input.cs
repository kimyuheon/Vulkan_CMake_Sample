// 자동 생성 — 직접 고치지 말 것. 원본: LotCAD_API.h
// 생성기: tools/gen_csharp_binding.py (엔진 빌드의 SDK 내보내기 단계에서 Windows 일 때만 실행)

using System;
using System.Runtime.InteropServices;

namespace LotCAD
{
    public static partial class CadApi
    {
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_OnKeyDownVK(int vkCode);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_OnKeyUpVK(int vkCode);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int CAD_GizmoHitTest(double x, double y);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_SetGizmoTouchMode([MarshalAs(UnmanagedType.I1)] bool enabled);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_SetSnapTouchMode([MarshalAs(UnmanagedType.I1)] bool enabled);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_SetDimensionTextScreenFixed([MarshalAs(UnmanagedType.I1)] bool enabled);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_GetDimensionTextScreenFixed();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int CAD_GetDimensionPointCount();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int CAD_GetPrompt(byte[] buf, int bufLen);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_RequestStartBoxSketch();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_RequestStartLineSketch();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_RequestStartRectangleSketch();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_RequestStartCircleSketch();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_RequestStartPolygonSketch();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_RequestCyclePolygonSides();
    }
}
