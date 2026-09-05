#pragma once

// CadCommands — QML 툴바 버튼이 호출하는 얇은 명령 브릿지.
// QML 에서 Q_INVOKABLE 메서드로 CAD_Request* / CAD_Undo 등 C API 를 부른다.
// (엔진 상태 변경은 전부 C API 경계를 지나가므로 QML 은 엔진 내부를 전혀 모른다.)

#include "../../sdk/include/VulkanCAD_API.h"

#include <QObject>
#include <QByteArray>
#include <QFileDialog>
#include <QFileInfo>
#include <QString>
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

    // ── 파일 ──
    // QtQuick.Dialogs(qml6-module-qtquick-dialogs) 는 배포처에 없을 수 있어 QFileDialog 를 쓴다.
    // qt6-base 에 이미 들어 있으므로 런타임 패키지가 늘지 않고, 별도 최상위 창이라
    // 네이티브 3D 자식 창에 가리지도 않는다.
    // 확장자로 포맷이 갈리는 건 엔진 몫이라 호스트는 경로만 넘긴다.
    Q_INVOKABLE void newDocument() { CAD_NewDocument("제목 없음"); }

    Q_INVOKABLE bool openDocument() {   // 새 탭으로 열기
        const QString p = QFileDialog::getOpenFileName(nullptr, "열기", QString(), kOpenFilter);
        return !p.isEmpty() && CAD_OpenDocument(p.toUtf8().constData()) >= 0;
    }

    Q_INVOKABLE bool importFile() {     // 현재 문서에 추가
        const QString p = QFileDialog::getOpenFileName(nullptr, "가져오기", QString(), kOpenFilter);
        return !p.isEmpty() && CAD_OpenFile(p.toUtf8().constData());
    }

    Q_INVOKABLE bool saveAs(bool selectedOnly = false) {
        QString p = QFileDialog::getSaveFileName(
            nullptr, selectedOnly ? "선택만 내보내기" : "다른 이름으로 저장", QString(), kSaveFilter);
        if (p.isEmpty()) return false;
        // 확장자를 안 붙이면 엔진이 포맷을 못 고른다 — 기본값으로 네이티브 형식을 채운다.
        if (QFileInfo(p).suffix().isEmpty()) p += ".lot";
        return CAD_SaveAs(p.toUtf8().constData(), selectedOnly);
    }

    // 실패 사유 표시용 — CAD_SaveAs/CAD_OpenFile 이 false 를 준 뒤 읽는다.
    Q_INVOKABLE QString statusMessage() const {
        char buf[512] = {0};
        const int n = CAD_GetStatusMessage(buf, int(sizeof(buf)));
        return n > 0 ? QString::fromUtf8(buf, n) : QString();
    }

private:
    static constexpr const char* kOpenFilter =
        "모든 지원 형식 (*.lot *.obj *.stl *.dxf *.ply *.gltf *.glb);;"
        "프로젝트 (*.lot);;메시 (*.obj *.stl *.gltf *.glb);;도면 (*.dxf);;점군 (*.ply);;"
        "모든 파일 (*)";
    static constexpr const char* kSaveFilter =
        "프로젝트 (*.lot);;OBJ (*.obj);;STL (*.stl);;DXF (*.dxf);;glTF (*.glb *.gltf);;"
        "모든 파일 (*)";
};
