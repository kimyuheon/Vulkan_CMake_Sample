using System.Drawing;

namespace VulkanCAD.Gallery.WinForms
{
    // 갤러리 색·글꼴 — 엔진 ImGui 테마(3dengine/lot_ui_manager.cpp)와 같은 값.
    static class Theme
    {
        static Color F(float r, float g, float b) => Color.FromArgb((int)(r * 255), (int)(g * 255), (int)(b * 255));

        public static readonly Color Bg = F(0.105f, 0.115f, 0.130f);           // WindowBg
        public static readonly Color Panel = F(0.085f, 0.095f, 0.108f);        // ChildBg
        public static readonly Color MenuBar = F(0.14f, 0.14f, 0.14f);         // MenuBarBg (ImGui 기본)
        public static readonly Color Frame = F(0.145f, 0.165f, 0.190f);        // FrameBg
        public static readonly Color Control = F(0.165f, 0.245f, 0.350f);      // Button
        public static readonly Color ButtonHover = F(0.235f, 0.355f, 0.505f);  // ButtonHovered
        public static readonly Color ButtonActive = F(0.165f, 0.285f, 0.435f); // ButtonActive
        public static readonly Color Accent = F(0.42f, 0.67f, 0.96f);          // 강조 파랑
        public static readonly Color Border = F(0.25f, 0.29f, 0.35f);
        public static readonly Color Separator = F(0.25f, 0.30f, 0.37f);
        public static readonly Color Text = F(1.00f, 1.00f, 1.00f);
        public static readonly Color TextDim = F(0.50f, 0.50f, 0.50f);         // TextDisabled
        public static readonly Color TextCode = F(0.55f, 0.71f, 1.00f);

        public static readonly Font Body = new Font("맑은 고딕", 9f);
        public static readonly Font Small = new Font("맑은 고딕", 8.25f);
        public static readonly Font Heading = new Font("맑은 고딕", 12f, FontStyle.Bold);
        public static readonly Font Section = new Font("맑은 고딕", 9f, FontStyle.Bold);
        public static readonly Font Code = new Font("Consolas", 8.5f);
    }
}
