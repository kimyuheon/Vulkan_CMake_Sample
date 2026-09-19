import AppKit

/// 하단 명령행 + 상태 문구. 기존 ImGui 앱의 명령행과 같은 역할이다.
///
/// 왼쪽 입력칸에 `line`, `c`, `box`, `2,3` 처럼 치면 그대로 엔진 명령행으로 들어간다
/// (CAD_ExecuteCommand — 진행 중인 도구가 있으면 값 입력으로 전달된다).
/// 가운데는 엔진 프롬프트("첫 번째 점을 지정" 등), 잠깐 뜨는 안내는 주황색으로 덮어 보여 준다.
@MainActor
final class CommandBarView: NSView, NSTextFieldDelegate {
    static let height: CGFloat = 30

    var onCommand: ((String) -> Void)?
    var onEscape: (() -> Void)?

    private let field = NSTextField()
    private let promptLabel = NSTextField(labelWithString: "")
    private let selectionLabel = NSTextField(labelWithString: "")
    private var prompt = ""
    private var transient = ""
    private var history: [String] = []
    private var historyCursor = 0

    override init(frame: NSRect) {
        super.init(frame: frame)
        wantsLayer = true
        setup()
    }
    required init?(coder: NSCoder) { fatalError() }

    // 배경은 updateLayer 에서 (layer 가 재생성돼도 유지되도록).
    override var wantsUpdateLayer: Bool { true }
    override func updateLayer() {
        layer?.backgroundColor = NSColor(white: 0.12, alpha: 1).cgColor
    }

    func setPrompt(_ text: String) {
        prompt = text
        refresh()
    }

    /// 일회성 안내. 빈 문자열이면 프롬프트로 돌아간다.
    func setTransient(_ text: String) {
        transient = text
        refresh()
    }

    func setSelectionCount(_ count: Int) {
        selectionLabel.stringValue = count > 0 ? "선택 \(count)" : ""
    }

    func focus() {
        window?.makeFirstResponder(field)
    }

    // MARK: - 구성

    private func setup() {
        translatesAutoresizingMaskIntoConstraints = false
        heightAnchor.constraint(equalToConstant: Self.height).isActive = true

        let caption = NSTextField(labelWithString: "명령:")
        caption.font = .monospacedSystemFont(ofSize: 11.5, weight: .semibold)
        caption.textColor = NSColor(white: 0.7, alpha: 1)

        field.font = .monospacedSystemFont(ofSize: 12, weight: .regular)
        field.placeholderString = "line · c · box · z · 2,3  (↑↓ 이전 명령, Esc 취소)"
        field.bezelStyle = .roundedBezel
        field.controlSize = .small
        field.delegate = self
        field.target = self
        field.action = #selector(submit(_:))
        field.setContentHuggingPriority(.defaultLow, for: .horizontal)

        promptLabel.font = .systemFont(ofSize: 11.5)
        promptLabel.textColor = NSColor(white: 0.75, alpha: 1)
        promptLabel.lineBreakMode = .byTruncatingTail
        promptLabel.setContentCompressionResistancePriority(.defaultLow, for: .horizontal)

        selectionLabel.font = .systemFont(ofSize: 11, weight: .medium)
        selectionLabel.textColor = NSColor(white: 0.6, alpha: 1)
        selectionLabel.alignment = .right

        let row = NSStackView(views: [caption, field, promptLabel, selectionLabel])
        row.orientation = .horizontal
        row.alignment = .centerY
        row.spacing = 10
        row.edgeInsets = NSEdgeInsets(top: 0, left: 10, bottom: 0, right: 10)
        row.translatesAutoresizingMaskIntoConstraints = false
        addSubview(row)

        NSLayoutConstraint.activate([
            row.leadingAnchor.constraint(equalTo: leadingAnchor),
            row.trailingAnchor.constraint(equalTo: trailingAnchor),
            row.topAnchor.constraint(equalTo: topAnchor),
            row.bottomAnchor.constraint(equalTo: bottomAnchor),
            field.widthAnchor.constraint(greaterThanOrEqualToConstant: 260),
            field.widthAnchor.constraint(lessThanOrEqualToConstant: 460),
            selectionLabel.widthAnchor.constraint(greaterThanOrEqualToConstant: 60),
        ])
    }

    private func refresh() {
        if transient.isEmpty {
            promptLabel.stringValue = prompt
            promptLabel.textColor = NSColor(white: 0.75, alpha: 1)
        } else {
            promptLabel.stringValue = transient
            promptLabel.textColor = NSColor(calibratedRed: 1.0, green: 0.68, blue: 0.25, alpha: 1)
        }
    }

    // MARK: - 입력

    @objc private func submit(_ sender: NSTextField) {
        let text = sender.stringValue.trimmingCharacters(in: .whitespaces)
        guard !text.isEmpty else {
            onCommand?("")      // 빈 Enter = 엔진 관례대로 "마지막 명령 반복 / 확정"
            return
        }
        history.append(text)
        historyCursor = history.count
        sender.stringValue = ""
        onCommand?(text)
    }

    func control(_ control: NSControl, textView: NSTextView, doCommandBy selector: Selector) -> Bool {
        switch selector {
        case #selector(NSResponder.cancelOperation(_:)):
            field.stringValue = ""
            onEscape?()
            return true
        case #selector(NSResponder.moveUp(_:)):
            guard historyCursor > 0 else { return true }
            historyCursor -= 1
            field.stringValue = history[historyCursor]
            return true
        case #selector(NSResponder.moveDown(_:)):
            guard historyCursor < history.count else { return true }
            historyCursor += 1
            field.stringValue = historyCursor < history.count ? history[historyCursor] : ""
            return true
        default:
            return false
        }
    }
}
