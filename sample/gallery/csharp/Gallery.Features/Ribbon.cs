using System;
using System.Collections.Generic;

namespace VulkanCAD.Gallery
{
    // 리본의 내용(탭·그룹·버튼). 그리는 건 각 호스트(WinForms/WPF)가 한다 — 엔진 ImGui 리본과 같은 모양으로.
    //   탭 = 기능의 Category, 그룹 = 기능의 Group, 큰 버튼 = 기능 하나
    //   "홈" 탭만 기능이 아니라 공통 명령(실행 취소·뷰·표시 스타일)을 작은 아이콘으로 둔다.
    public sealed class RibbonItem
    {
        public string Label;
        public string Icon;             // 유니코드 글리프 한 글자
        public string Tip;
        public bool Big;                // 큰 버튼(그룹 높이 전체) / 작은 아이콘(3줄 격자)
        public Action Click;
        public Func<bool> IsActive;     // 켜진 상태 강조(현재 기능 등). null 이면 강조 없음
    }

    public sealed class RibbonGroup
    {
        public string Caption;
        public List<RibbonItem> Items = new List<RibbonItem>();
    }

    public sealed class RibbonTab
    {
        public string Name;
        public List<RibbonGroup> Groups = new List<RibbonGroup>();
    }
}
