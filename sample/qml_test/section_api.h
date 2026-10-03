#pragma once

// SectionApi — 실시간 단면(셰이더가 잘라 보여 주는 보기 상태) C API 브릿지.
//
// 보기 상태라 undo 가 없고, 엔진의 section 패널과 **같은 상태**다 — 3D 뷰에서 패널을 만지면
// 여기 값도 따라와야 하므로 QML 이 패널을 보여 주는 동안 Timer 로 refresh() 한다.
//
// 슬라이더 범위는 직접 정하지 않고 엔진이 주는 sceneBounds 를 쓴다. 장면이 바뀌면 범위도 바뀐다.

#include "cad_json.h"

#include <QJsonArray>
#include <QJsonObject>
#include <QObject>
#include <QString>
#include <QtQml/qqmlregistration.h>

class SectionApi : public QObject {
    Q_OBJECT
    QML_ELEMENT
    QML_SINGLETON

    Q_PROPERTY(int     mode      READ mode      NOTIFY stateChanged)   // 0 끄기 1 평면 2 슬라이스 3 상자
    Q_PROPERTY(QString modeName  READ modeName  NOTIFY stateChanged)
    Q_PROPERTY(int     axis      READ axis      NOTIFY stateChanged)   // 0 X 1 Y 2 Z
    Q_PROPERTY(double  pos       READ pos       NOTIFY stateChanged)
    Q_PROPERTY(bool    flip      READ flip      NOTIFY stateChanged)
    Q_PROPERTY(double  thickness READ thickness NOTIFY stateChanged)
    // 지금 축에 해당하는 sceneBounds 구간 — QML Slider 의 from/to 로 그대로 쓴다.
    Q_PROPERTY(double  posMin    READ posMin    NOTIFY stateChanged)
    Q_PROPERTY(double  posMax    READ posMax    NOTIFY stateChanged)
    Q_PROPERTY(QString lastError READ lastError NOTIFY lastErrorChanged)

public:
    explicit SectionApi(QObject* parent = nullptr) : QObject(parent) { refresh(); }

    int     mode() const      { return mode_; }
    QString modeName() const  { return modeName_; }
    int     axis() const      { return axis_; }
    double  pos() const       { return pos_; }
    bool    flip() const      { return flip_; }
    double  thickness() const { return thickness_; }
    double  posMin() const    { return posMin_; }
    double  posMax() const    { return posMax_; }
    QString lastError() const { return lastError_; }

    // thickness 0 이하를 넘기면 엔진이 지금 값을 유지한다(슬라이스가 아닌 모드에서 쓰는 길).
    Q_INVOKABLE bool setSection(int mode, int axis, double pos, bool flip, double thickness) {
        const bool ok = CAD_SetSection(mode, axis, float(pos), flip, float(thickness));
        finish(!ok);
        return ok;
    }

    // 상자 값만 바꾼다 — 켜는 건 setSection(3, …).
    Q_INVOKABLE bool setBox(double minX, double minY, double minZ,
                            double maxX, double maxY, double maxZ) {
        const bool ok = CAD_SetSectionBox(float(minX), float(minY), float(minZ),
                                          float(maxX), float(maxY), float(maxZ));
        finish(!ok);
        return ok;
    }

    Q_INVOKABLE void setOptions(bool showArrows, bool selectedOnly) {
        CAD_SetSectionOptions(showArrows, selectedOnly);
        finish(false);
    }

    // 지금 평면 단면을 편집 가능한 2D 폴리선으로 뽑는다(모델 오른쪽 XY 평면). undo 1.
    Q_INVOKABLE int extractTo2D(bool includeBehind) {
        const uint32_t n = CAD_ExtractSectionTo2D(includeBehind);
        finish(n == 0);
        return int(n);
    }

    Q_INVOKABLE void refresh() {
        const QJsonObject s = cadjson::readObject(&CAD_GetSectionStateJson);
        if (s.isEmpty()) return;

        const int     mode      = s.value("mode").toInt();
        const QString modeName  = s.value("modeName").toString();
        const int     axis      = s.value("axis").toInt();
        const double  pos       = s.value("pos").toDouble();
        const bool    flip      = s.value("flip").toBool();
        const double  thickness = s.value("thickness").toDouble();

        // sceneBounds.min/max 는 [x,y,z] 배열 — 지금 축 성분만 꺼내 슬라이더 범위로 쓴다.
        const QJsonObject bounds = s.value("sceneBounds").toObject();
        const QJsonArray  bmin   = bounds.value("min").toArray();
        const QJsonArray  bmax   = bounds.value("max").toArray();
        double posMin = posMin_, posMax = posMax_;
        if (axis >= 0 && axis < bmin.size() && axis < bmax.size()) {
            posMin = bmin.at(axis).toDouble();
            posMax = bmax.at(axis).toDouble();
            if (posMax <= posMin) posMax = posMin + 1.0;   // 빈 장면이면 폭 0 — Slider 가 싫어한다
        }

        if (mode == mode_ && modeName == modeName_ && axis == axis_ && pos == pos_
            && flip == flip_ && thickness == thickness_ && posMin == posMin_ && posMax == posMax_)
            return;

        mode_ = mode; modeName_ = modeName; axis_ = axis; pos_ = pos;
        flip_ = flip; thickness_ = thickness; posMin_ = posMin; posMax_ = posMax;
        emit stateChanged();
    }

signals:
    void stateChanged();
    void lastErrorChanged();

private:
    void finish(bool failed) {
        const QString err = failed ? cadjson::lastError() : QString();
        if (err != lastError_) {
            lastError_ = err;
            emit lastErrorChanged();
        }
        refresh();
    }

    int     mode_{0};
    QString modeName_;
    int     axis_{0};
    double  pos_{0.0};
    bool    flip_{false};
    double  thickness_{0.0};
    double  posMin_{0.0};
    double  posMax_{1.0};
    QString lastError_;
};
