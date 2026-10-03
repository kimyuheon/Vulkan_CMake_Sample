// 파티클 — 프리셋 고르기 · 재생 · 설정값(발생량·수명·속도·크기·퍼짐·색) 슬라이더.
// 쓰는 API: CAD_GetParticlePreset* · CAD_CreateParticleEmitterFromPreset · CAD_SetParticlePlayback
//           CAD_Get/SetParticleSettingsJson · CAD_GetParticleLiveCount
#include "demo_internal.h"

namespace demo {
namespace {

// 색상(0~360) → RGB. 시작 색 슬라이더 하나로 불·연기·불꽃 색을 바꿔 보게.
void hueToRgb(float h, float& r, float& g, float& b) {
    const float x = 1.0f - std::fabs(std::fmod(h / 60.0f, 2.0f) - 1.0f);
    const int s = static_cast<int>(h / 60.0f) % 6;
    const float t[6][3] = {{1, x, 0}, {x, 1, 0}, {0, 1, x}, {0, x, 1}, {x, 0, 1}, {1, 0, x}};
    r = t[s][0]; g = t[s][1]; b = t[s][2];
}

class ParticleDemo : public Demo {
public:
    const char* id() const override { return "particle"; }
    const char* title() const override { return "파티클"; }
    const char* summary() const override { return "프리셋(불·연기·불꽃)을 고르고 발생량·수명·속도·크기·색을 바꿔 본다"; }

    void setup() override {
        presetNames_.clear();
        presetPaths_.clear();
        const uint32_t n = CAD_GetParticlePresetCount();
        for (uint32_t i = 0; i < n; ++i) {
            presetNames_.push_back(readString([i](char* o, int c) { return CAD_GetParticlePresetName(i, o, c); }));
            presetPaths_.push_back(readString([i](char* o, int c) { return CAD_GetParticlePresetPath(i, o, c); }));
        }
        // 바닥 판 — 크기 가늠용
        CAD_CreateBox(0, 0, -0.05f, 3.0f, 3.0f, 0.1f);
        preset_ = 0;
        playing_ = true;
        createEmitter();
        CAD_RequestSetView(3);
        CAD_RequestZoomExtents();
    }

    void update(double) override {
        status = emitter_ ? fmt("입자 %u개", CAD_GetParticleLiveCount(emitter_)) : "프리셋이 없습니다 (sdk/effects 확인)";
    }

protected:
    void buildControls() override {
        header("프리셋");
        choice("효과", presetNames_, [this] { return preset_; },
               [this](int k) { preset_ = k; createEmitter(); rebuild(); });
        toggle("재생", [this] { return playing_; }, [this](bool on) {
            playing_ = on;
            CAD_SetParticlePlayback(emitter_, on ? 1 : 0);
        });
        button("처음부터", [this] {
            CAD_SetParticlePlayback(emitter_, 2);
            if (playing_) CAD_SetParticlePlayback(emitter_, 1);
        });

        header("설정 (CAD_SetParticleSettingsJson)");
        setting("발생량", "개/초", "rate", 5, 1500);
        setting("수명", "초", "lifetime", 0.1f, 6);
        setting("속도", "m/s", "speed", 0, 6);
        setting("크기", "m", "size", 0.01f, 1.2f);
        setting("퍼짐", "", "spread", 0, 1.5f);
        slider("시작 색", "°", 0, 359, [this] { return hue_; }, [this](float h) {
            hue_ = h;
            float r, g, b;
            hueToRgb(h, r, g, b);
            CAD_SetParticleSettingsJson(emitter_, fmt("{\"start_color\":[%.3f,%.3f,%.3f]}", r, g, b).c_str());
        });
        button("프리셋 값으로", [this] { createEmitter(); rebuild(); });
        label("입자 수", [this] { return fmt("%u", emitter_ ? CAD_GetParticleLiveCount(emitter_) : 0u); });
    }

private:
    // 설정값 하나 = 슬라이더 하나. 읽기는 엔진 JSON 에서(프리셋마다 다르다).
    void setting(const char* name, const char* unit, const char* key, float lo, float hi) {
        const std::string k = key;
        slider(name, unit, lo, hi,
               [this, k] { return static_cast<float>(jsonNumber(settings_, k)); },
               [this, k](float v) {
                   CAD_SetParticleSettingsJson(emitter_, fmt("{\"%s\":%.4f}", k.c_str(), v).c_str());
                   refreshSettings();
               });
    }

    void createEmitter() {
        if (emitter_) CAD_DeleteObject(emitter_);
        emitter_ = 0;
        if (preset_ >= 0 && preset_ < static_cast<int>(presetPaths_.size()))
            emitter_ = CAD_CreateParticleEmitterFromPreset(presetPaths_[static_cast<size_t>(preset_)].c_str(), 0, 0, 0, 1.0f);
        if (!emitter_) emitter_ = CAD_CreateParticleEmitter(preset_ % 3, 0, 0, 0, 1.0f);
        CAD_ClearSelection();   // 만든 객체가 선택돼 기즈모가 뜨지 않게
        if (playing_) CAD_SetParticlePlayback(emitter_, 1);
        refreshSettings();
        const std::vector<double> c = jsonNumbers(settings_, "start_color");
        hue_ = 20;
        if (c.size() == 3) {   // RGB → 색상(대략)
            const double mx = std::fmax(c[0], std::fmax(c[1], c[2])), mn = std::fmin(c[0], std::fmin(c[1], c[2]));
            if (mx - mn > 1e-4) {
                double h = mx == c[0] ? std::fmod((c[1] - c[2]) / (mx - mn), 6.0)
                         : mx == c[1] ? (c[2] - c[0]) / (mx - mn) + 2 : (c[0] - c[1]) / (mx - mn) + 4;
                hue_ = static_cast<float>(std::fmod(h * 60 + 360, 360));
            }
        }
    }

    void refreshSettings() {
        const uint32_t id = emitter_;
        settings_ = readString([id](char* o, int c) { return CAD_GetParticleSettingsJson(id, o, c); });
    }

    std::vector<std::string> presetNames_, presetPaths_;
    int preset_ = 0;
    bool playing_ = true;
    uint32_t emitter_ = 0;
    std::string settings_;
    float hue_ = 20;
};

} // namespace

Demo* makeParticleDemo() { return new ParticleDemo(); }

} // namespace demo
