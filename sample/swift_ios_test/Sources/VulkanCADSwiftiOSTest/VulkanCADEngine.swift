import UIKit
import CVulkanCAD
import Foundation

struct CADPoint {
    let x: Float
    let y: Float
    let z: Float
}

// macOS 의 VulkanCADEngine 을 iOS UIView 기반으로 미러링.
// 거의 동일한 API — attach 만 UIView 를 받고, 내부적으로 CAD_AttachView 에 UIView 포인터 전달.
final class VulkanCADEngine {
    private(set) var isCreated = false
    private var demoObjectIds: [UInt32] = []

    func setRuntimeAssetPath(_ path: String) -> Bool {
        CAD_SetRuntimeAssetPath(path)
    }

    @MainActor func attach(to view: UIView) {
        CAD_AttachView(
            Unmanaged.passUnretained(view).toOpaque(),
            Int32(max(1.0, view.bounds.width)),
            Int32(max(1.0, view.bounds.height)))
    }

    func create() -> Bool {
        guard !isCreated else { return true }
        isCreated = CAD_CreateEngine()
        // 손가락/가상 커서는 마우스만큼 정밀하지 않다 — 스냅·선 픽 반경을 3.5배(12→42px)로.
        // 이 호출이 빠지면 모바일에서도 데스크톱과 같은 12px 이라 스냅이 거의 안 잡힌다.
        if isCreated { CAD_SetSnapTouchMode(true) }
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

    // ─── 툴바 버튼 (Android MainActivity 미러링) ───
    // 큐브 추가/전체선택/삭제/전체삭제/줌은 appCommands_ 큐를 거쳐 iOS render-on-demand 에서도
    // 자동으로 한 프레임 렌더됨. Undo/Redo 는 즉시 실행이라 C++ 측에서 markInputDirty 처리.
    func addCube()          { guard isCreated else { return }; CAD_RequestAddCube() }
    func selectAll()        { guard isCreated else { return }; CAD_RequestSelectAll() }
    func deleteSelected()   { guard isCreated else { return }; CAD_RequestDeleteSelected() }
    func clearAll()         { guard isCreated else { return }; CAD_RequestClearAll() }
    func zoomExtents()      { guard isCreated else { return }; CAD_RequestZoomExtents() }

    // ─── 명령 실행 ───
    // 데스크톱 명령행과 같은 이름을 그대로 쓴다. 진행 중인 도구에 좌표/값도 넘길 수 있다
    // (예: executeCommand("2,3")). 모바일은 키보드를 띄우기 부담스러워 버튼에서 호출한다.
    @discardableResult
    func executeCommand(_ name: String) -> Bool {
        guard isCreated else { return false }
        return CAD_ExecuteCommand(name)
    }

    // ─── 측정 — 선택만 하면 값이 나온다 ───
    // 점 찍기·스냅·치수 객체 생성이 전혀 없어서 모바일에서 가장 쓰기 쉬운 경로다.
    // 잴 게 없거나 선택이 비면 0.
    func selectedCount() -> UInt32 { isCreated ? CAD_GetSelectedCount() : 0 }
    /// 진행 중인 치수 도구의 점 개수. 비활성이면 -1.
    /// 클라이언트가 클릭을 세면 엔진이 클릭을 거부할 때 어긋나므로 **엔진 값을 본다**.
    func dimensionPointCount() -> Int32 { isCreated ? CAD_GetDimensionPointCount() : -1 }

    /// 진행 중인 도구의 안내 문구. 도구가 없으면 빈 문자열.
    /// 도구 종류와 무관하게 "지금 뭘 해야 하는지" 를 엔진에서 그대로 받아 쓴다.
    func prompt() -> String {
        guard isCreated else { return "" }
        var buf = [CChar](repeating: 0, count: 256)
        let n = CAD_GetPrompt(&buf, Int32(buf.count))
        return n > 0 ? (String(cString: buf)) : ""
    }

    // ─── 그리기 (전부 점 찍기 → 커서로 데스크톱과 동일하게 동작) ───
    func drawLine()      { executeCommand("line") }
    func drawRectangle() { executeCommand("rec") }
    func drawCircle()    { executeCommand("c") }
    func drawArc()       { executeCommand("a") }
    func drawPolyline()  { executeCommand("pl") }
    func drawPolygon()   { executeCommand("pol") }
    func selectionLength() -> Float { isCreated ? CAD_GetSelectionLength() : 0 }
    func selectionArea() -> Float { isCreated ? CAD_GetSelectionArea() : 0 }

    // ─── 치수 (전부 "점 찍기" 방식 — 대화상자 불필요라 모바일에서 그대로 동작) ───
    func dimAligned()  { executeCommand("dim") }   // 정렬 치수
    func dimLinear()   { executeCommand("dli") }   // 수평·수직
    func dimAngular()  { executeCommand("dan") }   // 각도
    func dimRadius()   { executeCommand("dra") }   // 반지름
    func dimDiameter() { executeCommand("ddi") }   // 지름

    /// 경로의 모델/도면을 씬에 연다. 지원: .lot .obj .stl .dxf .ply .gltf .glb
    /// - 주의: `.gltf`(분리형)는 `.bin`·텍스처가 같은 폴더에 있어야 한다.
    ///   모바일 파일 선택기는 파일 하나만 주므로 **단일 파일인 `.glb` 권장**.
    @discardableResult
    func openFile(_ path: String) -> Bool {
        guard isCreated else { return false }
        return CAD_OpenFile(path)
    }
    // viewType: 0=Front 1=Top 2=Right 3=Iso
    func setView(_ viewType: Int32) { guard isCreated else { return }; CAD_RequestSetView(viewType) }
    func undo()             { guard isCreated else { return }; CAD_Undo() }
    func redo()             { guard isCreated else { return }; CAD_Redo() }
    func objectCount() -> UInt32 { isCreated ? CAD_GetObjectCount() : 0 }

    // ─── 명령 요청 (SwiftUI 버튼/패널용) ───
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

    // ─── 입력 (터치/제스처 → 엔진 마우스 이벤트) ───
    // 엔진 버튼 규약: 0=left(select), 1=right(orbit), 2=middle(pan)
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

    // (x,y) 에 기즈모 핸들이 있는지 — 1손가락 드래그를 기즈모 조작 vs 궤도회전으로 라우팅.
    func gizmoHitTest(x: Double, y: Double) -> Bool {
        guard isCreated else { return false }
        return CAD_GizmoHitTest(x, y) != 0
    }

    func setGizmoTouchMode(_ enabled: Bool) {
        guard isCreated else { return }
        CAD_SetGizmoTouchMode(enabled)
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
