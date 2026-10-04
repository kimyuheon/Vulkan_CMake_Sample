using System;
using System.Collections.Generic;
using System.Linq;
using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery
{
    // 갤러리 리본 구성 — 홈 탭(공통 명령) + 기능 탭들(카탈로그에서 자동).
    public static class GalleryRibbon
    {
        public static List<RibbonTab> Build(IReadOnlyList<Feature> features, Action<Feature> open, Func<Feature> current)
        {
            var tabs = new List<RibbonTab> { Home() };
            foreach (var cat in features.GroupBy(f => f.Category))
            {
                var tab = new RibbonTab { Name = cat.Key };
                foreach (var grp in cat.GroupBy(f => f.Group))
                {
                    var g = new RibbonGroup { Caption = grp.Key };
                    foreach (var f in grp)
                    {
                        var ff = f;
                        g.Items.Add(new RibbonItem
                        {
                            Label = f.Title, Icon = f.Icon, Tip = f.Summary, Big = true,
                            Click = () => open(ff), IsActive = () => current() == ff,
                        });
                    }
                    tab.Groups.Add(g);
                }
                tabs.Add(tab);
            }
            return tabs;
        }

        // 엔진 리본의 "홈" 탭에 해당하는 공통 명령. 어떤 기능을 보고 있든 쓸 수 있다.
        static RibbonTab Home()
        {
            var tab = new RibbonTab { Name = "홈" };
            tab.Groups.Add(Group("실행",
                Small("실행 취소", "↶", "Ctrl+Z — CAD_Undo", CAD_Undo),
                Small("다시 실행", "↷", "Ctrl+Y — CAD_Redo", CAD_Redo)));
            tab.Groups.Add(Group("선택",
                Small("전체 선택", "▣", "CAD_RequestSelectAll", CAD_RequestSelectAll),
                Small("선택 해제", "□", "CAD_ClearSelection", CAD_ClearSelection),
                Small("선택 삭제", "✕", "CAD_RequestDeleteSelected", CAD_RequestDeleteSelected)));
            tab.Groups.Add(Group("뷰",
                Small("등각", "◈", "CAD_RequestSetView(3)", () => CAD_RequestSetView(3)),
                Small("평면", "▭", "CAD_RequestSetView(1)", () => CAD_RequestSetView(1)),
                Small("정면", "▯", "CAD_RequestSetView(0)", () => CAD_RequestSetView(0)),
                Small("우측면", "◧", "CAD_RequestSetView(2)", () => CAD_RequestSetView(2)),
                Small("줌 맞춤", "⤢", "CAD_RequestZoomExtents", CAD_RequestZoomExtents),
                Small("투영 전환", "⬚", "직교 ↔ 원근 — CAD_RequestToggleProjection", CAD_RequestToggleProjection)));
            tab.Groups.Add(Group("표시",
                Small("음영", "●", "CAD_SetVisualStyle(0)", () => CAD_SetVisualStyle(0)),
                Small("음영+모서리", "◉", "CAD_SetVisualStyle(1)", () => CAD_SetVisualStyle(1)),
                Small("와이어프레임", "○", "CAD_SetVisualStyle(2)", () => CAD_SetVisualStyle(2)),
                Small("숨은선", "◌", "CAD_SetVisualStyle(4)", () => CAD_SetVisualStyle(4)),
                Small("그리드 끄기", "#", "CAD_SetGridEnabled(false)", () => CAD_SetGridEnabled(false)),
                Small("그리드 켜기", "⊞", "CAD_SetGridEnabled(true)", () => CAD_SetGridEnabled(true))));
            tab.Groups.Add(Group("화면",
                Small("단일 뷰", "▢", "CAD_SetViewportLayout(0)", () => CAD_SetViewportLayout(0)),
                Small("좌우 분할", "◫", "CAD_SetViewportLayout(1)", () => CAD_SetViewportLayout(1)),
                Small("4분할", "⊞", "CAD_SetViewportLayout(3)", () => CAD_SetViewportLayout(3))));
            return tab;
        }

        static RibbonGroup Group(string caption, params RibbonItem[] items)
        {
            var g = new RibbonGroup { Caption = caption };
            g.Items.AddRange(items);
            return g;
        }

        static RibbonItem Small(string label, string icon, string tip, Action click) =>
            new RibbonItem { Label = label, Icon = icon, Tip = tip, Click = click };
    }
}
