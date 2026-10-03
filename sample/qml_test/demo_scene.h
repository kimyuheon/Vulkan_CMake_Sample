#pragma once

// DemoScene — 기능 시연에 쓸 장면을 C API 만으로 만든다.
//
// 도면 뷰·단면을 눌러 보려면 "볼 만한 솔리드"가 있어야 하는데, 사용자가 매번 손으로 그리게 하면
// 기능 확인이 아니라 모델링 연습이 된다. 엔진 도면 뷰 프로브(tests/drawing_view_probe.cpp)와
// 같은 부품을 버튼 하나로 만들어 둔다 — 그 부품으로 정투상·단면 A-A·상세도가 정상임이 확인돼 있다.
//
// 좌표 단위 mm. 판 120×80×10, 모서리 구멍 Ø10 ×4, 가운데 보스 Ø40×25, 그 가운데 관통 Ø20.

#include "cad_json.h"

#include <QObject>
#include <QString>
#include <QtQml/qqmlregistration.h>

class DemoScene : public QObject {
    Q_OBJECT
    QML_ELEMENT
    QML_SINGLETON

    Q_PROPERTY(QString lastError READ lastError NOTIFY lastErrorChanged)

public:
    explicit DemoScene(QObject* parent = nullptr) : QObject(parent) {}

    QString lastError() const { return lastError_; }

    // 반환 = 만든 판(솔리드)의 id, 실패 0. 실패 사유는 lastError 로.
    Q_INVOKABLE int createSamplePart() {
        setError(QString());

        // ── 판 ──
        const uint32_t sketch = CAD_CreateRectangle(0, 0, 0, kPlateW, kPlateD, 0);
        if (!sketch) return fail("판 스케치(사각형)를 만들지 못했습니다");
        const uint32_t plate = CAD_CreateExtrude(sketch, kPlateH);
        if (!plate) return fail("판 돌출에 실패했습니다");

        // ── 모서리 구멍 Ø10 ×4 (관통) ──
        // 스케치는 판 바닥면(z=0)에 두고 throughAll 로 뚫는다 — depth 는 무시된다.
        const float holes[4][2] = {
            { kInset,            kInset            },
            { kPlateW - kInset,  kInset            },
            { kPlateW - kInset,  kPlateD - kInset  },
            { kInset,            kPlateD - kInset  },
        };
        for (const auto& h : holes) {
            const uint32_t c = CAD_CreateCircle(h[0], h[1], 0, kHoleR, 0, 0, 1);
            if (!c) return fail("모서리 구멍 스케치를 만들지 못했습니다");
            if (!CAD_CutExtrudeFromSketch(plate, c, 0, true))
                return fail("모서리 구멍을 뚫지 못했습니다");
        }

        // ── 가운데 보스 Ø40×25 ──
        // 스케치를 위 캡(z = 판 두께)에 올려야 엔진이 "위쪽 캡에서 위로" 쌓는다.
        const uint32_t bossSketch =
            CAD_CreateCircle(kPlateW / 2, kPlateD / 2, kPlateH, kBossR, 0, 0, 1);
        if (!bossSketch) return fail("보스 스케치를 만들지 못했습니다");
        if (!CAD_BossExtrudeFromSketch(plate, bossSketch, kBossH))
            return fail("보스를 쌓지 못했습니다");

        // ── 보스 가운데 관통 Ø20 ──
        // 보스보다 **나중에** 뚫는다. 기존 관통 컷은 뒤에 쌓인 보스까지 관통하므로 순서가 중요하다.
        const uint32_t boreSketch =
            CAD_CreateCircle(kPlateW / 2, kPlateD / 2, 0, kBoreR, 0, 0, 1);
        if (!boreSketch) return fail("관통 구멍 스케치를 만들지 못했습니다");
        if (!CAD_CutExtrudeFromSketch(plate, boreSketch, 0, true))
            return fail("관통 구멍을 뚫지 못했습니다");

        CAD_RequestZoomExtents();   // 방금 만든 게 화면 밖이면 시연이 안 된다
        return int(plate);
    }

signals:
    void lastErrorChanged();

private:
    // 엔진이 사유를 안 채웠을 수도 있어 호스트 쪽 설명을 기본값으로 둔다.
    int fail(const char* what) {
        const QString engine = cadjson::lastError();
        setError(engine.isEmpty() ? QString::fromUtf8(what)
                                  : QString::fromUtf8(what) + " — " + engine);
        return 0;
    }

    void setError(const QString& e) {
        if (e == lastError_) return;
        lastError_ = e;
        emit lastErrorChanged();
    }

    static constexpr float kPlateW = 120.0f;
    static constexpr float kPlateD = 80.0f;
    static constexpr float kPlateH = 10.0f;
    static constexpr float kInset  = 12.0f;   // 모서리 구멍 중심의 변 간격
    static constexpr float kHoleR  = 5.0f;    // Ø10
    static constexpr float kBossR  = 20.0f;   // Ø40
    static constexpr float kBossH  = 25.0f;
    static constexpr float kBoreR  = 10.0f;   // Ø20

    QString lastError_;
};
