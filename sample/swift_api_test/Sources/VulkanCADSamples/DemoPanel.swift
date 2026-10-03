import AppKit
import DemoKit

// demo_kit 의 조작 항목 목록을 그대로 그리는 범용 패널 — 데모가 무엇이든 이 한 벌로 띄운다.
// 항목 종류: 제목 · 슬라이더 · 버튼 · 토글 · 선택지 · 입력칸 · 표시줄 · 파일 열기 (demo_kit.h).
@MainActor
final class DemoPanel: NSObject, NSTextFieldDelegate {
    let view: NSView
    private let stack = NSStackView()
    private let statusLabel = NSTextField(wrappingLabelWithString: "")
    private var revision: Int32 = -1

    // 항목 i 를 그린 컨트롤들 — 새로 고칠 때 값만 바꾼다
    private var sliders: [Int32: (NSSlider, NSTextField)] = [:]
    private var toggles: [Int32: NSButton] = [:]
    private var popups: [Int32: NSPopUpButton] = [:]
    private var texts: [Int32: NSTextField] = [:]
    private var labels: [Int32: NSTextField] = [:]
    private var enables: [Int32: NSControl] = [:]

    override init() {
        stack.orientation = .vertical
        stack.alignment = .leading
        stack.spacing = 6
        stack.edgeInsets = NSEdgeInsets(top: 12, left: 16, bottom: 16, right: 16)
        stack.translatesAutoresizingMaskIntoConstraints = false

        let doc = FlippedView()
        doc.translatesAutoresizingMaskIntoConstraints = false
        doc.addSubview(stack)
        let scroll = NSScrollView()
        scroll.hasVerticalScroller = true
        scroll.drawsBackground = false
        scroll.documentView = doc

        statusLabel.textColor = .systemOrange
        statusLabel.font = .systemFont(ofSize: 12, weight: .semibold)
        let column = NSStackView(views: [statusLabel, scroll])
        column.orientation = .vertical
        column.alignment = .leading
        column.edgeInsets = NSEdgeInsets(top: 12, left: 0, bottom: 0, right: 0)

        let panel = NSVisualEffectView()
        panel.material = .sidebar
        column.translatesAutoresizingMaskIntoConstraints = false
        panel.addSubview(column)
        NSLayoutConstraint.activate([
            panel.widthAnchor.constraint(equalToConstant: 340),
            column.topAnchor.constraint(equalTo: panel.topAnchor),
            column.bottomAnchor.constraint(equalTo: panel.bottomAnchor),
            column.leadingAnchor.constraint(equalTo: panel.leadingAnchor),
            column.trailingAnchor.constraint(equalTo: panel.trailingAnchor),
            statusLabel.leadingAnchor.constraint(equalTo: column.leadingAnchor, constant: 16),
            statusLabel.trailingAnchor.constraint(equalTo: column.trailingAnchor, constant: -16),
            scroll.leadingAnchor.constraint(equalTo: column.leadingAnchor),
            scroll.trailingAnchor.constraint(equalTo: column.trailingAnchor),
            doc.widthAnchor.constraint(equalTo: scroll.contentView.widthAnchor),
            stack.topAnchor.constraint(equalTo: doc.topAnchor),
            stack.leadingAnchor.constraint(equalTo: doc.leadingAnchor),
            stack.trailingAnchor.constraint(equalTo: doc.trailingAnchor),
            stack.bottomAnchor.constraint(equalTo: doc.bottomAnchor),
        ])
        view = panel
        super.init()
    }

    /// 몇 프레임에 한 번 — 항목 목록이 바뀌었으면 다시 만들고, 아니면 값·문구만 갱신.
    func refresh() {
        statusLabel.stringValue = String(cString: DemoKit_Status())
        if DemoKit_Revision() != revision { rebuild(); return }
        // 마우스로 끄는 중에는 슬라이더 값을 덮어쓰지 않는다(손과 싸우지 않게)
        let dragging = NSEvent.pressedMouseButtons != 0
        for (i, (slider, value)) in sliders {
            let v = DemoKit_GetValue(i)
            if !dragging { slider.doubleValue = Double(v) }
            value.stringValue = Self.format(v, unit: String(cString: DemoKit_ControlUnit(i)))
        }
        for (i, t) in toggles { t.state = DemoKit_GetValue(i) >= 0.5 ? .on : .off }
        for (i, p) in popups where !dragging { p.selectItem(at: Int(DemoKit_GetValue(i))) }
        for (i, l) in labels { l.stringValue = String(cString: DemoKit_GetText(i)) }
        for (i, c) in enables { c.isEnabled = DemoKit_ControlEnabled(i) }
    }

    private func rebuild() {
        revision = DemoKit_Revision()
        stack.arrangedSubviews.forEach { $0.removeFromSuperview() }
        sliders = [:]; toggles = [:]; popups = [:]; texts = [:]; labels = [:]; enables = [:]

        for i in 0..<DemoKit_ControlCount() {
            let title = String(cString: DemoKit_ControlLabel(i))
            switch DemoKit_ControlKind(i) {
            case 0:   // 제목
                let l = NSTextField(wrappingLabelWithString: title)   // 긴 제목(API 이름 포함)은 줄바꿈
                l.font = .boldSystemFont(ofSize: 13)
                add(l, spacingBefore: 14)
            case 1:   // 슬라이더
                let name = NSTextField(labelWithString: title)
                let value = NSTextField(labelWithString: "")
                value.textColor = .systemOrange
                value.alignment = .right
                value.font = .monospacedDigitSystemFont(ofSize: 12, weight: .regular)
                let row = NSStackView(views: [name, NSView(), value])
                add(row)
                let s = NSSlider(value: Double(DemoKit_GetValue(i)), minValue: Double(DemoKit_ControlMin(i)),
                                 maxValue: Double(DemoKit_ControlMax(i)), target: self, action: #selector(sliderChanged(_:)))
                s.tag = Int(i)
                s.isContinuous = true
                add(s)
                sliders[i] = (s, value)
            case 2:   // 버튼
                let b = NSButton(title: title, target: self, action: #selector(pressed(_:)))
                b.tag = Int(i)
                add(b)
                enables[i] = b
            case 3:   // 토글
                let b = NSButton(checkboxWithTitle: title, target: self, action: #selector(toggled(_:)))
                b.tag = Int(i)
                add(b)
                toggles[i] = b
            case 4:   // 선택지
                let p = NSPopUpButton(frame: .zero, pullsDown: false)
                for k in 0..<DemoKit_ChoiceCount(i) { p.addItem(withTitle: String(cString: DemoKit_ChoiceLabel(i, k))) }
                p.target = self
                p.action = #selector(chosen(_:))
                p.tag = Int(i)
                let row = NSStackView(views: [NSTextField(labelWithString: title), p])
                add(row)
                popups[i] = p
            case 5:   // 입력칸
                let f = NSTextField(string: String(cString: DemoKit_GetText(i)))
                f.tag = Int(i)
                f.delegate = self
                f.target = self
                f.action = #selector(textCommitted(_:))
                add(NSTextField(labelWithString: title))
                add(f)
                texts[i] = f
            case 6:   // 표시줄
                let name = NSTextField(labelWithString: title)
                name.textColor = .secondaryLabelColor
                let value = NSTextField(wrappingLabelWithString: "")
                value.font = .systemFont(ofSize: 12)
                add(name)
                add(value)
                labels[i] = value
            case 7:   // 파일
                let b = NSButton(title: title, target: self, action: #selector(pickFile(_:)))
                b.tag = Int(i)
                add(b)
            default:
                break
            }
        }
        refresh()
    }

    private func add(_ v: NSView, spacingBefore: CGFloat = 0) {
        if spacingBefore > 0, let last = stack.arrangedSubviews.last { stack.setCustomSpacing(spacingBefore, after: last) }
        stack.addArrangedSubview(v)
        v.translatesAutoresizingMaskIntoConstraints = false
        v.widthAnchor.constraint(equalTo: stack.widthAnchor, constant: -32).isActive = true
    }

    private static func format(_ v: Float, unit: String) -> String {
        let n = abs(v) >= 100 ? String(format: "%.0f", v) : abs(v) >= 10 ? String(format: "%.1f", v) : String(format: "%.2f", v)
        return unit.isEmpty ? n : "\(n) \(unit)"
    }

    // ── 입력 → demo_kit ──
    @objc private func sliderChanged(_ s: NSSlider) { DemoKit_SetValue(Int32(s.tag), Float(s.doubleValue)) }
    @objc private func pressed(_ b: NSButton) { DemoKit_Press(Int32(b.tag)); refresh() }
    @objc private func toggled(_ b: NSButton) { DemoKit_SetValue(Int32(b.tag), b.state == .on ? 1 : 0); refresh() }
    @objc private func chosen(_ p: NSPopUpButton) { DemoKit_SetValue(Int32(p.tag), Float(p.indexOfSelectedItem)); refresh() }
    @objc private func textCommitted(_ f: NSTextField) { DemoKit_SetText(Int32(f.tag), f.stringValue) }
    func controlTextDidChange(_ n: Notification) {
        guard let f = n.object as? NSTextField else { return }
        DemoKit_SetText(Int32(f.tag), f.stringValue)
    }
    @objc private func pickFile(_ b: NSButton) {
        let panel = NSOpenPanel()
        panel.allowedContentTypes = [.png, .jpeg, .image]
        guard panel.runModal() == .OK, let url = panel.url else { return }
        DemoKit_SetText(Int32(b.tag), url.path)
        refresh()
    }
}

/// 스크롤 내용이 위에서부터 쌓이게
private final class FlippedView: NSView {
    override var isFlipped: Bool { true }
}
