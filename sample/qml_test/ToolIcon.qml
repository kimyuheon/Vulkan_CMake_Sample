import QtQuick
import "ToolIcons.js" as Icons

// 선 아이콘 한 개 — 엔진 ui/lot_tool_icons.cpp 와 같은 도형을 Canvas 로 그린다.
// 색은 테마 글자색을 받아 쓰고(이미지가 아니라) 배율만 바꿔 작은 칸·큰 버튼에 같이 쓴다.
Canvas {
    id: canvas

    property string iconName: ""
    property color  iconColor: "#e6e6e6"
    property real   iconScale: 1.0

    // 아이콘은 설계 반경 ≈7px — 배율을 곱한 만큼 자리를 잡아 준다.
    implicitWidth:  Math.ceil(16 * iconScale)
    implicitHeight: Math.ceil(16 * iconScale)

    onPaint: {
        var ctx = getContext("2d")
        ctx.reset()
        Icons.drawIcon(ctx, iconName, width / 2, height / 2, iconColor.toString(), iconScale)
    }

    onIconNameChanged:  requestPaint()
    onIconColorChanged: requestPaint()
    onIconScaleChanged: requestPaint()
}
