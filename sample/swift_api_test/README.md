# Swift API Test

This sample calls `libVulkanCADCore.dylib` through the C API.

It includes two executables:

- `VulkanCADSwiftApiTest`: lets the engine create its existing GLFW window.
- `VulkanCADSwiftNativeViewTest`: creates an AppKit `NSWindow`/`NSView`, calls `CAD_AttachView()`, and lets the engine render into that native view.

The sample calls `CAD_SetRuntimeAssetPath()` with the project `build/` folder before calling `CAD_CreateEngine()` so the engine can find `shaders/`, `models/`, `textures/`, and `fonts/`.

Build the shared core first from the project root:

```bash
cmake -B build
cmake --build build --target VulkanCADCoreShared
```

Then build and run the Swift package:

```bash
cd samples/swift_api_test
swift run
```

Run the native AppKit view sample:

```bash
cd samples/swift_api_test
swift run VulkanCADSwiftNativeViewTest
```

Native view input currently forwards:

- Mouse: left/right/middle down-up-drag, mouse move, scroll wheel.
- Keyboard: `Esc`, `Delete`, `Enter`, arrows, `O`, `B`, `T`, `R`, `S`, `1`, `2`, `3`, `4`, and printable ASCII keys.
- ImGui viewport overlays such as OSnap symbols, OTRACK lines, transform gizmo, and the fixed axis gizmo are disabled in the native view sample until those overlays move to Vulkan rendering or a native UI backend.

`Package.swift` computes the absolute project `build/` path and adds it as an rpath, so Xcode and SwiftPM can find `libVulkanCADCore.dylib` even when their working directory differs.

If the dynamic library is still not found at runtime, set this in the Xcode scheme:

```text
DYLD_LIBRARY_PATH=/Users/lot700/Desktop/mac_vk/vk_cmake/3dEngine/build
```

Build-only check:

```bash
cd samples/swift_api_test
swift build
```

Known macOS note:
- If SwiftPM reports user cache permission issues inside Codex sandbox, run the same command from the local terminal.
- A warning like `building for macOS-14.0, but linking with dylib ... built for newer version 26.0` means the Swift package target and the local dylib were built with different macOS deployment targets/SDK defaults. The sample still links, but for distribution the deployment target should be aligned.
- `Cannot index window tabs due to missing main bundle identifier` is an AppKit warning from running as a Swift Package executable instead of a full `.app` bundle.

Current flow:

```text
CAD_SetRuntimeAssetPath
CAD_CreateEngine
CAD_CreateBox
CAD_SetPosition
CAD_SetScale
CAD_SetRotationAxisAngle
CAD_SelectObject
CAD_RequestZoomExtents
CAD_Tick until the GLFW window closes
CAD_DeleteObject
CAD_DestroyEngine
```

Native view flow:

```text
Create NSWindow + NSView
CAD_SetRuntimeAssetPath
CAD_AttachView(nsView, width, height)
CAD_CreateEngine
CAD_CreateBox
Timer calls CAD_Tick at about 60fps
Window close calls CAD_DestroyEngine
```
