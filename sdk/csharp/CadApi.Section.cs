// 자동 생성 — 직접 고치지 말 것. 원본: LotCAD_API.h
// 생성기: tools/gen_csharp_binding.py (엔진 빌드의 SDK 내보내기 단계에서 Windows 일 때만 실행)

using System;
using System.Runtime.InteropServices;

namespace LotCAD
{
    public static partial class CadApi
    {
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_SetSection(int mode, int axis, float pos, [MarshalAs(UnmanagedType.I1)] bool flip, float thickness);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_SetSectionBox(float minX, float minY, float minZ, float maxX, float maxY, float maxZ);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_SetSectionOptions([MarshalAs(UnmanagedType.I1)] bool showArrows, [MarshalAs(UnmanagedType.I1)] bool selectedOnly);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int CAD_GetSectionStateJson(byte[] @out, int cap);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_ExtractSectionTo2D([MarshalAs(UnmanagedType.I1)] bool includeBehind);
    }
}
