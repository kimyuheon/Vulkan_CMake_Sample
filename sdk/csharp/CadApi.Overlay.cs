// 자동 생성 — 직접 고치지 말 것. 원본: VulkanCAD_API.h
// 생성기: tools/gen_csharp_binding.py (엔진 빌드의 SDK 내보내기 단계에서 Windows 일 때만 실행)

using System;
using System.Runtime.InteropServices;

namespace VulkanCAD
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CAD_OverlayFrameInfo
    {
        public uint structSize;
        public ulong physicalDevice;
        public ulong device;
        public ulong graphicsQueue;
        public ulong renderPass;
        public ulong commandBuffer;
        public uint graphicsQueueFamily;
        public uint frameIndex;
        public uint frameCount;
        public uint framebufferWidth;
        public uint framebufferHeight;
        public int windowWidth;
        public int windowHeight;
        public float dpiScale;
        public double pointerX;
        public double pointerY;
        public uint pointerButtons;
        public int modifiers;
        public uint sampleCount;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate uint CAD_OverlayRenderFn(IntPtr frame, IntPtr user);

    [StructLayout(LayoutKind.Sequential)]
    public struct CAD_OverlayPointerInfo
    {
        public uint structSize;
        public int windowWidth;
        public int windowHeight;
        public double x;
        public double y;
        public uint buttons;
        public int modifiers;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate uint CAD_OverlayInputFn(IntPtr pointer, IntPtr user);

    public static partial class CadApi
    {
        public const uint CAD_OVERLAY_CAPTURE_POINTER = 1;
        public const uint CAD_OVERLAY_CAPTURE_KEYBOARD = 2;

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_SetOverlayRenderCallback(CAD_OverlayRenderFn callback, IntPtr user);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void CAD_SetOverlayInputCallback(CAD_OverlayInputFn callback, IntPtr user);
    }
}
