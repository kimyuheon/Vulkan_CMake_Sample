import AppKit
import Foundation

final class AppDelegate: NSObject, NSApplicationDelegate, NSWindowDelegate {
    private var window: NSWindow?
    private var hostView: VulkanCADView?
    private var frameTimer: Timer?
    private let engine = VulkanCADEngine()
    private var isShuttingDown = false

    func applicationDidFinishLaunching(_ notification: Notification) {
        // 런타임 에셋(models/ textures/ fonts/)은 sdk/ 바로 아래 — 라이브러리와 같은 층.
        let runtimeDirectory = projectRootDirectory().appendingPathComponent("sdk")
        guard engine.setRuntimeAssetPath(runtimeDirectory.path) else {
            fputs("[swift_native_view_test] failed to set runtime asset path\n", stderr)
            NSApp.terminate(nil)
            return
        }

        let sidebarWidth: CGFloat = 160
        let vulkanWidth:  CGFloat = 1024
        let windowHeight: CGFloat = 720

        let view = VulkanCADView(frame: NSRect(x: 0, y: 0, width: vulkanWidth, height: windowHeight))
        view.wantsLayer = true
        view.engine = engine
        hostView = view

        let sidebar = SidebarView(engine: engine)

        let container = NSView(frame: NSRect(x: 0, y: 0,
                                             width: sidebarWidth + vulkanWidth,
                                             height: windowHeight))
        sidebar.translatesAutoresizingMaskIntoConstraints = false
        view.translatesAutoresizingMaskIntoConstraints    = false
        container.addSubview(sidebar)
        container.addSubview(view)
        NSLayoutConstraint.activate([
            sidebar.leadingAnchor.constraint(equalTo:  container.leadingAnchor),
            sidebar.topAnchor.constraint(equalTo:      container.topAnchor),
            sidebar.bottomAnchor.constraint(equalTo:   container.bottomAnchor),
            sidebar.widthAnchor.constraint(equalToConstant: sidebarWidth),

            view.leadingAnchor.constraint(equalTo:  sidebar.trailingAnchor),
            view.trailingAnchor.constraint(equalTo: container.trailingAnchor),
            view.topAnchor.constraint(equalTo:      container.topAnchor),
            view.bottomAnchor.constraint(equalTo:   container.bottomAnchor),
        ])

        let window = NSWindow(
            contentRect: container.frame,
            styleMask: [.titled, .closable, .miniaturizable, .resizable],
            backing: .buffered,
            defer: false)
        window.title = "VulkanCAD Swift Native View"
        window.contentView = container
        window.delegate = self
        window.acceptsMouseMovedEvents = true
        window.center()
        window.makeKeyAndOrderFront(nil)
        window.makeFirstResponder(view)
        self.window = window

        NSApp.setActivationPolicy(.regular)
        NSApp.activate(ignoringOtherApps: true)

        engine.attach(to: view)
        guard engine.create() else {
            fputs("[swift_native_view_test] CAD_CreateEngine failed\n", stderr)
            NSApp.terminate(nil)
            return
        }

        view.notifyEngineResize()
        view.flushPendingResize()
        engine.createDemoScene()
        startFrameTimer()
    }

    @MainActor @objc private func onFrameTimer(_ timer: Timer) {
        guard !isShuttingDown else { return }
        hostView?.flushPendingResize()
        guard hostView?.hasRenderableSize ?? false else { return }
        if !engine.tick() {
            shutdownEngine()
            NSApp.terminate(nil)
        }
    }

    func windowWillClose(_ notification: Notification) {
        shutdownEngine()
        NSApp.terminate(nil)
    }

    func applicationWillTerminate(_ notification: Notification) {
        shutdownEngine()
    }

    private func startFrameTimer() {
        let timer = Timer(
            timeInterval: 1.0 / 60.0,
            target: self,
            selector: #selector(onFrameTimer(_:)),
            userInfo: nil,
            repeats: true)
        frameTimer = timer
        RunLoop.main.add(timer, forMode: .common)
    }

    private func shutdownEngine() {
        guard !isShuttingDown else { return }
        isShuttingDown = true
        frameTimer?.invalidate()
        frameTimer = nil
        engine.shutdown()
    }

    private func projectRootDirectory() -> URL {
        let sourceFile = URL(fileURLWithPath: #filePath)
        return sourceFile
            .deletingLastPathComponent() // VulkanCADSwiftNativeViewTest
            .deletingLastPathComponent() // Sources
            .deletingLastPathComponent() // swift_api_test
            .deletingLastPathComponent() // samples
            .deletingLastPathComponent() // 3dEngine
    }
}
