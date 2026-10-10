// 자동 생성 — 직접 고치지 말 것. 원본: LotCAD_API.h
// 생성기: tools/gen_csharp_binding.py (엔진 빌드의 SDK 내보내기 단계에서 Windows 일 때만 실행)

using System;
using System.Runtime.InteropServices;

namespace LotCAD
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void CAD_CommandFn(IntPtr user);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void CAD_EventFn(int kind, uint id, IntPtr user);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void CAD_SetOnPluginEntityLoaded_cb(uint id, IntPtr owner, IntPtr type, IntPtr data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)] public delegate bool CAD_SetOnConfirm_cb(IntPtr text);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void CAD_SetOnPluginEntityChanged_cb(uint id, IntPtr owner, IntPtr type, IntPtr data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void CAD_SetOnDrawPanel_cb(uint panelId, IntPtr title);

    public static partial class CadApi
    {
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_RegisterCommand([MarshalAs(UnmanagedType.LPUTF8Str)] string name, [MarshalAs(UnmanagedType.LPUTF8Str)] string title, CAD_CommandFn fn, IntPtr user, uint owner);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_UnregisterCommand([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_UnregisterCommandsByOwner(uint owner);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_GetRegisteredCommandCount();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int CAD_GetRegisteredCommandName(uint index, byte[] buf, int bufLen);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int CAD_GetRegisteredCommandTitle(uint index, byte[] buf, int bufLen);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_AddUiItem(int kind, [MarshalAs(UnmanagedType.LPUTF8Str)] string path, [MarshalAs(UnmanagedType.LPUTF8Str)] string title, [MarshalAs(UnmanagedType.LPUTF8Str)] string command, [MarshalAs(UnmanagedType.LPUTF8Str)] string icon, uint owner);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_RemoveUiItem(uint id);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_RemoveUiItemsByOwner(uint owner);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_GetUiItemCount();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_SetOverlayRibbonHeight(float logicalHeight);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float CAD_GetOverlayRibbonTop();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_GetUiItemId(uint index);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int CAD_GetUiItemKind(uint index);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int CAD_GetUiItemPath(uint index, byte[] buf, int bufLen);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int CAD_GetUiItemTitle(uint index, byte[] buf, int bufLen);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int CAD_GetUiItemCommand(uint index, byte[] buf, int bufLen);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int CAD_GetUiItemIcon(uint index, byte[] buf, int bufLen);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_AddUiControl(int kind, [MarshalAs(UnmanagedType.LPUTF8Str)] string path, [MarshalAs(UnmanagedType.LPUTF8Str)] string title, [MarshalAs(UnmanagedType.LPUTF8Str)] string command, double minimum, double maximum, double value, uint owner);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_SetUiControlValue(uint id, double value);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_GetUiControlValue(uint id, out double value, out double minimum, out double maximum);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_LoadPlugins([MarshalAs(UnmanagedType.LPUTF8Str)] string dir);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_UnloadPlugin(uint id);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_GetPluginCount();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_GetPluginId(uint index);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int CAD_GetPluginName(uint index, byte[] buf, int bufLen);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_CreateMesh(float[] xyz, uint vertexCount, uint[] indices, uint indexCount, float[] normals);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_ReplaceMesh(uint id, float[] xyz, uint vertexCount, uint[] indices, uint indexCount, float[] normals);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_SetPluginTag(uint id, [MarshalAs(UnmanagedType.LPUTF8Str)] string owner, [MarshalAs(UnmanagedType.LPUTF8Str)] string type, [MarshalAs(UnmanagedType.LPUTF8Str)] string data);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_ClearPluginTag(uint id);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_HasPluginTag(uint id);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int CAD_GetPluginTagOwner(uint id, byte[] buf, int bufLen);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int CAD_GetPluginTagType(uint id, byte[] buf, int bufLen);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int CAD_GetPluginTagData(uint id, byte[] buf, int bufLen);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_SetOnPluginEntityLoaded(CAD_SetOnPluginEntityLoaded_cb cb);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_SetOnConfirm(CAD_SetOnConfirm_cb cb);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_AddEntityProperty([MarshalAs(UnmanagedType.LPUTF8Str)] string owner, [MarshalAs(UnmanagedType.LPUTF8Str)] string type, [MarshalAs(UnmanagedType.LPUTF8Str)] string key, [MarshalAs(UnmanagedType.LPUTF8Str)] string label, int kind, float minV, float maxV, uint pluginId);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_RemoveEntityPropertiesByOwner(uint pluginId);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_SetOnPluginEntityChanged(CAD_SetOnPluginEntityChanged_cb cb);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern uint CAD_AddEventListener(int kinds, CAD_EventFn fn, IntPtr user, uint pluginId);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CAD_RemoveEventListener(uint handle);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern IntPtr CAD_GetImGuiContext();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_GetImGuiAllocators(out IntPtr allocFn, out IntPtr freeFn, out IntPtr userData);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_SetOnDrawPanel(CAD_SetOnDrawPanel_cb cb);
    }
}
