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
        public static extern bool CAD_LinkStart(int port, [MarshalAs(UnmanagedType.I1)] bool clamp);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_LinkStop();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int CAD_LinkPort();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_LinkSend([MarshalAs(UnmanagedType.LPUTF8Str)] string jsonLine);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_ApplyExternalPose(uint id, float x, float y, float yawDeg, [MarshalAs(UnmanagedType.I1)] bool clamp, [MarshalAs(UnmanagedType.I1)] out bool outBlocked);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_CreateVolumeMesh([MarshalAs(UnmanagedType.LPUTF8Str)] string path, float iso, int step);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_CreateLabelMeshes([MarshalAs(UnmanagedType.LPUTF8Str)] string path, int step);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_CreateLine(float x1, float y1, float z1, float x2, float y2, float z2);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_CreateCircle(float cx, float cy, float cz, float radius, float normalX, float normalY, float normalZ);
    }
}
