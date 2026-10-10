import AppKit

// LotCAD 기능 샘플 (macOS) — 첫 화면은 로봇 팔. 다른 샘플은 메뉴 [샘플] 에서.
// 시험용: LOTCAD_DEMO=nav 처럼 주면 그 샘플로 시작한다.
let app = NSApplication.shared
let delegate = DemoAppDelegate(demoId: ProcessInfo.processInfo.environment["LOTCAD_DEMO"] ?? "robot")
app.delegate = delegate
app.run()
