import AppKit
import Foundation
import UniformTypeIdentifiers

/// 창 구성:
///
///   ┌ 풀다운 메뉴 (NSApp.mainMenu) ─────────────────────────────┐
///   │ 리본 (탭 + 그룹)                                            │
///   ├────┬────┬──────────────────────────────────────────┬────┤
///   │표준│그리│ Vulkan 뷰                                  │솔리│  ← 도킹 영역(좌/우)에 이름 붙은
///   │    │기  │                                            │드  │     도구모음 띠가 나란히 붙는다
///   ├────┴────┴──────────────────────────────────────────┴────┤
///   │ 명령: [        ]  프롬프트                          선택 N │
///   └─────────────────────────────────────────────────────────┘
///
/// 메뉴·리본·도구모음의 버튼은 전부 runTool(_:) 로 들어와 ToolCatalog 의 도구를 실행한다.
/// 플러그인이 CAD_AddUiItem 으로 올린 항목은 엔진 생성 뒤 읽어 같은 자리에 덧붙인다.
@MainActor
final class AppDelegate: NSObject, NSApplicationDelegate, NSWindowDelegate,
                         NSMenuItemValidation, NSMenuDelegate, ToolbarDockDelegate {
    let engine = VulkanCADEngine()

    private var window: NSWindow?
    private var hostView: VulkanCADView?
    private var frameTimer: Timer?
    private var isShuttingDown = false

    private var catalog: ToolCatalog!
    private var ribbon: RibbonView!
    private var commandBar: CommandBarView!
    private var leftArea: DockAreaView!
    private var rightArea: DockAreaView!

    /// 도구모음 띠 전부(보이든 숨었든). 플러그인 도구모음은 "plugin" 키.
    private var strips: [String: ToolbarStripView] = [:]
    private var pluginToolbar: ToolbarDefinition?
    private static let pluginToolbarId = "plugin"
    private static let layoutSavedKey = "VulkanCAD.toolbars.saved"
    private static let layoutLeftKey = "VulkanCAD.toolbars.left"
    private static let layoutRightKey = "VulkanCAD.toolbars.right"

    private var gridEnabled = true
    private var visualStyle: Int32 = 0
    private var lastTransient = ""
    private var pluginMenuItems: [NSMenuItem] = []
    private var tickCount = 0

    // MARK: - 시작 / 종료

    func applicationDidFinishLaunching(_ notification: Notification) {
        // 런타임 에셋(models/ textures/ fonts/)은 sdk/ 바로 아래 — 라이브러리와 같은 층.
        let runtimeDirectory = projectRootDirectory().appendingPathComponent("sdk")
        guard engine.setRuntimeAssetPath(runtimeDirectory.path) else {
            fputs("[swift_native_view_test] failed to set runtime asset path\n", stderr)
            NSApp.terminate(nil)
            return
        }

        // CAD 앱답게 어두운 외관으로 고정 — 리본/도구모음 색을 여기에 맞췄다.
        NSApp.appearance = NSAppearance(named: .darkAqua)

        catalog = ToolCatalog()
        NSApp.mainMenu = MainMenuBuilder.build(catalog: catalog, target: self)

        let view = VulkanCADView(frame: NSRect(x: 0, y: 0, width: 1024, height: 700))
        view.wantsLayer = true
        view.engine = engine
        view.translatesAutoresizingMaskIntoConstraints = false
        view.setContentHuggingPriority(.defaultLow, for: .horizontal)
        view.setContentHuggingPriority(.defaultLow, for: .vertical)
        hostView = view

        ribbon = RibbonView(tabs: catalog.ribbonTabs, target: self, action: #selector(runTool(_:)))
        commandBar = CommandBarView(frame: .zero)
        commandBar.onCommand = { [weak self] text in self?.runCommandLine(text) }
        commandBar.onEscape = { [weak self] in self?.sendEscape() }

        leftArea = DockAreaView(side: .left)
        rightArea = DockAreaView(side: .right)
        for def in catalog.toolbars {
            strips[def.id] = makeStrip(def)
        }

        // 배치는 NSStackView 대신 명시적 제약으로 짠다.
        //   세로: [리본(높이 120|0)] [가운데 줄] [명령행(30)]
        //   가로: [왼쪽 도킹 영역(46×n)] [뷰] [오른쪽 도킹 영역(46×n)]
        // 스택 뷰의 "숨긴 뷰를 계층에서 뺐다 넣는" 동작이 도구모음 자리를 남기는 현상을 만들 수 있어
        // 그 경로를 아예 없앴다. 도킹 영역은 숨기지 않고 폭 0 이 된다.
        let container = NSView(frame: NSRect(x: 0, y: 0, width: 1400, height: 880))
        for v in [ribbon!, leftArea!, view, rightArea!, commandBar!] as [NSView] {
            v.translatesAutoresizingMaskIntoConstraints = false
            container.addSubview(v)
        }
        NSLayoutConstraint.activate([
            ribbon.topAnchor.constraint(equalTo: container.topAnchor),
            ribbon.leadingAnchor.constraint(equalTo: container.leadingAnchor),
            ribbon.trailingAnchor.constraint(equalTo: container.trailingAnchor),

            commandBar.bottomAnchor.constraint(equalTo: container.bottomAnchor),
            commandBar.leadingAnchor.constraint(equalTo: container.leadingAnchor),
            commandBar.trailingAnchor.constraint(equalTo: container.trailingAnchor),

            leftArea.leadingAnchor.constraint(equalTo: container.leadingAnchor),
            leftArea.topAnchor.constraint(equalTo: ribbon.bottomAnchor),
            leftArea.bottomAnchor.constraint(equalTo: commandBar.topAnchor),

            rightArea.trailingAnchor.constraint(equalTo: container.trailingAnchor),
            rightArea.topAnchor.constraint(equalTo: ribbon.bottomAnchor),
            rightArea.bottomAnchor.constraint(equalTo: commandBar.topAnchor),

            view.leadingAnchor.constraint(equalTo: leftArea.trailingAnchor),
            view.trailingAnchor.constraint(equalTo: rightArea.leadingAnchor),
            view.topAnchor.constraint(equalTo: ribbon.bottomAnchor),
            view.bottomAnchor.constraint(equalTo: commandBar.topAnchor),
        ])

        let window = NSWindow(
            contentRect: container.frame,
            styleMask: [.titled, .closable, .miniaturizable, .resizable],
            backing: .buffered,
            defer: false)
        window.contentView = container
        window.delegate = self
        window.acceptsMouseMovedEvents = true
        window.minSize = NSSize(width: 1000, height: 620)
        window.center()
        window.makeKeyAndOrderFront(nil)
        window.makeFirstResponder(view)
        self.window = window

        applySavedToolbarLayout()

        NSApp.setActivationPolicy(.regular)
        NSApp.activate(ignoringOtherApps: true)

        engine.attach(to: view)
        guard engine.create() else {
            fputs("[swift_native_view_test] CAD_CreateEngine failed\n", stderr)
            NSApp.terminate(nil)
            return
        }

        // 엔진 → UI 알림. 전부 CAD_Tick 안(메인)에서 불린다.
        engine.onPrompt { [weak self] text in self?.commandBar.setPrompt(text) }
        engine.onSelectionChanged { [weak self] in
            guard let self else { return }
            self.commandBar.setSelectionCount(self.engine.selectedCount)
        }
        engine.onDocumentDirty { [weak self] _ in self?.updateWindowTitle() }

        view.notifyEngineResize()
        view.flushPendingResize()
        engine.createDemoScene()
        commandBar.setPrompt(engine.statusMessage)
        installPluginUi()
        updateWindowTitle()
        startFrameTimer()
    }

    @objc private func onFrameTimer(_ timer: Timer) {
        guard !isShuttingDown else { return }
        hostView?.flushPendingResize()
        guard hostView?.hasRenderableSize ?? false else { return }
        if !engine.tick() {
            shutdownEngine()
            NSApp.terminate(nil)
            return
        }
        // 환경변수 VULKANCAD_LAYOUT_DUMP=1 로 띄우면 2.5초 뒤 레이아웃 진단을 콘솔에 찍는다(자동 검증용).
        tickCount += 1
        if tickCount == 150, ProcessInfo.processInfo.environment["VULKANCAD_LAYOUT_DUMP"] != nil {
            showLayoutDiagnostics()
        }
        // 일회성 안내("각도: 선을 클릭하세요" 등)는 콜백이 없어 폴링한다 — 바뀔 때만 UI 를 건드린다.
        let transient = engine.transientMessage
        if transient != lastTransient {
            lastTransient = transient
            commandBar.setTransient(transient)
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
        let timer = Timer(timeInterval: 1.0 / 60.0, target: self,
                          selector: #selector(onFrameTimer(_:)), userInfo: nil, repeats: true)
        frameTimer = timer
        // .common — 모달 패널(열기/저장 대화상자)이 떠 있는 동안에도 프레임이 돈다.
        RunLoop.main.add(timer, forMode: .common)
    }

    private func shutdownEngine() {
        guard !isShuttingDown else { return }
        isShuttingDown = true
        frameTimer?.invalidate()
        frameTimer = nil
        engine.shutdown()
    }

    // MARK: - 도구 실행 (메뉴·리본·도구모음 공통 입구)

    @objc func runTool(_ sender: Any) {
        let id: String?
        if let item = sender as? NSMenuItem {
            id = item.representedObject as? String
        } else if let control = sender as? NSControl {
            id = control.identifier?.rawValue
        } else {
            id = nil
        }
        guard let id else { return }

        if id.hasPrefix("cmd:") {
            // 플러그인 항목 — 명령 이름만 들고 있다
            engine.execute(String(id.dropFirst(4)))
        } else if id.hasPrefix("tb:") {
            // 도구모음 켜고 끄기 (뷰 > 도구모음, 띠 우클릭 메뉴)
            toggleToolbar(String(id.dropFirst(3)))
        } else if let tool = catalog.byId[id] {
            tool.run(self)
        }
        // 버튼을 누른 뒤 키 입력이 뷰로 가도록 (Esc 로 도구 취소 등)
        if !(sender is NSMenuItem), let view = hostView {
            window?.makeFirstResponder(view)
        }
    }

    func validateMenuItem(_ item: NSMenuItem) -> Bool {
        guard let id = item.representedObject as? String else { return true }
        switch id {
        case "undo":   return engine.canUndo
        case "redo":   return engine.canRedo
        case "delete", "deselect", "export", "focus":
            return engine.selectedCount > 0
        case "grid":
            item.state = gridEnabled ? .on : .off
        case "ui_ribbon":
            item.state = ribbon.isHidden ? .off : .on
        case "layout1", "layout2", "layout3", "layout4":
            let index = Int32(id.dropFirst(6))! - 1
            item.state = engine.viewportLayout == index ? .on : .off
        case "style0", "style1", "style2", "style4":
            item.state = visualStyle == Int32(id.dropFirst(5))! ? .on : .off
        default:
            if id.hasPrefix("tb:") {
                let visible = isToolbarVisible(String(id.dropFirst(3)))
                item.state = visible ? .on : .off
            }
        }
        return true
    }

    /// 뷰 > 도구모음 하위 메뉴 — 열 때마다 현재 목록(플러그인 포함)으로 채운다.
    func menuNeedsUpdate(_ menu: NSMenu) {
        menu.removeAllItems()
        toolbarMenuItems().forEach { menu.addItem($0) }
        menu.addItem(.separator())
        catalog.pick("tb_toggle_all", "tb_all_left", "tb_all_right", "tb_reset")
            .forEach { menu.addItem(MainMenuBuilder.toolItem($0, target: self)) }
    }

    private func runCommandLine(_ text: String) {
        if text.isEmpty {
            // 빈 Enter — 엔진 명령행과 같게 Enter 키를 넘긴다(진행 중 도구 확정 / 마지막 명령 반복)
            engine.keyDown(257, modifiers: 0)
            engine.keyUp(257, modifiers: 0)
            return
        }
        if !engine.execute(text) {
            commandBar.setTransient("알 수 없는 명령: \(text)")
        }
    }

    private func sendEscape() {
        engine.keyDown(256, modifiers: 0)
        engine.keyUp(256, modifiers: 0)
        if let view = hostView { window?.makeFirstResponder(view) }
    }

    // MARK: - 도킹 도구모음

    private func makeStrip(_ def: ToolbarDefinition) -> ToolbarStripView {
        let strip = ToolbarStripView(definition: def, target: self, action: #selector(runTool(_:)))
        strip.delegate = self
        return strip
    }

    private var allToolbarDefinitions: [ToolbarDefinition] {
        catalog.toolbars + (pluginToolbar.map { [$0] } ?? [])
    }

    private func area(of strip: ToolbarStripView) -> DockAreaView? {
        if leftArea.strips.contains(where: { $0 === strip }) { return leftArea }
        if rightArea.strips.contains(where: { $0 === strip }) { return rightArea }
        return nil
    }

    private func isToolbarVisible(_ id: String) -> Bool {
        guard let strip = strips[id] else { return false }
        return area(of: strip) != nil
    }

    /// 띠를 그쪽 영역에 붙인다(이미 보이면 옮긴다). index 가 있으면 그 자리에, 없으면 끝에.
    func showToolbar(_ id: String, side: DockSide, at index: Int? = nil) {
        guard let strip = strips[id] else { return }
        area(of: strip)?.remove(strip)
        (side == .left ? leftArea : rightArea).add(strip, at: index)
        saveToolbarLayout()
    }

    func hideToolbar(_ id: String) {
        guard let strip = strips[id] else { return }
        area(of: strip)?.remove(strip)
        saveToolbarLayout()
    }

    func toggleToolbar(_ id: String) {
        if isToolbarVisible(id) { hideToolbar(id) } else { showToolbar(id, side: .left) }
    }

    func toggleAllToolbars() {
        let visible = strips.keys.filter(isToolbarVisible)
        if visible.isEmpty {
            allToolbarDefinitions.forEach { showToolbar($0.id, side: .left) }
        } else {
            visible.forEach(hideToolbar)
        }
    }

    func moveAllToolbars(to side: DockSide) {
        let from = side == .left ? rightArea! : leftArea!
        from.strips.forEach { showToolbar($0.definition.id, side: side) }
    }

    func resetToolbarLayout() {
        UserDefaults.standard.removeObject(forKey: Self.layoutSavedKey)
        strips.keys.filter(isToolbarVisible).forEach { strips[$0].map { area(of: $0)?.remove($0) } }
        applySavedToolbarLayout()
    }

    /// 저장된 배치가 있으면 그대로, 없으면 카탈로그 기본 배치.
    private func applySavedToolbarLayout() {
        let defaults = UserDefaults.standard
        var left = catalog.defaultToolbarLayout.left
        var right = catalog.defaultToolbarLayout.right
        if defaults.bool(forKey: Self.layoutSavedKey) {
            left = defaults.stringArray(forKey: Self.layoutLeftKey) ?? []
            right = defaults.stringArray(forKey: Self.layoutRightKey) ?? []
        }
        for id in left where strips[id] != nil {
            area(of: strips[id]!)?.remove(strips[id]!)
            leftArea.add(strips[id]!)
        }
        for id in right where strips[id] != nil {
            area(of: strips[id]!)?.remove(strips[id]!)
            rightArea.add(strips[id]!)
        }
    }

    private func saveToolbarLayout() {
        let defaults = UserDefaults.standard
        defaults.set(true, forKey: Self.layoutSavedKey)
        defaults.set(leftArea.strips.map { $0.definition.id }, forKey: Self.layoutLeftKey)
        defaults.set(rightArea.strips.map { $0.definition.id }, forKey: Self.layoutRightKey)
    }

    /// 도구모음 켜고 끄기 항목들 — 뷰 메뉴와 띠 우클릭 메뉴가 같이 쓴다.
    private func toolbarMenuItems() -> [NSMenuItem] {
        allToolbarDefinitions.map { def in
            let item = NSMenuItem(title: def.title, action: #selector(runTool(_:)), keyEquivalent: "")
            item.target = self
            item.representedObject = "tb:\(def.id)"
            item.state = isToolbarVisible(def.id) ? .on : .off
            return item
        }
    }

    // ToolbarDockDelegate — 손잡이 끌기 / 우클릭 메뉴
    func toolbarStrip(_ strip: ToolbarStripView, requestDock side: DockSide, at index: Int?) {
        showToolbar(strip.definition.id, side: side, at: index)
    }

    func toolbarStripRequestHide(_ strip: ToolbarStripView) {
        hideToolbar(strip.definition.id)
    }

    func toolbarStripExtraMenuItems(_ strip: ToolbarStripView) -> [NSMenuItem] {
        toolbarMenuItems()
    }

    // MARK: - 도구가 부르는 호스트 기능

    func newDocument() {
        engine.newDocument()
        updateWindowTitle()
    }

    /// 열기(새 탭) / 가져오기(현재 탭). 확장자로 포맷이 정해지므로 확장자 목록만 준다.
    func openDocumentDialog(asNewTab: Bool) {
        let panel = NSOpenPanel()
        panel.title = asNewTab ? "열기 (새 탭)" : "가져오기 (현재 탭에 추가)"
        panel.allowedContentTypes = contentTypes(["lot", "obj", "stl", "dxf", "ply", "gltf", "glb", "fbx"])
        panel.allowsMultipleSelection = false
        panel.canChooseDirectories = false
        guard panel.runModal() == .OK, let url = panel.url else { return }

        let ok = asNewTab ? engine.openDocument(path: url.path) >= 0
                          : engine.openFile(path: url.path)
        if !ok {
            alert("열지 못했습니다", detail: engine.statusMessage.isEmpty ? url.lastPathComponent : engine.statusMessage)
        }
        updateWindowTitle()
    }

    /// 다른 이름으로 저장(전체) / 내보내기(선택). .lot 은 프로젝트, 나머지는 내보내기다.
    func saveDialog(selectedOnly: Bool) {
        if selectedOnly && engine.selectedCount == 0 {
            commandBar.setTransient("내보낼 객체를 먼저 선택하세요")
            return
        }
        let panel = NSSavePanel()
        panel.title = selectedOnly ? "선택 내보내기" : "다른 이름으로 저장"
        panel.allowedContentTypes = contentTypes(["lot", "stl", "dxf", "obj", "glb", "gltf"])
        panel.allowsOtherFileTypes = true
        panel.canCreateDirectories = true
        panel.nameFieldStringValue = selectedOnly ? "selection.stl" : "untitled.lot"
        panel.message = "확장자로 포맷이 정해집니다 — .lot(프로젝트) .stl .dxf .obj .glb .gltf"
        guard panel.runModal() == .OK, let url = panel.url else { return }

        if !engine.saveAs(path: url.path, selectedOnly: selectedOnly) {
            alert("저장하지 못했습니다", detail: engine.statusMessage.isEmpty ? url.lastPathComponent : engine.statusMessage)
        }
        updateWindowTitle()
    }

    func setVisualStyle(_ style: Int32) {
        visualStyle = style
        engine.setVisualStyle(style)
    }

    func toggleGrid() {
        gridEnabled.toggle()
        engine.setGridEnabled(gridEnabled)
    }

    func toggleRibbon() {
        ribbon.setCollapsed(!ribbon.isHidden)
    }

    func showCommandLineHelp() {
        alert("명령행 사용법", detail: """
        하단 입력칸에 엔진 명령을 그대로 칩니다. 메뉴·리본 버튼과 같은 입구입니다.

        line / l      선            rec         사각형        c / circle   원
        pl            폴리선        arc / a     호            text         문자
        box / b       박스          x           돌출          pp           밀당
        m / ro / sc   이동/회전/축척  cp          복사          ar           배열
        trim / ex     자르기/연장    offset      오프셋        fillet       필렛
        z             전체 보기     f           선택 맞춤     u            실행 취소

        도구가 진행 중일 때 값을 치면 그 도구로 들어갑니다.
          2,3      → 좌표        1500     → 거리        =1500,800 → 절대 좌표
        Esc 로 도구를 취소하고, ↑↓ 로 이전 명령을 다시 부릅니다.
        """)
    }

    func showRegisteredCommands() {
        let cmds = engine.registeredCommands()
        if cmds.isEmpty {
            alert("플러그인 명령 없음", detail: "플러그인이 등록한 명령이 없습니다. 실행 폴더의 plugins/ 를 시작 시 자동으로 읽고, 도움말 > 플러그인 폴더 불러오기로 더 올릴 수 있습니다.")
            return
        }
        let lines = cmds.map { $0.title.isEmpty || $0.title == $0.name ? $0.name : "\($0.name) — \($0.title)" }
        alert("플러그인 명령 \(cmds.count)개", detail: lines.joined(separator: "\n"))
    }

    /// 표시 이상(회색 띠, 도구모음 안 보임 등) 신고용 — 실제 프레임을 모아 클립보드에 넣고 보여 준다.
    func showLayoutDiagnostics() {
        func r(_ rect: NSRect) -> String {
            "(\(Int(rect.origin.x)),\(Int(rect.origin.y)) \(Int(rect.width))×\(Int(rect.height)))"
        }
        func strip(_ s: ToolbarStripView) -> String {
            "\(s.definition.id)\(s.isHidden ? "[hidden]" : "")\(s.superview == nil ? "[no-superview]" : "")\(r(s.frame))"
        }
        var lines: [String] = []
        if let w = window, let c = w.contentView {
            lines.append("window.content \(r(c.bounds))  scale=\(w.backingScaleFactor)  liveResize=\(c.inLiveResize)")
        }
        lines.append("ribbon \(ribbon.isHidden ? "[hidden]" : "") \(r(ribbon.frame))")
        for (name, area) in [("left", leftArea!), ("right", rightArea!)] {
            lines.append("\(name)Area \(area.isHidden ? "[hidden]" : "")\(area.superview == nil ? "[detached]" : "") \(r(area.frame))")
            area.strips.forEach { lines.append("    \(strip($0))") }
        }
        let unplaced = strips.values.filter { area(of: $0) == nil }.map { $0.definition.id }.sorted()
        lines.append("숨은 도구모음: \(unplaced)")
        if let v = hostView {
            lines.append("vulkanView \(r(v.frame))  layer=\(v.layer.map { String(describing: type(of: $0)) } ?? "nil")")
            if let ml = v.layer as? CAMetalLayer {
                lines.append("    layer.frame \(r(ml.frame))  drawable=\(Int(ml.drawableSize.width))×\(Int(ml.drawableSize.height))  scale=\(ml.contentsScale)")
            }
        }
        lines.append("commandBar \(r(commandBar.frame))")
        lines.append("defaults left=\(UserDefaults.standard.stringArray(forKey: Self.layoutLeftKey) ?? []) right=\(UserDefaults.standard.stringArray(forKey: Self.layoutRightKey) ?? [])")
        let text = lines.joined(separator: "\n")
        print("[layout]\n\(text)")
        NSPasteboard.general.clearContents()
        NSPasteboard.general.setString(text, forType: .string)
        alert("레이아웃 진단 (클립보드에 복사됨)", detail: text)
    }

    func loadPluginsDialog() {
        let panel = NSOpenPanel()
        panel.title = "플러그인 폴더 선택"
        panel.canChooseDirectories = true
        panel.canChooseFiles = false
        panel.allowsMultipleSelection = false
        guard panel.runModal() == .OK, let url = panel.url else { return }
        let count = engine.loadPlugins(directory: url.path)
        installPluginUi()
        commandBar.setTransient(count > 0 ? "플러그인 \(count)개 로드" : "올린 플러그인이 없습니다")
    }

    // MARK: - 플러그인 UI 항목 → 메뉴 · 리본 · 도구모음

    /// 플러그인은 (종류, 위치, 이름, 명령)만 올린다. 여기서 읽어 호스트 위젯으로 그린다.
    /// 다시 부르면 이전에 붙인 것을 걷어 내고 새로 붙인다(플러그인 추가 로드 뒤).
    private func installPluginUi() {
        let items = engine.uiItems()

        // 메뉴: path 첫 마디 = 상위 메뉴, 둘째 마디 = 하위 메뉴
        if let main = NSApp.mainMenu {
            pluginMenuItems.forEach { main.removeItem($0) }
            pluginMenuItems.removeAll()

            var topMenus: [(String, NSMenu)] = []
            var subMenus: [String: NSMenu] = [:]
            func menu(forPath path: String) -> NSMenu? {
                let parts = path.split(separator: "/").map(String.init)
                guard let top = parts.first else { return nil }
                let topMenu: NSMenu
                if let found = topMenus.first(where: { $0.0 == top })?.1 {
                    topMenu = found
                } else {
                    topMenu = NSMenu(title: top)
                    topMenus.append((top, topMenu))
                }
                guard parts.count > 1 else { return topMenu }
                let key = parts.prefix(2).joined(separator: "/")
                if let sub = subMenus[key] { return sub }
                let sub = NSMenu(title: parts[1])
                topMenu.addItem(MainMenuBuilder.submenu(parts[1], sub))
                subMenus[key] = sub
                return sub
            }
            for item in items where item.kind == .menuItem || item.kind == .separator {
                guard let target = menu(forPath: item.path) else { continue }
                if item.kind == .separator {
                    target.addItem(.separator())
                } else {
                    target.addItem(MainMenuBuilder.toolItem(pluginTool(item), target: self))
                }
            }
            pluginMenuItems = topMenus.map { MainMenuBuilder.insertPluginMenu($0.1, into: main) }
        }

        // 리본: 첫 마디 = 탭, 둘째 마디 = 그룹
        var tabOrder: [String] = []
        var groups: [String: [(String, [CADTool])]] = [:]
        for item in items where item.kind == .ribbonButton {
            let parts = item.path.split(separator: "/").map(String.init)
            let tab = parts.first ?? "플러그인"
            let group = parts.count > 1 ? parts[1] : "명령"
            if groups[tab] == nil { tabOrder.append(tab); groups[tab] = [] }
            if let gi = groups[tab]!.firstIndex(where: { $0.0 == group }) {
                groups[tab]![gi].1.append(pluginTool(item))
            } else {
                groups[tab]!.append((group, [pluginTool(item)]))
            }
        }
        let pluginTabs = tabOrder.map { tab in
            RibbonTab(title: tab, groups: groups[tab]!.map { RibbonGroup(title: $0.0, tools: $0.1) })
        }
        ribbon.reload(tabs: catalog.ribbonTabs + pluginTabs)

        // 도구모음: "플러그인" 띠 하나로. 이미 있으면 같은 자리에 새 띠로 갈아 끼운다.
        let toolButtons = items.filter { $0.kind == .toolButton }.map(pluginTool)
        let previousSide = strips[Self.pluginToolbarId].flatMap { area(of: $0)?.side }
        if let old = strips[Self.pluginToolbarId] {
            area(of: old)?.remove(old)
            strips[Self.pluginToolbarId] = nil
        }
        pluginToolbar = nil
        if !toolButtons.isEmpty {
            let def = ToolbarDefinition(id: Self.pluginToolbarId, title: "플러그인", tools: toolButtons)
            pluginToolbar = def
            strips[def.id] = makeStrip(def)
            // 처음 생기면 왼쪽, 사용자가 옮겨 뒀으면 그쪽, 숨겨 뒀으면(저장돼 있고 양쪽에 없음) 숨긴 채로
            let saved = UserDefaults.standard.bool(forKey: Self.layoutSavedKey)
            let savedLeft = UserDefaults.standard.stringArray(forKey: Self.layoutLeftKey) ?? []
            let savedRight = UserDefaults.standard.stringArray(forKey: Self.layoutRightKey) ?? []
            if let side = previousSide {
                showToolbar(def.id, side: side)
            } else if !saved || savedLeft.contains(def.id) {
                showToolbar(def.id, side: .left)
            } else if savedRight.contains(def.id) {
                showToolbar(def.id, side: .right)
            }
        }
    }

    private func pluginTool(_ item: VulkanCADEngine.UiItem) -> CADTool {
        let command = item.command
        return CADTool(id: "cmd:\(command)",
                       title: item.title.isEmpty ? command : item.title,
                       symbol: "",                                   // 아이콘은 글자로 (icon 필드 또는 첫 글자)
                       tip: item.icon.isEmpty ? command : "\(item.icon)  \(command)") { app in
            app.engine.execute(command)
        }
    }

    // MARK: - 잡동사니

    private func updateWindowTitle() {
        let title = engine.activeDocumentTitle
        let dirty = engine.isActiveDocumentDirty ? " •" : ""
        window?.title = title.isEmpty ? "VulkanCAD" : "VulkanCAD — \(title)\(dirty)"
    }

    private func contentTypes(_ extensions: [String]) -> [UTType] {
        extensions.compactMap { UTType(filenameExtension: $0) }
    }

    private func alert(_ message: String, detail: String) {
        let a = NSAlert()
        a.messageText = message
        a.informativeText = detail
        a.alertStyle = .informational
        if let window { a.beginSheetModal(for: window) } else { a.runModal() }
    }

    private func projectRootDirectory() -> URL {
        let sourceFile = URL(fileURLWithPath: #filePath)
        return sourceFile
            .deletingLastPathComponent() // VulkanCADSwiftNativeViewTest
            .deletingLastPathComponent() // Sources
            .deletingLastPathComponent() // swift_api_test
            .deletingLastPathComponent() // sample
            .deletingLastPathComponent() // 3dEngine_Sample
    }
}
