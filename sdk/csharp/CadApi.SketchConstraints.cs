// 자동 생성 — 직접 고치지 말 것. 원본: LotCAD_API.h
// 생성기: tools/gen_csharp_binding.py (엔진 빌드의 SDK 내보내기 단계에서 Windows 일 때만 실행)

using System;
using System.Runtime.InteropServices;

namespace LotCAD
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CAD_SketchConstraintInfo
    {
        public uint id;
        public int type;
        public uint entityA;
        public int pointA;
        public uint entityB;
        public int pointB;
        public float value;
        [MarshalAs(UnmanagedType.I1)] public bool driving;
        [MarshalAs(UnmanagedType.I1)] public bool enabled;
    }

    public static partial class CadApi
    {
        public const int CAD_SKETCH_WHOLE_ENTITY = 255;
        public const int CAD_SKETCH_COINCIDENT = 0;
        public const int CAD_SKETCH_HORIZONTAL = 1;
        public const int CAD_SKETCH_VERTICAL = 2;
        public const int CAD_SKETCH_CONCENTRIC = 3;
        public const int CAD_SKETCH_DISTANCE = 4;
        public const int CAD_SKETCH_RADIUS = 5;
        public const int CAD_SKETCH_DIAMETER = 6;
        public const int CAD_SKETCH_PARALLEL = 7;
        public const int CAD_SKETCH_PERPENDICULAR = 8;
        public const int CAD_SKETCH_EQUAL = 9;
        public const int CAD_SKETCH_FIXED = 10;
        public const int CAD_SKETCH_ANGLE = 11;
        public const int CAD_SKETCH_TANGENT = 12;
        public const int CAD_SKETCH_DISTANCE_X = 13;
        public const int CAD_SKETCH_DISTANCE_Y = 14;
        public const int CAD_SKETCH_UNDER = 0;
        public const int CAD_SKETCH_WELL = 1;
        public const int CAD_SKETCH_REDUNDANT = 2;
        public const int CAD_SKETCH_CONFLICT = 3;
        public const int CAD_SKETCH_INVALID = 4;

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_AddSketchConstraint(int type, uint entityA, int pointA, uint entityB, int pointB, float value);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_SetSketchConstraintValue(uint constraintId, float value);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_RemoveSketchConstraint(uint constraintId);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_SolveSketchConstraints();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_GetSketchConstraintCount();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_GetSketchConstraintIds(uint[] outIds, uint capacity);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_GetSketchConstraintInfo(uint constraintId, out CAD_SketchConstraintInfo outInfo);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int CAD_GetSketchDiagnosis(uint entityId, out int outDof, out int outStatus, uint[] outProblemIds, uint capacity);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_GetLastSketchConflict(uint[] outIds, uint capacity);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_CreatePolygon(float cx, float cy, float cz, float radius, int sides, float normalX, float normalY, float normalZ, float firstVertexDirX, float firstVertexDirY, float firstVertexDirZ);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_CreateArc(float startX, float startY, float startZ, float throughX, float throughY, float throughZ, float endX, float endY, float endZ);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_CreatePolyline(float[] xyz, uint pointCount, [MarshalAs(UnmanagedType.I1)] bool closed);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_CreateRectangle(float x0, float y0, float z0, float x2, float y2, float z2);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_CreateExtrude(uint sourceId, float height);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_DeleteObject(uint id);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_SelectObject(uint id, [MarshalAs(UnmanagedType.I1)] bool additive);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_ClearSelection();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_GetObjectCount();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_GetPosition(uint id, out float x, out float y, out float z);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_SetPosition(uint id, float x, float y, float z);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_GetScale(uint id, out float sx, out float sy, out float sz);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_SetScale(uint id, float sx, float sy, float sz);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_GetRotationAxisAngle(uint id, out float axisX, out float axisY, out float axisZ, out float angleRadians);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_SetRotationAxisAngle(uint id, float axisX, float axisY, float axisZ, float angleRadians);
    }
}
