// 자동 생성 — 직접 고치지 말 것. 원본: LotCAD_API.h
// 생성기: tools/gen_csharp_binding.py (엔진 빌드의 SDK 내보내기 단계에서 Windows 일 때만 실행)

using System;
using System.Runtime.InteropServices;

namespace LotCAD
{
    public static partial class CadApi
    {
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_LoadUrdf([MarshalAs(UnmanagedType.LPUTF8Str)] string path);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_GetRobotCount();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_GetJointCount(uint robot);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int CAD_FindJoint(uint robot, [MarshalAs(UnmanagedType.LPUTF8Str)] string jointName);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int CAD_GetJointName(uint robot, int jointIdx, byte[] outUtf8, int cap);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float CAD_SetJointValue(uint robot, int jointIdx, float value);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float CAD_GetJointValue(uint robot, int jointIdx);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_SetJointValues(uint robot, int[] jointIdx, float[] values, uint count);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_SetJointValuesByName(uint robot, IntPtr[] names, float[] values, uint count);

        /// <summary>CAD_SetJointValuesByName — string[] 를 UTF-8 로 바꿔 넘기는 편의 오버로드.</summary>
        public static uint CAD_SetJointValuesByName(uint robot, string[] names, float[] values, uint count)
        {
            using (var names_utf8 = new Utf8StringArray(names))
            {
                return CAD_SetJointValuesByName(robot, names_utf8.Pointers, values, count);
            }
        }

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_GetJointInfo(uint robot, int jointIdx, out int outType, out float outLower, out float outUpper, [MarshalAs(UnmanagedType.I1)] out bool outHasLimit);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_SetJointAxis(uint robot, int jointIdx, float x, float y, float z);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_GetJointAxis(uint robot, int jointIdx, out float x, out float y, out float z);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_SetJointType(uint robot, int jointIdx, int type);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_SetJointOrigin(uint robot, int jointIdx, float x, float y, float z);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_GetJointOrigin(uint robot, int jointIdx, out float x, out float y, out float z);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_GetLinkObject(uint robot, [MarshalAs(UnmanagedType.LPUTF8Str)] string linkName);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_ClearRobots();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_RigAttach(uint parentObj, uint childObj, float axisX, float axisY, float axisZ, int type, float pivotX, float pivotY, float pivotZ, [MarshalAs(UnmanagedType.I1)] bool usePivot);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_RigDetach(uint obj);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_GetObjectRobot(uint obj);
    }
}
