#pragma once

// CadCommands — QML 툴바 버튼이 호출하는 얇은 명령 브릿지.
// QML 에서 Q_INVOKABLE 메서드로 CAD_Request* / CAD_Undo 등 C API 를 부른다.
// (엔진 상태 변경은 전부 C API 경계를 지나가므로 QML 은 엔진 내부를 전혀 모른다.)

#include "../../api/VulkanCAD_API.h"

#include <QObject>
#include <QtQml/qqmlregistration.h>

class CadCommands : public QObject {
    Q_OBJECT
    QML_ELEMENT
    QML_SINGLETON   // QML 에서 CadCommands.addCube() 처럼 전역 싱글톤으로 접근

public:
    explicit CadCommands(QObject* parent = nullptr) : QObject(parent) {}

    Q_INVOKABLE void addCube()        { CAD_RequestAddCube(); }
    Q_INVOKABLE void selectAll()      { CAD_RequestSelectAll(); }
    Q_INVOKABLE void deleteSelected() { CAD_RequestDeleteSelected(); }
    Q_INVOKABLE void clearAll()       { CAD_RequestClearAll(); }
    Q_INVOKABLE void zoomExtents()    { CAD_RequestZoomExtents(); }
    Q_INVOKABLE void focusSelected()  { CAD_RequestFocusSelected(); }
    Q_INVOKABLE void toggleProjection(){ CAD_RequestToggleProjection(); }
    Q_INVOKABLE void setView(int v)   { CAD_RequestSetView(v); }   // 0정면 1평면 2우측 3Iso
    Q_INVOKABLE void undo()           { CAD_Undo(); }
    Q_INVOKABLE void redo()           { CAD_Redo(); }

    // 스케치 도구
    Q_INVOKABLE void startBox()       { CAD_RequestStartBoxSketch(); }
    Q_INVOKABLE void startLine()      { CAD_RequestStartLineSketch(); }
    Q_INVOKABLE void startRect()      { CAD_RequestStartRectangleSketch(); }
    Q_INVOKABLE void startCircle()    { CAD_RequestStartCircleSketch(); }
    Q_INVOKABLE void startPolyline()  { CAD_RequestStartPolylineSketch(); }
    Q_INVOKABLE void startExtrude()   { CAD_RequestStartExtrude(); }
};
