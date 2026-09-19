import AppKit

/// 테두리 없는 버튼 + 마우스를 올리면 배경만 살짝 밝아진다.
///
/// NSButton 의 showsBorderOnlyWhileMouseInside 는 호버 순간 베젤이 생기며 **내용 여백이 바뀌어**
/// 아이콘·글자가 1~2px 움직이고, 폭이 고정이 아니면 옆 버튼까지 밀린다(리본에서 보이던 떨림).
///
/// 강조 배경은 **draw(_:) 에서 직접 그린다.** 레이어의 backgroundColor 를 쓰지 않는 이유:
/// NSButton 의 backing layer 는 AppKit 이 관리해서 셀을 다시 그릴 때 값이 지워지거나
/// 반영이 늦어 호버가 안 보이거나 남아 있는 것처럼 보인다. 직접 그리면 그런 경로가 없다.
@MainActor
final class HoverButton: NSButton {
    private var tracking: NSTrackingArea?
    private var hovered = false { didSet { if hovered != oldValue { needsDisplay = true } } }

    override init(frame: NSRect) {
        super.init(frame: frame)
        isBordered = false
        focusRingType = .none
    }
    required init?(coder: NSCoder) { fatalError() }

    override func updateTrackingAreas() {
        super.updateTrackingAreas()
        if let tracking { removeTrackingArea(tracking) }
        let area = NSTrackingArea(rect: bounds,
                                  options: [.mouseEnteredAndExited, .activeInKeyWindow, .inVisibleRect],
                                  owner: self, userInfo: nil)
        addTrackingArea(area)
        tracking = area
        // 갱신 시점에 이미 커서가 위에 있으면 바로 반영 (탭 전환 직후 등)
        if let w = window {
            let p = convert(w.mouseLocationOutsideOfEventStream, from: nil)
            hovered = bounds.contains(p)
        }
    }

    override func mouseEntered(with event: NSEvent) {
        hovered = true
    }

    override func mouseExited(with event: NSEvent) {
        hovered = false
    }

    override func viewDidMoveToWindow() {
        super.viewDidMoveToWindow()
        if window == nil { hovered = false }
    }

    override func draw(_ dirtyRect: NSRect) {
        if isEnabled && (hovered || isHighlighted) {
            // 누르는 중이면 조금 더 진하게
            NSColor(white: 1.0, alpha: isHighlighted ? 0.18 : 0.11).setFill()
            NSBezierPath(roundedRect: bounds.insetBy(dx: 1, dy: 1), xRadius: 5, yRadius: 5).fill()
        }
        super.draw(dirtyRect)
    }
}
