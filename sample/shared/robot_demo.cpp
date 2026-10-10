// robot_demo — 로봇 팔 샘플 공용 로직. 설명은 robot_demo.h.
#include "robot_demo.h"
#include "LotCAD_API.h"

#include <algorithm>
#include <cmath>
#include <string>
#include <vector>

namespace {
    constexpr float kPi = 3.14159265f;
    constexpr float kRadToDeg = 180.0f / kPi;

    // 조작 항목 하나 = 관절 하나(회전) 또는 관절 여러 개(그리퍼).
    struct Control {
        std::string label;
        std::string unit;
        std::vector<int> joints;   // 같이 움직일 관절 인덱스
        float lower = 0, upper = 0; // 관절 단위(라디안/미터)
        bool gripper = false;       // true 면 값 = 0~100 % (lower..upper 의 비율)
        // 자동 재생용 — 항목마다 다른 속도·위상으로 흔들어 단조롭지 않게.
        float speed = 0.5f, phase = 0;
    };

    uint32_t g_robot = 0;
    std::vector<Control> g_controls;
    bool g_playing = false;
    double g_time = 0;

    // 관절 이름 → 화면 라벨. 모르는 이름은 "joint_" 를 떼고 그대로.
    std::string labelFor(const std::string& name) {
        static const std::pair<const char*, const char*> kNames[] = {
            {"joint_base", "회전대"},
            {"joint_shoulder", "어깨"},
            {"joint_elbow", "팔꿈치"},
            {"joint_wrist_roll", "손목 회전"},
            {"joint_wrist_pitch", "손목 꺾기"},
            {"joint_flange", "플랜지"},
        };
        for (const auto& [k, v] : kNames) if (name == k) return v;
        return name.rfind("joint_", 0) == 0 ? name.substr(6) : name;
    }

    bool valid(int i) { return g_robot != 0 && i >= 0 && i < static_cast<int>(g_controls.size()); }

    // 화면 값 ↔ 관절 값
    float toJoint(const Control& c, float v) {
        return c.gripper ? c.lower + (c.upper - c.lower) * (v / 100.0f) : v / kRadToDeg;
    }
    float toDisplay(const Control& c, float j) {
        if (!c.gripper) return j * kRadToDeg;
        const float span = c.upper - c.lower;
        return span > 0 ? (j - c.lower) / span * 100.0f : 0.0f;
    }

    void apply(const Control& c, float display) {
        const float j = toJoint(c, display);
        std::vector<float> vals(c.joints.size(), j);
        CAD_SetJointValues(g_robot, c.joints.data(), vals.data(), static_cast<uint32_t>(vals.size()));
    }

    void buildControls() {
        g_controls.clear();
        Control grip;
        grip.label = "그리퍼"; grip.unit = "%"; grip.gripper = true;
        grip.speed = 0.9f;

        const int n = static_cast<int>(CAD_GetJointCount(g_robot));
        for (int j = 0; j < n; ++j) {
            int type = 0; float lo = 0, hi = 0; bool hasLimit = false;
            if (!CAD_GetJointInfo(g_robot, j, &type, &lo, &hi, &hasLimit)) continue;
            if (type == 0) continue;   // 고정 관절은 조작할 게 없다
            if (type == 3) {           // 직선 관절 = 손가락 → 그리퍼 하나로
                if (grip.joints.empty()) { grip.lower = lo; grip.upper = hi; }
                grip.joints.push_back(j);
                continue;
            }
            char name[128] = {};
            CAD_GetJointName(g_robot, j, name, static_cast<int>(sizeof(name)));
            Control c;
            c.label = labelFor(name);
            c.unit = "°";
            c.joints = {j};
            // 한계가 없으면(continuous) 한 바퀴로 둔다.
            c.lower = hasLimit ? lo : -kPi;
            c.upper = hasLimit ? hi : kPi;
            const int k = static_cast<int>(g_controls.size());
            c.speed = 0.35f + 0.12f * static_cast<float>(k % 4);
            c.phase = 1.3f * static_cast<float>(k);
            g_controls.push_back(std::move(c));
        }
        if (!grip.joints.empty()) g_controls.push_back(std::move(grip));
    }
}

extern "C" {

bool RobotDemo_Load(void) {
    // 엔진이 다시 만들어졌으면(앱 재시작 — 프로세스는 살아 있어 이 전역은 남는다) 로봇이 없다.
    if (g_robot != 0 && CAD_GetRobotCount() >= g_robot) return true;
    g_robot = 0;
    g_playing = false;
    // 로봇 전용 장면 — 엔진 기본 데모 객체를 치운다(로봇이 그 사이에 묻혀 작게 보인다).
    // CAD_RequestClearAll 은 큐로 가서 다음 틱에 돌므로 방금 불러온 로봇까지 지운다 → 즉시 삭제.
    std::vector<uint32_t> ids(CAD_GetObjectIds(nullptr, 0));
    if (!ids.empty()) {
        const uint32_t n = CAD_GetObjectIds(ids.data(), static_cast<uint32_t>(ids.size()));
        ids.resize(std::min<std::size_t>(n, ids.size()));   // 반환은 총 개수 — 버퍼보다 클 수 있다
        for (uint32_t id : ids) CAD_DeleteObject(id);
    }
    // 엔진이 작업 폴더를 런타임 에셋 경로로 옮겨 두므로 상대 경로로 충분하다.
    g_robot = CAD_LoadUrdf("models/demo_arm.urdf");
    if (g_robot == 0) return false;
    buildControls();
    CAD_ClearSelection();         // 불러온 링크가 선택 강조색으로 보이지 않게
    CAD_RequestSetView(3);        // Iso
    CAD_RequestZoomExtents();
    return true;
}

int RobotDemo_ControlCount(void) { return g_robot ? static_cast<int>(g_controls.size()) : 0; }

const char* RobotDemo_ControlLabel(int i) { return valid(i) ? g_controls[i].label.c_str() : ""; }
const char* RobotDemo_ControlUnit(int i)  { return valid(i) ? g_controls[i].unit.c_str() : ""; }

float RobotDemo_ControlMin(int i) {
    if (!valid(i)) return 0;
    return g_controls[i].gripper ? 0.0f : g_controls[i].lower * kRadToDeg;
}
float RobotDemo_ControlMax(int i) {
    if (!valid(i)) return 0;
    return g_controls[i].gripper ? 100.0f : g_controls[i].upper * kRadToDeg;
}

float RobotDemo_GetControl(int i) {
    if (!valid(i)) return 0;
    const Control& c = g_controls[i];
    return toDisplay(c, CAD_GetJointValue(g_robot, c.joints.front()));
}

void RobotDemo_SetControl(int i, float value) {
    if (!valid(i)) return;
    g_playing = false;   // 손으로 잡으면 자동 재생과 싸우지 않게
    const float lo = RobotDemo_ControlMin(i), hi = RobotDemo_ControlMax(i);
    apply(g_controls[i], std::fmin(std::fmax(value, lo), hi));
}

void RobotDemo_Home(void) {
    g_playing = false;
    for (const auto& c : g_controls) apply(c, 0.0f);
}

void RobotDemo_SetPlaying(bool on) { g_playing = on && g_robot != 0; }
bool RobotDemo_IsPlaying(void) { return g_playing; }

void RobotDemo_Update(double dtSeconds) {
    if (!g_playing || g_robot == 0) return;
    g_time += dtSeconds;
    for (std::size_t k = 0; k < g_controls.size(); ++k) {
        const Control& c = g_controls[k];
        const float s = std::sin(static_cast<float>(g_time) * c.speed * 2.0f * kPi * 0.25f + c.phase);
        float v;
        if (c.gripper) {
            v = 50.0f + 50.0f * s;   // 열고 닫기
        } else {
            // 0 을 가운데로 한계의 40 % 까지만 — 팔이 바닥을 뚫고 들어가지 않을 만큼.
            const float lo = c.lower * kRadToDeg, hi = c.upper * kRadToDeg;
            const float amp = 0.4f * std::fmin(std::fabs(lo), std::fabs(hi));
            v = amp * s;
        }
        apply(c, v);
    }
}

} // extern "C"
