// 자동 생성 — 직접 고치지 말 것. 원본: LotCAD_API.h
// 생성기: tools/gen_csharp_binding.py (엔진 빌드의 SDK 내보내기 단계에서 Windows 일 때만 실행)

using System;
using System.Runtime.InteropServices;

namespace LotCAD
{
    public static partial class CadApi
    {
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_RequestStartRevolve();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_BooleanUnion([MarshalAs(UnmanagedType.I1)] bool keepOriginals);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_BooleanIntersection([MarshalAs(UnmanagedType.I1)] bool keepOriginals);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_BooleanDifference(uint[] baseIds, uint baseCount, uint[] subtractIds, uint subtractCount, [MarshalAs(UnmanagedType.I1)] bool keepOriginals);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_BooleanUnionIds(uint[] ids, uint count, [MarshalAs(UnmanagedType.I1)] bool keepOriginals);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_BooleanIntersectionIds(uint[] ids, uint count, [MarshalAs(UnmanagedType.I1)] bool keepOriginals);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_BooleanSubtract(uint[] baseIds, uint baseCount, uint[] cutIds, uint cutCount, [MarshalAs(UnmanagedType.I1)] bool keepOriginals, [MarshalAs(UnmanagedType.I1)] bool perBase, uint[] outIds, uint cap);
    }
}
