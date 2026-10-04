using System.Linq;
using System.Text.Json;
using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery
{
    // 도면층 한 줄 — CAD_GetLayersJson 의 layers[] 항목
    public sealed class LayerInfo
    {
        public uint Id;
        public string Name;
        public bool Visible, Locked, Fixed;      // Fixed = 도면층 0 (지울 수도, 색을 바꿀 수도 없다)
        public float R, G, B, Opacity;
        public uint LinetypeId;
        public string Linetype;
        public int ObjectCount, SelectedCount;
    }

    public sealed class LayerTable
    {
        public ulong Revision;
        public LayerInfo[] Layers = new LayerInfo[0];

        public static LayerTable Read()
        {
            var t = new LayerTable();
            string json = Str(CAD_GetLayersJson, 8192);
            if (string.IsNullOrEmpty(json)) return t;
            using var doc = JsonDocument.Parse(json);
            t.Revision = doc.RootElement.GetProperty("revision").GetUInt64();
            t.Layers = doc.RootElement.GetProperty("layers").EnumerateArray().Select(e =>
            {
                var c = e.GetProperty("color");
                return new LayerInfo
                {
                    Id = e.GetProperty("id").GetUInt32(),
                    Name = e.GetProperty("name").GetString(),
                    Visible = e.GetProperty("visible").GetBoolean(),
                    Locked = e.GetProperty("locked").GetBoolean(),
                    Fixed = e.TryGetProperty("fixed", out var f) && f.GetBoolean(),
                    R = c[0].GetSingle(), G = c[1].GetSingle(), B = c[2].GetSingle(),
                    Opacity = e.GetProperty("opacity").GetSingle(),
                    LinetypeId = e.GetProperty("linetypeId").GetUInt32(),
                    Linetype = e.GetProperty("linetype").GetString(),
                    ObjectCount = e.GetProperty("objectCount").GetInt32(),
                    SelectedCount = e.GetProperty("selectedCount").GetInt32(),
                };
            }).ToArray();
            return t;
        }
    }
}
