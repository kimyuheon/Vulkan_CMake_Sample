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
        public static extern bool CAD_ExportFile([MarshalAs(UnmanagedType.LPUTF8Str)] string path, [MarshalAs(UnmanagedType.I1)] bool selectedOnly);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_ExportObj([MarshalAs(UnmanagedType.LPUTF8Str)] string path, [MarshalAs(UnmanagedType.I1)] bool selectedOnly);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_SaveAs([MarshalAs(UnmanagedType.LPUTF8Str)] string pathUtf8, [MarshalAs(UnmanagedType.I1)] bool selectedOnly);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_OpenFile([MarshalAs(UnmanagedType.LPUTF8Str)] string pathUtf8);
    }
}
