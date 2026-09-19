import AppKit

final class VulkanCADView: NSView {
    weak var engine: VulkanCADEngine?
    private var trackingAreaRef: NSTrackingArea?
    private var resizePending = false

    private enum MouseButton {
        static let left: Int32 = 0
        static let right: Int32 = 1
        static let middle: Int32 = 2
    }

    private enum Modifier {
        static let shift: Int32 = 0x0001
        static let control: Int32 = 0x0002
        static let alt: Int32 = 0x0004
        static let superKey: Int32 = 0x0008
    }

    private enum EngineKey {
        static let space: Int32 = 32
        static let key0: Int32 = 48
        static let key1: Int32 = 49
        static let key2: Int32 = 50
        static let key3: Int32 = 51
        static let key4: Int32 = 52
        static let keyB: Int32 = 66
        static let keyO: Int32 = 79
        static let keyR: Int32 = 82
        static let keyS: Int32 = 83
        static let keyT: Int32 = 84
        static let escape: Int32 = 256
        static let enter: Int32 = 257
        static let tab: Int32 = 258
        static let backspace: Int32 = 259
        static let delete: Int32 = 261
        static let right: Int32 = 262
        static let left: Int32 = 263
        static let down: Int32 = 264
        static let up: Int32 = 265
    }

    override var wantsUpdateLayer: Bool {
        true
    }

    override var acceptsFirstResponder: Bool {
        true
    }

    override func acceptsFirstMouse(for event: NSEvent?) -> Bool {
        true
    }

    override func updateTrackingAreas() {
        super.updateTrackingAreas()

        if let trackingAreaRef {
            removeTrackingArea(trackingAreaRef)
        }

        let options: NSTrackingArea.Options = [
            .activeInKeyWindow,
            .mouseMoved,
            .enabledDuringMouseDrag,
            .inVisibleRect
        ]
        let area = NSTrackingArea(rect: bounds, options: options, owner: self, userInfo: nil)
        addTrackingArea(area)
        trackingAreaRef = area
    }

    override func setFrameSize(_ newSize: NSSize) {
        super.setFrameSize(newSize)
        notifyEngineResize()
    }

    override func viewDidMoveToWindow() {
        super.viewDidMoveToWindow()
        notifyEngineResize()
    }

    override func viewDidChangeBackingProperties() {
        super.viewDidChangeBackingProperties()
        notifyEngineResize()
    }

    func notifyEngineResize() {
        guard engine?.isCreated ?? false else { return }
        resizePending = true
    }

    var hasRenderableSize: Bool {
        bounds.width >= 2.0 && bounds.height >= 2.0
    }

    /// 마지막으로 엔진에 보낸 픽셀 크기. 매 틱 실제 크기와 대조해 어긋나 있으면 다시 보낸다.
    private var lastSentPixelSize = CGSize.zero

    /// 크기 변경을 엔진에 전달한다. 매 프레임(틱) 앞에서 불린다.
    ///
    /// resizePending 플래그만 믿지 않고 **실제 픽셀 크기와 마지막 전송값, 그리고 Metal 레이어의
    /// drawableSize** 를 매번 대조한다. 어떤 경로로든(레이아웃 지연, 화면 이동, 놓친 알림) 한 번
    /// 어긋나면 엔진이 이전 크기로 그려 옆에 빈 띠가 남는데, 이렇게 하면 다음 틱에 스스로 맞춰진다.
    /// 비교만 하는 거라 비용은 없다.
    func flushPendingResize() {
        guard hasRenderableSize, engine?.isCreated ?? false else { return }
        let scale = window?.backingScaleFactor ?? NSScreen.main?.backingScaleFactor ?? 1.0
        let pixel = CGSize(width: (bounds.width * scale).rounded(), height: (bounds.height * scale).rounded())
        let layerMismatch = (layer as? CAMetalLayer).map { $0.drawableSize != pixel } ?? false
        if resizePending || pixel != lastSentPixelSize || layerMismatch {
            resizePending = false
            lastSentPixelSize = pixel
            engine?.resize(to: bounds.size)
        }
        restoreLayerPosition()
    }

    /// 엔진의 macOS 호스트 창 코드가 크기를 맞출 때 `layer.frame = view.bounds` 로 써 버려,
    /// 뷰가 (138,30) 에 있어도 레이어는 부모의 (0,0) 으로 밀린다. 그러면 화면이 왼쪽 도구모음 폭만큼
    /// 왼쪽·아래로 어긋나 오른쪽에 그 폭의 빈 띠가 생기고 명령행 왼쪽이 레이어에 덮인다.
    /// 레이어 백킹 뷰의 레이어 위치는 AppKit 소유라 뷰 프레임과 같아야 한다 — 어긋나 있으면 되돌린다.
    /// (엔진 쪽도 같이 고쳤지만, 구버전 dylib 과 붙어도 안전하도록 호스트에서 한 번 더 지킨다.)
    private func restoreLayerPosition() {
        guard let layer, layer.frame != frame else { return }
        layer.frame = frame
    }

    override func mouseDown(with event: NSEvent) {
        sendMouseDown(event, button: MouseButton.left)
    }

    override func mouseDragged(with event: NSEvent) {
        sendMouseMove(event)
    }

    override func mouseUp(with event: NSEvent) {
        sendMouseUp(event, button: MouseButton.left)
    }

    override func rightMouseDown(with event: NSEvent) {
        sendMouseDown(event, button: MouseButton.right)
    }

    override func rightMouseDragged(with event: NSEvent) {
        sendMouseMove(event)
    }

    override func rightMouseUp(with event: NSEvent) {
        sendMouseUp(event, button: MouseButton.right)
    }

    override func otherMouseDown(with event: NSEvent) {
        sendMouseDown(event, button: mappedOtherMouseButton(event))
    }

    override func otherMouseDragged(with event: NSEvent) {
        sendMouseMove(event)
    }

    override func otherMouseUp(with event: NSEvent) {
        sendMouseUp(event, button: mappedOtherMouseButton(event))
    }

    override func mouseMoved(with event: NSEvent) {
        sendMouseMove(event)
    }

    override func scrollWheel(with event: NSEvent) {
        guard engine?.isCreated ?? false else { return }
        let point = enginePoint(for: event)
        engine?.mouseWheel(
            x: Double(point.x),
            y: Double(point.y),
            deltaX: Double(event.scrollingDeltaX),
            deltaY: Double(event.scrollingDeltaY))
    }

    override func keyDown(with event: NSEvent) {
        guard engine?.isCreated ?? false else { return }
        guard let key = engineKey(for: event) else {
            super.keyDown(with: event)
            return
        }
        engine?.keyDown(key, modifiers: modifiers(for: event))
    }

    override func keyUp(with event: NSEvent) {
        guard engine?.isCreated ?? false else { return }
        guard let key = engineKey(for: event) else {
            super.keyUp(with: event)
            return
        }
        engine?.keyUp(key, modifiers: modifiers(for: event))
    }

    private func sendMouseDown(_ event: NSEvent, button: Int32) {
        guard engine?.isCreated ?? false else { return }
        window?.makeFirstResponder(self)
        let point = enginePoint(for: event)
        engine?.mouseDown(
            button: button,
            x: Double(point.x),
            y: Double(point.y),
            modifiers: modifiers(for: event))
    }

    private func sendMouseUp(_ event: NSEvent, button: Int32) {
        guard engine?.isCreated ?? false else { return }
        let point = enginePoint(for: event)
        engine?.mouseUp(
            button: button,
            x: Double(point.x),
            y: Double(point.y),
            modifiers: modifiers(for: event))
    }

    private func sendMouseMove(_ event: NSEvent) {
        guard engine?.isCreated ?? false else { return }
        let point = enginePoint(for: event)
        engine?.mouseMove(x: Double(point.x), y: Double(point.y))
    }

    private func enginePoint(for event: NSEvent) -> CGPoint {
        let local = convert(event.locationInWindow, from: nil)
        let scale = window?.backingScaleFactor ?? NSScreen.main?.backingScaleFactor ?? 1.0
        let x = max(0.0, local.x) * scale
        let y = max(0.0, bounds.height - local.y) * scale
        return CGPoint(x: x, y: y)
    }

    private func modifiers(for event: NSEvent) -> Int32 {
        var result: Int32 = 0
        let flags = event.modifierFlags
        if flags.contains(.shift) { result |= Modifier.shift }
        if flags.contains(.control) { result |= Modifier.control }
        if flags.contains(.option) { result |= Modifier.alt }
        if flags.contains(.command) { result |= Modifier.superKey }
        return result
    }

    private func mappedOtherMouseButton(_ event: NSEvent) -> Int32 {
        event.buttonNumber == 2 ? MouseButton.middle : Int32(event.buttonNumber)
    }

    private func engineKey(for event: NSEvent) -> Int32? {
        switch event.keyCode {
            case 18: return EngineKey.key1
            case 19: return EngineKey.key2
            case 20: return EngineKey.key3
            case 21: return EngineKey.key4
            case 29: return EngineKey.key0
            case 11: return EngineKey.keyB
            case 31: return EngineKey.keyO
            case 15: return EngineKey.keyR
            case 1:  return EngineKey.keyS
            case 17: return EngineKey.keyT
            case 36, 76: return EngineKey.enter
            case 48: return EngineKey.tab
            case 49: return EngineKey.space
            case 51: return EngineKey.backspace
            case 53: return EngineKey.escape
            case 117: return EngineKey.delete
            case 123: return EngineKey.left
            case 124: return EngineKey.right
            case 125: return EngineKey.down
            case 126: return EngineKey.up
            default:
                guard let character = event.charactersIgnoringModifiers?.uppercased().first else {
                    return nil
                }
                if let scalar = character.unicodeScalars.first, scalar.isASCII {
                    return Int32(scalar.value)
                }
                return nil
        }
    }
}
