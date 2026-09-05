// Android 용 UI 스텁 — 데스크톱(ImGui/GLFW) 전용 클래스들의 no-op 구현.
// CMake 에서 lot_ui_manager/lot_main_menu/lot_toolbar/lot_status_bar/lot_properties_panel/
// ui_input_capture 를 제외하므로, 그 심볼들을 여기서 빈 구현으로 채운다.
// (link error 가 지목하는 것부터 추가. iOS 의 ios_stubs.cpp 와 같은 역할, 현재 헤더 시그니처에 맞춤.)

#include "lot_ui_manager.h"
#include "lot_ai_window.h"
#include "lot_status_bar.h"
#include "lot_file_dialog.h"
#include "ui_input_capture.h"
#include "ui_clipboard_bridge.h"

#include <functional>
#include <string>

namespace lot {

    // ── lot_file_dialog — 데스크톱 네이티브 다이얼로그. Android 는 SAF/Intent 로
    //    따로 이어야 한다(미구현). 빈 경로 = 사용자가 취소한 것과 같은 처리. ──
    std::string openModelFileDialog(IWindow*) { return {}; }
    std::string saveModelFileDialog(IWindow*, const std::string&) { return {}; }
    std::string openImageFileDialog(IWindow*) { return {}; }
    std::string saveImageFileDialog(IWindow*, const std::string&) { return {}; }

    // ── LotStatusBar — 하단 명령행/상태바는 데스크톱 ImGui 전용 ──
    void LotStatusBar::setPrompt(const std::string&) {}
    void LotStatusBar::appendTranscript(const std::string&) {}

    // ── ui_input_capture — ImGui 없음 → 항상 false (모든 입력이 3D 뷰로 흐름) ──
    bool uiWantsMouseCapture()    { return false; }
    bool uiWantsKeyboardCapture() { return false; }
    // 모바일은 ImGui 데스크톱 백엔드가 없다 — 클립보드는 iOS UIPasteboard /
    // Android ClipboardManager 로 따로 이어야 한다 (미구현).
    // 모바일엔 ImGui 백엔드가 없다 — 호스트가 등록한 처리기로 넘긴다(ui_clipboard_bridge).
    void uiSetClipboardText(const std::string& utf8) { uiClipboardWrite(utf8); }
    std::string uiGetClipboardText() { return uiPendingClipboardText(); }

    // ── LotAIWindow — 데스크톱 창(NSAlert/ImGui) 의존이라 제외. FirstApp::tick 이 부르므로 no-op. ──
    void LotAIWindow::render(LotAIClient& /*client*/, LotAILauncher& /*launcher*/) {}
    void LotAIWindow::setPrompt(const std::string&) {}
    void LotAIWindow::setSubmitRouter(std::function<bool(const std::string&)>) {}
    void LotAIWindow::setEscapeHandler(std::function<void()>) {}
    void LotAIWindow::setOrthoToggleHandler(std::function<void()>) {}

    // ── LotUiManager — ImGui 도킹/메뉴/툴바 전부 no-op ──
    LotUiManager::LotUiManager(LotDevice& device, IWindow& window, LotRenderer& renderer)
        : lotDevice{device}, lotWindow{window}, lotRenderer{renderer} {}
    LotUiManager::~LotUiManager() = default;

    void LotUiManager::beginFrame() {}
    void LotUiManager::setupDockSpace() {}
    void LotUiManager::render(const LotCamera& /*camera*/, bool& /*isPerspective*/, int& /*requestedView*/,
                              bool& /*requestOpenFile*/, bool& /*requestSelectAll*/,
                              bool& /*aiPromptOpen*/,
                              LotToolbar::ToolAction& /*outToolbarAction*/,
                              bool /*orthoTrackingOn*/,
                              bool& /*outOrthoToggleRequested*/,
                              int /*currentLayout*/, int& /*requestedLayout*/,
                              LotGameObject::Map& /*gameObjects*/,
                              ObjectSelectionManager& /*selectionManager*/,
                              bool /*commandLineAutoFocus*/,
                              LotStatusBar::CommandLineResult& /*outCmd*/) {}
    void LotUiManager::endFrame(VkCommandBuffer /*commandBuffer*/) {}
    void LotUiManager::buildViewportGizmo(const LotCamera& /*camera*/,
                                          float /*vpX*/, float /*vpY*/, float /*vpW*/, float /*vpH*/) {}
    void LotUiManager::discardFrame() {}

    // init/cleanup 은 private — 참조되면 추가.
    void LotUiManager::init() {}
    void LotUiManager::cleanup() {}

} // namespace lot
