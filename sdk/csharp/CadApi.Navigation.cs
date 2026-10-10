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
        public static extern bool CAD_NavBuildMap([MarshalAs(UnmanagedType.LPUTF8Str)] string paramsJson);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_NavShowGrid([MarshalAs(UnmanagedType.I1)] bool on);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_NavAddAgent(uint objectId, [MarshalAs(UnmanagedType.I1)] bool isVehicle);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_NavAddSelected();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_NavRemoveAgent(uint agentId);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_NavClearAgents();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_NavSetAgentParams(uint agentId, [MarshalAs(UnmanagedType.LPUTF8Str)] string json);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_NavSetGoal(uint agentId, float x, float y, float z);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_NavStart(uint agentId);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_NavStop(uint agentId);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_NavReset(uint agentId);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int CAD_NavGetStateJson(byte[] @out, int cap);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_NavGetPath(uint agentId, float[] outXY, uint cap);
    }
}
