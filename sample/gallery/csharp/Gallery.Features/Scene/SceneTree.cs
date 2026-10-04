using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery
{
    // 노드 트리 한 줄 — CAD_GetSceneTreeJson 의 nodes[] 항목.
    //   kind: scene(뿌리) · file(가져온 파일) · node(glTF/FBX 노드) · object(객체) · feature(피처·가공 기록)
    public sealed class TreeNode
    {
        public int Index, Parent, Depth;
        public string Key, Kind, Label;
        public int[] Children;
        public int Total, Shown;                 // 아래 객체 수 · 그중 보이는 수
        // 객체 줄만
        public uint ObjectId;
        public string ObjectKind = "";          // "mesh" · "line" · "polyline" · "circle" …
        public bool Visible, Selected, Locked;
        public uint LayerId;
        public string Layer;

        public bool IsObject => Kind == "object";
    }

    // 엔진 노드 트리를 읽어 온 결과. 엔진 ImGui 패널과 같은 구성이다(파일 → 노드 계층 → 객체 → 피처).
    public sealed class SceneTree
    {
        public ulong Revision;
        public TreeNode[] Nodes = new TreeNode[0];

        public TreeNode Root => Nodes.Length > 0 ? Nodes[0] : null;

        public static SceneTree Read()
        {
            var tree = new SceneTree();
            string json = Str(CAD_GetSceneTreeJson, 1 << 16);
            if (string.IsNullOrEmpty(json)) return tree;
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            tree.Revision = root.GetProperty("revision").GetUInt64();
            tree.Nodes = root.GetProperty("nodes").EnumerateArray().Select(Parse).ToArray();
            return tree;
        }

        static TreeNode Parse(JsonElement e)
        {
            var n = new TreeNode
            {
                Index = Int(e, "index"),
                Key = Text(e, "key"),
                Kind = Text(e, "kind"),
                Label = Text(e, "label"),
                Parent = Int(e, "parent", -1),
                Depth = Int(e, "depth"),
                Children = e.TryGetProperty("children", out var ch) ? ch.EnumerateArray().Select(c => c.GetInt32()).ToArray() : new int[0],
                Total = Int(e, "total"),
                Shown = Int(e, "shown"),
            };
            if (n.IsObject)
            {
                n.ObjectId = (uint)Int(e, "objectId");   // 빠져 있으면 0 — 선택·보이기 대상에서 빠진다
                n.ObjectKind = e.TryGetProperty("objectKind", out var k) ? k.ToString() : "";
                n.Visible = Bool(e, "visible");
                n.Selected = Bool(e, "selected");
                n.Locked = Bool(e, "locked");
                n.LayerId = (uint)Int(e, "layerId");
                n.Layer = Text(e, "layer");
            }
            return n;
        }

        // 이 줄 아래(자기 포함) 객체 id 전부 — 묶음 줄을 누르면 그 아래를 한꺼번에 선택·숨김한다
        public List<uint> ObjectIds(TreeNode n)
        {
            var ids = new List<uint>();
            var stack = new Stack<int>();
            stack.Push(n.Index);
            while (stack.Count > 0)
            {
                var cur = Nodes[stack.Pop()];
                if (cur.IsObject && cur.ObjectId != 0) ids.Add(cur.ObjectId);
                foreach (int c in cur.Children) stack.Push(c);
            }
            return ids;
        }

        // 필드가 빠져 있어도 죽지 않게 — 엔진 버전이 달라도 트리는 그려져야 한다
        static int Int(JsonElement e, string key, int fallback = 0) =>
            e.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.Number ? (int)v.GetInt64() : fallback;
        static string Text(JsonElement e, string key) =>
            e.TryGetProperty(key, out var v) ? (v.ValueKind == JsonValueKind.String ? v.GetString() : v.ToString()) : "";
        // true/false 또는 0/1 둘 다 받는다 (selected 는 숫자로 온다)
        static bool Bool(JsonElement e, string key) =>
            e.TryGetProperty(key, out var v) &&
            (v.ValueKind == JsonValueKind.True || (v.ValueKind == JsonValueKind.Number && v.GetDouble() != 0));
    }
}
