// AI — (1) 말로 시키기: OpenAI 호환 서버(로컬 llama-server 등)에 프롬프트를 보내고 결과 액션을 장면에 적용
//      (2) 서버 없이: 액션 JSON 을 바로 실행 — 호스트가 자기 LLM(Claude API 등)을 부를 때의 경로.
// 쓰는 API: CAD_AiSetEndpoint · CAD_AiStartLocalServer · CAD_AiSubmit · CAD_AiGetStatus · CAD_AiGetLastAnswer
//           CAD_AiGetNotes · CAD_AiCancel · CAD_RunAiActionsJson
#include "demo_internal.h"

namespace demo {
namespace {

struct Example { const char* name; const char* json; };

// lot_ai_prompt.h 의 액션 계약 그대로 — 이걸 LLM 이 만들어 준다고 보면 된다.
const Example kExamples[] = {
    {"방 4×3m",
     "[{\"type\":\"create_room\",\"origin\":[0,0,0],\"width\":4,\"depth\":3,\"height\":2.4,\"wall_thickness\":0.15,\"unit\":\"m\"},"
     "{\"type\":\"view_control\",\"op\":\"iso\"},{\"type\":\"view_control\",\"op\":\"zoom_extents\"}]"},
    {"도형 세 개",
     "[{\"type\":\"create_primitive\",\"shape\":\"cube\",\"position\":[-2,0,0.5],\"scale\":[1,1,1],\"color\":[0.9,0.3,0.2]},"
     "{\"type\":\"create_primitive\",\"shape\":\"cylinder\",\"position\":[0,0,0],\"radius\":0.5,\"height\":1.2,\"color\":[0.2,0.6,0.9]},"
     "{\"type\":\"create_primitive\",\"shape\":\"sphere\",\"position\":[2,0,0],\"radius\":0.6,\"color\":[0.3,0.8,0.3]},"
     "{\"type\":\"view_control\",\"op\":\"zoom_extents\"}]"},
    {"원 전부 빨갛게",
     "[{\"type\":\"draw_sketch\",\"shape\":\"circle\",\"center\":[0,3,0],\"radius\":0.8},"
     "{\"type\":\"draw_sketch\",\"shape\":\"circle\",\"center\":[2,3,0],\"radius\":0.5},"
     "{\"type\":\"edit_selected\",\"op\":\"color\",\"filter\":{\"kind\":\"circle\"},\"color\":[1,0,0]},"
     "{\"type\":\"view_control\",\"op\":\"zoom_extents\"}]"},
};

class AiDemo : public Demo {
public:
    const char* id() const override { return "ai"; }
    const char* title() const override { return "AI"; }
    const char* summary() const override { return "말로 시키면 장면을 만든다 — LLM 서버 연결, 또는 서버 없이 액션 JSON 바로 실행"; }

    void setup() override {
        CAD_RequestSetView(3);
        CAD_RequestZoomExtents();
    }

    void update(double) override {
        const int st = CAD_AiGetStatus();   // 2·3 은 한 번만 — 놓치지 않게 매 프레임
        if (st == 1) phase_ = "생각하는 중…";
        else if (st == 2) {
            phase_ = "완료";
            answer_ = readString([](char* o, int c) { return CAD_AiGetLastAnswer(o, c); });
            notes_ = readString([](char* o, int c) { return CAD_AiGetNotes(o, c); });
        } else if (st == 3) {
            phase_ = "오류: " + lastError();
        }
        busy_ = st == 1;
        status = "AI: " + phase_;
    }

    void teardown() override { CAD_AiCancel(); }

protected:
    void buildControls() override {
        header("LLM 서버 (OpenAI 호환 /v1/chat/completions)");
        textField("주소", [this] { return url_; }, [this](const std::string& s) { url_ = s; });
        button("연결", [this] {
            phase_ = CAD_AiSetEndpoint(url_.c_str(), nullptr) ? "연결 확인 중 (1초쯤)" : "연결 실패: " + lastError();
        });
        button("이 컴퓨터에서 서버 켜기", [this] {
            phase_ = CAD_AiStartLocalServer() ? "로컬 서버 시작 — 모델 로딩을 기다리세요" : "시작 못 함: " + lastError();
        });

        header("말로 시키기 (CAD_AiSubmit)");
        textField("요청", [this] { return prompt_; }, [this](const std::string& s) { prompt_ = s; });
        button("보내기", [this] {
            answer_.clear(); notes_.clear();
            phase_ = CAD_AiSubmit(prompt_.c_str()) ? "보냄" : "보내지 못함: " + lastError();
        }, [this] { return !busy_; });
        button("취소", [] { CAD_AiCancel(); }, [this] { return busy_; });
        label("답", [this] { return answer_.empty() ? std::string("—") : answer_; });
        label("결과", [this] { return notes_.empty() ? std::string("—") : notes_; });

        header("서버 없이 — 액션 JSON 바로 (CAD_RunAiActionsJson)");
        std::vector<std::string> names;
        for (const Example& e : kExamples) names.push_back(e.name);
        choice("예시", names, [this] { return example_; }, [this](int k) { example_ = k; });
        button("실행", [this] {
            const char* j = kExamples[example_].json;
            notes_ = readString([j](char* o, int c) { return CAD_RunAiActionsJson(j, o, c); });
            if (notes_.empty()) notes_ = "실패: " + lastError();
            phase_ = "액션 실행됨";
        });
        button("장면 비우기", [] { clearScene(); });
    }

private:
    std::string url_ = "http://127.0.0.1:8080";
    std::string prompt_ = "가로 4m 세로 3m 방을 만들고 가운데에 원기둥 하나 놓아 줘";
    std::string phase_ = "대기", answer_, notes_;
    int example_ = 0;
    bool busy_ = false;
};

} // namespace

Demo* makeAiDemo() { return new AiDemo(); }

} // namespace demo
