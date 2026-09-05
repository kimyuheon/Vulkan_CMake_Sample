#pragma once

// VulkanViewport — QML Item 위치에 엔진의 Vulkan 렌더를 얹는 브릿지.
//
// Qt 6.4 에는 QQuickRhiItem(6.7+) 이 없고, 우리 엔진은 자체 VkInstance/Device 를 소유하므로
// QML 씬그래프에 직접 합성하는 대신 "네이티브 자식 창 오버레이" 패턴을 쓴다:
//   1) QQuickItem 이 window 에 붙으면 네이티브 자식 QWindow 를 만든다.
//   2) winId(HWND/XID/NSView)를 CAD_AttachView → CAD_CreateEngine 으로 엔진에 넘긴다.
//   3) 이 Item 의 씬 좌표(geometry)를 추적해 자식 창을 같은 위치/크기로 이동 → QML 레이아웃과 정합.
//   4) QTimer(~60fps) 로 CAD_Tick(), 마우스/키 이벤트를 CAD_On* 로 주입.
//
// 한계(오버레이 방식): 네이티브 창이 항상 QML 위에 그려져 3D 뷰 위에 QML 오버레이를 겹칠 수 없다.
// 툴바/패널을 3D 영역 "바깥"에 배치하면 문제 없음 (일반적인 CAD 레이아웃).

#include <QQuickItem>

class QWindow;
class QTimer;

class VulkanViewport : public QQuickItem {
    Q_OBJECT
    QML_ELEMENT   // QML 에서 <VulkanViewport> 로 인스턴스화 가능 (qt_add_qml_module)

public:
    explicit VulkanViewport(QQuickItem* parent = nullptr);
    ~VulkanViewport() override;

protected:
    // window 진입/이탈 감지 → 네이티브 창 생성/파괴
    void itemChange(ItemChange change, const ItemChangeData& value) override;
    // 위치/크기 변화 → 자식 창 지오메트리 갱신
    void geometryChange(const QRectF& newGeom, const QRectF& oldGeom) override;
    // 네이티브 자식 창에서 올라오는 입력을 CAD_On* 로 변환
    bool eventFilter(QObject* watched, QEvent* event) override;

private slots:
    void tick();                // QTimer → CAD_Tick
    void syncGeometry();        // Item 씬 좌표 → 자식 창 위치/크기

private:
    void createEngineWindow();  // 네이티브 창 + 엔진 attach + create
    void destroyEngineWindow(); // detach + destroy + 창 파괴

    QWindow* nativeWindow_{nullptr};  // 엔진이 렌더하는 네이티브 자식 창
    QTimer*  tickTimer_{nullptr};
    bool     engineCreated_{false};
    double   devicePixelRatio_{1.0};
};
