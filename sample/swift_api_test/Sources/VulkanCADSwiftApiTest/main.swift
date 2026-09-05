import CVulkanCAD
import Foundation

@discardableResult
func require(_ condition: Bool, _ message: String) -> Bool {
    if !condition {
        fputs("[swift_api_test] \(message)\n", stderr)
    }
    return condition
}

let sourceFile = URL(fileURLWithPath: #filePath)
let projectRoot = sourceFile
    .deletingLastPathComponent() // VulkanCADSwiftApiTest
    .deletingLastPathComponent() // Sources
    .deletingLastPathComponent() // swift_api_test
    .deletingLastPathComponent() // samples
    .deletingLastPathComponent() // 3dEngine
let runtimeDirectory = projectRoot.appendingPathComponent("build")

guard CAD_SetRuntimeAssetPath(runtimeDirectory.path) else {
    fputs("[swift_api_test] failed to set runtime asset path to \(runtimeDirectory.path)\n", stderr)
    exit(1)
}

print("[swift_api_test] runtime asset path: \(runtimeDirectory.path)")
print("[swift_api_test] create engine")

guard CAD_CreateEngine() else {
    fputs("[swift_api_test] CAD_CreateEngine failed\n", stderr)
    exit(1)
}

let boxId = CAD_CreateBox(0.0, 0.0, 1.0, 0.5, 0.5, 0.5)
guard boxId != 0 else {
    fputs("[swift_api_test] CAD_CreateBox failed\n", stderr)
    CAD_DestroyEngine()
    exit(1)
}

CAD_SetPosition(boxId, 1.0, 0.0, 1.5)
CAD_SetScale(boxId, 0.8, 0.8, 0.8)
CAD_SetRotationAxisAngle(boxId, 0.0, 0.0, 1.0, 0.7853982)
CAD_SelectObject(boxId, false)
CAD_RequestZoomExtents()

var x: Float = 0.0
var y: Float = 0.0
var z: Float = 0.0
if CAD_GetPosition(boxId, &x, &y, &z) {
    print("[swift_api_test] box position: \(x), \(y), \(z)")
}

print("[swift_api_test] object count: \(CAD_GetObjectCount())")
print("[swift_api_test] selected count: \(CAD_GetSelectedCount())")
print("[swift_api_test] close the GLFW window to finish")

while !CAD_ShouldClose() {
    if !CAD_Tick() {
        break
    }
    Thread.sleep(forTimeInterval: 1.0 / 60.0)
}

CAD_DeleteObject(boxId)
CAD_DestroyEngine()

print("[swift_api_test] done")
