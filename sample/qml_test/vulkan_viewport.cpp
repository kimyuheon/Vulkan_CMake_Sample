#include "vulkan_viewport.h"

// 엔진 C API — extern "C" 경계로 Windows/Linux/macOS에서 같은 코드를 사용한다.
#include "../../api/VulkanCAD_API.h"

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
            createEngineWindow();
        } else {
            destroyEngineWindow();
        }
    }
    QQuickItem::itemChange(change, value);
}

void VulkanViewport::geometryChange(const QRectF& newGeom, const QRectF& oldGeom) {
    QQuickItem::geometryChange(newGeom, oldGeom);
    syncGeometry();
}

void VulkanViewport::createEngineWindow() {
    if (engineCreated_ || !window()) return;

    // 네이티브 자식 창 — QQuickWindow를 부모로. winId() 호출이 OS 창 생성을 강제한다.
    nativeWindow_ = new QWindow(window());
    nativeWindow_->setFlags(Qt::FramelessWindowHint);
    nativeWindow_->create();                       // 네이티브 핸들 실체화
    const WId nativeId = nativeWindow_->winId();    // HWND / X11 XID / NSView
    nativeWindow_->setGeometry(0, 0, 1, 1);
    nativeWindow_->show();

    syncGeometry();

    const int pw = int(width()  * devicePixelRatio_);
    const int ph = int(height() * devicePixelRatio_);

    // C API 임베드 시퀀스 (WPF/MFC/iOS 와 동일 패턴):
    //   AttachView(XID) → CreateEngine → 이후 매 프레임 Tick.
    CAD_AttachView(reinterpret_cast<void*>(static_cast<uintptr_t>(nativeId)),
                   pw > 0 ? pw : 1, ph > 0 ? ph : 1);
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
        CAD_ResizeView(int(w * devicePixelRatio_), int(h * devicePixelRatio_));
    }
}

void VulkanViewport::tick() {
    if (engineCreated_) CAD_Tick();
}

bool VulkanViewport::eventFilter(QObject* watched, QEvent* event) {
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
