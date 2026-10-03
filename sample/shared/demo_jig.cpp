// Jig — 객체를 "끌고 다니다가" 확정(AutoCAD AcEdJig). 마우스 없이 버튼·슬라이더로도 끈다.
// 쓰는 API: CAD_JigBeginOne · CAD_JigMoveTo · CAD_JigGetDelta · CAD_JigCommit · CAD_JigCancel · CAD_JigState
//           CAD_JigBeginVariants · CAD_JigNextVariant (Tab 처럼 형상 바꾸기)
#include "demo_internal.h"

namespace demo {
namespace {

class JigDemo : public Demo {
public:
    const char* id() const override { return "jig"; }
    const char* title() const override { return "Jig (끌어서 배치)"; }
    const char* summary() const override { return "상자를 끌어 옮기기·돌리기·키우기·복사, 끄는 중에 형상 바꾸기(변형 Jig)"; }

    void setup() override {
        CAD_CreateBox(0, 0, -0.05f, 8.0f, 8.0f, 0.1f);   // 바닥
        const float xs[3] = {-2.0f, 0.0f, 2.0f};
        for (int i = 0; i < 3; ++i) boxes_[i] = CAD_CreateBox(xs[i], 0, 0.5f, 1.0f, 1.0f, 1.0f);
        target_ = 0;
        mode_ = 0;
        x_ = 0; y_ = 0;
        lastState_ = "대기";
        CAD_ClearSelection();
        CAD_RequestSetView(3);
        CAD_RequestZoomExtents();
    }

    void update(double) override {
        const int st = CAD_JigState();          // 2·3 은 한 번만 온다 — 놓치지 않게 매 프레임 본다
        if (st == 2) lastState_ = "확정됨";
        else if (st == 3) lastState_ = "취소됨";
        dragging_ = st == 1;
        if (dragging_) {
            float dx = 0, dy = 0, dz = 0, ang = 0, sc = 1;
            CAD_JigGetDelta(&dx, &dy, &dz, &ang, &sc);
            delta_ = fmt("이동 (%.2f, %.2f, %.2f) · 회전 %.1f° · 배율 %.2f", dx, dy, dz, ang, sc);
            status = "끄는 중 — 슬라이더로 옮기고 [확정]";
        } else {
            status = "Jig: " + lastState_;
        }
    }

    void teardown() override { if (CAD_JigState() == 1) CAD_JigCancel(); }

protected:
    void buildControls() override {
        header("대상과 동작");
        choice("상자", {"왼쪽", "가운데", "오른쪽"}, [this] { return target_; }, [this](int k) { target_ = k; });
        choice("동작", {"이동", "회전", "축척", "복사"}, [this] { return mode_; }, [this](int k) { mode_ = k; });
        button("Jig 시작", [this] { begin(); }, [this] { return !dragging_; });

        header("끌기 (CAD_JigMoveTo)");
        slider("X", "m", -4, 4, [this] { return x_; }, [this](float v) { x_ = v; moveTo(); });
        slider("Y", "m", -4, 4, [this] { return y_; }, [this](float v) { y_ = v; moveTo(); });
        label("미리보기", [this] { return dragging_ ? delta_ : std::string("—"); });
        button("확정", [] { CAD_JigCommit(); }, [this] { return dragging_; });
        button("취소", [] { CAD_JigCancel(); }, [this] { return dragging_; });

        header("변형 Jig — 끄는 중에 형상 바꾸기");
        button("상자 · 원기둥 · 구 로 시작", [this] { beginVariants(); }, [this] { return !dragging_; });
        button("다음 형상 (Tab)", [] { CAD_JigNextVariant(); },
               [this] { return dragging_ && CAD_JigVariantIndex() >= 0; });
        label("후보", [] {
            const int i = CAD_JigVariantIndex();
            return i < 0 ? std::string("—") : fmt("%d번 (id %u)", i + 1, CAD_JigVariantId());
        });
    }

private:
    uint32_t pick() const {
        // 확정된 이동·복사로 객체가 바뀌어도(복사본) 원래 상자를 쓴다. 지워졌으면 0.
        const uint32_t id = boxes_[target_];
        return CAD_ObjectExists(id) ? id : 0;
    }

    void begin() {
        const uint32_t id = pick();
        if (!id) { lastState_ = "상자가 없습니다"; return; }
        // 회전·축척의 피벗 = 상자 바닥 중심. 이동·복사는 엔진이 경계 상자 중심을 기준점으로 잡는다.
        const float px = -2.0f + 2.0f * target_;
        if (!CAD_JigBeginOne(mode_, id, px, 0, 0)) { lastState_ = "시작 실패: " + lastError(); return; }
        // 이동·복사는 슬라이더가 기준점의 새 자리. 회전·축척은 피벗에서 끄는 점.
        x_ = mode_ == 1 || mode_ == 2 ? px + 1.0f : px;
        y_ = 0;
        moveTo();
    }

    void beginVariants() {
        const uint32_t ids[3] = {
            CAD_CreateBox(0, -2.5f, 0.5f, 1.0f, 1.0f, 1.0f),
            CAD_CreateCylinder(0, -2.5f, 0, 0.5f, 1.0f),
            CAD_CreateSphere(0, -2.5f, 0, 0.5f),
        };
        CAD_ClearSelection();
        if (!CAD_JigBeginVariants(0, ids, 3, 0, -2.5f, 0)) { lastState_ = "시작 실패: " + lastError(); return; }
        x_ = 0; y_ = -2.5f;
        moveTo();
    }

    // 높이 = 기준점 높이(상자 중심 0.5) — 0 을 주면 바닥에 반쯤 묻힌다
    void moveTo() { if (CAD_JigState() == 1) CAD_JigMoveTo(x_, y_, 0.5f); }

    uint32_t boxes_[3] = {};
    int target_ = 0, mode_ = 0;
    float x_ = 0, y_ = 0;
    bool dragging_ = false;
    std::string lastState_, delta_;
};

} // namespace

Demo* makeJigDemo() { return new JigDemo(); }

} // namespace demo
