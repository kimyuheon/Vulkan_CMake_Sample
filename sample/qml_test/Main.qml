import QtQuick
import QtQuick.Controls
import QtQuick.Layouts
// 네이티브 메뉴 — 별도 최상위 창으로 떠서 3D 네이티브 자식 창에 가리지 않는다.
// (QtQuick.Controls 의 Menu 는 QQuickWindow 안에 그려져 뷰 뒤로 숨는다)
import Qt.labs.platform as Platform
import VulkanCadQml

ApplicationWindow {
    id: win
    width: 1280
    height: 800
    visible: true
    title: "VulkanCAD — Qt6 Cross-platform Host"

    // 어느 기능 패널을 오른쪽에 띄울지. 0 닫힘 1 도면 2 단면.
    //
    // 왜 패널인가: 3D 뷰가 네이티브 자식 창이라 QML Popup/Dialog 를 같은 창에 띄우면 뷰 뒤로 숨는다.
    // 그래서 기능 UI 는 3D 영역 **바깥**에 두고, 메뉴만 별도 최상위 창(Platform.Menu)으로 띄운다.
    property int featurePage: 0

    // 단면 슬라이더 기본값 계산 — 엔진이 주는 sceneBounds 구간에서 가운데/폭.
    function axisMid()  { return (SectionApi.posMin + SectionApi.posMax) / 2 }
    function axisSpan() { return SectionApi.posMax - SectionApi.posMin }

    // 3각법 뷰 중 하나(정면도) — 단면도·상세도는 "정투상 뷰 위"를 가리켜야 한다.
    function baseView() {
        var vs = DrawingApi.views
        for (var i = 0; i < vs.length; ++i)
            if (vs[i].kind === "base") return vs[i]
        return vs.length > 0 ? vs[0] : null
    }

    // 단면도·상세도 입력칸을 정면도가 놓인 자리에서 채운다.
    //
    // 도면 뷰 좌표는 **도면 평면**(월드 XY, mm)이고 모델 좌표가 아니다. 120×80 부품이라고
    // 0~120 을 넣으면 엔진이 "자르는 선이 도면 뷰를 지나지 않습니다" 로 거절한다 —
    // 실제로 정면도는 배율·여백·치수를 반영해 한참 떨어진 자리(예: x 148~300)에 놓인다.
    // 그래서 사람이 숫자를 상상하는 대신 뷰 bounds 에서 끌어온다.
    function seedFromBaseView() {
        var v = baseView()
        if (!v || !v.bounds) return false
        var mn = v.bounds.min, mx = v.bounds.max
        var cx = (mn[0] + mx[0]) / 2, cy = (mn[1] + mx[1]) / 2
        var w = mx[0] - mn[0], h = mx[1] - mn[1]
        // 자르는 선은 뷰를 완전히 가로지르게 — 양쪽으로 조금 넘긴다.
        secAx.value = (mn[0] - w * 0.05).toFixed(1); secAy.value = cy.toFixed(1)
        secBx.value = (mx[0] + w * 0.05).toFixed(1); secBy.value = cy.toFixed(1)
        detCx.value = cx.toFixed(1); detCy.value = cy.toFixed(1)
        detR.value  = (Math.min(w, h) * 0.25).toFixed(1)
        return true
    }

    // 메뉴바는 QML 로 그리고, 펼쳐지는 팝업만 네이티브(Platform.Menu)로 띄운다.
    //
    // 왜 Platform.MenuBar 를 안 쓰나: 네이티브 메뉴바가 있는 플랫폼(macOS 글로벌 메뉴, Windows)
    // 에서만 표시된다. GNOME/Wayland 세션엔 그럴 자리가 없어 아무것도 안 보인다.
    // 왜 QtQuick.Controls 의 Menu 를 안 쓰나: QQuickWindow 안에 그려져 네이티브 3D 자식 창에 가린다.
    // Platform.Menu 는 별도 최상위 창이라 3D 뷰 위에 정상적으로 뜬다.
    menuBar: ToolBar {
        RowLayout {
            spacing: 0
            ToolButton { text: "파일";   onClicked: fileMenu.open(this) }
            ToolButton { text: "편집";   onClicked: editMenu.open(this) }
            ToolButton { text: "생성";   onClicked: createMenu.open(this) }
            ToolButton { text: "뷰";     onClicked: viewMenu.open(this) }
            ToolButton { text: "도면";   onClicked: drawingMenu.open(this) }
            ToolButton { text: "단면";   onClicked: sectionMenu.open(this) }
            Item { Layout.fillWidth: true }
        }
    }

    // 풀다운 내용 — 툴바와 같은 CadCommands 를 부르므로 명령 경로는 하나뿐이다.
    // 단축키는 MenuItem 에 직접 붙고 메뉴가 닫혀 있어도 동작한다.
    Platform.Menu {
        id: fileMenu
        title: "파일"
        Platform.MenuItem { text: "새 문서"; shortcut: StandardKey.New;  onTriggered: CadCommands.newDocument() }
        Platform.MenuSeparator {}
        Platform.MenuItem { text: "열기…";   shortcut: StandardKey.Open; onTriggered: CadCommands.openDocument() }
        Platform.MenuItem { text: "가져오기…(현재 문서에 추가)";        onTriggered: CadCommands.importFile() }
        Platform.MenuSeparator {}
        Platform.MenuItem { text: "다른 이름으로 저장…"; shortcut: StandardKey.SaveAs; onTriggered: CadCommands.saveAs(false) }
        Platform.MenuItem { text: "선택만 내보내기…";                    onTriggered: CadCommands.saveAs(true) }
        Platform.MenuSeparator {}
        Platform.MenuItem { text: "전체 삭제"; onTriggered: CadCommands.clearAll() }
        Platform.MenuSeparator {}
        Platform.MenuItem { text: "종료"; shortcut: "Ctrl+Q"; onTriggered: Qt.quit() }
    }

    Platform.Menu {
        id: editMenu
        title: "편집"
        Platform.MenuItem { text: "실행 취소"; shortcut: StandardKey.Undo;      onTriggered: CadCommands.undo() }
        Platform.MenuItem { text: "다시 실행"; shortcut: StandardKey.Redo;      onTriggered: CadCommands.redo() }
        Platform.MenuSeparator {}
        Platform.MenuItem { text: "전체 선택"; shortcut: StandardKey.SelectAll; onTriggered: CadCommands.selectAll() }
        Platform.MenuItem { text: "선택 삭제"; shortcut: StandardKey.Delete;    onTriggered: CadCommands.deleteSelected() }
    }

    Platform.Menu {
        id: createMenu
        title: "생성"
        Platform.MenuItem { text: "큐브";   onTriggered: CadCommands.addCube() }
        Platform.MenuSeparator {}
        Platform.MenuItem { text: "선";     onTriggered: CadCommands.startLine() }
        Platform.MenuItem { text: "사각형"; onTriggered: CadCommands.startRect() }
        Platform.MenuItem { text: "원";     onTriggered: CadCommands.startCircle() }
        Platform.MenuItem { text: "폴리선"; onTriggered: CadCommands.startPolyline() }
        Platform.MenuItem { text: "박스";   onTriggered: CadCommands.startBox() }
        Platform.MenuSeparator {}
        Platform.MenuItem { text: "돌출";   onTriggered: CadCommands.startExtrude() }
    }

    Platform.Menu {
        id: viewMenu
        title: "뷰"
        Platform.MenuItem { text: "정면"; shortcut: "Ctrl+1"; onTriggered: CadCommands.setView(0) }
        Platform.MenuItem { text: "평면"; shortcut: "Ctrl+2"; onTriggered: CadCommands.setView(1) }
        Platform.MenuItem { text: "우측"; shortcut: "Ctrl+3"; onTriggered: CadCommands.setView(2) }
        Platform.MenuItem { text: "Iso";  shortcut: "Ctrl+4"; onTriggered: CadCommands.setView(3) }
        Platform.MenuSeparator {}
        Platform.MenuItem { text: "투영 전환";                    onTriggered: CadCommands.toggleProjection() }
        Platform.MenuItem { text: "전체 보기"; shortcut: "Ctrl+0"; onTriggered: CadCommands.zoomExtents() }
        Platform.MenuItem { text: "선택 확대"; shortcut: "Ctrl+F"; onTriggered: CadCommands.focusSelected() }
    }

    // ── 기능 메뉴 ──
    // 좌표를 받아야 하는 항목(단면도 선, 상세도 원)은 메뉴에서 바로 실행하지 않고 패널을 연다.
    // 값 없이 되는 것만 메뉴에서 바로 실행한다.
    Platform.Menu {
        id: drawingMenu
        title: "도면"
        Platform.MenuItem { text: "도면 패널 열기";   onTriggered: win.featurePage = 1 }
        Platform.MenuSeparator {}
        Platform.MenuItem { text: "예제 부품 만들기"; onTriggered: DemoScene.createSamplePart() }
        Platform.MenuItem { text: "도면 뷰 만들기 (정투상 + 등각)"
                            onTriggered: { win.featurePage = 1
                                           if (DrawingApi.createStandardViews(0) > 0) win.seedFromBaseView() } }
        Platform.MenuSeparator {}
        Platform.MenuItem { text: "단면도 A-A…"; onTriggered: win.featurePage = 1 }
        Platform.MenuItem { text: "상세도…";     onTriggered: win.featurePage = 1 }
        Platform.MenuSeparator {}
        Platform.MenuItem { text: "도면 뷰 모두 지우기"; onTriggered: DrawingApi.clearViews() }
    }

    Platform.Menu {
        id: sectionMenu
        title: "단면"
        Platform.MenuItem { text: "단면 패널 열기"; onTriggered: win.featurePage = 2 }
        Platform.MenuSeparator {}
        // 모드만 바꾸고 축·위치는 지금 값을 그대로 쓴다(thickness 0 = 엔진이 지금 값 유지).
        Platform.MenuItem { text: "끄기"
                            onTriggered: SectionApi.setSection(0, SectionApi.axis, SectionApi.pos, SectionApi.flip, 0) }
        Platform.MenuItem { text: "평면"
                            onTriggered: { win.featurePage = 2; SectionApi.setSection(1, SectionApi.axis, win.axisMid(), false, 0) } }
        Platform.MenuItem { text: "슬라이스"
                            onTriggered: { win.featurePage = 2; SectionApi.setSection(2, SectionApi.axis, win.axisMid(), false, win.axisSpan() * 0.1) } }
        Platform.MenuItem { text: "상자"
                            onTriggered: { win.featurePage = 2; SectionApi.setSection(3, SectionApi.axis, SectionApi.pos, SectionApi.flip, 0) } }
        Platform.MenuSeparator {}
        Platform.MenuItem { text: "지금 단면을 2D 로 뽑기"; onTriggered: SectionApi.extractTo2D(false) }
    }

    // 상단 리본 — 엔진 ImGui 리본(ui/lot_ribbon.cpp)과 같은 탭·그룹·아이콘 구성.
    // 3D 뷰가 네이티브 자식 창이라 리본은 그 바깥(header)에 있어야 가려지지 않는다.
    header: Ribbon {}

    RowLayout {
        anchors.fill: parent
        spacing: 0

        // 좌측 속성/도구 패널 (QML) — 3D 영역 바깥이라 오버레이 제약과 무관.
        Rectangle {
            Layout.preferredWidth: 200
            Layout.fillHeight: true
            color: "#2b2b2b"

            ColumnLayout {
                anchors.fill: parent
                anchors.margins: 12
                spacing: 8

                Label { text: "뷰"; color: "#ddd"; font.bold: true }
                Button { Layout.fillWidth: true; text: "정면"; onClicked: CadCommands.setView(0) }
                Button { Layout.fillWidth: true; text: "평면"; onClicked: CadCommands.setView(1) }
                Button { Layout.fillWidth: true; text: "우측"; onClicked: CadCommands.setView(2) }
                Button { Layout.fillWidth: true; text: "Iso";  onClicked: CadCommands.setView(3) }
                Button { Layout.fillWidth: true; text: "투영 전환"; onClicked: CadCommands.toggleProjection() }

                Item { Layout.fillHeight: true }   // 아래 밀어내기

                Button { Layout.fillWidth: true; text: "전체 삭제"; onClicked: CadCommands.clearAll() }
            }
        }

        // 3D 뷰포트 — 엔진 Vulkan 렌더가 이 Item 위치에 네이티브 창으로 얹힘.
        VulkanViewport {
            Layout.fillWidth: true
            Layout.fillHeight: true
        }

        // ── 기능 패널 — 3D 영역 바깥(오른쪽). featurePage 가 0 이면 접힌다. ──
        Rectangle {
            id: featurePanel
            visible: win.featurePage !== 0
            Layout.preferredWidth: 330
            Layout.fillHeight: true
            color: "#232323"

            // 엔진 상태는 밖에서도 바뀐다 — 3D 뷰에서 section 패널을 만지거나 모델을 고치면
            // 도면 뷰가 따라 움직인다. 호스트가 그걸 통보받을 길이 없어 패널이 보이는 동안만
            // 낮은 빈도로 읽어 맞춘다(매 프레임은 필요 없다).
            Timer {
                interval: 200; repeat: true; running: featurePanel.visible
                onTriggered: win.featurePage === 1 ? DrawingApi.refresh() : SectionApi.refresh()
            }

            ColumnLayout {
                anchors.fill: parent
                anchors.margins: 12
                spacing: 8

                RowLayout {
                    Layout.fillWidth: true
                    Label {
                        text: win.featurePage === 1 ? "도면 뷰" : "실시간 단면"
                        color: "#ddd"; font.bold: true
                    }
                    Item { Layout.fillWidth: true }
                    ToolButton { text: "✕"; onClicked: win.featurePage = 0 }
                }

                StackLayout {
                    Layout.fillWidth: true
                    Layout.fillHeight: true
                    currentIndex: win.featurePage - 1   // 1 도면 → 0, 2 단면 → 1

                    // ══ 도면 ══
                    ColumnLayout {
                        spacing: 8

                        Button {
                            Layout.fillWidth: true
                            text: "예제 부품 만들기"
                            // 볼 만한 솔리드가 없으면 도면 뷰가 빈 종이로 나온다 — 시연용 부품을 깔아 준다.
                            onClicked: DemoScene.createSamplePart()
                        }

                        RowLayout {
                            Layout.fillWidth: true
                            Label { text: "배율"; color: "#bbb" }
                            TextField {
                                id: scaleField
                                Layout.fillWidth: true
                                text: "0"
                                selectByMouse: true
                                validator: DoubleValidator { bottom: 0 }
                            }
                            Button {
                                text: "뷰 만들기"
                                // 0 이면 엔진이 모델 크기에서 배율을 고른다.
                                // 만들어진 뷰 자리에서 아래 입력칸을 채운다(좌표계가 모델과 다르다).
                                onClicked: if (DrawingApi.createStandardViews(Number(scaleField.text)) > 0)
                                               win.seedFromBaseView()
                            }
                        }

                        GroupBox {
                            Layout.fillWidth: true
                            title: "단면도 A-A"
                            ColumnLayout {
                                anchors.fill: parent
                                spacing: 4
                                // 좌표는 **도면 평면**(월드 XY, mm)이다 — 모델 좌표가 아니다.
                                // 뷰를 만들면 자동으로 채워진다. 직접 넣을 땐 뷰 목록의 자리를 보면 된다.
                                Label {
                                    Layout.fillWidth: true
                                    text: "좌표 = 도면 평면 mm (모델 좌표 아님)"
                                    color: "#8a8a8a"; font.pixelSize: 11; wrapMode: Text.Wrap
                                }
                                RowLayout {
                                    Layout.fillWidth: true
                                    NumField { id: secAx; label: "ax"; value: "0" }
                                    NumField { id: secAy; label: "ay"; value: "0" }
                                }
                                RowLayout {
                                    Layout.fillWidth: true
                                    NumField { id: secBx; label: "bx"; value: "0" }
                                    NumField { id: secBy; label: "by"; value: "0" }
                                }
                                RowLayout {
                                    Layout.fillWidth: true
                                    Button {
                                        Layout.fillWidth: true
                                        enabled: DrawingApi.views.length > 0
                                        text: "만들기"
                                        onClicked: DrawingApi.createSectionView(
                                            Number(secAx.value), Number(secAy.value),
                                            Number(secBx.value), Number(secBy.value))
                                    }
                                    Button {
                                        Layout.fillWidth: true
                                        enabled: DrawingApi.views.length > 0
                                        text: "정면도 기준 채우기"
                                        onClicked: win.seedFromBaseView()
                                    }
                                }
                                Button {
                                    Layout.fillWidth: true
                                    text: "화면에서 고르기 (3D 뷰에서 클릭)"
                                    // 엔진 도구를 띄워 3D 뷰에서 직접 클릭하게 한다 — 숫자 입력의 대안.
                                    onClicked: CadCommands.executeCommand("viewsection")
                                }
                            }
                        }

                        GroupBox {
                            Layout.fillWidth: true
                            title: "상세도"
                            ColumnLayout {
                                anchors.fill: parent
                                spacing: 4
                                RowLayout {
                                    Layout.fillWidth: true
                                    NumField { id: detCx; label: "cx"; value: "0" }
                                    NumField { id: detCy; label: "cy"; value: "0" }
                                }
                                RowLayout {
                                    Layout.fillWidth: true
                                    NumField { id: detR; label: "r";  value: "0" }
                                    NumField { id: detF; label: "배"; value: "2" }
                                }
                                Button {
                                    Layout.fillWidth: true
                                    enabled: DrawingApi.views.length > 0
                                    text: "만들기"
                                    // 중심이 정투상 뷰 **위**에 있어야 한다 — 채우기 버튼이 그 자리를 넣어 준다.
                                    onClicked: DrawingApi.createDetailView(
                                        Number(detCx.value), Number(detCy.value),
                                        Number(detR.value), Number(detF.value))
                                }
                            }
                        }

                        Label {
                            text: "뷰 " + DrawingApi.views.length + "개"
                            color: "#bbb"
                        }

                        // 뷰 목록 — 고르면 아래 옮기기가 그 뷰에 적용된다.
                        ListView {
                            id: viewList
                            Layout.fillWidth: true
                            Layout.fillHeight: true
                            Layout.minimumHeight: 80
                            clip: true
                            model: DrawingApi.views
                            ScrollBar.vertical: ScrollBar {}
                            delegate: ItemDelegate {
                                required property int index
                                required property var modelData
                                width: viewList.width
                                highlighted: viewList.currentIndex === index
                                onClicked: viewList.currentIndex = index
                                text: "#" + modelData.id + "  " + modelData.name
                                      + "  (" + modelData.kind + ")"
                            }
                        }

                        RowLayout {
                            Layout.fillWidth: true
                            enabled: viewList.currentIndex >= 0 && DrawingApi.views.length > 0
                            NumField { id: movDx; label: "dx"; value: "0" }
                            NumField { id: movDy; label: "dy"; value: "0" }
                            Button {
                                text: "옮기기"
                                // 엔진이 3각법 정렬을 유지해 주므로 호스트는 증분만 넘긴다.
                                onClicked: DrawingApi.moveView(
                                    DrawingApi.views[viewList.currentIndex].id,
                                    Number(movDx.value), Number(movDy.value))
                            }
                        }

                        Button {
                            Layout.fillWidth: true
                            text: "뷰 모두 지우기"
                            onClicked: DrawingApi.clearViews()
                        }

                        Label {
                            Layout.fillWidth: true
                            visible: DrawingApi.lastError !== ""
                            text: DrawingApi.lastError
                            color: "#ff8a80"
                            wrapMode: Text.Wrap
                        }
                    }

                    // ══ 단면 ══
                    ColumnLayout {
                        spacing: 8

                        RowLayout {
                            Layout.fillWidth: true
                            Label { text: "모드"; color: "#bbb" }
                            ComboBox {
                                Layout.fillWidth: true
                                model: ["끄기", "평면", "슬라이스", "상자"]
                                currentIndex: SectionApi.mode
                                // activated = 사용자가 고른 경우만. currentIndex 바인딩과 안 싸운다.
                                onActivated: SectionApi.setSection(
                                    index, SectionApi.axis, SectionApi.pos, SectionApi.flip, 0)
                            }
                        }

                        RowLayout {
                            Layout.fillWidth: true
                            Label { text: "축"; color: "#bbb" }
                            ComboBox {
                                Layout.fillWidth: true
                                model: ["X", "Y", "Z"]
                                currentIndex: SectionApi.axis
                                onActivated: SectionApi.setSection(
                                    Math.max(1, SectionApi.mode), index, win.axisMid(), SectionApi.flip, 0)
                            }
                        }

                        Label {
                            text: "위치 " + SectionApi.pos.toFixed(1)
                                  + "  (" + SectionApi.posMin.toFixed(0) + " ~ " + SectionApi.posMax.toFixed(0) + ")"
                            color: "#bbb"
                        }
                        Slider {
                            id: posSlider
                            Layout.fillWidth: true
                            from: SectionApi.posMin
                            to: SectionApi.posMax
                            value: SectionApi.pos
                            // 끄기 상태에서 슬라이더를 만지면 평면 단면으로 켠다.
                            onMoved: SectionApi.setSection(
                                Math.max(1, SectionApi.mode), SectionApi.axis, value, SectionApi.flip, 0)
                            // 드래그는 value 바인딩을 끊는다. 엔진 값이 밖에서 바뀌면 다시 붙인다.
                            Connections {
                                target: SectionApi
                                function onStateChanged() {
                                    if (!posSlider.pressed) posSlider.value = SectionApi.pos
                                }
                            }
                        }

                        CheckBox {
                            text: "자르는 쪽 뒤집기"
                            checked: SectionApi.flip
                            onToggled: SectionApi.setSection(
                                Math.max(1, SectionApi.mode), SectionApi.axis, SectionApi.pos, checked, 0)
                        }

                        // 두께는 슬라이스(모드 2)에만 의미가 있다.
                        Label {
                            visible: SectionApi.mode === 2
                            text: "두께 " + SectionApi.thickness.toFixed(1)
                            color: "#bbb"
                        }
                        Slider {
                            id: thickSlider
                            visible: SectionApi.mode === 2
                            Layout.fillWidth: true
                            from: Math.max(0.1, win.axisSpan() * 0.001)
                            to: Math.max(1, win.axisSpan())
                            value: SectionApi.thickness
                            onMoved: SectionApi.setSection(
                                2, SectionApi.axis, SectionApi.pos, SectionApi.flip, value)
                            Connections {
                                target: SectionApi
                                function onStateChanged() {
                                    if (!thickSlider.pressed) thickSlider.value = SectionApi.thickness
                                }
                            }
                        }

                        // 상자 모드는 값이 6개라 패널을 더 키우지 않고 "장면 전체에서 조금 줄인 상자"를
                        // 기본으로 깔아 준다. 세부 조정은 엔진 section 패널 쪽이 낫다.
                        Button {
                            visible: SectionApi.mode === 3
                            Layout.fillWidth: true
                            text: "상자를 장면 80% 로 맞추기"
                            onClicked: {
                                var c = win.axisMid(), s = win.axisSpan() * 0.4
                                SectionApi.setBox(c - s, c - s, c - s, c + s, c + s, c + s)
                            }
                        }

                        CheckBox {
                            id: arrowsBox
                            text: "단면 화살표 표시"
                            checked: true
                            onToggled: SectionApi.setOptions(checked, selectedOnlyBox.checked)
                        }
                        CheckBox {
                            id: selectedOnlyBox
                            text: "선택한 것만 자르기"
                            onToggled: SectionApi.setOptions(arrowsBox.checked, checked)
                        }

                        Button {
                            Layout.fillWidth: true
                            enabled: SectionApi.mode === 1
                            text: "지금 단면을 2D 로 뽑기"
                            // 평면 단면만 뽑을 수 있다. 결과는 모델 오른쪽 XY 평면의 폴리선(undo 1).
                            onClicked: SectionApi.extractTo2D(false)
                        }

                        Item { Layout.fillHeight: true }

                        Label {
                            Layout.fillWidth: true
                            text: "상태: " + (SectionApi.modeName !== "" ? SectionApi.modeName : "-")
                            color: "#bbb"
                        }
                        Label {
                            Layout.fillWidth: true
                            visible: SectionApi.lastError !== ""
                            text: SectionApi.lastError
                            color: "#ff8a80"
                            wrapMode: Text.Wrap
                        }
                    }
                }
            }
        }
    }

    // 숫자 한 칸 — 기능 패널에 좌표 입력이 많아 라벨+입력을 묶어 둔다.
    component NumField : RowLayout {
        property alias label: numLabel.text
        property alias value: numInput.text
        spacing: 4
        Label { id: numLabel; color: "#bbb" }
        TextField {
            id: numInput
            Layout.fillWidth: true
            Layout.minimumWidth: 48
            selectByMouse: true
            validator: DoubleValidator {}
        }
    }
}
