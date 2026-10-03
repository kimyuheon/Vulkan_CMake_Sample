import QtQuick
import QtQuick.Controls
import QtQuick.Layouts
import VulkanCadQml

// 리본 — 엔진 ui/lot_ribbon.cpp 를 QML 로 옮긴 것.
//
// 같게 맞춘 것: 탭(홈/2D/3D) 한 줄 + 아이콘 3줄 격자, 그룹마다 [큰 버튼…][작은 격자] + 아래 이름 띠 +
// 세로 구분선, 활성 탭 밑줄, 탭 더블클릭으로 접기, 툴팁 "이름  (명령)\n설명", 강조 파랑.
// 치수도 엔진 상수를 그대로 쓴다(폰트 14px 기준 ≈121px).
//
// 버튼은 전부 CAD_ExecuteCommand 로 간다 — 엔진 리본이 ToolAction 으로 하는 일을 호스트에선
// 명령행과 같은 입구로 하는 것이라 결과가 같다. 단축키만 있는 항목(실행취소 등)은 전용 API 로.
//
// 3D 뷰가 네이티브 자식 창이라 리본 자체는 그 바깥(header)에 있어야 한다. 드롭다운이 필요하면
// QML Popup 이 아니라 Platform.Menu 로 띄워야 뷰 뒤로 숨지 않는다.
Item {
    id: root

    property int  activeTab: 0
    property bool collapsed: false
    property int  gizmoMode: -1      // 0 이동 1 회전 2 축척 — 켜진 버튼 강조용(호스트는 아직 모름)

    // ── 엔진 lot_ribbon.cpp 의 상수 ──
    readonly property real  fontSize:      14
    readonly property real  buttonSide:    Math.round(fontSize * 1.6)        // 22
    readonly property int   rows:          3
    readonly property real  cellGap:       2
    readonly property real  tabPadX:       12
    readonly property real  groupGap:      9
    readonly property real  captionScale:  0.85
    readonly property real  captionGap:    4
    readonly property real  tabRowHeight:  Math.round(fontSize + 10)         // 24
    readonly property real  captionHeight: Math.round(fontSize * captionScale) + 2
    readonly property real  gridHeight:    buttonSide * rows + cellGap * (rows - 1)   // 70
    readonly property real  bigMinWidth:   Math.round(fontSize * 3.1)

    readonly property color accent:     Qt.rgba(0.42, 0.67, 0.96, 1.0)
    readonly property color textCol:    "#e6e6e6"
    readonly property color dimCol:     "#9a9a9a"
    readonly property color tabBarBg:   "#232323"
    readonly property color bodyBg:     "#2d2d2d"
    readonly property color borderCol:  "#1a1a1a"

    implicitHeight: collapsed ? tabRowHeight + 1
                              : tabRowHeight + 4 + gridHeight + captionGap + captionHeight + 5

    // ── 내용 — 엔진 리본의 탭·그룹·버튼을 그대로 옮겼다(라벨·명령·설명 동일) ──
    // big: 그룹의 대표 도구(3줄 높이를 다 쓰는 큰 버튼). act: CadCommands 메서드(명령이 아닌 것).
    readonly property var tabs: [
        { name: "홈", groups: [
            { caption: "기즈모", items: [
                { icon: "GizmoMove",   label: "이동",   cmd: "gm", tip: "화살표 핸들을 끌어 이동 (기준점 안 물음)", big: true, mode: 0 },
                { icon: "GizmoRotate", label: "회전",   cmd: "gr", tip: "링 핸들을 끌어 회전", big: true, mode: 1 },
                { icon: "GizmoScale",  label: "축척",   cmd: "gs", tip: "상자 핸들을 끌어 축척", big: true, mode: 2 } ] },
            { caption: "편집", items: [
                { icon: "Move",        label: "이동",     cmd: "m",   tip: "기준점 → 목적지 또는 거리 입력", big: true },
                { icon: "Rotate",      label: "회전",     cmd: "ro",  tip: "중심 → 각도 또는 각도 입력" },
                { icon: "Scale",       label: "축척",     cmd: "sc",  tip: "기준점 → 배율 또는 배율 입력" },
                { icon: "Copy",        label: "복사",     cmd: "cp",  tip: "기준점 → 목적지 (반복 복사)" },
                { icon: "GroupRotate", label: "그룹회전", cmd: "gro", tip: "선택 전체를 지정 축 중심으로 3D 회전" },
                { icon: "Array",       label: "배열",     cmd: "ar",  tip: "직사각형(행×열) / 원형 배열" } ] },
            { caption: "선택", items: [
                { icon: "SelectAll", label: "전체선택", cmd: "Ctrl+A", tip: "모든 객체 선택", big: true, act: "selectAll" },
                { icon: "Delete",    label: "삭제",     cmd: "Del",    tip: "선택 객체 삭제", act: "deleteSelected" },
                { icon: "ClearAll",  label: "모두지움", cmd: "clear",  tip: "씬의 모든 객체 삭제" } ] },
            { caption: "실행", items: [
                { icon: "Undo", label: "실행취소", cmd: "Ctrl+Z", tip: "직전 동작 되돌리기", big: true, act: "undo" },
                { icon: "Redo", label: "재실행",   cmd: "Ctrl+Y", tip: "되돌린 동작 다시 적용", big: true, act: "redo" } ] },
            { caption: "뷰", items: [
                { icon: "ZoomExtents", label: "전체보기", cmd: "z",       tip: "모든 객체를 화면에 맞춤", big: true },
                { icon: "Focus",       label: "선택맞춤", cmd: "f",       tip: "선택 객체에 카메라 포커스" },
                { icon: "Section",     label: "단면",     cmd: "section", tip: "평면/슬라이스/상자로 잘라 내부 보기" } ] },
            { caption: "모드", items: [
                { icon: "Character", label: "조종",   cmd: "character", tip: "3인칭 캐릭터 조종 — WASD 로 이동", big: true },
                { icon: "Vehicle",   label: "자동차", cmd: "drive",     tip: "W/S 전·후진, A/D 조향. 다시 누르면 캐릭터로", big: true },
                { icon: "Cad",       label: "캐드",   cmd: "cad",       tip: "그리기/편집 — 궤도 카메라 + 명령행" },
                { icon: "Fly",       label: "비행",   cmd: "fly",       tip: "자유 비행 — WASD 이동, 마우스 시선" },
                { icon: "Walk",      label: "걷기",   cmd: "walk",      tip: "눈높이 워크스루 — 바닥 따라 걷기" } ] } ] },

        { name: "2D", groups: [
            { caption: "그리기", items: [
                { icon: "Line",      label: "선",       cmd: "l",    tip: "두 점 클릭", big: true },
                { icon: "Rectangle", label: "사각형",   cmd: "rec",  tip: "두 대각 모서리" },
                { icon: "Circle",    label: "원",       cmd: "c",    tip: "중심 + 반지름" },
                { icon: "Polygon",   label: "정다각형", cmd: "pol",  tip: "sides 명령으로 변 수 변경" },
                { icon: "Arc",       label: "호",       cmd: "a",    tip: "3점 (arcmode 로 모드 순환)" },
                { icon: "Polyline",  label: "폴리선",   cmd: "pl",   tip: "N클릭, x=완료 c=닫기" },
                { icon: "Text",      label: "문자",     cmd: "text", tip: "클릭해 배치 후 타이핑" },
                { icon: "Dimension", label: "치수",     cmd: "dp",   tip: "치수 패널 — 정렬·일반·각도·반지름·지름" } ] },
            { caption: "수정", items: [
                { icon: "Offset",  label: "오프셋", cmd: "of",      tip: "선/원/호/폴리선 평행 복제", big: true },
                { icon: "Mirror",  label: "대칭",   cmd: "mi",      tip: "미러선 기준 대칭 복제" },
                { icon: "Align",   label: "정렬",   cmd: "al",      tip: "소스·대상 점으로 정렬" },
                { icon: "Trim",    label: "자르기", cmd: "tr",      tip: "경계까지 잘라내기" },
                { icon: "Extend",  label: "연장",   cmd: "ex",      tip: "경계까지 늘리기" },
                { icon: "Fillet",  label: "필렛",   cmd: "fil",     tip: "두 선 모서리 호 처리" },
                { icon: "Chamfer", label: "모따기", cmd: "cha",     tip: "두 선 모서리 직선 처리" },
                { icon: "Join",    label: "결합",   cmd: "jo",      tip: "연결된 선/폴리선을 하나로" },
                { icon: "Explode", label: "분해",   cmd: "explode", tip: "문자→윤곽선, 폴리선/사각형→개별 선분" } ] },
            { caption: "폴리선", items: [
                { icon: "PolyClose",   label: "닫기",     cmd: "pclose",   tip: "마지막 점 ↔ 첫 점 연결", big: true },
                { icon: "PolyOpen",    label: "열기",     cmd: "popen",    tip: "닫힌 폴리선의 마지막 연결 해제" },
                { icon: "PolyReverse", label: "방향반전", cmd: "preverse", tip: "정점 순서 뒤집기" },
                { icon: "PolyInsert",  label: "정점삽입", cmd: "pinsert",  tip: "선택 그립 뒤에 중점 추가" },
                { icon: "PolyDelete",  label: "정점삭제", cmd: "pdelete",  tip: "선택한 그립의 정점 제거" } ] } ] },

        { name: "3D", groups: [
            { caption: "프리미티브", items: [
                { icon: "Box",      label: "박스",   cmd: "b",      tip: "3단계 스케치", big: true },
                { icon: "Sphere",   label: "구",     cmd: "sphere", tip: "기본 크기로 생성 후 이동/축척" },
                { icon: "Cylinder", label: "원기둥", cmd: "cyl",    tip: "축=Z, 기본 크기" },
                { icon: "Cone",     label: "원뿔",   cmd: "cone",   tip: "축=Z, 기본 크기" },
                { icon: "Torus",    label: "토러스", cmd: "torus",  tip: "도넛 — 평면=XY" },
                { icon: "Arrow",    label: "화살표", cmd: "arrow",  tip: "3D 화살표 — 꼬리/촉 2점" } ] },
            { caption: "형상", items: [
                { icon: "Extrude",   label: "돌출",   cmd: "x",   tip: "닫힌 스케치 → 3D 솔리드", big: true },
                { icon: "Revolve",   label: "회전체", cmd: "rev", tip: "닫힌 단면 + 축 → 회전 솔리드" },
                { icon: "Sweep",     label: "스윕",   cmd: "sw",  tip: "닫힌 단면 + 경로 → 쓸기 솔리드" },
                { icon: "Loft",      label: "로프트", cmd: "lo",  tip: "닫힌 단면 2개 이상을 순서대로 연결" },
                { icon: "Shell",     label: "셸",     cmd: "sh",  tip: "선택 솔리드의 속을 비우고 두께 생성" },
                { icon: "PushPull",  label: "밀당",   cmd: "pp",  tip: "면 클릭 → 드래그/거리로 밀기·당기기" },
                { icon: "FaceInset", label: "면 인셋", cmd: "fi", tip: "평면의 볼록 면에 일정 폭의 안쪽 테두리 생성" } ] },
            { caption: "편집", items: [
                { icon: "Union",       label: "합집합",       cmd: "union",     tip: "선택 메시 2개+ 합치기", big: true },
                { icon: "Difference",  label: "차집합",       cmd: "boolean",   tip: "불리언 대화상자에서 기준(A)과 빼기(B) 지정" },
                { icon: "Intersect",   label: "교집합",       cmd: "intersect", tip: "선택 메시 2개+ 교집합" },
                { icon: "Slice",       label: "슬라이스",     cmd: "sl",        tip: "현재 단면 평면으로 선택 솔리드를 둘로 분할" },
                { icon: "EdgeChamfer", label: "모서리모따기", cmd: "ec",        tip: "솔리드 볼록 모서리 평면 모따기" },
                { icon: "EdgeFillet",  label: "모서리필렛",   cmd: "ef",        tip: "솔리드 볼록 모서리 둥글리기" } ] },
            { caption: "조립", items: [
                { icon: "Exploded", label: "분해도",  cmd: "exview",        tip: "분해도 막대 켜고 끔 — 끌면 분해 정도", big: true },
                { icon: "Balloon",  label: "풍선",    cmd: "balloon",       tip: "품번 풍선 켜고 끔 — 같은 모양 = 같은 번호, ×수량" },
                { icon: "Bom",      label: "부품표",  cmd: "bom",           tip: "부품표 켜고 끔 — bom csv 로 저장" },
                { icon: "Manual",   label: "설명서",  cmd: "exview manual", tip: "조립 설명서 PDF — 표지(부품표) + 조립 단계마다 한 쪽" } ] } ] }
    ]

    function trigger(item) {
        if (item.act !== undefined) {
            if (item.act === "selectAll")           CadCommands.selectAll()
            else if (item.act === "deleteSelected") CadCommands.deleteSelected()
            else if (item.act === "undo")           CadCommands.undo()
            else if (item.act === "redo")           CadCommands.redo()
            return
        }
        if (item.mode !== undefined) root.gizmoMode = item.mode
        CadCommands.executeCommand(item.cmd)
    }

    // ── 바탕 — 탭 줄은 메뉴바 색, 본문은 창 색. 맨 아래 1px 선. ──
    Rectangle { anchors.fill: parent; color: root.bodyBg }
    Rectangle { width: parent.width; height: root.tabRowHeight; color: root.tabBarBg }
    Rectangle { y: root.height - 1; width: parent.width; height: 1; color: root.borderCol }

    // ── 탭 줄 ──
    Row {
        id: tabRow
        x: 6
        height: root.tabRowHeight
        Repeater {
            model: root.tabs
            delegate: Item {
                required property int index
                required property var modelData
                width: tabText.implicitWidth + root.tabPadX * 2
                height: root.tabRowHeight

                Text {
                    id: tabText
                    anchors.verticalCenter: parent.verticalCenter
                    x: root.tabPadX
                    text: modelData.name
                    font.pixelSize: root.fontSize
                    color: (root.activeTab === index || tabMouse.containsMouse) ? root.textCol : root.dimCol
                }
                // 활성 탭 밑줄 — 엔진과 같은 강조 파랑 2px.
                Rectangle {
                    visible: root.activeTab === index
                    x: 4; y: parent.height - 2
                    width: parent.width - 8; height: 2
                    color: root.accent
                }
                MouseArea {
                    id: tabMouse
                    anchors.fill: parent
                    hoverEnabled: true
                    onClicked: { root.activeTab = index; root.collapsed = false }
                    onDoubleClicked: root.collapsed = !root.collapsed   // 엔진과 같은 접기
                }
            }
        }
    }

    // ── 본문 ──
    Row {
        id: body
        visible: !root.collapsed
        x: 10
        y: root.tabRowHeight + 4
        spacing: 0

        Repeater {
            model: root.tabs[root.activeTab].groups
            delegate: Item {
                id: group
                required property int index
                required property var modelData

                readonly property var bigItems:   modelData.items.filter(function (i) { return i.big === true })
                readonly property var smallItems: modelData.items.filter(function (i) { return i.big !== true })
                readonly property bool isLast: index === root.tabs[root.activeTab].groups.length - 1

                width: gridRow.implicitWidth + (isLast ? 0 : root.groupGap * 2 + 1)
                height: root.gridHeight + root.captionGap + root.captionHeight

                Row {
                    id: gridRow
                    spacing: root.cellGap * 2
                    Repeater { model: group.bigItems;   delegate: BigButton { item: modelData } }
                    // 작은 아이콘은 위에서 아래로 채우고 다음 열 — 엔진 Group::slot() 과 같은 순서.
                    Grid {
                        rows: root.rows
                        flow: Grid.TopToBottom
                        spacing: root.cellGap
                        Repeater { model: group.smallItems; delegate: SmallButton { item: modelData } }
                    }
                }

                // 그룹 이름 띠 — 격자 아래. 큰 버튼의 이름과 겹치지 않도록 띠로 분리(엔진과 같은 이유).
                Rectangle {
                    x: -3
                    y: root.gridHeight + root.captionGap
                    width: gridRow.implicitWidth + 6
                    height: root.captionHeight
                    radius: 3
                    color: Qt.rgba(1, 1, 1, 0.055)
                    Text {
                        anchors.centerIn: parent
                        text: group.modelData.caption
                        font.pixelSize: Math.round(root.fontSize * root.captionScale)
                        color: root.dimCol
                    }
                }

                // 그룹 사이 세로 구분선
                Rectangle {
                    visible: !group.isLast
                    x: gridRow.implicitWidth + root.groupGap
                    y: 2
                    width: 1
                    height: root.gridHeight + root.captionGap + root.captionHeight - 2
                    color: "#4a4a4a"
                }
            }
        }
    }

    // ── 버튼 ──────────────────────────────────────────────────────────
    // 작은 아이콘 칸 — 배경 없음, 호버에 둥근 옅은 판, 켜진 모드는 강조색 판 + 테두리.
    component SmallButton : Item {
        property var item
        width: root.buttonSide
        height: root.buttonSide
        readonly property bool on: item.mode !== undefined && root.gizmoMode === item.mode

        Rectangle {
            anchors.fill: parent
            radius: 4
            color: parent.on ? Qt.rgba(root.accent.r, root.accent.g, root.accent.b, ma.pressed ? 0.55 : 0.32)
                 : ma.pressed   ? Qt.rgba(1, 1, 1, 0.16)
                 : ma.containsMouse ? Qt.rgba(1, 1, 1, 0.09)
                 : "transparent"
            border.width: parent.on ? 1 : 0
            border.color: Qt.rgba(root.accent.r, root.accent.g, root.accent.b, 0.9)
        }
        ToolIcon {
            anchors.centerIn: parent
            iconName: item.icon
            iconColor: root.textCol
            iconScale: 1.0
        }
        MouseArea {
            id: ma
            anchors.fill: parent
            hoverEnabled: true
            onClicked: root.trigger(item)
        }
        ToolTip.visible: ma.containsMouse
        ToolTip.delay: 400
        ToolTip.text: item.label + "  (" + item.cmd + ")\n" + item.tip
    }

    // 큰 버튼 — 그룹의 대표 도구. 3줄 높이를 다 쓰고 큰 아이콘 아래 이름(오피스·오토캐드 리본식).
    component BigButton : Item {
        property var item
        width: Math.max(root.bigMinWidth, bigLabel.implicitWidth + 10)
        height: root.gridHeight
        readonly property bool on: item.mode !== undefined && root.gizmoMode === item.mode

        Rectangle {
            anchors.fill: parent
            radius: 5
            color: parent.on ? Qt.rgba(root.accent.r, root.accent.g, root.accent.b, bma.pressed ? 0.55 : 0.32)
                 : bma.pressed   ? Qt.rgba(1, 1, 1, 0.16)
                 : bma.containsMouse ? Qt.rgba(1, 1, 1, 0.09)
                 : "transparent"
            border.width: parent.on ? 1 : 0
            border.color: Qt.rgba(root.accent.r, root.accent.g, root.accent.b, 0.9)
        }
        ToolIcon {
            anchors.horizontalCenter: parent.horizontalCenter
            y: (parent.height - root.captionHeight - height) / 2
            iconName: item.icon
            iconColor: root.textCol
            iconScale: 1.9
        }
        Text {
            id: bigLabel
            anchors.horizontalCenter: parent.horizontalCenter
            y: parent.height - root.captionHeight - 1
            text: item.label
            font.pixelSize: Math.round(root.fontSize * root.captionScale)
            color: root.textCol
        }
        MouseArea {
            id: bma
            anchors.fill: parent
            hoverEnabled: true
            onClicked: root.trigger(item)
        }
        ToolTip.visible: bma.containsMouse
        ToolTip.delay: 400
        ToolTip.text: item.label + "  (" + item.cmd + ")\n" + item.tip
    }
}
