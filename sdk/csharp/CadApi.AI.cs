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
        public static extern bool CAD_AiSetEndpoint([MarshalAs(UnmanagedType.LPUTF8Str)] string url, [MarshalAs(UnmanagedType.LPUTF8Str)] string model);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_AiStartLocalServer();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_AiSubmit([MarshalAs(UnmanagedType.LPUTF8Str)] string prompt);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_AiSubmitWithImage([MarshalAs(UnmanagedType.LPUTF8Str)] string prompt, [MarshalAs(UnmanagedType.LPUTF8Str)] string imagePath);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int CAD_AiGetStatus();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int CAD_AiGetLastAnswer(byte[] @out, int cap);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int CAD_AiGetNotes(byte[] @out, int cap);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_AiCancel();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int CAD_RunAiActionsJson([MarshalAs(UnmanagedType.LPUTF8Str)] string actionsJson, byte[] outNotes, int cap);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int CAD_McpCall([MarshalAs(UnmanagedType.LPUTF8Str)] string toolName, [MarshalAs(UnmanagedType.LPUTF8Str)] string argsJson, byte[] @out, int cap);
    }
}
