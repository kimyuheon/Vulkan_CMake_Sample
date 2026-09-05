# Samples

## qml_test

`qml_test`는 Windows, Ubuntu(X11/XWayland), macOS용 Qt6 QML 호스트 샘플입니다.
Qt Creator에서 `samples/qml_test/CMakeLists.txt`를 직접 열 수 있으며 별도 실행 `sh` 파일은 필수가 아닙니다.
엔진 공유 라이브러리를 먼저 빌드한 뒤 `VULKANCAD_ENGINE_BUILD`에 엔진 build 폴더를 지정합니다.

## cpp_api_test

`cpp_api_test` is a small C++ API smoke sample.

It calls the C API directly and lets the engine create its existing GLFW window.
It does not attach Vulkan rendering to an external native view yet.

Current flow:

```text
CAD_CreateEngine
CAD_CreateBox
CAD_SetPosition
CAD_SetScale
CAD_SetRotationAxisAngle
CAD_SelectObject
CAD_RequestZoomExtents
CAD_Tick
CAD_DeleteObject
CAD_DestroyEngine
```

Build:

```bash
cmake -B build
cmake --build build --target VulkanCADCore
cmake --build build --target VulkanCADCppApiTest
```

Run from the build directory:

```bash
./VulkanCADCppApiTest
```

On macOS/Linux, use the same Vulkan SDK runtime environment as `VulkanApp`.

`VulkanCADCore` is currently built as a static library:

```text
macOS/Linux: libVulkanCADCore.a
Windows: VulkanCADCore.lib
```

`VulkanCADCoreShared` is also available for external language integration:

```text
macOS: libVulkanCADCore.dylib
Linux: libVulkanCADCore.so
Windows: VulkanCADCore.dll + import lib
```

Current executable samples still link the static core for stability.

## swift_api_test

`swift_api_test` is a macOS Swift Package sample.

It imports the C API through `Sources/CVulkanCAD/module.modulemap` and links `../../build/libVulkanCADCore.dylib`.
`Package.swift` adds the absolute project `build/` directory as an rpath for Xcode/SwiftPM runs.

Build the shared core first:

```bash
cmake -B build
cmake --build build --target VulkanCADCoreShared
```

Run:

```bash
cd samples/swift_api_test
swift run
```

Build-only:

```bash
cd samples/swift_api_test
swift build
```

This sample keeps the GLFW window open until the user closes it.
