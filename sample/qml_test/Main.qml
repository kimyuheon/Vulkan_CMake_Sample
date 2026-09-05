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

    // 상단 툴바 — QML 로 그리고, 클릭 시 CadCommands(C API 브릿지) 호출.
    header: ToolBar {
        RowLayout {
            anchors.fill: parent
            spacing: 4

            ToolButton { text: "큐브";   onClicked: CadCommands.addCube() }
            ToolButton { text: "선";     onClicked: CadCommands.startLine() }
            ToolButton { text: "사각형"; onClicked: CadCommands.startRect() }
            ToolButton { text: "원";     onClicked: CadCommands.startCircle() }
            ToolButton { text: "돌출";   onClicked: CadCommands.startExtrude() }

            ToolSeparator {}

            ToolButton { text: "전체선택"; onClicked: CadCommands.selectAll() }
            ToolButton { text: "삭제";     onClicked: CadCommands.deleteSelected() }
            ToolButton { text: "줌";       onClicked: CadCommands.zoomExtents() }
            ToolButton { text: "Iso";      onClicked: CadCommands.setView(3) }

            ToolSeparator {}

            ToolButton { text: "Undo"; onClicked: CadCommands.undo() }
            ToolButton { text: "Redo"; onClicked: CadCommands.redo() }

            Item { Layout.fillWidth: true }   // 우측 여백
        }
    }

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
    }
}
