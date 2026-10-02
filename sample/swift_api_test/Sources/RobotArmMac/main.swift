import AppKit

// 로봇 팔 샘플 (macOS) — 구성은 RobotArmAppDelegate.swift.
let app = NSApplication.shared
let delegate = RobotArmAppDelegate()
app.delegate = delegate
app.run()
