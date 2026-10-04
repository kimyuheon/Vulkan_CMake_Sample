using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace VulkanCAD.Gallery.WinForms
{
    // 엔진 ImGui 리본(3dengine/ui/lot_ribbon.cpp)과 같은 모양을 GDI+ 로 직접 그린다.
    //   탭 한 줄(글자 + 활성 밑줄) → 그룹들(큰 버튼 + 작은 아이콘 3줄 격자) → 그룹 이름 띠 → 세로 구분선
    //   탭을 더블클릭하면 탭 줄만 남기고 접힌다.
    // 내용(탭·그룹·버튼)은 Gallery.Features 의 RibbonTab 모델에서 온다.
    public sealed class RibbonBar : Control
    {
        const int Rows = 3;
        const float CellGap = 2, TabPadX = 12, GroupGap = 9, CaptionScale = 0.85f, CaptionGap = 4;

        List<RibbonTab> _tabs = new List<RibbonTab>();
        int _active;
        bool _collapsed;

        // 그릴 때 계산한 클릭 영역
        readonly List<(RectangleF rect, int tab)> _tabHits = new List<(RectangleF, int)>();
        readonly List<(RectangleF rect, RibbonItem item)> _itemHits = new List<(RectangleF, RibbonItem)>();
        object _hover, _pressed;
        readonly ToolTip _tip = new ToolTip { InitialDelay = 400, ReshowDelay = 100 };
        readonly Font _icon, _iconBig, _caption;

        public event Action ItemClicked;

        public RibbonBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Font = Theme.Body;
            BackColor = Theme.Bg;
            _icon = new Font("Segoe UI Symbol", Fs * 0.85f, GraphicsUnit.Pixel);
            _iconBig = new Font("Segoe UI Symbol", Fs * 1.7f, GraphicsUnit.Pixel);
            _caption = new Font(Font.FontFamily, Font.Size * CaptionScale);
            Height = PreferredHeight;
        }

        public void SetTabs(List<RibbonTab> tabs, int active = 0)
        {
            _tabs = tabs;
            _active = Math.Clamp(active, 0, Math.Max(0, tabs.Count - 1));
            Invalidate();
        }

        public void SelectTab(string name)
        {
            int i = _tabs.FindIndex(t => t.Name == name);
            if (i >= 0 && i != _active) { _active = i; Invalidate(); }
        }

        // ── 치수 (엔진과 같은 비율, 글꼴 크기 기준) ─────────────────────────
        float Fs => Font.Height;
        float ButtonSide => MathF.Round(Fs * 1.6f);
        float TabRowHeight => MathF.Round(Fs + 10);
        float CaptionHeight => MathF.Round(Fs * CaptionScale) + 2;
        float GridHeight => ButtonSide * Rows + CellGap * (Rows - 1);
        int PreferredHeight => (int)(_collapsed ? TabRowHeight + 1 : TabRowHeight + 4 + GridHeight + CaptionGap + CaptionHeight + 5);

        // ── 그리기 ────────────────────────────────────────────────────────
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            _tabHits.Clear();
            _itemHits.Clear();

            // 탭 줄
            g.FillRectangle(Brush(Theme.MenuBar), 0, 0, Width, TabRowHeight);
            float x = 6;
            for (int i = 0; i < _tabs.Count; i++)
            {
                var size = g.MeasureString(_tabs[i].Name, Font);
                var r = new RectangleF(x, 0, size.Width + TabPadX * 2, TabRowHeight);
                bool hot = Equals(_hover, i);
                g.DrawString(_tabs[i].Name, Font, Brush(i == _active || hot ? Theme.Text : Theme.TextDim),
                             r.X + TabPadX, r.Y + (r.Height - size.Height) / 2);
                if (i == _active) Fill(g, Theme.Accent, new RectangleF(r.X + 4, r.Bottom - 2, r.Width - 8, 2), 1);
                _tabHits.Add((r, i));
                x = r.Right;
            }
            g.DrawLine(Pen(Theme.Border), 0, Height - 1, Width, Height - 1);
            if (_collapsed || _tabs.Count == 0) return;

            // 그룹들
            var next = new PointF(10, TabRowHeight + 4);
            foreach (var grp in _tabs[_active].Groups) next = DrawGroup(g, grp, next);
        }

        PointF DrawGroup(Graphics g, RibbonGroup grp, PointF origin)
        {
            float smallX = origin.X;
            int count = 0;
            foreach (var it in grp.Items)
            {
                if (it.Big)
                {
                    float w = BigWidth(g, it.Label);
                    var r = new RectangleF(smallX, origin.Y, w, GridHeight);
                    DrawBig(g, it, r);
                    smallX += w + CellGap * 2;
                }
                else
                {
                    int row = count % Rows, col = count / Rows;
                    count++;
                    float step = ButtonSide + CellGap;
                    DrawSmall(g, it, new RectangleF(smallX + col * step, origin.Y + row * step, ButtonSide, ButtonSide));
                }
            }
            int cols = (count + Rows - 1) / Rows;
            float width = (smallX - origin.X) + (cols > 0 ? cols * ButtonSide + (cols - 1) * CellGap : -CellGap * 2);

            // 그룹 이름 띠 + 오른쪽 구분선
            float bandY = origin.Y + GridHeight + CaptionGap;
            Fill(g, Color.FromArgb(140, Theme.Frame), new RectangleF(origin.X - 3, bandY, width + 6, CaptionHeight), 3);
            var ts = g.MeasureString(grp.Caption, _caption);
            g.DrawString(grp.Caption, _caption, Brush(Theme.TextDim),
                         origin.X + Math.Max(0, (width - ts.Width) / 2), bandY + (CaptionHeight - ts.Height) / 2);
            float sepX = origin.X + width + GroupGap;
            g.DrawLine(Pen(Theme.Separator), sepX, origin.Y + 2, sepX, bandY + CaptionHeight);
            return new PointF(sepX + GroupGap + 1, origin.Y);
        }

        float BigWidth(Graphics g, string label) =>
            MathF.Max(MathF.Round(Fs * 3.1f), g.MeasureString(label, _caption).Width + 10);

        void DrawBig(Graphics g, RibbonItem it, RectangleF r)
        {
            DrawButtonBack(g, it, r, 5);
            float labelH = CaptionHeight;
            var isz = g.MeasureString(it.Icon ?? "", _iconBig);
            g.DrawString(it.Icon ?? "", _iconBig, Brush(Theme.Text), r.X + (r.Width - isz.Width) / 2, r.Y + (r.Height - labelH - isz.Height) / 2);
            var lsz = g.MeasureString(it.Label, _caption);
            g.DrawString(it.Label, _caption, Brush(Theme.Text), r.X + (r.Width - lsz.Width) / 2, r.Bottom - labelH - 1);
            _itemHits.Add((r, it));
        }

        void DrawSmall(Graphics g, RibbonItem it, RectangleF r)
        {
            DrawButtonBack(g, it, r, 4);
            var isz = g.MeasureString(it.Icon ?? "", _icon);
            g.DrawString(it.Icon ?? "", _icon, Brush(Theme.Text), r.X + (r.Width - isz.Width) / 2, r.Y + (r.Height - isz.Height) / 2);
            _itemHits.Add((r, it));
        }

        // 배경 없음 · 호버에 옅은 판 · 누름에 진한 판 · 켜진 항목은 강조색 판 + 테두리 (엔진과 같다)
        void DrawButtonBack(Graphics g, RibbonItem it, RectangleF r, float round)
        {
            bool active = it.IsActive?.Invoke() == true, hot = _hover == it, held = hot && _pressed == it;
            if (active) Fill(g, Color.FromArgb(held ? 140 : 82, Theme.Accent), r, round);
            else if (held) Fill(g, Theme.ButtonActive, r, round);
            else if (hot) Fill(g, Theme.ButtonHover, r, round);
            if (active) using (var p = RoundRect(r, round)) g.DrawPath(Pen(Color.FromArgb(230, Theme.Accent)), p);
        }

        // ── 마우스 ────────────────────────────────────────────────────────
        object HitTest(Point p)
        {
            foreach (var (r, t) in _tabHits) if (r.Contains(p)) return t;
            foreach (var (r, it) in _itemHits) if (r.Contains(p)) return it;
            return null;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            var h = HitTest(e.Location);
            if (Equals(h, _hover)) return;
            _hover = h;
            _tip.SetToolTip(this, h is RibbonItem it ? (it.Big ? it.Label + "\n" + it.Tip : it.Label + "  " + it.Tip) : null);
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e) { _hover = null; Invalidate(); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            _pressed = HitTest(e.Location);
            if (_pressed is int tab)
            {
                if (e.Clicks == 2) { _collapsed = !_collapsed; Height = PreferredHeight; }
                else { _active = tab; if (_collapsed) { _collapsed = false; Height = PreferredHeight; } }
            }
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            var released = HitTest(e.Location);
            if (released is RibbonItem it && released == _pressed)
            {
                _pressed = null;
                Invalidate();
                it.Click?.Invoke();
                ItemClicked?.Invoke();
                Invalidate();   // 켜진 항목 강조가 바뀌었을 수 있다
                return;
            }
            _pressed = null;
            Invalidate();
        }

        // ── 그리기 도우미 ─────────────────────────────────────────────────
        static readonly Dictionary<Color, SolidBrush> _brushes = new Dictionary<Color, SolidBrush>();
        static readonly Dictionary<Color, Pen> _pens = new Dictionary<Color, Pen>();
        static SolidBrush Brush(Color c) => _brushes.TryGetValue(c, out var b) ? b : _brushes[c] = new SolidBrush(c);
        static Pen Pen(Color c) => _pens.TryGetValue(c, out var p) ? p : _pens[c] = new Pen(c);

        static void Fill(Graphics g, Color c, RectangleF r, float round)
        {
            using var path = RoundRect(r, round);
            g.FillPath(Brush(c), path);
        }

        static GraphicsPath RoundRect(RectangleF r, float rad)
        {
            var p = new GraphicsPath();
            float d = rad * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }
}
