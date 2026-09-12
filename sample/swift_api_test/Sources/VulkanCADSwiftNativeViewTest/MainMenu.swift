import AppKit

/// 풀다운 메뉴. 항목은 전부 ToolCatalog 의 도구를 가리키고, 실행은 AppDelegate.runTool 한 곳으로 모인다.
/// 활성/체크 상태는 AppDelegate.validateMenuItem 이 매번 계산한다(Undo 가능 여부, 격자 켜짐 등).
@MainActor
enum MainMenuBuilder {
    static func build(catalog: ToolCatalog, target: AppDelegate) -> NSMenu {
        let main = NSMenu()

        // ── 앱 메뉴 ──
        let app = NSMenu()
        app.addItem(withTitle: "VulkanCAD 정보",
                    action: #selector(NSApplication.orderFrontStandardAboutPanel(_:)), keyEquivalent: "")
        app.addItem(.separator())
        app.addItem(withTitle: "VulkanCAD 가리기", action: #selector(NSApplication.hide(_:)), keyEquivalent: "h")
        let hideOthers = app.addItem(withTitle: "기타 가리기",
                                     action: #selector(NSApplication.hideOtherApplications(_:)), keyEquivalent: "h")
        hideOthers.keyEquivalentModifierMask = [.command, .option]
        app.addItem(withTitle: "모두 보기", action: #selector(NSApplication.unhideAllApplications(_:)), keyEquivalent: "")
        app.addItem(.separator())
        app.addItem(withTitle: "VulkanCAD 종료", action: #selector(NSApplication.terminate(_:)), keyEquivalent: "q")
        main.addItem(submenu("VulkanCAD", app))

        // ── 파일 ──
        let file = NSMenu(title: "파일")
        add(file, catalog.pick("new", "open", "import"), target: target)
        file.addItem(.separator())
        add(file, catalog.pick("save", "export"), target: target)
        file.addItem(.separator())
        file.addItem(withTitle: "창 닫기", action: #selector(NSWindow.performClose(_:)), keyEquivalent: "w")
        main.addItem(submenu("파일", file))

        // ── 편집 ──
        let edit = NSMenu(title: "편집")
        add(edit, catalog.pick("undo", "redo"), target: target)
        edit.addItem(.separator())
        add(edit, catalog.pick("selectall", "deselect", "delete", "clear"), target: target)
        edit.addItem(.separator())
        add(edit, catalog.transformTools, target: target)
        edit.addItem(.separator())
        edit.addItem(submenu("기즈모", menu("기즈모", catalog.gizmoTools, target: target)))
        main.addItem(submenu("편집", edit))

        // ── 뷰 ──
        let view = NSMenu(title: "뷰")
        add(view, catalog.viewpointTools, target: target)
        view.addItem(.separator())
        add(view, catalog.cameraTools, target: target)
        view.addItem(.separator())
        view.addItem(submenu("뷰포트 분할", menu("뷰포트 분할", catalog.layoutTools, target: target)))
        view.addItem(submenu("비주얼 스타일", menu("비주얼 스타일", catalog.styleTools, target: target)))
        add(view, catalog.displayTools, target: target)
        view.addItem(submenu("탐색 모드", menu("탐색 모드", catalog.navigateTools, target: target)))
        view.addItem(.separator())
        add(view, catalog.pick("ui_ribbon"), target: target)
        // 도구모음 목록은 열 때마다 AppDelegate(menuNeedsUpdate) 가 채운다 — 플러그인 도구모음이 나중에 생기므로
        let toolbars = NSMenu(title: "도구모음")
        toolbars.delegate = target
        view.addItem(submenu("도구모음", toolbars))
        main.addItem(submenu("뷰", view))

        // ── 그리기 ──
        let draw = NSMenu(title: "그리기")
        add(draw, catalog.drawTools, target: target)
        draw.addItem(.separator())
        add(draw, catalog.annotationTools, target: target)
        main.addItem(submenu("그리기", draw))

        // ── 수정 ──
        let modify = NSMenu(title: "수정")
        add(modify, catalog.modifyTools, target: target)
        modify.addItem(.separator())
        modify.addItem(submenu("폴리선 편집", menu("폴리선 편집", catalog.polylineTools, target: target)))
        main.addItem(submenu("수정", modify))

        // ── 솔리드 ──
        let solid = NSMenu(title: "솔리드")
        add(solid, catalog.primitiveTools, target: target)
        solid.addItem(.separator())
        add(solid, catalog.solidOpTools, target: target)
        solid.addItem(.separator())
        add(solid, catalog.booleanTools, target: target)
        solid.addItem(.separator())
        add(solid, catalog.edgeTools, target: target)
        main.addItem(submenu("솔리드", solid))

        // ── 재질 ──
        main.addItem(submenu("재질", menu("재질", catalog.materialTools, target: target)))

        // ── 창 ── (플러그인 메뉴는 AppDelegate 가 이 앞에 끼워 넣는다)
        let window = NSMenu(title: "창")
        window.addItem(withTitle: "최소화", action: #selector(NSWindow.performMiniaturize(_:)), keyEquivalent: "m")
        window.addItem(withTitle: "확대/축소", action: #selector(NSWindow.performZoom(_:)), keyEquivalent: "")
        window.addItem(.separator())
        window.addItem(withTitle: "모두 앞으로 가져오기", action: #selector(NSApplication.arrangeInFront(_:)), keyEquivalent: "")
        let windowItem = submenu("창", window)
        main.addItem(windowItem)
        NSApp.windowsMenu = window

        // ── 도움말 ──
        let help = NSMenu(title: "도움말")
        add(help, catalog.helpTools, target: target)
        let helpItem = submenu("도움말", help)
        main.addItem(helpItem)
        NSApp.helpMenu = help

        return main
    }

    /// 플러그인 메뉴를 "창" 메뉴 앞에 끼운다. 반환 = 끼운 항목(나중에 빼기 위해).
    static func insertPluginMenu(_ menu: NSMenu, into main: NSMenu) -> NSMenuItem {
        let item = submenu(menu.title, menu)
        let windowIndex = main.items.firstIndex { $0.submenu === NSApp.windowsMenu } ?? main.items.count
        main.insertItem(item, at: windowIndex)
        return item
    }

    // MARK: - helpers

    static func toolItem(_ tool: CADTool, target: AppDelegate) -> NSMenuItem {
        let item = NSMenuItem(title: tool.title, action: #selector(AppDelegate.runTool(_:)), keyEquivalent: tool.key)
        item.keyEquivalentModifierMask = tool.keyMask
        item.target = target
        item.representedObject = tool.id
        item.toolTip = tool.tip
        return item
    }

    private static func add(_ menu: NSMenu, _ tools: [CADTool], target: AppDelegate) {
        tools.forEach { menu.addItem(toolItem($0, target: target)) }
    }

    private static func menu(_ title: String, _ tools: [CADTool], target: AppDelegate) -> NSMenu {
        let m = NSMenu(title: title)
        add(m, tools, target: target)
        return m
    }

    static func submenu(_ title: String, _ menu: NSMenu) -> NSMenuItem {
        let item = NSMenuItem(title: title, action: nil, keyEquivalent: "")
        item.submenu = menu
        return item
    }
}
