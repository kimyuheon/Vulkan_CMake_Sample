import AppKit

enum DockSide: String {
    case left, right
}

@MainActor
protocol ToolbarDockDelegate: AnyObject {
    /// 손잡이를 끌어 놓았거나 메뉴에서 골랐다 — 이 띠를 그쪽에 붙여 달라.
    /// index 가 있으면 그 자리에 끼우고(드래그로 순서 바꾸기), nil 이면 끝에 붙인다.
    func toolbarStrip(_ strip: ToolbarStripView, requestDock side: DockSide, at index: Int?)
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
        setup(target: target, action: action)
    }
    required init?(coder: NSCoder) { fatalError() }

    // 배경·테두리는 updateLayer 에서 매번 다시 칠한다.
    // 띠가 도킹 영역 사이를 오가며 창에서 잠시 빠질 때 AppKit 이 backing layer 를 새로 만들 수 있고,
    // 그러면 init 에서 layer 에 직접 넣은 색은 사라져 창 배경(회색)이 그대로 비친다.
    override var wantsUpdateLayer: Bool { true }
    override func updateLayer() {
        layer?.backgroundColor = NSColor(white: 0.14, alpha: 1).cgColor
        layer?.borderColor = NSColor(white: 0.08, alpha: 1).cgColor
        layer?.borderWidth = 0.5
    }

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

        // NSScrollView 의 문서 뷰는 좌표 원점이 **왼쪽 아래**라 내용이 짧으면 바닥에 붙는다.
        // 뒤집힌(flipped) 컨테이너에 스택을 넣어 위에서부터 쌓이게 한다.
        let doc = FlippedView()
        doc.translatesAutoresizingMaskIntoConstraints = false
        doc.addSubview(stack)
        scroll.documentView = doc

        NSLayoutConstraint.activate([
            grip.topAnchor.constraint(equalTo: topAnchor),
            grip.leadingAnchor.constraint(equalTo: leadingAnchor),
            grip.trailingAnchor.constraint(equalTo: trailingAnchor),
            grip.heightAnchor.constraint(equalToConstant: 18),

            scroll.topAnchor.constraint(equalTo: grip.bottomAnchor),
            scroll.leadingAnchor.constraint(equalTo: leadingAnchor),
            scroll.trailingAnchor.constraint(equalTo: trailingAnchor),
            scroll.bottomAnchor.constraint(equalTo: bottomAnchor),

            doc.topAnchor.constraint(equalTo: scroll.contentView.topAnchor),
            doc.leadingAnchor.constraint(equalTo: scroll.contentView.leadingAnchor),
            doc.trailingAnchor.constraint(equalTo: scroll.contentView.trailingAnchor),
            // 문서 높이는 스택 높이와 창 높이 중 큰 쪽 — 짧으면 채우고, 길면 스크롤
            doc.heightAnchor.constraint(greaterThanOrEqualTo: scroll.contentView.heightAnchor),
            doc.heightAnchor.constraint(greaterThanOrEqualTo: stack.heightAnchor),

            stack.topAnchor.constraint(equalTo: doc.topAnchor),
            stack.leadingAnchor.constraint(equalTo: doc.leadingAnchor),
            stack.trailingAnchor.constraint(equalTo: doc.trailingAnchor),
        ])
    }

    /// 아이콘만 있는 작은 버튼. 이름과 설명은 툴팁으로, 색은 계열별로. 호버는 배경색만(크기 불변).
    private func makeButton(_ tool: CADTool, target: AnyObject, action: Selector) -> NSButton {
        let btn = HoverButton(image: NSImage(), target: target, action: action)
        ToolIcons.apply(tool, to: btn, pointSize: 16)
        btn.identifier = NSUserInterfaceItemIdentifier(tool.id)
        btn.imagePosition = .imageOnly
        btn.imageScaling = .scaleProportionallyDown
        btn.toolTip = "\(tool.title) — \(tool.tip)"
        btn.translatesAutoresizingMaskIntoConstraints = false
        NSLayoutConstraint.activate([
            btn.widthAnchor.constraint(equalToConstant: 36),
            btn.heightAnchor.constraint(equalToConstant: 32),
        ])
        return btn
    }

    // MARK: - 손잡이가 부르는 것들

    /// 놓은 지점으로 (쪽, 순서)를 정한다. 그쪽 영역의 띠들 가운데 커서보다 오른쪽에 있는 첫 띠 앞에 끼운다.
    fileprivate func gripDropped(atWindowPoint p: NSPoint) {
        guard let content = window?.contentView else { return }
        let local = content.convert(p, from: nil)
        let side: DockSide = local.x > content.bounds.midX ? .right : .left
        var index: Int? = nil
        if let area = content.subviews.compactMap({ $0 as? DockAreaView }).first(where: { $0.side == side }) {
            let others = area.strips.filter { $0 !== self }
            let inArea = area.convert(p, from: nil)
            index = others.firstIndex { inArea.x < $0.frame.midX } ?? others.count
        }
        delegate?.toolbarStrip(self, requestDock: side, at: index)
    }

    /// 드래그 중 마우스를 따라다니는 반투명 복사본 (창 콘텐츠 뷰 맨 위에 얹는다).
    fileprivate func makeDragGhost() -> NSImageView? {
        guard let rep = bitmapImageRepForCachingDisplay(in: bounds) else { return nil }
        cacheDisplay(in: bounds, to: rep)
        let image = NSImage(size: bounds.size)
        image.addRepresentation(rep)
        let ghost = NSImageView(image: image)
        ghost.frame = NSRect(origin: .zero, size: bounds.size)
        ghost.alphaValue = 0.65
        ghost.wantsLayer = true
        ghost.layer?.shadowOpacity = 0.6
        ghost.layer?.shadowRadius = 8
        return ghost
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

    @objc private func dockLeft()  { delegate?.toolbarStrip(self, requestDock: .left, at: nil) }
    @objc private func dockRight() { delegate?.toolbarStrip(self, requestDock: .right, at: nil) }
    @objc private func hideSelf()  { delegate?.toolbarStripRequestHide(self) }
}

/// 좌표 원점을 왼쪽 위로 두는 빈 컨테이너 (스크롤 문서 뷰가 위에서부터 쌓이게 하려고).
@MainActor
final class FlippedView: NSView {
    override var isFlipped: Bool { true }
}

/// 창 왼쪽/오른쪽의 도킹 영역. 띠를 가로로 나란히 담는다.
///
/// 폭은 **항상 띠 개수 × 46 으로 직접 계산해 제약 상수로 박는다.** 비면 0.
/// isHidden 으로 숨기지 않는 이유: 부모가 NSStackView 면 숨은 뷰를 뷰 계층에서 뺐다가 다시 넣는데,
/// 그 과정에서 자리가 남거나 순서가 어긋나는 현상이 보고됐다. 폭 0 은 그런 경로가 없다.
@MainActor
final class DockAreaView: NSView {
    let side: DockSide
    private let row = NSStackView()
    private var widthConstraint: NSLayoutConstraint!

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
        widthConstraint = widthAnchor.constraint(equalToConstant: 0)
        NSLayoutConstraint.activate([
            widthConstraint,
            row.leadingAnchor.constraint(equalTo: leadingAnchor),
            row.trailingAnchor.constraint(equalTo: trailingAnchor),
            row.topAnchor.constraint(equalTo: topAnchor),
            row.bottomAnchor.constraint(equalTo: bottomAnchor),
        ])
    }
    required init?(coder: NSCoder) { fatalError() }

    func add(_ strip: ToolbarStripView, at index: Int? = nil) {
        // 어디에 붙어 있었든 먼저 완전히 떼어 낸다 — 두 곳에 동시에 속하는 일이 없도록
        (strip.superview as? NSStackView)?.removeArrangedSubview(strip)
        strip.removeFromSuperview()
        let count = row.arrangedSubviews.count
        row.insertArrangedSubview(strip, at: min(max(index ?? count, 0), count))
        updateWidth()
    }

    func remove(_ strip: ToolbarStripView) {
        if row.arrangedSubviews.contains(where: { $0 === strip }) {
            row.removeArrangedSubview(strip)
        }
        if strip.superview === row { strip.removeFromSuperview() }
        updateWidth()
    }

    private func updateWidth() {
        widthConstraint.constant = CGFloat(strips.count) * ToolbarStripView.width
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

    private var ghost: NSImageView?
    private var ghostOffset = NSPoint.zero

    override func mouseDown(with event: NSEvent) {
        dragStart = event.locationInWindow
        NSCursor.closedHand.push()
    }

    override func mouseDragged(with event: NSEvent) {
        guard let start = dragStart, let owner, let content = window?.contentView else { return }
        let p = event.locationInWindow
        // 6pt 이상 움직여야 드래그로 본다 (클릭과 구분)
        if ghost == nil {
            guard hypot(p.x - start.x, p.y - start.y) > 6 else { return }
            guard let g = owner.makeDragGhost() else { return }
            let stripOriginInWindow = owner.convert(NSPoint.zero, to: nil)
            ghostOffset = NSPoint(x: start.x - stripOriginInWindow.x, y: start.y - stripOriginInWindow.y)
            content.addSubview(g, positioned: .above, relativeTo: nil)
            ghost = g
            owner.alphaValue = 0.35   // 원래 자리는 흐리게
        }
        let origin = content.convert(NSPoint(x: p.x - ghostOffset.x, y: p.y - ghostOffset.y), from: nil)
        ghost?.frame.origin = origin
    }

    override func mouseUp(with event: NSEvent) {
        NSCursor.pop()
        defer { dragStart = nil }
        owner?.alphaValue = 1.0
        guard ghost != nil else { return }      // 드래그가 아니었으면 클릭 — 아무것도 안 함
        ghost?.removeFromSuperview()
        ghost = nil
        owner?.gripDropped(atWindowPoint: event.locationInWindow)
    }

    override func rightMouseDown(with event: NSEvent) {
        owner?.showContextMenu(with: event)
    }
}
