// demo_kit 내부 — 데모 하나 = Demo 를 상속해 장면(setup)과 조작 항목(controls)을 만든다.
// 플랫폼에는 안 보인다(demo_kit.h 의 C API 만 보인다).
#pragma once

#include "LotCAD_API.h"

#include <algorithm>
#include <cmath>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <functional>
#include <string>
#include <vector>

namespace demo {

struct Control {
    int kind = 0;
    std::string label, unit;
    float min = 0, max = 1;
    std::vector<std::string> choices;
    std::function<float()> get;                         // 슬라이더·토글·선택지 값
    std::function<void(float)> set;
    std::function<void()> press;                        // 버튼
    std::function<std::string()> text;                  // 입력칸·표시줄 문구
    std::function<void(const std::string&)> setText;    // 입력칸·파일
    std::function<bool()> enabled;                      // 없으면 늘 켜짐
};

class Demo {
public:
    virtual ~Demo() = default;
    virtual const char* id() const = 0;
    virtual const char* title() const = 0;
    virtual const char* summary() const = 0;
    virtual void setup() = 0;               // 장면 만들기 — 빈 장면에서 불린다
    virtual void update(double /*dt*/) {}
    virtual void teardown() {}              // 다른 데모로 바꿀 때(장면은 kit 가 비운다)

    std::vector<Control> controls;
    std::string status;
    int revision = 0;
    // 번호는 데모 전체에서 하나로 센다 — 데모를 바꿨을 때 두 데모의 번호가 우연히 같으면
    // 호스트가 목록이 바뀐 걸 모르고 이전 데모의 패널을 그대로 둔다.
    void rebuild() { controls.clear(); buildControls(); revision = ++nextRevision(); }
    static int& nextRevision() { static int n = 0; return n; }

protected:
    virtual void buildControls() = 0;

    // 항목 만들기 도우미
    void header(const std::string& l) { Control c; c.kind = 0; c.label = l; controls.push_back(c); }
    void slider(const std::string& l, const std::string& unit, float lo, float hi,
                std::function<float()> g, std::function<void(float)> s) {
        Control c; c.kind = 1; c.label = l; c.unit = unit; c.min = lo; c.max = hi; c.get = g; c.set = s;
        controls.push_back(c);
    }
    void button(const std::string& l, std::function<void()> p, std::function<bool()> en = nullptr) {
        Control c; c.kind = 2; c.label = l; c.press = p; c.enabled = en; controls.push_back(c);
    }
    void toggle(const std::string& l, std::function<bool()> g, std::function<void(bool)> s) {
        Control c; c.kind = 3; c.label = l; c.min = 0; c.max = 1;
        c.get = [g] { return g() ? 1.0f : 0.0f; };
        c.set = [s](float v) { s(v >= 0.5f); };
        controls.push_back(c);
    }
    void choice(const std::string& l, std::vector<std::string> items,
                std::function<int()> g, std::function<void(int)> s) {
        Control c; c.kind = 4; c.label = l; c.choices = std::move(items);
        c.max = static_cast<float>(c.choices.empty() ? 0 : c.choices.size() - 1);
        c.get = [g] { return static_cast<float>(g()); };
        c.set = [s](float v) { s(static_cast<int>(std::lround(v))); };
        controls.push_back(c);
    }
    void textField(const std::string& l, std::function<std::string()> g, std::function<void(const std::string&)> s) {
        Control c; c.kind = 5; c.label = l; c.text = g; c.setText = s; controls.push_back(c);
    }
    void label(const std::string& l, std::function<std::string()> g) {
        Control c; c.kind = 6; c.label = l; c.text = g; controls.push_back(c);
    }
    void fileButton(const std::string& l, std::function<void(const std::string&)> chosen) {
        Control c; c.kind = 7; c.label = l; c.setText = chosen; controls.push_back(c);
    }
};

// 데모 생성기 — demo_kit.cpp 의 목록에 등록한다.
Demo* makeRobotDemo();
Demo* makeParticleDemo();
Demo* makeJigDemo();
Demo* makeSectionDemo();
Demo* makeFloorplanDemo();
Demo* makeNavDemo();
Demo* makeAiDemo();

// ── 공용 도우미 (demo_kit.cpp) ──
void clearScene();                         // 객체·도면 뷰·단면·내비 대상·Jig 정리
std::string lastError();                   // CAD_GetLastError
std::string fmt(const char* f, ...);       // printf 식 문자열

// 엔진 JSON 에서 값 몇 개만 꺼내는 작은 읽개 — 샘플에 JSON 라이브러리를 들이지 않으려고.
// key 는 "a.b.c" 경로(객체 중첩). 못 찾으면 fallback.
double jsonNumber(const std::string& json, const std::string& path, double fallback = 0);
bool   jsonBool(const std::string& json, const std::string& path, bool fallback = false);
std::string jsonString(const std::string& json, const std::string& path);
// "path": [a, b, c] 의 숫자들
std::vector<double> jsonNumbers(const std::string& json, const std::string& path);
// "path": [ {...}, {...} ] 의 객체들을 원문 그대로 잘라 준다
std::vector<std::string> jsonObjects(const std::string& json, const std::string& path);

// 2회 호출 규약 문자열 API 를 std::string 으로
template <class F> std::string readString(F f) {
    int n = f(nullptr, 0);
    if (n <= 0) return {};
    std::string s(static_cast<size_t>(n) + 1, '\0');
    f(&s[0], n + 1);
    s.resize(static_cast<size_t>(n));
    return s;
}

} // namespace demo
