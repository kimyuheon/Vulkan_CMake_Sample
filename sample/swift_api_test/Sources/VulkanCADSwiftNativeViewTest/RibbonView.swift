import AppKit

/// 리본 — 위쪽 탭 줄 + 아래 그룹 줄. 탭을 누르면 그 탭의 그룹들로 아래를 갈아 끼운다.
/// 그룹은 [큰 버튼 가로 나열] 위에 [그룹 이름] 을 아래에 둔 형태이고, 그룹 사이에 세로 구분선.
@MainActor
final class RibbonView: NSView {
    // 탭 줄 30 + 그룹 줄(위 12 + 버튼 60 + 간격 3 + 이름 14 + 아래 7 = 96). 스크롤바는 겹침식이라 자리를 안 먹는다.
    static let height: CGFloat = 126
    private static let tabStripHeight: CGFloat = 30
    private static let buttonHeight: CGFloat = 60

    private let tabControl = NSSegmentedControl()
    private let groupRow = NSStackView()
    private let tabStrip = NSView()
    private var tabs: [RibbonTab] = []
    private weak var target: AnyObject?
    private let action: Selector
    private var selectedIndex = 0

    init(tabs: [RibbonTab], target: AnyObject, action: Selector) {
        self.target = target
        self.action = action
        super.init(frame: .zero)
        wantsLayer = true
        setup()
        reload(tabs: tabs)
    }
    required init?(coder: NSCoder) { fatalError() }

    // 배경은 updateLayer 에서 — 리본을 숨겼다 다시 켤 때(NSStackView 가 창에서 뺐다 넣는다)
    // backing layer 가 새로 생기면 init 에서 넣은 색이 사라진다.
    override var wantsUpdateLayer: Bool { true }
    override func updateLayer() {
        layer?.backgroundColor = NSColor(white: 0.17, alpha: 1).cgColor
        tabStrip.layer?.backgroundColor = NSColor(white: 0.12, alpha: 1).cgColor
    }

    /// 탭 목록을 통째로 바꾼다(플러그인 탭이 붙을 때). 보고 있던 탭은 제목으로 다시 찾는다.
    func reload(tabs: [RibbonTab]) {
        let previous = self.tabs.indices.contains(selectedIndex) ? self.tabs[selectedIndex].title : nil
        self.tabs = tabs
        tabControl.segmentCount = tabs.count
        for (i, tab) in tabs.enumerated() {
            tabControl.setLabel(tab.title, forSegment: i)
            tabControl.setWidth(0, forSegment: i)   // 0 = 글자 폭에 맞춤
        }
        let restored = previous.flatMap { title in tabs.firstIndex { $0.title == title } } ?? 0
        showTab(min(restored, max(0, tabs.count - 1)))
    }

    // MARK: - 구성

    private var heightConstraint: NSLayoutConstraint!

    /// 접기/펴기. isHidden 만 쓰지 않고 높이도 0 으로 내려 아래 뷰가 자리를 차지하게 한다.
    func setCollapsed(_ collapsed: Bool) {
        isHidden = collapsed
        heightConstraint.constant = collapsed ? 0 : Self.height
    }

    private func setup() {
        translatesAutoresizingMaskIntoConstraints = false
        heightConstraint = heightAnchor.constraint(equalToConstant: Self.height)
        heightConstraint.isActive = true

        // 탭 줄
        tabStrip.wantsLayer = true
        tabStrip.translatesAutoresizingMaskIntoConstraints = false
        addSubview(tabStrip)

        tabControl.segmentStyle = .texturedSquare
        tabControl.trackingMode = .selectOne
        tabControl.controlSize = .regular
        tabControl.font = .systemFont(ofSize: 12, weight: .medium)
        tabControl.target = self
        tabControl.action = #selector(tabChanged(_:))
        tabControl.translatesAutoresizingMaskIntoConstraints = false
        tabStrip.addSubview(tabControl)

        // 그룹 줄 — 창이 좁으면 가로 스크롤. 스크롤바는 **겹침식(overlay)** 으로 강제한다.
        // 시스템 설정이 "항상 표시" 면 고전식 스크롤바가 15px 를 차지해 그룹 이름이 잘렸다.
        let scroll = NSScrollView()
        scroll.drawsBackground = false
        scroll.hasHorizontalScroller = true
        scroll.hasVerticalScroller = false
        scroll.autohidesScrollers = true
        scroll.scrollerStyle = .overlay
        scroll.verticalScrollElasticity = .none
        scroll.borderType = .noBorder
        scroll.translatesAutoresizingMaskIntoConstraints = false
        addSubview(scroll)

        groupRow.orientation = .horizontal
        groupRow.alignment = .top
        groupRow.spacing = 6
        groupRow.edgeInsets = NSEdgeInsets(top: 12, left: 10, bottom: 7, right: 10)
        groupRow.translatesAutoresizingMaskIntoConstraints = false
        scroll.documentView = groupRow

        NSLayoutConstraint.activate([
            tabStrip.leadingAnchor.constraint(equalTo: leadingAnchor),
            tabStrip.trailingAnchor.constraint(equalTo: trailingAnchor),
            tabStrip.topAnchor.constraint(equalTo: topAnchor),
            tabStrip.heightAnchor.constraint(equalToConstant: Self.tabStripHeight),

            tabControl.leadingAnchor.constraint(equalTo: tabStrip.leadingAnchor, constant: 10),
            tabControl.centerYAnchor.constraint(equalTo: tabStrip.centerYAnchor),

            scroll.leadingAnchor.constraint(equalTo: leadingAnchor),
            scroll.trailingAnchor.constraint(equalTo: trailingAnchor),
            scroll.topAnchor.constraint(equalTo: tabStrip.bottomAnchor),
            scroll.bottomAnchor.constraint(equalTo: bottomAnchor),

            // 세로는 보이는 높이에 딱 맞춰 세로 스크롤이 생기지 않게 한다
            groupRow.topAnchor.constraint(equalTo: scroll.contentView.topAnchor),
            groupRow.heightAnchor.constraint(equalTo: scroll.contentView.heightAnchor),
            groupRow.leadingAnchor.constraint(equalTo: scroll.contentView.leadingAnchor),
        ])
    }

    @objc private func tabChanged(_ sender: NSSegmentedControl) {
        showTab(sender.selectedSegment)
    }

    private func showTab(_ index: Int) {
        guard tabs.indices.contains(index) else { return }
        selectedIndex = index
        tabControl.selectedSegment = index
        groupRow.arrangedSubviews.forEach { $0.removeFromSuperview() }
        for (i, group) in tabs[index].groups.enumerated() {
            if i > 0 { groupRow.addArrangedSubview(verticalSeparator()) }
            groupRow.addArrangedSubview(makeGroup(group))
        }
    }

    private func makeGroup(_ group: RibbonGroup) -> NSView {
        let buttons = NSStackView()
        buttons.orientation = .horizontal
        buttons.alignment = .top
        buttons.spacing = 2
        group.tools.forEach { buttons.addArrangedSubview(makeButton($0)) }

        let label = NSTextField(labelWithString: group.title)
        label.font = .systemFont(ofSize: 10.5)
        label.textColor = NSColor(white: 0.62, alpha: 1)
        label.alignment = .center

        let column = NSStackView(views: [buttons, label])
        column.orientation = .vertical
        column.alignment = .centerX
        column.spacing = 3
        return column
    }

    /// 리본 큰 버튼 — 아이콘 위, 글자 아래. 호버는 배경색만 바뀌고 크기는 **고정**이라 옆 버튼이 안 밀린다.
    private func makeButton(_ tool: CADTool) -> NSButton {
        let btn = HoverButton(title: tool.title, image: NSImage(), target: target, action: action)
        ToolIcons.apply(tool, to: btn, pointSize: 22)
        btn.identifier = NSUserInterfaceItemIdentifier(tool.id)
        btn.imagePosition = .imageAbove
        btn.imageScaling = .scaleProportionallyDown
        btn.font = .systemFont(ofSize: 11)
        btn.toolTip = tool.tip
        btn.translatesAutoresizingMaskIntoConstraints = false
        // 폭은 글자 길이로 한 번만 계산해 상수로 박는다 — 호버·상태 변화로 재계산되지 않게.
        let titleWidth = (tool.title as NSString).size(withAttributes: [.font: btn.font!]).width
        let width = max(58, ceil(titleWidth) + 16)
        NSLayoutConstraint.activate([
            btn.widthAnchor.constraint(equalToConstant: width),
            btn.heightAnchor.constraint(equalToConstant: Self.buttonHeight),
        ])
        return btn
    }

    private func verticalSeparator() -> NSView {
        let box = NSBox()
        box.boxType = .separator
        box.translatesAutoresizingMaskIntoConstraints = false
        box.widthAnchor.constraint(equalToConstant: 1).isActive = true
        box.heightAnchor.constraint(equalToConstant: Self.buttonHeight + 14).isActive = true
        return box
    }
}
