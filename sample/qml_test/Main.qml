import QtQuick
import QtQuick.Controls
import QtQuick.Layouts
import VulkanCadQml

ApplicationWindow {
    id: win
    width: 1280
    height: 800
    visible: true
    title: "VulkanCAD — Qt6 Cross-platform Host"

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
