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

    func flushPendingResize() {
        guard resizePending, hasRenderableSize else { return }
        resizePending = false
        engine?.resize(to: bounds.size)
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
