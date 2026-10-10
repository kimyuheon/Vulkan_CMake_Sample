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
        public static extern bool CAD_CreateRoom(float originX, float originY, float originZ, float width, float depth, float height, float wallThickness, uint[] outWallIds);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_CreateFloorplan(float originX, float originY, float originZ, float[] rectBounds, uint spaceCount, float height, float wallThickness, uint[] outWallIds, uint outCapacity);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_CreateFloorplanWithOpenings(float originX, float originY, float originZ, float[] rectBounds, uint spaceCount, float height, float wallThickness, CAD_FloorplanOpeningDesc[] openings, uint openingCount, uint[] outWallIds, uint outCapacity);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int CAD_FloorplanExtractFromImage([MarshalAs(UnmanagedType.LPUTF8Str)] string path, float widthMm, byte[] outJson, int cap);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int CAD_FloorplanExtractFromRgba(byte[] rgba, int w, int h, float widthMm, byte[] outJson, int cap);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_CreateWallGraph(float ox, float oy, float oz, float[] segs, uint count, float height, float defaultThickness, uint[] outIds, uint cap);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_CreateFloorplanFromImage([MarshalAs(UnmanagedType.LPUTF8Str)] string path, float widthMm, float height, [MarshalAs(UnmanagedType.I1)] bool underlayImage, uint[] outIds, uint cap);
    }
}
