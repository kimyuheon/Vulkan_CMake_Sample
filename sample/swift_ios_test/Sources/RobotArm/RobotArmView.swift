import SwiftUI
import UIKit

// 로봇 팔 샘플 — 위: 3D 뷰, 아래: [재생] [홈] [맞춤] + 관절 슬라이더.
// 무엇을 움직일지(관절 → 조작 항목, 자동 재생)는 공용 로직 robot_demo 가 정하고,
// 여기는 그것을 슬라이더로 보여 주기만 한다. Android RobotActivity 와 같은 구성.
struct RobotArmView: View {
    @StateObject private var controller = RobotArmController()
    @Environment(\.scenePhase) private var scenePhase

    var body: some View {
        GeometryReader { geo in
            VStack(spacing: 0) {
                VulkanCADViewRepresentable(engine: controller.engine) { view in
                    controller.start(view: view)
                }
                .ignoresSafeArea(edges: .top)

                panel
                    .frame(height: geo.size.height * 0.45)
            }
        }
        .background(Color(red: 0.09, green: 0.10, blue: 0.12))
        .onChange(of: scenePhase) { phase in
            if phase == .active { controller.resume() } else { controller.pause() }
        }
    }

    private var panel: some View {
        VStack(spacing: 8) {
            HStack(spacing: 8) {
                Button(controller.playing ? "⏸ 정지" : "▶ 재생") { controller.togglePlay() }
                Button("홈 자세") { controller.home() }
                Button("화면 맞춤") { controller.engine.zoomExtents() }
            }
            .buttonStyle(.bordered)
            .tint(.orange)
            .padding(.top, 10)

            if !controller.status.isEmpty {
                Text(controller.status).font(.caption).foregroundColor(.red)
            }

            ScrollView {
                VStack(alignment: .leading, spacing: 4) {
                    ForEach(controller.controls) { c in
                        HStack {
                            Text(c.label).foregroundColor(.white)
                            Spacer()
                            Text(controller.valueText(c)).foregroundColor(.orange).monospacedDigit()
                        }
                        Slider(value: controller.binding(c), in: c.min...c.max)
                            .tint(Color(red: 0.5, green: 0.8, blue: 0.75))
                    }
                }
                .padding(.horizontal, 20)
                .padding(.bottom, 16)
            }
        }
    }
}

@MainActor
final class RobotArmController: ObservableObject {
    struct Control: Identifiable {
        let id: Int
        let label: String
        let unit: String
        let min: Float
        let max: Float
    }

    let engine = VulkanCADEngine()
    @Published private(set) var controls: [Control] = []
    @Published private(set) var values: [Float] = []
    @Published private(set) var playing = false
    @Published private(set) var status = ""

    private var displayLink: CADisplayLink?
    private var lastTimestamp: CFTimeInterval = 0
    private var frame = 0
    private static let syncEveryFrames = 6   // 재생 중 슬라이더를 따라 움직이는 주기(≈10Hz)

    func start(view: UIView) {
        (view as? VulkanCADMetalView)?.oneFingerOrbits = true   // 로봇은 돌려 보는 게 주목적
        // 런타임 에셋(models/fonts/textures)은 .app 번들 Resources 에 들어 있다(project.yml).
        _ = engine.setRuntimeAssetPath(Bundle.main.resourcePath ?? "")
        engine.attach(to: view)
        guard engine.create() else { status = "CAD_CreateEngine 실패"; return }
        let scale = view.window?.screen.scale ?? UIScreen.main.scale
        engine.resize(to: CGSize(width: view.bounds.width * scale, height: view.bounds.height * scale))

        guard RobotDemo_Load() else { status = "demo_arm.urdf 를 불러오지 못했습니다"; return }
        controls = (0..<Int(RobotDemo_ControlCount())).map { i in
            let n = Int32(i)
            return Control(id: i,
                           label: String(cString: RobotDemo_ControlLabel(n)),
                           unit: String(cString: RobotDemo_ControlUnit(n)),
                           min: RobotDemo_ControlMin(n),
                           max: RobotDemo_ControlMax(n))
        }
        syncValues()
        // 시험용 — 탭 없이 재생부터 (테스트 앱 VULKANCAD_OPEN 과 같은 방식).
        //   xcrun simctl launch <기기> com.vulkancad.robotarm 앞에 SIMCTL_CHILD_ROBOT_AUTOPLAY=1
        if ProcessInfo.processInfo.environment["ROBOT_AUTOPLAY"] != nil { togglePlay() }

        let link = CADisplayLink(target: self, selector: #selector(tick(_:)))
        link.add(to: .main, forMode: .common)
        displayLink = link
    }

    @objc private func tick(_ link: CADisplayLink) {
        let dt = lastTimestamp == 0 ? 0 : link.timestamp - lastTimestamp
        lastTimestamp = link.timestamp
        RobotDemo_Update(dt)   // 재생 중이면 관절을 움직인다 → 엔진이 다시 그리기 표시
        guard engine.tick() else { status = "엔진 tick 실패"; pause(); return }
        // 재생 중엔 슬라이더가 로봇을 따라 움직이게 — 매 프레임은 SwiftUI 갱신 비용이 커서 몇 프레임에 한 번.
        if playing {
            frame += 1
            if frame % Self.syncEveryFrames == 0 { syncValues() }
        }
    }

    func binding(_ c: Control) -> Binding<Float> {
        Binding(
            get: { [weak self] in self?.values[safe: c.id] ?? 0 },
            set: { [weak self] v in
                RobotDemo_SetControl(Int32(c.id), v)   // 손으로 잡으면 재생은 멈춘다
                self?.values[c.id] = v
                self?.playing = RobotDemo_IsPlaying()
            })
    }

    func valueText(_ c: Control) -> String {
        let v = values[safe: c.id] ?? 0
        return c.unit == "%" ? String(format: "%.0f %%", v) : String(format: "%.0f°", v)
    }

    func togglePlay() {
        RobotDemo_SetPlaying(!RobotDemo_IsPlaying())
        playing = RobotDemo_IsPlaying()
    }

    func home() {
        RobotDemo_Home()
        playing = RobotDemo_IsPlaying()
        syncValues()
    }

    private func syncValues() {
        values = controls.map { RobotDemo_GetControl(Int32($0.id)) }
    }

    // 백그라운드에선 렌더를 멈춘다 — Metal 레이어 무효화와 경합 방지 (테스트 앱과 같은 규약).
    func pause() { displayLink?.isPaused = true }
    func resume() {
        lastTimestamp = 0
        displayLink?.isPaused = false
    }
}

private extension Array {
    subscript(safe i: Int) -> Element? { indices.contains(i) ? self[i] : nil }
}
