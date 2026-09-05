import SwiftUI
import UIKit
import UniformTypeIdentifiers

struct ContentView: View {
    @StateObject private var controller = VulkanCADController()
    // 앱 라이프사이클 — background 진입 시 렌더 정지 (Metal layer invalidate 와 race 회피)
    @Environment(\.scenePhase) private var scenePhase
    // 파일 열기 — OS 문서 선택기(UIDocumentPicker). 모바일엔 데스크톱 파일 다이얼로그가
    // 없어서(openModelFileDialog 는 iOS 에서 no-op 스텁) OS 선택기로 경로를 받아
    // CAD_OpenFile 에 넘긴다.
    @State private var showOpenPicker = false
    // "측정" 을 누르면 치수 종류 줄이 위에 하나 더 뜬다(점진적 노출 — 좁은 화면 대응).
    @State private var showDimMenu = false
    // "그리기" 줄 (선/사각형/원/호/폴리선/다각형)
    @State private var showDrawMenu = false
    @State private var showViewMenu = false
    // 숫자 입력 — CAD 정밀도는 "정확히 가리키기" 가 아니라 **숫자**에서 온다.
    // 전체 키보드는 3D 뷰를 덮어 못 쓰므로 좁은 숫자패드만 띄운다.
    @State private var numInput = ""

    var body: some View {
        ZStack {
            VulkanCADViewRepresentable(engine: controller.engine) { view in
                controller.start(view: view)
            }
            .ignoresSafeArea()

            // 단순 상태 표시 + 하단 툴바 (Android MainActivity 미러링)
            VStack(alignment: .leading, spacing: 6) {
                HStack {
                    Text(controller.statusText)
                        .font(.caption)
                        .padding(8)
                        .background(.black.opacity(0.5))
                        .foregroundColor(.white)
                        .cornerRadius(6)
                    Spacer()
                }
                // 선택한 것의 길이·면적 — 선택이 있을 때만 뜬다.
                if !controller.measureText.isEmpty {
                    HStack {
                        Text(controller.measureText)
                            .font(.callout.weight(.semibold).monospacedDigit())
                            .padding(.vertical, 8)
                            .padding(.horizontal, 12)
                            .background(.black.opacity(0.65))
                            .foregroundColor(.green)
                            .cornerRadius(8)
                        Spacer()
                    }
                }
                Spacer()
                toolbar
            }
            .padding()
        }
        .onDisappear {
            controller.shutdown()
        }
        .fileImporter(isPresented: $showOpenPicker,
                      allowedContentTypes: Self.openableTypes,
                      allowsMultipleSelection: false) { result in
            guard case .success(let urls) = result, let url = urls.first else { return }
            // iCloud/타 앱 폴더의 파일은 보안 스코프를 열어야 읽을 수 있다.
            let scoped = url.startAccessingSecurityScopedResource()
            defer { if scoped { url.stopAccessingSecurityScopedResource() } }
            if controller.engine.openFile(url.path) {
                controller.engine.zoomExtents()
                controller.statusText = "열기: \(url.lastPathComponent)"
            } else {
                controller.statusText = "열기 실패: \(url.lastPathComponent)"
            }
        }
        .onChange(of: scenePhase) { newPhase in
            switch newPhase {
            case .active:
                controller.resume()
            case .inactive, .background:
                // 시뮬레이터 닫기 / 앱 종료 / 홈 화면 진입 — 렌더 정지해서 swap chain race 회피
                controller.pause()
            @unknown default:
                break
            }
        }
    }

    // CAD_OpenFile 이 받는 확장자들. .glb/.gltf/.lot 등은 시스템 UTType 이 없어
    // filenameExtension 으로 만든다(없으면 nil → compactMap 으로 제외).
    // ⚠️ .gltf(분리형)는 .bin·텍스처가 같은 폴더에 있어야 해서 모바일에선 잘 안 열린다.
    //    단일 파일인 .glb 를 쓰는 것이 안전하다.
    private static let openableTypes: [UTType] = {
        let exts = ["glb", "gltf", "obj", "stl", "dxf", "ply", "lot"]
        let types = exts.compactMap { UTType(filenameExtension: $0) }
        return types.isEmpty ? [.data] : types + [.data]
    }()

    // 하단 툴바 — 치수 종류 줄(선택적) + 기본 줄.
    private var toolbar: some View {
        VStack(spacing: 8) {
            if showDrawMenu { drawMenuRow }
            if showDimMenu { dimMenuRow }
            if showViewMenu { viewMenuRow }
            if controller.cursorToolActive {
                if !controller.promptText.isEmpty { promptLabel }
                // 치수는 값을 안 받는다 — 패드를 띄우면 쳐도 반응이 없어 오해를 부른다.
                if controller.acceptsValueInput { numPadRow }
                cursorToolRow
            }
            mainToolbarRow
        }
    }

    // 뷰 종류 — "뷰" 를 누르면 위에 뜬다 (그리기·측정과 같은 점진적 노출).
    // TOP 으로 놓고 살짝 기울인 뒤 박스를 그리는 데스크톱 흐름을 한 탭으로. 고르면 줄은 닫힌다.
    private var viewMenuRow: some View {
        ScrollView(.horizontal, showsIndicators: false) {
            HStack(spacing: 8) {
                toolButton("Top")   { controller.engine.setView(1); showViewMenu = false }
                toolButton("Front") { controller.engine.setView(0); showViewMenu = false }
                toolButton("Right") { controller.engine.setView(2); showViewMenu = false }
                toolButton("Iso")   { controller.engine.setView(3); showViewMenu = false }
                toolButton("투영")  { controller.engine.executeCommand("proj"); showViewMenu = false }  // 직교 ↔ 원근
                toolButton("✕")     { showViewMenu = false }
            }
            .padding(.horizontal, 4)
        }
    }

    // 치수 종류 — 전부 "점 찍기" 방식이라 대화상자 없이 모바일에서 그대로 동작한다.
    private var dimMenuRow: some View {
        ScrollView(.horizontal, showsIndicators: false) {
            HStack(spacing: 8) {
                toolButton("정렬")   { startDim { controller.engine.dimAligned() } }
                toolButton("일반")   { startDim { controller.engine.dimLinear() } }
                toolButton("각도")   { startDim { controller.engine.dimAngular() } }
                toolButton("반지름") { startDim { controller.engine.dimRadius() } }
                toolButton("지름")   { startDim { controller.engine.dimDiameter() } }
                toolButton("✕")     { showDimMenu = false }
            }
            .padding(.horizontal, 4)
        }
    }

    // 도구를 켠 뒤엔 3D 뷰를 봐야 하므로 종류 줄은 닫고, 커서 모드로 들어간다.
    private func startDim(_ action: () -> Void) {
        action()
        showDimMenu = false
        controller.beginCursorTool()
    }

    // 그리기 — 전부 점 찍기 방식이라 커서로 데스크톱과 동일하게 동작한다.
    private var drawMenuRow: some View {
        ScrollView(.horizontal, showsIndicators: false) {
            HStack(spacing: 8) {
                toolButton("선")     { startTool { controller.engine.drawLine() } }
                toolButton("사각형") { startTool { controller.engine.drawRectangle() } }
                toolButton("원")     { startTool { controller.engine.drawCircle() } }
                toolButton("호")     { startTool { controller.engine.drawArc() } }
                toolButton("폴리선") { startTool { controller.engine.drawPolyline() } }
                toolButton("다각형") { startTool { controller.engine.drawPolygon() } }
                // ── 3D — 전부 클릭+값 방식이라 2D 와 같은 커서·숫자패드 흐름 ──
                //   박스: 코너 → 반대 코너(또는 가로,세로) → 높이   돌출: 닫힌 2D 선택 후 높이
                //   밀당: 면 클릭 → 거리.  구·원기둥·원뿔·토러스는 즉시 생성(커서 도구 아님).
                toolButton("박스")   { startTool { controller.engine.executeCommand("box") } }
                toolButton("돌출")   { startTool { controller.engine.executeCommand("x") } }
                toolButton("밀당")   { startTool { controller.engine.executeCommand("pp") } }
                toolButton("구")     { controller.engine.executeCommand("sphere");   showDrawMenu = false }
                toolButton("원기둥") { controller.engine.executeCommand("cylinder"); showDrawMenu = false }
                toolButton("원뿔")   { controller.engine.executeCommand("cone");     showDrawMenu = false }
                toolButton("토러스") { controller.engine.executeCommand("torus");    showDrawMenu = false }
                toolButton("✕")     { showDrawMenu = false }
            }
            .padding(.horizontal, 4)
        }
    }

    // 엔진이 주는 안내 문구 — 도구 종류와 무관하게 "지금 뭘 해야 하는지" 를 그대로 보여준다.
    private var promptLabel: some View {
        HStack {
            Text(controller.promptText)
                .font(.caption).padding(.vertical, 6).padding(.horizontal, 10)
                .background(.black.opacity(0.6)).foregroundColor(.orange).cornerRadius(6)
            Spacer()
        }
    }

    // 숫자패드 — 진행 중인 도구에 값을 그대로 전달한다(엔진 명령행과 같은 입구).
    // 예: 선 시작점 찍고 방향만 대충 잡은 뒤 "3000" → 정확히 3m.
    //
    // 5열 × 3줄 고정. 가로 스크롤이었을 땐 키가 화면 밖으로 밀려 숫자 하나 누르려고
    // 매번 좌우로 밀어야 했다 — 도면을 보며 값을 치는 흐름이 계속 끊긴다.
    // ↵ 는 격자에서 빼 값칸 옆에 둔다. 그래야 15칸에 0-9 . , − = ⌫ 가 정확히 맞아
    // 한 줄이 줄고(뷰 7%p 더 보임), "값 치고 바로 옆에서 확정" 이라 동선도 짧다.
    private static let numKeyRows: [[String]] = [
        ["1", "2", "3", "4", "5"],
        ["6", "7", "8", "9", "0"],
        [".", ",", "-", "=", "⌫"],
    ]

    private var numPadRow: some View {
        VStack(spacing: 6) {
            // 값 표시줄 + 확정. 비어 있어도 자리를 지켜야 키 위치가 위아래로 흔들리지 않는다.
            HStack(spacing: 6) {
                Text(numInput.isEmpty ? " " : numInput)
                    .font(.callout.monospacedDigit().weight(.bold))
                    .frame(maxWidth: .infinity, alignment: .leading)
                    .padding(.vertical, 8).padding(.horizontal, 12)
                    .background(Color.yellow.opacity(0.13))
                    .foregroundColor(.yellow).cornerRadius(6)
                Button(action: commitNumInput) {
                    Text("↵")
                        .font(.callout.weight(.bold))
                        .padding(.vertical, 8).padding(.horizontal, 16)
                        .background(numInput.isEmpty ? Color.gray.opacity(0.35) : Color.yellow)
                        .foregroundColor(numInput.isEmpty ? .white : .black)
                        .cornerRadius(6)
                }
                .disabled(numInput.isEmpty)
            }
            ForEach(Self.numKeyRows, id: \.self) { row in
                HStack(spacing: 6) {
                    ForEach(row, id: \.self) { k in
                        numKey(k) {
                            if k == "⌫" { if !numInput.isEmpty { numInput.removeLast() } }
                            else        { numInput += k }
                        }
                    }
                }
            }
        }
        .padding(.horizontal, 4)
    }

    /// 값 확정 — 엔진 명령행과 같은 입구라 데스크톱 문법(상대 기본 · `=` 절대)이 그대로 통한다.
    private func commitNumInput() {
        guard !numInput.isEmpty else { return }
        controller.engine.executeCommand(numInput)
        numInput = ""
    }

    // 5열이라 폭은 균등 분할(maxWidth: .infinity). 390pt 화면에서 한 칸 약 70pt —
    // 애플 권장 최소 44pt 의 1.6배라 좁지 않다.
    private func numKey(_ label: String, _ action: @escaping () -> Void) -> some View {
        Button(action: action) {
            Text(label)
                .font(.callout.weight(.semibold).monospacedDigit())
                .frame(maxWidth: .infinity)
                .padding(.vertical, 10)
                .background(.black.opacity(0.55)).foregroundColor(.white).cornerRadius(6)
        }
    }

    // 도구를 켠 뒤엔 3D 뷰를 봐야 하므로 종류 줄은 닫고, 커서 모드로 들어간다.
    private func startTool(_ action: () -> Void) {
        action()
        showDrawMenu = false
        showDimMenu = false
        numInput = ""
        controller.beginCursorTool()
    }

    // 커서 도구 줄 — 버튼은 하나이고 **문구만 단계에 따라 바뀐다**.
    //   선택 1 → 첫 번째 점, 선택 2 → 두 번째 점, 선택 → 치수선 위치(생성)
    private var cursorToolRow: some View {
        HStack(spacing: 8) {
            toolButton(controller.cursorStepLabel) { controller.commitCursorPoint() }
            toolButton("취소") { controller.endCursorTool() }
            Spacer()
        }
    }

    // 기본 줄 — 안드로이드(큐브/전체선택/삭제/줌/Iso/Undo)와 동일 구성 + 열기/그리기/측정.
    // 두 메뉴는 서로 배타 — 하나를 열면 다른 하나는 닫는다(두 줄이 겹쳐 뜨면 뷰가 좁아진다).
    private var mainToolbarRow: some View {
        ScrollView(.horizontal, showsIndicators: false) {
            HStack(spacing: 8) {
                toolButton("열기")   { showOpenPicker = true }
                toolButton("그리기") { showDrawMenu.toggle(); showDimMenu = false; showViewMenu = false }
                toolButton("측정")   { showDimMenu.toggle(); showDrawMenu = false; showViewMenu = false }
                toolButton("뷰")     { showViewMenu.toggle(); showDrawMenu = false; showDimMenu = false }
                toolButton("큐브")   { controller.engine.addCube() }
                toolButton("전체선택") { controller.engine.selectAll() }
                toolButton("삭제")   { controller.engine.deleteSelected() }
                toolButton("줌")     { controller.engine.zoomExtents() }
                toolButton("Undo")   { controller.engine.undo() }
            }
            .padding(.horizontal, 4)
        }
    }

    private func toolButton(_ label: String, _ action: @escaping () -> Void) -> some View {
        Button(action: action) {
            Text(label)
                .font(.subheadline.weight(.medium))
                .padding(.vertical, 10)
                .padding(.horizontal, 14)
                .background(.black.opacity(0.55))
                .foregroundColor(.white)
                .cornerRadius(8)
        }
    }
}

@MainActor
final class VulkanCADController: ObservableObject {
    let engine = VulkanCADEngine()
    @Published var statusText: String = "starting…"
    /// 선택한 것의 길이·면적. 선택이 없으면 빈 문자열(라벨 숨김).
    @Published var measureText: String = ""

    // ─── 커서 도구 (치수 등) ───────────────────────────────────
    // 커서가 마우스를 대신하므로 데스크톱 도구 흐름(점1 → 점2 → 치수선 위치)을
    // 그대로 쓴다. 터치용으로 도구를 다시 만들 필요가 없다.
    @Published var cursorToolActive = false
    @Published private(set) var cursorStep = 0
    /// 엔진이 주는 안내 문구. 도구가 없으면 빈 문자열 — **도구 활성 여부의 기준**이기도 하다.
    @Published private(set) var promptText = ""
    private var lastPrompt = ""
    weak var metalView: VulkanCADMetalView?

    /// 숫자패드를 띄울지 — 진행 중인 도구가 **값을 실제로 받을 때만**.
    ///
    /// 치수는 순수 점 찍기라 엔진 `feedActiveToolInput` 에 분기 자체가 없다. 값을 보내면
    /// 마지막 `return true`(입력 소비)로 조용히 삼켜져서, 사용자에겐 "패드가 있길래 쳤는데
    /// 반응이 없다" 가 된다. 그래서 치수 중엔 패드를 아예 감춘다.
    ///
    /// 판정은 **엔진에 물어본다** — 클라이언트가 도구 종류를 기억해 두면 ESC·자동 종료로
    /// 엔진만 도구를 끝냈을 때 어긋난다(치수 단계 세다가 틀어졌던 것과 같은 함정).
    var acceptsValueInput: Bool {
        engine.dimensionPointCount() < 0   // -1 = 치수 비활성 = 그리기 도구
    }

    /// 버튼은 하나이고 문구만 단계별로 바뀐다.
    var cursorStepLabel: String {
        // 치수만 단계가 정해져 있다. 그리기는 점 개수가 열려 있어(폴리선 등) 그냥 "선택".
        guard engine.dimensionPointCount() >= 0 else { return "선택" }
        switch cursorStep {
        case 0:  return "선택 1"
        case 1:  return "선택 2"
        default: return "선택"
        }
    }

    func beginCursorTool() {
        cursorStep = 0
        cursorToolActive = true
        metalView?.setCursorMode(true)
    }

    func endCursorTool() {
        cursorToolActive = false
        cursorStep = 0
        metalView?.setCursorMode(false)
        engine.executeCommand("esc")   // 진행 중이던 도구 취소
    }

    func commitCursorPoint() {
        metalView?.commitCursorClick()
        // 단계는 세지 않는다 — 다음 틱에 엔진 값(dimensionPointCount)으로 갱신된다.
    }

    /// 매 틱 엔진 상태를 반영. **도구 종류와 무관하게** 프롬프트로 활성 여부를 판단하므로
    /// 치수든 그리기든 같은 코드로 동작한다. 엔진이 클릭을 거부해도 어긋나지 않는다.
    private func syncCursorTool() {
        guard cursorToolActive else { return }
        let p = engine.prompt()
        if p != lastPrompt { lastPrompt = p; promptText = p }

        // 치수는 점 개수로 단계를 보여주고, 그 외 도구는 프롬프트만 쓴다.
        let n = engine.dimensionPointCount()
        if n >= 0 {
            if Int(n) != cursorStep { cursorStep = Int(n) }
            return
        }
        // 치수가 아닌 도구: 프롬프트가 비면 도구가 끝난 것으로 본다.
        if p.isEmpty {
            cursorToolActive = false
            cursorStep = 0
            promptText = ""
            lastPrompt = ""
            metalView?.setCursorMode(false)
        }
    }
    private var displayLink: CADisplayLink?
    // 매 프레임 폴링하되 **값이 바뀔 때만** @Published 를 건드린다.
    // 60fps 로 갱신하면 SwiftUI 가 매 프레임 다시 그려 렌더와 경합한다.
    private var lastMeasureKey: String = ""

    func start(view: UIView) {
        metalView = view as? VulkanCADMetalView
        // 런타임 에셋 위치 — 빌드 시 models/ textures/ fonts/ 가 .app 번들 내부 Resources 에
        // 복사되어 있다고 가정. 없으면 엔진은 cwd 기반 fallback. (셰이더는 라이브러리에 내장)
        let bundleResources = Bundle.main.resourcePath ?? ""
        _ = engine.setRuntimeAssetPath(bundleResources)

        engine.attach(to: view)
        guard engine.create() else {
            statusText = "CAD_CreateEngine failed"
            return
        }

        let scale = view.window?.screen.scale ?? UIScreen.main.scale
        engine.resize(to: CGSize(width: view.bounds.width * scale,
                                  height: view.bounds.height * scale))

        engine.createDemoScene()
        engine.setGizmoTouchMode(true)   // 손가락용 핸들 hit 허용범위 확대
        statusText = "engine ready"

        // 60fps 렌더 루프 — UIKit 의 CADisplayLink 는 화면 갱신과 동기
        startDisplayLink()
    }

    @objc private func tick(_ link: CADisplayLink) {
        if !engine.tick() {
            statusText = "engine tick failed"
            shutdown()
            return
        }
        updateMeasure()
        syncCursorTool()
    }

    /// 선택 → 길이·면적을 읽어 라벨 문자열을 만든다.
    /// 점 찍기·스냅 없이 **탭 한 번**으로 값이 나오는 경로라 모바일에서 가장 쓰기 쉽다.
    private func updateMeasure() {
        let n = engine.selectedCount()
        guard n > 0 else {
            if !measureText.isEmpty { measureText = ""; lastMeasureKey = "" }
            return
        }
        let len = engine.selectionLength()
        let area = engine.selectionArea()
        // 부동소수 그대로 비교하면 미세 변화로 매 프레임 갱신될 수 있어 표시 문자열로 비교한다.
        var parts: [String] = ["선택 \(n)"]
        if len > 0 { parts.append("길이 " + Self.fmt(len)) }
        if area > 0 { parts.append("면적 " + Self.fmt(area) + "²") }
        let text = parts.joined(separator: " · ")
        if text != lastMeasureKey { lastMeasureKey = text; measureText = text }
    }

    /// 값 크기에 따라 자릿수를 줄여 좁은 화면에서도 읽히게 한다.
    private static func fmt(_ v: Float) -> String {
        if v >= 100 { return String(format: "%.0f", v) }
        if v >= 10  { return String(format: "%.1f", v) }
        return String(format: "%.2f", v)
    }

    // 앱이 background 로 갈 때 호출 — Metal layer invalidate 와 race 방지.
    // shutdown 과 달리 엔진 자체는 유지, displayLink 만 멈춤.
    func pause() {
        displayLink?.isPaused = true
    }

    func resume() {
        guard engine.isCreated else { return }
        if let link = displayLink {
            link.isPaused = false
        } else {
            // 첫 attach 전이면 무시 (start() 가 곧 부를 것)
        }
    }

    func shutdown() {
        displayLink?.invalidate()
        displayLink = nil
        engine.shutdown()
    }

    private func startDisplayLink() {
        let link = CADisplayLink(target: self, selector: #selector(tick(_:)))
        link.add(to: .main, forMode: .common)
        displayLink = link
    }
}
