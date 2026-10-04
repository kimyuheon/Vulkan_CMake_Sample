using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace VulkanCAD.Gallery.Wpf
{
    // 엔진 ImGui 리본(3dengine/ui/lot_ribbon.cpp)과 같은 모양을 OnRender 로 직접 그린다.
    //   탭 한 줄(글자 + 활성 밑줄) → 그룹들(큰 버튼 + 작은 아이콘 3줄 격자) → 그룹 이름 띠 → 세로 구분선
    //   탭을 더블클릭하면 탭 줄만 남기고 접힌다.
    // 내용(탭·그룹·버튼)은 Gallery.Features 의 RibbonTab 모델에서 온다 — WinForms 갤러리와 같은 리본.
    public sealed class RibbonBar : FrameworkElement
    {
        const int Rows = 3;
        const double CellGap = 2, TabPadX = 12, GroupGap = 9, CaptionScale = 0.85, CaptionGap = 4;
        const double Fs = 13;   // 본문 글자 크기(DIP)

        static readonly Typeface Body = new Typeface("Malgun Gothic");
        static readonly Typeface Symbol = new Typeface("Segoe UI Symbol");

        List<RibbonTab> _tabs = new List<RibbonTab>();
        int _active;
        bool _collapsed;
        readonly List<(Rect rect, int tab)> _tabHits = new List<(Rect, int)>();
        readonly List<(Rect rect, RibbonItem item)> _itemHits = new List<(Rect, RibbonItem)>();
        object _hover, _pressed;

        public event Action ItemClicked;

        public RibbonBar()
        {
            ToolTipService.SetInitialShowDelay(this, 400);
        }

        public void SetTabs(List<RibbonTab> tabs, int active = 0)
        {
            _tabs = tabs;
            _active = Math.Clamp(active, 0, Math.Max(0, tabs.Count - 1));
            InvalidateVisual();
        }

        public void SelectTab(string name)
        {
            int i = _tabs.FindIndex(t => t.Name == name);
            if (i >= 0 && i != _active) { _active = i; InvalidateVisual(); }
        }

        // ── 치수 (엔진과 같은 비율) ──
        static double ButtonSide => Math.Round(Fs * 1.6 + 4);
        static double TabRowHeight => Math.Round(Fs + 12);
        static double CaptionHeight => Math.Round(Fs * CaptionScale) + 4;
        static double GridHeight => ButtonSide * Rows + CellGap * (Rows - 1);
        double PreferredHeight => _collapsed ? TabRowHeight + 1 : TabRowHeight + 4 + GridHeight + CaptionGap + CaptionHeight + 5;

        protected override Size MeasureOverride(Size available) => new Size(0, PreferredHeight);

        // ── 그리기 ──
        protected override void OnRender(DrawingContext dc)
        {
            _tabHits.Clear();
            _itemHits.Clear();
            dc.DrawRectangle(Brush("Bg"), null, new Rect(0, 0, ActualWidth, ActualHeight));
            dc.DrawRectangle(Brush("MenuBar"), null, new Rect(0, 0, ActualWidth, TabRowHeight));

            double x = 6;
            for (int i = 0; i < _tabs.Count; i++)
            {
                var ft = Text(_tabs[i].Name, Fs, Body, i == _active || Equals(_hover, i) ? Brush("Text") : Brush("TextDim"));
                var r = new Rect(x, 0, ft.Width + TabPadX * 2, TabRowHeight);
                dc.DrawText(ft, new Point(r.X + TabPadX, (r.Height - ft.Height) / 2));
                if (i == _active) dc.DrawRoundedRectangle(Brush("Accent"), null, new Rect(r.X + 4, r.Bottom - 2, r.Width - 8, 2), 1, 1);
                _tabHits.Add((r, i));
                x = r.Right;
            }
            dc.DrawLine(new Pen(Brush("Border"), 1), new Point(0, ActualHeight - 0.5), new Point(ActualWidth, ActualHeight - 0.5));
            if (_collapsed || _tabs.Count == 0) return;

            var next = new Point(10, TabRowHeight + 4);
            foreach (var grp in _tabs[_active].Groups) next = DrawGroup(dc, grp, next);
        }

        Point DrawGroup(DrawingContext dc, RibbonGroup grp, Point origin)
        {
            double smallX = origin.X;
            int count = 0;
            foreach (var it in grp.Items)
            {
                if (it.Big)
                {
                    double w = Math.Max(Math.Round(Fs * 3.4), Text(it.Label, Fs * CaptionScale, Body, null).Width + 12);
                    DrawBig(dc, it, new Rect(smallX, origin.Y, w, GridHeight));
                    smallX += w + CellGap * 2;
                }
                else
                {
                    int row = count % Rows, col = count / Rows;
                    count++;
                    double step = ButtonSide + CellGap;
                    DrawSmall(dc, it, new Rect(smallX + col * step, origin.Y + row * step, ButtonSide, ButtonSide));
                }
            }
            int cols = (count + Rows - 1) / Rows;
            double width = (smallX - origin.X) + (cols > 0 ? cols * ButtonSide + (cols - 1) * CellGap : -CellGap * 2);

            double bandY = origin.Y + GridHeight + CaptionGap;
            var band = Brush("Frame").Clone(); band.Opacity = 0.55;
            dc.DrawRoundedRectangle(band, null, new Rect(origin.X - 3, bandY, width + 6, CaptionHeight), 3, 3);
            var cap = Text(grp.Caption, Fs * CaptionScale, Body, Brush("TextDim"));
            dc.DrawText(cap, new Point(origin.X + Math.Max(0, (width - cap.Width) / 2), bandY + (CaptionHeight - cap.Height) / 2));
            double sepX = origin.X + width + GroupGap;
            dc.DrawLine(new Pen(Brush("Separator"), 1), new Point(sepX + 0.5, origin.Y + 2), new Point(sepX + 0.5, bandY + CaptionHeight));
            return new Point(sepX + GroupGap + 1, origin.Y);
        }

        void DrawBig(DrawingContext dc, RibbonItem it, Rect r)
        {
            DrawBack(dc, it, r, 5);
            var icon = Text(it.Icon ?? "", Fs * 1.9, Symbol, Brush("Text"));
            var label = Text(it.Label, Fs * CaptionScale, Body, Brush("Text"));
            dc.DrawText(icon, new Point(r.X + (r.Width - icon.Width) / 2, r.Y + (r.Height - CaptionHeight - icon.Height) / 2));
            dc.DrawText(label, new Point(r.X + (r.Width - label.Width) / 2, r.Bottom - CaptionHeight - 1));
            _itemHits.Add((r, it));
        }

        void DrawSmall(DrawingContext dc, RibbonItem it, Rect r)
        {
            DrawBack(dc, it, r, 4);
            var icon = Text(it.Icon ?? "", Fs * 1.05, Symbol, Brush("Text"));
            dc.DrawText(icon, new Point(r.X + (r.Width - icon.Width) / 2, r.Y + (r.Height - icon.Height) / 2));
            _itemHits.Add((r, it));
        }

        // 배경 없음 · 호버에 옅은 판 · 누름에 진한 판 · 켜진 항목은 강조색 판 + 테두리 (엔진과 같다)
        void DrawBack(DrawingContext dc, RibbonItem it, Rect r, double round)
        {
            bool active = it.IsActive?.Invoke() == true, hot = _hover == it, held = hot && _pressed == it;
            Brush fill = null;
            if (active) { var b = Brush("Accent").Clone(); b.Opacity = held ? 0.55 : 0.32; fill = b; }
            else if (held) fill = Brush("ButtonActive");
            else if (hot) fill = Brush("ButtonHover");
            var pen = active ? new Pen(Brush("Accent"), 1) : null;
            if (fill != null || pen != null) dc.DrawRoundedRectangle(fill, pen, r, round, round);
        }

        // ── 마우스 ──
        object HitTest(Point p)
        {
            foreach (var (r, t) in _tabHits) if (r.Contains(p)) return t;
            foreach (var (r, it) in _itemHits) if (r.Contains(p)) return it;
            return null;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            var h = HitTest(e.GetPosition(this));
            if (Equals(h, _hover)) return;
            _hover = h;
            ToolTip = h is RibbonItem it ? (it.Big ? it.Label + "\n" + it.Tip : it.Label + "  " + it.Tip) : null;
            InvalidateVisual();
        }

        protected override void OnMouseLeave(MouseEventArgs e) { _hover = null; InvalidateVisual(); }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            _pressed = HitTest(e.GetPosition(this));
            if (_pressed is int tab)
            {
                if (e.ClickCount == 2) _collapsed = !_collapsed;
                else { _active = tab; _collapsed = false; }
                InvalidateMeasure();
            }
            InvalidateVisual();
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            var released = HitTest(e.GetPosition(this));
            bool click = released is RibbonItem && released == _pressed;
            _pressed = null;
            InvalidateVisual();
            if (!click) return;
            ((RibbonItem)released).Click?.Invoke();
            ItemClicked?.Invoke();
            InvalidateVisual();   // 켜진 항목 강조가 바뀌었을 수 있다
        }

        // ── 도우미 ──
        Brush Brush(string key) => (Brush)FindResource(key);

        FormattedText Text(string s, double size, Typeface face, Brush brush) =>
            new FormattedText(s ?? "", CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, face, size,
                              brush ?? Brushes.White, VisualTreeHelper.GetDpi(this).PixelsPerDip);
    }
}
