import UIKit
import QuartzCore

// CAMetalLayer 를 backing layer 로 가지는 UIView.
// iOS 는 NSView 와 달리 .layer 를 사후에 바꿀 수 없어서, +layerClass 를 override 하는
// UIView 서브클래스가 필요하다. C++ 측 IOSNativeViewWindow 가 이걸 받아 Vulkan surface 를 만든다.
//
// 터치/제스처 매핑 (CAD 관례):
//   1손가락 드래그 → orbit  (엔진 right button = 1)
//   2손가락 드래그 → pan    (엔진 middle button = 2)
//   핀치          → zoom   (엔진 mouseWheel)
//   탭            → select (엔진 left button = 0, down+up)
final class VulkanCADMetalView: UIView {
    weak var engine: VulkanCADEngine?

    // ─── 가상 커서 (도구 사용 중) ────────────────────────────────
    // 손가락이 목표를 가려 스냅 심볼(□△○⊕)이 안 보이는 문제를 구조적으로 없앤다.
    // 커서는 손가락 위치와 **분리**돼 있고 드래그는 **상대 이동**이라, 화면 아무 데서나
    // 조작해도 된다. 데스크톱 마우스와 같은 모델이라 도구를 터치용으로 다시 만들 필요가 없다.
    private(set) var cursorMode = false
    private var cursorPoint: CGPoint = .zero          // 뷰 좌표(pt)
    private let crosshair = CAShapeLayer()
    /// 손가락 이동량 대비 커서 이동 배율. 1 미만이면 더 정밀하게 조준할 수 있다.
    private let cursorGain: CGFloat = 0.6

    func setCursorMode(_ on: Bool) {
        cursorMode = on
        if on && cursorPoint == .zero {
            cursorPoint = CGPoint(x: bounds.midX, y: bounds.midY)
        }
        crosshair.isHidden = !on
        layoutCrosshair()
        if on { sendCursorMove() }   // 스냅 미리보기가 바로 뜨도록
    }

    /// [선택] 버튼 — 현재 커서 자리를 클릭으로 확정한다.
    func commitCursorClick() {
        guard cursorMode, let engine = engine else { return }
        let p = enginePoint(cursorPoint)
        engine.mouseDown(button: MouseButton.left, x: Double(p.x), y: Double(p.y), modifiers: 0)
        engine.mouseUp(button: MouseButton.left, x: Double(p.x), y: Double(p.y), modifiers: 0)
    }

    private func sendCursorMove() {
        guard let engine = engine else { return }
        let p = enginePoint(cursorPoint)
        engine.mouseMove(x: Double(p.x), y: Double(p.y))   // 호버 = 스냅 갱신
    }

    private func layoutCrosshair() {
        // 암시적 애니메이션을 끄지 않으면 커서가 손가락을 따라오지 못하고 끌린다.
        CATransaction.begin()
        CATransaction.setDisableActions(true)
        crosshair.frame = bounds
        let r: CGFloat = 22, gap: CGFloat = 6
        let c = cursorPoint
        let path = UIBezierPath()
        path.move(to: CGPoint(x: c.x - r, y: c.y)); path.addLine(to: CGPoint(x: c.x - gap, y: c.y))
        path.move(to: CGPoint(x: c.x + gap, y: c.y)); path.addLine(to: CGPoint(x: c.x + r, y: c.y))
        path.move(to: CGPoint(x: c.x, y: c.y - r)); path.addLine(to: CGPoint(x: c.x, y: c.y - gap))
        path.move(to: CGPoint(x: c.x, y: c.y + gap)); path.addLine(to: CGPoint(x: c.x, y: c.y + r))
        path.append(UIBezierPath(arcCenter: c, radius: 3, startAngle: 0, endAngle: .pi * 2, clockwise: true))
        crosshair.path = path.cgPath
        CATransaction.commit()
    }

    // 엔진 마우스 버튼 규약
    private enum MouseButton {
        static let left: Int32 = 0
        static let right: Int32 = 1
        static let middle: Int32 = 2
    }

    override static var layerClass: AnyClass {
        return CAMetalLayer.self
    }

    var metalLayer: CAMetalLayer {
        // +layerClass 가 CAMetalLayer 라 강제 캐스팅 안전
        return self.layer as! CAMetalLayer
    }

    override init(frame: CGRect) {
        super.init(frame: frame)
        commonInit()
    }

    required init?(coder: NSCoder) {
        super.init(coder: coder)
        commonInit()
    }

    private func commonInit() {
        backgroundColor = .black
        contentScaleFactor = UIScreen.main.scale
        isMultipleTouchEnabled = true

        // 십자선 — Metal 레이어 위에 얹는다. 선 몇 개라 렌더 비용은 무시할 수준이고,
        // CAMetalLayer 와 별개 레이어라 Vulkan 렌더 루프를 건드리지 않는다.
        crosshair.strokeColor = UIColor.systemYellow.cgColor
        crosshair.fillColor = UIColor.clear.cgColor
        crosshair.lineWidth = 1.5
        crosshair.isHidden = true
        layer.addSublayer(crosshair)
        // CAMetalLayer 기본 픽셀 포맷 — MoltenVK 가 BGRA8 를 기본으로 받음
        metalLayer.pixelFormat = .bgra8Unorm
        metalLayer.framebufferOnly = false
        installGestures()
    }

    // Window 에 attach 됐을 때 / 사이즈 변경 (회전 포함) 시 호출.
    // 엔진이 살아있으면 resize 를 엔진에 위임 → C++ syncSizeFromView 가 drawableSize 갱신 +
    // resized_ 플래그 세팅 → 다음 tick 에서 swapchain/depth buffer 를 새 크기로 재생성.
    // 여기서 drawableSize 를 직접 만지면 swapchain 과 어긋나 "renderTargetHeight must be <=
    // attachment height" 검증 실패가 남 → 엔진 생성 후엔 절대 직접 설정하지 않음.
    override func layoutSubviews() {
        super.layoutSubviews()
        let scale = window?.screen.scale ?? UIScreen.main.scale
        let w = max(1, Int(bounds.size.width  * scale))
        let h = max(1, Int(bounds.size.height * scale))

        if let engine = engine, engine.isCreated {
            engine.resize(to: CGSize(width: w, height: h))
        } else {
            // 엔진 생성 전 — 초기 크기만 직접 설정
            metalLayer.contentsScale = scale
            metalLayer.drawableSize = CGSize(width: w, height: h)
        }
    }

    // ─── Gesture 설정 ───────────────────────────────────────────
    private func installGestures() {
        // ⭐ 1손가락 = 이동(pan), 2손가락 = 궤도회전.
        //    3D 뷰어 관례는 1손가락 궤도회전이지만 이건 CAD 다 — 도면을 볼 때 가장 자주 하는 건
        //    이동·줌이지 회전이 아니고, 지도/PDF 앱과 같은 감각이라 덜 어색하다. 특히 치수처럼
        //    점을 찍는 도구에서 실수로 뷰가 돌아가는 사고를 막는다.
        //    (기즈모 핸들 위에서 시작한 1손가락은 그대로 기즈모 조작 — handleOneFinger 참고)
        let oneFinger = UIPanGestureRecognizer(target: self, action: #selector(handleOneFinger(_:)))
        oneFinger.minimumNumberOfTouches = 1
        oneFinger.maximumNumberOfTouches = 1
        addGestureRecognizer(oneFinger)

        let orbit = UIPanGestureRecognizer(target: self, action: #selector(handleOrbit(_:)))
        orbit.minimumNumberOfTouches = 2
        orbit.maximumNumberOfTouches = 2

        // 3손가락 = 궤도회전, **항상**. 커서 모드에선 2손가락이 pan 으로 넘어가 회전 수단이
        // 없었다 — 점을 찍는 중에 TOP 에서 살짝 기울여 봐야 하는 일이 흔하다(사용자 요청).
        // 2손가락 인식기는 max=2 라 셋째 손가락이 닿는 순간 취소(mouseUp)되고, 그 뒤 이게
        // 시작되므로 둘이 겹치지 않는다 → sendDrag 의 버튼 래치를 공유해도 안전.
        let orbit3 = UIPanGestureRecognizer(target: self, action: #selector(handleThreeFinger(_:)))
        orbit3.minimumNumberOfTouches = 3
        orbit3.maximumNumberOfTouches = 3
        addGestureRecognizer(orbit3)
        addGestureRecognizer(orbit)

        // 핀치 = zoom (wheel)
        let pinch = UIPinchGestureRecognizer(target: self, action: #selector(handlePinch(_:)))
        addGestureRecognizer(pinch)

        // 탭 = select (left click)
        let tap = UITapGestureRecognizer(target: self, action: #selector(handleTap(_:)))
        addGestureRecognizer(tap)

        // 2손가락 궤도회전과 pinch 는 동시 인식 (CAD 의 표준 — 줌하면서 시점 조정)
        orbit.require(toFail: oneFinger) // 1손가락이 우선 판정되게
    }

    // 픽셀 좌표 변환 (엔진은 픽셀 단위 기대, top-left 원점)
    private func enginePoint(_ p: CGPoint) -> CGPoint {
        let scale = window?.screen.scale ?? UIScreen.main.scale
        return CGPoint(x: max(0, p.x) * scale, y: max(0, p.y) * scale)
    }

    // 1손가락 드래그가 시작될 때 결정된 버튼을 드래그 끝까지 유지.
    // 기즈모 핸들 위에서 시작 → left(기즈모 조작), 빈 곳 → middle(이동/pan).
    private var oneFingerButton: Int32 = MouseButton.middle

    @objc private func handleOneFinger(_ g: UIPanGestureRecognizer) {
        guard let engine = engine else { return }

        // 도구 사용 중이면 1손가락은 **커서 상대 이동**이다(손가락과 분리 → 가림 없음).
        // 이동량에 gain 을 곱해 손가락보다 천천히 움직이게 하면 픽셀 단위 조준이 된다.
        // pan 은 2손가락, 줌은 핀치로 그대로 쓸 수 있어 도구 진행 중에도 화면 조작이 가능하다.
        if cursorMode {
            let d = g.translation(in: self)
            g.setTranslation(.zero, in: self)
            cursorPoint.x = min(max(cursorPoint.x + d.x * cursorGain, 0), bounds.width)
            cursorPoint.y = min(max(cursorPoint.y + d.y * cursorGain, 0), bounds.height)
            layoutCrosshair()
            sendCursorMove()
            return
        }

        let pt = enginePoint(g.location(in: self))
        let x = Double(pt.x), y = Double(pt.y)
        switch g.state {
        case .began:
            // down 시점에 기즈모 핸들 hit-test → 버튼 결정.
            // 핸들 위가 아니면 이동(pan) — 도면에서 가장 자주 쓰는 동작.
            oneFingerButton = engine.gizmoHitTest(x: x, y: y) ? MouseButton.left
                                                              : MouseButton.middle
            engine.mouseDown(button: oneFingerButton, x: x, y: y, modifiers: 0)
        case .changed:
            engine.mouseMove(x: x, y: y)
        case .ended, .cancelled, .failed:
            engine.mouseUp(button: oneFingerButton, x: x, y: y, modifiers: 0)
        default:
            break
        }
    }

    // 2손가락 드래그 — 평소엔 궤도회전(우클릭), **커서 모드에선 pan(중클릭)**.
    //
    // 커서 모드는 1손가락이 커서에 묶여 있어 2손가락이 유일하게 남은 화면 조작 수단이다.
    // 여기서도 궤도회전을 하면 점을 찍는 내내 pan 을 할 방법이 없다(사용자 지적).
    // 점을 찍는 중에 정작 필요한 건 회전이 아니라 "화면 밖 목표를 끌어오는" pan 이고,
    // 도중에 시점이 돌면 스케치 평면 기준이 흔들려 오히려 방해가 된다. 줌은 핀치로 유지.
    @objc private func handleOrbit(_ g: UIPanGestureRecognizer) {
        sendDrag(g, button: cursorMode ? MouseButton.middle : MouseButton.right)
    }

    // 3손가락 드래그 = 궤도회전 (커서 모드 여부와 무관)
    @objc private func handleThreeFinger(_ g: UIPanGestureRecognizer) {
        sendDrag(g, button: MouseButton.right)
    }

    // 드래그 중 도구가 켜지고 꺼지면 down 과 up 의 버튼이 어긋나 엔진이 눌린 채로 남는다.
    // .began 에서 정한 버튼을 드래그 끝까지 유지한다(1손가락 oneFingerButton 과 같은 방식).
    private var twoFingerButton: Int32 = MouseButton.right

    private func sendDrag(_ g: UIPanGestureRecognizer, button: Int32) {
        guard let engine = engine else { return }
        let pt = enginePoint(g.location(in: self))
        switch g.state {
        case .began:
            twoFingerButton = button
            engine.mouseDown(button: twoFingerButton, x: Double(pt.x), y: Double(pt.y), modifiers: 0)
        case .changed:
            engine.mouseMove(x: Double(pt.x), y: Double(pt.y))
        case .ended, .cancelled, .failed:
            engine.mouseUp(button: twoFingerButton, x: Double(pt.x), y: Double(pt.y), modifiers: 0)
        default:
            break
        }
    }

    @objc private func handlePinch(_ g: UIPinchGestureRecognizer) {
        guard let engine = engine else { return }
        // 핀치 scale 변화량 → wheel deltaY. 핀치 중심에서 줌 (cursor-centered).
        let pt = enginePoint(g.location(in: self))
        // scale 은 누적값이라 매 콜백 1.0 기준 증감으로 변환. 확대(>1) = 양수 휠.
        let delta = Double(g.scale - 1.0) * 8.0   // 감도 계수 — 필요시 조정
        if abs(delta) > 0.0001 {
            engine.mouseWheel(x: Double(pt.x), y: Double(pt.y), deltaX: 0.0, deltaY: delta)
        }
        g.scale = 1.0   // 매 콜백마다 리셋 → 증분만 전달
    }

    @objc private func handleTap(_ g: UITapGestureRecognizer) {
        guard let engine = engine, g.state == .ended else { return }
        // 커서 모드에선 탭을 무시한다. 안 그러면 **손가락 위치**로 클릭이 나가
        // 커서가 가리키던 곳이 아닌 엉뚱한 점이 찍힌다(치수가 이상해지는 원인).
        // 확정은 오직 [선택] 버튼(commitCursorClick)으로만 한다.
        if cursorMode { return }
        let pt = enginePoint(g.location(in: self))
        let x = Double(pt.x), y = Double(pt.y)

        // 엔진 선택 로직은 isMouseButtonDown(LEFT) 을 매 프레임 폴링해서 rising-edge 를 본다.
        // down+up 을 같은 순간에 보내면 다음 tick 이 폴링하기 전에 이미 released 라 edge 미감지.
        // → mouseUp 을 ~3프레임(50ms) 뒤로 미뤄 최소 1 tick 동안 "눌림" 상태 유지.
        engine.mouseDown(button: MouseButton.left, x: x, y: y, modifiers: 0)
        DispatchQueue.main.asyncAfter(deadline: .now() + 0.05) { [weak engine] in
            engine?.mouseUp(button: MouseButton.left, x: x, y: y, modifiers: 0)
        }
    }
}
