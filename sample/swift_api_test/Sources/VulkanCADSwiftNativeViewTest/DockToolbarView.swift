import AppKit

enum DockSide: String {
    case left, right
}

@MainActor
protocol ToolbarDockDelegate: AnyObject {
    /// 손잡이를 끌어 놓았거나 메뉴에서 골랐다 — 이 띠를 그쪽에 붙여 달라.
    func toolbarStrip(_ strip: ToolbarStripView, requestDock side: DockSide)
    func toolbarStripRequestHide(_ strip: ToolbarStripView)
    /// 우클릭 메뉴 아랫부분 — 전체 도구모음 켜고 끄기 목록 등을 델리게이트가 채운다.
    func toolbarStripExtraMenuItems(_ strip: ToolbarStripView) -> [NSMenuItem]
}

/// 이름 붙은 도구모음 하나("그리기", "수정" …). 세로 띠이며 맨 위에 손잡이가 있다.
///
/// 도킹: 손잡이를 **창 반대편으로 끌어 놓으면** 그쪽에 붙고, 우클릭 메뉴로도 옮기거나 숨긴다.
/// 어느 쪽에 있는지는 이 뷰가 모른다 — 실제 재배치는 델리게이트(AppDelegate)가 한다.
@MainActor
final class ToolbarStripView: NSView {
    static let width: CGFloat = 46
    let definition: ToolbarDefinition
    weak var delegate: ToolbarDockDelegate?

    private let grip = ToolbarGripView()
    private let stack = NSStackView()

    init(definition: ToolbarDefinition, target: AnyObject, action: Selector) {
        self.definition = definition
        super.init(frame: .zero)
        wantsLayer = true
        layer?.backgroundColor = NSColor(white: 0.14, alpha: 1).cgColor
        layer?.borderColor = NSColor(white: 0.08, alpha: 1).cgColor
        layer?.borderWidth = 0.5
        setup(target: target, action: action)
    }
    required init?(coder: NSCoder) { fatalError() }

    private func setup(target: AnyObject, action: Selector) {
        translatesAutoresizingMaskIntoConstraints = false
        widthAnchor.constraint(equalToConstant: Self.width).isActive = true

        grip.owner = self
        grip.toolTip = "\(definition.title) — 끌어서 반대편에 도킹, 우클릭으로 메뉴"
        grip.translatesAutoresizingMaskIntoConstraints = false
        addSubview(grip)

        // 버튼이 창 높이보다 많으면 세로 스크롤
        let scroll = NSScrollView()
        scroll.drawsBackground = false
        scroll.hasVerticalScroller = true
        scroll.hasHorizontalScroller = false
        scroll.autohidesScrollers = true
        scroll.borderType = .noBorder
        scroll.translatesAutoresizingMaskIntoConstraints = false
        addSubview(scroll)

        stack.orientation = .vertical
        stack.alignment = .centerX
        stack.spacing = 1
        stack.edgeInsets = NSEdgeInsets(top: 3, left: 0, bottom: 8, right: 0)
        stack.translatesAutoresizingMaskIntoConstraints = false
        definition.tools.forEach { stack.addArrangedSubview(makeButton($0, target: target, action: action)) }
        scroll.documentView = stack

        NSLayoutConstraint.activate([
            grip.topAnchor.constraint(equalTo: topAnchor),
            grip.leadingAnchor.constraint(equalTo: leadingAnchor),
            grip.trailingAnchor.constraint(equalTo: trailingAnchor),
            grip.heightAnchor.constraint(equalToConstant: 18),

            scroll.topAnchor.constraint(equalTo: grip.bottomAnchor),
            scroll.leadingAnchor.constraint(equalTo: leadingAnchor),
            scroll.trailingAnchor.constraint(equalTo: trailingAnchor),
            scroll.bottomAnchor.constraint(equalTo: bottomAnchor),

            stack.topAnchor.constraint(equalTo: scroll.contentView.topAnchor),
            stack.leadingAnchor.constraint(equalTo: scroll.contentView.leadingAnchor),
            stack.trailingAnchor.constraint(equalTo: scroll.contentView.trailingAnchor),
        ])
    }

    /// 아이콘만 있는 작은 버튼. 이름과 설명은 툴팁으로, 색은 계열별로.
    private func makeButton(_ tool: CADTool, target: AnyObject, action: Selector) -> NSButton {
        let btn = NSButton(image: NSImage(), target: target, action: action)
        ToolIcons.apply(tool, to: btn, pointSize: 16)
        btn.identifier = NSUserInterfaceItemIdentifier(tool.id)
        btn.imagePosition = .imageOnly
        btn.imageScaling = .scaleProportionallyDown
        btn.bezelStyle = .regularSquare
        btn.showsBorderOnlyWhileMouseInside = true
        btn.toolTip = "\(tool.title) — \(tool.tip)"
        btn.translatesAutoresizingMaskIntoConstraints = false
        NSLayoutConstraint.activate([
            btn.widthAnchor.constraint(equalToConstant: 36),
            btn.heightAnchor.constraint(equalToConstant: 32),
        ])
        return btn
    }

    // MARK: - 손잡이가 부르는 것들

    fileprivate func gripDropped(atWindowPoint p: NSPoint) {
        guard let content = window?.contentView else { return }
        let local = content.convert(p, from: nil)
        delegate?.toolbarStrip(self, requestDock: local.x > content.bounds.midX ? .right : .left)
    }

    fileprivate func showContextMenu(with event: NSEvent) {
        let menu = NSMenu()
        let title = NSMenuItem(title: "「\(definition.title)」 도구모음", action: nil, keyEquivalent: "")
        title.isEnabled = false
        menu.addItem(title)
        menu.addItem(withTitle: "왼쪽에 도킹",   action: #selector(dockLeft),  keyEquivalent: "").target = self
        menu.addItem(withTitle: "오른쪽에 도킹", action: #selector(dockRight), keyEquivalent: "").target = self
        menu.addItem(withTitle: "숨기기",        action: #selector(hideSelf),  keyEquivalent: "").target = self
        if let extra = delegate?.toolbarStripExtraMenuItems(self), !extra.isEmpty {
            menu.addItem(.separator())
            extra.forEach { menu.addItem($0) }
        }
        NSMenu.popUpContextMenu(menu, with: event, for: grip)
    }

    @objc private func dockLeft()  { delegate?.toolbarStrip(self, requestDock: .left) }
    @objc private func dockRight() { delegate?.toolbarStrip(self, requestDock: .right) }
    @objc private func hideSelf()  { delegate?.toolbarStripRequestHide(self) }
}

/// 창 왼쪽/오른쪽의 도킹 영역. 띠를 가로로 나란히 담고, 비면 스스로 숨는다
/// (부모 NSStackView 가 숨은 뷰를 레이아웃에서 빼 주므로 폭이 0 이 된다).
@MainActor
final class DockAreaView: NSView {
    let side: DockSide
    private let row = NSStackView()

    var strips: [ToolbarStripView] {
        row.arrangedSubviews.compactMap { $0 as? ToolbarStripView }
    }

    init(side: DockSide) {
        self.side = side
        super.init(frame: .zero)
        translatesAutoresizingMaskIntoConstraints = false
        row.orientation = .horizontal
        row.alignment = .height
        row.spacing = 0
        row.translatesAutoresizingMaskIntoConstraints = false
        addSubview(row)
        NSLayoutConstraint.activate([
            row.leadingAnchor.constraint(equalTo: leadingAnchor),
            row.trailingAnchor.constraint(equalTo: trailingAnchor),
            row.topAnchor.constraint(equalTo: topAnchor),
            row.bottomAnchor.constraint(equalTo: bottomAnchor),
        ])
        isHidden = true
    }
    required init?(coder: NSCoder) { fatalError() }

    func add(_ strip: ToolbarStripView) {
        row.addArrangedSubview(strip)
        isHidden = false
    }

    func remove(_ strip: ToolbarStripView) {
        guard strip.superview === row else { return }
        row.removeArrangedSubview(strip)
        strip.removeFromSuperview()
        isHidden = strips.isEmpty
    }
}

/// 띠 맨 위의 손잡이. 점 여섯 개를 그리고, 끌기와 우클릭을 받는다.
@MainActor
private final class ToolbarGripView: NSView {
    weak var owner: ToolbarStripView?
    private var dragStart: NSPoint?

    override var mouseDownCanMoveWindow: Bool { false }

    override func draw(_ dirtyRect: NSRect) {
        NSColor(white: 0.45, alpha: 1).setFill()
        let cx = bounds.midX, cy = bounds.midY
        for row in -1...1 {
            for col in [-3.0, 3.0] {
                let r = NSRect(x: cx + col - 1.5, y: cy + CGFloat(row) * 4.5 - 1.5, width: 3, height: 3)
                NSBezierPath(ovalIn: r).fill()
            }
        }
    }

    override func resetCursorRects() {
        addCursorRect(bounds, cursor: .openHand)
    }

    override func mouseDown(with event: NSEvent) {
        dragStart = event.locationInWindow
        NSCursor.closedHand.push()
    }

    override func mouseUp(with event: NSEvent) {
        NSCursor.pop()
        defer { dragStart = nil }
        guard let start = dragStart else { return }
        // 살짝 움직인 건 클릭으로 본다 — 창 폭의 1/4 이상 끌었을 때만 도킹 요청
        let moved = abs(event.locationInWindow.x - start.x)
        guard let width = window?.contentView?.bounds.width, moved > width * 0.25 else { return }
        owner?.gripDropped(atWindowPoint: event.locationInWindow)
    }

    override func rightMouseDown(with event: NSEvent) {
        owner?.showContextMenu(with: event)
    }
}
