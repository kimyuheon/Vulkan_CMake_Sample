import AppKit
import RobotDemo

// 로봇 팔 샘플 (macOS) — 왼쪽: Vulkan 뷰, 오른쪽: [재생] [홈] [맞춤] + 관절 슬라이더.
// 무엇을 움직일지(관절 → 조작 항목, 자동 재생)는 공용 로직 robot_demo(sample/shared)가 정하고,
// 여기는 그것을 슬라이더로 보여 주기만 한다. iOS RobotArmView · Android RobotActivity 와 같은 구성.
// 엔진 래퍼(VulkanCADEngine)와 뷰(VulkanCADView)는 VulkanCADSwiftNativeViewTest 것을 그대로 쓴다(심볼릭 링크).
// 마우스는 데스크톱 규약 그대로 — 좌=선택 · 우 드래그=궤도회전 · 가운데 드래그=이동 · 휠=줌.
@MainActor
final class RobotArmAppDelegate: NSObject, NSApplicationDelegate, NSWindowDelegate {
    private struct Row {
        let slider: NSSlider
        let value: NSTextField
        let unit: String
    }

    private let engine = VulkanCADEngine()
    private var window: NSWindow!
    private var hostView: VulkanCADView!
    private var playButton: NSButton!
    private var rows: [Row] = []
    private var frameTimer: Timer?
    private var lastTime: CFTimeInterval = 0
    private var frame = 0
    private var isShuttingDown = false
    private static let syncEveryFrames = 6   // 재생 중 슬라이더를 따라 움직이는 주기(≈10Hz)

    func applicationDidFinishLaunching(_ notification: Notification) {
        NSApp.appearance = NSAppearance(named: .darkAqua)

        // 런타임 에셋(models/ textures/ fonts/)은 sdk/ 바로 아래 — 라이브러리와 같은 층.
        guard engine.setRuntimeAssetPath(sdkDirectory().path) else {
            fputs("[robot_arm] failed to set runtime asset path\n", stderr)
            NSApp.terminate(nil)
            return
        }

        let view = VulkanCADView(frame: NSRect(x: 0, y: 0, width: 900, height: 700))
        view.wantsLayer = true
        view.engine = engine
        view.setContentHuggingPriority(.defaultLow, for: .horizontal)
        hostView = view

        let panel = makePanel()
        let split = NSStackView(views: [view, panel])
        split.orientation = .horizontal
        split.spacing = 0
        split.distribution = .fill
        split.alignment = .height

        window = NSWindow(contentRect: NSRect(x: 0, y: 0, width: 1220, height: 760),
                          styleMask: [.titled, .closable, .miniaturizable, .resizable],
                          backing: .buffered, defer: false)
        window.title = "로봇 팔 — VulkanCAD"
        window.contentView = split
        window.minSize = NSSize(width: 900, height: 560)
        window.delegate = self
        window.center()
        window.makeKeyAndOrderFront(nil)
        window.makeFirstResponder(view)
        NSApp.setActivationPolicy(.regular)
        NSApp.activate(ignoringOtherApps: true)

        engine.attach(to: view)
        guard engine.create() else {
            fputs("[robot_arm] CAD_CreateEngine failed\n", stderr)
            NSApp.terminate(nil)
            return
        }
        view.notifyEngineResize()
        view.flushPendingResize()

        if RobotDemo_Load() {
            buildRows()
            syncAll()
            // 시험용 — 클릭 없이 재생부터 (iOS 와 같은 환경 변수).
            if ProcessInfo.processInfo.environment["ROBOT_AUTOPLAY"] != nil { togglePlay() }
        } else {
            showError("demo_arm.urdf 를 불러오지 못했습니다 (sdk/models 확인)")
        }

        let timer = Timer(timeInterval: 1.0 / 60.0, target: self,
                          selector: #selector(onFrame(_:)), userInfo: nil, repeats: true)
        RunLoop.main.add(timer, forMode: .common)   // 슬라이더를 끄는 동안(이벤트 추적 모드)에도 돈다
        frameTimer = timer
    }

    // MARK: - 프레임

    @objc private func onFrame(_ timer: Timer) {
        guard !isShuttingDown else { return }
        hostView.flushPendingResize()
        guard hostView.hasRenderableSize else { return }
        let now = CACurrentMediaTime()
        let dt = lastTime == 0 ? 0 : now - lastTime
        lastTime = now
        RobotDemo_Update(dt)   // 재생 중이면 관절을 움직인다 → 엔진이 다시 그리기 표시
        if !engine.tick() {
            shutdownEngine()
            NSApp.terminate(nil)
            return
        }
        // 재생 중엔 슬라이더가 로봇을 따라 움직이게 — 매 프레임은 레이아웃 비용이 커서 몇 프레임에 한 번.
        if RobotDemo_IsPlaying() {
            frame += 1
            if frame % Self.syncEveryFrames == 0 { syncAll() }
        }
    }

    // MARK: - 오른쪽 패널

    private var controlsStack = NSStackView()

    private func makePanel() -> NSView {
        playButton = NSButton(title: "▶ 재생", target: self, action: #selector(onPlay))
        let home = NSButton(title: "홈 자세", target: self, action: #selector(onHome))
        let fit = NSButton(title: "화면 맞춤", target: self, action: #selector(onFit))
        let buttons = NSStackView(views: [playButton, home, fit])
        buttons.orientation = .horizontal
        buttons.distribution = .fillEqually

        let title = NSTextField(labelWithString: "관절")
        title.font = .boldSystemFont(ofSize: 15)

        controlsStack.orientation = .vertical
        controlsStack.alignment = .leading
        controlsStack.spacing = 4

        let column = NSStackView(views: [buttons, title, controlsStack])
        column.orientation = .vertical
        column.alignment = .leading
        column.spacing = 14
        column.edgeInsets = NSEdgeInsets(top: 16, left: 16, bottom: 16, right: 16)
        column.translatesAutoresizingMaskIntoConstraints = false

        let panel = NSVisualEffectView()
        panel.material = .sidebar
        panel.addSubview(column)
        NSLayoutConstraint.activate([
            panel.widthAnchor.constraint(equalToConstant: 320),
            column.topAnchor.constraint(equalTo: panel.topAnchor),
            column.leadingAnchor.constraint(equalTo: panel.leadingAnchor),
            column.trailingAnchor.constraint(equalTo: panel.trailingAnchor),
            buttons.widthAnchor.constraint(equalTo: column.widthAnchor, constant: -32),
            controlsStack.widthAnchor.constraint(equalTo: column.widthAnchor, constant: -32),
        ])
        return panel
    }

    private func buildRows() {
        for i in 0..<RobotDemo_ControlCount() {
            let label = NSTextField(labelWithString: String(cString: RobotDemo_ControlLabel(i)))
            let value = NSTextField(labelWithString: "")
            value.textColor = .systemOrange
            value.alignment = .right
            value.font = .monospacedDigitSystemFont(ofSize: 13, weight: .regular)
            let header = NSStackView(views: [label, NSView(), value])
            header.orientation = .horizontal

            let slider = NSSlider(value: 0, minValue: Double(RobotDemo_ControlMin(i)),
                                  maxValue: Double(RobotDemo_ControlMax(i)),
                                  target: self, action: #selector(onSlider(_:)))
            slider.tag = Int(i)
            slider.isContinuous = true

            controlsStack.addArrangedSubview(header)
            controlsStack.addArrangedSubview(slider)
            header.widthAnchor.constraint(equalTo: controlsStack.widthAnchor).isActive = true
            slider.widthAnchor.constraint(equalTo: controlsStack.widthAnchor).isActive = true
            controlsStack.setCustomSpacing(12, after: slider)
            rows.append(Row(slider: slider, value: value, unit: String(cString: RobotDemo_ControlUnit(i))))
        }
    }

    private func showValue(_ row: Row, _ v: Float) {
        row.value.stringValue = row.unit == "%" ? String(format: "%.0f %%", v) : String(format: "%.0f°", v)
    }

    private func syncAll() {
        for (i, row) in rows.enumerated() {
            let v = RobotDemo_GetControl(Int32(i))
            row.slider.doubleValue = Double(v)
            showValue(row, v)
        }
    }

    private func updatePlayButton() {
        playButton.title = RobotDemo_IsPlaying() ? "⏸ 정지" : "▶ 재생"
    }

    private func togglePlay() {
        RobotDemo_SetPlaying(!RobotDemo_IsPlaying())
        updatePlayButton()
    }

    private func showError(_ text: String) {
        let label = NSTextField(wrappingLabelWithString: text)
        label.textColor = .systemRed
        controlsStack.addArrangedSubview(label)
    }

    @objc private func onSlider(_ sender: NSSlider) {
        let v = Float(sender.doubleValue)
        RobotDemo_SetControl(Int32(sender.tag), v)   // 손으로 잡으면 재생은 멈춘다
        showValue(rows[sender.tag], v)
        updatePlayButton()
    }

    @objc private func onPlay() { togglePlay() }
    @objc private func onHome() { RobotDemo_Home(); updatePlayButton(); syncAll() }
    @objc private func onFit() { engine.zoomExtents() }

    // MARK: - 종료

    func windowWillClose(_ notification: Notification) {
        shutdownEngine()
        NSApp.terminate(nil)
    }

    func applicationWillTerminate(_ notification: Notification) {
        shutdownEngine()
    }

    private func shutdownEngine() {
        guard !isShuttingDown else { return }
        isShuttingDown = true
        frameTimer?.invalidate()
        frameTimer = nil
        engine.shutdown()
    }

    /// 이 소스 파일 기준으로 레포 루트의 sdk/ — swift run 위치와 무관하게 찾는다.
    private func sdkDirectory() -> URL {
        URL(fileURLWithPath: #filePath)
            .deletingLastPathComponent()   // RobotArmMac
            .deletingLastPathComponent()   // Sources
            .deletingLastPathComponent()   // swift_api_test
            .deletingLastPathComponent()   // sample
            .deletingLastPathComponent()   // 3dEngine_Sample
            .appendingPathComponent("sdk")
    }
}
