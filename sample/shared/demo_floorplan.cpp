// 3D 평면도 — (1) 치수로 방 세우기(문·창 포함)  (2) 평면도 이미지에서 벽 찾아 세우기(AI 없이).
// 쓰는 API: CAD_CreateFloorplanWithOpenings · CAD_FloorplanExtractFromRgba · CAD_CreateWallGraph
//           CAD_CreateFloorplanFromImage(파일)
#include "demo_internal.h"

namespace demo {
namespace {

// 시험용 평면도 그림 — 흰 바탕에 검은 벽(외벽 두껍게, 내벽 얇게), 문 자리는 틈.
// 샘플에 도면 이미지를 따로 넣지 않으려고 코드로 그린다. 1px = 10mm.
struct Canvas {
    int w, h;
    std::vector<unsigned char> px;
    Canvas(int w_, int h_) : w(w_), h(h_), px(static_cast<size_t>(w_ * h_ * 4), 255) {}
    void rect(int x0, int y0, int x1, int y1) {   // 채운 사각형(검정)
        for (int y = std::max(0, y0); y < std::min(h, y1); ++y)
            for (int x = std::max(0, x0); x < std::min(w, x1); ++x) {
                unsigned char* p = &px[static_cast<size_t>((y * w + x) * 4)];
                p[0] = p[1] = p[2] = 0; p[3] = 255;
            }
    }
    void clear(int x0, int y0, int x1, int y1) {  // 문 자리 지우기(흰색)
        for (int y = std::max(0, y0); y < std::min(h, y1); ++y)
            for (int x = std::max(0, x0); x < std::min(w, x1); ++x) {
                unsigned char* p = &px[static_cast<size_t>((y * w + x) * 4)];
                p[0] = p[1] = p[2] = 255;
            }
    }
};

// 0: 방 두 개, 1: 방 세 개(ㄱ자 복도)
Canvas makePlan(int kind) {
    Canvas c(1000, 700);
    const int m = 50, T = 20, t = 10;     // 여백, 외벽 200mm, 내벽 100mm
    const int x0 = m, y0 = m, x1 = 1000 - m, y1 = 700 - m;
    c.rect(x0, y0, x1, y0 + T); c.rect(x0, y1 - T, x1, y1);   // 위·아래 외벽
    c.rect(x0, y0, x0 + T, y1); c.rect(x1 - T, y0, x1, y1);   // 왼·오른 외벽
    if (kind == 0) {
        const int xm = 520;
        c.rect(xm, y0, xm + t, y1);                 // 가운데 내벽
        c.clear(xm, 300, xm + t, 390);              // 문 900mm
        c.clear(200, y1 - T, 290, y1);              // 현관
    } else {
        const int xa = 380, xb = 680, ym = 380;
        c.rect(xa, y0, xa + t, y1);
        c.rect(xb, y0, xb + t, ym);
        c.rect(xa, ym, x1, ym + t);
        c.clear(xa, 160, xa + t, 250);
        c.clear(xb, 160, xb + t, 250);
        c.clear(780, ym, 870, ym + t);
        c.clear(150, y1 - T, 240, y1);
    }
    return c;
}

class FloorplanDemo : public Demo {
public:
    const char* id() const override { return "floorplan"; }
    const char* title() const override { return "3D 평면도"; }
    const char* summary() const override { return "치수로 방·문·창을 세우거나, 평면도 그림에서 벽을 찾아 3D 로 세운다"; }

    void setup() override { buildRooms(); }

    void update(double) override { status = note_; }

protected:
    void buildControls() override {
        header("치수로 세우기 (CAD_CreateFloorplanWithOpenings)");
        slider("거실 폭", "m", 2, 8, [this] { return wA_; }, [this](float v) { wA_ = v; });
        slider("방 폭", "m", 2, 6, [this] { return wB_; }, [this](float v) { wB_ = v; });
        slider("깊이", "m", 2, 8, [this] { return depth_; }, [this](float v) { depth_ = v; });
        slider("높이", "m", 2, 4, [this] { return height_; }, [this](float v) { height_ = v; });
        button("세우기", [this] { buildRooms(); });

        header("그림에서 벽 찾기 (CAD_FloorplanExtractFromRgba)");
        choice("시험 도면", {"방 두 개", "방 세 개"}, [this] { return plan_; }, [this](int k) { plan_ = k; });
        button("그림 → 3D 벽", [this] { buildFromImage(); });
        fileButton("도면 이미지 파일 열기…", [this](const std::string& path) { buildFromFile(path); });
        label("결과", [this] { return note_.empty() ? std::string("—") : note_; });
    }

private:
    void buildRooms() {
        clearScene();
        // 거실(0) | 방(1) 나란히. 단위 m, 원점 기준 로컬 좌표.
        const float rects[8] = {0, 0, wA_, depth_, wA_, 0, wA_ + wB_, depth_};
        CAD_FloorplanOpeningDesc op[4] = {};
        op[0] = {0, 0, 1, 0, 0.9f, 2.1f, 0, 0, 0};          // 거실↔방 문
        op[1] = {0, 0, -1, 0, 0.9f, 2.1f, 0, 1, 0.5f};      // 거실 남쪽 현관
        op[2] = {1, 0, -1, 1, 1.8f, 1.2f, 0.9f, 0, 0};      // 거실 북쪽 창
        op[3] = {1, 1, -1, 3, 1.2f, 1.2f, 0.9f, 0, 0};      // 방 동쪽 창
        uint32_t ids[64];
        const uint32_t n = CAD_CreateFloorplanWithOpenings(-(wA_ + wB_) * 0.5f, -depth_ * 0.5f, 0, rects, 2,
                                                           height_, 0.15f, op, 4, ids, 64);
        note_ = n ? fmt("벽 %u개 — 문 2 · 창 2", n) : "실패: " + lastError();
        CAD_ClearSelection();
        CAD_RequestSetView(3);
        CAD_RequestZoomExtents();
    }

    void buildFromImage() {
        const Canvas c = makePlan(plan_);
        const int w = c.w, h = c.h;
        const unsigned char* px = c.px.data();
        // 그림 폭 = 1000px × 10mm 중 벽 중심선 폭(여백·외벽 반 두께 제외)
        const float widthMm = (1000 - 2 * 50 - 20) * 10.0f;
        const std::string json = readString([&](char* o, int cap) {
            return CAD_FloorplanExtractFromRgba(px, w, h, widthMm, o, cap);
        });
        if (json.empty()) { note_ = "벽을 찾지 못했습니다: " + lastError(); return; }
        buildWalls(json);
    }

    void buildFromFile(const std::string& path) {
        clearScene();
        uint32_t ids[256];
        const uint32_t n = CAD_CreateFloorplanFromImage(path.c_str(), 0, height_, true, ids, 256);
        note_ = n ? fmt("벽 %u개 (축척은 문 폭 900mm 로 추정)", n) : "실패: " + lastError();
        CAD_ClearSelection();
        CAD_RequestSetView(3);
        CAD_RequestZoomExtents();
    }

    // 추출 JSON(mm) → [x1,y1,x2,y2,두께] (m) → CAD_CreateWallGraph
    void buildWalls(const std::string& json) {
        clearScene();
        std::vector<float> segs;
        for (const std::string& wobj : jsonObjects(json, "walls")) {
            segs.push_back(static_cast<float>(jsonNumber(wobj, "x1") / 1000.0));
            segs.push_back(static_cast<float>(jsonNumber(wobj, "y1") / 1000.0));
            segs.push_back(static_cast<float>(jsonNumber(wobj, "x2") / 1000.0));
            segs.push_back(static_cast<float>(jsonNumber(wobj, "y2") / 1000.0));
            segs.push_back(static_cast<float>(jsonNumber(wobj, "thicknessMm") / 1000.0));
        }
        const uint32_t count = static_cast<uint32_t>(segs.size() / 5);
        if (count == 0) { note_ = "벽 선분이 없습니다: " + jsonString(json, "summary"); return; }
        const float ox = -static_cast<float>(jsonNumber(json, "widthMm") / 2000.0);
        const float oy = -static_cast<float>(jsonNumber(json, "depthMm") / 2000.0);
        uint32_t ids[256];
        const uint32_t n = CAD_CreateWallGraph(ox, oy, 0, segs.data(), count, height_, 0.1f, ids, 256);
        note_ = n ? fmt("벽 선분 %u개 → 벽 %u개 · %s", count, n, jsonString(json, "summary").c_str())
                  : "실패: " + lastError();
        CAD_ClearSelection();
        CAD_RequestSetView(3);
        CAD_RequestZoomExtents();
    }

    float wA_ = 5, wB_ = 3.5f, depth_ = 4, height_ = 2.6f;
    int plan_ = 0;
    std::string note_;
};

} // namespace

Demo* makeFloorplanDemo() { return new FloorplanDemo(); }

} // namespace demo
