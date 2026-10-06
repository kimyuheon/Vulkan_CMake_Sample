// 자동 생성 — 직접 고치지 말 것. 원본: VulkanCAD_API.h
// 생성기: tools/gen_csharp_binding.py (엔진 빌드의 SDK 내보내기 단계에서 Windows 일 때만 실행)

using System;
using System.Runtime.InteropServices;

namespace VulkanCAD
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CAD_BRepInfo
    {
        public int featureType;
        public uint vertexCount;
        public uint edgeCount;
        public uint faceCount;
        public double volume;
        public double surfaceArea;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CAD_BRepParameters
    {
        public int featureType;
        public float dimensionsX;
        public float dimensionsY;
        public float dimensionsZ;
        public float radius;
        public float height;
        public float directionX;
        public float directionY;
        public float directionZ;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CAD_BRepFaceInfo
    {
        public uint faceId;
        public int surfaceType;
        public float originX;
        public float originY;
        public float originZ;
        public float normalX;
        public float normalY;
        public float normalZ;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CAD_BRepCutInfo
    {
        public uint cutIndex;
        public uint pointCount;
        public float depth;
        [MarshalAs(UnmanagedType.I1)] public bool throughAll;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CAD_BooleanCylinderCutInfo
    {
        public uint cutIndex;
        public float centerX;
        public float centerY;
        public float centerZ;
        public float axisX;
        public float axisY;
        public float axisZ;
        public float radius;
        public float height;
    }

    public static partial class CadApi
    {
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_GetBRepInfo(uint id, out CAD_BRepInfo outInfo);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_GetBRepParameters(uint id, out CAD_BRepParameters outParams);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_SetBRepBoxDimensions(uint id, float x, float y, float z);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_SetBRepCylinderDimensions(uint id, float radius, float height);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_SetBRepExtrudeHeight(uint id, float height);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_GetBRepFaceInfo(uint id, uint faceId, out CAD_BRepFaceInfo outInfo);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_PushPullBRepFace(uint id, uint faceId, float distance);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_CutExtrudeBRep(uint id, float[] profileXYZ, uint pointCount, float depth, [MarshalAs(UnmanagedType.I1)] bool throughAll);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_BossExtrudeBRep(uint id, float[] profileXYZ, uint pointCount, float height);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_GetBRepBossCount(uint id);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_GetBRepBossHeight(uint id, uint bossIndex, out float outHeight);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_SetBRepBossHeight(uint id, uint bossIndex, float height);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_GetBRepCutCount(uint id);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_GetBRepCutInfo(uint id, uint cutIndex, out CAD_BRepCutInfo outInfo);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_GetBRepCutProfile(uint id, uint cutIndex, float[] outXYZ, uint capacity);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_SetBRepCut(uint id, uint cutIndex, float[] profileXYZ, uint pointCount, float depth, [MarshalAs(UnmanagedType.I1)] bool throughAll);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_SetBRepCutFromSketch(uint id, uint cutIndex, uint sketchId, float depth, [MarshalAs(UnmanagedType.I1)] bool throughAll);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_RemoveBRepFeatures(uint id, uint[] cutIndices, uint cutCount, uint[] bossIndices, uint bossCount);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_RemoveBRepCut(uint id, uint cutIndex);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_RemoveBRepBoss(uint id, uint bossIndex);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_GetBooleanCylinderCutCount(uint id);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_GetBooleanCylinderCutInfo(uint id, uint cutIndex, out CAD_BooleanCylinderCutInfo outInfo);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_SetBooleanCylinderCut(uint id, uint cutIndex, float centerX, float centerY, float centerZ, float axisX, float axisY, float axisZ, float radius, float height);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_RebuildBRepMesh(uint id, uint curvedSegments);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int CAD_GetSelectedBRepFaceId();
    }
}
