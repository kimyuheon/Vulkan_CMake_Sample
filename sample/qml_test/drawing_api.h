#pragma once

// DrawingApi — 도면 뷰(3D → 2D, 솔리드웍스 Drawing 식) C API 브릿지.
//
// 도면 뷰는 장면 객체가 아니다(CAD_GetObjectIds 에 안 나온다). 좌표계는 도면 평면 = 월드 XY, 단위 mm.
// 3D 를 고치면 뷰가 따라온다(연관). 만들기·지우기·옮기기는 undo 1 단계.
//
// QML 쪽은 views(목록)만 보고 그린다. 엔진에서 뷰가 바뀌어도 호스트가 알 길이 없으므로
// 패널이 보이는 동안 QML Timer 가 refresh() 를 낮은 빈도로 불러 맞춘다(매 프레임은 필요 없다).

#include "cad_json.h"

#include <QObject>
#include <QVariantList>
#include <QtQml/qqmlregistration.h>

class DrawingApi : public QObject {
    Q_OBJECT
    QML_ELEMENT
    QML_SINGLETON

    Q_PROPERTY(QVariantList views READ views NOTIFY viewsChanged)
    Q_PROPERTY(QString lastError READ lastError NOTIFY lastErrorChanged)

public:
    explicit DrawingApi(QObject* parent = nullptr) : QObject(parent) {}

    QVariantList views() const { return views_; }
    QString lastError() const { return lastError_; }

    // 정면·평면·우측면(3각법) + 등각. scale <= 0 이면 엔진이 모델 크기에서 고른다.
    // 대상은 "보이는 솔리드 전부"(count 0) — 선택만 넘기는 변형은 QML 에 아직 필요 없다.
    Q_INVOKABLE int createStandardViews(double scale) {
        const uint32_t n = CAD_CreateDrawingViews(nullptr, 0, float(scale));
        finish(n == 0);
        return int(n);
    }

    // 단면도 A-A — 정투상 뷰를 가로지르는 선(a → b), 보는 쪽 = 선의 왼쪽. 반환 = 뷰 id(0 실패).
    Q_INVOKABLE int createSectionView(double ax, double ay, double bx, double by) {
        const uint32_t id = CAD_CreateSectionView(float(ax), float(ay), float(bx), float(by));
        finish(id == 0);
        return int(id);
    }

    // 상세도 — 정투상 뷰 위 원(중심·반지름)을 factor 배로. factor 0 이면 엔진 기본값 2배.
    Q_INVOKABLE int createDetailView(double cx, double cy, double radius, double factor) {
        const uint32_t id = CAD_CreateDetailView(float(cx), float(cy), float(radius), float(factor));
        finish(id == 0);
        return int(id);
    }

    // 뷰 옮기기 — 엔진이 3각법 정렬을 유지해 준다(평면도는 위아래로만 등).
    Q_INVOKABLE bool moveView(int viewId, double dx, double dy) {
        const bool ok = CAD_MoveDrawingView(uint32_t(viewId), float(dx), float(dy));
        finish(!ok);
        return ok;
    }

    Q_INVOKABLE void clearViews() {
        CAD_ClearDrawingViews();
        finish(false);
    }

    // 엔진에서 목록을 다시 읽는다. 내용이 같으면 신호를 내지 않아 QML 재바인딩이 헛돌지 않는다.
    Q_INVOKABLE void refresh() {
        const QVariantList next = cadjson::readArray(&CAD_GetDrawingViewsJson);
        if (next == views_) return;
        views_ = next;
        emit viewsChanged();
    }

signals:
    void viewsChanged();
    void lastErrorChanged();

private:
    // 호출 뒤 공통 마무리 — 실패면 사유를 담고, 목록은 어느 쪽이든 다시 읽는다.
    void finish(bool failed) {
        const QString err = failed ? cadjson::lastError() : QString();
        if (err != lastError_) {
            lastError_ = err;
            emit lastErrorChanged();
        }
        refresh();
    }

    QVariantList views_;
    QString      lastError_;
};
