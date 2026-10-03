import AppKit
import CVulkanCAD
import DemoKit

/// VulkanCAD 기능 샘플 (macOS) — 한 앱에 기능별 메뉴.
/// 메뉴 막대 [샘플] 에서 로봇 팔 · 파티클 · Jig · 단면도 · 평면도 · 내비 · AI 를 고르면 같은 창에서 바뀐다(⌘1~).
/// 왼쪽: Vulkan 뷰, 오른쪽: demo_kit 이 정의한 조작 패널(DemoPanel). 기능 로직은 sample/shared/demo_*.cpp.
/// 마우스는 데스크톱 규약 — 좌=선택 · 우 드래그=궤도회전 · 가운데 드래그=이동 · 휠=줌.
@MainActor
final class DemoAppDelegate: NSObject, NSApplicationDelegate, NSWindowDelegate {
    private var demoId: String
    private let engine = VulkanCADEngine()
    private var window: NSWindow!
    private var hostView: VulkanCADView!
    private var panel: DemoPanel!
    private var frameTimer: Timer?
    private var lastTime: CFTimeInterval = 0
    private var frame = 0
    private var isShuttingDown = false
    private static let refreshEveryFrames = 6   // 패널 값 갱신 주기(≈10Hz)

    init(demoId: String) { self.demoId = demoId }

    // ── 메뉴 ──
    private func buildMenu() {
        let main = NSMenu()
        let appItem = NSMenuItem()
        let appMenu = NSMenu()
        appMenu.addItem(withTitle: "VulkanCAD 샘플 종료", action: #selector(NSApplication.terminate(_:)), keyEquivalent: "q")
        appItem.submenu = appMenu
        main.addItem(appItem)

        let samplesItem = NSMenuItem()
        let samples = NSMenu(title: "샘플")
        for i in 0..<DemoKit_DemoCount() {
            let item = NSMenuItem(title: String(cString: DemoKit_DemoTitle(i)), action: #selector(selectDemo(_:)),
                                  keyEquivalent: i < 9 ? "\(i + 1)" : "")
            item.target = self
            item.tag = Int(i)
            item.toolTip = String(cString: DemoKit_DemoSummary(i))
            samples.addItem(item)
        }
        samplesItem.submenu = samples
        main.addItem(samplesItem)
        NSApp.mainMenu = main
    }

    @objc private func selectDemo(_ item: NSMenuItem) { start(String(cString: DemoKit_DemoId(Int32(item.tag)))) }

    private func start(_ id: String) {
        guard DemoKit_Start(id) else { fputs("[demo] unknown demo id: \(id)\n", stderr); return }
        demoId = id
        window.title = "\(String(cString: DemoKit_CurrentTitle())) — VulkanCAD 샘플"
        NSApp.mainMenu?.item(at: 1)?.submenu?.items.forEach {
            $0.state = String(cString: DemoKit_DemoId(Int32($0.tag))) == id ? .on : .off
        }
        panel.refresh()
    }

    func validateMenuItem(_ item: NSMenuItem) -> Bool { true }

    func applicationDidFinishLaunching(_ notification: Notification) {
        NSApp.appearance = NSAppearance(named: .darkAqua)
        buildMenu()

        // 런타임 에셋(models/ textures/ fonts/ effects/)은 sdk/ 바로 아래.
        guard engine.setRuntimeAssetPath(sdkDirectory().path) else {
            fputs("[demo] failed to set runtime asset path\n", stderr)
            NSApp.terminate(nil)
            return
        }

        let view = VulkanCADView(frame: NSRect(x: 0, y: 0, width: 900, height: 720))
        view.wantsLayer = true
        view.engine = engine
        view.setContentHuggingPriority(.defaultLow, for: .horizontal)
        hostView = view
        panel = DemoPanel()

        let split = NSStackView(views: [view, panel.view])
        split.orientation = .horizontal
        split.spacing = 0
        split.alignment = .height

        window = NSWindow(contentRect: NSRect(x: 0, y: 0, width: 1240, height: 780),
                          styleMask: [.titled, .closable, .miniaturizable, .resizable],
                          backing: .buffered, defer: false)
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
            fputs("[demo] CAD_CreateEngine failed\n", stderr)
            NSApp.terminate(nil)
            return
        }
        view.notifyEngineResize()
        view.flushPendingResize()

        start(demoId)

        let timer = Timer(timeInterval: 1.0 / 60.0, target: self,
                          selector: #selector(onFrame(_:)), userInfo: nil, repeats: true)
        RunLoop.main.add(timer, forMode: .common)   // 슬라이더를 끄는 동안(이벤트 추적 모드)에도 돈다
        frameTimer = timer
    }

    @objc private func onFrame(_ timer: Timer) {
        guard !isShuttingDown else { return }
        hostView.flushPendingResize()
        guard hostView.hasRenderableSize else { return }
        let now = CACurrentMediaTime()
        let dt = lastTime == 0 ? 0 : now - lastTime
        lastTime = now
        DemoKit_Update(dt)
        if !engine.tick() {
            shutdownEngine()
            NSApp.terminate(nil)
            return
        }
        frame += 1
        if frame % Self.refreshEveryFrames == 0 { panel.refresh() }
        runTestHooks()
    }

    // ── 시험용 (자동 확인) ──
    // VULKANCAD_PRESS="목적지 정하기;출발" — 0.5초 간격으로 그 이름의 버튼을 누른다.
    // VULKANCAD_SHOT=/경로/폴더 — 누르기가 끝나고 2초 뒤 화면을 <데모>.png 로 찍고 패널 상태를 출력한 뒤 종료.
    private lazy var testPresses: [String] = (ProcessInfo.processInfo.environment["VULKANCAD_PRESS"] ?? "")
        .split(separator: ";").map(String.init)
    private var testShotFrame = 0

    private func runTestHooks() {
        guard let shotDir = ProcessInfo.processInfo.environment["VULKANCAD_SHOT"] else { return }
        if !testPresses.isEmpty {
            if frame % 30 == 0 {
                let name = testPresses.removeFirst()
                if let i = (0..<DemoKit_ControlCount()).first(where: { String(cString: DemoKit_ControlLabel($0)) == name }) {
                    DemoKit_Press(i)
                    print("[test] press \(name)")
                } else {
                    print("[test] no control: \(name)")
                }
            }
            return
        }
        if testShotFrame == 0 { testShotFrame = frame + 120 }
        guard frame == testShotFrame else { return }
        let path = "\(shotDir)/\(demoId).png"
        print("[test] capture \(path): \(CAD_CaptureViewport(path, false))")
        print("[test] status: \(String(cString: DemoKit_Status()))")
        for i in 0..<DemoKit_ControlCount() where [6, 1].contains(DemoKit_ControlKind(i)) {
            let k = DemoKit_ControlKind(i)
            let v = k == 6 ? String(cString: DemoKit_GetText(i)) : String(DemoKit_GetValue(i))
            print("[test]   \(String(cString: DemoKit_ControlLabel(i))) = \(v)")
        }
        shutdownEngine()
        exit(0)
    }

    func windowWillClose(_ notification: Notification) {
        shutdownEngine()
        NSApp.terminate(nil)
    }

    func applicationWillTerminate(_ notification: Notification) { shutdownEngine() }

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
            .deletingLastPathComponent()   // VulkanCADSamples
            .deletingLastPathComponent()   // Sources
            .deletingLastPathComponent()   // swift_api_test
            .deletingLastPathComponent()   // sample
            .deletingLastPathComponent()   // 3dEngine_Sample
            .appendingPathComponent("sdk")
    }
}
