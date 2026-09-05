import AppKit
import CVulkanCAD
import Foundation

struct CADPoint {
    let x: Float
    let y: Float
    let z: Float
}

final class VulkanCADEngine {
    private(set) var isCreated = false
    private var demoObjectIds: [UInt32] = []

    func setRuntimeAssetPath(_ path: String) -> Bool {
        CAD_SetRuntimeAssetPath(path)
    }

    @MainActor func attach(to view: NSView) {
        CAD_AttachView(
            Unmanaged.passUnretained(view).toOpaque(),
            Int32(max(1.0, view.bounds.width)),
            Int32(max(1.0, view.bounds.height)))
    }

    func create() -> Bool {
        guard !isCreated else { return true }
        isCreated = CAD_CreateEngine()
        return isCreated
    }

    func tick() -> Bool {
        guard isCreated else { return false }
        return CAD_Tick()
    }

    func resize(to size: CGSize) {
        guard isCreated else { return }
        CAD_ResizeView(
            Int32(max(1.0, size.width)),
            Int32(max(1.0, size.height)))
    }

    /// 엔진 명령을 이름으로 실행한다 — 하단 명령행에 치는 것과 같다.
    ///
    /// 개별 래퍼(createBox 등)는 자주 쓰는 것만 있고 엔진에는 명령이 200개가 넘는다.
    /// 없는 기능은 이걸로 부르면 된다. 진행 중인 도구엔 값으로 전달된다.
    ///
    ///     engine.execute("trim")
    ///     engine.execute("circle"); engine.execute("2")   // 반지름 2
    @discardableResult
    func execute(_ command: String) -> Bool {
        guard isCreated else { return false }
        return CAD_ExecuteCommand(command)
    }

    // ── 알림(콜백) ──
    // ⚠️ Swift 클로저를 그대로 넘길 수 없다(C 함수 포인터는 문맥을 못 가진다).
    //    전역에 보관해 두고 C 함수에서 꺼내 쓴다.
    //    호출 시점은 tick() 안이라 여기서 UI 를 바로 고쳐도 된다.
    //    ⚠️ C 함수 포인터는 @convention(c) 라 액터에 속할 수 없다. 그런데 보관소는
    //    @MainActor 다 — 그래서 안에서 assumeIsolated 로 "여기는 메인" 임을 알린다.
    //    거짓말이 아니라 사실이다: 이 콜백들은 전부 CAD_Tick 안에서 불린다.
    @MainActor
    func onSelectionChanged(_ handler: (() -> Void)?) {
        VulkanCADEngineCallbacks.selectionChanged = handler
        CAD_SetOnSelectionChanged(handler == nil ? nil : {
            MainActor.assumeIsolated { VulkanCADEngineCallbacks.selectionChanged?() }
        })
    }

    @MainActor
    func onObjectCreated(_ handler: ((UInt32) -> Void)?) {
        VulkanCADEngineCallbacks.objectCreated = handler
        CAD_SetOnObjectCreated(handler == nil ? nil : { id in
            MainActor.assumeIsolated { VulkanCADEngineCallbacks.objectCreated?(id) }
        })
    }

    @MainActor
    func onObjectDeleted(_ handler: ((UInt32) -> Void)?) {
        VulkanCADEngineCallbacks.objectDeleted = handler
        CAD_SetOnObjectDeleted(handler == nil ? nil : { id in
            MainActor.assumeIsolated { VulkanCADEngineCallbacks.objectDeleted?(id) }
        })
    }

    @MainActor
    func onPrompt(_ handler: ((String) -> Void)?) {
        VulkanCADEngineCallbacks.prompt = handler
        CAD_SetOnPrompt(handler == nil ? nil : { text in
            let s = text.map { String(cString: $0) } ?? ""
            MainActor.assumeIsolated { VulkanCADEngineCallbacks.prompt?(s) }
        })
    }

    func createDemoScene() {
        guard isCreated, demoObjectIds.isEmpty else { return }

        let boxId = createBox(
            center: CADPoint(x: 0.0, y: 0.0, z: 1.0),
            scale: CADPoint(x: 0.5, y: 0.5, z: 0.5))
        if boxId != 0 {
            CAD_SetPosition(boxId, 1.0, 0.0, 1.5)
            CAD_SetScale(boxId, 0.8, 0.8, 0.8)
            CAD_SetRotationAxisAngle(boxId, 0.0, 0.0, 1.0, 0.7853982)
            CAD_SelectObject(boxId, false)
        }

        _ = createLine(
            from: CADPoint(x: -3.0, y: -1.4, z: 0.05),
            to: CADPoint(x: -1.2, y: -0.2, z: 0.05))
        _ = createCircle(
            center: CADPoint(x: -2.0, y: 1.0, z: 0.05),
            radius: 0.55,
            normal: CADPoint(x: 0.0, y: 0.0, z: 1.0))
        _ = createPolygon(
            center: CADPoint(x: 0.0, y: -1.3, z: 0.05),
            radius: 0.65,
            sides: 6,
            normal: CADPoint(x: 0.0, y: 0.0, z: 1.0),
            firstVertexDir: CADPoint(x: 1.0, y: 0.0, z: 0.0))
        _ = createArc(
            start: CADPoint(x: 1.1, y: -1.1, z: 0.05),
            through: CADPoint(x: 1.9, y: -0.2, z: 0.05),
            end: CADPoint(x: 2.7, y: -1.1, z: 0.05))
        _ = createPolyline(
            points: [
                CADPoint(x: 1.2, y: 0.7, z: 0.05),
                CADPoint(x: 1.8, y: 1.3, z: 0.05),
                CADPoint(x: 2.5, y: 0.9, z: 0.05),
                CADPoint(x: 2.7, y: 1.6, z: 0.05)
            ],
            closed: false)

        CAD_RequestZoomExtents()
    }

    @discardableResult
    func createBox(center: CADPoint, scale: CADPoint) -> UInt32 {
        guard isCreated else { return 0 }
        let id = CAD_CreateBox(center.x, center.y, center.z, scale.x, scale.y, scale.z)
        trackDemoObject(id)
        return id
    }

    @discardableResult
    func createLine(from start: CADPoint, to end: CADPoint) -> UInt32 {
        guard isCreated else { return 0 }
        let id = CAD_CreateLine(start.x, start.y, start.z, end.x, end.y, end.z)
        trackDemoObject(id)
        return id
    }

    @discardableResult
    func createCircle(center: CADPoint, radius: Float, normal: CADPoint) -> UInt32 {
        guard isCreated else { return 0 }
        let id = CAD_CreateCircle(center.x, center.y, center.z, radius, normal.x, normal.y, normal.z)
        trackDemoObject(id)
        return id
    }

    @discardableResult
    func createPolygon(
        center: CADPoint,
        radius: Float,
        sides: Int32,
        normal: CADPoint,
        firstVertexDir: CADPoint
    ) -> UInt32 {
        guard isCreated else { return 0 }
        let id = CAD_CreatePolygon(
            center.x, center.y, center.z,
            radius,
            sides,
            normal.x, normal.y, normal.z,
            firstVertexDir.x, firstVertexDir.y, firstVertexDir.z)
        trackDemoObject(id)
        return id
    }

    @discardableResult
    func createArc(start: CADPoint, through: CADPoint, end: CADPoint) -> UInt32 {
        guard isCreated else { return 0 }
        let id = CAD_CreateArc(
            start.x, start.y, start.z,
            through.x, through.y, through.z,
            end.x, end.y, end.z)
        trackDemoObject(id)
        return id
    }

    @discardableResult
    func createPolyline(points: [CADPoint], closed: Bool) -> UInt32 {
        guard isCreated, points.count >= 2 else { return 0 }
        let xyz = points.flatMap { [$0.x, $0.y, $0.z] }
        let id = xyz.withUnsafeBufferPointer { buffer in
            CAD_CreatePolyline(buffer.baseAddress, UInt32(points.count), closed)
        }
        trackDemoObject(id)
        return id
    }

    @discardableResult
    func createRectangle(corner0: CADPoint, corner2: CADPoint) -> UInt32 {
        guard isCreated else { return 0 }
        let id = CAD_CreateRectangle(
            corner0.x, corner0.y, corner0.z,
            corner2.x, corner2.y, corner2.z)
        trackDemoObject(id)
        return id
    }

    @discardableResult
    func createExtrude(sourceId: UInt32, height: Float) -> UInt32 {
        guard isCreated else { return 0 }
        let id = CAD_CreateExtrude(sourceId, height)
        trackDemoObject(id)
        return id
    }

    func startLineSketch() { guard isCreated else { return }; CAD_RequestStartLineSketch() }
    func startRectangleSketch() { guard isCreated else { return }; CAD_RequestStartRectangleSketch() }
    func startCircleSketch() { guard isCreated else { return }; CAD_RequestStartCircleSketch() }
    func startPolygonSketch() { guard isCreated else { return }; CAD_RequestStartPolygonSketch() }
    func cyclePolygonSides() { guard isCreated else { return }; CAD_RequestCyclePolygonSides() }
    func startExtrude() { guard isCreated else { return }; CAD_RequestStartExtrude() }
    func startArcSketch() { guard isCreated else { return }; CAD_RequestStartArcSketch() }
    func cycleArcMode() { guard isCreated else { return }; CAD_RequestCycleArcMode() }
    func startPolylineSketch() { guard isCreated else { return }; CAD_RequestStartPolylineSketch() }
    func startLightPlacement() { guard isCreated else { return }; CAD_RequestStartLightPlacement() }
    func cycleMaterial() { guard isCreated else { return }; CAD_RequestCycleMaterial() }
    func removeMaterial() { guard isCreated else { return }; CAD_RequestRemoveMaterial() }
    func adjustTextureScale(_ delta: Float) {
        guard isCreated else { return }
        CAD_RequestAdjustTextureScale(delta)
    }
    func adjustLineWidth(_ deltaPx: Float) {
        guard isCreated else { return }
        CAD_RequestAdjustLineWidth(deltaPx)
    }
    func moveSelected(direction: CADPoint, distance: Float) {
        guard isCreated else { return }
        CAD_RequestMoveSelected(direction.x, direction.y, direction.z, distance)
    }
    func rotateSelected(axis: CADPoint, angleDegrees: Float) {
        guard isCreated else { return }
        CAD_RequestRotateSelected(axis.x, axis.y, axis.z, angleDegrees)
    }
    func scaleSelected(factor: Float) {
        guard isCreated else { return }
        CAD_RequestScaleSelected(factor)
    }
    func copySelected(direction: CADPoint, distance: Float, count: Int32) {
        guard isCreated else { return }
        CAD_RequestCopySelected(direction.x, direction.y, direction.z, distance, count)
    }
    func colorSelected(_ color: CADPoint) {
        guard isCreated else { return }
        CAD_RequestColorSelected(color.x, color.y, color.z)
    }

    func mouseDown(button: Int32, x: Double, y: Double, modifiers: Int32) {
        guard isCreated else { return }
        CAD_OnMouseDown(button, x, y, modifiers)
    }

    func mouseUp(button: Int32, x: Double, y: Double, modifiers: Int32) {
        guard isCreated else { return }
        CAD_OnMouseUp(button, x, y, modifiers)
    }

    func mouseMove(x: Double, y: Double) {
        guard isCreated else { return }
        CAD_OnMouseMove(x, y)
    }

    func mouseWheel(x: Double, y: Double, deltaX: Double, deltaY: Double) {
        guard isCreated else { return }
        CAD_OnMouseWheel(x, y, deltaX, deltaY)
    }

    func keyDown(_ key: Int32, modifiers: Int32) {
        guard isCreated else { return }
        CAD_OnKeyDown(key, modifiers)
    }

    func keyUp(_ key: Int32, modifiers: Int32) {
        guard isCreated else { return }
        CAD_OnKeyUp(key, modifiers)
    }

    func shutdown() {
        for id in demoObjectIds.reversed() {
            CAD_DeleteObject(id)
        }
        demoObjectIds.removeAll()
        if isCreated {
            CAD_DestroyEngine()
            isCreated = false
        }
    }

    private func trackDemoObject(_ id: UInt32) {
        if id != 0 {
            demoObjectIds.append(id)
        }
    }
}

/// C 콜백이 꺼내 쓸 보관소.
///
/// C 함수 포인터는 문맥(캡처)을 가질 수 없어서 Swift 클로저를 그대로 못 넘긴다.
/// 여기 담아 두고 C 쪽 함수에서 꺼내 부른다. 호출은 CAD_Tick 안 = 메인 스레드다.
///
/// `@MainActor` 인 이유: Swift 6 는 nonisolated 한 static var 를 "동시성 안전하지 않음" 으로
/// 막는다(MutableGlobalVariable). 실제로 여기 접근하는 곳은 전부 메인 스레드
/// (설정은 UI, 호출은 CAD_Tick)이라 액터를 명시하는 게 사실에 맞고 검사도 통과한다.
@MainActor
enum VulkanCADEngineCallbacks {
    static var selectionChanged: (() -> Void)?
    static var objectCreated: ((UInt32) -> Void)?
    static var objectDeleted: ((UInt32) -> Void)?
    static var prompt: ((String) -> Void)?
}
