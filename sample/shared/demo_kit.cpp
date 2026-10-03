// demo_kit — 데모 목록 + C API + 공용 도우미. 설명은 demo_kit.h.
#include "demo_kit.h"
#include "demo_internal.h"

#include <algorithm>
#include <cstdarg>
#include <memory>

namespace demo {

// ── 공용 도우미 ──────────────────────────────────────────────────────

void clearScene() {
    if (CAD_JigState() == 1) CAD_JigCancel();
    CAD_NavStop(0);
    CAD_NavClearAgents();
    CAD_NavShowGrid(false);
    CAD_SetSection(0, 0, 0, false, 0);
    CAD_ClearDrawingViews();
    CAD_ClearRobots();
    // CAD_RequestClearAll 은 큐로 가서 다음 틱에 돈다 → 방금 만든 장면까지 지운다. 즉시 삭제.
    std::vector<uint32_t> ids(CAD_GetObjectIds(nullptr, 0));
    if (!ids.empty()) {
        const uint32_t n = CAD_GetObjectIds(ids.data(), static_cast<uint32_t>(ids.size()));
        ids.resize(std::min<size_t>(n, ids.size()));
        for (uint32_t id : ids) CAD_DeleteObject(id);
    }
    CAD_ClearSelection();
}

std::string lastError() {
    return readString([](char* o, int c) { return CAD_GetLastError(o, c); });
}

std::string fmt(const char* f, ...) {
    char buf[512];
    va_list ap;
    va_start(ap, f);
    std::vsnprintf(buf, sizeof(buf), f, ap);
    va_end(ap);
    return buf;
}

// ── 작은 JSON 읽개 ──
// 키 경로를 따라 들어가며 "key": 다음 위치를 찾는다. 같은 깊이만 본다(중첩 객체 안의 같은 이름 키에 속지 않게).
namespace {
    size_t skipWs(const std::string& s, size_t p) {
        while (p < s.size() && (s[p] == ' ' || s[p] == '\n' || s[p] == '\r' || s[p] == '\t')) ++p;
        return p;
    }
    // [p] 가 값의 시작일 때 그 값의 끝(다음 문자) 위치
    size_t skipValue(const std::string& s, size_t p) {
        p = skipWs(s, p);
        if (p >= s.size()) return p;
        if (s[p] == '"') {
            for (++p; p < s.size(); ++p) {
                if (s[p] == '\\') { ++p; continue; }
                if (s[p] == '"') return p + 1;
            }
            return p;
        }
        if (s[p] == '{' || s[p] == '[') {
            int depth = 0;
            bool inStr = false;
            for (; p < s.size(); ++p) {
                const char ch = s[p];
                if (inStr) { if (ch == '\\') ++p; else if (ch == '"') inStr = false; continue; }
                if (ch == '"') inStr = true;
                else if (ch == '{' || ch == '[') ++depth;
                else if (ch == '}' || ch == ']') { if (--depth == 0) return p + 1; }
            }
            return p;
        }
        while (p < s.size() && s[p] != ',' && s[p] != '}' && s[p] != ']') ++p;
        return p;
    }
    // 객체 [begin] = '{' 안에서 key 의 값 시작 위치. 없으면 npos.
    size_t findKey(const std::string& s, size_t begin, const std::string& key) {
        size_t p = skipWs(s, begin);
        if (p >= s.size() || s[p] != '{') return std::string::npos;
        ++p;
        while (true) {
            p = skipWs(s, p);
            if (p >= s.size() || s[p] == '}') return std::string::npos;
            if (s[p] != '"') return std::string::npos;
            const size_t k0 = p + 1;
            size_t k1 = k0;
            while (k1 < s.size() && s[k1] != '"') { if (s[k1] == '\\') ++k1; ++k1; }
            const std::string k = s.substr(k0, k1 - k0);
            p = skipWs(s, k1 + 1);
            if (p >= s.size() || s[p] != ':') return std::string::npos;
            const size_t v = skipWs(s, p + 1);
            if (k == key) return v;
            p = skipWs(s, skipValue(s, v));
            if (p < s.size() && s[p] == ',') ++p;
        }
    }
    size_t findPath(const std::string& s, const std::string& path) {
        size_t at = skipWs(s, 0);
        size_t start = 0;
        while (start <= path.size()) {
            const size_t dot = path.find('.', start);
            const std::string key = path.substr(start, dot == std::string::npos ? std::string::npos : dot - start);
            at = findKey(s, at, key);
            if (at == std::string::npos || dot == std::string::npos) return at;
            start = dot + 1;
        }
        return std::string::npos;
    }
}

double jsonNumber(const std::string& json, const std::string& path, double fallback) {
    const size_t p = findPath(json, path);
    if (p == std::string::npos) return fallback;
    char* end = nullptr;
    const double v = std::strtod(json.c_str() + p, &end);
    return end == json.c_str() + p ? fallback : v;
}

bool jsonBool(const std::string& json, const std::string& path, bool fallback) {
    const size_t p = findPath(json, path);
    if (p == std::string::npos) return fallback;
    if (json.compare(p, 4, "true") == 0) return true;
    if (json.compare(p, 5, "false") == 0) return false;
    return fallback;
}

std::string jsonString(const std::string& json, const std::string& path) {
    const size_t p = findPath(json, path);
    if (p == std::string::npos || json[p] != '"') return {};
    std::string out;
    for (size_t i = p + 1; i < json.size() && json[i] != '"'; ++i) {
        if (json[i] == '\\' && i + 1 < json.size()) {
            ++i;
            out += json[i] == 'n' ? '\n' : json[i];
        } else {
            out += json[i];
        }
    }
    return out;
}

std::vector<double> jsonNumbers(const std::string& json, const std::string& path) {
    std::vector<double> out;
    size_t p = findPath(json, path);
    if (p == std::string::npos || json[p] != '[') return out;
    const size_t end = skipValue(json, p);
    ++p;
    while (p < end) {
        p = skipWs(json, p);
        char* e = nullptr;
        const double v = std::strtod(json.c_str() + p, &e);
        if (e == json.c_str() + p) break;
        out.push_back(v);
        p = skipWs(json, static_cast<size_t>(e - json.c_str()));
        if (p < end && json[p] == ',') ++p;
    }
    return out;
}

std::vector<std::string> jsonObjects(const std::string& json, const std::string& path) {
    std::vector<std::string> out;
    // path 가 비면 최상위가 배열
    size_t p = path.empty() ? skipWs(json, 0) : findPath(json, path);
    if (p == std::string::npos || p >= json.size() || json[p] != '[') return out;
    const size_t end = skipValue(json, p);
    ++p;
    while (p < end) {
        p = skipWs(json, p);
        if (p >= end || json[p] == ']') break;
        const size_t e = skipValue(json, p);
        if (json[p] == '{') out.push_back(json.substr(p, e - p));
        p = skipWs(json, e);
        if (p < end && json[p] == ',') ++p;
    }
    return out;
}

} // namespace demo

// ── C API ────────────────────────────────────────────────────────────

namespace {
    using demo::Demo;

    std::vector<std::unique_ptr<Demo>>& registry() {
        static std::vector<std::unique_ptr<Demo>> list = [] {
            std::vector<std::unique_ptr<Demo>> v;
            v.emplace_back(demo::makeRobotDemo());
            v.emplace_back(demo::makeParticleDemo());
            v.emplace_back(demo::makeJigDemo());
            v.emplace_back(demo::makeSectionDemo());
            v.emplace_back(demo::makeFloorplanDemo());
            v.emplace_back(demo::makeNavDemo());
            v.emplace_back(demo::makeAiDemo());
            return v;
        }();
        return list;
    }

    Demo* g_current = nullptr;
    std::string g_str;   // 문자열 반환 보관(다음 호출까지 유효)

    const char* keep(std::string s) { g_str = std::move(s); return g_str.c_str(); }

    demo::Control* control(int i) {
        if (!g_current || i < 0 || i >= static_cast<int>(g_current->controls.size())) return nullptr;
        return &g_current->controls[static_cast<size_t>(i)];
    }
    bool validDemo(int i) { return i >= 0 && i < static_cast<int>(registry().size()); }
}

extern "C" {

int DemoKit_DemoCount(void) { return static_cast<int>(registry().size()); }
const char* DemoKit_DemoId(int i)      { return validDemo(i) ? registry()[i]->id() : ""; }
const char* DemoKit_DemoTitle(int i)   { return validDemo(i) ? registry()[i]->title() : ""; }
const char* DemoKit_DemoSummary(int i) { return validDemo(i) ? registry()[i]->summary() : ""; }

bool DemoKit_Start(const char* demoId) {
    if (!demoId) return false;
    for (auto& d : registry()) {
        if (std::strcmp(d->id(), demoId) != 0) continue;
        if (g_current) g_current->teardown();
        demo::clearScene();
        g_current = d.get();
        g_current->status.clear();
        g_current->setup();
        g_current->rebuild();
        return true;
    }
    return false;
}

const char* DemoKit_CurrentTitle(void) { return g_current ? g_current->title() : ""; }
void DemoKit_Update(double dt) { if (g_current) g_current->update(dt); }
int  DemoKit_Revision(void) { return g_current ? g_current->revision : 0; }
const char* DemoKit_Status(void) { return g_current ? keep(g_current->status) : ""; }

int DemoKit_ControlCount(void) { return g_current ? static_cast<int>(g_current->controls.size()) : 0; }
int DemoKit_ControlKind(int i) { auto* c = control(i); return c ? c->kind : -1; }
const char* DemoKit_ControlLabel(int i) { auto* c = control(i); return c ? keep(c->label) : ""; }
const char* DemoKit_ControlUnit(int i)  { auto* c = control(i); return c ? keep(c->unit) : ""; }
float DemoKit_ControlMin(int i) { auto* c = control(i); return c ? c->min : 0; }
float DemoKit_ControlMax(int i) { auto* c = control(i); return c ? c->max : 0; }
bool  DemoKit_ControlEnabled(int i) { auto* c = control(i); return c && (!c->enabled || c->enabled()); }
float DemoKit_GetValue(int i) { auto* c = control(i); return (c && c->get) ? c->get() : 0; }

// 값 변경·버튼은 항목 목록을 바꿀 수 있다(rebuild) — 콜백 안에서 controls 가 다시 만들어져도
// 지금 콜백 객체가 살아 있게 복사본으로 부른다.
void DemoKit_SetValue(int i, float v) {
    auto* c = control(i);
    if (!c || !c->set) return;
    auto f = c->set;
    f(std::fmin(std::fmax(v, c->min), c->max));
}
void DemoKit_Press(int i) {
    auto* c = control(i);
    if (!c || !c->press || (c->enabled && !c->enabled())) return;
    auto f = c->press;
    f();
}
int DemoKit_ChoiceCount(int i) { auto* c = control(i); return c ? static_cast<int>(c->choices.size()) : 0; }
const char* DemoKit_ChoiceLabel(int i, int k) {
    auto* c = control(i);
    if (!c || k < 0 || k >= static_cast<int>(c->choices.size())) return "";
    return keep(c->choices[static_cast<size_t>(k)]);
}
const char* DemoKit_GetText(int i) { auto* c = control(i); return (c && c->text) ? keep(c->text()) : ""; }
void DemoKit_SetText(int i, const char* s) {
    auto* c = control(i);
    if (!c || !c->setText) return;
    auto f = c->setText;
    f(s ? s : "");
}

} // extern "C"
