using System;
using System.Runtime.InteropServices;
using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery
{
    // sdk/csharp 의 원시 선언을 짧게 쓰기 위한 도우미. 엔진 동작은 없다.
    // (CadApi 는 자동 생성이라 손대지 않고, 편의 함수는 이렇게 바깥에 둔다)
    public static class Cad
    {
        // (uint[] out, uint cap) → 총 개수를 돌려주는 함수 공용: 개수 질의 → 버퍼 → 다시 호출
        public static uint[] Ids(Func<uint[], uint, uint> getter)
        {
            uint n = getter(null, 0);
            if (n == 0) return Array.Empty<uint>();
            var ids = new uint[n];
            uint got = getter(ids, n);
            if (got < n) Array.Resize(ref ids, (int)got);
            return ids;
        }

        public static uint[] AllObjectIds() => Ids(CAD_GetObjectIds);

        public static uint[] SelectedIds()
        {
            uint n = CAD_GetSelectedCount();
            var ids = new uint[n];
            for (uint i = 0; i < n; i++) CAD_GetSelectedObjectId(i, out ids[i]);
            return ids;
        }

        public static string LastError() => Str(CAD_GetLastError);
        public static string StatusMessage() => Str(CAD_GetStatusMessage);
        public static string TransientMessage() => Str(CAD_GetTransientMessage);

        // 콜백으로 받은 const char* (UTF-8) → string
        public static string FromUtf8(IntPtr p) => p == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(p);
    }
}
