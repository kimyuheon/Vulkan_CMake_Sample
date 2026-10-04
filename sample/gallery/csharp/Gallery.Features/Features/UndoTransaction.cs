using System;
using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery
{
    public sealed class UndoTransaction : Feature
    {
        public override string Category => "편집";
        public override string Group => "기록";
        public override string Icon => "↶";
        public override string Title => "Undo·트랜잭션";
        public override string Summary =>
            "API 로 한 편집은 각각 Undo 한 단계다. 여러 편집을 한 단계로 묶으려면 BeginTransaction … EndTransaction. " +
            "RollbackTransaction 은 묶음을 되돌리고 버린다 — '미리 보여 주고 취소' 할 때.";
        public override string[] Apis => new[]
        {
            "CAD_BeginTransaction", "CAD_EndTransaction", "CAD_RollbackTransaction", "CAD_TransactionDepth",
            "CAD_Undo", "CAD_Redo", "CAD_CanUndo", "CAD_CanRedo",
        };

        readonly Random _rnd = new Random(7);

        protected override void Setup()
        {
            Scene.Frame();
            Ui.Section("묶기");
            Ui.Button("상자 10개 — 묶어서 (Undo 한 번에 전부)", () => Boxes(true));
            Ui.Button("상자 10개 — 따로 (Undo 열 번)", () => Boxes(false));
            Ui.Button("미리보기 후 롤백 (아무것도 안 남음)", Preview);
            Ui.Section("되돌리기");
            Ui.Button("Undo", () => { CAD_Undo(); State(); });
            Ui.Button("Redo", () => { CAD_Redo(); State(); });
        }

        void Boxes(bool grouped)
        {
            if (grouped) CAD_BeginTransaction("상자 10개");
            for (int i = 0; i < 10; i++)
            {
                float x = (float)_rnd.NextDouble() * 8 - 4, y = (float)_rnd.NextDouble() * 8 - 4;
                Tint(Box(x, y, 0.6f, 0.6f, 0.3f + (float)_rnd.NextDouble()), 0.4f + (float)_rnd.NextDouble() * 0.5f, 0.6f, 0.8f);
            }
            if (grouped) CAD_EndTransaction();
            CAD_ClearSelection();
            Log($"{(grouped ? "묶어서" : "따로")} 10개 만듦 — 객체 {CAD_GetObjectCount()}개");
            State();
        }

        void Preview()
        {
            CAD_BeginTransaction("미리보기");
            CAD_CreateSphere(0, 0, 0, 2);
            Log($"트랜잭션 안: 객체 {CAD_GetObjectCount()}개, 깊이 {CAD_TransactionDepth()}");
            CAD_RollbackTransaction();
            Log($"롤백 후: 객체 {CAD_GetObjectCount()}개");
            State();
        }

        void State() => Log($"  CanUndo={CAD_CanUndo()} CanRedo={CAD_CanRedo()}");

        public override void Demo()
        {
            Boxes(true);
            Preview();
            Later(2, CAD_RequestZoomExtents);
        }
    }
}
