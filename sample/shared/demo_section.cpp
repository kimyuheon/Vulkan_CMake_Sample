// 단면도 · 도면 뷰 — 실시간 단면(평면·슬라이스·상자)으로 속을 보고, 2D 윤곽을 뽑고, 3각법 도면 뷰·단면도·상세도를 만든다.
// 쓰는 API: CAD_SetSection · CAD_SetSectionBox · CAD_SetSectionOptions · CAD_GetSectionStateJson · CAD_ExtractSectionTo2D
//           CAD_CreateDrawingViews · CAD_CreateSectionView · CAD_CreateDetailView · CAD_GetDrawingViewsJson · CAD_ClearDrawingViews
#include "demo_internal.h"

namespace demo {
namespace {

class SectionDemo : public Demo {
public:
    const char* id() const override { return "section"; }
    const char* title() const override { return "단면도 · 도면 뷰"; }
    const char* summary() const override { return "모델을 잘라 속을 보고, 2D 윤곽·3각법 도면·단면도 A-A·상세도를 만든다"; }

    void setup() override {
        model_ = 0;
        models_ = {"models/GearboxAssy.glb", "models/2CylinderEngine.glb"};
        loadModel(0);
    }

protected:
    void buildControls() override {
        header("모델");
        choice("열기", {"기어박스", "2기통 엔진"}, [this] { return model_; },
               [this](int k) { loadModel(k); rebuild(); });

        header("실시간 단면 (CAD_SetSection)");
        choice("방식", {"끄기", "평면", "슬라이스", "상자"}, [this] { return mode_; },
               [this](int k) { mode_ = k; apply(); });
        choice("축", {"X", "Y", "Z"}, [this] { return axis_; },
               [this](int k) { axis_ = k; pos_ = mid(axis_); thick_ = span(axis_) * 0.15f; apply(); rebuild(); });
        slider("위치", "", lo(axis_), hi(axis_), [this] { return pos_; }, [this](float v) { pos_ = v; apply(); });
        slider("두께 (슬라이스)", "", span(axis_) * 0.01f, span(axis_) * 0.5f,
               [this] { return thick_; }, [this](float v) { thick_ = v; apply(); });
        toggle("반대쪽 자르기", [this] { return flip_; }, [this](bool on) { flip_ = on; apply(); });
        toggle("단면 화살표", [this] { return arrows_; }, [this](bool on) { arrows_ = on; CAD_SetSectionOptions(arrows_, false); });
        button("2D 윤곽 뽑기", [this] {
            const uint32_t n = CAD_ExtractSectionTo2D(true);
            note_ = n ? fmt("2D 윤곽 %u개 — 모델 오른쪽 XY 평면", n) : "실패: " + lastError();
            // 윤곽은 모델 오른쪽에 놓인다 — 단면을 켜 둔 채면 그쪽이 잘려 안 보인다
            if (n) { sectionOff(); CAD_RequestSetView(1); CAD_RequestZoomExtents(); }
        }, [this] { return mode_ == 1; });

        header("도면 뷰 (CAD_CreateDrawingViews)");
        button("3각법 도면 만들기", [this] {
            sectionOff();   // 도면 뷰는 모델 옆에 놓인다 — 실시간 단면이 그쪽을 자르지 않게 끈다
            const uint32_t n = CAD_CreateDrawingViews(nullptr, 0, 1.0f);
            note_ = n ? fmt("도면 뷰 %u개 (정면·평면·우측면·등각)", n) : "실패: " + lastError();
            CAD_RequestSetView(1);   // 도면은 XY 평면 — 위에서 본다
            CAD_RequestZoomExtents();
        });
        button("단면도 A-A", [this] { sectionView(); }, [this] { return hasViews(); });
        button("상세도", [this] { detailView(); }, [this] { return hasViews(); });
        button("도면 뷰 지우기", [this] {
            CAD_ClearDrawingViews();
            note_ = "도면 뷰를 지웠습니다";
            CAD_RequestSetView(3);
            CAD_RequestZoomExtents();
        }, [this] { return hasViews(); });
        label("결과", [this] { return note_.empty() ? std::string("—") : note_; });
    }

public:
    void update(double) override {
        static const char* names[] = {"끄기", "평면", "슬라이스", "상자"};
        status = fmt("단면: %s", names[mode_ & 3]);
    }

private:
    void loadModel(int k) {
        clearScene();
        model_ = k;
        if (!CAD_OpenFile(models_[static_cast<size_t>(k)].c_str())) {
            note_ = "모델을 열지 못했습니다: " + models_[static_cast<size_t>(k)];
        }
        CAD_ClearSelection();
        readBounds();
        mode_ = 1;
        axis_ = 0;
        pos_ = mid(0);
        thick_ = span(0) * 0.15f;
        flip_ = false;
        apply();
        CAD_RequestSetView(3);
        CAD_RequestZoomExtents();
    }

    // 장면 범위 = 슬라이더 범위 (단면 상태 JSON 의 sceneBounds)
    void readBounds() {
        const std::string s = readString([](char* o, int c) { return CAD_GetSectionStateJson(o, c); });
        const std::vector<double> mn = jsonNumbers(s, "sceneBounds.min"), mx = jsonNumbers(s, "sceneBounds.max");
        for (int i = 0; i < 3; ++i) {
            bmin_[i] = mn.size() == 3 ? static_cast<float>(mn[i]) : -1.0f;
            bmax_[i] = mx.size() == 3 ? static_cast<float>(mx[i]) : 1.0f;
            if (bmax_[i] - bmin_[i] < 1e-4f) { bmin_[i] -= 0.5f; bmax_[i] += 0.5f; }
        }
    }

    void apply() {
        if (mode_ == 3) {
            // 상자 = 장면 범위의 가운데 70% (안쪽만 남긴다)
            float a[3], b[3];
            for (int i = 0; i < 3; ++i) { const float m = (bmax_[i] - bmin_[i]) * 0.15f; a[i] = bmin_[i] + m; b[i] = bmax_[i] - m; }
            CAD_SetSectionBox(a[0], a[1], a[2], b[0], b[1], b[2]);
        }
        if (!CAD_SetSection(mode_, axis_, pos_, flip_, thick_)) note_ = "단면 실패: " + lastError();
    }

    std::string views() const { return readString([](char* o, int c) { return CAD_GetDrawingViewsJson(o, c); }); }
    bool hasViews() const { return !jsonObjects(views(), "").empty(); }

    // 첫 기본 뷰(정면)의 자리 — 단면선·상세 원을 그 위에 놓는다
    bool baseBounds(double& x0, double& y0, double& x1, double& y1) const {
        for (const std::string& v : jsonObjects(views(), "")) {
            if (jsonString(v, "kind") != "base") continue;
            const std::vector<double> a = jsonNumbers(v, "bounds.min"), b = jsonNumbers(v, "bounds.max");
            if (a.size() < 2 || b.size() < 2) continue;
            x0 = a[0]; y0 = a[1]; x1 = b[0]; y1 = b[1];
            return true;
        }
        return false;
    }

    void sectionView() {
        double x0, y0, x1, y1;
        if (!baseBounds(x0, y0, x1, y1)) { note_ = "기본 뷰가 없습니다"; return; }
        const double y = (y0 + y1) * 0.5, pad = (x1 - x0) * 0.1;   // 정면도 가운데를 가로지르는 선
        const uint32_t v = CAD_CreateSectionView(static_cast<float>(x0 - pad), static_cast<float>(y),
                                                 static_cast<float>(x1 + pad), static_cast<float>(y));
        note_ = v ? "단면도 A-A 를 만들었습니다" : "실패: " + lastError();
        CAD_RequestZoomExtents();
    }

    void detailView() {
        double x0, y0, x1, y1;
        if (!baseBounds(x0, y0, x1, y1)) { note_ = "기본 뷰가 없습니다"; return; }
        const double r = std::fmin(x1 - x0, y1 - y0) * 0.2;
        const uint32_t v = CAD_CreateDetailView(static_cast<float>((x0 + x1) * 0.5), static_cast<float>((y0 + y1) * 0.5),
                                                static_cast<float>(r), 2.0f);
        note_ = v ? "상세도(2배)를 만들었습니다" : "실패: " + lastError();
        CAD_RequestZoomExtents();
    }

    void sectionOff() { mode_ = 0; apply(); rebuild(); }

    float lo(int a) const { return bmin_[a]; }
    float hi(int a) const { return bmax_[a]; }
    float mid(int a) const { return (bmin_[a] + bmax_[a]) * 0.5f; }
    float span(int a) const { return bmax_[a] - bmin_[a]; }

    std::vector<std::string> models_;
    int model_ = 0, mode_ = 1, axis_ = 0;
    float pos_ = 0, thick_ = 0.1f;
    bool flip_ = false, arrows_ = true;
    float bmin_[3] = {-1, -1, -1}, bmax_[3] = {1, 1, 1};
    std::string note_;
};

} // namespace

Demo* makeSectionDemo() { return new SectionDemo(); }

} // namespace demo
