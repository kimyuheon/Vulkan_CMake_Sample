// 내비(경로 주행) — 방 안에 장애물을 두고, 지도(격자)를 만들고, 목적지를 정해 대상을 달리게 한다.
// 쓰는 API: CAD_NavBuildMap · CAD_NavShowGrid · CAD_NavAddAgent · CAD_NavSetAgentParams · CAD_NavSetGoal
//           CAD_NavStart · CAD_NavStop · CAD_NavReset · CAD_NavGetStateJson
#include "demo_internal.h"

namespace demo {
namespace {

class NavDemo : public Demo {
public:
    const char* id() const override { return "nav"; }
    const char* title() const override { return "내비 (경로 주행)"; }
    const char* summary() const override { return "장애물 사이로 길을 찾아 달린다 — 목적지·속도·라이다 광선"; }

    void setup() override {
        uint32_t walls[4];
        CAD_CreateRoom(0, 0, 0, 12, 9, 1.2f, 0.2f, walls);
        CAD_CreateBox(0, 0, -0.05f, 12.4f, 9.4f, 0.1f);           // 바닥
        // 장애물 — 가운데를 막아 돌아가게
        CAD_CreateBox(-1.0f, 0.5f, 0.5f, 0.8f, 5.5f, 1.0f);
        CAD_CreateBox(2.5f, -1.5f, 0.5f, 3.0f, 0.8f, 1.0f);
        CAD_CreateCylinder(3.0f, 2.2f, 0, 0.7f, 1.0f);
        body_ = CAD_CreateCylinder(-4.5f, -3.0f, 0, 0.3f, 0.8f);  // 달리는 대상
        CAD_ClearSelection();
        gx_ = 4.5f; gy_ = 3.0f;
        speed_ = 2.0f; rays_ = true; grid_ = false;
        agent_ = 0;
        buildMap();
        CAD_RequestSetView(3);
        CAD_RequestZoomExtents();
    }

    void update(double) override {
        state_ = readString([](char* o, int c) { return CAD_NavGetStateJson(o, c); });
        const std::vector<std::string> agents = jsonObjects(state_, "agents");
        if (agents.empty()) { status = "대상 없음"; return; }
        const std::string& a = agents.front();
        if (jsonBool(a, "arrived")) status = "도착";
        else if (jsonBool(a, "driving")) status = fmt("주행 중 — 남은 경로 점 %d/%d",
                                                     static_cast<int>(jsonNumber(a, "pathIndex")),
                                                     static_cast<int>(jsonNumber(a, "pathPoints")));
        else if (jsonBool(a, "hasGoal")) status = fmt("경로 %.1f m — [출발]", jsonNumber(a, "pathLength"));
        else status = "목적지를 정하세요";
    }

    void teardown() override { CAD_NavStop(0); }

protected:
    void buildControls() override {
        header("지도 (CAD_NavBuildMap)");
        toggle("격자 보기", [this] { return grid_; }, [this](bool on) { grid_ = on; CAD_NavShowGrid(on); });
        button("지도 다시 만들기", [this] { buildMap(); });

        header("목적지 (CAD_NavSetGoal)");
        slider("X", "m", -5.5f, 5.5f, [this] { return gx_; }, [this](float v) { gx_ = v; });
        slider("Y", "m", -4.0f, 4.0f, [this] { return gy_; }, [this](float v) { gy_ = v; });
        button("목적지 정하기", [this] {
            if (!CAD_NavSetGoal(agent_, gx_, gy_, 0)) note_ = "길이 없습니다: " + lastError();
            else note_ = fmt("목적지 (%.1f, %.1f)", gx_, gy_);
        }, [this] { return agent_ != 0; });

        header("주행");
        button("출발", [this] { CAD_NavStart(agent_); }, [this] { return agent_ != 0; });
        button("정지", [this] { CAD_NavStop(agent_); }, [this] { return agent_ != 0; });
        button("처음 자리로", [this] { CAD_NavReset(agent_); }, [this] { return agent_ != 0; });
        slider("속도", "m/s", 0.3f, 6, [this] { return speed_; }, [this](float v) { speed_ = v; params(); });
        toggle("라이다 광선", [this] { return rays_; }, [this](bool on) { rays_ = on; params(); });

        header("상태 (CAD_NavGetStateJson)");
        label("대상", [this] {
            const std::vector<std::string> agents = jsonObjects(state_, "agents");
            if (agents.empty()) return std::string("—");
            const std::string& a = agents.front();
            const std::vector<double> p = jsonNumbers(a, "position");
            return fmt("위치 (%.2f, %.2f) · 방향 %.0f° · 경로 %.1f m",
                       p.size() > 0 ? p[0] : 0.0, p.size() > 1 ? p[1] : 0.0,
                       jsonNumber(a, "yaw"), jsonNumber(a, "pathLength"));
        });
        label("지도", [this] {
            return fmt("%d×%d 칸 · 막힌 칸 %d", static_cast<int>(jsonNumber(state_, "grid.w")),
                       static_cast<int>(jsonNumber(state_, "grid.h")),
                       static_cast<int>(jsonNumber(state_, "grid.blockedCells")));
        });
        label("메모", [this] { return note_.empty() ? std::string("—") : note_; });
    }

private:
    void buildMap() {
        if (!CAD_NavBuildMap(nullptr)) { note_ = "지도 실패: " + lastError(); return; }
        if (!agent_) agent_ = CAD_NavAddAgent(body_, false);
        if (!agent_) { note_ = "대상 추가 실패: " + lastError(); return; }
        params();
        CAD_NavShowGrid(grid_);
        note_ = "지도를 만들었습니다";
    }

    void params() {
        if (agent_) CAD_NavSetAgentParams(agent_, fmt("{\"speed\":%.2f,\"showRays\":%s,\"checked\":true}",
                                                      speed_, rays_ ? "true" : "false").c_str());
    }

    uint32_t body_ = 0, agent_ = 0;
    float gx_ = 4.5f, gy_ = 3.0f, speed_ = 2.0f;
    bool rays_ = true, grid_ = false;
    std::string state_, note_;
};

} // namespace

Demo* makeNavDemo() { return new NavDemo(); }

} // namespace demo
