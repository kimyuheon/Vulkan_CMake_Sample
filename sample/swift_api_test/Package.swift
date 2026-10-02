// swift-tools-version: 6.0

import PackageDescription
import Foundation

let packageDirectory = URL(fileURLWithPath: #filePath).deletingLastPathComponent()
let projectRoot = packageDirectory
    .deletingLastPathComponent()
    .deletingLastPathComponent()
let buildDirectory = projectRoot.appendingPathComponent("sdk").path

let package = Package(
    name: "VulkanCADSwiftApiTest",
    platforms: [
        .macOS(.v14)
    ],
    products: [
        .executable(name: "VulkanCADSwiftApiTest", targets: ["VulkanCADSwiftApiTest"]),
        .executable(name: "VulkanCADSwiftNativeViewTest", targets: ["VulkanCADSwiftNativeViewTest"]),
        .executable(name: "RobotArmMac", targets: ["RobotArmMac"])
    ],
    targets: [
        .systemLibrary(
            name: "CVulkanCAD",
            path: "Sources/CVulkanCAD",
            pkgConfig: nil,
            providers: []
        ),
        .executableTarget(
            name: "VulkanCADSwiftApiTest",
            dependencies: ["CVulkanCAD"],
            linkerSettings: [
                .unsafeFlags([
                    "-L\(buildDirectory)",
                    "-lVulkanCADCore",
                    "-Xlinker", "-rpath",
                    "-Xlinker", buildDirectory
                ])
            ]
        ),
        .executableTarget(
            name: "VulkanCADSwiftNativeViewTest",
            dependencies: ["CVulkanCAD"],
            linkerSettings: [
                .linkedFramework("AppKit"),
                .unsafeFlags([
                    "-L\(buildDirectory)",
                    "-lVulkanCADCore",
                    "-Xlinker", "-rpath",
                    "-Xlinker", buildDirectory
                ])
            ]
        ),
        // 로봇 팔 공용 로직 — sample/shared/robot_demo (iOS·Android 와 같은 파일, 심볼릭 링크).
        // SwiftPM 은 패키지 밖 경로를 소스로 못 받아서 링크로 들여온다. CAD_* 는 앱 타깃이 dylib 를 링크한다.
        .target(
            name: "RobotDemo",
            path: "Sources/RobotDemo",
            cxxSettings: [.unsafeFlags(["-I\(projectRoot.appendingPathComponent("sdk/include").path)"])]
        ),
        // 로봇 팔 샘플 앱. VulkanCADEngine/VulkanCADView 는 NativeViewTest 것을 링크로 같이 쓴다.
        .executableTarget(
            name: "RobotArmMac",
            dependencies: ["CVulkanCAD", "RobotDemo"],
            linkerSettings: [
                .linkedFramework("AppKit"),
                .unsafeFlags([
                    "-L\(buildDirectory)",
                    "-lVulkanCADCore",
                    "-Xlinker", "-rpath",
                    "-Xlinker", buildDirectory
                ])
            ]
        )
    ],
    cxxLanguageStandard: .cxx17
)
