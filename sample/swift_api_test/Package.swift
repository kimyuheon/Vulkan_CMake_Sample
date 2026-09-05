// swift-tools-version: 6.0

import PackageDescription
import Foundation

let packageDirectory = URL(fileURLWithPath: #filePath).deletingLastPathComponent()
let projectRoot = packageDirectory
    .deletingLastPathComponent()
    .deletingLastPathComponent()
let buildDirectory = projectRoot.appendingPathComponent("sdk").appendingPathComponent("lib").path

let package = Package(
    name: "VulkanCADSwiftApiTest",
    platforms: [
        .macOS(.v14)
    ],
    products: [
        .executable(name: "VulkanCADSwiftApiTest", targets: ["VulkanCADSwiftApiTest"]),
        .executable(name: "VulkanCADSwiftNativeViewTest", targets: ["VulkanCADSwiftNativeViewTest"])
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
        )
    ]
)
