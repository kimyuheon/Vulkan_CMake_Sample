// swift-tools-version: 6.0

import PackageDescription
import Foundation

let packageDirectory = URL(fileURLWithPath: #filePath).deletingLastPathComponent()
let projectRoot = packageDirectory
    .deletingLastPathComponent()
    .deletingLastPathComponent()
let buildDirectory = projectRoot.appendingPathComponent("sdk").path
// macOS 엔진 dylib 은 MoltenVK 를 정적으로 품고 있어(엔진 db9c21d) Vulkan 로더·SDK 경로가 필요 없다 — sdk/ 하나면 된다.

let package = Package(
    name: "VulkanCADSwiftApiTest",
    platforms: [
        .macOS(.v14)
    ],
    products: [
        .executable(name: "VulkanCADSwiftApiTest", targets: ["VulkanCADSwiftApiTest"]),
        .executable(name: "VulkanCADSwiftNativeViewTest", targets: ["VulkanCADSwiftNativeViewTest"]),
        .executable(name: "RobotArmMac", targets: ["RobotArmMac"]),
        .executable(name: "VulkanCADSamples", targets: ["VulkanCADSamples"])
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
        ),
        // 기능 샘플 공용 로직 + 조작 항목 정의 — sample/shared/demo_*.cpp (iOS·Android 와 같은 파일, 심볼릭 링크).
        .target(
            name: "DemoKit",
            path: "Sources/DemoKit",
            cxxSettings: [.unsafeFlags(["-I\(projectRoot.appendingPathComponent("sdk/include").path)"])]
        ),
        // 기능 샘플 앱 — 한 창에 메뉴 [샘플] 로 기능을 바꾼다. 조작 패널은 demo_kit 항목을 그대로 그리는 범용 패널.
        .executableTarget(
            name: "VulkanCADSamples",
            dependencies: ["CVulkanCAD", "DemoKit"],
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
