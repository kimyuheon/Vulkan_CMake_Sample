// 로봇 팔 — robot_demo(관절 → 조작 항목·자동 재생)를 demo_kit 항목으로 그대로 보여 준다.
// 로직은 robot_demo.cpp 한 벌 — 따로 있는 로봇 팔 앱(RobotArmiOS·RobotArmMac·android robot)과 같다.
#include "demo_internal.h"
#include "robot_demo.h"

namespace demo {
namespace {

class RobotDemo : public Demo {
public:
    const char* id() const override { return "robot"; }
    const char* title() const override { return "로봇 팔"; }
    const char* summary() const override { return "URDF 로봇의 관절을 슬라이더로 움직이고, 자동 재생으로 흔든다"; }

    void setup() override {
        loaded_ = RobotDemo_Load();
        status = loaded_ ? "" : "demo_arm.urdf 를 불러오지 못했습니다";
    }

    void update(double dt) override {
        RobotDemo_Update(dt);
        if (loaded_) status = RobotDemo_IsPlaying() ? "재생 중" : "정지";
    }

    void teardown() override { RobotDemo_SetPlaying(false); }

protected:
    void buildControls() override {
        header("동작");
        toggle("자동 재생", [] { return RobotDemo_IsPlaying(); }, [](bool on) { RobotDemo_SetPlaying(on); });
        button("홈 자세", [] { RobotDemo_Home(); });
        button("화면 맞춤", [] { CAD_RequestZoomExtents(); });

        header("관절");
        for (int i = 0; i < RobotDemo_ControlCount(); ++i) {
            slider(RobotDemo_ControlLabel(i), RobotDemo_ControlUnit(i), RobotDemo_ControlMin(i), RobotDemo_ControlMax(i),
                   [i] { return RobotDemo_GetControl(i); }, [i](float v) { RobotDemo_SetControl(i, v); });
        }
    }

private:
    bool loaded_ = false;
};

} // namespace

Demo* makeRobotDemo() { return new RobotDemo(); }

} // namespace demo
