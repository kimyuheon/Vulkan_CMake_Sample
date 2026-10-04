// 자동 생성 — 직접 고치지 말 것. 원본: VulkanCAD_API.h
// 생성기: tools/gen_csharp_binding.py (엔진 빌드의 SDK 내보내기 단계에서 Windows 일 때만 실행)

using System;
using System.Runtime.InteropServices;

namespace VulkanCAD
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CAD_InterferenceInfo
    {
        public uint objectA;
        public uint objectB;
        public double volume;
        public uint resultId;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CAD_MeshCheckInfo
    {
        public uint triangles;
        public uint vertices;
        public uint degenerate;
        public uint duplicates;
        public uint openEdges;
        public uint nonManifoldEdges;
        public uint flippedEdges;
        public uint holes;
        public uint components;
        [MarshalAs(UnmanagedType.I1)] public bool closed;
        [MarshalAs(UnmanagedType.I1)] public bool ok;
        public double volume;
        public double area;
    }

    public static partial class CadApi
    {
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_CreateEllipse(float cx, float cy, float cz, float majorX, float majorY, float majorZ, float ratio, float normalX, float normalY, float normalZ);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_CreateSpline(float[] fitXYZ, uint count, [MarshalAs(UnmanagedType.I1)] bool closed);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_CreateLeader(float tipX, float tipY, float tipZ, float bendX, float bendY, float bendZ, [MarshalAs(UnmanagedType.LPUTF8Str)] string textUtf8, float textHeight);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_StretchWindowXY(float minX, float minY, float maxX, float maxY, float dx, float dy, float dz);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_Lengthen(uint id, int mode, float value, [MarshalAs(UnmanagedType.I1)] bool atStart);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_BreakObject(uint id, float x1, float y1, float z1, float x2, float y2, float z2);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_CheckInterference(uint[] ids, uint count, [MarshalAs(UnmanagedType.I1)] bool createObjects, [Out] CAD_InterferenceInfo[] @out, uint cap);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_CheckMesh(uint id, out CAD_MeshCheckInfo @out);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_RepairMeshes(uint[] ids, uint count, [MarshalAs(UnmanagedType.I1)] bool fillHoles);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_RepairMeshesEx(uint[] ids, uint count, [MarshalAs(UnmanagedType.I1)] bool fillHoles, [MarshalAs(UnmanagedType.I1)] bool curvedFill);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_SmoothMeshes(uint[] ids, uint count, int iterations);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_DecimateMeshes(uint[] ids, uint count, double ratio);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_StitchMeshes(uint[] ids, uint count, float tolerance);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_ThickenMeshes(uint[] ids, uint count, double thickness);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_RemeshMeshes(uint[] ids, uint count, double edgeLength);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_AddEdgeBevel(uint id, [MarshalAs(UnmanagedType.I1)] bool fillet, float distance, [MarshalAs(UnmanagedType.I1)] bool allEdges, uint brepEdge);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_GetEdgeBevelCount(uint id);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_Array3DRect(int cols, int rows, int levels, float dx, float dy, float dz);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_Array3DPolar(int count, float angleDeg, float ax, float ay, float az, float bx, float by, float bz, [MarshalAs(UnmanagedType.I1)] bool rotateItems);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_Align3D(uint[] ids, uint count, float[] srcXYZ, float[] dstXYZ, int pairs);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_CreateWedge(float x, float y, float z, float sx, float sy, float sz);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_CreatePyramid(float x, float y, float z, float radius, float height, int sides, float topRadius);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_CreateSphere(float x, float y, float z, float radius);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_CreateCylinder(float x, float y, float z, float radius, float height);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_CreateCone(float x, float y, float z, float radius, float height);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_CreateTorus(float x, float y, float z, float majorRadius, float minorRadius);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_CreateHatch(uint[] ids, uint count, [MarshalAs(UnmanagedType.LPUTF8Str)] string pattern, float scale, float angleDeg);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_Plot([MarshalAs(UnmanagedType.LPUTF8Str)] string path, [MarshalAs(UnmanagedType.I1)] bool selectedOnly, [MarshalAs(UnmanagedType.I1)] bool monochrome, float paperW, float paperH, int pngLongSidePx);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int CAD_ExplodeView([MarshalAs(UnmanagedType.LPUTF8Str)] string argsJson, byte[] outUtf8, int cap);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int CAD_GetMcpToolsJson(byte[] outUtf8, int cap);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_Offset(uint[] ids, uint count, float distance, float sx, float sy, float sz, uint[] outIds, uint outCapacity);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_Mirror(uint[] ids, uint count, float x1, float y1, float z1, float x2, float y2, float z2, float nx, float ny, float nz, [MarshalAs(UnmanagedType.I1)] bool deleteOriginals, uint[] outIds, uint outCapacity);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_Trim(uint id, float px, float py, float pz, uint[] boundaryIds, uint boundaryCount, float nx, float ny, float nz);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_Extend(uint id, float px, float py, float pz, uint[] boundaryIds, uint boundaryCount, float nx, float ny, float nz);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_CreateTube(float x, float y, float z, float outerRadius, float innerRadius, float height);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_SetFeatureDimensionsVisible([MarshalAs(UnmanagedType.I1)] bool visible);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_GetFeatureDimensionsVisible();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_GetFeatureDimensionCount(uint id);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_GetFeatureDimension(uint id, uint index, out int outKind, out float outValue, out uint outSketchId);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_SetFeatureDimension(uint id, uint index, float value);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_Sweep(uint profileId, uint pathId);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_Revolve(uint profileId, float x1, float y1, float z1, float x2, float y2, float z2, int segments);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_ApplyMaterial(uint id, [MarshalAs(UnmanagedType.LPUTF8Str)] string nameUtf8);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_GetMaterialCount();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int CAD_GetMaterialName(uint index, byte[] buf, int bufLen);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_CaptureViewport([MarshalAs(UnmanagedType.LPUTF8Str)] string pathUtf8, [MarshalAs(UnmanagedType.I1)] bool clean);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_OrbitCamera(float ax, float ay, float az, float angleDeg);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_ShowMessage([MarshalAs(UnmanagedType.LPUTF8Str)] string textUtf8, float seconds);
    }
}
