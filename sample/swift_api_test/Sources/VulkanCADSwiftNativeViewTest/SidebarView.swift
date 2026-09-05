import AppKit
import CVulkanCAD

final class SidebarView: NSScrollView {
    private weak var engine: VulkanCADEngine?
    private var actions: [ObjectIdentifier: () -> Void] = [:]

    init(engine: VulkanCADEngine) {
        super.init(frame: .zero)
        self.engine = engine
        setup()
    }
    required init?(coder: NSCoder) { fatalError() }

    private func setup() {
        hasVerticalScroller  = true
        autohidesScrollers   = true
        borderType           = .noBorder
        backgroundColor      = NSColor(white: 0.13, alpha: 1.0)

        let stack = NSStackView()
        stack.orientation = .vertical
        stack.alignment   = .leading
        stack.spacing     = 5
        stack.edgeInsets  = NSEdgeInsets(top: 12, left: 8, bottom: 12, right: 8)
        stack.translatesAutoresizingMaskIntoConstraints = false

        // ── 도형 생성 ──────────────────────────────────────
        stack.addArrangedSubview(sectionLabel("도형 생성"))
        addFull(stack, "Box")       { [weak self] in _ = self?.engine?.createBox(center: .init(x:0,y:0,z:0.5), scale: .init(x:1,y:1,z:1)) }
        addFull(stack, "Line")      { [weak self] in self?.engine?.startLineSketch() }
        addFull(stack, "Circle")    { [weak self] in self?.engine?.startCircleSketch() }
        addFull(stack, "Rectangle") { [weak self] in self?.engine?.startRectangleSketch() }
        addFull(stack, "Polygon")   { [weak self] in self?.engine?.startPolygonSketch() }
        addFull(stack, "Arc")       { [weak self] in self?.engine?.startArcSketch() }
        addFull(stack, "Polyline")  { [weak self] in self?.engine?.startPolylineSketch() }
        addFull(stack, "Extrude")   { [weak self] in self?.engine?.startExtrude() }

        // ── 편집 ──────────────────────────────────────────
        stack.addArrangedSubview(separator())
        stack.addArrangedSubview(sectionLabel("편집"))

        addPair(stack,
            ("+X", { [weak self] in self?.engine?.moveSelected(direction: .init(x:1,y:0,z:0), distance:0.5) }),
            ("-X", { [weak self] in self?.engine?.moveSelected(direction: .init(x:-1,y:0,z:0), distance:0.5) }))
        addPair(stack,
            ("+Y", { [weak self] in self?.engine?.moveSelected(direction: .init(x:0,y:1,z:0), distance:0.5) }),
            ("-Y", { [weak self] in self?.engine?.moveSelected(direction: .init(x:0,y:-1,z:0), distance:0.5) }))
        addPair(stack,
            ("+Z", { [weak self] in self?.engine?.moveSelected(direction: .init(x:0,y:0,z:1), distance:0.5) }),
            ("-Z", { [weak self] in self?.engine?.moveSelected(direction: .init(x:0,y:0,z:-1), distance:0.5) }))

        addPair(stack,
            ("Rot+", { [weak self] in self?.engine?.rotateSelected(axis: .init(x:0,y:0,z:1), angleDegrees: 15) }),
            ("Rot-", { [weak self] in self?.engine?.rotateSelected(axis: .init(x:0,y:0,z:1), angleDegrees: -15) }))
        addPair(stack,
            ("Sc+",  { [weak self] in self?.engine?.scaleSelected(factor: 1.2) }),
            ("Sc-",  { [weak self] in self?.engine?.scaleSelected(factor: 0.8) }))

        addFull(stack, "Copy ×3 +X") { [weak self] in
            self?.engine?.copySelected(direction: .init(x:1,y:0,z:0), distance:1.5, count:3)
        }

        // Color row
        let colorRow = makeHRow()
        for (title, r, g, b): (String, Float, Float, Float) in [("Red",1,0,0), ("Green",0,1,0), ("Blue",0,0,1)] {
            let btn = makeBtn(title)
            let c = CADPoint(x: r, y: g, z: b)
            register(btn) { [weak self] in self?.engine?.colorSelected(c) }
            colorRow.addArrangedSubview(btn)
        }
        stack.addArrangedSubview(colorRow)

        // ── 뷰 ────────────────────────────────────────────
        stack.addArrangedSubview(separator())
        stack.addArrangedSubview(sectionLabel("뷰"))

        addFull(stack, "Zoom Extents") { CAD_RequestZoomExtents() }
        addFull(stack, "Material")     { [weak self] in self?.engine?.cycleMaterial() }
        addFull(stack, "Remove Mat")   { [weak self] in self?.engine?.removeMaterial() }
        addFull(stack, "Light")        { [weak self] in self?.engine?.startLightPlacement() }

        addPair(stack,
            ("Tex+", { [weak self] in self?.engine?.adjustTextureScale( 0.5) }),
            ("Tex-", { [weak self] in self?.engine?.adjustTextureScale(-0.5) }))
        addPair(stack,
            ("LW+",  { [weak self] in self?.engine?.adjustLineWidth( 1.0) }),
            ("LW-",  { [weak self] in self?.engine?.adjustLineWidth(-1.0) }))

        documentView = stack
        NSLayoutConstraint.activate([
            stack.leadingAnchor.constraint(equalTo:  contentView.leadingAnchor),
            stack.trailingAnchor.constraint(equalTo: contentView.trailingAnchor),
            stack.topAnchor.constraint(equalTo:      contentView.topAnchor),
        ])
    }

    // MARK: – helpers

    private func sectionLabel(_ text: String) -> NSTextField {
        let lbl = NSTextField(labelWithString: text.uppercased())
        lbl.font      = .systemFont(ofSize: 10, weight: .semibold)
        lbl.textColor = NSColor(white: 0.55, alpha: 1.0)
        return lbl
    }

    private func separator() -> NSBox {
        let box = NSBox()
        box.boxType = .separator
        return box
    }

    private func makeBtn(_ title: String) -> NSButton {
        let btn = NSButton(title: title, target: self, action: #selector(tapped(_:)))
        btn.bezelStyle  = .rounded
        btn.controlSize = .small
        btn.font        = .monospacedSystemFont(ofSize: 11, weight: .regular)
        return btn
    }

    private func makeHRow() -> NSStackView {
        let row = NSStackView()
        row.orientation = .horizontal
        row.spacing     = 4
        return row
    }

    private func register(_ btn: NSButton, action: @escaping () -> Void) {
        actions[ObjectIdentifier(btn)] = action
    }

    private func addFull(_ stack: NSStackView, _ title: String, action: @escaping () -> Void) {
        let btn = makeBtn(title)
        register(btn, action: action)
        stack.addArrangedSubview(btn)
        btn.translatesAutoresizingMaskIntoConstraints = false
        btn.widthAnchor.constraint(equalTo: stack.widthAnchor, constant: -16).isActive = true
    }

    private func addPair(_ stack: NSStackView,
                         _ lhs: (String, () -> Void),
                         _ rhs: (String, () -> Void)) {
        let row = makeHRow()
        let l   = makeBtn(lhs.0)
        let r   = makeBtn(rhs.0)
        register(l, action: lhs.1)
        register(r, action: rhs.1)
        row.addArrangedSubview(l)
        row.addArrangedSubview(r)
        stack.addArrangedSubview(row)
    }

    @objc private func tapped(_ sender: NSButton) {
        actions[ObjectIdentifier(sender)]?()
    }
}
