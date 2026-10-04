// 자동 생성 — 직접 고치지 말 것. 원본: VulkanCAD_API.h
// 생성기: tools/gen_csharp_binding.py (엔진 빌드의 SDK 내보내기 단계에서 Windows 일 때만 실행)

using System;
using System.Runtime.InteropServices;

namespace VulkanCAD
{
    public static partial class CadApi
    {
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_AttachImageFromFile([MarshalAs(UnmanagedType.LPUTF8Str)] string pathUtf8, float widthWorld, float originX, float originY, float originZ);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_AttachImageFromMemory(byte[] rgba, int width, int height, [MarshalAs(UnmanagedType.LPUTF8Str)] string displayName, float widthWorld, float originX, float originY, float originZ);
    }
}
