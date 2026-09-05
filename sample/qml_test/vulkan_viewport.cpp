#include "vulkan_viewport.h"

// 엔진 C API — extern "C" 경계로 Windows/Linux/macOS에서 같은 코드를 사용한다.
#include "../../sdk/include/VulkanCAD_API.h"

#include <QWindow>
#include <QTimer>
#include <QQuickWindow>
#include <QMouseEvent>
#include <QWheelEvent>
#include <QKeyEvent>
#include <cstdint>

namespace {
    // ── Qt → GLFW 입력 코드 변환 (엔진은 GLFW 키/버튼/수정자 컨벤션 사용) ──
    // GLFW: MOUSE_LEFT=0, RIGHT=1, MIDDLE=2
    int glfwMouseButton(Qt::MouseButton b) {
        switch (b) {
            case Qt::LeftButton:   return 0;
            case Qt::RightButton:  return 1;
            case Qt::MiddleButton: return 2;
            default:               return 0;
        }
    }

    // GLFW mods: SHIFT=0x1, CONTROL=0x2, ALT=0x4, SUPER=0x8
    int glfwMods(Qt::KeyboardModifiers m) {
        int mods = 0;
        if (m & Qt::ShiftModifier)   mods |= 0x0001;
        if (m & Qt::ControlModifier) mods |= 0x0002;
        if (m & Qt::AltModifier)     mods |= 0x0004;
        if (m & Qt::MetaModifier)    mods |= 0x0008;
        return mods;
    }

    // 문자/숫자 키는 GLFW 코드 = ASCII 대문자 = Qt::Key 값이라 그대로 통과.
    // 특수키만 개별 매핑 (GLFW 정의값 하드코딩 — 엔진 단축키/편집에 필요한 최소 집합).
    int glfwKey(int qtKey) {
        switch (qtKey) {
            case Qt::Key_Escape:    return 256;
            case Qt::Key_Return:
            case Qt::Key_Enter:     return 257;
            case Qt::Key_Backspace: return 259;
            case Qt::Key_Delete:    return 261;
            case Qt::Key_Right:     return 262;
            case Qt::Key_Left:      return 263;
            case Qt::Key_Down:      return 264;
            case Qt::Key_Up:        return 265;
            case Qt::Key_Space:     return 32;
            case Qt::Key_Control:   return 341;
            case Qt::Key_Shift:     return 340;
            case Qt::Key_Alt:       return 342;
            default:
                // A–Z / 0–9 : Qt::Key 값이 이미 GLFW(=ASCII 대문자) 와 동일
                if (qtKey >= Qt::Key_A && qtKey <= Qt::Key_Z) return qtKey;
                if (qtKey >= Qt::Key_0 && qtKey <= Qt::Key_9) return qtKey;
                return -1;   // 매핑 없음 — 무시
        }
    }
}

VulkanViewport::VulkanViewport(QQuickItem* parent) : QQuickItem(parent) {
    // 지오메트리 변화를 자식 창에 반영하기 위해 필요.
    setFlag(ItemHasContents, false);   // 우리가 QML 로 그리는 건 없음 (엔진이 별도 창에 렌더)
}

VulkanViewport::~VulkanViewport() {
    destroyEngineWindow();
}

void VulkanViewport::itemChange(ItemChange change, const ItemChangeData& value) {
    if (change == ItemSceneChange) {
        // window 에 붙거나(value.window!=null) 떨어짐(null).
        if (value.window) {
            devicePixelRatio_ = value.window->devicePixelRatio();
            // window 이동 시에도 자식 창 위치를 다시 맞춰야 함.
            connect(value.window, &QQuickWindow::xChanged, this, &VulkanViewport::syncGeometry);
            connect(value.window, &QQuickWindow::yChanged, this, &VulkanViewport::syncGeometry);
            // 닫기를 **미리** 받으려고 호스트 창에도 필터를 건다. 아래 ItemSceneChange(window==null)
            // 는 QQuickWindow 가 이미 헐린 뒤에 오고, 자식인 nativeWindow_ 의 X 창도 그때 같이
            // 사라진다. 그 사이 16ms 틱이 한 번 더 들어가면 죽은 서피스로 acquire 를 걸어
            // "failed to acquire swap chain image!" 가 뜬다(종료 때만 나던 그 한 줄).
            // isExposed() 가드로는 못 막는다 — 파괴 전에 expose 이벤트가 온다는 보장이 없다.
            // (QQuickWindow::closing 시그널은 QQuickCloseEvent 가 전방 선언뿐이라 connect 가
            //  메타타입 검사에서 막힌다. QEvent::Close 를 직접 보는 게 같은 시점이고 더 싸다.)
            value.window->installEventFilter(this);
            // 엔진 생성 조건(크기·노출)은 이 시점엔 아직 갖춰지지 않는다 —
            // tryCreateEngineWindow 주석 참고. 갖춰질 때까지 프레임마다 다시 본다.
            connect(value.window, &QQuickWindow::afterAnimating,
                    this, &VulkanViewport::tryCreateEngineWindow);
            tryCreateEngineWindow();
        } else {
            destroyEngineWindow();
        }
    }
    QQuickItem::itemChange(change, value);
}

void VulkanViewport::geometryChange(const QRectF& newGeom, const QRectF& oldGeom) {
    QQuickItem::geometryChange(newGeom, oldGeom);
    syncGeometry();
    tryCreateEngineWindow();   // 레이아웃이 처음 크기를 준 프레임이 생성 시점일 수 있다
}

// 엔진을 붙이기 전에 두 조건이 **모두** 만족돼야 한다.
//
//   1) Item 이 실제 크기를 가질 것 — ItemSceneChange 는 QML 레이아웃(RowLayout)이 돌기
//      전에 오므로 그 시점의 width()/height() 는 0 이다.
//   2) 호스트 창이 노출(map)돼 있을 것 — 매핑 전 창은 surface extent 가 0 이다.
//
// 예전엔 ItemSceneChange 에서 바로 만들었다. 그러면 1x1 짜리 미매핑 창에 스왑체인이 잡히고,
// 곧이어 레이아웃이 실제 크기를 주면서 스왑체인을 다시 만든다. X11 의 실제 리사이즈는
// 비동기라 그 사이에 낀 Tick 이 옛 extent 로 acquire 를 시도해
// "failed to acquire swap chain image!" 로 깨진다 — 타이밍에 따라 되기도 하고 안 되기도 하는
// 경주였다. 조건이 갖춰질 때까지 미루면 스왑체인을 처음부터 한 번만, 맞는 크기로 만든다.
void VulkanViewport::tryCreateEngineWindow() {
    if (engineCreated_) return;
    QQuickWindow* host = window();
    if (!host || !host->isExposed()) return;
    if (width() < 1 || height() < 1) return;

    createEngineWindow();

    if (engineCreated_)   // 매 프레임 도는 슬롯이라 성공하면 감시를 끊는다.
        disconnect(host, &QQuickWindow::afterAnimating,
                   this, &VulkanViewport::tryCreateEngineWindow);
}

void VulkanViewport::createEngineWindow() {
    if (engineCreated_ || !window()) return;

    const QPointF topLeft = mapToScene(QPointF(0, 0));
    const int w = int(width());
    const int h = int(height());

    // 네이티브 자식 창 — QQuickWindow를 부모로. winId() 호출이 OS 창 생성을 강제한다.
    // 지오메트리는 create() **전** 에 정한다: X11 창이 처음부터 최종 크기로 태어나야
    // 엔진이 surface capabilities 를 물었을 때 맞는 extent 가 나온다.
    nativeWindow_ = new QWindow(window());
    nativeWindow_->setFlags(Qt::FramelessWindowHint);
    nativeWindow_->setGeometry(int(topLeft.x()), int(topLeft.y()), w, h);
    nativeWindow_->create();                       // 네이티브 핸들 실체화
    const WId nativeId = nativeWindow_->winId();    // HWND / X11 XID / NSView
    nativeWindow_->show();

    // 엔진엔 물리 픽셀 크기 전달 (Vulkan swapchain extent 와 일치).
    const int pw = int(w * devicePixelRatio_);
    const int ph = int(h * devicePixelRatio_);

    // C API 임베드 시퀀스 (WPF/MFC/iOS 와 동일 패턴):
    //   AttachView(XID) → CreateEngine → 이후 매 프레임 Tick.
    lastPixelW_ = pw;   // 이후 syncGeometry 가 같은 크기로 재차 Resize 하지 않도록 기록
    lastPixelH_ = ph;
    CAD_AttachView(reinterpret_cast<void*>(static_cast<uintptr_t>(nativeId)), pw, ph);
    if (!CAD_CreateEngine()) {
        qWarning("[qml] CAD_CreateEngine failed");
        destroyEngineWindow();
        return;
    }
    // 호스트가 창을 소유 — 엔진의 close 요청 무시 (임베드 모드).
    CAD_SetIgnoreCloseRequest(true);
    engineCreated_ = true;

    // 자식 창에서 발생하는 입력을 가로채 CAD_On* 로 주입.
    nativeWindow_->installEventFilter(this);

    // ~60fps 렌더 틱.
    tickTimer_ = new QTimer(this);
    connect(tickTimer_, &QTimer::timeout, this, &VulkanViewport::tick);
    tickTimer_->start(16);
}

void VulkanViewport::destroyEngineWindow() {
    if (tickTimer_) {
        tickTimer_->stop();
        tickTimer_->deleteLater();
        tickTimer_ = nullptr;
    }
    if (engineCreated_) {
        CAD_DetachView();
        CAD_DestroyEngine();
        engineCreated_ = false;
    }
    tickFailed_ = false;
    if (nativeWindow_) {
        nativeWindow_->removeEventFilter(this);
        nativeWindow_->deleteLater();
        nativeWindow_ = nullptr;
    }
}

void VulkanViewport::syncGeometry() {
    if (!nativeWindow_ || !window()) return;
    // Item 의 씬 좌표(로컬 0,0 → 씬) = QQuickWindow 클라이언트 영역 기준 위치.
    // 자식 창이 QQuickWindow 를 부모로 하므로 이 좌표를 그대로 setGeometry 에 사용.
    const QPointF topLeft = mapToScene(QPointF(0, 0));
    const int x = int(topLeft.x());
    const int y = int(topLeft.y());
    const int w = int(width());
    const int h = int(height());
    if (w <= 0 || h <= 0) return;

    nativeWindow_->setGeometry(x, y, w, h);

    if (engineCreated_) {
        // 엔진엔 물리 픽셀 크기 전달 (Vulkan swapchain extent 와 일치).
        // 크기가 그대로면 부르지 않는다 — CAD_ResizeView 는 같은 값이어도 스왑체인을
        // 다시 만들고, 창 이동(xChanged/yChanged)만으로도 이 함수가 불리기 때문에
        // 그냥 두면 드래그하는 내내 스왑체인을 새로 만들게 된다.
        const int pw = int(w * devicePixelRatio_);
        const int ph = int(h * devicePixelRatio_);
        if (pw != lastPixelW_ || ph != lastPixelH_) {
            lastPixelW_ = pw;
            lastPixelH_ = ph;
            CAD_ResizeView(pw, ph);
        }
    }
}

void VulkanViewport::tick() {
    if (!engineCreated_) return;

    // 창이 최소화됐거나 아직 매핑되기 전이면 surface extent 가 0 이라 acquire 가 실패한다.
    // 그릴 수 없는 프레임은 엔진에 넘기지 않고 건너뛴다.
    if (!nativeWindow_->isExposed()) return;

    if (!CAD_Tick()) {
        // 리사이즈 도중 한두 프레임 실패하는 건 다음 프레임에 복구된다 —
        // 타이머를 멈추면 오히려 화면이 영영 죽으므로 계속 돌리되 로그만 한 번 낸다.
        if (!tickFailed_) {
            tickFailed_ = true;
            qWarning("[qml] CAD_Tick 실패 — 다음 프레임에 복구를 시도합니다.");
        }
    } else {
        tickFailed_ = false;   // 복구되면 다음 실패를 다시 알린다
    }
}

bool VulkanViewport::eventFilter(QObject* watched, QEvent* event) {
    // 호스트 창이 닫히는 순간 — 창이 실제로 헐리기 **전**이다. 여기서 정리해야 타이머가
    // 죽은 서피스를 보지 않는다. 이벤트는 먹지 않고(false) 그대로 흘려보낸다.
    if (watched == window() && event->type() == QEvent::Close) {
        destroyEngineWindow();
        return false;
    }

    if (watched != nativeWindow_ || !engineCreated_)
        return QQuickItem::eventFilter(watched, event);

    switch (event->type()) {
        case QEvent::MouseButtonPress: {
            auto* e = static_cast<QMouseEvent*>(event);
            CAD_OnMouseDown(glfwMouseButton(e->button()),
                            e->position().x() * devicePixelRatio_,
                            e->position().y() * devicePixelRatio_,
                            glfwMods(e->modifiers()));
            return true;
        }
        case QEvent::MouseButtonRelease: {
            auto* e = static_cast<QMouseEvent*>(event);
            CAD_OnMouseUp(glfwMouseButton(e->button()),
                          e->position().x() * devicePixelRatio_,
                          e->position().y() * devicePixelRatio_,
                          glfwMods(e->modifiers()));
            return true;
        }
        case QEvent::MouseMove: {
            auto* e = static_cast<QMouseEvent*>(event);
            CAD_OnMouseMove(e->position().x() * devicePixelRatio_,
                            e->position().y() * devicePixelRatio_);
            return true;
        }
        case QEvent::Wheel: {
            auto* e = static_cast<QWheelEvent*>(event);
            // Qt angleDelta 는 1/8도 단위, 120 = 한 노치. GLFW yoffset 은 노치당 ±1.
            const double dy = e->angleDelta().y() / 120.0;
            const double dx = e->angleDelta().x() / 120.0;
            CAD_OnMouseWheel(e->position().x() * devicePixelRatio_,
                             e->position().y() * devicePixelRatio_, dx, dy);
            return true;
        }
        case QEvent::KeyPress: {
            auto* e = static_cast<QKeyEvent*>(event);
            const int k = glfwKey(e->key());
            if (k >= 0) CAD_OnKeyDown(k, glfwMods(e->modifiers()));
            return true;
        }
        case QEvent::KeyRelease: {
            auto* e = static_cast<QKeyEvent*>(event);
            const int k = glfwKey(e->key());
            if (k >= 0) CAD_OnKeyUp(k, glfwMods(e->modifiers()));
            return true;
        }
        default:
            break;
    }
    return QQuickItem::eventFilter(watched, event);
}
